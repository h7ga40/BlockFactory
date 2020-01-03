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
 * @fileoverview Text blocks for Blockly.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Texts
	{
		/**
		 * Common HSV hue for all blocks in this category.
		 */
		public const int HUE = 160;
	}

	public class TextBlock : BlockSvg
	{
		public const string type_name = "text";

		public TextBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for text value.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.TEXT_TEXT_HELPURL);
			this.setColour(Texts.HUE);
			this.appendDummyInput()
				.appendField(this.newQuote_(true))
				.appendField(new FieldTextInput(""), "TEXT")
				.appendField(this.newQuote_(false));
			this.setOutput(true, "String");
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			// Text block is trivial.  Use tooltip of parent block if it exists.
			this.setTooltip(new Func<string>(() => {
				var parent = thisBlock.getParent();
				return (parent != null && parent.getInputsInline() && !String.IsNullOrEmpty(parent.tooltip.ToString())) ? parent.tooltip.ToString() :
					Msg.TEXT_TEXT_TOOLTIP;
			}));
		}

		/**
		 * Create an image of an open or closed quote.
		 * @param {boolean} open True if open quote, false if closed.
		 * @return {!FieldImage} The field image of the quote.
		 * @this Blockly.Block
		 * @private
		 */
		public Field newQuote_(bool open)
		{
			return TextBlock.newQuote_(open, this.RTL);
		}

		public static Field newQuote_(bool open, bool RTL)
		{
			string file;
			if (open == RTL) {
				file = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAKCAQAAAAqJXdxAAAAqUlEQVQI1z3KvUpCcRiA8ef9E4JNHhI0aFEacm1o0BsI0Slx8wa8gLauoDnoBhq7DcfWhggONDmJJgqCPA7neJ7p934EOOKOnM8Q7PDElo/4x4lFb2DmuUjcUzS3URnGib9qaPNbuXvBO3sGPHJDRG6fGVdMSeWDP2q99FQdFrz26Gu5Tq7dFMzUvbXy8KXeAj57cOklgA+u1B5AoslLtGIHQMaCVnwDnADZIFIrXsoXrgAAAABJRU5ErkJggg==";
			}
			else {
				file = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAKCAQAAAAqJXdxAAAAn0lEQVQI1z3OMa5BURSF4f/cQhAKjUQhuQmFNwGJEUi0RKN5rU7FHKhpjEH3TEMtkdBSCY1EIv8r7nFX9e29V7EBAOvu7RPjwmWGH/VuF8CyN9/OAdvqIXYLvtRaNjx9mMTDyo+NjAN1HNcl9ZQ5oQMM3dgDUqDo1l8DzvwmtZN7mnD+PkmLa+4mhrxVA9fRowBWmVBhFy5gYEjKMfz9AylsaRRgGzvZAAAAAElFTkSuQmCC";
			}
			return new FieldImage(file, 12, 12, "\"");
		}
	}

	public class TextJoinBlock : BlockSvg
	{
		public const string type_name = "text_join";
		internal int itemCount_;

		public TextJoinBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for creating a string made up of any number of elements of any type.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.TEXT_JOIN_HELPURL);
			this.setColour(Texts.HUE);
			this.itemCount_ = 2;
			this.updateShape_();
			this.setOutput(true, "String");
			this.setMutator(new Mutator(new[] { TextCreateJoinItemBlock.type_name }));
			this.setTooltip(Msg.TEXT_JOIN_TOOLTIP);
		}

		/**
		 * Create XML to represent number of text inputs.
		 * @return {!Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("items", this.itemCount_.ToString());
			return container;
		}

		/**
		 * Parse XML to restore the text inputs.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			var count = xmlElement.GetAttribute("items");
			this.itemCount_ = count == null ? 0 : Int32.Parse(count);
			this.updateShape_();
		}

		/**
		 * Populate the mutator's dialog with this block's components.
		 * @param {!Workspace} workspace Mutator's workspace.
		 * @return {!Blockly.Block} Root block in mutator.
		 * @this Blockly.Block
		 */
		public override Block decompose(Workspace workspace)
		{
			var containerBlock = (BlockSvg)workspace.newBlock(TextCreateJoinContainerBlock.type_name);
			containerBlock.initSvg();
			var connection = containerBlock.getInput("STACK").connection;
			for (var i = 0; i < this.itemCount_; i++) {
				var itemBlock = (BlockSvg)workspace.newBlock(TextCreateJoinItemBlock.type_name);
				itemBlock.initSvg();
				connection.connect(itemBlock.previousConnection);
				connection = itemBlock.nextConnection;
			}
			return containerBlock;
		}

		/**
		 * Reconfigure this block based on the mutator dialog's components.
		 * @param {!Blockly.Block} containerBlock Root block in mutator.
		 * @this Blockly.Block
		 */
		public override void compose(Block containerBlock)
		{
			var itemBlock = (TextCreateJoinItemBlock)containerBlock.getInputTargetBlock("STACK");
			// Count number of inputs.
			var connections = new JsArray<Connection>();
			while (itemBlock != null) {
				connections.Push(itemBlock.valueConnection_);
				itemBlock = (itemBlock.nextConnection != null) ?
					(TextCreateJoinItemBlock)itemBlock.nextConnection.targetBlock() : null;
			}
			// Disconnect any children that don't belong.
			for (var i = 0; i < this.itemCount_; i++) {
				var connection = this.getInput("ADD" + i).connection.targetConnection;
				if (connection != null && Array.IndexOf(connections, connection) == -1) {
					connection.disconnect();
				}
			}
			this.itemCount_ = connections.Length;
			this.updateShape_();
			// Reconnect any child blocks.
			for (var i = 0; i < this.itemCount_; i++) {
				Mutator.reconnect(connections[i], this, "ADD" + i);
			}
		}

		/**
		 * Store pointers to any connected child blocks.
		 * @param {!Blockly.Block} containerBlock Root block in mutator.
		 * @this Blockly.Block
		 */
		public override void saveConnections(Block containerBlock)
		{
			var itemBlock = (TextCreateJoinItemBlock)containerBlock.getInputTargetBlock("STACK");
			var i = 0;
			while (itemBlock != null) {
				var input = this.getInput("ADD" + i);
				itemBlock.valueConnection_ = (input != null) ? input.connection.targetConnection : null;
				i++;
				itemBlock = (itemBlock.nextConnection != null) ?
					(TextCreateJoinItemBlock)itemBlock.nextConnection.targetBlock() : null;
			}
		}

		/**
		 * Modify this block to have the correct number of inputs.
		 * @private
		 * @this Blockly.Block
		 */
		private void updateShape_()
		{
			if (this.itemCount_ != 0 && this.getInput("EMPTY") != null) {
				this.removeInput("EMPTY");
			}
			else if (this.itemCount_ == 0 && this.getInput("EMPTY") == null) {
				this.appendDummyInput("EMPTY")
					.appendField(this.newQuote_(true))
					.appendField(this.newQuote_(false));
			}
			// Add new inputs.
			int i;
			for (i = 0; i < this.itemCount_; i++) {
				if (this.getInput("ADD" + i) == null) {
					var input = this.appendValueInput("ADD" + i);
					if (i == 0) {
						input.appendField(Msg.TEXT_JOIN_TITLE_CREATEWITH);
					}
				}
			}
			// Remove deleted inputs.
			while (this.getInput("ADD" + i) != null) {
				this.removeInput("ADD" + i);
				i++;
			}
		}

		public Field newQuote_(bool open)
		{
			return TextBlock.newQuote_(open, this.RTL);
		}
	}

	public class TextCreateJoinContainerBlock : BlockSvg
	{
		public const string type_name = "text_create_join_container";

		public TextCreateJoinContainerBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Mutator block for container.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setColour(Texts.HUE);
			this.appendDummyInput()
				.appendField(Msg.TEXT_CREATE_JOIN_TITLE_JOIN);
			this.appendStatementInput("STACK");
			this.setTooltip(Msg.TEXT_CREATE_JOIN_TOOLTIP);
			this.contextMenu = false;
		}
	}

	public class TextCreateJoinItemBlock : BlockSvg
	{
		public const string type_name = "text_create_join_item";
		public Connection valueConnection_;

		public TextCreateJoinItemBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Mutator block for add items.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setColour(Texts.HUE);
			this.appendDummyInput()
				.appendField(Msg.TEXT_CREATE_JOIN_ITEM_TITLE_ITEM);
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			this.setTooltip(Msg.TEXT_CREATE_JOIN_ITEM_TOOLTIP);
			this.contextMenu = false;
		}
	}

	public class TextAppendBlock : BlockSvg
	{
		public const string type_name = "text_append";

		public TextAppendBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for appending to a variable in place.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.TEXT_APPEND_HELPURL);
			this.setColour(Texts.HUE);
			this.appendValueInput("TEXT")
				.appendField(Msg.TEXT_APPEND_TO)
				.appendField(new FieldVariable(
				Msg.TEXT_APPEND_VARIABLE), "VAR")
				.appendField(Msg.TEXT_APPEND_APPENDTEXT);
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				return Msg.TEXT_APPEND_TOOLTIP.Replace("%1", thisBlock.getFieldValue("VAR"));
			}));
		}
	}

	public class TextLengthBlock : BlockSvg
	{
		public const string type_name = "text_length";

		public TextLengthBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for string length.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.TEXT_LENGTH_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "VALUE" },
						{ "check", new [] { "String", "Array" } }
					}
				} },
				{ "output", "Number" },
				{ "colour", Texts.HUE },
				{ "tooltip", Msg.TEXT_LENGTH_TOOLTIP },
				{ "helpUrl", Msg.TEXT_LENGTH_HELPURL }
			});
		}
	}

	public class TextIsEmptyBlock : BlockSvg
	{
		public const string type_name = "text_isEmpty";

		public TextIsEmptyBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for is the string null?
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.TEXT_ISEMPTY_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "VALUE" },
						{ "check", new [] { "String", "Array" } }
					}
				} },
				{ "output", "Boolean" },
				{ "colour", Texts.HUE },
				{ "tooltip", Msg.TEXT_ISEMPTY_TOOLTIP },
				{ "helpUrl", Msg.TEXT_ISEMPTY_HELPURL }
			});
		}
	}

	public class TextIndexOfBlock : BlockSvg
	{
		public const string type_name = "text_indexOf";

		public TextIndexOfBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for finding a substring in the text.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_INDEXOF_OPERATOR_FIRST, "FIRST"),
				new DropdownItemInfo(Msg.TEXT_INDEXOF_OPERATOR_LAST, "LAST")
			};
			this.setHelpUrl(Msg.TEXT_INDEXOF_HELPURL);
			this.setColour(Texts.HUE);
			this.setOutput(true, "Number");
			this.appendValueInput("VALUE")
				.setCheck("String")
				.appendField(Msg.TEXT_INDEXOF_INPUT_INTEXT);
			this.appendValueInput("FIND")
				.setCheck("String")
				.appendField(new FieldDropdown(OPERATORS), "END");
			if (!String.IsNullOrEmpty(Msg.TEXT_INDEXOF_TAIL)) {
				this.appendDummyInput().appendField(Msg.TEXT_INDEXOF_TAIL);
			}
			this.setInputsInline(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				return Msg.TEXT_INDEXOF_TOOLTIP.Replace("%1",
					thisBlock.workspace.options.oneBasedIndex ? "0" : "-1");
			}));
		}
	}

	public class TextCharAtBlock : BlockSvg
	{
		public const string type_name = "text_charAt";
		JsArray<DropdownItemInfo> WHERE_OPTIONS;

		public TextCharAtBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for getting a character from the string.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.WHERE_OPTIONS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_CHARAT_FROM_START, "FROM_START"),
				new DropdownItemInfo(Msg.TEXT_CHARAT_FROM_END, "FROM_END"),
				new DropdownItemInfo(Msg.TEXT_CHARAT_FIRST, "FIRST"),
				new DropdownItemInfo(Msg.TEXT_CHARAT_LAST, "LAST"),
				new DropdownItemInfo(Msg.TEXT_CHARAT_RANDOM, "RANDOM")
			};
			this.setHelpUrl(Msg.TEXT_CHARAT_HELPURL);
			this.setColour(Texts.HUE);
			this.setOutput(true, "String");
			this.appendValueInput("VALUE")
				.setCheck("String")
				.appendField(Msg.TEXT_CHARAT_INPUT_INTEXT);
			this.appendDummyInput("AT");
			this.setInputsInline(true);
			this.updateAt_(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				var where = thisBlock.getFieldValue("WHERE");
				var tooltip = Msg.TEXT_CHARAT_TOOLTIP;
				if (where == "FROM_START" || where == "FROM_END") {
					var msg = (where == "FROM_START") ?
						Msg.LISTS_INDEX_FROM_START_TOOLTIP :
						Msg.LISTS_INDEX_FROM_END_TOOLTIP;
					tooltip += "  " + msg.Replace("%1",
						thisBlock.workspace.options.oneBasedIndex ? "#1" : "#0");
				}
				return tooltip;
			}));
		}

		/**
		 * Create XML to represent whether there is an "AT" input.
		 * @return {!Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			var isAt = this.getInput("AT").type == Core.INPUT_VALUE;
			container.SetAttribute("at", isAt.ToString());
			return container;
		}

		/**
		 * Parse XML to restore the "AT" input.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			// Note: Until January 2013 this block did not have mutations,
			// so "at" defaults to true.
			var isAt = (xmlElement.GetAttribute("at") != "false");
			this.updateAt_(isAt);
		}

		/**
		 * Create or delete an input for the numeric index.
		 * @param {boolean} isAt True if the input should exist.
		 * @private
		 * @this Blockly.Block
		 */
		public void updateAt_(bool isAt)
		{
			// Destroy old "AT" and "ORDINAL" inputs.
			this.removeInput("AT");
			this.removeInput("ORDINAL", true);
			// Create either a value "AT" input or a dummy input.
			if (isAt) {
				this.appendValueInput("AT").setCheck("Number");
				if (!String.IsNullOrEmpty(Msg.ORDINAL_NUMBER_SUFFIX)) {
					this.appendDummyInput("ORDINAL")
						.appendField(Msg.ORDINAL_NUMBER_SUFFIX);
				}
			}
			else {
				this.appendDummyInput("AT");
			}
			if (!String.IsNullOrEmpty(Msg.TEXT_CHARAT_TAIL)) {
				this.removeInput("TAIL", true);
				this.appendDummyInput("TAIL")
					.appendField(Msg.TEXT_CHARAT_TAIL);
			}
			var menu = new FieldDropdown(this.WHERE_OPTIONS, (field, value) => {
				var newAt = (value == "FROM_START") || (value == "FROM_END");
				// The "isAt" variable is available due to this function being a closure.
				if (newAt != isAt) {
					this.updateAt_(newAt);
					// This menu has been destroyed and replaced.  Update the replacement.
					this.setFieldValue(value, "WHERE");
					return null;
				}
				return Script.Undefined;
			});
			this.getInput("AT").appendField(menu, "WHERE");
		}
	}

	public class TextGetSubstringBlock : BlockSvg
	{
		public const string type_name = "text_getSubstring";

		private JsArray<DropdownItemInfo>[] WHERE_OPTIONS;

		public TextGetSubstringBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for getting substring.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			WHERE_OPTIONS = new[] {
				new JsArray<DropdownItemInfo> {
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_START_FROM_START, "FROM_START"),
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_START_FROM_END, "FROM_END"),
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_START_FIRST, "FIRST")
				},
				new JsArray<DropdownItemInfo> {
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_END_FROM_START, "FROM_START"),
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_END_FROM_END, "FROM_END"),
					new DropdownItemInfo(Msg.TEXT_GET_SUBSTRING_END_LAST, "LAST")
				}
			};
			this.setHelpUrl(Msg.TEXT_GET_SUBSTRING_HELPURL);
			this.setColour(Texts.HUE);
			this.appendValueInput("STRING")
				.setCheck("String")
				.appendField(Msg.TEXT_GET_SUBSTRING_INPUT_IN_TEXT);
			this.appendDummyInput("AT1");
			this.appendDummyInput("AT2");
			if (!String.IsNullOrEmpty(Msg.TEXT_GET_SUBSTRING_TAIL)) {
				this.appendDummyInput("TAIL")
					.appendField(Msg.TEXT_GET_SUBSTRING_TAIL);
			}
			this.setInputsInline(true);
			this.setOutput(true, "String");
			this.updateAt_(1, true);
			this.updateAt_(2, true);
			this.setTooltip(Msg.TEXT_GET_SUBSTRING_TOOLTIP);
		}

		/**
		 * Create XML to represent whether there are "AT" inputs.
		 * @return {!Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			var isAt1 = this.getInput("AT1").type == Core.INPUT_VALUE;
			container.SetAttribute("at1", isAt1.ToString());
			var isAt2 = this.getInput("AT2").type == Core.INPUT_VALUE;
			container.SetAttribute("at2", isAt2.ToString());
			return container;
		}

		/**
		 * Parse XML to restore the "AT" inputs.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			var isAt1 = (xmlElement.GetAttribute("at1") == "true");
			var isAt2 = (xmlElement.GetAttribute("at2") == "true");
			this.updateAt_(1, isAt1);
			this.updateAt_(2, isAt2);
		}

		/**
		 * Create or delete an input for a numeric index.
		 * This block has two such inputs, independant of each other.
		 * @param {number} n Specify first or second input (1 or 2).
		 * @param {boolean} isAt True if the input should exist.
		 * @private
		 * @this Blockly.Block
		 */
		public void updateAt_(int n, bool isAt)
		{
			// Create or delete an input for the numeric index.
			// Destroy old "AT" and "ORDINAL" inputs.
			this.removeInput("AT" + n);
			this.removeInput("ORDINAL" + n, true);
			// Create either a value "AT" input or a dummy input.
			if (isAt) {
				this.appendValueInput("AT" + n).setCheck("Number");
				if (!String.IsNullOrEmpty(Msg.ORDINAL_NUMBER_SUFFIX)) {
					this.appendDummyInput("ORDINAL" + n)
						.appendField(Msg.ORDINAL_NUMBER_SUFFIX);
				}
			}
			else {
				this.appendDummyInput("AT" + n);
			}
			// Move tail, if present, to end of block.
			if (n == 2 && !String.IsNullOrEmpty(Msg.TEXT_GET_SUBSTRING_TAIL)) {
				this.removeInput("TAIL", true);
				this.appendDummyInput("TAIL")
					.appendField(Msg.TEXT_GET_SUBSTRING_TAIL);
			}
			var menu = new FieldDropdown(WHERE_OPTIONS[n - 1], (field, value) => {
				var newAt = (value == "FROM_START") || (value == "FROM_END");
				// The "isAt" variable is available due to this function being a
				// closure.
				if (newAt != isAt) {
					this.updateAt_(n, newAt);
					// This menu has been destroyed and replaced.
					// Update the replacement.
					this.setFieldValue(value, "WHERE" + n);
					return null;
				}
				return Script.Undefined;
			});

			this.getInput("AT" + n)
				.appendField(menu, "WHERE" + n);
			if (n == 1) {
				this.moveInputBefore("AT1", "AT2");
			}
		}
	}

	public class TextChangeCaseBlock : BlockSvg
	{
		public const string type_name = "text_changeCase";

		public TextChangeCaseBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for changing capitalization.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_CHANGECASE_OPERATOR_UPPERCASE, "UPPERCASE"),
				new DropdownItemInfo(Msg.TEXT_CHANGECASE_OPERATOR_LOWERCASE, "LOWERCASE"),
				new DropdownItemInfo(Msg.TEXT_CHANGECASE_OPERATOR_TITLECASE, "TITLECASE")
			};
			this.setHelpUrl(Msg.TEXT_CHANGECASE_HELPURL);
			this.setColour(Texts.HUE);
			this.appendValueInput("TEXT")
				.setCheck("String")
				.appendField(new FieldDropdown(OPERATORS), "CASE");
			this.setOutput(true, "String");
			this.setTooltip(Msg.TEXT_CHANGECASE_TOOLTIP);
		}
	}

	public class TextTrimBlock : BlockSvg
	{
		public const string type_name = "text_trim";

		public TextTrimBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for trimming spaces.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_TRIM_OPERATOR_BOTH, "BOTH"),
				new DropdownItemInfo(Msg.TEXT_TRIM_OPERATOR_LEFT, "LEFT"),
				new DropdownItemInfo(Msg.TEXT_TRIM_OPERATOR_RIGHT, "RIGHT")
			};
			this.setHelpUrl(Msg.TEXT_TRIM_HELPURL);
			this.setColour(Texts.HUE);
			this.appendValueInput("TEXT")
				.setCheck("String")
				.appendField(new FieldDropdown(OPERATORS), "MODE");
			this.setOutput(true, "String");
			this.setTooltip(Msg.TEXT_TRIM_TOOLTIP);
		}
	}

	public class TextPrintBlock : BlockSvg
	{
		public const string type_name = "text_print";

		public TextPrintBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for print statement.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object>{
				{ "message0", Msg.TEXT_PRINT_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "TEXT" }
					}
				} },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Texts.HUE },
				{ "tooltip", Msg.TEXT_PRINT_TOOLTIP },
				{ "helpUrl", Msg.TEXT_PRINT_HELPURL }
			});
		}
	}

	public class TextPromptExtBlock : BlockSvg
	{
		public const string type_name = "text_prompt_ext";

		public TextPromptExtBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for prompt function (external message).
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var TYPES = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_PROMPT_TYPE_TEXT, "TEXT"),
				new DropdownItemInfo(Msg.TEXT_PROMPT_TYPE_NUMBER, "NUMBER")
			};
			this.setHelpUrl(Msg.TEXT_PROMPT_HELPURL);
			this.setColour(Texts.HUE);
			// Assign "this" to a variable for use in the closures below.
			var thisBlock = this;
			var dropdown = new FieldDropdown(TYPES, (field, newOp) => {
				thisBlock.updateType_(newOp);
				return Script.Undefined;
			});
			this.appendValueInput("TEXT")
				.appendField(dropdown, "TYPE");
			this.setOutput(true, "String");
			this.setTooltip(new Func<string>(() => {
				return (thisBlock.getFieldValue("TYPE") == "TEXT") ?
					Msg.TEXT_PROMPT_TOOLTIP_TEXT :
					Msg.TEXT_PROMPT_TOOLTIP_NUMBER;
			}));
		}

		/**
		 * Modify this block to have the correct output type.
		 * @param {string} newOp Either "TEXT" or "NUMBER".
		 * @private
		 * @this Blockly.Block
		 */
		public void updateType_(string newOp)
		{
			this.outputConnection.setCheck(newOp == "NUMBER" ? "Number" : "String");
		}

		/**
		 * Create XML to represent the output type.
		 * @return {!Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("type", this.getFieldValue("TYPE"));
			return container;
		}

		/**
		 * Parse XML to restore the output type.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			this.updateType_(xmlElement.GetAttribute("type"));
		}
	}

	public class TextPromptBlock : BlockSvg
	{
		public const string type_name = "text_prompt";

		public TextPromptBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for prompt function (internal message).
		 * The "text_prompt_ext" block is preferred as it is more flexible.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var TYPES = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.TEXT_PROMPT_TYPE_TEXT, "TEXT"),
				new DropdownItemInfo(Msg.TEXT_PROMPT_TYPE_NUMBER, "NUMBER")
			};
			// Assign "this" to a variable for use in the closures below.
			var thisBlock = this;
			this.setHelpUrl(Msg.TEXT_PROMPT_HELPURL);
			this.setColour(Texts.HUE);
			var dropdown = new FieldDropdown(TYPES, (field, newOp) => {
				thisBlock.updateType_(newOp);
				return Script.Undefined;
			});
			this.appendDummyInput()
				.appendField(dropdown, "TYPE")
				.appendField(this.newQuote_(true))
				.appendField(new FieldTextInput(""), "TEXT")
				.appendField(this.newQuote_(false));
			this.setOutput(true, "String");
			this.setTooltip(new Func<string>(() => {
				return (thisBlock.getFieldValue("TYPE") == "TEXT") ?
					Msg.TEXT_PROMPT_TOOLTIP_TEXT :
					Msg.TEXT_PROMPT_TOOLTIP_NUMBER;
			}));
		}

		public Field newQuote_(bool open)
		{
			return TextBlock.newQuote_(open, this.RTL);
		}

		/**
		 * Modify this block to have the correct output type.
		 * @param {string} newOp Either "TEXT" or "NUMBER".
		 * @private
		 * @this Blockly.Block
		 */
		public void updateType_(string newOp)
		{
			this.outputConnection.setCheck(newOp == "NUMBER" ? "Number" : "String");
		}

		/**
		 * Create XML to represent the output type.
		 * @return {!Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("type", this.getFieldValue("TYPE"));
			return container;
		}

		/**
		 * Parse XML to restore the output type.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			this.updateType_(xmlElement.GetAttribute("type"));
		}
	}
}
