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
		public node controls_if(ControlsIfBlock block)
		{
			// If/elseif/else condition.
			node code = null;
			if (block.elseCount_ != 0) {
				code = statementToCode(block, "ELSE");
				if (code == null) code = new nil_node(this);
			}
			for (var n = block.elseifCount_; n >= 0; n--) {
				var argument = valueToCode(block, "IF" + n);
				if (argument == null) argument = new false_node(this);
				var branch = statementToCode(block, "DO" + n);
				code = new if_node(this, argument, branch, code, false);
			}
			return code;
		}

		public node logic_compare(LogicCompareBlock block)
		{
			// Comparison operator.
			var OPERATORS = new Dictionary<string, string>() {
				{ "EQ", "==" },
				{ "NEQ", "!=" },
				{ "LT", "<" },
				{ "LTE", "<=" },
				{ "GT", ">" },
				{ "GTE", ">=" },
			};
			var @operator = OPERATORS[block.getFieldValue("OP")];
			var argument0 = valueToCode(block, "A");
			if (argument0 == null) argument0 = new int_node(this, 0);
			var argument1 = valueToCode(block, "B");
			if (argument1 == null) argument1 = new int_node(this, 0);
			return new call_node(this, argument0, intern(@operator), argument1);
		}

		public node logic_operation(LogicOperationBlock block)
		{
			// Operations 'and', 'or'.
			var @operator = (block.getFieldValue("OP") == "AND") ? "&&" : "||";
			var argument0 = valueToCode(block, "A");
			var argument1 = valueToCode(block, "B");
			if (argument0 == null && argument1 == null) {
				// If there are no arguments, then the return value is false.
				argument0 = new false_node(this);
				argument1 = new false_node(this);
			}
			else {
				// Single missing arguments have no effect on the return value.
				var defaultArgument = (@operator == "&&") ? (node)new true_node(this) : (node)new false_node(this);
				if (argument0 == null) {
					argument0 = defaultArgument;
				}
				if (argument1 == null) {
					argument1 = defaultArgument;
				}
			}
			if (@operator == "&&")
				return new and_node(this, argument0, argument1);
			else
				return new or_node(this, argument0, argument1);
		}

		public node logic_negate(LogicNegateBlock block)
		{
			// Negation.
			var argument0 = valueToCode(block, "BOOL");
			if (argument0 == null) argument0 = new true_node(this);
			return new call_node(this, argument0, intern("!"), (node)null);
		}

		public node logic_boolean(LogicBooleanBlock block)
		{
			// Boolean values true and false.
			return (block.getFieldValue("BOOL") == "TRUE") ? (node)new true_node(this) : (node)new false_node(this);
		}

		public node logic_null(LogicNullBlock block)
		{
			// Null data type.
			return new nil_node(this);
		}

		public node logic_ternary(LogicTernaryBlock block)
		{
			// Ternary operator.
			var value_if = valueToCode(block, "IF");
			if (value_if == null) value_if = new false_node(this);
			var value_then = valueToCode(block, "THEN");
			if (value_then == null) value_then = new nil_node(this);
			var value_else = valueToCode(block, "ELSE");
			if (value_else == null) value_else = new nil_node(this);
			return new if_node(this, value_if, value_then, value_else, true);
		}
	}
}
