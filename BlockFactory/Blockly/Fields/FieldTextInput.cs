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
 * @fileoverview Text input field.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Text.RegularExpressions;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldTextInput : Field
	{
		public Action<string> onFinishEditing_;

		/// <summary>
		/// Class for an editable text field.
		/// </summary>
		/// <param name="text">The initial content of the field.</param>
		/// <param name="opt_validator">An optional function that is called
		/// to validate any constraints on what the user entered.  Takes the new
		/// text as an argument and returns either the accepted text, a replacement
		/// text, or null to abort the change.</param>
		public FieldTextInput(string text, Func<Field, string, object> opt_validator = null)
			: base(text, opt_validator)
		{
		}

		/// <summary>
		/// Point size of text.  Should match blocklyText's font-size in CSS.
		/// </summary>
		public const int FONTSIZE = 11;

		/// <summary>
		/// Mouse cursor style when over the hotspot that initiates the editor.
		/// </summary>
		public new string CURSOR = "text";

		/// <summary>
		/// Allow browser to spellcheck this field.
		/// </summary>
		private bool spellcheck_;

		/// <summary>
		/// Close the input widget if this input is being deleted.
		/// </summary>
		public override void dispose()
		{
			WidgetDiv.hideIfOwner(this);
			base.dispose();
		}

		/// <summary>
		/// Set the text in this field.
		/// </summary>
		/// <param name="text">New text.</param>
		public override void setValue(string text)
		{
			if (text == null) {
				return;  // No change if null.
			}
			if (this.sourceBlock_ != null) {
				var validated = this.callValidator(text);
				// If the new text is invalid, validation returns null.
				// In this case we still want to display the illegal result.
				if (validated != null) {
					text = validated;
				}
			}
			base.setValue(text);
		}

		/// <summary>
		/// Set whether this field is spellchecked by the browser.
		/// </summary>
		/// <param name="check">True if checked.</param>
		public void setSpellcheck(bool check)
		{
			this.spellcheck_ = check;
		}

		private WorkspaceSvg workspace_;
		internal static HTMLInputElement htmlInput_;

		/// <summary>
		/// Show the inline free-text editor on top of the text.
		/// </summary>
		/// <param name="opt_quietInput">True if editor should be created without
		/// focus.Defaults to false.</param>
		public override void showEditor_(bool opt_quietInput)
		{
			this.workspace_ = (WorkspaceSvg)this.sourceBlock_.workspace;
			var quietInput = opt_quietInput || false;
			if (!quietInput && (goog.userAgent.MOBILE || goog.userAgent.ANDROID ||
								goog.userAgent.IPAD)) {
				// Mobile browsers have issues with in-line textareas (focus & keyboards).
				var newValue = Window.Prompt(Msg.CHANGE_VALUE_TITLE, this.text_);
				if (this.sourceBlock_ != null) {
					newValue = this.callValidator(newValue);
				}
				this.setValue(newValue);
				return;
			}

			WidgetDiv.show(this, this.sourceBlock_.RTL, this.widgetDispose_());
			var div = WidgetDiv.DIV;
			// Create the input.
			var htmlInput = (HTMLInputElement)
				goog.dom.createDom(goog.dom.TagName.INPUT, "blocklyHtmlInput");
			htmlInput.SetAttribute("spellcheck", this.spellcheck_.ToString());
			var fontSize =
				(FieldTextInput.FONTSIZE * this.workspace_.scale) + "pt";
			div.Style.FontSize = fontSize;
			htmlInput.Style.FontSize = fontSize;
			/** @type {!HTMLInputElement} */
			FieldTextInput.htmlInput_ = htmlInput;
			div.AppendChild(htmlInput);

			htmlInput.Value = htmlInput.DefaultValue = this.text_;
			htmlInput["oldValue_"] = null;
			this.validate_();
			this.resizeEditor_();
			if (!quietInput) {
				htmlInput.Focus();
				htmlInput.Select();
			}

			// Bind to keydown -- trap Enter without IME and Esc to hide.
			htmlInput["onKeyDownWrapper_"] =
				Core.bindEventWithChecks_(htmlInput, "keydown", this,
				new Action<KeyboardEvent>(this.onHtmlInputKeyDown_));
			// Bind to keyup -- trap Enter; resize after every keystroke.
			htmlInput["onKeyUpWrapper_"] =
				Core.bindEventWithChecks_(htmlInput, "keyup", this,
				new Action<Event>(this.onHtmlInputChange_));
			// Bind to keyPress -- repeatedly resize when holding down a key.
			htmlInput["onKeyPressWrapper_"] =
				Core.bindEventWithChecks_(htmlInput, "keypress", this,
				new Action<Event>(this.onHtmlInputChange_));
			htmlInput["onWorkspaceChangeWrapper_"] = new Action<Events.Abstract>(this.resizeEditor_);
			this.workspace_.addChangeListener((Action<Events.Abstract>)htmlInput["onWorkspaceChangeWrapper_"]);
		}

		/// <summary>
		/// Handle key down to the editor.
		/// </summary>
		/// <param name="e">Keyboard event.</param>
		private void onHtmlInputKeyDown_(KeyboardEvent e)
		{
			var htmlInput = FieldTextInput.htmlInput_;
			int tabKey = 9, enterKey = 13, escKey = 27;
			if (e.KeyCode == enterKey) {
				WidgetDiv.hide();
			}
			else if (e.KeyCode == escKey) {
				htmlInput.Value = htmlInput.DefaultValue;
				WidgetDiv.hide();
			}
			else if (e.KeyCode == tabKey) {
				WidgetDiv.hide();
				this.sourceBlock_.tab(this, !e.ShiftKey);
				e.PreventDefault();
			}
		}

		/// <summary>
		/// Handle a change to the editor.
		/// </summary>
		/// <param name="e">Keyboard event.</param>
		private void onHtmlInputChange_(Event e)
		{
			var htmlInput = FieldTextInput.htmlInput_;
			// Update source block.
			var text = htmlInput.Value;
			if (text != (string)htmlInput["oldValue_"]) {
				htmlInput["oldValue_"] = text;
				this.setValue(text);
				this.validate_();
			}
			else if (goog.userAgent.WEBKIT) {
				// Cursor key.  Render the source block to show the caret moving.
				// Chrome only (version 26, OS X).
				this.sourceBlock_.render();
			}
			this.resizeEditor_();
			Core.svgResize((WorkspaceSvg)this.sourceBlock_.workspace);
		}

		/// <summary>
		/// Check to see if the contents of the editor validates.
		/// Style the editor accordingly.
		/// </summary>
		protected void validate_()
		{
			var valid = true;
			goog.asserts.assertObject(FieldTextInput.htmlInput_, "");
			var htmlInput = FieldTextInput.htmlInput_;
			if (this.sourceBlock_ != null) {
				valid = this.callValidator(htmlInput.Value) != null;
			}
			if (!valid) {
				Core.addClass_(htmlInput, "blocklyInvalidInput");
			}
			else {
				Core.removeClass_(htmlInput, "blocklyInvalidInput");
			}
		}

		/// <summary>
		/// Resize the editor and the underlying block to fit the text.
		/// </summary>
		protected void resizeEditor_(Events.Abstract e = null)
		{
			var div = WidgetDiv.DIV;
			var bBox = this.fieldGroup_.getBBox();
			div.Style.Width = bBox.width * this.workspace_.scale + "px";
			div.Style.Height = bBox.height * this.workspace_.scale + "px";
			var xy = this.getAbsoluteXY_();
			// In RTL mode block fields and LTR input fields the left edge moves,
			// whereas the right edge is fixed.  Reposition the editor.
			if (this.sourceBlock_.RTL) {
				var borderBBox = this.getScaledBBox_();
				xy.x += borderBBox.width;
				xy.x -= div.OffsetWidth;
			}
			// Shift by a few pixels to line up exactly.
			xy.y += 1;
			if (goog.userAgent.GECKO && WidgetDiv.DIV.Style.Top != null) {
				// Firefox mis-reports the location of the border by a pixel
				// once the WidgetDiv is moved into position.
				xy.x -= 1;
				xy.y -= 1;
			}
			if (goog.userAgent.WEBKIT) {
				xy.y -= 3;
			}
			div.Style.Left = xy.x + "px";
			div.Style.Top = xy.y + "px";
		}

		/// <summary>
		/// Close the editor, save the results, and dispose of the editable
		/// text field's elements.
		/// </summary>
		/// <returns>Closure to call on destruction of the WidgetDiv.</returns>
		private Action widgetDispose_()
		{
			var thisField = this;
			return new Action(() => {
				var htmlInput = FieldTextInput.htmlInput_;
				// Save the edit (if it validates).
				var text = htmlInput.Value;
				if (thisField.sourceBlock_ != null) {
					var text1 = thisField.callValidator(text);
					if (text1 == null) {
						// Invalid edit.
						text = htmlInput.DefaultValue;
					}
					else {
						// Validation function has changed the text.
						text = text1;
						if (thisField.onFinishEditing_ != null) {
							thisField.onFinishEditing_(text);
						}
					}
				}
				thisField.setValue(text);
				if (thisField.sourceBlock_.rendered) thisField.sourceBlock_.render();
				Core.unbindEvent_((JsArray<EventWrapInfo>)htmlInput["onKeyDownWrapper_"]);
				Core.unbindEvent_((JsArray<EventWrapInfo>)htmlInput["onKeyUpWrapper_"]);
				Core.unbindEvent_((JsArray<EventWrapInfo>)htmlInput["onKeyPressWrapper_"]);
				thisField.workspace_.removeChangeListener(
					(Action<Events.Abstract>)htmlInput["onWorkspaceChangeWrapper_"]);
				FieldTextInput.htmlInput_ = null;
				// Delete style properties.
				var style = WidgetDiv.DIV.Style;
				style.Width = "auto";
				style.Height = "auto";
				style.FontSize = "";
			});
		}

		/// <summary>
		/// Ensure that only a number may be entered.
		/// </summary>
		/// <param name="text">The user's text.</param>
		/// <returns>A string representing a valid number, or null if invalid.</returns>
		public virtual string numberValidator(string text)
		{
			Console.WriteLine("Blockly.FieldTextInput.numberValidator is deprecated. " +
						 "Use Blockly.FieldNumber instead.");
			if (text == null) {
				return null;
			}
			text = text.ToString();
			// TODO: Handle cases like 'ten', '1.203,14', etc.
			// 'O' is sometimes mistaken for '0' by inexperienced users.
			text = text.Replace(new Regex(@"O", RegexOptions.Multiline | RegexOptions.IgnoreCase), "0");
			// Strip out thousands separators.
			text = text.Replace(new Regex(@",", RegexOptions.Multiline), "");
			var n = Script.ParseFloat(text != null ? text : "0");
			return Double.IsNaN(n) ? null : n.ToString();
		}

		/// <summary>
		/// Ensure that only a nonnegative integer may be entered.
		/// </summary>
		/// <param name="text">The user's text.</param>
		/// <returns>A string representing a valid int, or null if invalid.</returns>
		public string nonnegativeIntegerValidator(string text)
		{
			var n = numberValidator(text);
			if (n != null) {
				n = System.Math.Max(0, System.Math.Floor(Script.ParseFloat(n))).ToString();
			}
			return n;
		}
	}
}
