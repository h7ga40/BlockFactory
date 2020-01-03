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
 * @fileoverview Math blocks for Blockly.
 * @author q.neutron@gmail.com (Quynh Neutron)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Math
	{
		/**
		 * Common HSV hue for all blocks in this category.
		 */
		public const int HUE = 230;
	}

	public class MathNumberBlock : BlockSvg
	{
		public const string type_name = "math_number";

		public MathNumberBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for numeric value.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.MATH_NUMBER_HELPURL);
			this.setColour(Math.HUE);
			this.appendDummyInput()
				.appendField(new FieldNumber("0", "-Infinity", "Infinity", 0), "NUM");
			this.setOutput(true, "Number");
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			// Number block is trivial.  Use tooltip of parent block if it exists.
			this.setTooltip(new Func<string>(() => {
				var parent = thisBlock.getParent();
				return (parent != null && parent.getInputsInline() && !String.IsNullOrEmpty(parent.tooltip.ToString())) ? parent.tooltip.ToString() :
					Msg.MATH_NUMBER_TOOLTIP;
			}));
		}
	}

	public class MathArithmeticBlock : BlockSvg
	{
		public const string type_name = "math_arithmetic";

		public MathArithmeticBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for basic arithmetic operator.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1 %2 %3" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "A" },
						{ "check", "Number" }
					},
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "OP" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo(Msg.MATH_ADDITION_SYMBOL, "ADD"),
							new DropdownItemInfo(Msg.MATH_SUBTRACTION_SYMBOL, "MINUS"),
							new DropdownItemInfo(Msg.MATH_MULTIPLICATION_SYMBOL, "MULTIPLY"),
							new DropdownItemInfo(Msg.MATH_DIVISION_SYMBOL, "DIVIDE"),
							new DropdownItemInfo(Msg.MATH_POWER_SYMBOL, "POWER")
						} }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "B" },
						{ "check", "Number" }
					}
				} },
				{ "inputsInline", true },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "helpUrl", Msg.MATH_ARITHMETIC_HELPURL }
			});
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "ADD": return Msg.MATH_ARITHMETIC_TOOLTIP_ADD;
				case "MINUS": return Msg.MATH_ARITHMETIC_TOOLTIP_MINUS;
				case "MULTIPLY": return Msg.MATH_ARITHMETIC_TOOLTIP_MULTIPLY;
				case "DIVIDE": return Msg.MATH_ARITHMETIC_TOOLTIP_DIVIDE;
				case "POWER": return Msg.MATH_ARITHMETIC_TOOLTIP_POWER;
				};
				return "";
			}));
		}
	}

	public class MathSingleBlock : BlockSvg
	{
		public const string type_name = "math_single";

		public MathSingleBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for advanced math operators with single operand.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "OP" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo (Msg.MATH_SINGLE_OP_ROOT, "ROOT"),
							new DropdownItemInfo (Msg.MATH_SINGLE_OP_ABSOLUTE, "ABS"),
							new DropdownItemInfo ("-", "NEG"),
							new DropdownItemInfo ("ln", "LN"),
							new DropdownItemInfo ("log10", "LOG10"),
							new DropdownItemInfo ("e^", "EXP"),
							new DropdownItemInfo("10^", "POW10")
						} }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "NUM" },
						{ "check", "Number" }
					}
				} },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "helpUrl", Msg.MATH_SINGLE_HELPURL }
			});
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "ROOT": return Msg.MATH_SINGLE_TOOLTIP_ROOT;
				case "ABS": return Msg.MATH_SINGLE_TOOLTIP_ABS;
				case "NEG": return Msg.MATH_SINGLE_TOOLTIP_NEG;
				case "LN": return Msg.MATH_SINGLE_TOOLTIP_LN;
				case "LOG10": return Msg.MATH_SINGLE_TOOLTIP_LOG10;
				case "EXP": return Msg.MATH_SINGLE_TOOLTIP_EXP;
				case "POW10": return Msg.MATH_SINGLE_TOOLTIP_POW10;
				}
				return "";
			}));
		}
	}

	public class MathTrigBlock : BlockSvg
	{
		public const string type_name = "math_trig";

		public MathTrigBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for trigonometry operators.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object>{
				{ "message0", "%1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "OP" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo (Msg.MATH_TRIG_SIN, "SIN" ),
							new DropdownItemInfo (Msg.MATH_TRIG_COS, "COS" ),
							new DropdownItemInfo (Msg.MATH_TRIG_TAN, "TAN" ),
							new DropdownItemInfo (Msg.MATH_TRIG_ASIN, "ASIN" ),
							new DropdownItemInfo (Msg.MATH_TRIG_ACOS, "ACOS" ),
							new DropdownItemInfo (Msg.MATH_TRIG_ATAN, "ATAN" )
						} }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "NUM" },
						{ "check", "Number" }
					}
				} },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "helpUrl", Msg.MATH_TRIG_HELPURL }
			});
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "SIN": return Msg.MATH_TRIG_TOOLTIP_SIN;
				case "COS": return Msg.MATH_TRIG_TOOLTIP_COS;
				case "TAN": return Msg.MATH_TRIG_TOOLTIP_TAN;
				case "ASIN": return Msg.MATH_TRIG_TOOLTIP_ASIN;
				case "ACOS": return Msg.MATH_TRIG_TOOLTIP_ACOS;
				case "ATAN": return Msg.MATH_TRIG_TOOLTIP_ATAN;
				}
				return "";
			}));
		}
	}

	public class MathConstantBlock : BlockSvg
	{
		public const string type_name = "math_constant";

		public MathConstantBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for constants: PI, E, the Golden Ratio, sqrt(2), 1/sqrt(2), INFINITY.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "CONSTANT" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo ("\u03c0", "PI"),
							new DropdownItemInfo ("e", "E"),
							new DropdownItemInfo ("\u03c6", "GOLDEN_RATIO"),
							new DropdownItemInfo ("sqrt(2)", "SQRT2"),
							new DropdownItemInfo ("sqrt(\u00bd)", "SQRT1_2"),
							new DropdownItemInfo ("\u221e", "INFINITY")
						} }
					}
				} },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_CONSTANT_TOOLTIP },
				{ "helpUrl", Msg.MATH_CONSTANT_HELPURL }
			});
		}
	}

	public class MathNumberPropertyBlock : BlockSvg
	{
		public const string type_name = "math_number_property";

		public MathNumberPropertyBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for checking if a number is even, odd, prime, whole, positive,
		 * negative or if it is divisible by certain number.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var PROPERTIES = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.MATH_IS_EVEN, "EVEN"),
				new DropdownItemInfo(Msg.MATH_IS_ODD, "ODD"),
				new DropdownItemInfo(Msg.MATH_IS_PRIME, "PRIME"),
				new DropdownItemInfo(Msg.MATH_IS_WHOLE, "WHOLE"),
				new DropdownItemInfo(Msg.MATH_IS_POSITIVE, "POSITIVE"),
				new DropdownItemInfo(Msg.MATH_IS_NEGATIVE, "NEGATIVE"),
				new DropdownItemInfo(Msg.MATH_IS_DIVISIBLE_BY, "DIVISIBLE_BY")
			};
			this.setColour(Math.HUE);
			this.appendValueInput("NUMBER_TO_CHECK")
				.setCheck("Number");
			var dropdown = new FieldDropdown(PROPERTIES, (field, option) => {
				var divisorInput = (option == "DIVISIBLE_BY");
				this.updateShape_(divisorInput);
				return Script.Undefined;
			});
			this.appendDummyInput()
				.appendField(dropdown, "PROPERTY");
			this.setInputsInline(true);
			this.setOutput(true, "Boolean");
			this.setTooltip(Msg.MATH_IS_TOOLTIP);
		}

		/**
		 * Create XML to represent whether the "divisorInput" should be present.
		 * @return {Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			var divisorInput = (this.getFieldValue("PROPERTY") == "DIVISIBLE_BY");
			container.SetAttribute("divisor_input", divisorInput.ToString());
			return container;
		}

		/**
		 * Parse XML to restore the "divisorInput".
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			var divisorInput = (xmlElement.GetAttribute("divisor_input") == "true");
			this.updateShape_(divisorInput);
		}

		/**
		 * Modify this block to have (or not have) an input for "is divisible by".
		 * @param {boolean} divisorInput True if this block has a divisor input.
		 * @private
		 * @this Blockly.Block
		 */
		public void updateShape_(bool divisorInput)
		{
			// Add or remove a Value Input.
			var inputExists = this.getInput("DIVISOR");
			if (divisorInput) {
				if (inputExists == null) {
					this.appendValueInput("DIVISOR")
						.setCheck("Number");
				}
			}
			else if (inputExists != null) {
				this.removeInput("DIVISOR");
			}
		}
	}

	public class MathChangeBlock : BlockSvg
	{
		public const string type_name = "math_change";

		public MathChangeBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for adding to a variable in place.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.MATH_CHANGE_TITLE },
				{ "args0", new object[] {
					new  Dictionary<string, object> {
						{ "type", "field_variable" },
						{ "name", "VAR" },
						{ "variable", Msg.MATH_CHANGE_TITLE_ITEM }
					},
					new  Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "DELTA" },
						{ "check", "Number" }
					}
				} },
				{ "previousStatement", (Union<string, string[]>)null },
				{ "nextStatement", (Union<string, string[]>)null },
				{ "colour", Variables.HUE },
				{ "helpUrl", Msg.MATH_CHANGE_HELPURL }
			});
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			this.setTooltip(new Func<string>(() => {
				return Msg.MATH_CHANGE_TOOLTIP.Replace("%1", thisBlock.getFieldValue("VAR"));
			}));
		}
	}

	public class MathRoundBlock : BlockSvg
	{
		public const string type_name = "math_round";

		public MathRoundBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for rounding functions.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_dropdown" },
						{ "name", "OP" },
						{ "options", new JsArray<DropdownItemInfo> {
							new DropdownItemInfo(Msg.MATH_ROUND_OPERATOR_ROUND, "ROUND"),
							new DropdownItemInfo(Msg.MATH_ROUND_OPERATOR_ROUNDUP, "ROUNDUP"),
							new DropdownItemInfo(Msg.MATH_ROUND_OPERATOR_ROUNDDOWN, "ROUNDDOWN")
						} }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "NUM" },
						{ "check", "Number" }
					}
				} },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_ROUND_TOOLTIP },
				{ "helpUrl", Msg.MATH_ROUND_HELPURL }
			});
		}
	}

	public class MathOnListBlock : BlockSvg
	{
		public const string type_name = "math_on_list";

		public MathOnListBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for evaluating a list of numbers to return sum, average, min, max,
		 * etc.  Some functions also work on text (min, max, mode, median).
		 * @this Blockly.Block
		 */
		public override void init()
		{
			var OPERATORS = new JsArray<DropdownItemInfo> {
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_SUM, "SUM"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_MIN, "MIN"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_MAX, "MAX"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_AVERAGE, "AVERAGE"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_MEDIAN, "MEDIAN"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_MODE, "MODE"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_STD_DEV, "STD_DEV"),
				new DropdownItemInfo(Msg.MATH_ONLIST_OPERATOR_RANDOM, "RANDOM")
			};
			// Assign "this" to a variable for use in the closures below.
			var thisBlock = this;
			this.setHelpUrl(Msg.MATH_ONLIST_HELPURL);
			this.setColour(Math.HUE);
			this.setOutput(true, "Number");
			var dropdown = new FieldDropdown(OPERATORS, (field, newOp) => {
				thisBlock.updateType_(newOp);
				return Script.Undefined;
			});
			this.appendValueInput("LIST")
				.setCheck("Array")
				.appendField(dropdown, "OP");
			this.setTooltip(new Func<string>(() => {
				switch (thisBlock.getFieldValue("OP")) {
				case "SUM": return Msg.MATH_ONLIST_TOOLTIP_SUM;
				case "MIN": return Msg.MATH_ONLIST_TOOLTIP_MIN;
				case "MAX": return Msg.MATH_ONLIST_TOOLTIP_MAX;
				case "AVERAGE": return Msg.MATH_ONLIST_TOOLTIP_AVERAGE;
				case "MEDIAN": return Msg.MATH_ONLIST_TOOLTIP_MEDIAN;
				case "MODE": return Msg.MATH_ONLIST_TOOLTIP_MODE;
				case "STD_DEV": return Msg.MATH_ONLIST_TOOLTIP_STD_DEV;
				case "RANDOM": return Msg.MATH_ONLIST_TOOLTIP_RANDOM;
				}
				return "";
			}));
		}

		/**
		 * Modify this block to have the correct output type.
		 * @param {string} newOp Either "MODE" or some op than returns a number.
		 * @private
		 * @this Blockly.Block
		 */
		public void updateType_(string newOp)
		{
			if (newOp == "MODE") {
				this.outputConnection.setCheck("Array");
			}
			else {
				this.outputConnection.setCheck("Number");
			}
		}

		/**
		 * Create XML to represent the output type.
		 * @return {Element} XML storage element.
		 * @this Blockly.Block
		 */
		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("op", this.getFieldValue("OP"));
			return container;
		}

		/**
		 * Parse XML to restore the output type.
		 * @param {!Element} xmlElement XML storage element.
		 * @this Blockly.Block
		 */
		public override void domToMutation(Element xmlElement)
		{
			this.updateType_(xmlElement.GetAttribute("op"));
		}
	}

	public class MathModuloBlock : BlockSvg
	{
		public const string type_name = "math_modulo";

		public MathModuloBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for remainder of a division.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.MATH_MODULO_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "DIVIDEND" },
						{ "check", "Number" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "DIVISOR" },
						{ "check", "Number" }
					}
				} },
				{ "inputsInline", true },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_MODULO_TOOLTIP },
				{ "helpUrl", Msg.MATH_MODULO_HELPURL }
			});
		}
	}

	public class MathConstrainBlock : BlockSvg
	{
		public const string type_name = "math_constrain";

		public MathConstrainBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for constraining a number between two limits.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.MATH_CONSTRAIN_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "VALUE" },
						{ "check", "Number" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "LOW" },
						{ "check", "Number" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "HIGH" },
						{ "check", "Number" }
					}
				} },
				{ "inputsInline", true },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_CONSTRAIN_TOOLTIP },
				{ "helpUrl", Msg.MATH_CONSTRAIN_HELPURL }
			});
		}
	}

	public class MathRandomIntBlock : BlockSvg
	{
		public const string type_name = "math_random_int";

		public MathRandomIntBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for random integer between [X] and [Y].
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.MATH_RANDOM_INT_TITLE },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "FROM" },
						{ "check", "Number" }
					},
					new Dictionary<string, object> {
						{ "type", "input_value" },
						{ "name", "TO" },
						{ "check", "Number" }
					}
				} },
				{ "inputsInline", true },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_RANDOM_INT_TOOLTIP },
				{ "helpUrl", Msg.MATH_RANDOM_INT_HELPURL }
			});
		}
	}

	public class MathRandomFloatBlock : BlockSvg
	{
		public const string type_name = "math_random_float";

		public MathRandomFloatBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for random fraction between 0 and 1.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.MATH_RANDOM_FLOAT_TITLE_RANDOM },
				{ "output", "Number" },
				{ "colour", Math.HUE },
				{ "tooltip", Msg.MATH_RANDOM_FLOAT_TOOLTIP },
				{ "helpUrl", Msg.MATH_RANDOM_FLOAT_HELPURL }
			});
		}
	}
}
