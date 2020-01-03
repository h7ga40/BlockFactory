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
 * @fileoverview Variable input field.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;

namespace Blockly
{
	public class FieldVariable : FieldDropdown
	{
		/// <summary>
		/// Class for a variable's dropdown field.
		/// </summary>
		/// <param name="varname">The default name for the variable.  If null,
		/// a unique variable name will be generated.</param>
		/// <param name="opt_validator">A function that is executed when a new
		/// option is selected.  Its sole argument is the new option value.</param>
		public FieldVariable(string varname, Func<Field, string, object> opt_validator = null)
			: base(new JsArray<DropdownItemInfo>(), opt_validator)
		{
			setMenuGenerator_(dropdownCreate());
			this.setValue(varname ?? "");
		}

		/// <summary>
		/// Install this dropdown on a block.
		/// </summary>
		public override void init()
		{
			if (this.fieldGroup_ != null) {
				// Dropdown has already been initialized once.
				return;
			}
			base.init();
			if (this.getValue() == null) {
				// Variables without names get uniquely named for this workspace.
				var workspace =
					this.sourceBlock_.isInFlyout ?
						this.sourceBlock_.workspace.targetWorkspace :
						this.sourceBlock_.workspace;
				this.setValue(Core.Variables.generateUniqueName(workspace));
			}
			// If the selected variable doesn't exist yet, create it.
			// For instance, some blocks in the toolbox have variable dropdowns filled
			// in by default.
			if (!this.sourceBlock_.isInFlyout) {
				this.sourceBlock_.workspace.createVariable(this.getValue());
			}
		}

		/// <summary>
		/// Get the variable's name (use a variableDB to convert into a real name).
		/// Unline a regular dropdown, variables are literal and have no neutral value.
		/// </summary>
		/// <returns>Current text.</returns>
		public override string getValue()
		{
			return this.getText();
		}

		/// <summary>
		/// Set the variable name.
		/// </summary>
		/// <param name="newValue">New text.</param>
		public override void setValue(string newValue)
		{
			if (this.sourceBlock_ != null && Events.isEnabled()) {
				Events.fire(new Events.Change(
					this.sourceBlock_, "field", this.name, this.value_, newValue));
			}
			this.value_ = newValue;
			this.setText(newValue);
		}

		/// <summary>
		/// Return a sorted list of variable names for variable dropdown menus.
		/// Include a special option at the end for creating a new variable name.
		/// </summary>
		/// <returns>Array of variable names.</returns>
		public JsArray<DropdownItemInfo> dropdownCreate()
		{
			JsArray<string> variableList;
			if (this.sourceBlock_ != null && this.sourceBlock_.workspace != null) {
				// Get a copy of the list, so that adding rename and new variable options
				// doesn't modify the workspace's list.
				variableList = this.sourceBlock_.workspace.variableList.Slice(0);
			}
			else {
				variableList = new JsArray<string>();
			}
			// Ensure that the currently selected variable is an option.
			var name = this.getText();
			if (name != null && variableList.IndexOf(name) == -1) {
				variableList.Push(name);
			}
			Array.Sort(variableList, CaseInsensitiveCompare.caseInsensitiveCompare);
			variableList.Push(Msg.RENAME_VARIABLE);
			variableList.Push(Msg.DELETE_VARIABLE.Replace("%1", name));
			// Variables are not language-specific, use the name as both the user-facing
			// text and the internal representation.
			var options = new JsArray<DropdownItemInfo>(variableList.Length);
			for (var i = 0; i < variableList.Length; i++) {
				options[i] = new DropdownItemInfo(variableList[i], variableList[i]);
			}
			return options;
		}

		public override object classValidator(string text)
		{
			var workspace = this.sourceBlock_.workspace;
			if (text == Msg.RENAME_VARIABLE) {
				var oldVar = this.getText();
				Core.hideChaff();
				Variables.promptName(
					Msg.RENAME_VARIABLE_TITLE.Replace("%1", oldVar), oldVar, (newVar) => {
						text = newVar;
					});
				if (text != null) {
					workspace.renameVariable(oldVar, text);
				}
				return null;
			}
			else if (text == Msg.DELETE_VARIABLE.Replace("%1",
			  this.getText())) {
				workspace.deleteVariable(this.getText());
				return null;
			}
			return Script.Undefined;
		}
	}
}
