using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bridge;

namespace BlocklyMruby
{
	public class Mruby
	{
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate void clearerr_t(int fno);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate int feof_t(int fno);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate int getc_t(int fno);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate int fwrite_t([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In]byte[] buffer, int len, int fno);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate int fflush_t(int fno);
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		delegate void abort_t();

		[DllImport("mruby.dll")]
		extern static int mruby_main(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), In]string[] argv);
		[DllImport("mruby.dll")]
		extern static int mrbc_main(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), In]string[] argv);
		[DllImport("mruby.dll")]
		extern static int mrdb_main(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), In]string[] argv);
		[DllImport("mruby.dll")]
		extern static int mrdb_break();
		[DllImport("mruby.dll")]
		extern static void set_func(clearerr_t pclearerr, feof_t pfeof, getc_t pgetc, fwrite_t pfwrite, fflush_t pfflush, abort_t pabort);
		[DllImport("mruby.dll")]
		extern static void clear_func();
		[DllImport("mruby.dll")]
		extern static int objdump_main(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), In]string[] argv);

		clearerr_t _clearerr;
		feof_t _feof;
		getc_t _getc;
		fwrite_t _fwrite;
		fflush_t _fflush;
		abort_t _abort;
		public event EventHandler<StdioEventArgs> Stdio;
		Thread _Thread;
		Queue<byte> _StdinBuf = new Queue<byte>();
		AutoResetEvent _Event = new AutoResetEvent(false);

		public bool IsRunning { get { return _Thread != null; } }

		public Mruby()
		{
			_clearerr = clearerr;
			_feof = feof;
			_getc = getc;
			_fwrite = fwrite;
			_fflush = fflush;
			_abort = abort;
			set_func(_clearerr, _feof, _getc, _fwrite, _fflush, _abort);
		}

		private void WriteStdout(StdioType type, string text)
		{
			Stdio?.Invoke(this, new StdioEventArgs(type, text));
		}

		public bool run(string[] args, Action<int> callback)
		{
			if (IsRunning)
				return false;

			_Thread = new Thread(() => {
				int ret = -1;
				WriteStdout(StdioType.Out, "> " + String.Join(" ", args) + "\n");
				try {
					switch (args[0]) {
					case "mruby":
						ret = mruby_main(args.Length, args);
						break;
					case "mrbc":
						ret = mrbc_main(args.Length, args);
						break;
					case "mrdb":
						ret = mrdb_main(args.Length, args);
						break;
					case "objdump":
						ret = objdump_main(args.Length, args);
						break;
					}
				}
				catch (Exception) {
					ret = -1;
				}
				WriteStdout(StdioType.Out, $"exit {ret}\n");
				_Thread = null;
				callback(ret);
			});
			_Thread.Start();

			return true;
		}

		public bool break_program()
		{
			if (!IsRunning)
				return false;

			return mrdb_break() != 0;
		}

		internal void WriteStdin(string data)
		{
			var str = Encoding.UTF8.GetBytes(data);
			lock (_StdinBuf) {
				foreach (var c in str)
					_StdinBuf.Enqueue(c);
			}
			_Event.Set();
		}

		public void clearerr(int fno)
		{
		}

		public int feof(int fno)
		{
			lock (_StdinBuf) {
				if (_StdinBuf.Count == 0)
					return -1/*EOF*/;
				return 0;
			}
		}

		public int getc(int fno)
		{
			lock (_StdinBuf) {
				if (_StdinBuf.Count > 0)
					return _StdinBuf.Dequeue();
			}
			_Event.WaitOne();
			lock (_StdinBuf) {
				if (_StdinBuf.Count > 0)
					return _StdinBuf.Dequeue();
			}
			return -1/*EOF*/;
		}

		public int fflush(int fno)
		{
			return 0;
		}

		public int fwrite([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In]byte[] buffer, int len, int fno)
		{
			WriteStdout((StdioType)fno, Encoding.UTF8.GetString(buffer, 0, len));
			return len;
		}

		public void abort()
		{
			WriteStdout(StdioType.Out, "abort!\n");
			throw new Exception();
		}
	}

	public enum StdioType
	{
		In,
		Out,
		Error,
	}

	public class StdioEventArgs : EventArgs
	{
		public StdioType Type { get; private set; }
		public string Text { get; private set; }

		public StdioEventArgs(StdioType type, string text)
		{
			Type = type;
			Text = text;
		}
	}
}
