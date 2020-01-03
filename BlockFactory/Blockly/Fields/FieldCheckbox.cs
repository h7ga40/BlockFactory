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
 * @fileoverview Checkbox field.  Checked or not checked.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldCheckbox : Field
	{
		/// <summary>
		/// Class for a checkbox field.
		/// </summary>
		/// <param name="state">The initial state of the field ('TRUE' or 'FALSE').</param>
		/// <param name="opt_validator">A function that is executed when a new
		/// option is selected.  Its sole argument is the new checkbox state.  If
		/// it returns a value, this becomes the new checkbox state, unless the
		/// value is null, in which case the change is aborted.</param>
		public FieldCheckbox(string state, Func<Field, string, object> opt_validator = null)
			: base("", opt_validator)
		{
			// Set the initial state.
			this.setValue(state);
		}

		/// <summary>
		/// Character for the checkmark.
		/// </summary>
		public static string CHECK_CHAR = "\u2713";

		/// <summary>
		/// Mouse cursor style when over the hotspot that initiates editability.
		/// </summary>
		public new string CURSOR = "default";

		private SVGElement checkElement_;

		private bool state_;

		/// <summary>
		/// Install this checkbox on a block.
		/// </summary>
		public override void init()
		{
			if (this.fieldGroup_ != null) {
				// Checkbox has already been initialized once.
				return;
			}
			base.init();
			// The checkbox doesn't use the inherited text element.
			// Instead it uses a custom checkmark element that is either visible or not.
			this.checkElement_ = Core.createSvgElement("text",
				new Dictionary<string, object>() {
					{ "class", "blocklyText blocklyCheckbox" },
					{ "x", -3 },
					{ "y", 14 }
				},
				this.fieldGroup_);
			var textNode = Document.CreateTextNode(FieldCheckbox.CHECK_CHAR);
			this.checkElement_.AppendChild(textNode);
			this.checkElement_.style.Display = this.state_ ? Display.Block : Display.None;
		}

		/// <summary>
		/// Return 'TRUE' if the checkbox is checked, 'FALSE' otherwise.
		/// </summary>
		/// <returns>Current state.</returns>
		public override string getValue()
		{
			return state_.ToString().ToUpper();
		}

		/// <summary>
		/// Set the checkbox to be checked if strBool is 'TRUE', unchecks otherwise.
		/// </summary>
		/// <param name="strBool">New state.</param>
		public override void setValue(string strBool)
		{
			var newState = (strBool.ToUpper() == "TRUE");
			if (this.state_ != newState) {
				if (this.sourceBlock_ != null && Events.isEnabled()) {
					Events.fire(new Events.Change(
						this.sourceBlock_, "field", this.name, this.state_.ToString(), newState.ToString()));
				}
				this.state_ = newState;
				if (this.checkElement_ != null) {
					this.checkElement_.style.Display = newState ? Display.Block : Display.None;
				}
			}
		}

		/// <summary>
		/// Toggle the state of the checkbox.
		/// </summary>
		public override void showEditor_(bool opt_quietInput)
		{
			var newState = (!this.state_).ToString();
			if (this.sourceBlock_ != null) {
				// Call any validation function, and allow it to override.
				newState = this.callValidator(newState);
			}
			if (newState != null) {
				this.setValue(newState.ToUpper());
			}
		}
	}
}
