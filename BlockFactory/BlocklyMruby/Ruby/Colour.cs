// Porting from 
// https://github.com/jeanlazarou/blockly2ruby
// Copyright (c) 2014 Jean Lazarou
// MIT Lisence
using System;
using Bridge;
using System.Collections.Generic;
using Blockly;

namespace BlocklyMruby
{
	partial class Ruby
	{
		public node colour_picker(ColourPickerBlock block)
		{
			// Colour picker.
			var value_colour = block.getFieldValue("COLOUR");
			return new str_node(this, value_colour);
		}

		public node colour_random(ColourRandomBlock block)
		{
			// Generate a random colour.
			var p = new JsArray<node>();
			return new fcall_node(this, intern("colour_random"), p, null);
		}

		public node colour_rgb(ColourRGBBlock block)
		{
			// Compose a colour from RGB components expressed as percentages.
			var r = valueToCode(block, "RED");
			if (r == null) r = new int_node(this, 0);
			var g = valueToCode(block, "GREEN");
			if (g == null) g = new int_node(this, 0);
			var b = valueToCode(block, "BLUE");
			if (b == null) b = new int_node(this, 0);
			var p = new JsArray<node>() { r, g, b };
			return new fcall_node(this, intern("colour_rgb"), p, null);
		}

		public node colour_blend(ColourBlendBlock block)
		{
			// Blend two colours together.
			var colour1 = valueToCode(block, "COLOUR1");
			if (colour1 == null) colour1 = new str_node(this, "#000000");
			var colour2 = valueToCode(block, "COLOUR2");
			if (colour2 == null) colour2 = new str_node(this, "#000000");
			var ratio = valueToCode(block, "RATIO");
			if (ratio == null) ratio = new int_node(this, 0);
			var p = new JsArray<node>() { colour1, colour2, ratio };
			return new fcall_node(this, intern("colour_blend"), p, null);
		}
	}
}