using System;
using System.Text;

namespace Bridge.Html5
{
	public class Uint8Array
	{
		byte[] data;

		public Uint8Array(int len)
		{
			data = new byte[len];
		}

		public Uint8Array(byte[] data)
		{
			this.data = data;
		}

		public int Length { get { return data.Length; } }

		public Uint8Array SubArray(int offset, int length)
		{
			var result = new Uint8Array(length);
			if (offset + length > data.Length)
				length = data.Length - offset;
			for (int i = 0; i < length; i++)
				result[i] = data[offset + i];
			return result;
		}

		public void Set(Uint8Array source, int offset)
		{
			int len = offset + source.Length;
			if (len > data.Length)
				Array.Resize(ref data, len);
			for (int i = 0; i < len; i++)
				data[offset + i] = source[i];
		}

		public override string ToString()
		{
			return Encoding.UTF8.GetString(data);
		}

		public byte[] ToArray() { return data; }

		public byte this[int index] { get { return data[index]; } set { data[index] = value; } }
	}
}
