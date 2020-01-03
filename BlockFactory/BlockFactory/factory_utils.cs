/*
 * @license
 * Blockly Demos: Block Factory
 *
 * Copyright 2016 Google Inc.
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

/*
 * @fileoverview FactoryUtils is a namespace that holds block starter code
 * generation functions shared by the Block Factory, Workspace Factory, and
 * Exporter applications within Blockly Factory. Holds functions to generate
 * block definitions and generator stubs and to create and download files.
 *
 * @author fraser@google.com (Neil Fraser), quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	/*
	 * Namespace for FactoryUtils.
	 */
	public static class FactoryUtils
	{


		/// <summary>
		/// Get block definition code for the current block.
		/// </summary>
		/// <param  name="blockType"></param> Type of block.
		/// <param name="rootBlock"> RootBlock from main workspace in which
		/// user uses Block Factory Blocks to create a custom block.</param>
		/// <param  name="format">"JSON" or "JavaScript".</param>
		/// <param name="workspace"> Where the root block lives.</param>
		/// <returns>Block definition.</returns>
		public static string getBlockDefinition(string blockType, Blockly.Block rootBlock, string format, Blockly.Workspace workspace)
		{
			string code = null;
			blockType = blockType.Replace(new Regex(@"\W", RegexOptions.Multiline), "_").Replace(new Regex(@"^(\d)"), "_\\1");
			switch (format) {
			case "JSON":
				code = FactoryUtils.formatJson_(blockType, rootBlock);
				break;
			case "JavaScript":
				code = FactoryUtils.formatJavaScript_(blockType, rootBlock, workspace);
				break;
			}
			return code;
		}

		/// <summary>
		/// Get the generator code for a given block.
		/// </summary>
		/// <param name="block"> Rendered block in preview workspace.</param>
		/// <param name="generatorLanguage">"JavaScript", "Python", "PHP", "Lua",
		/// "Dart".</param>
		/// <returns>Generator code for multiple blocks.</returns>
		public static string getGeneratorStub(Blockly.Block block, string generatorLanguage)
		{
			var makeVar = new Func<string, string, string>((root, name) => {
				name = name.ToLowerCase().Replace(new Regex(@"\W", RegexOptions.Multiline), "_");
				return "  var " + root + "_" + name;
			});
			// The makevar function lives in the original update generator.
			var language = generatorLanguage;
			var code = new JsArray<string>();
			code.Push("Blockly." + language + "[\"" + block.type +
					  "\"] = (block) => {");

			// Generate getters for any fields or inputs.
			for (var i = 0; i < block.inputList.Length; i++) {
				string name;
				var input = block.inputList[i];
				for (var j = 0; j < input.fieldRow.Length; j++) {
					var field = input.fieldRow[j];
					name = field.name;
					if (String.IsNullOrEmpty(name)) {
						continue;
					}
					if (typeof(Blockly.FieldVariable).IsInstanceOfType(field)) {
						// Subclass of Blockly.FieldDropdown, must test first.
						code.Push(makeVar("variable", name) +
							" = Blockly." + language +
							".variableDB_.getName(block.getFieldValue(\"" + name +
							"\"), Blockly.Variables.NAME_TYPE);");
					}
					else if (typeof(Blockly.FieldAngle).IsInstanceOfType(field)) {
						// Subclass of Blockly.FieldTextInput, must test first.
						code.Push(makeVar("angle", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
					else if (typeof(Blockly.FieldDate).IsInstanceOfType(field)) {
						// Blockly.FieldDate may not be compiled into Blockly.
						code.Push(makeVar("date", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
					else if (typeof(Blockly.FieldColour).IsInstanceOfType(field)) {
						code.Push(makeVar("colour", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
					else if (typeof(Blockly.FieldCheckbox).IsInstanceOfType(field)) {
						code.Push(makeVar("checkbox", name) +
							" = block.getFieldValue(\"" + name + "\") == \"TRUE\";");
					}
					else if (typeof(Blockly.FieldDropdown).IsInstanceOfType(field)) {
						code.Push(makeVar("dropdown", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
					else if (typeof(Blockly.FieldNumber).IsInstanceOfType(field)) {
						code.Push(makeVar("number", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
					else if (typeof(Blockly.FieldTextInput).IsInstanceOfType(field)) {
						code.Push(makeVar("text", name) +
							" = block.getFieldValue(\"" + name + "\");");
					}
				}
				name = input.name;
				if (!String.IsNullOrEmpty(name)) {
					if (input.type == Blockly.Core.INPUT_VALUE) {
						code.Push(makeVar("value", name) +
							" = Blockly." + language + ".valueToCode(block, \"" + name +
							"\", Blockly." + language + ".ORDER_ATOMIC);");
					}
					else if (input.type == Blockly.Core.NEXT_STATEMENT) {
						code.Push(makeVar("statements", name) +
							" = Blockly." + language + ".statementToCode(block, \"" +
							name + "\");");
					}
				}
			}
			// Most languages end lines with a semicolon.  Python does not.
			var lineEnd = new Dictionary<string, string>{
				{ "JavaScript", ";"},
				{"Python", ""},
				{"PHP", ";"},
				{"Dart", ";"}
			};
			code.Push("  // TODO: Assemble " + language + " into code variable.");
			if (block.outputConnection != null) {
				code.Push("  var code = \"...\";");
				code.Push("  // TODO: Change ORDER_NONE to the correct strength.");
				code.Push("  return [code, Blockly." + language + ".ORDER_NONE];");
			}
			else {
				code.Push("  var code = \"...\" + (lineEnd[language] || \"\") + \"\\n\";");
				code.Push("  return code;");
			}
			code.Push("};");

			return code.Join("\n");
		}

		/// <summary>
		/// Update the language code as JSON.
		/// </summary>
		/// <param name="blockType"> Name of block.</param>
		/// <param  name="rootBlock">Factory_base block.</param>
		/// <returns>Generanted language code.</returns>
		private static string formatJson_(string blockType, Blockly.Block rootBlock)
		{
			var JS = new Dictionary<string, object>();
			// Type is not used by Blockly, but may be used by a loader.
			JS["type"] = blockType;
			// Generate inputs.
			var message = new JsArray<string>();
			var args = new JsArray<object>();
			var contentsBlock = rootBlock.getInputTargetBlock("INPUTS");
			Blockly.Block lastInput = null;
			while (contentsBlock != null) {
				if (!contentsBlock.disabled && !contentsBlock.getInheritedDisabled()) {
					var fields = FactoryUtils.getFieldsJson_(
						contentsBlock.getInputTargetBlock("FIELDS"));
					for (var i = 0; i < fields.Length; i++) {
						if (fields[i] is string str) {
							message.Push(str.Replace(new Regex("%", RegexOptions.Multiline), "%%"));
						}
						else {
							args.Push(fields[i]);
							message.Push("%" + args.Length);
						}
					}

					var input = new Dictionary<string, object>();
					input.Add("type", contentsBlock.type);
					// Dummy inputs don't have names.  Other inputs do.
					if (contentsBlock.type != "input_dummy") {
						input["name"] = contentsBlock.getFieldValue("INPUTNAME");
					}
					var check = JSON.Parse(
						FactoryUtils.getOptTypesFrom(contentsBlock, "TYPE") ?? "null");
					if (check != null) {
						input["check"] = check;
					}
					var align = contentsBlock.getFieldValue("ALIGN");
					if (align != "LEFT") {
						input["align"] = align;
					}
					args.Push(input);
					message.Push("%" + args.Length);
					lastInput = contentsBlock;
				}
				contentsBlock = contentsBlock.nextConnection == null ? null :
					contentsBlock.nextConnection.targetBlock();
			}
			// Remove last input if dummy and not empty.
			if (lastInput != null && lastInput.type == "input_dummy") {
				var fields = lastInput.getInputTargetBlock("FIELDS");
				if (fields != null && FactoryUtils.getFieldsJson_(fields).Join("").Trim() != "") {
					var align = lastInput.getFieldValue("ALIGN");
					if (align != "LEFT") {
						JS["lastDummyAlign0"] = align;
					}
					args.Pop();
					message.Pop();
				}
			}
			JS["message0"] = message.Join(" ");
			if (args.Length > 0) {
				JS["args0"] = args;
			}
			// Generate inline/external switch.
			if (rootBlock.getFieldValue("INLINE") == "EXT") {
				JS["inputsInline"] = false;
			}
			else if (rootBlock.getFieldValue("INLINE") == "INT") {
				JS["inputsInline"] = true;
			}
			// Generate output, or next/previous connections.
			switch (rootBlock.getFieldValue("CONNECTIONS")) {
			case "LEFT":
				JS["output"] =
					JSON.Parse(
						FactoryUtils.getOptTypesFrom(rootBlock, "OUTPUTTYPE") ?? "null");
				break;
			case "BOTH":
				JS["previousStatement"] =
					JSON.Parse(
						FactoryUtils.getOptTypesFrom(rootBlock, "TOPTYPE") ?? "null");
				JS["nextStatement"] =
					JSON.Parse(
						FactoryUtils.getOptTypesFrom(rootBlock, "BOTTOMTYPE") ?? "null");
				break;
			case "TOP":
				JS["previousStatement"] =
					JSON.Parse(
						FactoryUtils.getOptTypesFrom(rootBlock, "TOPTYPE") ?? "null");
				break;
			case "BOTTOM":
				JS["nextStatement"] =
					JSON.Parse(
						FactoryUtils.getOptTypesFrom(rootBlock, "BOTTOMTYPE") ?? "null");
				break;
			}
			// Generate colour.
			var colourBlock = rootBlock.getInputTargetBlock("COLOUR");
			if (colourBlock != null && !colourBlock.disabled) {
				var hue = Script.ParseFloat(colourBlock.getFieldValue("HUE"));
				JS["colour"] = hue;
			}
			JS["tooltip"] = "";
			JS["helpUrl"] = "http://www.example.com/";
			return JSON.Stringify(JS, null, "  ");
		}

		/// <summary>
		/// Update the language code as JavaScript.
		/// </summary>
		/// <param name="blockType"> Name of block.</param>
		/// <param name="rootBlock"> Factory_base block.</param>
		/// <param name="workspace"></param> Where the root block lives.
		/// <returns>Generated language code.</returns>
		private static string formatJavaScript_(string blockType, Blockly.Block rootBlock, Blockly.Workspace workspace)
		{
			var code = new JsArray<string>();
			code.Push("Blockly.Core.Blocks[\"" + blockType + "\"] = {");
			code.Push("  init: () => {");
			// Generate inputs.
			var TYPES = new Dictionary<string, string>() { { "input_value", "appendValueInput"},
				{ "input_statement", "appendStatementInput" },
				{ "input_dummy", "appendDummyInput" } };
			var contentsBlock = rootBlock.getInputTargetBlock("INPUTS");
			while (contentsBlock != null) {
				if (!contentsBlock.disabled && !contentsBlock.getInheritedDisabled()) {
					var name = "";
					// Dummy inputs don't have names.  Other inputs do.
					if (contentsBlock.type != "input_dummy") {
						name =
							FactoryUtils.escapeString(contentsBlock.getFieldValue("INPUTNAME"));
					}
					code.Push("    this." + TYPES[contentsBlock.type] + "(" + name + ")");
					var check = FactoryUtils.getOptTypesFrom(contentsBlock, "TYPE");
					if (!String.IsNullOrEmpty(check)) {
						code.Push("        .setCheck(" + check + ")");
					}
					var align = contentsBlock.getFieldValue("ALIGN");
					if (align != "LEFT") {
						code.Push("        .setAlign(Blockly.ALIGN_" + align + ")");
					}
					var fields = FactoryUtils.getFieldsJs_(
						contentsBlock.getInputTargetBlock("FIELDS"));
					for (var i = 0; i < fields.Length; i++) {
						code.Push("        .appendField(" + fields[i] + ")");
					}
					// Add semicolon to last line to finish the statement.
					code[code.Length - 1] += ";";
				}
				contentsBlock = contentsBlock.nextConnection == null ? null :
					contentsBlock.nextConnection.targetBlock();
			}
			// Generate inline/external switch.
			if (rootBlock.getFieldValue("INLINE") == "EXT") {
				code.Push("    this.setInputsInline(false);");
			}
			else if (rootBlock.getFieldValue("INLINE") == "INT") {
				code.Push("    this.setInputsInline(true);");
			}
			// Generate output, or next/previous connections.
			switch (rootBlock.getFieldValue("CONNECTIONS")) {
			case "LEFT":
				code.Push(FactoryUtils.connectionLineJs_("setOutput", "OUTPUTTYPE", workspace));
				break;
			case "BOTH":
				code.Push(
					FactoryUtils.connectionLineJs_("setPreviousStatement", "TOPTYPE", workspace));
				code.Push(
					FactoryUtils.connectionLineJs_("setNextStatement", "BOTTOMTYPE", workspace));
				break;
			case "TOP":
				code.Push(
					FactoryUtils.connectionLineJs_("setPreviousStatement", "TOPTYPE", workspace));
				break;
			case "BOTTOM":
				code.Push(
					FactoryUtils.connectionLineJs_("setNextStatement", "BOTTOMTYPE", workspace));
				break;
			}
			// Generate colour.
			var colourBlock = rootBlock.getInputTargetBlock("COLOUR");
			if (colourBlock != null && !colourBlock.disabled) {
				var hue = Script.ParseFloat(colourBlock.getFieldValue("HUE"));
				if (!Double.IsNaN(hue)) {
					code.Push("    this.setColour(" + hue + ");");
				}
			}
			code.Push("    this.setTooltip(\"\");");
			code.Push("    this.setHelpUrl(\"http://www.example.com/\");");
			code.Push("  }");
			code.Push("};");
			return code.Join("\n");
		}

		/// <summary>
		/// Create JS code required to create a top, bottom, or value connection.
		/// </summary>
		/// <param name="functionName"> JavaScript function name.</param>
		/// <param name="typeName"> Name of type input.</param>
		/// <param  name="workspace"></param> Where the root block lives.
		/// <returns>Line of JavaScript code to create connection.</returns> 
		private static string connectionLineJs_(string functionName, string typeName, Blockly.Workspace workspace)
		{
			var type = FactoryUtils.getOptTypesFrom(
				FactoryUtils.getRootBlock(workspace), typeName);
			if (!String.IsNullOrEmpty(type)) {
				type = ", " + type;
			}
			else {
				type = "";
			}
			return "    this." + functionName + "(true" + type + ");";
		}

		/// <summary>
		/// Returns field strings and any config.
		/// </summary>
		/// <param  name="block">Input block.</param>
		/// <returns>Field strings.</returns>
		private static JsArray<string> getFieldsJs_(Blockly.Block block)
		{
			var fields = new JsArray<string>();
			while (block != null) {
				if (!block.disabled && !block.getInheritedDisabled()) {
					switch (block.type) {
					case "field_static":
						// Result: "hello"
						fields.Push(FactoryUtils.escapeString(block.getFieldValue("TEXT")));
						break;
					case "field_input":
						// Result: new Blockly.FieldTextInput("Hello"), "GREET"
						fields.Push("new Blockly.FieldTextInput(" +
							FactoryUtils.escapeString(block.getFieldValue("TEXT")) + "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_number":
						// Result: new Blockly.FieldNumber(10, 0, 100, 1), "NUMBER"
						var args = new JsArray<double> {
						  Script.ParseFloat(block.getFieldValue("VALUE")),
						  Script.ParseFloat(block.getFieldValue("MIN")),
						  Script.ParseFloat(block.getFieldValue("MAX")),
						  Script.ParseFloat(block.getFieldValue("PRECISION"))
						};
						// Remove any trailing arguments that aren't needed.
						if (args[3] == 0) {
							args.Pop();
							if (args[2] == Double.PositiveInfinity) {
								args.Pop();
								if (args[1] == Double.NegativeInfinity) {
									args.Pop();
								}
							}
						}
						fields.Push("new Blockly.FieldNumber(" + args.Join(", ") + "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_angle":
						// Result: new Blockly.FieldAngle(90), "ANGLE"
						fields.Push("new Blockly.FieldAngle(" +
							Script.ParseFloat(block.getFieldValue("ANGLE")) + "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_checkbox":
						// Result: new Blockly.FieldCheckbox("TRUE"), "CHECK"
						fields.Push("new Blockly.FieldCheckbox(" +
							FactoryUtils.escapeString(block.getFieldValue("CHECKED")) +
							 "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_colour":
						// Result: new Blockly.FieldColour("#ff0000"), "COLOUR"
						fields.Push("new Blockly.FieldColour(" +
							FactoryUtils.escapeString(block.getFieldValue("COLOUR")) +
							"), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_date":
						// Result: new Blockly.FieldDate("2015-02-04"), "DATE"
						fields.Push("new Blockly.FieldDate(" +
							FactoryUtils.escapeString(block.getFieldValue("DATE")) + "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_variable":
						// Result: new Blockly.FieldVariable("item"), "VAR"
						var varname
							= FactoryUtils.escapeString(block.getFieldValue("TEXT") ?? null);
						fields.Push("new Blockly.FieldVariable(" + varname + "), " +
							FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						break;
					case "field_dropdown":
						// Result:
						// new Blockly.FieldDropdown([["yes", "1"], ["no", "0"]]), "TOGGLE"
						var options = new JsArray<string>();
						for (var i = 0; i < ((FieldDropdown)block).optionCount_; i++) {
							options[i] = "[" +
								FactoryUtils.escapeString(block.getFieldValue("USER" + i)) +
								", " +
								FactoryUtils.escapeString(block.getFieldValue("CPU" + i)) + "]";
						}
						if (options.Length > 0) {
							fields.Push("new Blockly.FieldDropdown([" +
								options.Join(", ") + "]), " +
								FactoryUtils.escapeString(block.getFieldValue("FIELDNAME")));
						}
						break;
					case "field_image":
						// Result: new Blockly.FieldImage("http://...", 80, 60)
						var src = FactoryUtils.escapeString(block.getFieldValue("SRC"));
						var width = Convert.ToDouble(block.getFieldValue("WIDTH"));
						var height = Convert.ToDouble(block.getFieldValue("HEIGHT"));
						var alt = FactoryUtils.escapeString(block.getFieldValue("ALT"));
						fields.Push("new Blockly.FieldImage(" +
							src + ", " + width + ", " + height + ", " + alt + ")");
						break;
					}
				}
				block = block.nextConnection == null ? null : block.nextConnection.targetBlock();
			}
			return fields;
		}

		/// <summary>
		/// Returns field strings and any config.
		/// </summary>
		/// <param  name="block"></param> Input block.
		/// <returns></returns> Array of static text and field configs.
		private static JsArray<object> getFieldsJson_(Blockly.Block block)
		{
			var fields = new JsArray<object>();
			while (block != null) {
				if (!block.disabled && !block.getInheritedDisabled()) {
					switch (block.type) {
					case "field_static":
						// Result: "hello"
						fields.Push(block.getFieldValue("TEXT"));
						break;
					case "field_input":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							text = block.getFieldValue("TEXT")
						});
						break;
					case "field_number":
						var obj = new Dictionary<string, object>();
						obj["type"] = block.type;
						obj["name"] = block.getFieldValue("FIELDNAME");
						obj["value"] = Script.ParseFloat(block.getFieldValue("VALUE"));
						var min = Script.ParseFloat(block.getFieldValue("MIN"));
						if (min > Double.NegativeInfinity) {
							obj["min"] = min;
						}
						var max = Script.ParseFloat(block.getFieldValue("MAX"));
						if (max < Double.PositiveInfinity) {
							obj["max"] = max;
						}
						var precision = block.getFieldValue("PRECISION");
						if (!String.IsNullOrEmpty(precision)) {
							obj["precision"] = Script.ParseFloat(precision);
						}
						fields.Push(obj);
						break;
					case "field_angle":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							angle = Convert.ToDouble(block.getFieldValue("ANGLE"))
						});
						break;
					case "field_checkbox":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							@checked = block.getFieldValue("CHECKED") == "TRUE"
						});
						break;
					case "field_colour":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							colour = block.getFieldValue("COLOUR")
						});
						break;
					case "field_date":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							date = block.getFieldValue("DATE")
						});
						break;
					case "field_variable":
						fields.Push(new {
							type = block.type,
							name = block.getFieldValue("FIELDNAME"),
							variable = block.getFieldValue("TEXT") ?? null
						});
						break;
					case "field_dropdown":
						var count = ((FieldDropdown)block).optionCount_;
						var options = new JsArray<DropdownItemInfo>(count);
						for (var i = 0; i < count; i++) {
							options[i] = new DropdownItemInfo(
								block.getFieldValue("USER" + i),
								block.getFieldValue("CPU" + i)
							);
						}
						if (options.Length >= 0) {
							fields.Push(new {
								type = block.type,
								name = block.getFieldValue("FIELDNAME"),
								options = options
							});
						}
						break;
					case "field_image":
						fields.Push(new {
							type = block.type,
							src = block.getFieldValue("SRC"),
							width = Convert.ToDouble(block.getFieldValue("WIDTH")),
							height = Convert.ToDouble(block.getFieldValue("HEIGHT")),
							alt = block.getFieldValue("ALT")
						});
						break;
					}
				}
				block = block.nextConnection == null ? null : block.nextConnection.targetBlock();
			}
			return fields;
		}

		/// <summary>
		/// Fetch the type(s) defined in the given input.
		/// Format as a string for appending to the generated code.
		/// </summary>
		/// <param  name="block">Block with input.</param>
		/// <param name="name"> Name of the input.</param>
		/// <returns>String defining the types.</returns>
		public static string getOptTypesFrom(Blockly.Block block, string name)
		{
			var types = FactoryUtils.getTypesFrom_(block, name);
			if (types.Length == 0) {
				return null;
			}
			else if (types.IndexOf("null") != -1) {
				return "null";
			}
			else if (types.Length == 1) {
				return types[0];
			}
			else {
				return "[" + types.Join(", ") + "]";
			}
		}


		/// <summary>
		/// Fetch the type(s) defined in the given input.
		/// </summary>
		/// <param name="block"> Block with input.</param>
		/// <param  name="name">Name of the input.</param>
		/// <returns>List of types.</returns>
		private static JsArray<string> getTypesFrom_(Blockly.Block block, string name)
		{
			var typeBlock = block.getInputTargetBlock(name);
			JsArray<string> types;
			if (typeBlock == null || typeBlock.disabled) {
				types = new JsArray<string>();
			}
			else if (typeBlock.type == "type_other") {
				types = new JsArray<string> { FactoryUtils.escapeString(typeBlock.getFieldValue("TYPE")) };
			}
			else if (typeBlock.type == "type_group") {
				types = new JsArray<string>();
				for (var n = 0; n < ((TypeGroup)typeBlock).typeCount_; n++) {
					types = types.Concat(FactoryUtils.getTypesFrom_(typeBlock, "TYPE" + n));
				}
				// Remove duplicates.
				var hash = new Dictionary<string, object>();
				for (var n = types.Length - 1; n >= 0; n--) {
					if (hash.ContainsKey(types[n])) {
						types.Splice(n, 1);
					}
					hash[types[n]] = true;
				}
			}
			else {
				var fi = typeBlock.GetType().GetField("valueType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
				types = new JsArray<string> { FactoryUtils.escapeString((string)fi.GetValue(null)) };
			}
			return types;
		}

		/// <summary>
		/// Escape a string.
		/// </summary>
		/// <param name="@string"> String to escape.</param>
		/// <returns>Escaped string surrouned by quotes.</returns>
		public static string escapeString(string @string)
		{
			return JSON.Stringify(@string);
		}

		/// <summary>
		/// Return the uneditable container block that everything else attaches to in
		/// given workspace.
		/// </summary>
		/// <param name="workspace"> Where the root block lives.</param>
		/// <returns>Root block.</returns>
		public static Blockly.Block getRootBlock(Blockly.Workspace workspace)
		{
			var blocks = workspace.getTopBlocks(false);
			for (var i = 0; i < blocks.Length; i++) {
				var block = blocks[i];
				if (block.type == "factory_base") {
					return block;
				}
			}
			return null;
		}

		// TODO(quachtina96): Move hide, show, makeInvisible, and makeVisible to a new
		// AppView namespace.

		/// <summary>
		/// Hides element so that it's invisible and doesn't take up space.
		/// </summary>
		/// <param name="elementID"> ID of element to hide.</param>
		public static void hide(string elementID)
		{
			((HTMLElement)Document.GetElementById(elementID)).Style.Display = Display.None;
		}

		/// <summary>
		/// Un-hides an element.
		/// </summary>
		/// <param name="elementID"> ID of element to hide.</param>
		public static void show(string elementID)
		{
			((HTMLElement)Document.GetElementById(elementID)).Style.Display = Display.Block;
		}

		/// <summary>
		/// Hides element so that it's invisible but still takes up space.
		/// </summary>
		/// <param name="elementID"> ID of element to hide.</param>
		public static void makeInvisible(string elementID)
		{
			((SVGElement)Document.GetElementById(elementID)).style.Visibility = Visibility.Hidden;
		}

		/// <summary>
		/// Makes element visible.
		/// </summary>
		/// <param name="elementID"> ID of element to hide.</param>
		public static void makeVisible(string elementID)
		{
			((SVGElement)Document.GetElementById(elementID)).style.Visibility = Visibility.Visible;
		}

		/// <summary>
		/// Create a file with the given attributes and download it.
		/// </summary>
		/// <param name="contents">The contents of the file.</param>
		/// <param name="filename">The name of the file to save to.</param>
		/// <param name="fileType">The type of the file to save.</param>
		public static void createAndDownloadFile(string contents, string filename, string fileType)
		{
			var data = new Blob(new string[] { contents }, new Dictionary<string, object> { { "type", "text/" + fileType } });
			var clickEvent = new MouseEvent("click", new Dictionary<string, object>{
				{ "view", Window.Instance },
				{ "bubbles", true },
				{ "cancelable", false }
			});

			var a = Document.CreateElement<HTMLAnchorElement>("a");
			a.Href = Window.URL.CreateObjectURL(data);
			a.Download = filename;
			a.TextContent = "Download file!";
			a.DispatchEvent(clickEvent);
		}

		/// <summary>
		/// Get Blockly Block by rendering pre-defined block in workspace.
		/// </summary>
		/// <param name="blockType"> Type of block that has already been defined.</param>
		/// <param name="workspace">Workspace on which to render</param>
		/// the block.
		/// <returns>The Blockly.Block of desired type.</returns>
		public static Blockly.Block getDefinedBlock(string blockType, Blockly.Workspace workspace)
		{
			workspace.clear();
			return workspace.newBlock(blockType);
		}

		/// <summary>
		/// Parses a block definition get the type of the block it defines.
		/// </summary>
		/// <param name="blockDef"> A single block definition.</param>
		/// <returns>Type of block defined by the given definition.</returns>
		public static string getBlockTypeFromJsDefinition(string blockDef)
		{
			var indexOfStartBracket = blockDef.IndexOf("[\"");
			var indexOfEndBracket = blockDef.IndexOf("\"]");
			if (indexOfStartBracket != -1 && indexOfEndBracket != -1) {
				return blockDef.Substring(indexOfStartBracket + 2, indexOfEndBracket);
			}
			else {
				throw new Exception("Could not parse block type out of JavaScript block " +
					"definition. Brackets normally enclosing block type not found.");
			}
		}

		/// <summary>
		/// Generates a category containing blocks of the specified block types.
		/// </summary>
		/// <param name="blocks">Blocks to include in the category.</param>
		/// <param name="categoryName"> Name to use for the generated category.</param>
		/// <returns>Category XML containing the given block types.</returns>
		public static Element generateCategoryXml(JsArray<Blockly.Block> blocks, string categoryName)
		{
			// Create category DOM element.
			var categoryElement = goog.dom.createDom("category");
			categoryElement.SetAttribute("name", categoryName);

			// For each block, add block element to category.
			for (var i = 0; i < blocks.Length; i++) {
				var block = blocks[i];
				// Get preview block XML.
				var blockXml = Blockly.Xml.blockToDom(block);
				blockXml.RemoveAttribute("id");

				// Add block to category and category to XML.
				categoryElement.AppendChild(blockXml);
			}
			return categoryElement;
		}

		/// <summary>
		/// Parses a string containing JavaScript block definition(s) to create an array
		/// in which each element is a single block definition.
		/// </summary>
		/// <param name="blockDefsString"> JavaScript block definition(s).</param>
		/// <returns>Array of block definitions.</returns>
		public static JsArray<string> parseJsBlockDefinitions(string blockDefsString)
		{
			var blockDefArray = new JsArray<string>();
			var defStart = blockDefsString.IndexOf("Blockly.Core.Blocks");

			while (blockDefsString.IndexOf("Blockly.Core.Blocks", defStart) != -1) {
				var nextStart = blockDefsString.IndexOf("Blockly.Core.Blocks", defStart + 1);
				if (nextStart == -1) {
					// This is the last block definition.
					nextStart = blockDefsString.Length;
				}
				var blockDef = blockDefsString.Substring(defStart, nextStart);
				blockDefArray.Push(blockDef);
				defStart = nextStart;
			}
			return blockDefArray;
		}

		/// <summary>
		/// Parses a string containing JSON block definition(s) to create an array
		/// in which each element is a single block definition. Expected input is
		/// one or more block definitions in the form of concatenated, stringified
		/// JSON objects.
		/// </summary>
		/// <param  name="blockDefsString">String containing JSON block
		/// definition(s).</param>
		/// <returns>Array of block definitions.</returns>
		public static JsArray<string> parseJsonBlockDefinitions(string blockDefsString)
		{
			var blockDefArray = new JsArray<string>();
			var unbalancedBracketCount = 0;
			var defStart = 0;
			// Iterate through the blockDefs string. Keep track of whether brackets
			// are balanced.
			for (var i = 0; i < blockDefsString.Length; i++) {
				var currentChar = blockDefsString[i];
				if (currentChar == '{') {
					unbalancedBracketCount++;
				}
				else if (currentChar == '}') {
					unbalancedBracketCount--;
					if (unbalancedBracketCount == 0 && i > 0) {
						// The brackets are balanced. We"ve got a complete block defintion.
						var blockDef = blockDefsString.Substring(defStart, i + 1);
						blockDefArray.Push(blockDef);
						defStart = i + 1;
					}
				}
			}
			return blockDefArray;
		}

		/// <summary>
		/// Define blocks from imported block definitions.
		/// </summary>
		/// <param  name="blockDefsString"></param> Block definition(s).
		/// <param name="format"> Block definition format ("JSON" or "JavaScript").</param>
		/// <returns>Array of block types defined.</returns>
		public static JsArray<string> defineAndGetBlockTypes(string blockDefsString, string format)
		{
			var blockTypes = new JsArray<string>();

			// Define blocks and get block types.
			if (format == "JSON") {
				var blockDefArray = FactoryUtils.parseJsonBlockDefinitions(blockDefsString);

				// Populate array of blocktypes and define each block.
				for (var i = 0; i < blockDefArray.Length; i++) {
					var blockDef = blockDefArray[i];
					var json = (Dictionary<string, object>)JSON.Parse(blockDef);
					blockTypes.Push((string)json["type"]);

					// Define the block.
					Blockly.Core.Blocks.Add((string)json["type"], blockDef);
				}
			}
			else if (format == "JavaScript") {
				var blockDefArray = FactoryUtils.parseJsBlockDefinitions(blockDefsString);

				// Populate array of block types.
				for (var i = 0; i < blockDefArray.Length; i++) {
					var blockDef = blockDefArray[i];
					var blockType = FactoryUtils.getBlockTypeFromJsDefinition(blockDef);
					blockTypes.Push(blockType);
				}

				// Define all blocks.
				Script.Eval(blockDefsString);
			}

			return blockTypes;
		}

		/// <summary>
		/// Inject code into a pre tag, with syntax highlighting.
		/// Safe from HTML/script injection.
		/// </summary>
		/// <param name="code">Lines of code.</param>
		/// <param name="id"> ID of <pre> element to inject into.</param>
		public static void injectCode(string code, string id)
		{
			var pre = Document.GetElementById(id);
			pre.TextContent = code;
			code = pre.InnerHTML;
			code = Script.PrettyPrintOne(code, "js");
			pre.InnerHTML = code;
		}

		/// <summary>
		/// Returns whether or not two blocks are the same based on their XML. Expects
		/// XML with a single child node that is a factory_base block, the XML found on
		/// Block Factory's main workspace.
		/// </summary>
		/// <param name="blockXml1"> An XML element with a single child node that
		/// is a factory_base block.</param>
		/// <param name="blockXml2"></param> An XML element with a single child node that
		/// is a factory_base block.
		/// <returns>Whether or not two blocks are the same based on their XML.</returns>
		public static bool sameBlockXml(Element blockXml1, Element blockXml2)
		{
			// Each XML element should contain a single child element with a "block" tag
			if (blockXml1.TagName.ToLowerCase() != "xml" ||
				blockXml2.TagName.ToLowerCase() != "xml") {
				throw new Exception("Expected two XML elements, recieved elements with tag " +
					"names: " + blockXml1.TagName + " and " + blockXml2.TagName + ".");
			}

			// Compare the block elements directly. The XML tags may include other meta
			// information we want to igrore.
			var blockElement1 = (Element)blockXml1.GetElementsByTagName("block")[0];
			var blockElement2 = (Element)blockXml2.GetElementsByTagName("block")[0];

			if (blockElement1 == null || blockElement2 == null) {
				throw new Exception("Could not get find block element in XML.");
			}

			var blockXmlText1 = Blockly.Xml.domToText(blockElement1);
			var blockXmlText2 = Blockly.Xml.domToText(blockElement2);

			// Strip white space.
			blockXmlText1 = blockXmlText1.Replace(new Regex(@"\s+", RegexOptions.Multiline), "");
			blockXmlText2 = blockXmlText2.Replace(new Regex(@"\s+", RegexOptions.Multiline), "");

			// Return whether or not changes have been saved.
			return blockXmlText1 == blockXmlText2;
		}

		/// <summary>
		/// Checks if a block has a variable field. Blocks with variable fields cannot
		/// be shadow blocks.
		/// </summary>
		/// <param name="block"> The block to check if a variable field exists.</param>
		/// <returns>True if the block has a variable field, false otherwise.</returns>
		public static bool hasVariableField(Blockly.Block block)
		{
			if (block == null) {
				return false;
			}
			return block.getVars().Length > 0;
		}

		/// <summary>
		/// Checks if a block is a procedures block. If procedures block names are
		/// ever updated or expanded, this function should be updated as well (no
		/// other known markers for procedure blocks beyond name).
		/// </summary>
		/// <param name="block"> The block to check.</param>
		/// <returns>True if the block is a procedure block, false otherwise.</returns>
		public static bool isProcedureBlock(Blockly.Block block)
		{
			return block != null &&
				(block.type == "procedures_defnoreturn" ||
				block.type == "procedures_defreturn" ||
				block.type == "procedures_callnoreturn" ||
				block.type == "procedures_callreturn" ||
				block.type == "procedures_ifreturn");
		}

		/// <summary>
		/// Returns whether or not a modified block's changes has been saved to the
		/// Block Library.
		/// TODO(quachtina96): move into the Block Factory Controller once made.
		/// </summary>
		/// <param name="blockLibraryController"> Block Library
		/// Controller storing custom blocks.</param>
		/// <returns>True if all changes made to the block have been saved to
		/// the given Block Library.</returns>
		public static bool savedBlockChanges(BlockLibraryController blockLibraryController)
		{
			if (BlockFactory.isStarterBlock()) {
				return true;
			}
			var blockType = blockLibraryController.getCurrentBlockType();
			var currentXml = Blockly.Xml.workspaceToDom(BlockFactory.mainWorkspace);

			if (blockLibraryController.has(blockType)) {
				// Block is saved in block library.
				var savedXml = blockLibraryController.getBlockXml(blockType);
				return FactoryUtils.sameBlockXml(savedXml, currentXml);
			}
			return false;
		}
	}
}