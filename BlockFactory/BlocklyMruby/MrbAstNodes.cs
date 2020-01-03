using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bridge;
using Bridge.Html5;

namespace BlocklyMruby
{
	public interface IMrbParser
	{
		int lineno { get; set; }
		int column { get; set; }
		string filename { get; }
		string sym2name(mrb_sym sym);
		JsArray<mrb_sym> locals_node();
		void yyError(string message, params object[] expected);
	}

	public enum node_type
	{
		NODE_METHOD = 1,
		NODE_FBODY,
		NODE_CFUNC,
		NODE_SCOPE,
		NODE_BLOCK,
		NODE_IF,
		NODE_CASE,
		NODE_WHEN,
		NODE_OPT_N,
		NODE_WHILE,
		NODE_UNTIL,
		NODE_ITER,
		NODE_FOR,
		NODE_BREAK,
		NODE_NEXT,
		NODE_REDO,
		NODE_RETRY,
		NODE_BEGIN,
		NODE_RESCUE,
		NODE_ENSURE,
		NODE_AND,
		NODE_OR,
		NODE_NOT,
		NODE_MASGN,
		NODE_ASGN,
		NODE_CDECL,
		NODE_CVASGN,
		NODE_CVDECL,
		NODE_OP_ASGN,
		NODE_CALL,
		NODE_SCALL,
		NODE_FCALL,
		NODE_VCALL,
		NODE_SUPER,
		NODE_ZSUPER,
		NODE_ARRAY,
		NODE_ZARRAY,
		NODE_HASH,
		NODE_RETURN,
		NODE_YIELD,
		NODE_LVAR,
		NODE_DVAR,
		NODE_GVAR,
		NODE_IVAR,
		NODE_CONST,
		NODE_CVAR,
		NODE_NTH_REF,
		NODE_BACK_REF,
		NODE_MATCH,
		NODE_MATCH2,
		NODE_MATCH3,
		NODE_INT,
		NODE_FLOAT,
		NODE_NEGATE,
		NODE_LAMBDA,
		NODE_SYM,
		NODE_STR,
		NODE_DSTR,
		NODE_XSTR,
		NODE_DXSTR,
		NODE_REGX,
		NODE_DREGX,
		NODE_DREGX_ONCE,
		NODE_LIST,
		NODE_ARG,
		NODE_ARGSCAT,
		NODE_ARGSPUSH,
		NODE_SPLAT,
		NODE_TO_ARY,
		NODE_SVALUE,
		NODE_BLOCK_ARG,
		NODE_DEF,
		NODE_SDEF,
		NODE_ALIAS,
		NODE_UNDEF,
		NODE_CLASS,
		NODE_MODULE,
		NODE_SCLASS,
		NODE_COLON2,
		NODE_COLON3,
		NODE_CREF,
		NODE_DOT2,
		NODE_DOT3,
		NODE_FLIP2,
		NODE_FLIP3,
		NODE_ATTRSET,
		NODE_SELF,
		NODE_NIL,
		NODE_TRUE,
		NODE_FALSE,
		NODE_DEFINED,
		NODE_NEWLINE,
		NODE_POSTEXE,
		NODE_ALLOCA,
		NODE_DMETHOD,
		NODE_BMETHOD,
		NODE_MEMO,
		NODE_IFUNC,
		NODE_DSYM,
		NODE_ATTRASGN,
		NODE_HEREDOC,
		NODE_LITERAL_DELIM,
		NODE_WORDS,
		NODE_SYMBOLS,
		NODE_LAST
	}

	/* lexer states */
	public enum mrb_lex_state_enum
	{
		EXPR_BEG,                   /* ignore newline, +/- is a sign. */
		EXPR_END,                   /* newline significant, +/- is an operator. */
		EXPR_ENDARG,                /* ditto, and unbound braces. */
		EXPR_ENDFN,                 /* ditto, and unbound braces. */
		EXPR_ARG,                   /* newline significant, +/- is an operator. */
		EXPR_CMDARG,                /* newline significant, +/- is an operator. */
		EXPR_MID,                   /* newline significant, +/- is an operator. */
		EXPR_FNAME,                 /* ignore newline, no reserved words. */
		EXPR_DOT,                   /* right after '.' or '::', no reserved words. */
		EXPR_CLASS,                 /* immediate after 'class', no here document. */
		EXPR_VALUE,                 /* alike EXPR_BEG but label is disallowed. */
		EXPR_MAX_STATE
	}

	public enum mrb_sym { }

	public enum stack_type { }

	[Flags]
	enum mrb_string_type
	{
		STR_FUNC_PARSING = 0x01,
		STR_FUNC_EXPAND = 0x02,
		STR_FUNC_REGEXP = 0x04,
		STR_FUNC_WORD = 0x08,
		STR_FUNC_SYMBOL = 0x10,
		STR_FUNC_ARRAY = 0x20,
		STR_FUNC_HEREDOC = 0x40,
		STR_FUNC_XQUOTE = 0x80,

		str_not_parsing = (0),
		str_squote = (STR_FUNC_PARSING),
		str_dquote = (STR_FUNC_PARSING | STR_FUNC_EXPAND),
		str_regexp = (STR_FUNC_PARSING | STR_FUNC_REGEXP | STR_FUNC_EXPAND),
		str_sword = (STR_FUNC_PARSING | STR_FUNC_WORD | STR_FUNC_ARRAY),
		str_dword = (STR_FUNC_PARSING | STR_FUNC_WORD | STR_FUNC_ARRAY | STR_FUNC_EXPAND),
		str_ssym = (STR_FUNC_PARSING | STR_FUNC_SYMBOL),
		str_ssymbols = (STR_FUNC_PARSING | STR_FUNC_SYMBOL | STR_FUNC_ARRAY),
		str_dsymbols = (STR_FUNC_PARSING | STR_FUNC_SYMBOL | STR_FUNC_ARRAY | STR_FUNC_EXPAND),
		str_heredoc = (STR_FUNC_PARSING | STR_FUNC_HEREDOC),
		str_xquote = (STR_FUNC_PARSING | STR_FUNC_XQUOTE | STR_FUNC_EXPAND),
	}

	class parser_heredoc_info
	{
		public bool allow_indent;
		public bool line_head;
		public mrb_string_type type;
		public Uint8Array term;
		public int term_len;
		public JsArray<node> doc = new JsArray<node>();

		public string GetString()
		{
			var sb = new StringBuilder();

			foreach (var str in doc) {
				if (str is str_node) {
					sb.Append(MrbParser.UTF8ArrayToString(((str_node)str).str, 0));
				}
				else if (str is begin_node) {
					foreach (var p in ((begin_node)str).progs) {
						if (p is str_node) {
							sb.Append(MrbParser.UTF8ArrayToString(((str_node)p).str, 0));
						}
						else if (p is heredoc_node) {
							sb.Append(((heredoc_node)p).info.GetString());
						}
						else if (p is call_node) {

						}
						else {
							throw new NotImplementedException();
						}
					}
				}
				else {
					throw new NotImplementedException();
				}
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return $"{term} {GetString()}";
		}

		internal void push_doc(node str)
		{
			doc.Push(str);
		}

		internal void claer_doc()
		{
			doc.Splice(0, doc.Length);
		}

		internal void to_ruby(ruby_code_cond cond)
		{
			cond.write_line("<<" + MrbParser.UTF8ArrayToString(term, 0));
			foreach (var d in doc) {
				if (d is begin_node) {
					cond.write("#{");
					d.to_ruby(cond);
					cond.write("}");
				}
				else {
					d.to_ruby(cond);
				}
			}
			cond.write_line();
			cond.write_line(MrbParser.UTF8ArrayToString(term, 0));
		}
	}

	public class xml_code_cond
	{
		public xml_code_cond()
		{
		}

		internal Element CreateElement(string tagname)
		{
			return Document.CreateElement<Element>(tagname);
		}

		internal Text CreateTextNode(string text)
		{
			return Document.CreateTextNode(text);
		}
	}

	public class ruby_code_cond
	{
		public string newline_str { get; private set; }
		public string indent_str { get; set; }
		public string indent { get; private set; }
		public int nest { get; private set; }

		bool first;
		bool space;
		StringBuilder _code = new StringBuilder();
		public string filename { get; private set; }
		public int lineno { get; private set; }
		public int column { get; private set; }

		public ruby_code_cond(string filename, string indent_str = "  ")
		{
			this.filename = filename;
			lineno = 1;
			column = 0;
			this.indent_str = indent_str;
			indent = "";
			newline_str = "\r\n";
			first = true;
			space = false;
		}

		public void increment_indent()
		{
			indent += indent_str;
			if (!first) {
				new_line();
				first = true;
				space = false;
			}
		}

		private void new_line()
		{
			_code.Append(newline_str);
			lineno++;
			column = 0;
		}

		private void write_code(string code)
		{
			_code.Append(code);
			column += code.Length;
		}

		public void decrement_indent()
		{
			indent = indent.Substring(0, indent.Length - indent_str.Length);
			if (!first) {
				new_line();
				first = true;
				space = false;
			}
		}

		public void increment_nest()
		{
			nest++;
		}

		public void decrement_nest()
		{
			nest--;
			System.Diagnostics.Debug.Assert(nest >= 0);
		}

		public void write(string code)
		{
			if (first) {
				first = false;
				write_code(indent);
			}
			write_code(code);
			space = code.EndsWith(" ");
		}

		public void write_line(string code = null)
		{
			if (nest != 0 && !first && !space) {
				write_code(" ");
				space = true;
			}
			if (code != null) {
				if (first) {
					first = false;
					write_code(indent);
				}
				write_code(code);
				space = code.EndsWith(" ");
			}
			if (nest == 0) {
				new_line();
				space = false;
				first = true;
			}
		}

		internal void separate_line()
		{
			if (!first) {
				new_line();
				first = true;
			}
		}

		public JsArray<node> nodes = new JsArray<node>();

		public void add_node(node node)
		{
			nodes.Push(node);
		}

		public override string ToString()
		{
			return _code.ToString();
		}
	}

	public class node
	{
		object _car;
		object _cdr;

		public IMrbParser p { get; private set; }
		public object car {
			get { return _car; }
			set {
				System.Diagnostics.Debug.Assert(GetType() == typeof(node));
				_car = value;
			}
		}
		public object cdr {
			get { return _cdr; }
			set {
				System.Diagnostics.Debug.Assert(GetType() == typeof(node));
				_cdr = value;
			}
		}
		public int lineno { get; private set; }
		public int column { get; private set; }
		public string filename { get; private set; }
		public string workspace_id { get; private set; }
		public string block_id { get; private set; }

		protected node(IMrbParser p, node_type car)
		{
			this.p = p;
			_car = car;
			lineno = p.lineno;
			column = p.column;
			filename = p.filename;
		}

		public override string ToString()
		{
			if (cdr == null)
				return $"({car})";
			return $"({car}, {cdr})";
		}

		public void SET_LINENO(int n) { lineno = n; }

		public void NODE_LINENO(node n)
		{
			if (n != null) {
				filename = n.filename;
				lineno = n.lineno;
				column = n.column;
			}
		}

		public void set_blockid(string workspace_id, string block_id)
		{
			this.workspace_id = workspace_id;
			this.block_id = block_id;
		}

		public static node cons(IMrbParser p, object car, object cdr)
		{
			var result = new node(p, 0);
			result.car = car;
			result.cdr = cdr;
			return result;
		}

		public virtual void append(node b)
		{
			node c = this;
			while (c.cdr != null) {
				c = (node)c.cdr;
			}
			if (b != null) {
				c.cdr = b;
			}
		}

		public static void dump_recur<T>(JsArray<T> list, node tree)
		{
			while (tree != null) {
				list.Push((T)tree.car);
				tree = (node)tree.cdr;
			}
		}

		public virtual Element to_xml(xml_code_cond cond)
		{
			var a = car as node;
			if (a != null && cdr == null) {
				return a.to_xml(cond);
			}

			throw new NotImplementedException();
		}

		public int start_lineno { get; private set; }
		public int start_column { get; private set; }

		public void to_ruby(ruby_code_cond cond)
		{
			filename = cond.filename;
			start_lineno = cond.lineno;
			start_column = cond.column;

			to_rb(cond);

			lineno = cond.lineno;
			column = cond.column;

			cond.add_node(this);
		}

		protected virtual void to_rb(ruby_code_cond cond)
		{
			var a = car as node;
			while (a != null) {
				a.to_ruby(cond);
				a = a.cdr as node;
			}
		}
	}

	public class locals_node
	{
		public JsArray<mrb_sym> symList = new JsArray<mrb_sym>();
		public locals_node cdr;

		public locals_node(locals_node cdr)
		{
			this.cdr = cdr;
		}

		internal void push(mrb_sym sym)
		{
			symList.Push(sym);
		}
	}

	/* (:scope (vars..) (prog...)) */
	class scope_node : node
	{
		private JsArray<mrb_sym> _local_variables = new JsArray<mrb_sym>();
		private node _body;

		public scope_node(IMrbParser p, node body)
			: base(p, node_type.NODE_SCOPE)
		{
			_local_variables = (JsArray<mrb_sym>)_local_variables.Concat(p.locals_node());
			_body = body;
		}

		public JsArray<mrb_sym> local_variables { get { return _local_variables; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			// TODO:？？？
			var s = cond.CreateElement("scope");
			var b = _body.to_xml(cond);
			if (b != null) {
				s.AppendChild(b);
			}
			return s;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_body.to_ruby(cond);
		}

		public override string ToString()
		{
			var str = "(:scope (";
			foreach (var v in local_variables) {
				str += $"{v}, ";
			}
			return str + $") {body})";
		}
	}

	/* (:begin prog...) */
	public class begin_node : node
	{
		private JsArray<node> _progs = new JsArray<node>();
		bool _parentheses;

		public begin_node(IMrbParser p, node body, bool parentheses = false)
			: base(p, node_type.NODE_BEGIN)
		{
			while (body != null) {
				_progs.Push(body);
				body = (node)body.cdr;
			}
			_parentheses = parentheses;
		}

		public begin_node(IMrbParser p, JsArray<node> progs)
			: base(p, node_type.NODE_BEGIN)
		{
			_progs = (JsArray<node>)_progs.Concat(progs);
		}

		public JsArray<node> progs { get { return _progs; } }

		public override void append(node b)
		{
			_progs.Push((node)b.car);
		}

		public override Element to_xml(xml_code_cond cond)
		{
			switch (_progs.Length) {
			case 0:
				return null;
			case 1:
				return _progs[0].to_xml(cond);
			}
			var b = _progs[0].to_xml(cond);
			var p = b;
			for (int i = 1; i < _progs.Length; i++) {
				var n = cond.CreateElement("next");
				var q = _progs[i].to_xml(cond);
				n.AppendChild(q);
				p.AppendChild(n);
				p = q;
			}

			return b;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			var i = progs.Length;
			foreach (var v in progs) {
				var prn = _parentheses || (v is dot2_node);
				if (prn) {
					cond.increment_nest();
					cond.write("(");
				}
				v.to_ruby(cond);
				if (prn) {
					cond.write(")");
					cond.decrement_nest();
				}
				i--; if (i > 0) cond.separate_line();
			}
		}

		public override string ToString()
		{
			var str = "(:begin ";
			foreach (var v in progs) {
				str += $"{v}, ";
			}
			return str + ")";
		}
	}

	/* (:rescue body rescue else) */
	class rescue_node : node
	{
		public class rescue_t
		{
			public JsArray<node> handle_classes = new JsArray<node>();
			public node exc_var;
			public node body;

			public override string ToString()
			{
				var str = "(";
				foreach (var c in handle_classes) {
					str += $"{c}, ";
				}
				return $"{exc_var} {body})";
			}

			internal void to_ruby(ruby_code_cond cond)
			{
				cond.increment_nest();
				cond.write("rescue");
				int i = handle_classes.Length;
				if (i > 0)
					cond.write(" ");
				foreach (var c in handle_classes) {
					c.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (exc_var != null) {
					cond.write(" => ");
					exc_var.to_ruby(cond);
				}
				cond.decrement_nest();
				cond.write_line();
				cond.increment_indent();
				body.to_ruby(cond);
				cond.decrement_indent();
			}
		}

		private node _body;
		private JsArray<rescue_t> _rescue = new JsArray<rescue_t>();
		private node _else;
		public bool ensure;

		public rescue_node(IMrbParser p, node body, node resq, node els)
			: base(p, node_type.NODE_RESCUE)
		{
			_body = body;
			if (resq != null) {
				var n2 = (node)resq;

				while (n2 != null) {
					rescue_t r = new rescue_t();
					var n3 = (node)n2.car;
					if (n3.car != null) {
						dump_recur(r.handle_classes, (node)n3.car);
					}
					r.exc_var = (node)((node)n3.cdr).car;
					r.body = (node)((node)((node)n3.cdr).cdr).car;
					_rescue.Push(r);
					n2 = (node)n2.cdr;
				}
			}
			_else = els;
		}

		public node body { get { return _body; } }
		public JsArray<rescue_t> rescue { get { return _rescue; } }
		public node @else { get { return _else; } }

		public override Element to_xml(xml_code_cond cond)
		{
			// TODO:？？？
			return body.to_xml(cond);
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			if (!ensure) {
				cond.write_line("begin");
				cond.increment_indent();
			}
			_body.to_ruby(cond);
			cond.decrement_indent();
			foreach (var r in _rescue) {
				r.to_ruby(cond);
			}
			if (_else != null) {
				cond.write_line("else");
				cond.increment_indent();
				_else.to_ruby(cond);
				cond.decrement_indent();
			}
			if (!ensure) {
				cond.write_line("end");
			}
			else {
				cond.increment_indent();
			}
		}

		public override string ToString()
		{
			var str = $"(:rescue {body} ";
			foreach (var r in rescue) {
				str += $"{r}, ";
			}
			return str + $" {@else})";
		}
	}

	/* (:ensure body ensure) */
	class ensure_node : node
	{
		private node _body;
		private node _ensure;
		public bool def;

		public ensure_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_ENSURE)
		{
			_body = a;
			_ensure = b;
			if (_body is rescue_node) {
				((rescue_node)_body).ensure = true;
			}
		}

		public node body { get { return _body; } }
		public node ensure { get { return _ensure; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			if (!def) {
				cond.write_line("begin");
				cond.increment_indent();
			}
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("ensure");
			cond.increment_indent();
			_ensure.to_ruby(cond);
			if (!def) {
				cond.decrement_indent();
				cond.write_line("end");
			}
		}

		public override string ToString()
		{
			return $"(:ensure {body} {ensure})";
		}
	}

	/* (:nil) */
	class nil_node : node, IEvaluatable
	{
		public nil_node(IMrbParser p)
			: base(p, node_type.NODE_NIL)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_null");
			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("nil");
		}

		public override string ToString()
		{
			return $"(:nil)";
		}

		public node evaluate(string method, JsArray<node> args)
		{
			switch (method) {
			case "!":
				if (args.Length != 0)
					break;
				return new true_node(p);
			}
			return null;
		}
	}

	/* (:true) */
	class true_node : node, IEvaluatable
	{
		public true_node(IMrbParser p)
			: base(p, node_type.NODE_TRUE)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_boolean");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "BOOL");
			field.AppendChild(cond.CreateTextNode("TRUE"));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("true");
		}

		public override string ToString()
		{
			return $"(:true)";
		}

		public node evaluate(string method, JsArray<node> args)
		{
			switch (method) {
			case "!":
				if (args.Length != 0)
					break;
				return new false_node(p);
			}
			return null;
		}
	}

	/* (:false) */
	class false_node : node, IEvaluatable
	{
		public false_node(IMrbParser p)
			: base(p, node_type.NODE_FALSE)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_boolean");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "BOOL");
			field.AppendChild(cond.CreateTextNode("FALSE"));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("false");
		}

		public override string ToString()
		{
			return $"(:false)";
		}

		public node evaluate(string method, JsArray<node> args)
		{
			switch (method) {
			case "!":
				if (args.Length != 0)
					break;
				return new true_node(p);
			}
			return null;
		}
	}

	/* (:alias new old) */
	class alias_node : node
	{
		private mrb_sym _new;
		private mrb_sym _old;

		public alias_node(IMrbParser p, mrb_sym a, mrb_sym b)
			: base(p, node_type.NODE_ALIAS)
		{
			_new = a;
			_old = b;
		}

		public mrb_sym @new { get { return _new; } }
		public mrb_sym old { get { return _old; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write_line($"alias {p.sym2name(_new)} {p.sym2name(_old)}");
		}

		public override string ToString()
		{
			return $"(:alias {p.sym2name(@new)} {p.sym2name(old)})";
		}
	}

	/* (:if cond then else) */
	class if_node : node
	{
		private node _cond;
		private node _then;
		private node _else;
		bool _inline;

		public if_node(IMrbParser p, node cond, node then, node @else, bool inline)
			: base(p, node_type.NODE_IF)
		{
			_cond = cond;
			_then = then;
			_else = @else;
			_inline = inline;
		}

		public node cond { get { return _cond; } }
		public node then { get { return _then; } }
		public node @else { get { return _else; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var _elsif = new JsArray<Tuple<node, node>>();
			node _else = this._else;

			_elsif.Push(new Tuple<node, node>(_cond, _then));
			for (var c = _else as if_node; c != null; _else = c._else, c = _else as if_node) {
				_elsif.Push(new Tuple<node, node>(c._cond, c._then));
			}

			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_if");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("elseif", _elsif.Length.ToString());
			mutation.SetAttribute("else", _else != null ? "1" : "0");
			block.AppendChild(mutation);

			int i = 0;
			foreach (var e in _elsif) {
				var value = cond.CreateElement("value");
				value.SetAttribute("name", $"IF{i}");
				value.AppendChild(e.Item1.to_xml(cond));
				block.AppendChild(value);

				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", $"DO{i}");
				statement.AppendChild(e.Item2.to_xml(cond));
				block.AppendChild(statement);
				i++;
			}

			if (_else != null) {
				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "ELSE");
				statement.AppendChild(_else.to_xml(cond));
				block.AppendChild(statement);
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			if (_inline) {
				cond.increment_nest();
				cond.write("(");
				_cond.to_ruby(cond);
				cond.write(" ? ");
				_then.to_ruby(cond);
				cond.write(" : ");
				_else.to_ruby(cond);
				cond.decrement_nest();
			}
			else {
				var _elsif = new JsArray<Tuple<node, node>>();
				node _else = this._else;

				for (var c = _else as if_node; c != null; _else = c._else, c = _else as if_node) {
					_elsif.Push(new Tuple<node, node>(c._cond, c._then));
				}

				cond.increment_nest();
				cond.write("if ");
				_cond.to_ruby(cond);
				cond.decrement_nest();
				cond.increment_indent();
				_then.to_ruby(cond);
				cond.decrement_indent();
				foreach (var e in _elsif) {
					cond.increment_nest();
					cond.write("elsif ");
					e.Item1.to_ruby(cond);
					cond.decrement_nest();
					cond.write_line();
					cond.increment_indent();
					e.Item2.to_ruby(cond);
					cond.decrement_indent();
				}

				if (_else != null) {
					cond.write_line("else");
					cond.increment_indent();
					_else.to_ruby(cond);
					cond.decrement_indent();
				}
				cond.write_line("end");
			}
		}

		public override string ToString()
		{
			return $"(:if {cond} {then} {@else})";
		}
	}

	/* (:unless cond then else) */
	class unless_node : node
	{
		private node _cond;
		private node _then;
		private node _else;

		public unless_node(IMrbParser p, node a, node b, node c)
			: base(p, node_type.NODE_IF)
		{
			_cond = a;
			_then = c;
			_else = b;
		}

		public node cond { get { return _cond; } }
		public node then { get { return _then; } }
		public node @else { get { return _else; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_if");

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "IF0");
			value.AppendChild(_cond.to_xml(cond));
			block.AppendChild(value);

			if (_then != null) {
				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "DO0");
				block.AppendChild(statement);

				statement.AppendChild(_then.to_xml(cond));
			}

			if (_else != null) {
				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "ELSE");
				block.AppendChild(statement);

				statement.AppendChild(_else.to_xml(cond));
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("unless ");
			_cond.to_ruby(cond);
			cond.decrement_nest();
			cond.increment_indent();
			_else.to_ruby(cond);
			cond.decrement_indent();

			if (_then != null) {
				cond.write_line("else");
				cond.increment_indent();
				_then.to_ruby(cond);
				cond.decrement_indent();
			}
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:unless {cond} {then} {@else})";
		}
	}

	/* (:while cond body) */
	class while_node : node
	{
		private node _cond;
		private node _body;

		public while_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_WHILE)
		{
			_cond = a;
			_body = b;
		}

		public node cond { get { return _cond; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_whileUntil");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "MODE");
			field.AppendChild(cond.CreateTextNode("WHILE"));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "BOOL");
			value.AppendChild(_cond.to_xml(cond));
			block.AppendChild(value);

			var statement = cond.CreateElement("statement");
			statement.SetAttribute("name", "DO");
			statement.AppendChild(_body.to_xml(cond));
			block.AppendChild(statement);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("while ");
			_cond.to_ruby(cond);
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:while {cond} {body})";
		}
	}

	/* (:until cond body) */
	class until_node : node
	{
		private node _cond;
		private node _body;

		public until_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_UNTIL)
		{
			_cond = a;
			_body = b;
		}

		public node cond { get { return _cond; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_whileUntil");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "MODE");
			field.AppendChild(cond.CreateTextNode("UNTIL"));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "BOOL");
			value.AppendChild(_cond.to_xml(cond));
			block.AppendChild(value);

			var statement = cond.CreateElement("statement");
			statement.SetAttribute("name", "DO");
			statement.AppendChild(_body.to_xml(cond));
			block.AppendChild(statement);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("until ");
			_cond.to_ruby(cond);
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:until {cond} {body})";
		}
	}

	/* (:for var obj body) */
	class for_node : node
	{
		public class var_t
		{
			public JsArray<node> pre = new JsArray<node>();
			public node rest;
			public JsArray<node> post = new JsArray<node>();

			public override string ToString()
			{
				var str = "(";
				foreach (var p in pre) {
					str += $"{p} ";
				}
				str += $"{rest} ";
				foreach (var p in post) {
					str += $"{p} ";
				}
				return str + ")";
			}

			internal void to_ruby(ruby_code_cond cond)
			{
				int i = pre.Length + (rest != null ? 1 : 0) + post.Length;
				foreach (var p in pre) {
					p.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (rest != null) {
					rest.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var p in post) {
					p.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
			}
		}
		private var_t _var;
		private node _in;
		private node _do;

		public for_node(IMrbParser p, node v, node o, node b)
			: base(p, node_type.NODE_FOR)
		{
			_var = new var_t();
			node n2 = v;

			if (n2.car != null) {
				dump_recur(_var.pre, (node)n2.car);
			}
			n2 = (node)n2.cdr;
			if (n2 != null) {
				if (n2.car != null) {
					_var.rest = (node)n2.car;
				}
				n2 = (node)n2.cdr;
				if (n2 != null) {
					if (n2.car != null) {
						dump_recur(_var.post, (node)n2.car);
					}
				}
			}
			_in = o;
			_do = b;
		}

		public var_t var { get { return _var; } }
		public node @in { get { return _in; } }
		public node @do { get { return _do; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_forEach");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			// TODO:var？
			var pre = var.pre[0];
			switch ((node_type)pre.car) {
			case node_type.NODE_GVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((gvar_node)pre).name)));
				break;
			case node_type.NODE_CVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((cvar_node)pre).name)));
				break;
			case node_type.NODE_IVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((ivar_node)pre).name)));
				break;
			case node_type.NODE_LVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((lvar_node)pre).name)));
				break;
			default:
				// TODO: ？
				throw new NotImplementedException();
			}
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "LIST");
			value.AppendChild(_in.to_xml(cond));
			block.AppendChild(value);

			var statement = cond.CreateElement("statement");
			statement.SetAttribute("name", "DO");
			statement.AppendChild(_do.to_xml(cond));
			block.AppendChild(statement);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("for ");
			_var.to_ruby(cond);
			cond.write(" in ");
			_in.to_ruby(cond);
			cond.write(" do");
			cond.decrement_nest();
			cond.increment_indent();
			_do.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:for {var} {@in} {@do})";
		}
	}

	/* (:case a ((when ...) body) ((when...) body)) */
	class case_node : node
	{
		public class when_t
		{
			public JsArray<node> value = new JsArray<node>();
			public node body;

			public override string ToString()
			{
				var str = $"(when ";
				foreach (var c in value) {
					str += $"{c} ";
				}
				return str + $"{body})";
			}
		}
		private node _arg;
		private JsArray<when_t> _when = new JsArray<when_t>();

		public case_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_CASE)
		{
			_arg = a;
			while (b != null) {
				var w = new when_t();
				dump_recur(w.value, (node)((node)b.car).car);
				w.body = (node)((node)b.car).cdr;
				_when.Push(w);
				b = (node)b.cdr;
			}
		}

		public case_node(IMrbParser p, node a, JsArray<when_t> b)
			: base(p, node_type.NODE_CASE)
		{
			_arg = a;
			_when = (JsArray<when_t>)_when.Concat(b);
		}

		public node arg { get { return _arg; } }
		public JsArray<when_t> when { get { return _when; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "switch_case_number");

			int c = 0, d = 0;
			when_t default_node = null;
			foreach (var w in _when) {
				if (w.value.Length == 0) {
					default_node = w;
					d = 1;
				}
				else
					c++;
			}

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("case", c.ToString());
			mutation.SetAttribute("default", d.ToString());
			block.AppendChild(mutation);

			int i = 0;
			foreach (var w in _when) {
				if (w.value.Length == 0)
					continue;

				var field = cond.CreateElement("field");
				field.SetAttribute("name", "CONST" + i);
				// TODO:whenの値が複数の場合
				field.AppendChild(cond.CreateTextNode(MrbParser.UTF8ArrayToString(((int_node)w.value[0]).num, 0)));
				block.AppendChild(field);
				i++;
			}

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "SWITCH");
			block.AppendChild(value);

			value.AppendChild(_arg.to_xml(cond));

			foreach (var w in _when) {
				if (w.value.Length == 0)
					continue;

				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "DO" + i);
				statement.AppendChild(w.body.to_xml(cond));
				block.AppendChild(statement);
			}

			if (default_node != null) {
				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "DEFAULT");
				statement.AppendChild(default_node.body.to_xml(cond));
				block.AppendChild(statement);
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("case ");
			if (_arg != null)
				_arg.to_ruby(cond);
			cond.decrement_nest();
			cond.separate_line();
			foreach (var w in _when) {
				cond.increment_nest();
				var count = w.value.Length;
				if (count != 0) {
					cond.write("when ");
					foreach (var v in w.value) {
						v.to_ruby(cond);
						count--;
						if (count > 0)
							cond.write(", ");
					}
				}
				else
					cond.write("else");
				cond.decrement_nest();

				cond.increment_indent();
				w.body.to_ruby(cond);
				cond.decrement_indent();
			}
			cond.write_line("end");
		}

		public override string ToString()
		{
			var str = $"(:case {arg} ";
			foreach (var w in when) {
				str += $"{w} ";
			}
			return str + ")";
		}
	}

	/* (:postexe a) */
	class postexe_node : node
	{
		private node _postexe;

		public postexe_node(IMrbParser p, node a)
			: base(p, node_type.NODE_POSTEXE)
		{
			_postexe = a;
		}

		public node postexe { get { return _postexe; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write_line("END {");
			_postexe.to_ruby(cond);
			cond.write_line("}");
		}

		public override string ToString()
		{
			return $"(:postexe {postexe})";
		}
	}

	/* (:self) */
	class self_node : node
	{
		public self_node(IMrbParser p)
			: base(p, node_type.NODE_SELF)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("self");
		}

		public override string ToString()
		{
			return $"(:self)";
		}
	}

	/* (:call a b c) */
	class call_node : node
	{
		private node _obj;
		private mrb_sym _method;
		private JsArray<node> _args = new JsArray<node>();
		private node _block;
		private MrbTokens _pass;

		public call_node(IMrbParser p, node a, mrb_sym b, node c, MrbTokens pass)
			: base(p, pass != 0 ? node_type.NODE_CALL : node_type.NODE_SCALL)
		{
			_pass = pass;
			NODE_LINENO(a);

			_obj = a;
			_method = b;
			if (c != null) {
				dump_recur(_args, (node)c.car);
				if (c.cdr != null) {
					_block = (node)c.cdr;
				}
			}
		}

		public call_node(IMrbParser p, node a, mrb_sym b, JsArray<node> args = null, node block = null)
			: base(p, node_type.NODE_CALL)
		{
			_obj = a;
			_method = b;
			if (args != null)
				_args = (JsArray<node>)_args.Concat(args);
			_block = block;
		}

		public call_node(IMrbParser p, node a, mrb_sym b, node arg)
			: base(p, node_type.NODE_CALL)
		{
			_obj = a;
			_method = b;
			if (arg != null)
				_args.Push(arg);
			_pass = (MrbTokens)1;
		}

		public node obj { get { return _obj; } }
		public mrb_sym method { get { return _method; } }
		public JsArray<node> args { get { return _args; } }
		public node block { get { return _block; } }

		internal void add_block(node b)
		{
			if (b != null) {
				if (_block != null) {
					p.yyError("both block arg and actual block given");
				}
				_block = b;
			}
		}

		public override Element to_xml(xml_code_cond cond)
		{
			var method = p.sym2name(_method);
			switch (method) {
			case "==": return logic_compare(cond, "EQ");
			case "!=": return logic_compare(cond, "NEQ");
			case "<": return logic_compare(cond, "LT");
			case "<=": return logic_compare(cond, "LTE");
			case ">": return logic_compare(cond, "GT");
			case ">=": return logic_compare(cond, "GTE");
			}

			return procedures_callreturn(cond, method);
		}

		private Element procedures_callreturn(xml_code_cond cond, string method)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "procedures_callreturn");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("name", method);
			block.AppendChild(mutation);

			int i = 0;
			foreach (var a in args) {
				var arg = cond.CreateElement("arg");
				// TODO: 引数名を持ってくkる
				arg.SetAttribute("name", i.ToString());
				arg.AppendChild(a.to_xml(cond));
				block.AppendChild(arg);
				i++;
			}

			return block;
		}

		private Element logic_compare(xml_code_cond cond, string op)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_compare");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "OP");
			field.AppendChild(cond.CreateTextNode(op));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "A");
			value.AppendChild(_obj.to_xml(cond));
			block.AppendChild(value);

			// argsは１つ
			value = cond.CreateElement("value");
			value.SetAttribute("name", "B");
			value.AppendChild(args[0].to_xml(cond));
			block.AppendChild(value);

			return block;
		}

		public bool isArray(node _obj)
		{
			var cnst = _obj as const_node;
			return (cnst != null) && (p.sym2name(cnst.name) == "Array");
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			var blk_arg = _block as block_arg_node;
			var blk = _block;
			if (blk_arg != null)
				blk = null;
			var m = p.sym2name(_method);
			int i = _args.Length + (blk_arg != null ? 1 : 0);
			if (_pass == (MrbTokens)1) {
				cond.increment_nest();
				switch (m) {
				case "!":
				case "~":
					cond.write(m);
					_obj.to_ruby(cond);
					foreach (var a in _args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
					if (blk_arg != null) {
						blk_arg.to_ruby(cond);
					}
					break;
				case "+@":
				case "-@":
					cond.write(m.Substring(0, 1));
					_obj.to_ruby(cond);
					foreach (var a in _args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
					if (blk_arg != null) {
						blk_arg.to_ruby(cond);
					}
					break;
				default:
					_obj.to_ruby(cond);
					cond.write(" " + m + " ");
					foreach (var a in _args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
					if (blk_arg != null) {
						blk_arg.to_ruby(cond);
					}
					break;
				}
				cond.decrement_nest();
			}
			else {
				string call_op = _pass == MrbTokens.tCOLON2 ? "::" : ".";
				switch (m) {
				case "[]":
					cond.increment_nest();
					_obj.to_ruby(cond);
					if (isArray(_obj)) {
						cond.write(call_op + "[]");
						cond.write("(");
						foreach (var a in _args) {
							a.to_ruby(cond);
							i--; if (i > 0) cond.write(", ");
						}
						cond.write(")");
					}
					else if (i == 0)
						cond.write("[]");
					else {
						cond.write("[");
						foreach (var a in _args) {
							a.to_ruby(cond);
							i--; if (i > 0) cond.write(", ");
						}
						cond.write("]");
					}
					cond.decrement_nest();
					break;
				case "[]=":
					cond.increment_nest();
					_obj.to_ruby(cond);
					cond.write(".[]=");
					cond.write("(");
					foreach (var a in _args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
					if (blk_arg != null) {
						blk_arg.to_ruby(cond);
					}
					cond.write(")");
					cond.decrement_nest();
					break;
				default:
					cond.increment_nest();
					_obj.to_ruby(cond);
					if (i == 0)
						cond.write(call_op + m);
					else {
						cond.write(call_op + m + "(");
						foreach (var a in _args) {
							a.to_ruby(cond);
							i--; if (i > 0) cond.write(", ");
						}
						if (blk_arg != null) {
							blk_arg.to_ruby(cond);
						}
						cond.write(")");
					}
					cond.decrement_nest();
					break;
				}
			}
			if (blk != null) {
				blk.to_ruby(cond);
			}
			else if (cond.nest == 0)
				cond.write_line();
		}

		public override string ToString()
		{
			var str = $"(:call {obj} {p.sym2name(method)} ";
			foreach (var v in args) {
				str += v.ToString() + " ";
			}
			return str + $"{block})";
		}
	}

	/* (:fcall self mid args) */
	class fcall_node : node
	{
		private node _self;
		private mrb_sym _method;
		private JsArray<node> _args = new JsArray<node>();
		private node _block;

		public fcall_node(IMrbParser p, mrb_sym b, node c)
			: base(p, node_type.NODE_FCALL)
		{
			node n = new self_node(p);
			n.NODE_LINENO(c);
			NODE_LINENO(c);

			_self = n;
			_method = b;
			if (c != null) {
				dump_recur(_args, (node)c.car);
				if (c.cdr != null) {
					_block = (node)c.cdr;
				}
			}
		}

		public fcall_node(IMrbParser p, mrb_sym b, JsArray<node> args = null, node block = null)
			: base(p, node_type.NODE_FCALL)
		{
			_self = new self_node(p);
			_method = b;
			if (args != null)
				_args = (JsArray<node>)_args.Concat(args);
			_block = block;
		}

		public node self { get { return _self; } }
		public mrb_sym method { get { return _method; } }
		public JsArray<node> args { get { return _args; } }
		public node block { get { return _block; } }

		internal void add_block(node b)
		{
			if (b != null) {
				if (_block != null) {
					p.yyError("both block arg and actual block given");
				}
				_block = b;
			}
		}

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "procedures_callreturn");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("name", p.sym2name(_method));
			block.AppendChild(mutation);

			int i = 0;
			foreach (var a in args) {
				var arg = cond.CreateElement("arg");
				// TODO: 引数名を持ってくkる
				arg.SetAttribute("name", i.ToString());
				arg.AppendChild(a.to_xml(cond));
				block.AppendChild(arg);
				i++;
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			var blk_arg = _block as block_arg_node;
			var blk = _block;
			if (blk_arg != null)
				blk = null;
			var m = p.sym2name(_method);
			int i = _args.Length + (blk_arg != null ? 1 : 0);
			switch (m) {
			case "include":
			case "raise":
				cond.write(m + " ");
				foreach (var a in _args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (blk_arg != null) {
					blk_arg.to_ruby(cond);
				}
				break;
			default:
				if (i == 0)
					cond.write(m);
				else {
					cond.write(m + "(");
					foreach (var a in _args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
					if (blk_arg != null) {
						blk_arg.to_ruby(cond);
					}
					cond.write(")");
				}
				break;
			}
			cond.decrement_nest();
			if (blk != null) {
				blk.to_ruby(cond);
			}
			else if (cond.nest == 0)
				cond.write_line();
		}

		public override string ToString()
		{
			var str = $"(:fcall {self} {p.sym2name(method)} ";
			foreach (var v in args) {
				str += v.ToString() + " ";
			}
			return str + $"{block})";
		}
	}

	/* (:super . c) */
	class super_node : node
	{
		private JsArray<node> _args = new JsArray<node>();
		private node _block;

		public super_node(IMrbParser p, node c)
			: base(p, node_type.NODE_SUPER)
		{
			if (c != null) {
				dump_recur(_args, (node)c.car);
				if (c.cdr != null) {
					_block = (node)c.cdr;
				}
			}
		}

		public super_node(IMrbParser p, JsArray<node> args)
			: base(p, node_type.NODE_SUPER)
		{
			_args = (JsArray<node>)_args.Concat(args);
		}

		public JsArray<node> args { get { return _args; } }
		public node block { get { return _block; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "procedures_callreturn");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("name", "super");
			block.AppendChild(mutation);

			int i = 0;
			foreach (var a in args) {
				var arg = cond.CreateElement("arg");
				// TODO: 引数名を持ってくkる
				arg.SetAttribute("name", i.ToString());
				arg.AppendChild(a.to_xml(cond));
				block.AppendChild(arg);
				i++;
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			var blk_arg = _block as block_arg_node;
			var blk = _block;
			if (blk_arg != null)
				blk = null;
			int i = _args.Length + (blk_arg != null ? 1 : 0);
			if (i == 0)
				cond.write("super");
			else {
				cond.write("super(");
				foreach (var a in _args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (blk_arg != null) {
					blk_arg.to_ruby(cond);
				}
				cond.write(")");
			}
			cond.decrement_nest();
			if (blk != null) {
				blk.to_ruby(cond);
			}
			else if (cond.nest == 0)
				cond.write_line();
		}

		public override string ToString()
		{
			var str = "(:super ";
			foreach (var v in args) {
				str += v.ToString() + " ";
			}
			return str + block + ")";
		}

		internal void add_block(node b)
		{
			if (b != null) {
				if (_block != null) {
					p.yyError("both block arg and actual block given");
				}
				_block = b;
			}
		}
	}

	/* (:zsuper) */
	class zsuper_node : node
	{
		private node _block; // 必要?

		public zsuper_node(IMrbParser p)
			: base(p, node_type.NODE_ZSUPER)
		{
		}

		internal void add_block(node b)
		{
			if (b != null) {
				if (_block != null) {
					p.yyError("both block arg and actual block given");
				}
				_block = b;
			}
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			var blk_arg = _block as block_arg_node;
			var blk = _block;
			if (blk_arg != null)
				blk = null;
			if (blk_arg == null)
				cond.write("super");
			else {
				cond.write("super(");
				if (blk_arg != null) {
					blk_arg.to_ruby(cond);
				}
				cond.write(")");
			}
			cond.decrement_nest();
			if (blk != null) {
				blk.to_ruby(cond);
			}
			else if (cond.nest == 0)
				cond.write_line();
		}

		public override string ToString()
		{
			return $"(:zsuper)";
		}
	}

	/* (:yield . c) */
	class yield_node : node
	{
		private JsArray<node> _args = new JsArray<node>();

		public yield_node(IMrbParser p, node c)
			: base(p, node_type.NODE_YIELD)
		{
			if (c != null) {
				if (c.cdr != null) {
					p.yyError("both block arg and actual block given");
				}
				c = (node)c.car;
			}

			dump_recur(_args, (node)c);
		}

		public JsArray<node> args { get { return _args; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			int i = _args.Length;
			if (i == 0)
				cond.write("yield");
			else {
				cond.write("yield ");
				foreach (var a in _args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
			}
			cond.decrement_nest();
			if (cond.nest == 0)
				cond.write_line();
		}

		public override string ToString()
		{
			var str = "(:yield ";
			foreach (var v in args) {
				str += v.ToString() + " ";
			}
			return str + ")";
		}
	}

	/* (:return . c) */
	class return_node : node
	{
		private node _retval;

		public return_node(IMrbParser p, node c)
			: base(p, node_type.NODE_RETURN)
		{
			_retval = c;
		}

		public node retval { get { return _retval; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "procedures_return");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("value", (_retval != null) ? "0" : "1");
			block.AppendChild(mutation);

			if (_retval != null) {
				var value = cond.CreateElement("value");
				value.SetAttribute("name", "VALUE");
				value.AppendChild(_retval.to_xml(cond));
				block.AppendChild(value);
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			if (_retval != null) {
				cond.write("return ");
				_retval.to_ruby(cond);
				cond.write_line();
			}
			else {
				cond.write_line("return");
			}
			cond.decrement_nest();
		}

		public override string ToString()
		{
			return $"(:return . {retval})";
		}
	}

	/* (:break . c) */
	class break_node : node
	{
		private node _retval;

		public break_node(IMrbParser p, node c)
			: base(p, node_type.NODE_BREAK)
		{
			_retval = c;
		}

		public node retval { get { return _retval; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_flow_statements");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "FLOW");
			field.AppendChild(cond.CreateTextNode("BREAK"));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			if (_retval != null) {
				cond.write("break ");
				_retval.to_ruby(cond);
				cond.write_line();
			}
			else {
				cond.write_line("break");
			}
			cond.decrement_nest();
		}

		public override string ToString()
		{
			return $"(:break . {retval})";
		}
	}

	/* (:next . c) */
	class next_node : node
	{
		private node _retval;

		public next_node(IMrbParser p, node c)
			: base(p, node_type.NODE_NEXT)
		{
			_retval = c;
		}

		public node retval { get { return _retval; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "controls_flow_statements");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "FLOW");
			field.AppendChild(cond.CreateTextNode("CONTINUE"));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			if (_retval != null) {
				cond.write("next ");
				_retval.to_ruby(cond);
				cond.write_line();
			}
			else {
				cond.write_line("next");
			}
			cond.decrement_nest();
		}

		public override string ToString()
		{
			return $"(:next . {retval})";
		}
	}

	/* (:redo) */
	class redo_node : node
	{
		public redo_node(IMrbParser p)
			: base(p, node_type.NODE_REDO)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write_line("redo");
		}

		public override string ToString()
		{
			return $"(:redo)";
		}
	}

	/* (:retry) */
	class retry_node : node
	{
		public retry_node(IMrbParser p)
			: base(p, node_type.NODE_RETRY)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write_line("retry");
		}

		public override string ToString()
		{
			return $"(:retry)";
		}
	}

	/* (:dot2 a b) */
	class dot2_node : node
	{
		private node _a;
		private node _b;

		public dot2_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_DOT2)
		{
			_a = a;
			_b = b;
		}

		public node a { get { return _a; } }

		public node b { get { return _b; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_a.to_ruby(cond);
			cond.write("..");
			_b.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:dot2 {a} {b})";
		}
	}

	/* (:dot3 a b) */
	class dot3_node : node
	{
		private node _a;
		private node _b;

		public dot3_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_DOT3)
		{
			_a = a;
			_b = b;
		}

		public node a { get { return _a; } }

		public node b { get { return _b; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_a.to_ruby(cond);
			cond.write("...");
			_b.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:dot3 {a} {b})";
		}
	}

	/* (:colon2 b c) */
	class colon2_node : node
	{
		private node _b;
		private mrb_sym _c;

		public colon2_node(IMrbParser p, node b, mrb_sym c)
			: base(p, node_type.NODE_COLON2)
		{
			_b = b;
			_c = c;
		}

		public node b { get { return _b; } }
		public mrb_sym c { get { return _c; } }

		public override Element to_xml(xml_code_cond cond)
		{
			// TODO:？？？
			var block = cond.CreateElement("class");
			block.SetAttribute("const", p.sym2name(((const_node)_b).name));
			block.SetAttribute("name", p.sym2name(_c));

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_b.to_ruby(cond);
			cond.write("::" + p.sym2name(_c));
		}

		public override string ToString()
		{
			return $"(:colon2 {b} {p.sym2name(c)})";
		}
	}

	/* (:colon3 . c) */
	class colon3_node : node
	{
		private mrb_sym _c;

		public colon3_node(IMrbParser p, mrb_sym c)
			: base(p, node_type.NODE_COLON3)
		{
			_c = c;
		}

		public mrb_sym c { get { return _c; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("::" + p.sym2name(_c));
		}

		public override string ToString()
		{
			return $"(:colon3 . {p.sym2name(c)})";
		}
	}

	/* (:and a b) */
	class and_node : node
	{
		private node _a;
		private node _b;

		public and_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_AND)
		{
			_a = a;
			_b = b;
		}

		public node a { get { return _a; } }

		public node b { get { return _b; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_operation");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "OP");
			field.AppendChild(cond.CreateTextNode("AND"));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "A");
			value.AppendChild(_a.to_xml(cond));
			block.AppendChild(value);

			value = cond.CreateElement("value");
			value.SetAttribute("name", "B");
			value.AppendChild(_b.to_xml(cond));
			block.AppendChild(value);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_a.to_ruby(cond);
			cond.write(" && ");
			_b.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:and {a} {b})";
		}
	}

	/* (:or a b) */
	class or_node : node
	{
		private node _a;
		private node _b;

		public or_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_OR)
		{
			_a = a;
			_b = b;
		}

		public node a { get { return _a; } }
		public node b { get { return _b; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "logic_operation");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "OP");
			field.AppendChild(cond.CreateTextNode("OR"));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "A");
			value.AppendChild(_a.to_xml(cond));
			block.AppendChild(value);

			value = cond.CreateElement("value");
			value.SetAttribute("name", "B");
			value.AppendChild(_b.to_xml(cond));
			block.AppendChild(value);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_a.to_ruby(cond);
			cond.write(" || ");
			_b.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:or {a} {b})";
		}
	}

	/* (:array a...) */
	class array_node : node
	{
		private JsArray<node> _array = new JsArray<node>();
		private bool _item_per_line;

		public array_node(IMrbParser p, node a)
			: base(p, node_type.NODE_ARRAY)
		{
			dump_recur(_array, a);
		}

		public array_node(IMrbParser p, JsArray<node> a, bool item_per_line = false)
			: base(p, node_type.NODE_ARRAY)
		{
			_array = (JsArray<node>)_array.Concat(a);
			_item_per_line = item_per_line;
		}


		public JsArray<node> array { get { return _array; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "lists_create_with");

			var mutation = cond.CreateElement("mutation");
			mutation.SetAttribute("items", _array.Length.ToString());
			block.AppendChild(mutation);

			int i = 0;
			foreach (var item in _array) {
				var value = cond.CreateElement("value");
				value.SetAttribute("name", $"ADD{i}");
				value.AppendChild(_array[i].to_xml(cond));
				block.AppendChild(value);
				i++;
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("[");
			if (_item_per_line) {
				cond.separate_line();
				cond.increment_indent();
			}
			int i = _array.Length;
			foreach (var item in _array) {
				item.to_ruby(cond);
				i--;
				if (i > 0) {
					if (_item_per_line) {
						cond.write(",");
						cond.separate_line();
					}
					else
						cond.write(", ");
				}
				else if (_item_per_line) {
					cond.decrement_indent();
					cond.separate_line();
				}
			}
			cond.write("]");
			cond.decrement_nest();
		}

		public override string ToString()
		{
			var str = $"(:array ";
			foreach (var n in array) {
				str += $"{n} ";
			}
			return str + ")";
		}
	}

	/* (:splat . a) */
	class splat_node : node
	{
		private node _a;

		public splat_node(IMrbParser p, node a)
			: base(p, node_type.NODE_SPLAT)
		{
			_a = a;
		}

		public node a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("*");
			_a.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:splat . {a})";
		}
	}

	/* (:hash (k . v) (k . v)...) */
	class hash_node : node
	{
		public class kv_t
		{
			public node key;
			public node value;

			public kv_t(node key, node value)
			{
				this.key = key;
				this.value = value;
			}

			public override string ToString()
			{
				return $"({key} . {value})";
			}
		}
		JsArray<kv_t> _kvs = new JsArray<kv_t>();

		public hash_node(IMrbParser p, node a)
			: base(p, node_type.NODE_HASH)
		{
			while (a != null) {
				var kv = new kv_t((node)((node)a.car).car, (node)((node)a.car).cdr);
				_kvs.Push(kv);
				a = (node)a.cdr;
			}
		}

		public hash_node(IMrbParser p, JsArray<kv_t> items)
			: base(p, node_type.NODE_HASH)
		{
			_kvs = (JsArray<kv_t>)_kvs.Concat(items);
		}

		public JsArray<kv_t> kvs { get { return _kvs; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			if (cond.nest != 0)
				cond.write("{");
			else
				cond.write_line("{");
			int i = _kvs.Length;
			foreach (var kv in _kvs) {
				kv.key.to_ruby(cond);
				cond.write(" => ");
				kv.value.to_ruby(cond);
				i--; if (i > 0) cond.write(", ");
			}
			if (cond.nest != 0)
				cond.write("}");
			else
				cond.write_line("}");
		}

		public override string ToString()
		{
			var str = $"(:hash ";
			foreach (var n in kvs) {
				str += $"{n} ";
			}
			return str + ")";
		}
	}

	/* (:sym . a) */
	class sym_node : node
	{
		private mrb_sym _name;

		public sym_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_SYM)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(":" + p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(":" + p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:sym . {p.sym2name(name)})";
		}
	}

	/* (:lvar . a) */
	class lvar_node : node
	{
		private mrb_sym _name;

		public lvar_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_LVAR)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:lvar . {p.sym2name(name)})";
		}
	}

	/* (:gvar . a) */
	class gvar_node : node
	{
		private mrb_sym _name;

		public gvar_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_GVAR)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:gvar . {p.sym2name(name)})";
		}
	}

	/* (:ivar . a) */
	class ivar_node : node
	{
		private mrb_sym _name;

		public ivar_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_IVAR)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:ivar . {p.sym2name(name)})";
		}
	}

	/* (:cvar . a) */
	class cvar_node : node
	{
		private mrb_sym _name;

		public cvar_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_CVAR)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:cvar . {p.sym2name(name)})";
		}
	}

	/* (:const . a) */
	class const_node : node
	{
		private mrb_sym _name;

		public const_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_CONST)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_get");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:const . {p.sym2name(name)})";
		}
	}

	/* (:undef a...) */
	class undef_node : node
	{
		private JsArray<mrb_sym> _syms = new JsArray<mrb_sym>();

		public undef_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_UNDEF)
		{
			_syms.Push(sym);
		}

		public JsArray<mrb_sym> syms { get { return _syms; } }

		public override void append(node b)
		{
			_syms.Push((mrb_sym)b.car);
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("undef");
			int i = _syms.Length;
			if (i > 0)
				cond.write(" ");
			foreach (var sym in _syms) {
				cond.write(p.sym2name(sym));
				i--;
				if (i > 0)
					cond.write(", ");
			}
		}

		public override string ToString()
		{
			var str = "(:undef ";
			foreach (var n in syms) {
				str += $"{p.sym2name(n)}, ";
			}
			return str + ")";
		}
	}

	/* (:class class super body) */
	class class_node : node
	{
		private string _prefix;
		private node _type;
		private mrb_sym _name;
		private node _super;
		private mrb_sym _arg;
		private node _body;

		public class_node(IMrbParser p, node c, node s, node b)
			: base(p, node_type.NODE_CLASS)
		{
			if (c.car is int) {
				var type = (int)c.car;
				if (type == 0) {
					_prefix = ""/*":"*/;
					_name = (mrb_sym)c.cdr;
				}
				else if (type == 1) {
					_prefix = "::";
					_name = (mrb_sym)c.cdr;
				}
			}
			else {
				_prefix = "::";
				_type = (node)c.car;
				_name = (mrb_sym)c.cdr;
			}
			_super = s;
			var a = p.locals_node();
			_arg = (a.Length == 0) ? 0 : a[0];
			_body = b;
		}

		public class_node(IMrbParser p, mrb_sym name, node s, node b)
			: base(p, node_type.NODE_CLASS)
		{
			_prefix = "";
			_name = name;
			_super = s;
			_body = b;
		}

		public string prefix { get { return _prefix; } }
		public mrb_sym name { get { return _name; } }
		public node super { get { return _super; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			// TODO:クラス？
			var block = cond.CreateElement("class");
			block.SetAttribute("name", p.sym2name(_name));

			if (_super != null) {
				var field = cond.CreateElement("field");
				field.SetAttribute("name", "SUPER");
				field.AppendChild(_super.to_xml(cond));
				block.AppendChild(field);
			}

			var statement = cond.CreateElement("statement");
			statement.SetAttribute("name", "BODY");
			statement.AppendChild(_body.to_xml(cond));
			block.AppendChild(statement);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("class ");
			if (_type != null)
				_type.to_ruby(cond);
			if (_prefix != null)
				cond.write(_prefix);
			cond.write(p.sym2name(_name) + " ");
			if (_super != null) {
				cond.write("< ");
				if (_super != null)
					_super.to_ruby(cond);
			}
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:class {prefix}{p.sym2name(name)} {super} {body})";
		}
	}

	/* (:sclass obj body) */
	class sclass_node : node
	{
		private node _obj;
		private JsArray<mrb_sym> _super;
		private node _body;

		public sclass_node(IMrbParser p, node o, node b)
			: base(p, node_type.NODE_SCLASS)
		{
			_obj = o;
			_super = p.locals_node();
			_body = b;
		}

		public node obj { get { return _obj; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("class << ");
			_obj.to_ruby(cond);
			/*if (_super != null) {
				cond.write(">");
				_super.to_ruby(cond);
			}*/
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:sclass {obj} {body})";
		}
	}

	/* (:module module body) */
	class module_node : node
	{
		private string _prefix;
		private mrb_sym _name;
		private node _type;
		private JsArray<mrb_sym> _super;
		private node _body;

		public module_node(IMrbParser p, node m, node b)
			: base(p, node_type.NODE_MODULE)
		{
			if (m.car is int) {
				if ((int)m.car == 0) {
					_prefix = ""/*":"*/;
					_name = (mrb_sym)m.cdr;
				}
				else if ((int)m.car == 1) {
					_prefix = "::";
					_name = (mrb_sym)m.cdr;
				}
			}
			else {
				_prefix = "::";
				_type = (node)m.car;
				_name = (mrb_sym)m.cdr;
			}
			_super = p.locals_node();
			_body = b;
		}

		public string prefix { get { return _prefix; } }
		public mrb_sym name { get { return _name; } }
		public object type { get { return _type; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("module " + p.sym2name(_name) + " ");
			if (_super != null) {
				if (_type != null)
					_type.to_ruby(cond);
				foreach (var s in _super) {
					if (_prefix != null)
						cond.write(_prefix + "::");
					cond.write(p.sym2name(s));
				}
			}
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			return $"(:module {prefix}{p.sym2name(name)} {body})";
		}
	}

	public class args_t
	{
		IMrbParser p;
		public mrb_sym name;
		public node arg;

		public args_t(IMrbParser p)
		{
			this.p = p;
		}

		public override string ToString()
		{
			return $"({p.sym2name(name)} {arg})";
		}

		internal void to_ruby(ruby_code_cond cond)
		{
			cond.write(p.sym2name(name));
			if (arg != null) {
				cond.write(" = ");
				arg.to_ruby(cond);
			}
		}
	}

	/* (:def m lv (arg . body)) */
	class def_node : node
	{
		private mrb_sym _name;
		private JsArray<mrb_sym> _local_variables = new JsArray<mrb_sym>();
		private JsArray<arg_node> _mandatory_args = new JsArray<arg_node>();
		private JsArray<args_t> _optional_args = new JsArray<args_t>();
		private mrb_sym _rest;
		private JsArray<arg_node> _post_mandatory_args = new JsArray<arg_node>();
		private mrb_sym _blk;
		private node _body;

		public def_node(IMrbParser p, mrb_sym m, node a, node b)
			: base(p, node_type.NODE_DEF)
		{
			_name = m;
			_local_variables = (JsArray<mrb_sym>)_local_variables.Concat(p.locals_node());
			if (a != null) {
				node n = a;

				if (n.car != null) {
					dump_recur(_mandatory_args, (node)n.car);
				}
				n = (node)n.cdr;
				if (n.car != null) {
					var n2 = (node)n.car;

					while (n2 != null) {
						var arg = new args_t(p);
						arg.name = (mrb_sym)((node)n2.car).car;
						arg.arg = (node)((node)n2.car).cdr;
						_optional_args.Push(arg);
						n2 = (node)n2.cdr;
					}
				}
				n = (node)n.cdr;
				if (n.car != null) {
					_rest = (mrb_sym)n.car;
				}
				n = (node)n.cdr;
				if (n.car != null) {
					dump_recur(_post_mandatory_args, (node)n.car);
				}
				if (n.cdr != null) {
					_blk = (mrb_sym)n.cdr;
				}
			}
			_body = b;
			if (_body is ensure_node) {
				((ensure_node)_body).def = true;
			}
		}

		public def_node(IMrbParser p, mrb_sym m, JsArray<arg_node> a, node b)
			: base(p, node_type.NODE_DEF)
		{
			_name = m;
			_mandatory_args = (JsArray<arg_node>)_mandatory_args.Concat(a);
			_body = b;
		}

		public mrb_sym name { get { return _name; } }
		public JsArray<mrb_sym> local_variables { get { return _local_variables; } }
		public JsArray<arg_node> mandatory_args { get { return _mandatory_args; } }
		internal JsArray<args_t> optional_args { get { return _optional_args; } }
		public mrb_sym rest { get { return _rest; } }
		public JsArray<arg_node> post_mandatory_args { get { return _post_mandatory_args; } }
		public mrb_sym blk { get { return _blk; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "procedures_defreturn");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "NAME");
			field.AppendChild(cond.CreateTextNode(p.sym2name(_name)));
			block.AppendChild(field);

			Element bxml;
			if (_body != null && (bxml = _body.to_xml(cond)) != null) {
				var statement = cond.CreateElement("statement");
				statement.SetAttribute("name", "STACK");
				statement.AppendChild(bxml);
				block.AppendChild(statement);
			}

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("def " + p.sym2name(_name) + "(");
			int i = _mandatory_args.Length + _optional_args.Length
				+ (_rest != 0 ? 1 : 0) + _post_mandatory_args.Length
				+ (_blk != 0 ? 1 : 0);
			if (i > 0) {
				foreach (var a in _mandatory_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var a in _optional_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (_rest != 0) {
					cond.write("*");
					if (_rest != (mrb_sym)(-1))
						cond.write(p.sym2name(_rest));
					i--; if (i > 0) cond.write(", ");
					foreach (var a in _post_mandatory_args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
				}
				if (_blk != 0) {
					cond.write("&" + p.sym2name(_blk));
				}
			}
			cond.write(")");
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			var str = $"(:def {p.sym2name(name)} ";
			foreach (var n in local_variables) {
				str += $"{p.sym2name(n)}, ";
			}
			str += $" (";
			foreach (var n in mandatory_args) {
				str += $"{n}, ";
			}
			foreach (var n in optional_args) {
				str += $"{n}, ";
			}
			str += $"{p.sym2name(rest)}, ";
			foreach (var n in post_mandatory_args) {
				str += $"{n}, ";
			}
			str += $"{p.sym2name(blk)}, ";
			return str + $" . {body}))";
		}
	}

	/* (:sdef obj m lv (arg . body)) */
	class sdef_node : node
	{
		private node _obj;
		private mrb_sym _name;
		private JsArray<mrb_sym> _lv;
		private JsArray<arg_node> _mandatory_args = new JsArray<arg_node>();
		private JsArray<args_t> _optional_args = new JsArray<args_t>();
		private mrb_sym _rest;
		private JsArray<arg_node> _post_mandatory_args = new JsArray<arg_node>();
		private mrb_sym _blk;
		private node _body;

		public sdef_node(IMrbParser p, node o, mrb_sym m, node a, node b)
			: base(p, node_type.NODE_SDEF)
		{
			_obj = o;
			_name = m;
			_lv = p.locals_node();
			if (a != null) {
				node n = a;

				if (n.car != null) {
					dump_recur(_mandatory_args, (node)n.car);
				}
				n = (node)n.cdr;
				if (n.car != null) {
					var n2 = (node)n.car;

					while (n2 != null) {
						var arg = new args_t(p);
						arg.name = (mrb_sym)((node)n2.car).car;
						arg.arg = (node)((node)n2.car).cdr;
						_optional_args.Push(arg);
						n2 = (node)n2.cdr;
					}
				}
				n = (node)n.cdr;
				if (n.car != null) {
					_rest = (mrb_sym)n.car;
				}
				n = (node)n.cdr;
				if (n.car != null) {
					dump_recur(_post_mandatory_args, (node)n.car);
				}
				_blk = (mrb_sym)n.cdr;
			}
			_body = b;
		}

		public node obj { get { return _obj; } }
		public mrb_sym name { get { return _name; } }
		public JsArray<arg_node> mandatory_args { get { return _mandatory_args; } }
		internal JsArray<args_t> optional_args { get { return _optional_args; } }
		public mrb_sym rest { get { return _rest; } }
		public JsArray<arg_node> post_mandatory_args { get { return _post_mandatory_args; } }
		public mrb_sym blk { get { return _blk; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("def self." + p.sym2name(_name) + "(");
			int i = _mandatory_args.Length + _optional_args.Length
				+ (_rest != 0 ? 1 : 0) + _post_mandatory_args.Length
				+ (_blk != 0 ? 1 : 0);
			if (i > 0) {
				foreach (var a in _mandatory_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var a in _optional_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (_rest != 0) {
					if (_rest == (mrb_sym)(-1))
						cond.write("*");
					else
						cond.write(p.sym2name(_rest));
					i--; if (i > 0) cond.write(", ");
					foreach (var a in _post_mandatory_args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
				}
				if (_blk != 0) {
					cond.write("&" + p.sym2name(_blk));
				}
			}
			cond.write(")");
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("end");
		}

		public override string ToString()
		{
			var str = $"(:sdef {obj} {p.sym2name(name)} (";
			foreach (var n in mandatory_args) {
				str += $"{n}, ";
			}
			foreach (var n in optional_args) {
				str += $"{n}, ";
			}
			str += $"{p.sym2name(rest)}, ";
			foreach (var n in post_mandatory_args) {
				str += $"{n}, ";
			}
			str += $"{p.sym2name(blk)}, ";
			return str + $" . {body}))";
		}
	}

	/* (:arg . sym) */
	class arg_node : node
	{
		private mrb_sym _name;

		public arg_node(IMrbParser p, mrb_sym sym)
			: base(p, node_type.NODE_ARG)
		{
			_name = sym;
		}

		public mrb_sym name { get { return _name; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(p.sym2name(_name));
		}

		public override string ToString()
		{
			return $"(:arg . {p.sym2name(name)})";
		}
	}

	/* (:block_arg . a) */
	class block_arg_node : node
	{
		private node _a;

		public block_arg_node(IMrbParser p, node a)
			: base(p, node_type.NODE_BLOCK_ARG)
		{
			_a = a;
		}

		public node a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("&");
			_a.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:block_arg . {a})";
		}
	}

	/* (:block arg body) */
	class block_node : node
	{
		private JsArray<mrb_sym> _local_variables = new JsArray<mrb_sym>();
		private JsArray<node> _mandatory_args = new JsArray<node>();
		private JsArray<args_t> _optional_args = new JsArray<args_t>();
		private mrb_sym _rest;
		private JsArray<node> _post_mandatory_args = new JsArray<node>();
		private mrb_sym _blk;
		private node _body;
		private bool _brace;

		public block_node(IMrbParser p, node args, node body, bool brace)
			: base(p, node_type.NODE_BLOCK)
		{
			_local_variables = (JsArray<mrb_sym>)_local_variables.Concat(p.locals_node());
			if (args != null) {
				node n = args;

				if (n.car != null) {
					dump_recur(_mandatory_args, (node)n.car);
				}
				n = (node)n.cdr;
				if (n.car != null) {
					var n2 = (node)n.car;

					while (n2 != null) {
						var arg = new args_t(p);
						arg.name = (mrb_sym)((node)n2.car).car;
						arg.arg = (node)((node)n2.car).cdr;
						_optional_args.Push(arg);
						n2 = (node)n2.cdr;
					}
				}
				n = (node)n.cdr;
				if (n.car != null) {
					_rest = (mrb_sym)n.car;
				}
				n = (node)n.cdr;
				if (n.car != null) {
					dump_recur(_post_mandatory_args, (node)n.car);
				}
				if (n.cdr != null) {
					_blk = (mrb_sym)n.cdr;
				}
			}
			_body = body;
			if (_body is ensure_node) {
				((ensure_node)_body).def = true;
			}
			_brace = brace;
		}

		public block_node(IMrbParser p, JsArray<node> args, node body, bool brace)
			: base(p, node_type.NODE_BLOCK)
		{
			_mandatory_args = (JsArray<node>)_mandatory_args.Concat(args);
			_body = body;
			if (_body is ensure_node) {
				((ensure_node)_body).def = true;
			}
			_brace = brace;
		}

		public JsArray<mrb_sym> local_variables { get { return _local_variables; } }
		public JsArray<node> mandatory_args { get { return _mandatory_args; } }
		internal JsArray<args_t> optional_args { get { return _optional_args; } }
		public mrb_sym rest { get { return _rest; } }
		public JsArray<node> post_mandatory_args { get { return _post_mandatory_args; } }
		public mrb_sym blk { get { return _blk; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			string beg, end;
			if (_brace) {
				beg = " {";
				end = "}";
			}
			else {
				beg = " do";
				end = "end";
			}

			cond.increment_nest();
			cond.write(beg);
			int i = _mandatory_args.Length + _optional_args.Length
				+ (_rest != 0 ? 1 : 0) + _post_mandatory_args.Length
				+ (_blk != 0 ? 1 : 0);
			if (i > 0) {
				cond.write(" |");
				foreach (var a in _mandatory_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var a in _optional_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (_rest != 0) {
					cond.write("*");
					if (_rest != (mrb_sym)(-1))
						cond.write(p.sym2name(_rest));
					i--; if (i > 0) cond.write(", ");
					foreach (var a in _post_mandatory_args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
				}
				if (_blk != 0) {
					cond.write("&" + p.sym2name(_blk));
				}
				cond.write("|");
			}
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line(end);
		}

		public override string ToString()
		{
			var str = $"(:block ";
			foreach (var n in mandatory_args) {
				str += $"{n}, ";
			}
			foreach (var n in optional_args) {
				str += $"{n}, ";
			}
			return str + $" . {body}))";
		}
	}

	/* (:lambda arg body) */
	class lambda_node : node
	{
		private JsArray<mrb_sym> _local_variables = new JsArray<mrb_sym>();
		private JsArray<node> _mandatory_args = new JsArray<node>();
		private JsArray<args_t> _optional_args = new JsArray<args_t>();
		private mrb_sym _rest;
		private JsArray<node> _post_mandatory_args = new JsArray<node>();
		private mrb_sym _blk;
		private node _body;

		public lambda_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_LAMBDA)
		{
			_local_variables = (JsArray<mrb_sym>)_local_variables.Concat(p.locals_node());
			if (a != null) {
				node n = a;

				if (n.car != null) {
					dump_recur(_mandatory_args, (node)n.car);
				}
				n = (node)n.cdr;
				if (n.car != null) {
					var n2 = (node)n.car;

					while (n2 != null) {
						var arg = new args_t(p);
						arg.name = (mrb_sym)((node)n2.car).car;
						arg.arg = (node)((node)n2.car).cdr;
						_optional_args.Push(arg);
						n2 = (node)n2.cdr;
					}
				}
				n = (node)n.cdr;
				if (n.car != null) {
					_rest = (mrb_sym)n.car;
				}
				n = (node)n.cdr;
				if (n.car != null) {
					dump_recur(_post_mandatory_args, (node)n.car);
				}
				if (n.cdr != null) {
					_blk = (mrb_sym)n.cdr;
				}
			}
			_body = b;
			if (_body is ensure_node) {
				((ensure_node)_body).def = true;
			}
		}

		public JsArray<mrb_sym> local_variables { get { return _local_variables; } }
		public JsArray<node> mandatory_args { get { return _mandatory_args; } }
		internal JsArray<args_t> optional_args { get { return _optional_args; } }
		public mrb_sym rest { get { return _rest; } }
		public JsArray<node> post_mandatory_args { get { return _post_mandatory_args; } }
		public mrb_sym blk { get { return _blk; } }
		public node body { get { return _body; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("{");
			int i = _mandatory_args.Length + _optional_args.Length
				+ (_rest != 0 ? 1 : 0) + _post_mandatory_args.Length
				+ (_blk != 0 ? 1 : 0);
			if (i > 0) {
				cond.write(" |");
				foreach (var a in _mandatory_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var a in _optional_args) {
					a.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (_rest != 0) {
					cond.write("*");
					if (_rest != (mrb_sym)(-1))
						cond.write(p.sym2name(_rest));
					i--; if (i > 0) cond.write(", ");
					foreach (var a in _post_mandatory_args) {
						a.to_ruby(cond);
						i--; if (i > 0) cond.write(", ");
					}
				}
				if (_blk != 0) {
					cond.write("&" + p.sym2name(_blk));
				}
				cond.write("|");
			}
			cond.decrement_nest();
			cond.increment_indent();
			_body.to_ruby(cond);
			cond.decrement_indent();
			cond.write_line("}");
		}

		public override string ToString()
		{
			var str = $"(:lambda ";
			foreach (var n in mandatory_args) {
				str += $"{n}, ";
			}
			foreach (var n in optional_args) {
				str += $"{n}, ";
			}
			return str + $" . {body}))";
		}
	}

	/* (:asgn lhs rhs) */
	class asgn_node : node
	{
		private node _lhs;
		private node _rhs;

		public asgn_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_ASGN)
		{
			_lhs = a;
			_rhs = b;
		}

		public node lhs { get { return _lhs; } }
		public node rhs { get { return _rhs; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "variables_set");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "VAR");
			switch ((node_type)_lhs.car) {
			case node_type.NODE_GVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((gvar_node)_lhs).name)));
				break;
			case node_type.NODE_CVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((cvar_node)_lhs).name)));
				break;
			case node_type.NODE_IVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((ivar_node)_lhs).name)));
				break;
			case node_type.NODE_LVAR:
				field.AppendChild(cond.CreateTextNode(p.sym2name(((lvar_node)_lhs).name)));
				break;
			default:
				// TODO: list[0] = ...？
				field.AppendChild(_lhs.to_xml(cond));
				break;
			}
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "VALUE");
			block.AppendChild(value);

			value.AppendChild(_rhs.to_xml(cond));

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			_lhs.to_ruby(cond);
			cond.write(" = ");
			_rhs.to_ruby(cond);
			cond.decrement_nest();
			cond.write_line();
		}

		public override string ToString()
		{
			return $"(:asgn {lhs} {rhs})";
		}
	}

	/* (:masgn mlhs=(pre rest post)  mrhs) */
	class masgn_node : node
	{
		public class mlhs_t
		{
			public JsArray<node> pre = new JsArray<node>();
			public node rest;
			public bool rest_empty;
			public JsArray<node> post = new JsArray<node>();

			public override string ToString()
			{
				var str = "(";
				foreach (var p in pre) {
					str += $"{p} ";
				}
				str += $"{rest} ";
				foreach (var p in post) {
					str += $"{p} ";
				}
				return str + ")";
			}

			internal void to_ruby(ruby_code_cond cond)
			{
				int i = pre.Length + (rest != null ? 1 : 0) + post.Length;
				foreach (var p in pre) {
					p.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				if (rest != null) {
					rest.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
				foreach (var p in post) {
					p.to_ruby(cond);
					i--; if (i > 0) cond.write(", ");
				}
			}
		}
		private mlhs_t _mlhs;
		private node _mrhs;

		public masgn_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_MASGN)
		{
			_mlhs = new mlhs_t();
			{
				node n2 = a;

				if (n2.car != null) {
					dump_recur(_mlhs.pre, (node)n2.car);
				}
				n2 = (node)n2.cdr;
				if (n2 != null) {
					if (n2.car != null) {
						if (n2.car is int && (int)n2.car == -1) {
							_mlhs.rest = null; //(empty)?
							_mlhs.rest_empty = true;
						}
						else {
							_mlhs.rest = (node)n2.car;
						}
					}
					n2 = (node)n2.cdr;
					if (n2 != null) {
						if (n2.car != null) {
							dump_recur(_mlhs.post, (node)n2.car);
						}
					}
				}
			}
			_mrhs = b;
		}

		public mlhs_t mlhs { get { return _mlhs; } }
		public node mrhs { get { return _mrhs; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_mlhs.to_ruby(cond);
			cond.write(" = ");
			if (_mrhs != null)
				_mrhs.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:masgn {mlhs} {mrhs})";
		}

		internal bool remove(JsArray<mrb_sym> args)
		{
			bool m = false;
			foreach (var a in _mlhs.pre) {
				var arg = a as arg_node;
				if (arg != null)
					m = args.Remove(arg.name);
				else {
					var masgn = a as masgn_node;
					m = masgn.remove(args);
				}
				if (m)
					return true;
			}
			if (_mlhs.rest != null) {
				var rest = _mlhs.rest as arg_node;
				if (rest != null) {
					m = args.Remove(rest.name);
					if (m)
						return true;
				}
			}
			foreach (var a in _mlhs.post) {
				var arg = a as arg_node;
				if (arg != null)
					m = args.Remove(arg.name);
				else {
					var masgn = a as masgn_node;
					m = masgn.remove(args);
				}
				if (m)
					return true;
			}
			return m;
		}
	}

	/* (:asgn lhs rhs) */
	class op_asgn_node : node
	{
		private node _lhs;
		private mrb_sym _op;
		private node _rhs;

		public op_asgn_node(IMrbParser p, node lhs, mrb_sym op, node rhs)
			: base(p, node_type.NODE_OP_ASGN)
		{
			_lhs = lhs;
			_op = op;
			_rhs = rhs;
		}

		public node lhs { get { return _lhs; } }
		public mrb_sym op { get { return _op; } }
		public node rhs { get { return _rhs; } }

		public override Element to_xml(xml_code_cond cond)
		{
			// TODO:Rubyの演算は数値だけとは限らない
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "math_arithmetic");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "OP");
			switch (p.sym2name(op)) {
			case "+": field.AppendChild(cond.CreateTextNode("ADD")); break;
			case "-": field.AppendChild(cond.CreateTextNode("MINUS")); break;
			case "*": field.AppendChild(cond.CreateTextNode("MULTIPLY")); break;
			case "/": field.AppendChild(cond.CreateTextNode("DIVIDE")); break;
			case "**": field.AppendChild(cond.CreateTextNode("POWER")); break;
			}
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "A");
			value.AppendChild(lhs.to_xml(cond));
			block.AppendChild(value);

			value = cond.CreateElement("value");
			value.SetAttribute("name", "B");
			value.AppendChild(rhs.to_xml(cond));
			block.AppendChild(value);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			_lhs.to_ruby(cond);
			cond.write(" " + p.sym2name(_op) + "= ");
			_rhs.to_ruby(cond);
			cond.decrement_nest();
			cond.write_line();
		}

		public override string ToString()
		{
			return $"(:asgn {lhs} {p.sym2name(op)} {rhs})";
		}
	}

	class negate_node : node
	{
		node _n;

		public negate_node(IMrbParser p, node n)
			: base(p, node_type.NODE_NEGATE)
		{
			this._n = n;
		}

		public node n { get { return _n; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "math_single");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "OP");
			field.AppendChild(cond.CreateTextNode("NEG"));
			block.AppendChild(field);

			var value = cond.CreateElement("value");
			value.SetAttribute("name", "NUM");
			value.AppendChild(_n.to_xml(cond));
			block.AppendChild(value);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("-");
			_n.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:nagete {n})";
		}
	}

	/* (:int . i) */
	class int_node : node, IEvaluatable
	{
		private Uint8Array _s;
		private int _base;

		public int_node(IMrbParser p, Uint8Array s, int @base)
			: base(p, node_type.NODE_INT)
		{
			_s = MrbParser.strdup(s, 0);
			_base = @base;
		}

		public int_node(IMrbParser p, int i, int @base = 10)
			: base(p, node_type.NODE_INT)
		{
			string str = "";
			switch (@base) {
			case 2:
				for (uint b = 0x80000000u; b != 0; b >>= 1) {
					str += (b & i) != 0 ? "1" : "0";
				}
				break;
			case 8:
				for (int s = 30; s > 0; s -= 3) {
					str = ((i << s) & 0xE).ToString() + str;
				}
				break;
			case 16:
				str = i.ToString("X");
				break;
			default:
				@base = 10;
				str = i.ToString();
				break;
			}
			_s = MrbParser.UTF8StringToArray(str);
			_base = @base;
		}

		public Uint8Array num { get { return _s; } }
		public int @base { get { return _base; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "math_number");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "NUM");
			field.AppendChild(cond.CreateTextNode(GetString()));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			switch (_base) {
			case 2:
				cond.write("0b" + MrbParser.UTF8ArrayToString(_s, 0));
				break;
			case 8:
				cond.write("0o" + MrbParser.UTF8ArrayToString(_s, 0));
				break;
			case 16:
				cond.write("0x" + MrbParser.UTF8ArrayToString(_s, 0));
				break;
			default:
				cond.write(MrbParser.UTF8ArrayToString(_s, 0));
				break;
			}
		}

		public override string ToString()
		{
			string num = GetString();
			return $"(:int . {num})";
		}

		private string GetString()
		{
			string num;

			switch (_base) {
			case 2:
				num = "0b" + MrbParser.UTF8ArrayToString(_s, 0);
				break;
			case 8:
				num = "0o" + MrbParser.UTF8ArrayToString(_s, 0);
				break;
			case 16:
				num = "0x" + MrbParser.UTF8ArrayToString(_s, 0);
				break;
			default:
				num = MrbParser.UTF8ArrayToString(_s, 0);
				break;
			}

			return num;
		}

		internal long to_i()
		{
			var str = MrbParser.UTF8ArrayToString(_s, 0);
			return Convert.ToInt64(str, _base);
		}

		public node evaluate(string method, JsArray<node> args)
		{
			if (args.Length != 1)
				return null;

			var arg = args[0];
			var a = to_i();

			if (arg is int_node) {
				var b = ((int_node)arg).to_i();

				switch (method) {
				case "+": {
					var c = MrbParser.UTF8StringToArray((a + b).ToString());
					return new int_node(p, c, 10);
				}
				case "-": {
					var c = MrbParser.UTF8StringToArray((a - b).ToString());
					return new int_node(p, c, 10);
				}
				case "*": {
					var c = MrbParser.UTF8StringToArray((a * b).ToString());
					return new int_node(p, c, 10);
				}
				case "/": {
					var c = MrbParser.UTF8StringToArray((a / b).ToString());
					return new int_node(p, c, 10);
				}
				case "%": {
					var c = MrbParser.UTF8StringToArray((a % b).ToString());
					return new int_node(p, c, 10);
				}
				case "==": {
					if (a == b)
						return new true_node(p);
					else
						return new false_node(p);
				}
				case "&": {
					var c = MrbParser.UTF8StringToArray((a & b).ToString());
					return new int_node(p, c, 10);
				}
				case "|": {
					var c = MrbParser.UTF8StringToArray((a | b).ToString());
					return new int_node(p, c, 10);
				}
				case "^": {
					var c = MrbParser.UTF8StringToArray((a ^ b).ToString());
					return new int_node(p, c, 10);
				}
				case "<<": {
					var c = MrbParser.UTF8StringToArray((a << (int)b).ToString());
					return new int_node(p, c, 10);
				}
				case ">>": {
					var c = MrbParser.UTF8StringToArray((a >> (int)b).ToString());
					return new int_node(p, c, 10);
				}
				}
			}
			else if (arg is float_node) {
				var b = ((float_node)arg).to_f();

				switch (method) {
				case "+": {
					var c = MrbParser.UTF8StringToArray((a + b).ToString());
					return new float_node(p, c);
				}
				case "-": {
					var c = MrbParser.UTF8StringToArray((a - b).ToString());
					return new float_node(p, c);
				}
				case "*": {
					var c = MrbParser.UTF8StringToArray((a * b).ToString());
					return new float_node(p, c);
				}
				case "/": {
					var c = MrbParser.UTF8StringToArray((a / b).ToString());
					return new float_node(p, c);
				}
				case "%": {
					var c = MrbParser.UTF8StringToArray((a % b).ToString());
					return new float_node(p, c);
				}
				case "==": {
					if (a == b)
						return new true_node(p);
					else
						return new false_node(p);
				}
				}
			}

			return null;
		}
	}

	/* (:float . i) */
	class float_node : node, IEvaluatable
	{
		private Uint8Array _s;

		public float_node(IMrbParser p, Uint8Array s)
			: base(p, node_type.NODE_FLOAT)
		{
			_s = MrbParser.strdup(s, 0);
		}

		public float_node(IMrbParser p, double f)
			: base(p, node_type.NODE_FLOAT)
		{
			_s = MrbParser.UTF8StringToArray(f.ToString());
		}

		public Uint8Array num { get { return _s; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "math_number");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "NUM");
			field.AppendChild(cond.CreateTextNode(MrbParser.UTF8ArrayToString(_s, 0)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(MrbParser.UTF8ArrayToString(_s, 0));
		}

		public override string ToString()
		{
			return $"(:float . {num})";
		}

		internal double to_f()
		{
			var str = MrbParser.UTF8ArrayToString(_s, 0);
			return Convert.ToDouble(str);
		}

		public node evaluate(string method, JsArray<node> args)
		{
			if (args.Length != 1)
				return null;

			var arg = args[0];
			var a = to_f();

			if (arg is int_node) {
				var b = ((int_node)arg).to_i();

				switch (method) {
				case "+": {
					var c = MrbParser.UTF8StringToArray((a + b).ToString());
					return new float_node(p, c);
				}
				case "-": {
					var c = MrbParser.UTF8StringToArray((a - b).ToString());
					return new float_node(p, c);
				}
				case "*": {
					var c = MrbParser.UTF8StringToArray((a * b).ToString());
					return new float_node(p, c);
				}
				case "/": {
					var c = MrbParser.UTF8StringToArray((a / b).ToString());
					return new float_node(p, c);
				}
				case "%": {
					var c = MrbParser.UTF8StringToArray((a % b).ToString());
					return new float_node(p, c);
				}
				case "==": {
					if (a == b)
						return new true_node(p);
					else
						return new false_node(p);
				}
				}
			}
			else if (arg is float_node) {
				var b = ((float_node)arg).to_f();

				switch (method) {
				case "+": {
					var c = MrbParser.UTF8StringToArray((a + b).ToString());
					return new float_node(p, c);
				}
				case "-": {
					var c = MrbParser.UTF8StringToArray((a - b).ToString());
					return new float_node(p, c);
				}
				case "*": {
					var c = MrbParser.UTF8StringToArray((a * b).ToString());
					return new float_node(p, c);
				}
				case "/": {
					var c = MrbParser.UTF8StringToArray((a / b).ToString());
					return new float_node(p, c);
				}
				case "%": {
					var c = MrbParser.UTF8StringToArray((a % b).ToString());
					return new float_node(p, c);
				}
				case "==": {
					if (a == b)
						return new true_node(p);
					else
						return new false_node(p);
				}
				}
			}

			return null;
		}
	}

	/* (:str . (s . len)) */
	class str_node : node, IEvaluatable
	{
		private Uint8Array _str;
		private int _len;

		public str_node(IMrbParser p, Uint8Array s, int len)
			: base(p, node_type.NODE_STR)
		{
			_str = MrbParser.strndup(s, 0, len);
			_len = len;
		}

		public str_node(IMrbParser p, string s)
			: base(p, node_type.NODE_STR)
		{
			_str = MrbParser.UTF8StringToArray(s);
			_len = _str.Length;
		}

		public Uint8Array str { get { return _str; } }
		public int len { get { return _len; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "text");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "TEXT");
			field.AppendChild(cond.CreateTextNode(MrbParser.UTF8ArrayToString(_str, 0)));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			bool esc;
			var str = MrbParser.UTF8ArrayToStringEsc(_str, 0, out esc);
			if (esc)
				cond.write("\"" + str.Replace("\"", "\\\"") + "\"");
			else
				cond.write("'" + str.Replace("'", "\\'") + "'");
		}

		public override string ToString()
		{
			return $"(:str . ('{str}' . {len}))";
		}

		public string to_s()
		{
			return MrbParser.UTF8ArrayToString(_str, 0);
		}

		public node evaluate(string method, JsArray<node> args)
		{
			var s = to_s();

			switch (method) {
			case "<=>": {
				if ((args.Length != 1) || !(args[0] is str_node))
					break;
				var c = MrbParser.UTF8StringToArray(String.Compare(s, ((str_node)args[0]).to_s()).ToString());
				return new int_node(p, c, 10);
			}
			case "==": {
				if ((args.Length != 1) || !(args[0] is str_node))
					break;
				if (String.Compare(s, ((str_node)args[0]).to_s()) == 0)
					return new true_node(p);
				else
					return new true_node(p);
			}
			case "+": {
				if ((args.Length != 1) || !(args[0] is str_node))
					break;
				var t = MrbParser.UTF8StringToArray(s + ((str_node)args[0]).to_s());
				return new str_node(p, t, t.Length - 1);
			}
			case "*": {
				int a;
				if (args.Length != 1)
					break;
				if (args[0] is int_node)
					a = (int)((int_node)args[0]).to_i();
				else if (args[0] is float_node)
					a = (int)((float_node)args[0]).to_f();
				else
					break;
				var sb = new StringBuilder();
				for (var i = 0; i < a; i++) {
					sb.Append(a);
				}
				var t = MrbParser.UTF8StringToArray(sb.ToString());
				return new str_node(p, t, t.Length - 1);
			}
			}

			return null;
		}
	}

	/* (:dstr . a) */
	class dstr_node : node
	{
		private JsArray<node> _a = new JsArray<node>();

		public dstr_node(IMrbParser p, node a)
			: base(p, node_type.NODE_DSTR)
		{
			dump_recur(_a, a);
		}

		public JsArray<node> a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.increment_nest();
			cond.write("\"");
			foreach (var i in _a) {
				var s = i as str_node;
				if (s != null) {
					var str = MrbParser.UTF8ArrayToString(s.str, 0);
					cond.write(str.Replace("\"", "\\\""));
				}
				else {
					cond.write("#{");
					i.to_ruby(cond);
					cond.write("}");
				}
			}
			cond.write("\"");
			cond.decrement_nest();
		}

		public override string ToString()
		{
			var str = $"(:dstr . ";
			foreach (var n in a) {
				str += $"{n} ";
			}
			return str + ")";
		}
	}

	/* (:str . (s . len)) */
	class xstr_node : node
	{
		private Uint8Array _str;
		private int _len;

		public xstr_node(IMrbParser p, Uint8Array s, int len)
			: base(p, node_type.NODE_XSTR)
		{
			_str = MrbParser.strndup(s, 0, len);
			_len = len;
		}

		public Uint8Array str { get { return _str; } }
		public int len { get { return _len; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("%x(");
			cond.write(MrbParser.UTF8ArrayToString(_str, 0));
			cond.write(")");
		}

		public override string ToString()
		{
			return $"(:str . ({str} . {len}))";
		}
	}

	/* (:xstr . a) */
	class dxstr_node : node
	{
		private JsArray<node> _a = new JsArray<node>();

		public dxstr_node(IMrbParser p, node a)
			: base(p, node_type.NODE_DXSTR)
		{
			dump_recur(_a, a);
		}

		public JsArray<node> a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			foreach (var i in _a) {
				i.to_ruby(cond);
			}
		}

		public override string ToString()
		{
			var str = $"(:xstr . ";
			foreach (var n in a) {
				str += $"{n} ";
			}
			return str + ")";
		}
	}

	/* (:dsym . a) */
	class dsym_node : node
	{
		private dstr_node _a;

		public dsym_node(IMrbParser p, node a)
			: base(p, node_type.NODE_DSYM)
		{
			_a = new dstr_node(p, a);
		}

		public JsArray<node> a { get { return _a.a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(":");
			_a.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:dsym . {a})";
		}
	}

	/* (:regx . (a . a)) */
	class regx_node : node
	{
		Uint8Array _pattern;
		Uint8Array _flags;
		Uint8Array _encp;

		public regx_node(IMrbParser p, Uint8Array pattern, Uint8Array flags, Uint8Array encp)
			: base(p, node_type.NODE_REGX)
		{
			_pattern = pattern;
			_flags = flags;
			_encp = encp;
		}

		public Uint8Array pattern { get { return _pattern; } }
		public Uint8Array flags { get { return _flags; } }
		public Uint8Array encp { get { return _encp; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("/" + MrbParser.UTF8ArrayToString(_pattern, 0));
			cond.write("/" + MrbParser.UTF8ArrayToString(_flags, 0));
			cond.write("/" + MrbParser.UTF8ArrayToString(_encp, 0));
			cond.write("/");
		}

		public override string ToString()
		{
			return $"(:regx . ({pattern} . {flags} . {encp}))";
		}
	}

	/* (:dregx . a) */
	class dregx_node : node
	{
		private JsArray<node> _a = new JsArray<node>();
		private Uint8Array _opt;
		private Uint8Array _tail;

		public dregx_node(IMrbParser p, node a, node b)
			: base(p, node_type.NODE_DREGX)
		{
			dump_recur(_a, a);
			_tail = (Uint8Array)((node)b.cdr).car;
			_opt = (Uint8Array)((node)b.cdr).cdr;
		}

		public JsArray<node> a { get { return _a; } }
		public Uint8Array opt { get { return _opt; } }
		public Uint8Array tail { get { return _tail; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			foreach (var i in _a) {
				i.to_ruby(cond);
			}
			cond.write(MrbParser.UTF8ArrayToString(_opt, 0));
			cond.write(MrbParser.UTF8ArrayToString(_tail, 0));
		}

		public override string ToString()
		{
			var str = $"(:dregx . ";
			foreach (var n in a) {
				str += $"{n} ";
			}
			return str + $"{opt} {tail})";
		}
	}

	/* (:backref . n) */
	class back_ref_node : node
	{
		private int _n;

		public back_ref_node(IMrbParser p, int n)
			: base(p, node_type.NODE_BACK_REF)
		{
			_n = n;
		}

		public int n { get { return _n; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(n.ToString());
		}

		public override string ToString()
		{
			return $"(:backref . {n})";
		}
	}

	/* (:nthref . n) */
	class nth_ref_node : node
	{
		private int _n;

		public nth_ref_node(IMrbParser p, int n)
			: base(p, node_type.NODE_NTH_REF)
		{
			_n = n;
		}

		public int n { get { return _n; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write(n.ToString());
		}

		public override string ToString()
		{
			return $"(:nthref . {n})";
		}
	}

	/* (:heredoc . a) */
	class heredoc_node : node
	{
		private parser_heredoc_info _info;

		public heredoc_node(IMrbParser p)
			: base(p, node_type.NODE_HEREDOC)
		{
			_info = new parser_heredoc_info();
		}

		public parser_heredoc_info info { get { return _info; } }

		public override Element to_xml(xml_code_cond cond)
		{
			var block = cond.CreateElement("block");
			block.SetAttribute("type", "text");

			var field = cond.CreateElement("field");
			field.SetAttribute("name", "TEXT");
			field.AppendChild(cond.CreateTextNode(info.GetString()));
			block.AppendChild(field);

			return block;
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			_info.to_ruby(cond);
		}

		public override string ToString()
		{
			return $"(:heredoc . {info})";
		}
	}

	class literal_delim_node : node
	{
		public literal_delim_node(IMrbParser p)
			: base(p, node_type.NODE_LITERAL_DELIM)
		{
		}

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
		}

		public override string ToString()
		{
			return $"(:literal_delim)";
		}
	}

	/* (:words . a) */
	class words_node : node
	{
		private JsArray<node> _a = new JsArray<node>();

		public words_node(IMrbParser p, node a)
			: base(p, node_type.NODE_WORDS)
		{
			dump_recur(_a, a);
		}

		public JsArray<node> a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("%w(");
			foreach (var i in _a) {
				i.to_ruby(cond);
			}
			cond.write(")");
		}

		public override string ToString()
		{
			return $"(:words . {a})";
		}
	}

	/* (:symbols . a) */
	class symbols_node : node
	{
		private JsArray<node> _a = new JsArray<node>();

		public symbols_node(IMrbParser p, node a)
			: base(p, node_type.NODE_SYMBOLS)
		{
			dump_recur(_a, a);
		}

		public JsArray<node> a { get { return _a; } }

		public override Element to_xml(xml_code_cond cond)
		{
			throw new NotImplementedException();
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("%i{");
			foreach (var i in _a) {
				i.to_ruby(cond);
			}
			cond.write("}");
		}

		public override string ToString()
		{
			return $"(:symbols . {a})";
		}
	}

	class filename_node : str_node
	{
		public filename_node(IMrbParser p, Uint8Array s, int len)
			: base(p, s, len)
		{
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("__FILE__");
		}
	}

	class lineno_node : int_node
	{
		public lineno_node(IMrbParser p, int lineno)
			: base(p, MrbParser.UTF8StringToArray(lineno.ToString()), 10)
		{
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("__LINE__");
		}
	}

	class encoding_node : int_node
	{
		public encoding_node(IMrbParser p, Uint8Array s, int len)
			: base(p, s, len)
		{
		}

		protected override void to_rb(ruby_code_cond cond)
		{
			cond.write("__ENCODING__");
		}
	}
}
