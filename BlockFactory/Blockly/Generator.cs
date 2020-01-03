/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2012 Google Inc.
 * https://developers.google.com/blockly/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * @fileoverview Utility functions for generating executable code from
 * Blockly code.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Bridge;
using System.Text.RegularExpressions;

namespace Blockly
{
	public abstract class Generator<AstNode> where AstNode : class
	{
		public string name_;

		/// <summary>
		/// Class for a code generator that translates the blocks into a language.
		/// </summary>
		/// <param name="name">Language name of this generator.</param>
		public Generator(string name)
		{
			this.name_ = name;
			this.FUNCTION_NAME_PLACEHOLDER_REGEXP_ =
				new Regex(this.FUNCTION_NAME_PLACEHOLDER_, RegexOptions.Multiline);
		}

		public MethodInfo this[string name] {
			get {
				var m = GetType().GetMethod(name);
				var p = m.GetParameters();
				if ((p.Length != 1) || (p[0].ParameterType != typeof(Block) && !p[0].ParameterType.IsSubclassOf(typeof(Block))))
					return null;
				var type = m.ReturnParameter.ParameterType;
				if (type != typeof(AstNode) && !type.IsSubclassOf(typeof(AstNode))) {
					return null;
				}
				return m;
			}
		}

		/// <summary>
		// Category to separate generated function names from variables and procedures.
		/// </summary>
		public static string NAME_TYPE = "generated_function";

		/// <summary>
		/// Arbitrary code to inject into locations that risk causing infinite loops.
		/// Any instances of '%1' will be replaced by the block ID that failed.
		/// E.g. '  checkTimeout(%1);\n'
		/// </summary>
		public string INFINITE_LOOP_TRAP = null;

		/// <summary>
		/// Arbitrary code to inject before every statement.
		/// Any instances of '%1' will be replaced by the block ID of the statement.
		/// E.g. 'highlight(%1);\n'
		/// </summary>
		public string STATEMENT_PREFIX = null;

		/// <summary>
		/// The method of indenting.  Defaults to two spaces, but language generators
		/// may override this to increase indent or change to tabs.
		/// </summary>
		public string INDENT = "  ";

		/// <summary>
		/// Maximum length for a comment before wrapping.  Does not account for
		/// indenting level.
		/// </summary>
		public int COMMENT_WRAP = 60;

		/// <summary>
		/// List of outer-inner pairings that do NOT require parentheses.
		/// </summary>
		public JsArray<int[]> ORDER_OVERRIDES = new JsArray<int[]>();

		public abstract void init(Workspace workspace);
		public abstract string finish(JsArray<AstNode> code);
		public abstract JsArray<AstNode> scrubNakedValue(JsArray<AstNode> line);
		public abstract JsArray<AstNode> scrub_(Block block, JsArray<AstNode> code);

		/// <summary>
		/// Generate code for all blocks in the workspace to the specified language.
		/// </summary>
		/// <param name="workspace">workspace Workspace to generate code from.</param>
		/// <returns>Generated code.</returns>
		public string workspaceToCode(Workspace workspace)
		{
			if (workspace == null) {
				// Backwards compatibility from before there could be multiple workspaces.
				Console.WriteLine("No workspace specified in workspaceToCode call.  Guessing.");
				workspace = Core.getMainWorkspace();
			}
			this.init(workspace);
			var codes = workspaceToNodes(workspace);
			return this.finish(codes);
		}

		public JsArray<AstNode> workspaceToNodes(Workspace workspace)
		{
			var nodes = new JsArray<AstNode>();
			var blocks = workspace.getTopBlocks(true);
			foreach (var block in blocks) {
				var line = this.blockToCode(block);
				if (line != null) {
					if (block.outputConnection != null/*&& this.scrubNakedValue*/) {
						// This block is a naked value.  Ask the language's code generator if
						// it wants to append a semicolon, or something.
						line = this.scrubNakedValue(line);
					}
					nodes = nodes.Concat(line);
				}
			}
			return nodes;
		}

		// The following are some helpful functions which can be used by multiple
		// languages.

		/// <summary>
		/// Prepend a common prefix onto each line of code.
		/// </summary>
		/// <param name="text">The lines of code.</param>
		/// <param name="prefix">The common prefix.</param>
		/// <returns>The prefixed lines of code.</returns>
		public string prefixLines(string text, string prefix)
		{
			return prefix + text.Replace(new Regex(@"(?!\n$)\n"), "\n" + prefix);
		}

		/// <summary>
		/// Recursively spider a tree of blocks, returning all their comments.
		/// </summary>
		/// <param name="block">The block from which to start spidering.</param>
		/// <returns>Concatenated list of comments.</returns>
		public string allNestedComments(Block block)
		{
			var comments = new JsArray<string>();
			var blocks = block.getDescendants();
			for (var i = 0; i < blocks.Length; i++) {
				var comment = blocks[i].getCommentText();
				if (comment != null) {
					comments.Push(comment);
				}
			}
			// Append an empty string to create a trailing line break when joined.
			if (comments.Length != 0) {
				comments.Push("");
			}
			return String.Join("\n", comments);
		}

		/// <summary>
		/// Generate code for the specified block (and attached blocks).
		/// </summary>
		/// <param name="block">The block to generate code for.</param>
		/// <returns>For statement blocks, the generated code.
		/// For value blocks, an array containing the generated code and an
		/// operator order value.  Returns '' if block is null.
		/// </returns>
		public JsArray<AstNode> blockToCode(Block block)
		{
			if (block == null) {
				return null;
			}
			if (block.disabled) {
				// Skip past this block if it is disabled.
				return this.blockToCode(block.getNextBlock());
			}

			var func = this[block.type];
			if (func == null)
				return null;

			var code = (AstNode)func.Invoke(this, new object[] { block });
			if (code == null) {
				// Block has handled code generation itself.
				return null;
			}
			var result = blockToAstNodes(block, code);
			return this.scrub_(block, result);
		}

		protected abstract JsArray<AstNode> blockToAstNodes(Block block, AstNode code);

		/// <summary>
		/// Generate code representing the specified value input.
		/// </summary>
		/// <param name="block">The block containing the input.</param>
		/// <param name="name">The name of the input.</param>
		/// 
		/// <returns>Generated code or '' if no blocks are connected or the
		/// specified input does not exist.</returns>
		public AstNode valueToCode(Block block, string name)
		{
			var targetBlock = block.getInputTargetBlock(name);
			if (targetBlock == null) {
				return null;
			}
			var code = this.blockToCode(targetBlock);
			if(code == null) {
				return null;
			}
			else if (code.Length == 1) {
				return code[0];
			}
			else {
				throw new Exception();
			}
		}

		/// <summary>
		/// Generate code representing the statement.  Indent the code.
		/// </summary>
		/// <param name="block">The block containing the input.</param>
		/// <param name="name">The name of the input.</param>
		/// <returns>Generated code or '' if no blocks are connected.</returns>
		public AstNode statementToCode(Block block, string name)
		{
			var targetBlock = block.getInputTargetBlock(name);
			var code = this.blockToCode(targetBlock);
			// Value blocks must return code and order of operations info.
			// Statement blocks must only return code.
			//goog.asserts.assertString(code, "Expecting code from statement block \"%s\".",
			//	targetBlock != null ? targetBlock.type : "");
			if (code == null)
				code = new JsArray<AstNode>();
			return createAstNode(code);
		}

		protected abstract AstNode createAstNode(JsArray<AstNode> code);

		/// <summary>
		/// Comma-separated list of reserved words.
		/// </summary>
		public static string RESERVED_WORDS_ = "";

		/// <summary>
		/// Add one or more words to the list of reserved words for this language.
		/// </summary>
		/// <param name="words">Comma-separated list of words to add to the list.
		/// No spaces.  Duplicates are ok.</param>
		public static void addReservedWords(string words)
		{
			RESERVED_WORDS_ += words + ",";
		}

		/// <summary>
		/// This is used as a placeholder in functions defined using
		/// Blockly.Generator.provideFunction_.  It must not be legal code that could
		/// legitimately appear in a function definition (or comment), and it must
		/// not confuse the regular expression parser.
		/// </summary>
		public string FUNCTION_NAME_PLACEHOLDER_ = "{leCUI8hutHZI4480Dc}";
		public Regex FUNCTION_NAME_PLACEHOLDER_REGEXP_;
	}
}
