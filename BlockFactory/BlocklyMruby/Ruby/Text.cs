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
		public node text(TextBlock block)
		{
			// Text value.
			return new_str_node(block.getFieldValue("TEXT"));
		}

		public node text_join(TextJoinBlock block)
		{
			// Create a string made up of any number of elements of any type.
			if (block.itemCount_ == 0) {
				return new str_node(this, "");
			}
			else if (block.itemCount_ == 1) {
				var argument0 = valueToCode(block, "ADD0");
				if (argument0 == null) argument0 = new str_node(this, "");
				return new call_node(this, argument0, intern("to_s"));
			}
			else {
				var argument0 = valueToCode(block, "ADD0");
				if (argument0 == null)
					argument0 = new str_node(this, "");
				else
					argument0 = new call_node(this, argument0, intern("to_s"));
				for (var n = 1; n < block.itemCount_; n++) {
					var argument1 = valueToCode(block, "ADD" + n);
					if (argument1 == null)
						argument1 = new str_node(this, "");
					else
						argument1 = new call_node(this, argument1, intern("to_s"));
					argument0 = new call_node(this, argument0, intern("+"), argument1);
				}
				return argument0;
			}
		}

		public node text_append(TextAppendBlock block)
		{
			// Append to a variable in place.
			var varName = get_var_name(block.getFieldValue("VAR"));
			var argument0 = valueToCode(block, "TEXT");
			if (argument0 == null)
				argument0 = new str_node(this, "");
			else
				argument0 = new call_node(this, argument0, intern("to_s"));
			var code = new call_node(this, new_var_node(varName), intern("to_s"));
			code = new call_node(this, code, intern("+"), argument0);
			return new asgn_node(this, new_var_node(varName), code);
		}

		public node text_length(TextLengthBlock block)
		{
			// String length.
			var argument0 = valueToCode(block, "VALUE");
			if (argument0 == null) argument0 = new str_node(this, "");
			return new call_node(this, argument0, intern("size"));
		}

		public node text_isEmpty(TextIsEmptyBlock block)
		{
			// Is the string null?
			var argument0 = valueToCode(block, "VALUE");
			if (argument0 == null) argument0 = new str_node(this, "");
			return new call_node(this, argument0, intern("empty?"));
		}

		public node text_indexOf(TextIndexOfBlock block)
		{
			// Search the text for a substring.
			// Should we allow for non-case sensitive???
			var finder = block.getFieldValue("END") == "FIRST" ? "find_first" : "find_last";
			var search = valueToCode(block, "FIND");
			if (search == null) search = new str_node(this, "");
			var text = valueToCode(block, "VALUE");
			if (text == null) text = new str_node(this, "");
			return new call_node(this, text, intern(finder), new JsArray<node>() { search }, null);
		}

		public node text_charAt(TextCharAtBlock block)
		{
			// Get letter at index.
			// Note: Until January 2013 this block did not have the WHERE input.
			var where = block.getFieldValue("WHERE");
			if (String.IsNullOrEmpty(where)) where = "FROM_START";
			var at = valueToCode(block, "AT");
			if (at == null) at = new int_node(this, 1);
			var text = valueToCode(block, "VALUE");
			if (text == null) text = new str_node(this, "");

			// Blockly uses one-based indicies.
			if (at is int_node) {
				// If the index is a naked number, decrement it right now.
				at = new int_node(this, (int)(((int_node)at).to_i() - 1));
			}
			else {
				// If the index is dynamic, decrement it in code.
				at = new call_node(this, at, intern("to_i"));
				at = new call_node(this, at, intern("-"), new int_node(this, 1));
			}

			switch (where) {
			case "FIRST":
				return new call_node(this, text, intern("[]"), new JsArray<node>() { new int_node(this, 0) }, null);
			case "LAST":
				return new call_node(this, text, intern("[]"), new JsArray<node>() { new int_node(this, -1) }, null);
			case "FROM_START":
				return new fcall_node(this, intern("text_get_from_start"), new JsArray<node>() { text, at }, null);
			case "FROM_END":
				return new fcall_node(this, intern("text_get_from_end"), new JsArray<node>() { text, at }, null);
			case "RANDOM":
				return new fcall_node(this, intern("text_random_letter"), new JsArray<node>() { text }, null);
			}
			throw new Exception("Unhandled option (text_charAt).");
		}

		public node text_getSubstring(TextGetSubstringBlock block)
		{
			// Get substring.
			var text = valueToCode(block, "STRING");
			if (text == null) text = new str_node(this, "");
			var where1 = block.getFieldValue("WHERE1");
			var where2 = block.getFieldValue("WHERE2");
			var at1 = valueToCode(block, "AT1");
			if (at1 == null) at1 = new int_node(this, 1);
			var at2 = valueToCode(block, "AT2");
			if (at2 == null) at2 = new int_node(this, 1);
			if (where1 == "FIRST" || (where1 == "FROM_START" && at1 is int_node && ((int_node)at1).to_i() == 1)) {
				at1 = new int_node(this, 0);
			}
			else if (where1 == "FROM_START") {
				// Blockly uses one-based indicies.
				if (at1 is int_node) {
					// If the index is a naked number, decrement it right now.
					at1 = new int_node(this, (int)(((int_node)at1).to_i() - 1));
				}
				else {
					// If the index is dynamic, decrement it in code.
					at1 = new call_node(this, at1, intern("to_i"));
					at1 = new call_node(this, at1, intern("-"), new int_node(this, 1));
				}
			}
			else if (where1 == "FROM_END") {
				if (at1 is int_node) {
					at1 = new int_node(this, (int)(-((int_node)at1).to_i()));
				}
				else {
					at1 = new call_node(this, at1, intern("-@"), (node)null);
					at1 = new call_node(this, at1, intern("to_i"));
				}
			}
			if (where2 == "LAST" || (where2 == "FROM_END" && at2 is int_node && ((int_node)at2).to_i() == 1)) {
				at2 = new int_node(this, -1);
			}
			else if (where2 == "FROM_START") {
				if (at2 is int_node) {
					at2 = new int_node(this, (int)(((int_node)at2).to_i() - 1));
				}
				else {
					at2 = new call_node(this, at2, intern("to_i"));
					at2 = new call_node(this, at2, intern("-"), new int_node(this, 1));
				}
			}
			else if (where2 == "FROM_END") {
				if (at2 is int_node) {
					at2 = new int_node(this, (int)(-((int_node)at2).to_i()));
				}
				else {
					at2 = new call_node(this, at2, intern("-@"), (node)null);
					at2 = new call_node(this, at2, intern("to_i"));
				}
			}
			var code = new dot2_node(this, at1, at2);
			return new call_node(this, text, intern("[]"), new JsArray<node>() { code }, null);
		}

		public node text_changeCase(TextChangeCaseBlock block)
		{
			// Change capitalization.
			var OPERATORS = new Dictionary<string, string>() {
				{ "UPPERCASE", "upcase" },
				{ "LOWERCASE", "downcase"},
				{ "TITLECASE", null }
			};
			node code;
			var @operator = OPERATORS[block.getFieldValue("CASE")];
			if (!String.IsNullOrEmpty(@operator)) {
				@operator = OPERATORS[block.getFieldValue("CASE")];
				var argument0 = valueToCode(block, "TEXT");
				if (argument0 == null) argument0 = new str_node(this, "");
				code = new call_node(this, argument0, intern(@operator));
			}
			else {
				// Title case is not a native Ruby function. Define one.
				var argument0 = valueToCode(block, "TEXT");
				if (argument0 == null) argument0 = new str_node(this, "");
				code = new fcall_node(this, intern("text_to_title_case"), new JsArray<node>() { argument0 }, null);
			}
			return code;
		}

		public node text_trim(TextTrimBlock block)
		{
			// Trim spaces.
			var OPERATORS = new Dictionary<string, string>() {
				{ "LEFT", ".lstrip" },
				{ "RIGHT", ".rstrip"},
				{ "BOTH", ".strip" }
			};
			var @operator = OPERATORS[block.getFieldValue("MODE")];
			var argument0 = valueToCode(block, "TEXT");
			if (argument0 == null) argument0 = new str_node(this, "");
			return new call_node(this, argument0, intern(@operator));
		}

		public node text_print(TextPrintBlock block)
		{
			// Print statement.
			var argument0 = valueToCode(block, "TEXT");
			if (argument0 == null) argument0 = new str_node(this, "");
			return new fcall_node(this, intern("blockly_puts"), new JsArray<node>() { argument0 }, null);
		}

		public node text_prompt(TextPromptBlock block)
		{
			// Prompt function.
			var msg = new str_node(this, block.getFieldValue("TEXT"));
			node code = new fcall_node(this, intern("text_prompt"), new JsArray<node>() { msg }, null);
			var toNumber = block.getFieldValue("TYPE") == "NUMBER";
			if (toNumber) {
				code = new call_node(this, code, intern("to_f"));
			}
			return code;
		}
	}
}
