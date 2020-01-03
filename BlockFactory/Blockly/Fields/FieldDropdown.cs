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
 * @fileoverview Dropdown input field.  Used for editable titles and variables.
 * In the interests of a consistent UI, the toolbox shares some functions and
 * properties with the context menu.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldDropdown : Field
	{
		private Union<JsArray<DropdownItemInfo>, Func<JsArray<DropdownItemInfo>>> menuGenerator_ = new JsArray<DropdownItemInfo>();

		/// <summary>
		/// Class for an editable dropdown field.
		/// </summary>
		/// <param name="menuGenerator">An array of
		/// options for a dropdown list, or a function which generates these options.</param>
		/// <param name="opt_validator">A function that is executed when a new
		/// option is selected, with the newly selected value as its sole argument.
		/// If it returns a value, that value (which must be one of the options) will
		/// become selected in place of the newly selected option, unless the return
		/// value is null, in which case the change is aborted.
		/// </param>
		public FieldDropdown(Union<JsArray<DropdownItemInfo>, Func<JsArray<DropdownItemInfo>>> menuGenerator, Func<Field, string, object> opt_validator = null)
			: base("", opt_validator)
		{
			setMenuGenerator_(menuGenerator);
		}

		protected void setMenuGenerator_(Union<JsArray<DropdownItemInfo>, Func<JsArray<DropdownItemInfo>>> menuGenerator)
		{
			this.menuGenerator_ = menuGenerator;
			this.trimOptions_();
			var firstTuple = this.getOptions_().FirstOrDefault();
			setValue(firstTuple != null ? firstTuple.value : "");
		}

		/// <summary>
		/// Horizontal distance that a checkmark ovehangs the dropdown.
		/// </summary>
		public const int CHECKMARK_OVERHANG = 25;

		/// <summary>
		/// Android can't (in 2014) display "▾", so use "▼" instead.
		/// </summary>
		public static string ARROW_CHAR = goog.userAgent.ANDROID ? "\u25BE" : "\u25BE";

		/// <summary>
		/// Mouse cursor style when over the hotspot that initiates the editor.
		/// </summary>
		public new string CURSOR = "default";

		private SVGElement arrow_;
		protected string value_;

		/// <summary>
		/// Install this dropdown on a block.
		/// </summary>
		public override void init()
		{
			if (this.fieldGroup_ != null) {
				// Dropdown has already been initialized once.
				return;
			}
			// Add dropdown arrow: "option ▾" (LTR) or "▾ אופציה" (RTL)
			this.arrow_ = Core.createSvgElement("tspan", new Dictionary<string, object>(), null);
			this.arrow_.AppendChild(Document.CreateTextNode(
				this.sourceBlock_.RTL ? FieldDropdown.ARROW_CHAR + " " :
					' ' + FieldDropdown.ARROW_CHAR));

			base.init();
			// Force a reset of the text to add the arrow.
			var text = this.text_;
			this.text_ = null;
			this.setText(text);
		}

		/// <summary>
		/// Create a dropdown menu under the text.
		/// </summary>
		public override void showEditor_(bool opt_quietInput)
		{
			WidgetDiv.show(this, this.sourceBlock_.RTL, null);
			var thisField = this;

			var callback = new Action<goog.events.Event>((e) => {
				var menuItem = (goog.ui.MenuItem)e.target;
				if (menuItem != null) {
					var value = menuItem.getValue();
					if (thisField.sourceBlock_ != null) {
						// Call any validation function, and allow it to override.
						value = thisField.callValidator(value);
					}
					if (value != null) {
						thisField.setValue(value);
					}
				}
				WidgetDiv.hideIfOwner(thisField);
			});

			var menu = new goog.ui.Menu();
			menu.setRightToLeft(this.sourceBlock_.RTL);
			var options = this.getOptions_();
			for (var i = 0; i < options.Length; i++) {
				var text = options[i].text;  // Human-readable text.
				var value = options[i].value; // Language-neutral value.
				var menuItem = new goog.ui.MenuItem(text);
				menuItem.setRightToLeft(this.sourceBlock_.RTL);
				menuItem.setValue(value);
				menuItem.setCheckable(true);
				menu.addChild(menuItem, true);
				menuItem.setChecked(value == this.value_);
			}
			// Listen for mouse/keyboard events.
			goog.events.listen(menu, goog.ui.Component.EventType.ACTION, callback);
			// Listen for touch events (why doesn't Closure handle this already?).
			var callbackTouchStart = new Action<goog.events.BrowserEvent>((e) => {
				var control = menu.getOwnerControl((Node)e.target);
				// Highlight the menu item.
				control.handleMouseDown(e);
			});
			var callbackTouchEnd = new Action<goog.events.Event>((e) => {
				var control = menu.getOwnerControl((Node)e.target);
				// Activate the menu item.
				control.performActionInternal(e);
			});
			menu.getHandler().listen(menu.getElement(), goog.events.EventType.TOUCHSTART,
									callbackTouchStart);
			menu.getHandler().listen(menu.getElement(), goog.events.EventType.TOUCHEND,
									callbackTouchEnd);

			// Record windowSize and scrollOffset before adding menu.
			var windowSize = goog.dom.getViewportSize();
			var scrollOffset = goog.style.getViewportPageOffset(Document.Instance);
			var xy = this.getAbsoluteXY_();
			var borderBBox = this.getScaledBBox_();
			var div = WidgetDiv.DIV;
			menu.render(div);
			var menuDom = menu.getElement();
			Core.addClass_(menuDom, "blocklyDropdownMenu");
			// Record menuSize after adding menu.
			var menuSize = goog.style.getSize(menuDom);
			// Recalculate height for the total content, not only box height.
			menuSize.height = menuDom.ScrollHeight;

			// Position the menu.
			// Flip menu vertically if off the bottom.
			if (xy.y + menuSize.height + borderBBox.height >=
				windowSize.height + scrollOffset.y) {
				xy.y -= menuSize.height + 2;
			}
			else {
				xy.y += borderBBox.height;
			}
			if (this.sourceBlock_.RTL) {
				xy.x += borderBBox.width;
				xy.x += FieldDropdown.CHECKMARK_OVERHANG;
				// Don't go offscreen left.
				if (xy.x < scrollOffset.x + menuSize.width) {
					xy.x = scrollOffset.x + menuSize.width;
				}
			}
			else {
				xy.x -= FieldDropdown.CHECKMARK_OVERHANG;
				// Don't go offscreen right.
				if (xy.x > windowSize.width + scrollOffset.x - menuSize.width) {
					xy.x = windowSize.width + scrollOffset.x - menuSize.width;
				}
			}
			WidgetDiv.position(xy.x, xy.y, windowSize, scrollOffset,
									   this.sourceBlock_.RTL);
			menu.setAllowAutoFocus(true);
			menuDom.Focus();
		}

		/// <summary>
		/// Factor out common words in statically defined options.
		/// Create prefix and/or suffix labels.
		/// </summary>
		private void trimOptions_()
		{
			this.prefixField = null;
			this.suffixField = null;
			var options = this.menuGenerator_.As<JsArray<DropdownItemInfo>>();
			if (options == null || options.Length < 2) {
				return;
			}
			var strings = options.Map((t) => { return t.text; });
			var shortest = Core.shortestStringLength(strings);
			var prefixLength = Core.commonWordPrefix(strings, shortest);
			var suffixLength = Core.commonWordSuffix(strings, shortest);
			if (prefixLength == 0 && suffixLength == 0) {
				return;
			}
			if (shortest <= prefixLength + suffixLength) {
				// One or more strings will entirely vanish if we proceed.  Abort.
				return;
			}
			if (prefixLength != 0) {
				this.prefixField = strings[0].Substring(0, prefixLength - 1);
			}
			if (suffixLength != 0) {
				this.suffixField = strings[0].Substr(1 - suffixLength);
			}
			// Remove the prefix and suffix from the options.
			var newOptions = new JsArray<DropdownItemInfo>(options.Length);
			for (var i = 0; i < options.Length; i++) {
				var text = options[i].text;
				var value = options[i].value;
				text = text.Substring(prefixLength, text.Length - suffixLength);
				newOptions[i] = new DropdownItemInfo(text, value);
			}
			this.menuGenerator_ = newOptions;
		}

		/// <summary>
		/// Return a list of the options for this dropdown.
		/// </summary>
		/// <returns>Array of option tuples:
		/// (human-readable text, language-neutral name).</returns>
		private JsArray<DropdownItemInfo> getOptions_()
		{
			if (this.menuGenerator_.Is<Func<JsArray<DropdownItemInfo>>>()) {
				return (this.menuGenerator_.As<Func<JsArray<DropdownItemInfo>>>())();
			}
			return this.menuGenerator_.As<JsArray<DropdownItemInfo>>();
		}

		/// <summary>
		/// Get the language-neutral value from this dropdown menu.
		/// </summary>
		/// <returns>Current text.</returns>
		public override string getValue()
		{
			return this.value_;
		}

		/// <summary>
		/// Set the language-neutral value for this dropdown menu.
		/// </summary>
		/// <param name="newValue">New value to set.</param>
		public override void setValue(string newValue)
		{
			if (newValue == null || newValue == this.value_) {
				return;  // No change if null.
			}
			if (this.sourceBlock_ != null && Events.isEnabled()) {
				Events.fire(new Events.Change(
					this.sourceBlock_, "field", this.name, this.value_, newValue));
			}
			this.value_ = newValue;
			// Look up and display the human-readable text.
			var options = this.getOptions_();
			for (var i = 0; i < options.Length; i++) {
				// Options are tuples of human-readable text and language-neutral values.
				if (options[i].value == newValue) {
					this.setText(options[i].text);
					return;
				}
			}
			// Value not found.  Add it, maybe it will become valid once set
			// (like variable names).
			this.setText(newValue);
		}

		/// <summary>
		/// Set the text in this field.  Trigger a rerender of the source block.
		/// </summary>
		/// <param name="text">New text.</param>
		public override void setText(string text)
		{
			if (this.sourceBlock_ != null && this.arrow_ != null) {
				// Update arrow's colour.
				this.arrow_.style.Fill = this.sourceBlock_.getColour();
			}
			if (text == null || text == this.text_) {
				// No change if null.
				return;
			}
			this.text_ = text;
			this.updateTextNode_();

			if (this.textElement_ != null) {
				// Insert dropdown arrow.
				if (this.sourceBlock_.RTL) {
					this.textElement_.InsertBefore(this.arrow_, this.textElement_.FirstChild);
				}
				else {
					this.textElement_.AppendChild(this.arrow_);
				}
			}

			if (this.sourceBlock_ != null && this.sourceBlock_.rendered) {
				this.sourceBlock_.render();
				this.sourceBlock_.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Close the dropdown menu if this input is being deleted.
		/// </summary>
		public override void dispose()
		{
			WidgetDiv.hideIfOwner(this);
			base.dispose();
		}
	}

	public class DropdownItemInfo
	{
		public string text;
		public string value;

		public DropdownItemInfo()
		{
		}

		public DropdownItemInfo(string text, string value)
		{
			this.text = text;
			this.value = value;
		}
	}
}
