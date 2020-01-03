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
 * @fileoverview Loop blocks for Blockly.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace Blockly
{
	public class Loops
	{
		/**
		 * Common HSV hue for all blocks in this category.
		 */
		public const int HUE = 120;
	}

	public class ControlsRepeatExtBlock : BlockSvg
	{
		public const string type_name = "controls_repeat_ext";

		public ControlsRepeatExtBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for repeat n times (external number).
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.CONTROLS_REPEAT_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "TIMES" },
						{ "check", "Number" }
					}
				} },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Loops.HUE },
				{ "tooltip", Msg.CONTROLS_REPEAT_TOOLTIP },
				{ "helpUrl", Msg.CONTROLS_REPEAT_HELPURL }
			});
			this.appendStatementInput("DO")
				.appendField(Msg.CONTROLS_REPEAT_INPUT_DO);
		}
	}

	public class ControlsRepeatBlock : BlockSvg
	{
		public const string type_name = "controls_repeat";

		public ControlsRepeatBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for repeat n times (internal number).
		 * The "controls_repeat_ext" block is preferred as it is more flexible.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object>{
				{ "message0", Msg.CONTROLS_REPEAT_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_number" },
						{ "name", "TIMES" },
						{ "value", 10 },
						{ "min", 0 },
						{ "precision", 1 }
					}
				} },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Loops.HUE },
				{ "tooltip", Msg.CONTROLS_REPEAT_TOOLTIP },
				{ "helpUrl", Msg.CONTROLS_REPEAT_HELPURL }
			});
			this.appendStatementInput("DO")
				.appendField(Msg.CONTROLS_REPEAT_INPUT_DO);
		}
	}

	public class ControlsWhileUntilBlock : BlockSvg
	{
		public const string type_name = "controls_whileUntil";

		public ControlsWhileUntilBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for "do while/until" loop.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.CONTROLS_WHILEUNTIL_OPERATOR_WHILE, "WHILE"),
				new DropdownItemInfo(Msg.CONTROLS_WHILEUNTIL_OPERATOR_UNTIL, "UNTIL")
			};
			this.setHelpUrl(Msg.CONTROLS_WHILEUNTIL_HELPURL);
			this.setColour(Loops.HUE);
			this.appendValueInput("BOOL")
				.setCheck("Boolean")
				.appendField(new FieldDropdown(OPERATORS), "MODE");
			this.appendStatementInput("DO")
				.appendField(Msg.CONTROLS_WHILEUNTIL_INPUT_DO);
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("MODE")) {
				case "WHILE": return Msg.CONTROLS_WHILEUNTIL_TOOLTIP_WHILE;
				case "UNTIL": return Msg.CONTROLS_WHILEUNTIL_TOOLTIP_UNTIL;
				}
				return "";
			}));
		}
	}

	public class ControlsForBlock : BlockSvg
	{
		public const string type_name = "controls_for";

		public ControlsForBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for "for" loop.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.CONTROLS_FOR_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_variable" },
						{ "name", "VAR" },
						{ "variable", (string)null }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "FROM" },
						{ "check", "Number" },
						{ "align", "RIGHT" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "TO" },
						{ "check", "Number" },
						{ "align", "RIGHT" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "BY" },
						{ "check", "Number" },
						{ "align", "RIGHT" }
					}
				} },
				{ "inputsInline", true },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Loops.HUE },
				{ "helpUrl", Msg.CONTROLS_FOR_HELPURL }
			});
			this.appendStatementInput("DO")
				.appendField(Msg.CONTROLS_FOR_INPUT_DO);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				return Msg.CONTROLS_FOR_TOOLTIP.Replace("%1", thisBlock.getFieldValue("VAR"));
			}));
		}

		/**
		 * Add menu option to create getter block for loop variable.
		 * @param {!Array} options List of menu options to add to.
		 * @this Blockly.Block
		 */
		public override void customContextMenu(JsArray<ContextMenuOption> options)
		{
			if (!this.isCollapsed()) {
				var option = new ContextMenuOption() { enabled = true };
				var name = this.getFieldValue("VAR");
				option.text = Msg.VARIABLES_SET_CREATE_GET.Replace("%1", name);
				var xmlField = goog.dom.createDom("field", null, name);
				xmlField.SetAttribute("name", "VAR");
				var xmlBlock = goog.dom.createDom("block", null, xmlField);
				xmlBlock.SetAttribute("type", VariablesGetBlock.type_name);
				option.callback = ContextMenu.callbackFactory(this, xmlBlock);
				options.Push(option);
			}
		}
	}

	public class ControlsForEachBlock : BlockSvg
	{
		public const string type_name = "controls_forEach";

		public ControlsForEachBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for "for each" loop.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.CONTROLS_FOREACH_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_variable" },
						{ "name", "VAR" },
						{ "variable", (string)null }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "LIST" },
						{ "check", "Array" }
					}
				} },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Loops.HUE },
				{ "helpUrl", Msg.CONTROLS_FOREACH_HELPURL }
			});
			this.appendStatementInput("DO")
				.appendField(Msg.CONTROLS_FOREACH_INPUT_DO);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				return Msg.CONTROLS_FOREACH_TOOLTIP.Replace("%1", thisBlock.getFieldValue("VAR"));
			}));
		}

		/**
		 * Add menu option to create getter block for loop variable.
		 * @param {!Array} options List of menu options to add to.
		 * @this Blockly.Block
		 */
		public override void customContextMenu(JsArray<ContextMenuOption> options)
		{
			if (!this.isCollapsed()) {
				var option = new ContextMenuOption() { enabled = true };
				var name = this.getFieldValue("VAR");
				option.text = Msg.VARIABLES_SET_CREATE_GET.Replace("%1", name);
				var xmlField = goog.dom.createDom("field", null, name);
				xmlField.SetAttribute("name", "VAR");
				var xmlBlock = goog.dom.createDom("block", null, xmlField);
				xmlBlock.SetAttribute("type", VariablesGetBlock.type_name);
				option.callback = ContextMenu.callbackFactory(this, xmlBlock);
				options.Push(option);
			}
		}
	}

	public class ControlsFlowStatementsBlock : BlockSvg
	{
		public const string type_name = "controls_flow_statements";

		public ControlsFlowStatementsBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for flow statements: continue, break.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.CONTROLS_FLOW_STATEMENTS_OPERATOR_BREAK, "BREAK"),
				new DropdownItemInfo(Msg.CONTROLS_FLOW_STATEMENTS_OPERATOR_CONTINUE, "CONTINUE")
			};
			this.setHelpUrl(Msg.CONTROLS_FLOW_STATEMENTS_HELPURL);
			this.setColour(Loops.HUE);
			this.appendDummyInput()
				.appendField(new FieldDropdown(OPERATORS), "FLOW");
			this.setPreviousStatement(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("FLOW")) {
				case "BREAK": return Msg.CONTROLS_FLOW_STATEMENTS_TOOLTIP_BREAK;
				case "CONTINUE": return Msg.CONTROLS_FLOW_STATEMENTS_TOOLTIP_CONTINUE;
				}
				return "";
			}));
		}

		/**
		 * Called whenever anything on the workspace changes.
		 * Add warning if this flow block is not nested inside a loop.
		 * @param {!Abstract} e Change event.
		 * @this Blockly.Block
		 */
		protected override void onchange(Events.Abstract e)
		{
			if (((WorkspaceSvg)this.workspace).isDragging()) {
				return;  // Don't change state at the start of a drag.
			}
			var legal = false;
			// Is the block nested in a loop?
			var block = (Block)this;
			do {
				if (Array.IndexOf(this.LOOP_TYPES, block.type) != -1) {
					legal = true;
					break;
				}
				block = block.getSurroundParent();
			} while (block != null);
			if (legal) {
				this.setWarningText(null);
				if (!this.isInFlyout) {
					this.setDisabled(false);
				}
			}
			else {
				this.setWarningText(Msg.CONTROLS_FLOW_STATEMENTS_WARNING);
				if (!this.isInFlyout && !this.getInheritedDisabled()) {
					this.setDisabled(true);
				}
			}
		}
		/**
		 * List of block types that are loops and thus do not need warnings.
		 * To add a new loop type add this to your code:
		 * Blockly.Blocks["controls_flow_statements"].LOOP_TYPES.push("custom_loop");
		 */
		string[] LOOP_TYPES = new[] {
			ControlsRepeatBlock .type_name,
			ControlsRepeatExtBlock.type_name,
			ControlsForEachBlock.type_name,
			ControlsForBlock.type_name,
			ControlsWhileUntilBlock.type_name
		};
	}
}
