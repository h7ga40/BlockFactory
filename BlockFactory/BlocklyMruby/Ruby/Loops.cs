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
		public node controls_repeat(ControlsRepeatBlock block)
		{
			// Repeat n times (internal number).
			var times = block.getFieldValue("TIMES");
			var repeats = new int_node(this, times == null ? 0 : Int32.Parse(times));
			var branch = statementToCode(block, "DO");
			return new call_node(this, repeats, intern("times"), new JsArray<node>(), new block_node(this, new JsArray<node>(), branch, false));
		}

		public node controls_repeat_ext(ControlsRepeatExtBlock block)
		{
			// Repeat n times (external number).
			var repeats = valueToCode(block, "TIMES");
			if (repeats == null) repeats = new int_node(this, 0);
			if (repeats is int_node) {
			}
			else {
				repeats = new call_node(this, repeats, intern("to_i"));
			}
			var branch = statementToCode(block, "DO");
			return new call_node(this, repeats, intern("times"), new JsArray<node>(), new block_node(this, new JsArray<node>(), branch, false));
		}

		public node controls_whileUntil(ControlsWhileUntilBlock block)
		{
			// Do while/until loop.
			var until = block.getFieldValue("MODE") == "UNTIL";
			var argument0 = valueToCode(block, "BOOL");
			if (argument0 == null) argument0 = new false_node(this);
			var branch = statementToCode(block, "DO");
			if (until)
				return new until_node(this, argument0, branch);
			else
				return new while_node(this, argument0, branch);
		}

		public node controls_for(ControlsForBlock block)
		{
			// For loop.
			var lv = local_switch();

			var loopVar = local_add_f(block.getFieldValue("VAR"));
			var fromVal = valueToCode(block, "FROM");
			if (fromVal == null) fromVal = new int_node(this, 0);
			var toVal = valueToCode(block, "TO");
			if (toVal == null) toVal = new int_node(this, 0);
			var increment = valueToCode(block, "BY");
			var branch = statementToCode(block, "DO");

			if (fromVal is int_node && toVal is int_node &&
				(increment == null || increment is int_node)) {

				if (increment == null) increment = new int_node(this, 1);

				// All parameters are simple numbers.
			}
			else {
				fromVal = new call_node(this, fromVal, intern("to_f"));
				toVal = new call_node(this, toVal, intern("to_f"));
				if (increment == null)
					increment = new float_node(this, 1);
				else
					increment = new call_node(this, increment, intern("to_f"));
			}

			local_resume(lv);

			var arg = new hash_node(this, new JsArray<hash_node.kv_t>() {
				new hash_node.kv_t(new sym_node(this, intern("from")), fromVal),
				new hash_node.kv_t(new sym_node(this, intern("to")), toVal),
				new hash_node.kv_t(new sym_node(this, intern("by")), increment),
			});
			var exec = new block_node(this, new JsArray<node>() { new arg_node(this, loopVar) }, branch, false);
			return new fcall_node(this, intern("for_loop"), new JsArray<node>() { arg }, exec);
		}

		public node controls_forEach(ControlsForEachBlock block)
		{
			// For each loop.
			var lv = local_switch();

			var loopVar = local_add_f(block.getFieldValue("VAR"));
			var argument0 = valueToCode(block, "LIST");
			if (argument0 == null) argument0 = new array_node(this, new JsArray<node>());
			var branch = statementToCode(block, "DO");

			local_resume(lv);

			var exec = new block_node(this, new JsArray<node>() { new arg_node(this, loopVar) }, branch, false);
			return new call_node(this, argument0, intern("each"), new JsArray<node>(), exec);
		}

		public node controls_flow_statements(ControlsFlowStatementsBlock block)
		{
			// Flow statements: continue, break.
			switch (block.getFieldValue("FLOW")) {
			case "BREAK":
				return new break_node(this, null);
			case "CONTINUE":
				return new next_node(this, null);
			}
			throw new Exception("Unknown flow statement.");
		}
	}
}
