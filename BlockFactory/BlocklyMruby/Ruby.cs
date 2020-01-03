// Porting from 
// https://github.com/jeanlazarou/blockly2ruby
// Copyright (c) 2014 Jean Lazarou
// MIT Lisence
using System;
using System.Linq;
using Bridge;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Blockly;

namespace BlocklyMruby
{
	public partial class Ruby : Generator<node>, IMrbParser
	{
		static Ruby()
		{
			/**
			 * List of illegal variable names.
			 * This is not intended to be a security feature.  Blockly is 100% client-side,
			 * so bypassing this list is trivial.  This is intended to prevent users from
			 * accidentally clobbering a built-in object or function.
			 * @private
			 */
			addReservedWords(
				"Class,Object,BEGIN,END,__ENCODING__,__END__,__FILE__,__LINE__" +
				"alias,and,begin,break,case,class,def,defined?,do,else,elsif,end,ensure,false,for,if,in,module,next" +
				"nil,not,or,redo,rescue,retry,return,self,super,then,true,undef,unless,until,when,while,yield");
		}

		node tree;
		JsArray<node> allNodes;
		locals_node locals;
		bool global = true;

		public Ruby(string filename)
			: base("Ruby")
		{
			this.filename = filename;
		}

		/**
		 * Initialise the database of variable names.
		 */
		public override void init(Workspace workspace)
		{
			locals = new locals_node(null);
		}

		/**
		 * Prepend the generated code with the variable definitions.
		 * @param {string} code Generated code.
		 * @return {string} Completed code.
		 */
		public override string finish(JsArray<node> codes)
		{
			tree = new scope_node(this, new begin_node(this, codes));
			var cond = new ruby_code_cond(filename, INDENT);
			tree.to_ruby(cond);
			allNodes = cond.nodes;
			return cond.ToString();
		}

		/**
		 * Naked values are top-level blocks with outputs that aren't plugged into
		 * anything.
		 * @param {string} line Line of generated code.
		 * @return {string} Legal line of code.
		 */
		public override JsArray<node> scrubNakedValue(JsArray<node> line)
		{
			return line;
		}

		/**
		 * Common tasks for generating Ruby from blocks.
		 * Handles comments for the specified block and any connected value blocks.
		 * Calls any statements following this block.
		 * @param {!Blockly.Block} block The current block.
		 * @param {string} code The Ruby code created for this block.
		 * @return {string} Ruby code with comments and subsequent blocks added.
		 * @this {Blockly.CodeGenerator}
		 * @private
		 */
		public override JsArray<node> scrub_(Block block, JsArray<node> code)
		{
			var commentCode = "";
			// Only collect comments for blocks that aren't inline.
			if (block.outputConnection == null || block.outputConnection.targetConnection == null) {
				// Collect comment for this block.
				var comment = block.getCommentText();
				if (!String.IsNullOrEmpty(comment)) {
					commentCode += this.prefixLines(comment, "# ") + "\n";
				}
				// Collect comments for all value arguments.
				// Don't collect comments for nested statements.
				var inputList = block.inputList;
				for (var x = 0; x < inputList.Length; x++) {
					if (inputList[x].type == Core.INPUT_VALUE) {
						var childBlock = inputList[x].connection.targetBlock();
						if (childBlock != null) {
							comment = allNestedComments(childBlock);
							if (!String.IsNullOrEmpty(comment)) {
								commentCode += this.prefixLines(comment, "# ");
							}
						}
					}
				}
			}
			Block nextBlock = null;
			if (block.nextConnection != null)
				nextBlock = block.nextConnection.targetBlock();
			var nextCode = blockToCode(nextBlock);
			if (nextCode == null)
				return code;
			return code.Concat(nextCode);
		}

		protected override JsArray<node> blockToAstNodes(Block block, node code)
		{
			var result = new JsArray<node>();

			if (code.GetType() == typeof(node)) {
				do {
					var c = (node)code.car;
					c.set_blockid(block.workspace.id, block.id);
					result.Push(c);
				} while ((code = code.cdr as node) != null);
			}
			else {
				code.set_blockid(block.workspace.id, block.id);
				result.Push(code);
			}

			return result;
		}

		protected override node createAstNode(JsArray<node> code)
		{
			return new begin_node(this, code);
		}

		public int lineno { get; set; }
		public int column { get; set; }
		public string filename { get; }

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

		mrb_sym intern(string str)
		{
			return get_sym(str);
		}

		mrb_sym get_var_name(string str)
		{
			int i = syms.IndexOf(str);

			// ローカル変数なら登録されているはずなので、
			if (i < 0)
				// グローバル変数とする
				return get_sym((global ? "$" : "@") + str);

			var sym = (mrb_sym)(i + 1);

			// ローカル変数でなければ、
			if (!local_var_p(sym))
				// グローバル変数とする
				return get_sym((global ? "$" : "@") + str);

			return sym;
		}

		node new_var_node(mrb_sym sym)
		{
			var name = sym2name(sym);
			if (name.StartsWith("$")) {
				return new gvar_node(this, sym);
			}
			else if (name.StartsWith("@@")) {
				return new cvar_node(this, sym);
			}
			else if (name.StartsWith("@")) {
				return new ivar_node(this, sym);
			}
			else {
				return new lvar_node(this, sym);
			}
		}

		node new_num_node(string num)
		{
			var result = MrbParser.parse(num);
			var begin = result as begin_node;
			if ((begin != null) && (begin.progs.Length == 1))
				return begin.progs[0];
			return result;
		}

		node new_str_node(string text)
		{
			var result = MrbParser.parse("\"" + text + "\"");
			var begin = result as begin_node;
			if ((begin != null) && (begin.progs.Length == 1))
				return begin.progs[0];
			return result;
		}

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

		mrb_sym local_add_f(string name)
		{
			var sym = intern(name);
			local_add_f(sym);
			return sym;
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
					n = new fcall_node(this, lvar.name, null);
					return n;
				}
			}

			return lhs;
		}

		public string[] GetBlockId(string filename, int lineno)
		{
			var nodes = allNodes;
			var result = new JsArray<string>();
			foreach (var node in nodes) {
				if (node.filename != filename)
					continue;
				if (lineno < node.start_lineno)
					continue;
				if (node.column == 0) {
					if (lineno >= node.lineno)
						continue;
				}
				else {
					if (lineno > node.lineno)
						continue;
				}
				if (String.IsNullOrEmpty(node.block_id))
					continue;

				result.Push(node.block_id);
			}
			return result;
		}

		public string[] GetBlockId(string filename, int lineno, int column)
		{
			var nodes = allNodes;
			var result = new JsArray<string>();
			foreach (var node in nodes) {
				if (node.filename != filename)
					continue;
				if ((lineno < node.start_lineno))
					continue;
				if ((lineno == node.start_lineno) && (column < node.column))
					continue;
				if (node.column == 0) {
					if (lineno >= node.lineno)
						continue;
				}
				else {
					if ((lineno == node.lineno) && (column > node.column))
						continue;
					if (lineno > node.lineno)
						continue;
				}
				if (String.IsNullOrEmpty(node.block_id))
					continue;

				result.Push(node.block_id);
			}
			return result;
		}

		public void yyError(string message, params object[] expected)
		{
			Console.WriteLine($"{filename}({lineno},{column}): error {String.Format(message, expected)}");
		}
	}
}
