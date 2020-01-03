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
 * @fileoverview Colour blocks for Blockly.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace Blockly
{
	public class Colour
	{
		/**
		 * Common HSV hue for all blocks in this category.
		 */
		public const int HUE = 20;
	}

	public class ColourPickerBlock : BlockSvg
	{
		public const string type_name = "colour_picker";

		public ColourPickerBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for colour picker.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "%1" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_colour" },
						{ "name", "COLOUR" },
						{ "colour", "#ff0000" }
					}
				} },
				{ "output", "Colour" },
				{ "colour", Colour.HUE },
				{ "helpUrl", Msg.COLOUR_PICKER_HELPURL }
			});
			// Assign "this" to a variable for use in the tooltip closure below.
			var thisBlock = this;
			// Colour block is trivial.  Use tooltip of parent block if it exists.
			this.setTooltip(new Func<string>(() => {
				var parent = (BlockSvg)thisBlock.getParent();
				return (parent != null && parent.getInputsInline() && !String.IsNullOrEmpty(parent.tooltip.ToString())) ? parent.tooltip.ToString() :
					Msg.COLOUR_PICKER_TOOLTIP;
			}));
		}
	}

	public class ColourRandomBlock : BlockSvg
	{
		public const string type_name = "colour_random";

		public ColourRandomBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for random colour.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", Msg.COLOUR_RANDOM_TITLE },
				{ "output", "Colour" },
				{ "colour", Colour.HUE },
				{ "tooltip", Msg.COLOUR_RANDOM_TOOLTIP },
				{ "helpUrl", Msg.COLOUR_RANDOM_HELPURL }
			});
		}
	}

	public class ColourRGBBlock : BlockSvg
	{
		public const string type_name = "colour_rgb";

		public ColourRGBBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for composing a colour from RGB components.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.COLOUR_RGB_HELPURL);
			this.setColour(Colour.HUE);
			this.appendValueInput("RED")
				.setCheck("Number")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_RGB_TITLE)
				.appendField(Msg.COLOUR_RGB_RED);
			this.appendValueInput("GREEN")
				.setCheck("Number")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_RGB_GREEN);
			this.appendValueInput("BLUE")
				.setCheck("Number")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_RGB_BLUE);
			this.setOutput(true, "Colour");
			this.setTooltip(Msg.COLOUR_RGB_TOOLTIP);
		}
	}

	public class ColourBlendBlock : BlockSvg
	{
		public const string type_name = "colour_blend";

		public ColourBlendBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/**
		 * Block for blending two colours together.
		 * @this Blockly.Block
		 */
		public override void init()
		{
			this.setHelpUrl(Msg.COLOUR_BLEND_HELPURL);
			this.setColour(Colour.HUE);
			this.appendValueInput("COLOUR1")
				.setCheck("Colour")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_BLEND_TITLE)
				.appendField(Msg.COLOUR_BLEND_COLOUR1);
			this.appendValueInput("COLOUR2")
				.setCheck("Colour")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_BLEND_COLOUR2);
			this.appendValueInput("RATIO")
				.setCheck("Number")
				.setAlign(Core.ALIGN_RIGHT)
				.appendField(Msg.COLOUR_BLEND_RATIO);
			this.setOutput(true, "Colour");
			this.setTooltip(Msg.COLOUR_BLEND_TOOLTIP);
		}
	}
}
