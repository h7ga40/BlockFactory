/*
** parse.y - mruby parser
**
** See Copyright Notice in mruby.h
*/
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlocklyMruby
{
	interface IEvaluatable
	{
		node evaluate(string method, JsArray<node> args);
	}

	class kwtable
	{
		public Uint8Array name;
		public MrbTokens id0;
		public MrbTokens id1;
		public mrb_lex_state_enum state;

		public kwtable(string name, MrbTokens id0, MrbTokens id1, mrb_lex_state_enum state)
		{
			this.name = MrbParser.UTF8StringToArray(name);
			this.id0 = id0;
			this.id1 = id1;
			this.state = state;
		}

		public kwtable(string name)
		{
			this.name = MrbParser.UTF8StringToArray(name);
		}
	}

	delegate int partial_hook_t(MrbParser p);

	public class MrbToken
	{
		private string m_Filename;
		private MrbTokens m_Kind;
		private string m_Token;
		private object m_Value;

		public string Filename { get { return m_Filename; } }
		public MrbTokens Kind { get { return m_Kind; } }
		public object Value { get { return m_Value; } }

		public node nd { get { return (node)m_Value; } set { this.m_Value = value; } }
		public mrb_sym id { get { return (mrb_sym)m_Value; } set { this.m_Value = value; } }
		public int num { get { return (int)m_Value; } set { this.m_Value = value; } }
		public stack_type stack { get { return (stack_type)m_Value; } set { this.m_Value = value; } }
		//public vtable vars { get { return (vtable)value; } set { this.value = value; } }

		public MrbToken(string filename)
		{
			m_Filename = filename;
		}

		internal void SetToken(MrbTokens kind, string token)
		{
			m_Kind = kind;
			m_Token = token;
		}
	}

	public partial class MrbParser : IMrbParser, MrbParser.yyInput, MrbParser.yyConsoleOut
	{
		const int MRB_PARSER_TOKBUF_MAX = 65536;
		const int MRB_PARSER_TOKBUF_SIZE = 256;

		Uint8Array s;
		int sp;
		public string filename {
			get {
				if (current_filename_index < filename_table.Length)
					return filename_table[current_filename_index];
				else
					return "(null)";
			}
		}
		public int lineno { get; set; }
		public int column { get; set; }

		mrb_lex_state_enum lstate;
		node lex_strterm;

		stack_type cond_stack;
		stack_type cmdarg_stack;
		int paren_nest;
		int lpar_beg;
		int in_def, in_single;
		bool cmd_start;
		locals_node locals;

		node pb;
		Uint8Array buf = new Uint8Array(MRB_PARSER_TOKBUF_SIZE);
		Uint8Array tokbuf;
		int tidx;
		int tsiz;

		node all_heredocs;
		node heredocs_from_nextline;
		node parsing_heredoc;
		node lex_strterm_before_heredoc;

		internal node tree;

		JsArray<string> filename_table = new JsArray<string>();
		int filename_table_length { get { return filename_table.Length; } }
		public int current_filename_index;

		internal partial_hook_t partial_hook;
		internal object partial_data;

		MrbToken yylval;

		public MrbParser()
		{
			yyConsole = this;
		}

		void mrb_assert(bool cond)
		{
			if (!cond) throw new Exception();
		}

		const int ERANGE = 1;
		int errno;
		static int memcmp(Uint8Array a, int aofs, Uint8Array b, int bofs, int len)
		{
			int result;
			for (int i = 0; i < len; i++) {
				result = a[i] - b[i];
				if (result != 0)
					return result;
			}
			return 0;
		}

		static bool isdigit(int c)
		{
			return (c >= '0' && c <= '9');
		}

		static bool isalnum(int c)
		{
			return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		}

		static int tolower(int c)
		{
			if (c >= 'A' && c <= 'Z')
				return c - 'A' + 'a';
			return c;
		}

		static int strlen(Uint8Array a, int ofs)
		{
			int i = ofs;
			for (; i < a.Length; i++) {
				if (a[i] == 0)
					break;
			}
			return i - ofs;
		}

		static int strncmp(Uint8Array a, int aofs, Uint8Array b, int bofs, int len)
		{
			int result;
			for (int i = 0; i < len; i++) {
				if (a[i + aofs] == 0) {
					if (b[i + bofs] == 0) {
						return 0;
					}
					return byte.MaxValue;
				}
				if (b[i + bofs] == 0) {
					return -byte.MaxValue;
				}

				result = a[i + aofs] - b[i + bofs];
				if (result != 0)
					return result;
			}
			return 0;
		}

		static int strchr(Uint8Array s, int ofs, int c)
		{
			int i = ofs;
			for (; i < s.Length; i++) {
				if (s[i] == c)
					break;
			}
			return i;
		}

		public static Uint8Array strndup(Uint8Array s, int ofs, int len)
		{
			return s.SubArray(ofs, len + 1);
		}

		public static Uint8Array strdup(Uint8Array s, int ofs)
		{
			return s.SubArray(ofs, strlen(s, ofs) + 1);
		}

		private static string escape(byte c)
		{
			switch ((char)c) {
			case '\\': return "\\";
			case '\n': return "\\n";
			case '\t': return "\\t";
			case '\r': return "\\r";
			case '\f': return "\\f";
			case '\v': return "\\v";
			case '\a': return "\\a";
			case '\x27': return "\\e";
			case '\b': return "\\b";
			case ' ': return "\\s";
			case '\0': return "\\0";
			default: return $"\\x{c:X}";
			}
		}

		internal static string UTF8ArrayToString(Uint8Array data, int idx)
		{
			bool esc;
			return UTF8ArrayToStringEsc(data, idx, out esc);
		}

		internal static string UTF8ArrayToStringEsc(Uint8Array data, int idx, out bool esc)
		{
			var str = "";
			int c = 0, t = 0, end = data.Length;
			var temp = new byte[6];

			esc = false;
			if (end > 0 && data[end - 1] == '\x0')
				end--;

			for (int i = idx; i < end; i++) {
				var d = data[i];
				temp[c] = d;
				if (t == 0) {
					// 1Byteコード
					if ((d & 0x80) == 0) {
						// 表示可能なコード
						if (d >= 0x20 && d < 0x7F)
							str += (char)d;
						// 表示不可ならエスケープ
						else {
							esc = true;
							str += escape(d);
						}
						continue;
					}
					// 2Byteコード
					else if ((d & 0xE0) == 0xC0) {
						t = 2;
					}
					// 3Byteコード
					else if ((d & 0xF0) == 0xE0) {
						t = 3;
					}
					// 4Byteコード
					else if ((d & 0xF8) == 0xF0) {
						t = 4;
					}
					// 5Byteコード
					else if ((d & 0xFC) == 0xF8) {
						t = 5;
					}
					// 6Byteコード
					else if ((d & 0xFE) == 0xFC) {
						t = 6;
					}
					// 表示不可ならエスケープ
					else {
						esc = true;
						str += escape(d);
						continue;
					}
					c = 1;
				}
				else {
					// 表示不可ならエスケープ
					if ((d & 0xC0) != 0x80) {
						for (int j = 0; j < c; j++) {
							esc = true;
							str += escape(temp[j]);
						}
						t = 0;
						c = 0;
						continue;
					}
					c++;
					// 表示可能なコード
					if (c == t) {
						switch (t) {
						case 2:
							str += ConvertFromUtf32(((temp[0] & 0x1F) << 6) | (temp[1] & 0x3F));
							break;
						case 3:
							str += ConvertFromUtf32(((temp[0] & 0x0F) << 12) | ((temp[1] & 0x3F) << 6) | (temp[2] & 0x3F));
							break;
						case 4:
							str += ConvertFromUtf32(((temp[0] & 0x07) << 18) | ((temp[1] & 0x3F) << 12) | ((temp[2] & 0x3F) << 6) | (temp[3] & 0x3F));
							break;
						case 5:
							str += ConvertFromUtf32(((temp[0] & 0x03) << 24) | ((temp[1] & 0x3F) << 18) | ((temp[2] & 0x3F) << 12) | ((temp[3] & 0x3F) << 6) | (temp[4] & 0x3F));
							break;
						case 6:
							str += ConvertFromUtf32(((temp[0] & 0x01) << 30) | ((temp[1] & 0x3F) << 24) | ((temp[2] & 0x3F) << 18) | ((temp[3] & 0x3F) << 12) | ((temp[4] & 0x3F) << 6) | (temp[5] & 0x3F));
							break;
						}
						t = 0;
						c = 0;
						continue;
					}
				}
			}

			if (c > 0)
				esc = true;
			for (int i = 0; i < c; i++) {
				str += escape(temp[i]);
			}

			return str;
		}

		// from Emscripten (http://kripken.github.io/emscripten-site/)
		// Gotcha: fromCharCode constructs a character from a UTF-16 encoded code (pair), not from a Unicode code point! So encode the code point to UTF-16 for constructing.
		// See http://unicode.org/faq/utf_bom.html#utf16-3
		public static string ConvertFromUtf32(int utf32)
		{
			var str = "";
			if (utf32 >= 0x10000) {
				var ch = utf32 - 0x10000;
				str += (char)(0xD800 | (ch >> 10));
				str += (char)(0xDC00 | (ch & 0x3FF));
			}
			else {
				str += (char)(utf32);
			}
			return str;
		}

		// from Emscripten (http://kripken.github.io/emscripten-site/)
		// Copies the given Javascript String object 'str' to the given byte array at address 'outIdx',
		// encoded in UTF8 form and null-terminated. The copy will require at most str.length*4+1 bytes of space in the HEAP.
		// Use the function lengthBytesUTF8() to compute the exact number of bytes (excluding null terminator) that this function will write.
		// Parameters:
		//   str: the Javascript string to copy.
		//   outU8Array: the array to copy to. Each index in this array is assumed to be one 8-byte element.
		//   outIdx: The starting offset in the array to begin the copying.
		//   maxBytesToWrite: The maximum number of bytes this function can write to the array. This count should include the null
		//                    terminator, i.e. if maxBytesToWrite=1, only the null terminator will be written and nothing else.
		//                    maxBytesToWrite=0 does not write any bytes to the output, not even the null terminator.
		// Returns the number of bytes written, EXCLUDING the null terminator.
		internal static int stringToUTF8Array(string str, Uint8Array outU8Array, int outIdx, int maxBytesToWrite)
		{
			if (!(maxBytesToWrite > 0)) // Parameter maxBytesToWrite is not optional. Negative values, 0, null, undefined and false each don't write out any bytes.
				return 0;

			var startIdx = outIdx;
			var endIdx = outIdx + maxBytesToWrite - 1; // -1 for string null terminator.
			for (var i = 0; i < str.Length; ++i) {
				// Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! So decode UTF16->UTF32->UTF8.
				// See http://unicode.org/faq/utf_bom.html#utf16-3
				// For UTF8 byte structure, see http://en.wikipedia.org/wiki/UTF-8#Description and https://www.ietf.org/rfc/rfc2279.txt and https://tools.ietf.org/html/rfc3629
				var u = str[i]; // possibly a lead surrogate
				if (u >= 0xD800 && u <= 0xDFFF) u = (char)(0x10000 + ((u & 0x3FF) << 10) | (str[++i] & 0x3FF));
				if (u <= 0x7F) {
					if (outIdx >= endIdx) break;
					outU8Array[outIdx++] = (byte)u;
				}
				else if (u <= 0x7FF) {
					if (outIdx + 1 >= endIdx) break;
					outU8Array[outIdx++] = (byte)(0xC0 | (u >> 6));
					outU8Array[outIdx++] = (byte)(0x80 | (u & 63));
				}
				else if (u <= 0xFFFF) {
					if (outIdx + 2 >= endIdx) break;
					outU8Array[outIdx++] = (byte)(0xE0 | (u >> 12));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 6) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | (u & 63));
				}
				else if (u <= 0x1FFFFF) {
					if (outIdx + 3 >= endIdx) break;
					outU8Array[outIdx++] = (byte)(0xF0 | (u >> 18));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 12) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 6) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | (u & 63));
				}
				else if (u <= 0x3FFFFFF) {
					if (outIdx + 4 >= endIdx) break;
					outU8Array[outIdx++] = (byte)(0xF8 | (u >> 24));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 18) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 12) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 6) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | (u & 63));
				}
				else {
					if (outIdx + 5 >= endIdx) break;
					outU8Array[outIdx++] = (byte)(0xFC | (u >> 30));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 24) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 18) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 12) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | ((u >> 6) & 63));
					outU8Array[outIdx++] = (byte)(0x80 | (u & 63));
				}
			}
			// Null-terminate the pointer to the buffer.
			outU8Array[outIdx] = 0;
			return outIdx - startIdx;
		}

		// from Emscripten (http://kripken.github.io/emscripten-site/)
		// Returns the number of bytes the given Javascript string takes if encoded as a UTF8 byte array, EXCLUDING the null terminator byte.
		internal static int lengthBytesUTF8(string str)
		{
			var len = 0;
			for (var i = 0; i < str.Length; ++i) {
				// Gotcha: charCodeAt returns a 16-bit word that is a UTF-16 encoded code unit, not a Unicode code point of the character! So decode UTF16->UTF32->UTF8.
				// See http://unicode.org/faq/utf_bom.html#utf16-3
				var u = str[i]; // possibly a lead surrogate
				if (u >= 0xD800 && u <= 0xDFFF) u = (char)(0x10000 + ((u & 0x3FF) << 10) | (str[++i] & 0x3FF));
				if (u <= 0x7F) {
					++len;
				}
				else if (u <= 0x7FF) {
					len += 2;
				}
				else if (u <= 0xFFFF) {
					len += 3;
				}
				else if (u <= 0x1FFFFF) {
					len += 4;
				}
				else if (u <= 0x3FFFFFF) {
					len += 5;
				}
				else {
					len += 6;
				}
			}
			return len;
		}

		internal static Uint8Array UTF8StringToArray(string str)
		{
			var len = lengthBytesUTF8(str) + 1;
			var result = new Uint8Array(len);
			stringToUTF8Array(str, result, 0, len);
			return result;
		}

		static ulong strtoul(Uint8Array s, int ofs, out Uint8Array endptr, int _base_)
		{
			if (_base_ != 10) throw new AggregateException();
			ulong result;
			if (UInt64.TryParse(UTF8ArrayToString(s.SubArray(ofs, s.Length - ofs + 1), 0), out result)) {
				endptr = null;
			}
			else {
				endptr = s;
			}
			return result;
		}

		static double mrb_float_read(Uint8Array s, int ofs, out Uint8Array endptr)
		{
			double result;
			if (Double.TryParse(UTF8ArrayToString(s.SubArray(ofs, s.Length - ofs + 1), 0), out result)) {
				endptr = null;
			}
			else {
				endptr = s;
			}
			return result;
		}

		bool identchar(int c) { return (ISALNUM(c) || (c) == '_' || !ISASCII(c)); }

		void BITSTACK_PUSH(ref stack_type stack, uint n) { stack = (stack_type)(((uint)stack << 1) | (n & 1)); }
		void BITSTACK_POP(ref stack_type stack) { stack = (stack_type)((uint)stack >> 1); }
		void BITSTACK_LEXPOP(ref stack_type stack) { stack = (stack_type)(((uint)stack >> 1) | ((uint)stack & 1)); }
		stack_type BITSTACK_SET_P(ref stack_type stack) { return (stack_type)((uint)stack & 1); }

		void COND_PUSH(uint n) { BITSTACK_PUSH(ref cond_stack, (n)); }
		void COND_POP() { BITSTACK_POP(ref cond_stack); }
		void COND_LEXPOP() { BITSTACK_LEXPOP(ref cond_stack); }
		stack_type COND_P() { return BITSTACK_SET_P(ref cond_stack); }

		void CMDARG_PUSH(uint n) { BITSTACK_PUSH(ref cmdarg_stack, (n)); }
		void CMDARG_POP() { BITSTACK_POP(ref cmdarg_stack); }
		void CMDARG_LEXPOP() { BITSTACK_LEXPOP(ref cmdarg_stack); }
		stack_type CMDARG_P() { return BITSTACK_SET_P(ref cmdarg_stack); }

		JsArray<string> syms = new JsArray<string>();

		private mrb_sym get_sym(string str)
		{
			int i = syms.IndexOf(str);
			if (i < 0) {
				i = syms.Length;
				syms.Push(str);
			}
			return (mrb_sym)(i + 1);
		}

		public string sym2name(mrb_sym sym)
		{
			int i = (int)sym - 1;
			if ((i < 0) || (i >= syms.Length))
				return ((int)sym).ToString();
			return syms[i];
		}

		mrb_sym mrb_intern(Uint8Array s, int len)
		{
			string str = UTF8ArrayToString(s.SubArray(0, len + 1), 0);
			return get_sym(str);
		}

		mrb_sym intern_cstr(Uint8Array s)
		{
			string str = UTF8ArrayToString(s.SubArray(0, strlen(s, 0) + 1), 0);
			return get_sym(str);
		}

		mrb_sym intern(string s, int len)
		{
			string str = s.Substring(0, len);
			return get_sym(str);
		}

		mrb_sym intern_c(char c)
		{
			string str = c.ToString();
			return get_sym(str);
		}

		public node cons(object car, object cdr)
		{
			return node.cons(this, car, cdr);
		}

		public node list1(object a)
		{
			return cons(a, null);
		}

		public node list2(object a, object b)
		{
			return cons(a, cons(b, null));
		}

		public node list3(object a, object b, object c)
		{
			return cons(a, cons(b, cons(c, null)));
		}

		public node list4(object a, object b, object c, object d)
		{
			return cons(a, cons(b, cons(c, cons(d, null))));
		}

		public node list5(object a, object b, object c, object d, object e)
		{
			return cons(a, cons(b, cons(c, cons(d, cons(e, null)))));
		}

		node append(node a, node b)
		{
			if (a == null) return b;
			a.append(b);
			return a;
		}

		node push(node a, object b)
		{
			return append(a, list1(b));
		}

		/* xxx ----------------------------- */

		locals_node local_switch()
		{
			var prev = this.locals;
			this.locals = new locals_node(null);
			return prev;
		}

		void local_resume(locals_node prev)
		{
			this.locals = prev;
		}

		void local_nest()
		{
			this.locals = new locals_node(this.locals);
		}

		void local_unnest()
		{
			if (this.locals != null) {
				this.locals = this.locals.cdr;
			}
		}

		bool local_var_p(mrb_sym sym)
		{
			locals_node l = this.locals;

			while (l != null) {
				if (l.symList.Contains(sym))
					return true;
				l = l.cdr;
			}
			return false;
		}

		void local_add_f(mrb_sym sym)
		{
			if (this.locals != null) {
				this.locals.push(sym);
			}
		}

		void local_add(mrb_sym sym)
		{
			if (!local_var_p(sym)) {
				local_add_f(sym);
			}
		}

		public JsArray<mrb_sym> locals_node()
		{
			return this.locals != null ? this.locals.symList : null;
		}

		/* (:scope (vars..) (prog...)) */
		scope_node new_scope(node body)
		{
			return new scope_node(this, body);
		}

		/* (:begin prog...) */
		begin_node new_begin(node body)
		{
			return new begin_node(this, body);
		}

		node newline_node(node n)
		{
			return n;
		}

		/* (:rescue body rescue else) */
		rescue_node new_rescue(node body, node resq, node els)
		{
			return new rescue_node(this, body, resq, els);
		}

		rescue_node new_mod_rescue(node body, node resq)
		{
			return new_rescue(body, list1(list3(null, null, resq)), null);
		}

		/* (:ensure body ensure) */
		ensure_node new_ensure(node a, node b)
		{
			return new ensure_node(this, a, b);
		}

		/* (:nil) */
		nil_node new_nil()
		{
			return new nil_node(this);
		}

		/* (:true) */
		true_node new_true()
		{
			return new true_node(this);
		}

		/* (:false) */
		false_node new_false()
		{
			return new false_node(this);
		}

		/* (:alias new old) */
		alias_node new_alias(mrb_sym a, mrb_sym b)
		{
			return new alias_node(this, a, b);
		}

		/* (:if cond then else) */
		if_node new_if(node a, node b, node c, bool inline = false)
		{
			void_expr_error(a);
			return new if_node(this, a, b, c, inline);
		}

		/* (:unless cond then else) */
		unless_node new_unless(node a, node b, node c)
		{
			void_expr_error(a);
			return new unless_node(this, a, b, c);
		}

		/* (:while cond body) */
		while_node new_while(node a, node b)
		{
			void_expr_error(a);
			return new while_node(this, a, b);
		}

		/* (:until cond body) */
		until_node new_until(node a, node b)
		{
			void_expr_error(a);
			return new until_node(this, a, b);
		}

		/* (:for var obj body) */
		for_node new_for(node v, node o, node b)
		{
			void_expr_error(o);
			return new for_node(this, v, o, b);
		}

		/* (:case a ((when ...) body) ((when...) body)) */
		case_node new_case(node a, node b)
		{
			void_expr_error(a);
			return new case_node(this, a, b);
		}

		/* (:postexe a) */
		postexe_node new_postexe(node a)
		{
			return new postexe_node(this, a);
		}

		/* (:self) */
		internal self_node new_self()
		{
			return new self_node(this);
		}

		/* (:call a b c) */
		call_node new_call(node a, mrb_sym b, node c, MrbTokens pass)
		{
			void_expr_error(a);
			return new call_node(this, a, b, c, pass);
		}

		/* (:fcall self mid args) */
		fcall_node new_fcall(mrb_sym b, node c)
		{
			return new fcall_node(this, b, c);
		}

		/* (:super . c) */
		super_node new_super(node c)
		{
			return new super_node(this, c);
		}

		/* (:zsuper) */
		zsuper_node new_zsuper()
		{
			return new zsuper_node(this);
		}

		/* (:yield . c) */
		yield_node new_yield(node c)
		{
			return new yield_node(this, c);
		}

		/* (:return . c) */
		return_node new_return(node c)
		{
			return new return_node(this, c);
		}

		/* (:break . c) */
		break_node new_break(node c)
		{
			return new break_node(this, c);
		}

		/* (:next . c) */
		next_node new_next(node c)
		{
			return new next_node(this, c);
		}

		/* (:redo) */
		redo_node new_redo()
		{
			return new redo_node(this);
		}

		/* (:retry) */
		retry_node new_retry()
		{
			return new retry_node(this);
		}

		/* (:dot2 a b) */
		dot2_node new_dot2(node a, node b)
		{
			return new dot2_node(this, a, b);
		}

		/* (:dot3 a b) */
		dot3_node new_dot3(node a, node b)
		{
			return new dot3_node(this, a, b);
		}

		/* (:colon2 b c) */
		colon2_node new_colon2(node b, mrb_sym c)
		{
			void_expr_error(b);
			return new colon2_node(this, b, c);
		}

		/* (:colon3 . c) */
		colon3_node new_colon3(mrb_sym c)
		{
			return new colon3_node(this, c);
		}

		/* (:and a b) */
		and_node new_and(node a, node b)
		{
			return new and_node(this, a, b);
		}

		/* (:or a b) */
		or_node new_or(node a, node b)
		{
			return new or_node(this, a, b);
		}

		/* (:array a...) */
		array_node new_array(node a)
		{
			return new array_node(this, a);
		}

		/* (:splat . a) */
		splat_node new_splat(node a)
		{
			return new splat_node(this, a);
		}

		/* (:hash (k . v) (k . v)...) */
		hash_node new_hash(node a)
		{
			return new hash_node(this, a);
		}

		/* (:sym . a) */
		sym_node new_sym(mrb_sym sym)
		{
			return new sym_node(this, sym);
		}

		mrb_sym new_strsym(node str)
		{
			Uint8Array s;
			int len;

			if (str is str_node) {
				s = ((str_node)str).str;
				len = ((str_node)str).len;
			}
			else {
				s = (Uint8Array)((node)str.cdr).car;
				len = (int)((node)str.cdr).cdr;
			}

			return mrb_intern(s, len);
		}

		/* (:lvar . a) */
		lvar_node new_lvar(mrb_sym sym)
		{
			return new lvar_node(this, sym);
		}

		/* (:gvar . a) */
		gvar_node new_gvar(mrb_sym sym)
		{
			return new gvar_node(this, sym);
		}

		/* (:ivar . a) */
		ivar_node new_ivar(mrb_sym sym)
		{
			return new ivar_node(this, sym);
		}

		/* (:cvar . a) */
		cvar_node new_cvar(mrb_sym sym)
		{
			return new cvar_node(this, sym);
		}

		/* (:const . a) */
		const_node new_const(mrb_sym sym)
		{
			return new const_node(this, sym);
		}

		/* (:undef a...) */
		undef_node new_undef(mrb_sym sym)
		{
			return new undef_node(this, sym);
		}

		/* (:class class super body) */
		class_node new_class(node c, node s, node b)
		{
			void_expr_error(s);
			return new class_node(this, c, s, b);
		}

		/* (:sclass obj body) */
		sclass_node new_sclass(node o, node b)
		{
			void_expr_error(o);
			return new sclass_node(this, o, b);
		}

		/* (:module module body) */
		module_node new_module(node m, node b)
		{
			return new module_node(this, m, b);
		}

		/* (:def m lv (arg . body)) */
		def_node new_def(mrb_sym m, node a, node b)
		{
			return new def_node(this, m, a, b);
		}

		/* (:sdef obj m lv (arg . body)) */
		sdef_node new_sdef(node o, mrb_sym m, node a, node b)
		{
			void_expr_error(o);
			return new sdef_node(this, o, m, a, b);
		}

		/* (:arg . sym) */
		arg_node new_arg(mrb_sym sym)
		{
			return new arg_node(this, sym);
		}

		/* (m o r m2 b) */
		/* m: (a b c) */
		/* o: ((a . e1) (b . e2)) */
		/* r: a */
		/* m2: (a b c) */
		/* b: a */
		node new_args(node m, node opt, mrb_sym rest, node m2, mrb_sym blk)
		{
			node n;

			n = cons(m2, blk);
			n = cons(rest, n);
			n = cons(opt, n);
			return cons(m, n);
		}

		/* (:block_arg . a) */
		block_arg_node new_block_arg(node a)
		{
			return new block_arg_node(this, a);
		}

		/* (:block arg body) */
		block_node new_block(node a, node b, bool brace)
		{
			return new block_node(this, a, b, brace);
		}

		/* (:lambda arg body) */
		lambda_node new_lambda(node a, node b)
		{
			return new lambda_node(this, a, b);
		}

		/* (:asgn lhs rhs) */
		asgn_node new_asgn(node a, node b)
		{
			void_expr_error(b);
			return new asgn_node(this, a, b);
		}

		/* (:masgn mlhs=(pre rest post)  mrhs) */
		masgn_node new_masgn(node a, node b)
		{
			void_expr_error(b);
			return new masgn_node(this, a, b);
		}

		/* (:asgn lhs rhs) */
		op_asgn_node new_op_asgn(node a, mrb_sym op, node b)
		{
			void_expr_error(b);
			return new op_asgn_node(this, a, op, b);
		}

		/* (:int . i) */
		int_node new_int(Uint8Array s, int _base)
		{
			return new int_node(this, s, _base);
		}

		int_node new_int(string s, int _base) { return new_int(MrbParser.UTF8StringToArray(s), _base); }

		/* (:float . i) */
		float_node new_float(Uint8Array s)
		{
			return new float_node(this, s);
		}

		/* (:str . (s . len)) */
		str_node new_str(Uint8Array s, int len)
		{
			return new str_node(this, s, len);
		}

		str_node new_str(string s, int len) { return new_str(MrbParser.UTF8StringToArray(s), len); }

		/* (:dstr . a) */
		internal dstr_node new_dstr(node a)
		{
			return new dstr_node(this, a);
		}

		/* (:str . (s . len)) */
		xstr_node new_xstr(Uint8Array s, int len)
		{
			return new xstr_node(this, s, len);
		}

		/* (:xstr . a) */
		dxstr_node new_dxstr(node a)
		{
			return new dxstr_node(this, a);
		}

		/* (:dsym . a) */
		dsym_node new_dsym(node a)
		{
			return new dsym_node(this, a);
		}

		/* (:regx . (s . (opt . enc))) */
		regx_node new_regx(Uint8Array p1, Uint8Array p2, Uint8Array p3)
		{
			return new regx_node(this, p1, p2, p3);
		}

		/* (:dregx . (a . b)) */
		dregx_node new_dregx(node a, node b)
		{
			return new dregx_node(this, a, b);
		}

		/* (:backref . n) */
		back_ref_node new_back_ref(int n)
		{
			return new back_ref_node(this, n);
		}

		/* (:nthref . n) */
		nth_ref_node new_nth_ref(int n)
		{
			return new nth_ref_node(this, n);
		}

		/* (:heredoc . a) */
		heredoc_node new_heredoc()
		{
			return new heredoc_node(this);
		}

		void new_bv(mrb_sym id)
		{
		}

		literal_delim_node new_literal_delim()
		{
			return new literal_delim_node(this);
		}

		/* (:words . a) */
		words_node new_words(node a)
		{
			return new words_node(this, a);
		}

		/* (:symbols . a) */
		symbols_node new_symbols(node a)
		{
			return new symbols_node(this, a);
		}

		filename_node new_filename(string s)
		{
			var str = MrbParser.UTF8StringToArray(s);
			return new filename_node(this, str, str.Length);
		}

		lineno_node new_lineno(int lineno)
		{
			return new lineno_node(this, lineno);
		}

		encoding_node new_encoding()
		{
			var str = MrbParser.UTF8StringToArray("UTF-8");
			return new encoding_node(this, str, str.Length);
		}

		/* xxx ----------------------------- */

		/* (:call a op) */
		node call_uni_op(node recv, string m)
		{
			void_expr_error(recv);
			return new_call(recv, intern_cstr(MrbParser.UTF8StringToArray(m)), null, (MrbTokens)1);
		}

		/* (:call a op b) */
		node call_bin_op(node recv, string m, node arg1)
		{
			return new_call(recv, intern_cstr(MrbParser.UTF8StringToArray(m)), list1(list1(arg1)), (MrbTokens)1);
		}

		void args_with_block(node a, node b)
		{
			if (b != null) {
				if (a.cdr != null) {
					yyError("both block arg and actual block given");
				}
				a.cdr = b;
			}
		}

		void call_with_block(node a, node b)
		{
			switch ((node_type)a.car) {
			case node_type.NODE_SUPER:
				((super_node)a).add_block(b);
				break;
			case node_type.NODE_ZSUPER:
				((zsuper_node)a).add_block(b);
				break;
			case node_type.NODE_CALL:
				((call_node)a).add_block(b);
				break;
			case node_type.NODE_FCALL:
				((fcall_node)a).add_block(b);
				break;
			default:
				break;
			}
		}

		node negate_lit(node n)
		{
			return new negate_node(this, n);
		}

		static node cond(node n)
		{
			return n;
		}

		node ret_args(node n)
		{
			if (n.cdr != null) {
				yyError("block argument should not be given");
				return null;
			}
			if (((node)n.car).cdr == null) return (node)((node)n.car).car;
			return new_array((node)n.car);
		}

		void assignable(node lhs)
		{
			var lvar = lhs as lvar_node;
			if (lvar != null) {
				local_add(lvar.name);
			}
		}

		node var_reference(node lhs)
		{
			node n;

			var lvar = lhs as lvar_node;
			if (lvar != null) {
				if (!local_var_p(lvar.name)) {
					n = new_fcall(lvar.name, null);
					return n;
				}
			}

			return lhs;
		}

		node new_strterm(mrb_string_type type, int term, int paren)
		{
			return cons(type, cons(0, cons(paren, term)));
		}

		void end_strterm()
		{
			this.lex_strterm = null;
		}

		parser_heredoc_info parsing_heredoc_inf()
		{
			node nd = this.parsing_heredoc;
			if (nd == null)
				return null;
			/* mrb_assert(nd.car.car == node_type.NODE_HEREDOC); */
			return ((heredoc_node)nd.car).info;
		}

		void heredoc_treat_nextline()
		{
			if (this.heredocs_from_nextline == null)
				return;
			if (this.parsing_heredoc == null) {
				node n;
				this.parsing_heredoc = this.heredocs_from_nextline;
				this.lex_strterm_before_heredoc = this.lex_strterm;
				this.lex_strterm = new_strterm(parsing_heredoc_inf().type, 0, 0);
				n = this.all_heredocs;
				if (n != null) {
					while (n.cdr != null)
						n = (node)n.cdr;
					n.cdr = this.parsing_heredoc;
				}
				else {
					this.all_heredocs = this.parsing_heredoc;
				}
			}
			else {
				node n, m;
				m = this.heredocs_from_nextline;
				while (m.cdr != null)
					m = (node)m.cdr;
				n = this.all_heredocs;
				mrb_assert(n != null);
				if (n == this.parsing_heredoc) {
					m.cdr = n;
					this.all_heredocs = this.heredocs_from_nextline;
					this.parsing_heredoc = this.heredocs_from_nextline;
				}
				else {
					while (n.cdr != this.parsing_heredoc) {
						n = (node)n.cdr;
						mrb_assert(n != null);
					}
					m.cdr = n.cdr;
					n.cdr = this.heredocs_from_nextline;
					this.parsing_heredoc = this.heredocs_from_nextline;
				}
			}
			this.heredocs_from_nextline = null;
		}

		void heredoc_end()
		{
			this.parsing_heredoc = (node)this.parsing_heredoc.cdr;
			if (this.parsing_heredoc == null) {
				this.lstate = mrb_lex_state_enum.EXPR_BEG;
				this.cmd_start = true;
				end_strterm();
				this.lex_strterm = (node)this.lex_strterm_before_heredoc;
				this.lex_strterm_before_heredoc = null;
			}
			else {
				/* next heredoc */
				this.lex_strterm.car = parsing_heredoc_inf().type;
			}
		}

		bool is_strterm_type(mrb_string_type str_func)
		{
			return (((int)lex_strterm.car) & (int)str_func) != 0;
		}

		static Uint8Array begin = MrbParser.UTF8StringToArray("begin");
		static Uint8Array end = MrbParser.UTF8StringToArray("\n=end");

		void pushback(int c)
		{
			if (c >= 0) {
				column--;
			}
			this.pb = cons(c, this.pb);
		}

		void backref_error(node n)
		{
			var c = (node_type)n.car;

			if (c == node_type.NODE_NTH_REF) {
				yyError("can't set variable ${0}", ((int)n.cdr).ToString());
			}
			else if (c == node_type.NODE_BACK_REF) {
				yyError("can't set variable ${0}", ((char)n.cdr).ToString());
			}
			else {
				//mrb_bug(mrb, "Internal error in backref_error() : n=>car == %S", mrb_fixnum_value(c));
			}
		}

		void void_expr_error(node n)
		{
			if (n == null) return;
			switch ((node_type)n.car) {
			case node_type.NODE_BREAK:
			case node_type.NODE_RETURN:
			case node_type.NODE_NEXT:
			case node_type.NODE_REDO:
			case node_type.NODE_RETRY:
				yyError("void value expression");
				break;
			case node_type.NODE_AND:
			case node_type.NODE_OR:
				void_expr_error((node)((node)n.cdr).car);
				void_expr_error((node)((node)n.cdr).cdr);
				break;
			case node_type.NODE_BEGIN:
				if (n.cdr != null) {
					while (n.cdr != null) {
						n = (node)n.cdr;
					}
					void_expr_error((node)n.car);
				}
				break;
			default:
				break;
			}
		}

		int nextc()
		{
			for (; ; ) {
				int c;

				if (this.pb != null) {
					node tmp;

					c = (int)this.pb.car;
					tmp = this.pb;
					this.pb = (node)this.pb.cdr;
				}
				else {
#if false
					if (this.f != null) {
						if (!this.f.CanRead)
							break;
						c = this.f.ReadByte();
						if (c == -1) break;
					}
					else
#endif
					if (this.s == null || this.sp >= this.s.Length) {
						break;
					}
					else {
						c = (byte)this.s[sp++];
					}
				}
				if (c >= 0) {
					this.column++;
				}
				if (c == '\r') {
					c = nextc();
					if (c != '\n') {
						pushback(c);
						return '\r';
					}
					return c;
				}
				return c;
			}

			if (this.partial_hook == null) return -1;
			else {
				if (this.partial_hook(this) < 0)
					return -1;                /* end of program(s) */
				return -2;                  /* end of a file in the program files */
			}
		}

		void skip(char term)
		{
			int c;

			for (; ; ) {
				c = nextc();
				if (c < 0) break;
				if (c == term) break;
			}
		}

		int peekc_n(int n)
		{
			node list = null;
			int c0;

			do {
				c0 = nextc();
				if (c0 == -1) return c0;    /* do not skip partial EOF */
				if (c0 >= 0) --column;
				list = push(list, c0);
			} while (n-- != 0);
			if (this.pb != null) {
				this.pb = append(list, this.pb);
			}
			else {
				this.pb = list;
			}
			return c0;
		}

		bool peek_n(int c, int n)
		{
			return peekc_n(n) == c && c >= 0;
		}

		bool peek(int c)
		{
			return peek_n(c, 0);
		}

		bool peeks(Uint8Array s, int p)
		{
			int len = strlen(s, p);
#if false
			if (this.f != null) {
				int n = 0;
				while (s[p] != 0) {
					if (!peek_n(s[p++], n++)) return false;
				}
				return true;
			}
			else
#endif
			if (this.s != null && this.sp + len <= this.s.Length) {
				if (memcmp(this.s, this.sp, s, p, len) == 0) return true;
			}
			return false;
		}

		bool skips(Uint8Array s, int p)
		{
			int c;

			for (; ; ) {
				/* skip until first char */
				for (; ; ) {
					c = nextc();
					if (c < 0) return false;
					if (c == '\n') {
						this.lineno++;
						this.column = 0;
					}
					if (c == s[p]) break;
				}
				p++;
				if (peeks(s, p)) {
					int len = strlen(s, p);

					while (len-- != 0) {
						if (nextc() == '\n') {
							this.lineno++;
							this.column = 0;
						}
					}
					return true;
				}
				else {
					p--;
				}
			}
		}

		int newtok()
		{
			if (this.tokbuf != this.buf) {
				//delete this.tokbuf;
				this.tokbuf = this.buf;
				this.tsiz = MRB_PARSER_TOKBUF_SIZE;
			}
			this.tidx = 0;
			return this.column - 1;
		}

		void tokadd(int c)
		{
			Uint8Array utf8 = new Uint8Array(4);
			int len;

			/* mrb_assert(-0x10FFFF <= c && c <= 0xFF); */
			if (c >= 0) {
				/* Single byte from source or non-Unicode escape */
				utf8[0] = (byte)c;
				len = 1;
			}
			else {
				/* Unicode character */
				c = -c;
				if (c < 0x80) {
					utf8[0] = (byte)c;
					len = 1;
				}
				else if (c < 0x800) {
					utf8[0] = (byte)(0xC0 | (c >> 6));
					utf8[1] = (byte)(0x80 | (c & 0x3F));
					len = 2;
				}
				else if (c < 0x10000) {
					utf8[0] = (byte)(0xE0 | (c >> 12));
					utf8[1] = (byte)(0x80 | ((c >> 6) & 0x3F));
					utf8[2] = (byte)(0x80 | (c & 0x3F));
					len = 3;
				}
				else {
					utf8[0] = (byte)(0xF0 | (c >> 18));
					utf8[1] = (byte)(0x80 | ((c >> 12) & 0x3F));
					utf8[2] = (byte)(0x80 | ((c >> 6) & 0x3F));
					utf8[3] = (byte)(0x80 | (c & 0x3F));
					len = 4;
				}
			}
			if (this.tidx + len >= this.tsiz) {
				if (this.tsiz >= MRB_PARSER_TOKBUF_MAX) {
					this.tidx += len;
					return;
				}
				this.tsiz *= 2;
				if (this.tokbuf == this.buf) {
					this.tokbuf = new Uint8Array(this.tsiz);
					//for(int i = 0; i < MRB_PARSER_TOKBUF_SIZE; i++) this.tokbuf[i] = this.buf[i];
					this.tokbuf.Set(this.buf, 0);
				}
				else {
					var buf = new Uint8Array(this.tsiz);
					buf.Set(this.tokbuf, 0);
					this.tokbuf = buf;
				}
			}
			for (int i = 0; i < len; i++) {
				this.tokbuf[this.tidx++] = utf8[i];
			}
		}

		int toklast()
		{
			return this.tokbuf[this.tidx - 1];
		}

		void tokfix()
		{
			if (this.tidx >= MRB_PARSER_TOKBUF_MAX) {
				this.tidx = MRB_PARSER_TOKBUF_MAX - 1;
				yyError("string too long (truncated)");
			}
			this.tokbuf[this.tidx] = (byte)'\0';
		}

		Uint8Array tok()
		{
			return this.tokbuf;
		}

		int toklen()
		{
			return this.tidx;
		}

		bool ISASCII(int c) { return ((uint)c <= 0x7f); }
		bool ISPRINT(int c) { return ((uint)(c - 0x20) < 0x5f); }
		bool ISSPACE(int c) { return (c == ' ' || (uint)(c - '\t') < 5); }
		bool ISUPPER(int c) { return ((uint)(c - 'A') < 26); }
		bool ISLOWER(int c) { return ((uint)(c - 'a') < 26); }
		bool ISALPHA(int c) { return ((uint)((c | 0x20) - 'a') < 26); }
		bool ISDIGIT(int c) { return ((uint)(c - '0') < 10); }
		bool ISXDIGIT(int c) { return (ISDIGIT(c) || (uint)((c | 0x20) - 'a') < 6); }
		bool ISALNUM(int c) { return (ISALPHA(c) || ISDIGIT(c)); }
		bool ISBLANK(int c) { return (c == ' ' || c == '\t'); }
		bool ISCNTRL(int c) { return ((uint)c < 0x20 || c == 0x7f); }
		int TOUPPER(int c) { return (ISLOWER(c) ? (c & 0x5f) : (c)); }
		int TOLOWER(int c) { return (ISUPPER(c) ? (c | 0x20) : (c)); }

		bool IS_ARG() { return (this.lstate == mrb_lex_state_enum.EXPR_ARG || this.lstate == mrb_lex_state_enum.EXPR_CMDARG); }
		bool IS_END() { return (this.lstate == mrb_lex_state_enum.EXPR_END || this.lstate == mrb_lex_state_enum.EXPR_ENDARG || this.lstate == mrb_lex_state_enum.EXPR_ENDFN); }
		bool IS_BEG() { return (this.lstate == mrb_lex_state_enum.EXPR_BEG || this.lstate == mrb_lex_state_enum.EXPR_MID || this.lstate == mrb_lex_state_enum.EXPR_VALUE || this.lstate == mrb_lex_state_enum.EXPR_CLASS); }
		bool IS_SPCARG(int c, bool space_seen) { return (IS_ARG() && space_seen && !ISSPACE(c)); }
		bool IS_LABEL_POSSIBLE(bool cmd_state) { return ((this.lstate == mrb_lex_state_enum.EXPR_BEG && !cmd_state) || IS_ARG()); }
		bool IS_LABEL_SUFFIX(int n) { return (peek_n(':', (n)) && !peek_n(':', (n) + 1)); }

		static int scan_oct(int[] start, int len, ref int retlen)
		{
			int s = 0;
			int retval = 0;

			/* mrb_assert(len <= 3) */
			while (len-- != 0 && start[s] >= '0' && start[s] <= '7') {
				retval <<= 3;
				retval |= start[s++] - '0';
			}
			retlen = s;

			return retval;
		}

		int scan_hex(int[] start, int len, ref int retlen)
		{
			Uint8Array hexdigit = MrbParser.UTF8StringToArray("0123456789abcdef0123456789ABCDEF");
			int s = 0;
			uint retval = 0;
			int tmp;

			/* mrb_assert(len <= 8) */
			while (len-- != 0 && start[s] != 0 && (tmp = strchr(hexdigit, 0, start[s])) >= 0) {
				retval <<= 4;
				retval |= (uint)tmp & 15;
				s++;
			}
			retlen = s;

			return (int)retval;
		}

		int read_escape_unicode(int limit)
		{
			for (; ; ) {
				int[] buf = new int[9];
				int i;
				int hex;

				/* Look for opening brace */
				i = 0;
				buf[0] = nextc();
				if (buf[0] < 0) break;
				if (ISXDIGIT(buf[0])) {
					bool error = false;
					/* \uxxxx form */
					for (i = 1; i < limit; i++) {
						buf[i] = nextc();
						if (buf[i] < 0) { error = true; break; }
						if (!ISXDIGIT(buf[i])) {
							pushback(buf[i]);
							break;
						}
					}
					if (error)
						break;
				}
				else {
					pushback(buf[0]);
				}
				hex = scan_hex(buf, i, ref i);
				if (i == 0 || hex > 0x10FFFF || (hex & 0xFFFFF800) == 0xD800) {
					yyError("invalid Unicode code point");
					return -1;
				}
				return hex;
			}

			yyError("invalid escape character syntax");
			return -1;
		}

		/* Return negative to indicate Unicode code point */
		int read_escape()
		{
			int c;

			switch (c = nextc()) {
			case '\\':/* Backslash */
				return c;

			case 'n':/* newline */
				return '\n';

			case 't':/* horizontal tab */
				return '\t';

			case 'r':/* carriage-return */
				return '\r';

			case 'f':/* form-feed */
				return '\f';

			case 'v':/* vertical tab */
				return '\v';

			case 'a':/* alarm(bell) */
				return '\x07';

			case 'e':/* escape */
				return 033;
			case '0':
			case '1':
			case '2':
			case '3': /* octal constant */
			case '4':
			case '5':
			case '6':
			case '7': {
				int[] buf = new int[3];
				int i;

				bool error = false;
				buf[0] = c;
				for (i = 1; i < 3; i++) {
					buf[i] = nextc();
					if (buf[i] < 0) {
						error = true;
						break;
					}
					if (buf[i] < '0' || '7' < buf[i]) {
						pushback(buf[i]);
						break;
					}
				}
				if (error)
					break;
				c = scan_oct(buf, i, ref i);
			}
			return c;

			case 'x':     /* hex constant */
				{
				int[] buf = new int[2];
				int i;

				bool error = false;
				for (i = 0; i < 2; i++) {
					buf[i] = nextc();
					if (buf[i] < 0) {
						error = true;
						break;
					}
					if (!ISXDIGIT(buf[i])) {
						pushback(buf[i]);
						break;
					}
				}
				if (error)
					break;
				c = scan_hex(buf, i, ref i);
				if (c < 0) return '\0';
				return c;
			}

			case 'u':     /* Unicode */
				if (peek('{')) {
					/* \u{xxxxxxxx} form */
					nextc();
					c = read_escape_unicode(8);
					if (c < 0) return 0;
					if (nextc() != '}') break;
				}
				else {
					c = read_escape_unicode(4);
					if (c < 0) return 0;
				}
				return -c;

			case 'b':/* backspace */
				return '\b';

			case 's':/* space */
				return ' ';

			case 'M':
				if ((c = nextc()) != '-') {
					yyError("Invalid escape character syntax");
					pushback(c);
					return '\0';
				}
				if ((c = nextc()) == '\\') {
					return read_escape() | 0x80;
				}
				else if (c < 0) break;
				else {
					return ((c & 0xff) | 0x80);
				}

			case 'C':
			case 'c':
				if (c == 'C') {
					if ((c = nextc()) != '-') {
						yyError("Invalid escape character syntax");
						pushback(c);
						return '\0';
					}
				}
				if ((c = nextc()) == '\\') {
					c = read_escape();
				}
				else if (c == '?')
					return 0177;
				else if (c < 0) break;
				return c & 0x9f;

			case -1:
			case -2:                      /* end of a file */
				break;

			default:
				return c;
			}

			yyError("Invalid escape character syntax");
			return '\0';
		}

		MrbTokens parse_string()
		{
			int c;
			var type = (mrb_string_type)this.lex_strterm.car;
			var nest_level = (int)((node)this.lex_strterm.cdr).car;
			var beg = (int)((node)((node)this.lex_strterm.cdr).cdr).car;
			var end = (int)((node)((node)this.lex_strterm.cdr).cdr).cdr;
			var hinf = (type & mrb_string_type.STR_FUNC_HEREDOC) != 0 ? parsing_heredoc_inf() : null;
			var cmd_state = this.cmd_start;

			if (beg == 0) beg = -3;       /* should never happen */
			if (end == 0) end = -3;
			newtok();
			while ((c = nextc()) != end || nest_level != 0) {
				if (hinf != null && (c == '\n' || c < 0)) {
					bool line_head;
					tokadd('\n');
					tokfix();
					this.lineno++;
					this.column = 0;
					line_head = hinf.line_head;
					hinf.line_head = true;
					if (line_head) {
						/* check whether end of heredoc */
						Uint8Array s = tok();
						int p = 0;
						int len = toklen();
						if (hinf.allow_indent) {
							while (ISSPACE(s[p]) && len > 0) {
								++p;
								--len;
							}
						}
						if ((len - 1 == hinf.term_len) && (strncmp(s, p, hinf.term, 0, len - 1) == 0)) {
							if (c < 0) {
								parsing_heredoc = null;
							}
							else {
								return MrbTokens.tHEREDOC_END;
							}
						}
					}
					if (c < 0) {
						yyError("can't find heredoc delimiter \"{0}\" anywhere before EOF", MrbParser.UTF8ArrayToString(hinf.term, 0));
						return 0;
					}
					yylval.nd = new_str(tok(), toklen());
					return MrbTokens.tHD_STRING_MID;
				}
				if (c < 0) {
					yyError("unterminated Uint8Array meets end of file");
					return 0;
				}
				else if (c == beg) {
					nest_level++;
					((node)this.lex_strterm.cdr).car = nest_level;
				}
				else if (c == end) {
					nest_level--;
					((node)this.lex_strterm.cdr).car = nest_level;
				}
				else if (c == '\\') {
					c = nextc();
					if ((type & mrb_string_type.STR_FUNC_EXPAND) != 0) {
						if (c == end || c == beg) {
							tokadd(c);
						}
						else if (c == '\n') {
							this.lineno++;
							this.column = 0;
							if ((type & mrb_string_type.STR_FUNC_ARRAY) != 0) {
								tokadd('\n');
							}
						}
						else if ((type & mrb_string_type.STR_FUNC_REGEXP) != 0) {
							tokadd('\\');
							tokadd(c);
						}
						else if (c == 'u' && peek('{')) {
							/* \u{xxxx xxxx xxxx} form */
							nextc();
							while (true) {
								do c = nextc(); while (ISSPACE(c));
								if (c == '}') break;
								pushback(c);
								c = read_escape_unicode(8);
								if (c < 0) break;
								tokadd(-c);
							}
							if (hinf != null)
								hinf.line_head = false;
						}
						else {
							pushback(c);
							tokadd(read_escape());
							if (hinf != null)
								hinf.line_head = false;
						}
					}
					else {
						if (c != beg && c != end) {
							if (c == '\n') {
								this.lineno++;
								this.column = 0;
							}
							if (!(c == '\\' || ((type & mrb_string_type.STR_FUNC_ARRAY) != 0 && ISSPACE(c)))) {
								tokadd('\\');
							}
						}
						tokadd(c);
					}
					continue;
				}
				else if ((c == '#') && (type & mrb_string_type.STR_FUNC_EXPAND) != 0) {
					c = nextc();
					if (c == '{') {
						tokfix();
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						this.cmd_start = true;
						yylval.nd = new_str(tok(), toklen());
						if (hinf != null) {
							hinf.line_head = false;
							return MrbTokens.tHD_STRING_PART;
						}
						return MrbTokens.tSTRING_PART;
					}
					tokadd('#');
					pushback(c);
					continue;
				}
				if ((type & mrb_string_type.STR_FUNC_ARRAY) != 0 && ISSPACE(c)) {
					if (toklen() == 0) {
						do {
							if (c == '\n') {
								this.lineno++;
								this.column = 0;
								heredoc_treat_nextline();
								if (this.parsing_heredoc != null) {
									return MrbTokens.tHD_LITERAL_DELIM;
								}
							}
							c = nextc();
						} while (ISSPACE(c));
						pushback(c);
						return MrbTokens.tLITERAL_DELIM;
					}
					else {
						pushback(c);
						tokfix();
						yylval.nd = new_str(tok(), toklen());
						return MrbTokens.tSTRING_MID;
					}
				}
				tokadd(c);
			}

			tokfix();
			this.lstate = mrb_lex_state_enum.EXPR_ENDARG;
			end_strterm();

			if ((type & mrb_string_type.STR_FUNC_XQUOTE) != 0) {
				yylval.nd = new_xstr(tok(), toklen());
				return MrbTokens.tXSTRING;
			}

			if ((type & mrb_string_type.STR_FUNC_REGEXP) != 0) {
				int f = 0;
				int re_opt;
				Uint8Array s = strndup(tok(), 0, toklen());
				Uint8Array flags = new Uint8Array(3);
				int flag = 0;
				var enc = (byte)'\0';
				Uint8Array encp;
				Uint8Array dup;

				newtok();
				while ((re_opt = nextc()) >= 0 && ISALPHA(re_opt)) {
					switch (re_opt) {
					case 'i': f |= 1; break;
					case 'x': f |= 2; break;
					case 'm': f |= 4; break;
					case 'u': f |= 16; break;
					case 'n': f |= 32; break;
					default: tokadd(re_opt); break;
					}
				}
				pushback(re_opt);
				if (toklen() != 0) {
					tokfix();
					yyError("unknown regexp option%s - %s", toklen() > 1 ? "s" : "", MrbParser.UTF8ArrayToString(tok().SubArray(0, toklen() + 1), 0));
				}
				if (f != 0) {
					if ((f & 1) != 0) flags[flag++] = (byte)'i';
					if ((f & 2) != 0) flags[flag++] = (byte)'x';
					if ((f & 4) != 0) flags[flag++] = (byte)'m';
					if ((f & 16) != 0) enc = (byte)'u';
					if ((f & 32) != 0) enc = (byte)'n';
				}
				if (flag > 0) {
					dup = strndup(flags, 0, flag);
				}
				else {
					dup = null;
				}
				if (enc != 0) {
					encp = strndup(new Uint8Array(new byte[] { enc }), 0, 1);
				}
				else {
					encp = null;
				}
				yylval.nd = new_regx(s, dup, encp);

				return MrbTokens.tREGEXP;
			}
			yylval.nd = new_str(tok(), toklen());
			if (IS_LABEL_POSSIBLE(cmd_state)) {
				if (IS_LABEL_SUFFIX(0)) {
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					nextc();
					return MrbTokens.tLABEL_END;
				}
			}
			return MrbTokens.tSTRING;
		}

		MrbTokens heredoc_identifier()
		{
			int c;
			mrb_string_type type = mrb_string_type.str_heredoc;
			bool indent = false;
			bool quote = false;
			heredoc_node newnode;
			parser_heredoc_info info;

			c = nextc();
			if (ISSPACE(c) || c == '=') {
				pushback(c);
				return 0;
			}
			if (c == '-') {
				indent = true;
				c = nextc();
			}
			if (c == '\'' || c == '"') {
				int term = c;
				if (c == '\'')
					quote = true;
				newtok();
				while ((c = nextc()) >= 0 && c != term) {
					if (c == '\n') {
						c = -1;
						break;
					}
					tokadd(c);
				}
				if (c < 0) {
					yyError("unterminated here document identifier");
					return 0;
				}
			}
			else {
				if (c < 0) {
					return 0;                 /* missing here document identifier */
				}
				if (!identchar(c)) {
					pushback(c);
					if (indent) pushback('-');
					return 0;
				}
				newtok();
				do {
					tokadd(c);
				} while ((c = nextc()) >= 0 && identchar(c));
				pushback(c);
			}
			tokfix();
			newnode = new_heredoc();
			info = newnode.info;
			info.term = strndup(tok(), 0, toklen());
			info.term_len = toklen();
			if (!quote)
				type |= mrb_string_type.STR_FUNC_EXPAND;
			info.type = type;
			info.allow_indent = indent;
			info.line_head = true;
			info.claer_doc();
			this.heredocs_from_nextline = push(this.heredocs_from_nextline, newnode);
			this.lstate = mrb_lex_state_enum.EXPR_END;

			yylval.nd = newnode;
			return MrbTokens.tHEREDOC_BEG;
		}

		MrbTokens start_num(int c)
		{
			int nondigit;

			nondigit = 0;
			this.lstate = mrb_lex_state_enum.EXPR_ENDARG;
			newtok();
			if (c == '-' || c == '+') {
				tokadd(c);
				c = nextc();
			}
			if (c == '0') {
				int start = toklen();
				c = nextc();
				if (c == 'x' || c == 'X') {
					/* hexadecimal */
					c = nextc();
					if (c >= 0 && ISXDIGIT(c)) {
						do {
							if (c == '_') {
								if (nondigit != 0) break;
								nondigit = c;
								continue;
							}
							if (!ISXDIGIT(c)) break;
							nondigit = 0;
							tokadd(tolower(c));
						} while ((c = nextc()) >= 0);
					}
					pushback(c);
					tokfix();
					if (toklen() == start) {
						yyError("numeric literal without digits");
						return 0;
					}
					else if (nondigit != 0) return trailing_uc(nondigit, 0);
					yylval.nd = new_int(tok(), 16);
					return MrbTokens.tINTEGER;
				}
				if (c == 'b' || c == 'B') {
					/* binary */
					c = nextc();
					if (c == '0' || c == '1') {
						do {
							if (c == '_') {
								if (nondigit != 0) break;
								nondigit = c;
								continue;
							}
							if (c != '0' && c != '1') break;
							nondigit = 0;
							tokadd(c);
						} while ((c = nextc()) >= 0);
					}
					pushback(c);
					tokfix();
					if (toklen() == start) {
						yyError("numeric literal without digits");
						return 0;
					}
					else if (nondigit != 0) return trailing_uc(nondigit, 0);
					yylval.nd = new_int(tok(), 2);
					return MrbTokens.tINTEGER;
				}
				if (c == 'd' || c == 'D') {
					/* decimal */
					c = nextc();
					if (c >= 0 && ISDIGIT(c)) {
						do {
							if (c == '_') {
								if (nondigit != 0) break;
								nondigit = c;
								continue;
							}
							if (!ISDIGIT(c)) break;
							nondigit = 0;
							tokadd(c);
						} while ((c = nextc()) >= 0);
					}
					pushback(c);
					tokfix();
					if (toklen() == start) {
						yyError("numeric literal without digits");
						return 0;
					}
					else if (nondigit != 0) return trailing_uc(nondigit, 0);
					yylval.nd = new_int(tok(), 10);
					return MrbTokens.tINTEGER;
				}
				if (c == 'o' || c == 'O') {
					/* prefixed octal */
					c = nextc();
					if (c < 0 || c == '_' || !ISDIGIT(c)) {
						yyError("numeric literal without digits");
						return 0;
					}
				}
				if ((c == '_')/* 0_0 */ ||
					(c >= '0' && c <= '7')/* octal */) {
					do {
						if (c == '_') {
							if (nondigit != 0) break;
							nondigit = c;
							continue;
						}
						if (c < '0' || c > '9') break;
						if (c > '7') {
							yyError("Invalid octal digit");
							return invalid_octal(c, nondigit);
						}
						nondigit = 0;
						tokadd(c);
					} while ((c = nextc()) >= 0);

					if (toklen() > start) {
						pushback(c);
						tokfix();
						if (nondigit != 0) return trailing_uc(nondigit, 0);
						yylval.nd = new_int(tok(), 8);
						return MrbTokens.tINTEGER;
					}
					if (nondigit != 0) {
						pushback(c);
						return trailing_uc(nondigit, 0);
					}
				}

				if (c > '7' && c <= '9') {
					yyError("Invalid octal digit");
					return invalid_octal(c, nondigit);
				}
				else if (c == '.' || c == 'e' || c == 'E') {
					tokadd('0');
					return invalid_octal(c, nondigit);
				}
				else {
					pushback(c);
					yylval.nd = new_int("0", 10);
					return MrbTokens.tINTEGER;
				}
			}

			return invalid_octal(c, nondigit);
		}

		MrbTokens invalid_octal(int c, int nondigit)
		{
			int is_float, seen_point, seen_e;

			is_float = seen_point = seen_e = 0;

			for (; ; ) {
				switch (c) {
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					nondigit = 0;
					tokadd(c);
					break;

				case '.':
					if (nondigit != 0) return trailing_uc(nondigit, is_float);
					if (seen_point != 0 || seen_e != 0) {
						pushback(c);
						return trailing_uc(nondigit, is_float);
					}
					else {
						int c0 = nextc();
						if (c0 < 0 || !ISDIGIT(c0)) {
							pushback(c0);
							pushback(c);
							return trailing_uc(nondigit, is_float);
						}
						c = c0;
					}
					tokadd('.');
					tokadd(c);
					is_float++;
					seen_point++;
					nondigit = 0;
					break;

				case 'e':
				case 'E':
					if (nondigit != 0) {
						pushback(c);
						c = nondigit;
						pushback(c);
						return trailing_uc(nondigit, is_float);
					}
					if (seen_e != 0) {
						pushback(c);
						return trailing_uc(nondigit, is_float);
					}
					tokadd(c);
					seen_e++;
					is_float++;
					nondigit = c;
					c = nextc();
					if (c != '-' && c != '+') continue;
					tokadd(c);
					nondigit = c;
					break;

				case '_':       /* '_' in number just ignored */
					if (nondigit != 0) {
						pushback(c);
						return trailing_uc(nondigit, is_float);
					}
					nondigit = c;
					break;

				default: {
					pushback(c);
					return trailing_uc(nondigit, is_float);
				}
				}
				c = nextc();
			}
		}

		MrbTokens trailing_uc(int nondigit, int is_float)
		{
			if (nondigit != 0) {
				yyError("trailing '{0}' in number", nondigit.ToString());
			}

			tokfix();
			if (is_float != 0) {
				double d;
				Uint8Array endp;

				errno = 0;
				d = mrb_float_read(tok(), 0, out endp);
				if (d == 0 && endp == tok()) {
					yyWarning("corrupted float value {0}", MrbParser.UTF8ArrayToString(tok().SubArray(0, toklen() + 1), 0));
				}
				else if (errno == ERANGE) {
					yyWarning("float {0} out of range", MrbParser.UTF8ArrayToString(tok().SubArray(0, toklen() + 1), 0));
					errno = 0;
				}
				yylval.nd = new_float(tok());
				return MrbTokens.tFLOAT;
			}
			yylval.nd = new_int(tok(), 10);
			return MrbTokens.tINTEGER;
		}

		bool arg_ambiguous()
		{
			yyWarning("ambiguous first argument; put parentheses or even spaces");
			return true;
		}

		MrbTokens quotation(int c)
		{
			int term;
			int paren;

			if (c < 0 || !ISALNUM(c)) {
				term = c;
				c = 'Q';
			}
			else {
				term = nextc();
				if (isalnum(term)) {
					yyError("unknown type of %string");
					return 0;
				}
			}
			if (c < 0 || term < 0) {
				yyError("unterminated quoted string meets end of file");
				return 0;
			}
			paren = term;
			if (term == '(') term = ')';
			else if (term == '[') term = ']';
			else if (term == '{') term = '}';
			else if (term == '<') term = '>';
			else paren = 0;

			switch (c) {
			case 'Q':
				this.lex_strterm = new_strterm(mrb_string_type.str_dquote, term, paren);
				return MrbTokens.tSTRING_BEG;

			case 'q':
				this.lex_strterm = new_strterm(mrb_string_type.str_squote, term, paren);
				return parse_string();

			case 'W':
				this.lex_strterm = new_strterm(mrb_string_type.str_dword, term, paren);
				return MrbTokens.tWORDS_BEG;

			case 'w':
				this.lex_strterm = new_strterm(mrb_string_type.str_sword, term, paren);
				return MrbTokens.tWORDS_BEG;

			case 'x':
				this.lex_strterm = new_strterm(mrb_string_type.str_xquote, term, paren);
				return MrbTokens.tXSTRING_BEG;

			case 'r':
				this.lex_strterm = new_strterm(mrb_string_type.str_regexp, term, paren);
				return MrbTokens.tREGEXP_BEG;

			case 's':
				this.lex_strterm = new_strterm(mrb_string_type.str_ssym, term, paren);
				return MrbTokens.tSYMBEG;

			case 'I':
				this.lex_strterm = new_strterm(mrb_string_type.str_dsymbols, term, paren);
				return MrbTokens.tSYMBOLS_BEG;

			case 'i':
				this.lex_strterm = new_strterm(mrb_string_type.str_ssymbols, term, paren);
				return MrbTokens.tSYMBOLS_BEG;

			default:
				yyError("unknown type of %string");
				return 0;
			}
		}

		static readonly Dictionary<string, kwtable> wordlist = new Dictionary<string, kwtable>()
		{
			{"break",           new kwtable("break",        MrbTokens.keyword_break,       MrbTokens.keyword_break,       mrb_lex_state_enum.EXPR_MID) },
			{"else",            new kwtable("else",         MrbTokens.keyword_else,        MrbTokens.keyword_else,        mrb_lex_state_enum.EXPR_BEG) },
			{"nil",             new kwtable("nil",          MrbTokens.keyword_nil,         MrbTokens.keyword_nil,         mrb_lex_state_enum.EXPR_END) },
			{"ensure",          new kwtable("ensure",       MrbTokens.keyword_ensure,      MrbTokens.keyword_ensure,      mrb_lex_state_enum.EXPR_BEG) },
			{"end",             new kwtable("end",          MrbTokens.keyword_end,         MrbTokens.keyword_end,         mrb_lex_state_enum.EXPR_END) },
			{"then",            new kwtable("then",         MrbTokens.keyword_then,        MrbTokens.keyword_then,        mrb_lex_state_enum.EXPR_BEG) },
			{"not",             new kwtable("not",          MrbTokens.keyword_not,         MrbTokens.keyword_not,         mrb_lex_state_enum.EXPR_ARG) },
			{"false",           new kwtable("false",        MrbTokens.keyword_false,       MrbTokens.keyword_false,       mrb_lex_state_enum.EXPR_END) },
			{"self",            new kwtable("self",         MrbTokens.keyword_self,        MrbTokens.keyword_self,        mrb_lex_state_enum.EXPR_END) },
			{"elsif",           new kwtable("elsif",        MrbTokens.keyword_elsif,       MrbTokens.keyword_elsif,       mrb_lex_state_enum.EXPR_VALUE) },
			{"rescue",          new kwtable("rescue",       MrbTokens.keyword_rescue,      MrbTokens.modifier_rescue,     mrb_lex_state_enum.EXPR_MID) },
			{"true",            new kwtable("true",         MrbTokens.keyword_true,        MrbTokens.keyword_true,        mrb_lex_state_enum.EXPR_END) },
			{"until",           new kwtable("until",        MrbTokens.keyword_until,       MrbTokens.modifier_until,      mrb_lex_state_enum.EXPR_VALUE) },
			{"unless",          new kwtable("unless",       MrbTokens.keyword_unless,      MrbTokens.modifier_unless,     mrb_lex_state_enum.EXPR_VALUE) },
			{"return",          new kwtable("return",       MrbTokens.keyword_return,      MrbTokens.keyword_return,      mrb_lex_state_enum.EXPR_MID) },
			{"def",             new kwtable("def",          MrbTokens.keyword_def,         MrbTokens.keyword_def,         mrb_lex_state_enum.EXPR_FNAME) },
			{"and",             new kwtable("and",          MrbTokens.keyword_and,         MrbTokens.keyword_and,         mrb_lex_state_enum.EXPR_VALUE) },
			{"do",              new kwtable("do",           MrbTokens.keyword_do,          MrbTokens.keyword_do,          mrb_lex_state_enum.EXPR_BEG) },
			{"yield",           new kwtable("yield",        MrbTokens.keyword_yield,       MrbTokens.keyword_yield,       mrb_lex_state_enum.EXPR_ARG) },
			{"for",             new kwtable("for",          MrbTokens.keyword_for,         MrbTokens.keyword_for,         mrb_lex_state_enum.EXPR_VALUE) },
			{"undef",           new kwtable("undef",        MrbTokens.keyword_undef,       MrbTokens.keyword_undef,       mrb_lex_state_enum.EXPR_FNAME) },
			{"or",              new kwtable("or",           MrbTokens.keyword_or,          MrbTokens.keyword_or,          mrb_lex_state_enum.EXPR_VALUE) },
			{"in",              new kwtable("in",           MrbTokens.keyword_in,          MrbTokens.keyword_in,          mrb_lex_state_enum.EXPR_VALUE) },
			{"when",            new kwtable("when",         MrbTokens.keyword_when,        MrbTokens.keyword_when,        mrb_lex_state_enum.EXPR_VALUE) },
			{"retry",           new kwtable("retry",        MrbTokens.keyword_retry,       MrbTokens.keyword_retry,       mrb_lex_state_enum.EXPR_END) },
			{"if",              new kwtable("if",           MrbTokens.keyword_if,          MrbTokens.modifier_if,         mrb_lex_state_enum.EXPR_VALUE) },
			{"case",            new kwtable("case",         MrbTokens.keyword_case,        MrbTokens.keyword_case,        mrb_lex_state_enum.EXPR_VALUE) },
			{"redo",            new kwtable("redo",         MrbTokens.keyword_redo,        MrbTokens.keyword_redo,        mrb_lex_state_enum.EXPR_END) },
			{"next",            new kwtable("next",         MrbTokens.keyword_next,        MrbTokens.keyword_next,        mrb_lex_state_enum.EXPR_MID) },
			{"super",           new kwtable("super",        MrbTokens.keyword_super,       MrbTokens.keyword_super,       mrb_lex_state_enum.EXPR_ARG) },
			{"module",          new kwtable("module",       MrbTokens.keyword_module,      MrbTokens.keyword_module,      mrb_lex_state_enum.EXPR_VALUE) },
			{"begin",           new kwtable("begin",        MrbTokens.keyword_begin,       MrbTokens.keyword_begin,       mrb_lex_state_enum.EXPR_BEG) },
			{"__LINE__",        new kwtable("__LINE__",     MrbTokens.keyword__LINE__,     MrbTokens.keyword__LINE__,     mrb_lex_state_enum.EXPR_END) },
			{"__FILE__",        new kwtable("__FILE__",     MrbTokens.keyword__FILE__,     MrbTokens.keyword__FILE__,     mrb_lex_state_enum.EXPR_END) },
			{"__ENCODING__",    new kwtable("__ENCODING__", MrbTokens.keyword__ENCODING__, MrbTokens.keyword__ENCODING__, mrb_lex_state_enum.EXPR_END) },
			{"END",             new kwtable("END",          MrbTokens.keyword_END,         MrbTokens.keyword_END,         mrb_lex_state_enum.EXPR_END) },
			{"alias",           new kwtable("alias",        MrbTokens.keyword_alias,       MrbTokens.keyword_alias,       mrb_lex_state_enum.EXPR_FNAME) },
			{"BEGIN",           new kwtable("BEGIN",        MrbTokens.keyword_BEGIN,       MrbTokens.keyword_BEGIN,       mrb_lex_state_enum.EXPR_END) },
			{"class",           new kwtable("class",        MrbTokens.keyword_class,       MrbTokens.keyword_class,       mrb_lex_state_enum.EXPR_CLASS) },
			{"while",           new kwtable("while",        MrbTokens.keyword_while,       MrbTokens.modifier_while,      mrb_lex_state_enum.EXPR_VALUE) },
		};

		kwtable mrb_reserved_word(Uint8Array str, int len)
		{
			var key = MrbParser.UTF8ArrayToString(str.SubArray(0, len + 1), 0);
			kwtable result;

			if (wordlist.TryGetValue(key, out result)) {
				return result;
			}

			return null;
		}

		MrbTokens parser_yylex()
		{
			int c;
			bool space_seen = false;
			bool cmd_state;
			mrb_lex_state_enum last_state;
			int token_column = 0;

			if (this.lex_strterm != null) {
				if (is_strterm_type(mrb_string_type.STR_FUNC_HEREDOC)) {
					if (this.parsing_heredoc != null)
						return parse_string();
				}
				else
					return parse_string();
			}
			cmd_state = this.cmd_start;
			this.cmd_start = false;
			for (; ; ) {
				last_state = this.lstate;
				switch (c = nextc()) {
				/* white spaces */
				case ' ':
				case '\t':
				case '\f':
				case '\r':
				case '\v':
					space_seen = true;
					continue;

				case '\x04':  /* ^D */
				case '\x1a':  /* ^Z */
				case '\0':    /* NUL */
				case -1:      /* end of script. */
				case '#':     /* it's a comment */
				case -2:      /* end of a file */
				case '\n':
					if (c == '#') {
						skip('\n');
					}
					else if ((c != -2) && (c != '\n')) {
						if (this.heredocs_from_nextline == null)
							return 0;
					}
					heredoc_treat_nextline();
					switch (this.lstate) {
					case mrb_lex_state_enum.EXPR_BEG:
					case mrb_lex_state_enum.EXPR_FNAME:
					case mrb_lex_state_enum.EXPR_DOT:
					case mrb_lex_state_enum.EXPR_CLASS:
					case mrb_lex_state_enum.EXPR_VALUE:
						this.lineno++;
						this.column = 0;
						if (this.parsing_heredoc != null) {
							if (this.lex_strterm != null) {
								return parse_string();
							}
						}
						continue;
					default:
						break;
					}
					if (this.parsing_heredoc != null) {
						return (MrbTokens)'\n';
					}
					bool retry = false;
					while ((c = nextc()) != 0) {
						switch (c) {
						case ' ':
						case '\t':
						case '\f':
						case '\r':
						case '\v':
							space_seen = true;
							continue;
						case '.':
							if ((c = nextc()) != '.') {
								pushback(c);
								pushback('.');
								retry = true;
							}
							break;
						case -1:                  /* EOF */
						case -2:                  /* end of a file */
							break;
						default:
							pushback(c);
							break;
						}
						break;
					}
					if (retry)
						continue;
					this.cmd_start = true;
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					return (MrbTokens)'\n';

				case '*':
					if ((c = nextc()) == '*') {
						if ((c = nextc()) == '=') {
							yylval.id = intern("**", 2);
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						c = (int)MrbTokens.tPOW;
					}
					else {
						if (c == '=') {
							yylval.id = intern_c('*');
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						if (IS_SPCARG(c, space_seen)) {
							yyWarning("'*' interpreted as argument prefix");
							c = (int)MrbTokens.tSTAR;
						}
						else if (IS_BEG()) {
							c = (int)MrbTokens.tSTAR;
						}
						else {
							c = '*';
						}
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					return (MrbTokens)c;

				case '!':
					c = nextc();
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
						if (c == '@') {
							return (MrbTokens)'!';
						}
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					if (c == '=') {
						return MrbTokens.tNEQ;
					}
					if (c == '~') {
						return MrbTokens.tNMATCH;
					}
					pushback(c);
					return (MrbTokens)'!';

				case '=':
					if (this.column == 1) {
						if (peeks(begin, 0)) {
							c = peekc_n(begin.Length - 1);
							if (c < 0 || ISSPACE(c)) {
								do {
									if (!skips(end, 0)) {
										yyError("embedded document meets end of file");
										return 0;
									}
									c = nextc();
								} while (!(c < 0 || ISSPACE(c)));
								if (c != '\n') skip('\n');
								this.lineno++;
								this.column = 0;
								continue;
							}
						}
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					if ((c = nextc()) == '=') {
						if ((c = nextc()) == '=') {
							return MrbTokens.tEQQ;
						}
						pushback(c);
						return MrbTokens.tEQ;
					}
					if (c == '~') {
						return MrbTokens.tMATCH;
					}
					else if (c == '>') {
						return MrbTokens.tASSOC;
					}
					pushback(c);
					return (MrbTokens)'=';

				case '<':
					c = nextc();
					if (c == '<' &&
						this.lstate != mrb_lex_state_enum.EXPR_DOT &&
						this.lstate != mrb_lex_state_enum.EXPR_CLASS &&
						!IS_END() &&
						(!IS_ARG() || space_seen)) {
						MrbTokens token = heredoc_identifier();
						if (token != 0)
							return token;
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						if (this.lstate == mrb_lex_state_enum.EXPR_CLASS) {
							this.cmd_start = true;
						}
					}
					if (c == '=') {
						if ((c = nextc()) == '>') {
							return MrbTokens.tCMP;
						}
						pushback(c);
						return MrbTokens.tLEQ;
					}
					if (c == '<') {
						if ((c = nextc()) == '=') {
							yylval.id = intern("<<", 2);
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						return MrbTokens.tLSHFT;
					}
					pushback(c);
					return (MrbTokens)'<';

				case '>':
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					if ((c = nextc()) == '=') {
						return MrbTokens.tGEQ;
					}
					if (c == '>') {
						if ((c = nextc()) == '=') {
							yylval.id = intern(">>", 2);
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						return MrbTokens.tRSHFT;
					}
					pushback(c);
					return (MrbTokens)'>';

				case '"':
					this.lex_strterm = new_strterm(mrb_string_type.str_dquote, '"', 0);
					return MrbTokens.tSTRING_BEG;

				case '\'':
					this.lex_strterm = new_strterm(mrb_string_type.str_squote, '\'', 0);
					return parse_string();

				case '`':
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME) {
						this.lstate = mrb_lex_state_enum.EXPR_ENDFN;
						return (MrbTokens)'`';
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						if (cmd_state)
							this.lstate = mrb_lex_state_enum.EXPR_CMDARG;
						else
							this.lstate = mrb_lex_state_enum.EXPR_ARG;
						return (MrbTokens)'`';
					}
					this.lex_strterm = new_strterm(mrb_string_type.str_xquote, '`', 0);
					return MrbTokens.tXSTRING_BEG;

				case '?':
					if (IS_END()) {
						this.lstate = mrb_lex_state_enum.EXPR_VALUE;
						return (MrbTokens)'?';
					}
					c = nextc();
					if (c < 0) {
						yyError("incomplete character syntax");
						return 0;
					}
					if (ISSPACE(c)) {
						if (!IS_ARG()) {
							int c2;
							switch (c) {
							case ' ':
								c2 = 's';
								break;
							case '\n':
								c2 = 'n';
								break;
							case '\t':
								c2 = 't';
								break;
							case '\v':
								c2 = 'v';
								break;
							case '\r':
								c2 = 'r';
								break;
							case '\f':
								c2 = 'f';
								break;
							default:
								c2 = 0;
								break;
							}
							if (c2 != 0) {
								yyError(String.Format("invalid character syntax; use ?\\{0}", c2));
							}
						}

						pushback(c);
						this.lstate = mrb_lex_state_enum.EXPR_VALUE;
						return (MrbTokens)'?';
					}
					newtok();
					/* need support UTF-8 if configured */
					if ((isalnum(c) || c == '_')) {
						int c2 = nextc();
						pushback(c2);
						if ((isalnum(c2) || c2 == '_')) {
							pushback(c);
							this.lstate = mrb_lex_state_enum.EXPR_VALUE;
							return (MrbTokens)'?';
						}
					}
					if (c == '\\') {
						c = read_escape();
						tokadd(c);
					}
					else {
						tokadd(c);
					}
					tokfix();
					yylval.nd = new_str(tok(), toklen());
					this.lstate = mrb_lex_state_enum.EXPR_ENDARG;
					return MrbTokens.tCHAR;

				case '&':
					if ((c = nextc()) == '&') {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						if ((c = nextc()) == '=') {
							yylval.id = intern("&&", 2);
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						return MrbTokens.tANDOP;
					}
					else if (c == '.') {
						this.lstate = mrb_lex_state_enum.EXPR_DOT;
						return MrbTokens.tANDDOT;
					}
					else if (c == '=') {
						yylval.id = intern_c('&');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					pushback(c);
					if (IS_SPCARG(c, space_seen)) {
						yyWarning("'&' interpreted as argument prefix");
						c = (int)MrbTokens.tAMPER;
					}
					else if (IS_BEG()) {
						c = (int)MrbTokens.tAMPER;
					}
					else {
						c = '&';
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					return (MrbTokens)c;

				case '|':
					if ((c = nextc()) == '|') {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						if ((c = nextc()) == '=') {
							yylval.id = intern("||", 2);
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						pushback(c);
						return MrbTokens.tOROP;
					}
					if (c == '=') {
						yylval.id = intern_c('|');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					pushback(c);
					return (MrbTokens)'|';

				case '+':
					c = nextc();
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
						if (c == '@') {
							return MrbTokens.tUPLUS;
						}
						pushback(c);
						return (MrbTokens)'+';
					}
					if (c == '=') {
						yylval.id = intern_c('+');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					if (IS_BEG() || (IS_SPCARG(c, space_seen) && arg_ambiguous())) {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						pushback(c);
						if (c >= 0 && ISDIGIT(c)) {
							c = '+';
							return start_num(c);
						}
						return MrbTokens.tUPLUS;
					}
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					pushback(c);
					return (MrbTokens)'+';

				case '-':
					c = nextc();
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
						if (c == '@') {
							return MrbTokens.tUMINUS;
						}
						pushback(c);
						return (MrbTokens)'-';
					}
					if (c == '=') {
						yylval.id = intern_c('-');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					if (c == '>') {
						this.lstate = mrb_lex_state_enum.EXPR_ENDFN;
						return MrbTokens.tLAMBDA;
					}
					if (IS_BEG() || (IS_SPCARG(c, space_seen) && arg_ambiguous())) {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						pushback(c);
						if (c >= 0 && ISDIGIT(c)) {
							return MrbTokens.tUMINUS_NUM;
						}
						return MrbTokens.tUMINUS;
					}
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					pushback(c);
					return (MrbTokens)'-';

				case '.':
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					if ((c = nextc()) == '.') {
						if ((c = nextc()) == '.') {
							return MrbTokens.tDOT3;
						}
						pushback(c);
						return MrbTokens.tDOT2;
					}
					pushback(c);
					if (c >= 0 && ISDIGIT(c)) {
						yyError("no .<digit> floating literal anymore; put 0 before dot");
					}
					this.lstate = mrb_lex_state_enum.EXPR_DOT;
					return (MrbTokens)'.';

				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return start_num(c);

				case ')':
				case ']':
				case '}':
					if (c != '}') {
						this.paren_nest--;
					}
					COND_LEXPOP();
					CMDARG_LEXPOP();
					if (c == ')')
						this.lstate = mrb_lex_state_enum.EXPR_ENDFN;
					else
						this.lstate = mrb_lex_state_enum.EXPR_END;
					return (MrbTokens)c;

				case ':':
					c = nextc();
					if (c == ':') {
						if (IS_BEG() || this.lstate == mrb_lex_state_enum.EXPR_CLASS || IS_SPCARG(-1, space_seen)) {
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tCOLON3;
						}
						this.lstate = mrb_lex_state_enum.EXPR_DOT;
						return MrbTokens.tCOLON2;
					}
					if (IS_END() || ISSPACE(c)) {
						pushback(c);
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return (MrbTokens)':';
					}
					pushback(c);
					this.lstate = mrb_lex_state_enum.EXPR_FNAME;
					return MrbTokens.tSYMBEG;

				case '/':
					if (IS_BEG()) {
						this.lex_strterm = new_strterm(mrb_string_type.str_regexp, '/', 0);
						return MrbTokens.tREGEXP_BEG;
					}
					if ((c = nextc()) == '=') {
						yylval.id = intern_c('/');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					pushback(c);
					if (IS_SPCARG(c, space_seen)) {
						this.lex_strterm = new_strterm(mrb_string_type.str_regexp, '/', 0);
						return MrbTokens.tREGEXP_BEG;
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					return (MrbTokens)'/';

				case '^':
					if ((c = nextc()) == '=') {
						yylval.id = intern_c('^');
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						return MrbTokens.tOP_ASGN;
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					pushback(c);
					return (MrbTokens)'^';

				case ';':
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					return (MrbTokens)';';

				case ',':
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					return (MrbTokens)',';

				case '~':
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						if ((c = nextc()) != '@') {
							pushback(c);
						}
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					return (MrbTokens)'~';

				case '(':
					if (IS_BEG()) {
						c = (int)MrbTokens.tLPAREN;
					}
					else if (IS_SPCARG(-1, space_seen)) {
						c = (int)MrbTokens.tLPAREN_ARG;
					}
					this.paren_nest++;
					COND_PUSH(0);
					CMDARG_PUSH(0);
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					return (MrbTokens)c;

				case '[':
					this.paren_nest++;
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
						if ((c = nextc()) == ']') {
							if ((c = nextc()) == '=') {
								return MrbTokens.tASET;
							}
							pushback(c);
							return MrbTokens.tAREF;
						}
						pushback(c);
						return (MrbTokens)'[';
					}
					else if (IS_BEG()) {
						c = (int)MrbTokens.tLBRACK;
					}
					else if (IS_ARG() && space_seen) {
						c = (int)MrbTokens.tLBRACK;
					}
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					COND_PUSH(0);
					CMDARG_PUSH(0);
					return (MrbTokens)c;

				case '{':
					if (this.lpar_beg != 0 && this.lpar_beg == this.paren_nest) {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
						this.lpar_beg = 0;
						this.paren_nest--;
						COND_PUSH(0);
						CMDARG_PUSH(0);
						return MrbTokens.tLAMBEG;
					}
					if (IS_ARG() || this.lstate == mrb_lex_state_enum.EXPR_END || this.lstate == mrb_lex_state_enum.EXPR_ENDFN)
						c = '{';          /* block (primary) */
					else if (this.lstate == mrb_lex_state_enum.EXPR_ENDARG)
						c = (int)MrbTokens.tLBRACE_ARG;  /* block (expr) */
					else
						c = (int)MrbTokens.tLBRACE;      /* hash */
					COND_PUSH(0);
					CMDARG_PUSH(0);
					this.lstate = mrb_lex_state_enum.EXPR_BEG;
					return (MrbTokens)c;

				case '\\':
					c = nextc();
					if (c == '\n') {
						this.lineno++;
						this.column = 0;
						space_seen = true;
						continue; /* skip \\n */
					}
					pushback(c);
					return (MrbTokens)'\\';

				case '%':
					if (IS_BEG()) {
						c = nextc();
						return quotation(c);
					}
					for (; ; ) {
						if ((c = nextc()) == '=') {
							yylval.id = intern_c('%');
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							return MrbTokens.tOP_ASGN;
						}
						if (IS_SPCARG(c, space_seen)) {
							return quotation(c);
						}
						break;
					}
					if (this.lstate == mrb_lex_state_enum.EXPR_FNAME || this.lstate == mrb_lex_state_enum.EXPR_DOT) {
						this.lstate = mrb_lex_state_enum.EXPR_ARG;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_BEG;
					}
					pushback(c);
					return (MrbTokens)'%';

				case '$':
					this.lstate = mrb_lex_state_enum.EXPR_END;
					token_column = newtok();
					c = nextc();
					if (c < 0) {
						yyError("incomplete global variable syntax");
						return 0;
					}
					switch (c) {
					case '_':     /* $_: last read line string */
					case '~':     /* $~: match-data */
					case '*':     /* $*: argv */
					case '$':     /* $$: pid */
					case '?':     /* $?: last status */
					case '!':     /* $!: error string */
					case '@':     /* $@: error position */
					case '/':     /* $/: input record separator */
					case '\\':    /* $\: output record separator */
					case ';':     /* $;: field separator */
					case ',':     /* $,: output field separator */
					case '.':     /* $.: last read line number */
					case '=':     /* $=: ignorecase */
					case ':':     /* $:: load path */
					case '<':     /* $<: reading filename */
					case '>':     /* $>: default output handle */
					case '\"':    /* $": already loaded files */
						if (c == '_') {
							c = nextc();
							if (c >= 0 && identchar(c)) { /* if there is more after _ it is a variable */
								tokadd('$');
								tokadd(c);
								break;
							}
							pushback(c);
							c = '_';
						}
						tokadd('$');
						tokadd(c);
						tokfix();
						yylval.id = intern_cstr(tok());
						return MrbTokens.tGVAR;

					case '-':
						tokadd('$');
						tokadd(c);
						c = nextc();
						pushback(c);
						tokfix();
						yylval.id = intern_cstr(tok());
						return MrbTokens.tGVAR;

					case '&':     /* $&: last match */
					case '`':     /* $`: string before last match */
					case '\'':    /* $': string after last match */
					case '+':     /* $+: string matches last pattern */
						if (last_state == mrb_lex_state_enum.EXPR_FNAME) {
							tokadd('$');
							tokadd(c);
							tokfix();
							yylval.id = intern_cstr(tok());
							return MrbTokens.tGVAR;
						}
						yylval.nd = new_back_ref(c);
						return MrbTokens.tBACK_REF;

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						do {
							tokadd(c);
							c = nextc();
						} while (c >= 0 && isdigit(c));
						pushback(c);
						if (last_state == mrb_lex_state_enum.EXPR_FNAME) {
							tokfix();
							yylval.id = intern_cstr(tok());
							return MrbTokens.tGVAR;
						}
						tokfix(); {
							Uint8Array t;
							ulong n = strtoul(tok(), 0, out t, 10);
							if (n > int.MaxValue) {
								yyError("capture group index must be <= {0}", int.MaxValue.ToString());
								return 0;
							}
							yylval.nd = new_nth_ref((int)n);
						}
						return MrbTokens.tNTH_REF;

					default:
						if (!identchar(c)) {
							pushback(c);
							return (MrbTokens)'$';
						}
						tokadd('$');
						break;

					case '0':
						tokadd('$');
						break;
					}
					break;

				case '@':
					c = nextc();
					token_column = newtok();
					tokadd('@');
					if (c == '@') {
						tokadd('@');
						c = nextc();
					}
					if (c < 0) {
						if (this.tidx == 1) {
							yyError("incomplete instance variable syntax");
						}
						else {
							yyError("incomplete class variable syntax");
						}
						return 0;
					}
					else if (isdigit(c)) {
						if (this.tidx == 1) {
							yyError("'@{0}' is not allowed as an instance variable name", ((char)c).ToString());
						}
						else {
							yyError("'@@{0}' is not allowed as a class variable name", ((char)c).ToString());
						}
						return 0;
					}
					if (!identchar(c)) {
						pushback(c);
						return (MrbTokens)'@';
					}
					break;

				case '_':
					token_column = newtok();
					break;

				default:
					if (!identchar(c)) {
						yyError("Invalid char '\\x{0}' in expression", c.ToString("X2"));
						continue;
					}

					token_column = newtok();
					break;
				}
				break;
			}

			do {
				tokadd(c);
				c = nextc();
				if (c < 0) break;
			} while (identchar(c));
			if (token_column == 0 && toklen() == 7 && (c < 0 || c == '\n') &&
				strncmp(tok(), 0, MrbParser.UTF8StringToArray("__END__"), 0, toklen()) == 0)
				return (MrbTokens)(-1);

			switch ((char)tok()[0]) {
			case '@':
			case '$':
				pushback(c);
				break;
			default:
				if ((c == '!' || c == '?') && !peek('=')) {
					tokadd(c);
				}
				else {
					pushback(c);
				}
				break;
			}
			tokfix();
			{
				MrbTokens result = 0;

				switch ((char)tok()[0]) {
				case '$':
					this.lstate = mrb_lex_state_enum.EXPR_END;
					result = MrbTokens.tGVAR;
					break;
				case '@':
					this.lstate = mrb_lex_state_enum.EXPR_END;
					if (tok()[1] == '@')
						result = MrbTokens.tCVAR;
					else
						result = MrbTokens.tIVAR;
					break;

				default:
					if (toklast() == '!' || toklast() == '?') {
						result = MrbTokens.tFID;
					}
					else {
						if (this.lstate == mrb_lex_state_enum.EXPR_FNAME) {
							if ((c = nextc()) == '=' && !peek('~') && !peek('>') &&
								(!peek('=') || (peek_n('>', 1)))) {
								result = MrbTokens.tIDENTIFIER;
								tokadd(c);
								tokfix();
							}
							else {
								pushback(c);
							}
						}
						if (result == 0 && ISUPPER(tok()[0])) {
							result = MrbTokens.tCONSTANT;
						}
						else {
							result = MrbTokens.tIDENTIFIER;
						}
					}

					if (IS_LABEL_POSSIBLE(cmd_state)) {
						if (IS_LABEL_SUFFIX(0)) {
							this.lstate = mrb_lex_state_enum.EXPR_BEG;
							nextc();
							tokfix();
							yylval.id = intern_cstr(tok());
							return MrbTokens.tLABEL;
						}
					}
					if (this.lstate != mrb_lex_state_enum.EXPR_DOT) {
						kwtable kw;
						/* See if it is a reserved word.  */
						kw = mrb_reserved_word(tok(), toklen());
						if (kw != null) {
							mrb_lex_state_enum state = this.lstate;
							yylval.num = this.lineno;
							this.lstate = kw.state;
							if (state == mrb_lex_state_enum.EXPR_FNAME) {
								yylval.id = intern_cstr(kw.name);
								return kw.id0;
							}
							if (this.lstate == mrb_lex_state_enum.EXPR_BEG) {
								this.cmd_start = true;
							}
							if (kw.id0 == MrbTokens.keyword_do) {
								if (this.lpar_beg != 0 && this.lpar_beg == this.paren_nest) {
									this.lpar_beg = 0;
									this.paren_nest--;
									return MrbTokens.keyword_do_LAMBDA;
								}
								if (COND_P() != 0) return MrbTokens.keyword_do_cond;
								if (CMDARG_P() != 0 && state != mrb_lex_state_enum.EXPR_CMDARG)
									return MrbTokens.keyword_do_block;
								if (state == mrb_lex_state_enum.EXPR_ENDARG || state == mrb_lex_state_enum.EXPR_BEG)
									return MrbTokens.keyword_do_block;
								return MrbTokens.keyword_do;
							}
							if (state == mrb_lex_state_enum.EXPR_BEG || state == mrb_lex_state_enum.EXPR_VALUE)
								return kw.id0;
							else {
								if (kw.id0 != kw.id1)
									this.lstate = mrb_lex_state_enum.EXPR_BEG;
								return kw.id1;
							}
						}
					}

					if (IS_BEG() || this.lstate == mrb_lex_state_enum.EXPR_DOT || IS_ARG()) {
						if (cmd_state) {
							this.lstate = mrb_lex_state_enum.EXPR_CMDARG;
						}
						else {
							this.lstate = mrb_lex_state_enum.EXPR_ARG;
						}
					}
					else if (this.lstate == mrb_lex_state_enum.EXPR_FNAME) {
						this.lstate = mrb_lex_state_enum.EXPR_ENDFN;
					}
					else {
						this.lstate = mrb_lex_state_enum.EXPR_END;
					}
					break;
				}
				{
					mrb_sym ident = intern_cstr(tok());

					yylval.id = ident;
#if false
					if (last_state != mrb_lex_state_enum.EXPR_DOT && islower(tok()[0]) && lvar_defined(ident)) {
						this.lstate = mrb_lex_state_enum.EXPR_END;
					}
#endif
				}
				return result;
			}
		}

		private void mrb_parser_parse()
		{
			yylval = new MrbToken(filename);

			try {
				this.cmd_start = true;
				this.in_def = this.in_single = 0;
				this.lex_strterm = null;
				this.tokbuf = this.buf;
				this.tsiz = MRB_PARSER_TOKBUF_SIZE;

				yyParse(this, null);
			}
			catch (Exception) {
				yyError("memory allocation error");
				this.tree = null;
			}
		}

		public void mrb_parser_set_filename(string f)
		{
			int i;

			this.lineno = (this.filename_table_length > 0) ? 0 : 1;

			for (i = 0; i < this.filename_table_length; ++i) {
				if (this.filename_table[i] == f) {
					this.current_filename_index = i;
					return;
				}
			}

			this.current_filename_index = this.filename_table_length + 1;
			this.filename_table.Push(f);
		}

		public void mrb_parse_nstring(string filename, Uint8Array s)
		{
			mrb_parser_set_filename(filename);
			this.s = s;
			this.sp = 0;

			mrb_parser_parse();
		}

		public static node parse(string text, string filename = "temporary.rb")
		{
			var generator = new MrbParser();
			generator.mrb_parse_nstring(filename, UTF8StringToArray(text));
			var scope = generator.tree as scope_node;
			if (scope == null)
				return null;
			return scope.body;
		}

		public static node evaluate(node tree)
		{
			var p = tree.p;

			var node = tree as IEvaluatable;
			if (node != null)
				return tree;

			var begin = tree as begin_node;
			if (begin != null) {
				var progs = new JsArray<node>();
				foreach (var r in begin.progs) {
					progs.Push(evaluate(r));
				}
				if (progs.Length != 1)
					return new begin_node(p, progs);
				return progs[0];
			}

			var negate = tree as negate_node;
			if (negate != null) {
				var n = evaluate(negate.n);
				if (n is int_node) {
					var a = ((int_node)n).to_i();
					var c = UTF8StringToArray((-a).ToString());
					return new int_node(p, c, 10);
				}
				if (n is float_node) {
					var a = ((float_node)n).to_f();
					var c = UTF8StringToArray((-a).ToString());
					return new float_node(p, c);
				}
				return n;
			}

			var dot2 = tree as dot2_node;
			if (dot2 != null) {
				var a = evaluate(dot2.a);
				var b = evaluate(dot2.b);
				return new dot2_node(p, a, b);
			}

			var dot3 = tree as dot3_node;
			if (dot3 != null) {
				var a = evaluate(dot3.a);
				var b = evaluate(dot3.b);
				return new dot3_node(p, a, b);
			}

			var call = tree as call_node;
			if (call != null) {
				var obj = evaluate(call.obj);
				var args = new JsArray<node>();
				foreach (var a in call.args) {
					args.Push(evaluate(a));
				}

				var eva = obj as IEvaluatable;
				if (eva != null) {
					node ret;
					if ((ret = eva.evaluate(p.sym2name(call.method), args)) != null)
						return ret;
				}
				return new call_node(p, obj, call.method, args, call.block);
			}

			return tree;
		}

		public string to_ruby()
		{
			if (tree != null) {
				var cond = new ruby_code_cond(filename);
				tree.to_ruby(cond);
				return cond.ToString();
			}
			if (s != null)
				return UTF8ArrayToString(s, 0);
			else
				return "";
		}
		int MrbParser.yyInput.Token { get { return (int)yylval.Kind; } }

		object MrbParser.yyInput.Value { get { return yylval.Value; } }

		bool MrbParser.yyInput.Advance()
		{
			var token = parser_yylex();
			yylval.SetToken(token, MrbParser.UTF8ArrayToString(tok().SubArray(0, toklen() + 1), 0));

			return token > 0;
		}

		void yyConsoleOut.yyWarning(string message, object[] expected)
		{
			Console.WriteLine($"{filename}({lineno},{column}): warning {String.Format(message, expected)}");
		}

		void yyConsoleOut.yyError(string message, object[] expected)
		{
			Console.WriteLine($"{filename}({lineno},{column}): error {String.Format(message, expected)}");
		}
	}
}
