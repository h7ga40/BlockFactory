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
		public node procedures_defreturn(ProceduresDefBlock block)
		{
			var lv = local_switch();

			var args = new JsArray<arg_node>();
			for (var x = 0; x < block.arguments_.Length; x++) {
				args.Push(new arg_node(this, local_add_f(block.arguments_[x])));
			}
			var funcName = intern(block.getFieldValue("NAME"));
			var branch = statementToCode(block, "STACK");
			var returnValue = valueToCode(block, "RETURN");
			if (returnValue == null) {
				returnValue = new return_node(this, returnValue);
			}
			((begin_node)branch).progs.Push(returnValue);
			var code = new def_node(this, funcName, args, branch);

			local_resume(lv);

			return code;
		}

		// Defining a procedure without a return value uses the same generator as
		// a procedure with a return value.
		public node procedures_defnoreturn(ProceduresDefnoreturnBlock block)
		{
			return procedures_defreturn(block);
		}

		public node procedures_callreturn(ProceduresCallreturnBlock block)
		{
			// Call a procedure with a return value.
			var funcName = intern(block.getFieldValue("NAME"));
			var args = new JsArray<node>();
			for (var x = 0; x < block.arguments_.Length; x++) {
				args.Push(valueToCode(block, "ARG" + x));
				if (args[x] == null)
					args[x] = new nil_node(this);
			}
			return new fcall_node(this, funcName, args, null);
		}

		public node procedures_callnoreturn(ProceduresCallnoreturnBlock block)
		{
			// Call a procedure with no return value.
			var funcName = intern(block.getFieldValue("NAME"));
			var args = new JsArray<node>();
			for (var x = 0; x < block.arguments_.Length; x++) {
				args.Push(valueToCode(block, "ARG" + x));
				if (args[x] == null)
					args[x] = new nil_node(this);
			}
			return new fcall_node(this, funcName, args, null);
		}

		public node procedures_ifreturn(ProceduresIfreturnBlock block)
		{
			// Conditionally return value from a procedure.
			var condition = valueToCode(block, "CONDITION");
			if (condition == null) condition = new false_node(this);
			node code = null;
			if (block.hasReturnValue_) {
				var value = valueToCode(block, "VALUE");
				if (value == null) value = new nil_node(this);
				code = new return_node(this, value);
			}
			else {
				code = new return_node(this, null);
			}
			return new if_node(this, condition, code, null, false);
		}
	}
}
