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
 * @fileoverview Logic blocks for Blockly.
 * @author q.neutron@gmail.com (Quynh Neutron)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Logic
	{
		/**
		 * Common HSV hue for all blocks in this category.
		 */
		public const int HUE = 210;
	}

	public class ControlsIfBlock : BlockSvg
	{
		public const string type_name = "controls_if";
		internal int elseifCount_;
		internal int elseCount_;

		public ControlsIfBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for if/elseif/else condition.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.CONTROLS_IF_HELPURL);
			this.setColour(Logic.HUE);
			this.appendValueInput("IF0")
				.setCheck("Boolean")
				.appendField(Msg.CONTROLS_IF_MSG_IF);
			this.appendStatementInput("DO0")
				.appendField(Msg.CONTROLS_IF_MSG_THEN);
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			this.setMutator(new Mutator(new[] { ControlsIfElseIfBlock.type_name, ControlsIfElseBlock.type_name }));
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				if (thisBlock.elseifCount_ == 0 && thisBlock.elseCount_ == 0) {
					return Msg.CONTROLS_IF_TOOLTIP_1;
				}
				else if (thisBlock.elseifCount_ == 0 && thisBlock.elseCount_ != 0) {
					return Msg.CONTROLS_IF_TOOLTIP_2;
				}
				else if (thisBlock.elseifCount_ != 0 && thisBlock.elseCount_ == 0) {
					return Msg.CONTROLS_IF_TOOLTIP_3;
				}
				else if (thisBlock.elseifCount_ != 0 && thisBlock.elseCount_ != 0) {
					return Msg.CONTROLS_IF_TOOLTIP_4;
				}
				return "";
			}));
			this.elseifCount_ = 0;
			this.elseCount_ = 0;
		}

		/**
		 * Create XML to represent the number of else-if and else inputs.
		 * @return {Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			if (this.elseifCount_ == 0 && this.elseCount_ == 0) {
				return null;
			}
			var container = Document.CreateElement<Element>("mutation");
			if (this.elseifCount_ != 0) {
				container.SetAttribute("elseif", this.elseifCount_.ToString());
			}
			if (this.elseCount_ != 0) {
				container.SetAttribute("else", "1");
			}
			return container;
		}

		/**
		 * Parse XML to restore the else-if and else inputs.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			var count = xmlElement.GetAttribute("elseif");
			this.elseifCount_ = count == null ? 0 : Int32.Parse(count);
			count = xmlElement.GetAttribute("else");
			this.elseCount_ = count == null ? 0 : Int32.Parse(count);
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
			var containerBlock = (BlockSvg)workspace.newBlock(ControlsIfIfBlock.type_name);
			containerBlock.initSvg();
			var connection = containerBlock.nextConnection;
			for (var i = 1; i <= this.elseifCount_; i++) {
				var elseifBlock = (BlockSvg)workspace.newBlock(ControlsIfElseIfBlock.type_name);
				elseifBlock.initSvg();
				connection.connect(elseifBlock.previousConnection);
				connection = elseifBlock.nextConnection;
			}
			if (this.elseCount_ != 0) {
				var elseBlock = (BlockSvg)workspace.newBlock(ControlsIfElseBlock.type_name);
				elseBlock.initSvg();
				connection.connect(elseBlock.previousConnection);
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
			var clauseBlock = containerBlock.nextConnection.targetBlock();
			// Count number of inputs.
			this.elseifCount_ = 0;
			this.elseCount_ = 0;
			var valueConnections = new JsArray<Connection> { null };
			var statementConnections = new JsArray<Connection> { null };
			var elseStatementConnection = (Connection)null;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case ControlsIfElseIfBlock.type_name:
					this.elseifCount_++;
					valueConnections.Push(((ControlsIfElseIfBlock)clauseBlock).valueConnection_);
					statementConnections.Push(((ControlsIfElseIfBlock)clauseBlock).statementConnection_);
					break;
				case ControlsIfElseBlock.type_name:
					this.elseCount_++;
					elseStatementConnection = ((ControlsIfElseBlock)clauseBlock).statementConnection_;
					break;
				default:
					throw new Exception("Unknown block type.");
				}
				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
			this.updateShape_();
			// Reconnect any child blocks.
			for (var i = 1; i <= this.elseifCount_; i++) {
				Mutator.reconnect(valueConnections[i], this, "IF" + i);
				Mutator.reconnect(statementConnections[i], this, "DO" + i);
			}
			Mutator.reconnect(elseStatementConnection, this, "ELSE");
		}

		/**
		 * Store pointers to any connected child blocks.
		 * @param {!Blockly.Block} containerBlock Root block in mutator.
		 * @this Blockly.Block
		 */
		public override void saveConnections(Block containerBlock)
		{
			var clauseBlock = containerBlock.nextConnection.targetBlock();
			var i = 1;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case ControlsIfElseIfBlock.type_name: {
					var inputIf = this.getInput("IF" + i);
					var inputDo = this.getInput("DO" + i);
					((ControlsIfElseIfBlock)clauseBlock).valueConnection_ =
						(inputIf != null) ? inputIf.connection.targetConnection : null;
					((ControlsIfElseIfBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
					i++;
				}
				break;
				case ControlsIfElseBlock.type_name: {
					var inputDo = this.getInput("ELSE");
					((ControlsIfElseBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
				}
				break;
				default:
					throw new Exception("Unknown block type.");
				}
				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
		}

		/**
		 * Modify this block to have the correct number of inputs.
		 * @private
		 * @this Blockly.Block
		 */
		private void updateShape_()
		{
			// Delete everything.
			if (this.getInput("ELSE") != null) {
				this.removeInput("ELSE");
			}
			var i = 1;
			while (this.getInput("IF" + i) != null) {
				this.removeInput("IF" + i);
				this.removeInput("DO" + i);
				i++;
			}
			// Rebuild block.
			for (i = 1; i <= this.elseifCount_; i++) {
				this.appendValueInput("IF" + i)
					.setCheck("Boolean")
					.appendField(Msg.CONTROLS_IF_MSG_ELSEIF);
				this.appendStatementInput("DO" + i)
					.appendField(Msg.CONTROLS_IF_MSG_THEN);
			}
			if (this.elseCount_ != 0) {
				this.appendStatementInput("ELSE")
					.appendField(Msg.CONTROLS_IF_MSG_ELSE);
			}
		}
	}

	public class ControlsIfIfBlock : BlockSvg
	{
		public const string type_name = "controls_if_if";

		public ControlsIfIfBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Mutator block for if container.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setColour(Logic.HUE);
			this.appendDummyInput()
				.appendField(Msg.CONTROLS_IF_IF_TITLE_IF);
			this.setNextStatement(true);
			this.setTooltip(Msg.CONTROLS_IF_IF_TOOLTIP);
			this.contextMenu = false;
		}
	}

	public class ControlsIfElseIfBlock : BlockSvg
	{
		public const string type_name = "controls_if_elseif";
		public Connection valueConnection_;
		public Connection statementConnection_;

		public ControlsIfElseIfBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Mutator bolck for else-if condition.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setColour(Logic.HUE);
			this.appendDummyInput()
				.appendField(Msg.CONTROLS_IF_ELSEIF_TITLE_ELSEIF);
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			this.setTooltip(Msg.CONTROLS_IF_ELSEIF_TOOLTIP);
			this.contextMenu = false;
		}
	}

	public class ControlsIfElseBlock : BlockSvg
	{
		public const string type_name = "controls_if_else";
		public Connection statementConnection_;

		public ControlsIfElseBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Mutator block for else condition.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setColour(Logic.HUE);
			this.appendDummyInput()
				.appendField(Msg.CONTROLS_IF_ELSE_TITLE_ELSE);
			this.setPreviousStatement(true);
			this.setTooltip(Msg.CONTROLS_IF_ELSE_TOOLTIP);
			this.contextMenu = false;
		}
	}

	public class LogicCompareBlock : BlockSvg
	{
		public const string type_name = "logic_compare";

		public LogicCompareBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for comparison operator.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var rtlOperators = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo("=", "EQ"),
				new DropdownItemInfo("\u2260", "NEQ"),
				new DropdownItemInfo("\u200F<\u200F", "LT"),
				new DropdownItemInfo("\u200F\u2264\u200F", "LTE"),
				new DropdownItemInfo("\u200F>\u200F", "GT"),
				new DropdownItemInfo("\u200F\u2265\u200F", "GTE")
			};
			var ltrOperators = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo("=", "EQ"),
				new DropdownItemInfo("\u2260", "NEQ"),
				new DropdownItemInfo("<", "LT"),
				new DropdownItemInfo("\u2264", "LTE"),
				new DropdownItemInfo(">", "GT"),
				new DropdownItemInfo("\u2265", "GTE")
			};
			var OPERATORS = this.RTL ? rtlOperators : ltrOperators;
			this.setHelpUrl(Msg.LOGIC_COMPARE_HELPURL);
			this.setColour(Logic.HUE);
			this.setOutput(true, "Boolean");
			this.appendValueInput("A");
			this.appendValueInput("B")
				.appendField(new FieldDropdown(OPERATORS), "OP");
			this.setInputsInline(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "EQ": return Msg.LOGIC_COMPARE_TOOLTIP_EQ;
				case "NEQ": return Msg.LOGIC_COMPARE_TOOLTIP_NEQ;
				case "LT": return Msg.LOGIC_COMPARE_TOOLTIP_LT;
				case "LTE": return Msg.LOGIC_COMPARE_TOOLTIP_LTE;
				case "GT": return Msg.LOGIC_COMPARE_TOOLTIP_GT;
				case "GTE": return Msg.LOGIC_COMPARE_TOOLTIP_GTE;
				}
				return "";
			}));
			this.prevBlocks_ = new Block[] { null, null };
		}

		/**
		 * Called whenever anything on the workspace changes.
		 * Prevent mismatched types from being compared.
		 * @param {!Abstract} e Change event.
		 * @this Blockly.Block
		 */
		protected override void onchange(Events.Abstract e)
		{
			var blockA = this.getInputTargetBlock("A");
			var blockB = this.getInputTargetBlock("B");
			// Disconnect blocks that existed prior to this change if they don't match.
			if (blockA != null && blockB != null &&
				!blockA.outputConnection.checkType_(blockB.outputConnection)) {
				// Mismatch between two inputs.  Disconnect previous and bump it away.
				// Ensure that any disconnections are grouped with the causing event.
				Events.setGroup(e.group);
				for (var i = 0; i < this.prevBlocks_.Length; i++) {
					var block = this.prevBlocks_[i];
					if (block == blockA || block == blockB) {
						block.unplug();
						block.bumpNeighbours_();
					}
				}
				Events.setGroup(false);
			}
			this.prevBlocks_[0] = blockA;
			this.prevBlocks_[1] = blockB;
		}
	}

	public class LogicOperationBlock : BlockSvg
	{
		public const string type_name = "logic_operation";

		public LogicOperationBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for logical operations: "and", "or".
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.LOGIC_OPERATION_AND, "AND"),
				new DropdownItemInfo(Msg.LOGIC_OPERATION_OR, "OR")
			};
			this.setHelpUrl(Msg.LOGIC_OPERATION_HELPURL);
			this.setColour(Logic.HUE);
			this.setOutput(true, "Boolean");
			this.appendValueInput("A")
				.setCheck("Boolean");
			this.appendValueInput("B")
				.setCheck("Boolean")
				.appendField(new FieldDropdown(OPERATORS), "OP");
			this.setInputsInline(true);
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "AND": return Msg.LOGIC_OPERATION_TOOLTIP_AND;
				case "OR": return Msg.LOGIC_OPERATION_TOOLTIP_OR;
				}
				return "";
			}));
		}
	}

	public class LogicNegateBlock : BlockSvg
	{
		public const string type_name = "logic_negate";

		public LogicNegateBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for negation.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.LOGIC_NEGATE_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "BOOL" },
						{ "check", "Boolean" }
					}
				} },
				{ "output", "Boolean" },
				{ "colour", Logic.HUE },
				{ "tooltip", Msg.LOGIC_NEGATE_TOOLTIP },
				{ "helpUrl", Msg.LOGIC_NEGATE_HELPURL }
			});
		}
	}

	public class LogicBooleanBlock : BlockSvg
	{
		public const string type_name = "logic_boolean";

		public LogicBooleanBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for boolean data type: true and false.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "BOOL" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo(Msg.LOGIC_BOOLEAN_TRUE, "TRUE"),
							new DropdownItemInfo(Msg.LOGIC_BOOLEAN_FALSE, "FALSE")
						} }
					}
				} },
				{ "output", "Boolean" },
				{ "colour", Logic.HUE },
				{ "tooltip", Msg.LOGIC_BOOLEAN_TOOLTIP },
				{ "helpUrl", Msg.LOGIC_BOOLEAN_HELPURL }
			});
		}
	}

	public class LogicNullBlock : BlockSvg
	{
		public const string type_name = "logic_null";

		public LogicNullBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for null data type.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.LOGIC_NULL },
				{ "output", null },
				{ "colour", Logic.HUE },
				{ "tooltip", Msg.LOGIC_NULL_TOOLTIP },
				{ "helpUrl", Msg.LOGIC_NULL_HELPURL }
			});
		}
	}

	public class LogicTernaryBlock : BlockSvg
	{
		public const string type_name = "logic_ternary";
		Connection prevParentConnection_;

		public LogicTernaryBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for ternary operator.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.LOGIC_TERNARY_HELPURL);
			this.setColour(Logic.HUE);
			this.appendValueInput("IF")
				.setCheck("Boolean")
				.appendField(Msg.LOGIC_TERNARY_CONDITION);
			this.appendValueInput("THEN")
				.appendField(Msg.LOGIC_TERNARY_IF_TRUE);
			this.appendValueInput("ELSE")
				.appendField(Msg.LOGIC_TERNARY_IF_FALSE);
			this.setOutput(true);
			this.setTooltip(Msg.LOGIC_TERNARY_TOOLTIP);
			this.prevParentConnection_ = null;
		}

		/**
		 * Called whenever anything on the workspace changes.
		 * Prevent mismatched types.
		 * @param {!Abstract} e Change event.
		 * @this Blockly.Block
		 */
		protected override void onchange(Events.Abstract e)
		{
			var blockA = this.getInputTargetBlock("THEN");
			var blockB = this.getInputTargetBlock("ELSE");
			var parentConnection = this.outputConnection.targetConnection;
			// Disconnect blocks that existed prior to this change if they don't match.
			if ((blockA != null || blockB != null) && parentConnection != null) {
				for (var i = 0; i < 2; i++) {
					var block = (i == 1) ? blockA : blockB;
					if (block != null && !block.outputConnection.checkType_(parentConnection)) {
						// Ensure that any disconnections are grouped with the causing event.
						Events.setGroup(e.group);
						if (parentConnection == this.prevParentConnection_) {
							this.unplug();
							parentConnection.getSourceBlock().bumpNeighbours_();
						}
						else {
							block.unplug();
							block.bumpNeighbours_();
						}
						Events.setGroup(false);
					}
				}
			}
			this.prevParentConnection_ = parentConnection;
		}
	}
}
