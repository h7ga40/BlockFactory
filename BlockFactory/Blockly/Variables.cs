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
 * @fileoverview Utility functions for handling variables.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Variables
	{
		/// <summary>
		/// Category to separate variable names from procedures and generated functions.
		/// </summary>
		public string NAME_TYPE = "VARIABLE";

		/// <summary>
		/// Common HSV hue for all blocks in this category.
		/// </summary>
		public const int HUE = 330;

		/// <summary>
		/// Find all user-created variables that are in use in the workspace.
		/// For use by generators.
		/// </summary>
		/// <param name="root">Root block or workspace.</param>
		/// <returns>Array of variable names.</returns>
		public string[] allUsedVariables(Union<Block, Workspace> root)
		{
			Block[] blocks;
			if (root.Is<Block>()) {
				// Root is Block.
				blocks = root.As<Block>().getDescendants();
			}
			else if (root.Is<Workspace>()) {
				// Root is Workspace.
				blocks = root.As<Workspace>().getAllBlocks();
			}
			else {
				throw new Exception("Not Block or Workspace: " + root);
			}
			var variableHash = new Dictionary<string, string>();
			// Iterate through every block and add each variable to the hash.
			for (var x = 0; x < blocks.Length; x++) {
				var blockVariables = blocks[x].getVars();
				if (blockVariables != null) {
					for (var y = 0; y < blockVariables.Length; y++) {
						var varName = blockVariables[y];
						// Variable name may be null if the block is only half-built.
						if (varName != null) {
							variableHash[varName.ToLower()] = varName;
						}
					}
				}
			}
			// Flatten the hash into a list.
			var variableList = new JsArray<string>();
			foreach (var name in variableHash.Keys) {
				variableList.Push(variableHash[name]);
			}
			return variableList;
		}

		/// <summary>
		/// Find all variables that the user has created through the workspace or
		/// toolbox.  For use by generators.
		/// </summary>
		/// <param name="root">The workspace to inspect.</param>
		/// <returns>Array of variable names.</returns>
		public string[] allVariables(Workspace root)
		{
			return root.variableList;
		}

		/// <summary>
		/// Construct the blocks required by the flyout for the variable category.
		/// </summary>
		/// <param name="workspace">The workspace contianing variables.</param>
		/// <returns>Array of XML block elements.</returns>
		public static JsArray<Node> flyoutCategory(Workspace workspace)
		{
			var variableList = workspace.variableList;
			Array.Sort(variableList, CaseInsensitiveCompare.caseInsensitiveCompare);

			var xmlList = new JsArray<Node>();
			var button = goog.dom.createDom("button");
			button.SetAttribute("text", Msg.NEW_VARIABLE);
			button.SetAttribute("callbackKey", "CREATE_VARIABLE");

			Core.registerButtonCallback("CREATE_VARIABLE", (btn) => {
				createVariable(btn.getTargetWorkspace());
			});

			xmlList.Push(button);

			if (variableList.Length > 0) {
				if (Core.Blocks.ContainsKey("variables_set")) {
					// <block type="variables_set" gap="20">
					//   <field name="VAR">item</field>
					// </block>
					var block = goog.dom.createDom("block");
					block.SetAttribute("type", "variables_set");
					if (Core.Blocks.ContainsKey("math_change")) {
						block.SetAttribute("gap", "8");
					}
					else {
						block.SetAttribute("gap", "24");
					}
					var field = goog.dom.createDom("field", null, variableList[0]);
					field.SetAttribute("name", "VAR");
					block.AppendChild(field);
					xmlList.Push(block);
				}
				if (Core.Blocks.ContainsKey("math_change")) {
					// <block type="math_change">
					//   <value name="DELTA">
					//     <shadow type="math_number">
					//       <field name="NUM">1</field>
					//     </shadow>
					//   </value>
					// </block>
					var block = goog.dom.createDom("block");
					block.SetAttribute("type", "math_change");
					if (Core.Blocks.ContainsKey("variables_get")) {
						block.SetAttribute("gap", "20");
					}
					var value = goog.dom.createDom("value");
					value.SetAttribute("name", "DELTA");
					block.AppendChild(value);

					var field = goog.dom.createDom("field", null, variableList[0]);
					field.SetAttribute("name", "VAR");
					block.AppendChild(field);

					var shadowBlock = goog.dom.createDom("shadow");
					shadowBlock.SetAttribute("type", "math_number");
					value.AppendChild(shadowBlock);

					var numberField = goog.dom.createDom("field", null, "1");
					numberField.SetAttribute("name", "NUM");
					shadowBlock.AppendChild(numberField);

					xmlList.Push(block);
				}

				for (var i = 0; i < variableList.Length; i++) {
					if (Core.Blocks.ContainsKey("variables_get")) {
						// <block type="variables_get" gap="8">
						//   <field name="VAR">item</field>
						// </block>
						var block = goog.dom.createDom("block");
						block.SetAttribute("type", "variables_get");
						if (Core.Blocks.ContainsKey("variables_set")) {
							block.SetAttribute("gap", "8");
						}
						var field = goog.dom.createDom("field", null, variableList[i]);
						field.SetAttribute("name", "VAR");
						block.AppendChild(field);
						xmlList.Push(block);
					}
				}
			}
			return xmlList;
		}

		/// <summary>
		/// Return a new variable name that is not yet being used. This will try to
		/// generate single letter variable names in the range 'i' to 'z' to start with.
		/// If no unique name is located it will try 'i' to 'z', 'a' to 'h',
		/// then 'i2' to 'z2' etc.  Skip 'l'.
		/// </summary>
		/// <param name="workspace">The workspace to be unique in.</param>
		/// <returns>New variable name.</returns>
		public string generateUniqueName(Workspace workspace)
		{
			var variableList = workspace.variableList;
			var newName = "";
			if (variableList.Length != 0) {
				var nameSuffix = 1;
				var letters = "ijkmnopqrstuvwxyzabcdefgh";  // No 'l'.
				var letterIndex = 0;
				var potName = letters.CharAt(letterIndex);
				while (String.IsNullOrEmpty(newName)) {
					var inUse = false;
					for (var i = 0; i < variableList.Length; i++) {
						if (variableList[i].ToLower() == potName) {
							// This potential name is already used.
							inUse = true;
							break;
						}
					}
					if (inUse) {
						// Try the next potential name.
						letterIndex++;
						if (letterIndex == letters.Length) {
							// Reached the end of the character sequence so back to 'i'.
							// a new suffix.
							letterIndex = 0;
							nameSuffix++;
						}
						potName = letters.CharAt(letterIndex);
						if (nameSuffix > 1) {
							potName += nameSuffix;
						}
					}
					else {
						// We can use the current potential name.
						newName = potName;
					}
				}
			}
			else {
				newName = "i";
			}
			return newName;
		}

		/// <summary>
		/// Create a new variable on the given workspace.
		/// </summary>
		/// <param name="workspace">The workspace on which to create the variable.</param>
		/// <param name="opt_callback">A callback. It will
		/// return an acceptable new variable name, or null if change is to be
		/// aborted (cancel button), or undefined if an existing variable was chosen.</param>
		public static void createVariable(Workspace workspace, Func<string, object> opt_callback = null)
		{
			Action<string> promptAndCheckWithAlert = null;
			promptAndCheckWithAlert = new Action<string>((defaultName) => {
				promptName(Msg.NEW_VARIABLE_TITLE, defaultName,
					(text) => {
						if (text != null) {
							if (workspace.variableIndexOf(text) != -1) {
								Core.alert(Msg.VARIABLE_ALREADY_EXISTS.Replace("%1",
									text.ToLower()),
									() => {
										promptAndCheckWithAlert(text);  // Recurse
									});
							}
							else {
								workspace.createVariable(text);
								if (opt_callback != null) {
									opt_callback(text);
								}
							}
						}
						else {
							// User canceled prompt without a value.
							if (opt_callback != null) {
								opt_callback(null);
							}
						}
					});
			});
			promptAndCheckWithAlert("");
		}

		/// <summary>
		/// Prompt the user for a new variable name.
		/// </summary>
		/// <param name="promptText">The string of the prompt.</param>
		/// <param name="defaultText">The default value to show in the prompt's field.</param>
		/// <param name="callback">A callback. It will return the new
		/// variable name, or null if the user picked something illegal.</param>
		public static void promptName(string promptText, string defaultText, Action<string> callback)
		{
			Core.prompt(promptText, defaultText, new Action<string>((newVar) => {
				// Merge runs of whitespace.  Strip leading and trailing whitespace.
				// Beyond this, all names are legal.
				if (newVar != null) {
					newVar = newVar.Replace(new Regex(@"[\s\xa0]+", RegexOptions.Multiline), " ").Replace(new Regex(@"^ | $", RegexOptions.Multiline), "");
					if (newVar == Msg.RENAME_VARIABLE ||
						newVar == Msg.NEW_VARIABLE) {
						// Ok, not ALL names are legal...
						newVar = null;
					}
				}
				callback(newVar);
			}));
		}
	}
}
