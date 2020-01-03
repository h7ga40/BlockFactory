using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Bridge
{
	public class ResourceReader
	{
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		static extern IntPtr FindResource(IntPtr hModule, string lpName, uint lpType);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

		const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

		private string m_DllName;
		private IntPtr hMod;

		public string DllName { get { return m_DllName; } }

		public ResourceReader(string dllName)
		{
			m_DllName = dllName;
			hMod = LoadLibraryEx(dllName + ".dll", IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
		}

		public bool GetResource(string url, out byte[] bPtr)
		{
			bPtr = null;

			if (hMod == IntPtr.Zero)
				return false;

			IntPtr hRes = FindResource(hMod, url, 23);
			if (hRes == IntPtr.Zero)
				return false;

			uint size = SizeofResource(hMod, hRes);
			IntPtr pt = LoadResource(hMod, hRes);

			bPtr = new byte[size];
			Marshal.Copy(pt, bPtr, 0, (int)size);

			return true;
		}

		public Icon GetFavicon(Uri url)
		{
			if (url.Scheme == "res") {
				byte[] iconbuf;
				if (GetResource("favicon.ico", out iconbuf))
					return new Icon(new MemoryStream(iconbuf));
			}
			else {
				string iconurl = url.Scheme + "://" + url.Host + "/favicon.ico";
				WebRequest request = WebRequest.Create(iconurl);
				try {
					WebResponse response = request.GetResponse();

					return new Icon(response.GetResponseStream());
				}
				catch (Exception) {
				}
			}

			return null;
		}
	}
}
