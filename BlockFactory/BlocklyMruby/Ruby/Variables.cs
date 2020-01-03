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
		public node variables_get(VariablesGetBlock block)
		{
			// Variable getter.
			var code = get_var_name(block.getFieldValue("VAR"));
			return new_var_node(code);
		}

		public node variables_set(VariablesSetBlock block)
		{
			// Variable setter.
			var argument0 = valueToCode(block, "VALUE");
			if (argument0 == null) argument0 = new int_node(this, 0);
			var varName = get_var_name(block.getFieldValue("VAR"));
			return new asgn_node(this, new_var_node(varName), argument0);
		}
	}
}
