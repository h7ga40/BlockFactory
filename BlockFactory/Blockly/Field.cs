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
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Field
	{
		public goog.math.Size size_;
		internal string prefixField;
		internal string suffixField;

		public double renderSep;
		public double renderWidth;

		/// <summary>
		/// Abstract class for an editable field.
		/// </summary>
		/// <param name="text">The initial content of the field.</param>
		/// <param name="opt_validator">An optional function that is called
		/// to validate any constraints on what the user entered.  Takes the new
		/// text as an argument and returns either the accepted text, a replacement
		/// text, or null to abort the change.</param>
		public Field(string text, Func<Field, string, object> opt_validator = null)
		{
			size_ = new goog.math.Size(0, 25);
			setValue(text);
			setValidator(opt_validator);
		}

		/// <summary>
		/// Temporary cache of text widths.
		/// </summary>
		private static Dictionary<string, double> cacheWidths_;

		/// <summary>
		/// Number of current references to cache.
		/// </summary>
		private static int cacheReference_;

		/// <summary>
		/// Name of field.  Unique within each block.
		/// Static labels are usually unnamed.
		/// </summary>
		public string name;

		/// <summary>
		/// Maximum characters of text to display before adding an ellipsis.
		/// </summary>
		public int maxDisplayLength = 50;

		/// <summary>
		/// Visible text to display.
		/// </summary>
		internal string text_ = "";

		/// <summary>
		/// Block this field is attached to.  Starts as null, then in set in init.
		/// </summary>
		internal BlockSvg sourceBlock_;

		/// <summary>
		/// Is the field visible, or hidden due to the block being collapsed?
		/// </summary>
		protected bool visible_ = true;

		/// <summary>
		/// Validation function called when user edits an editable field.
		/// </summary>
		private Func<Field, string, object> validator_;

		/// <summary>
		/// Non-breaking space.
		/// </summary>
		public const string NBSP = "\u00A0";

		/// <summary>
		/// Editable fields are saved by the XML renderer, non-editable fields are not.
		/// </summary>
		public bool EDITABLE = true;


		protected string CURSOR;

		/// <summary>
		/// Attach this field to a block.
		/// </summary>
		/// <param name="block">The block containing this field.</param>
		public void setSourceBlock(BlockSvg block)
		{
			System.Diagnostics.Debug.Assert(sourceBlock_ == null, "Field already bound to a block.");
			sourceBlock_ = block;
		}

		protected SVGElement fieldGroup_;
		protected SVGElement borderRect_;
		protected SVGElement textElement_;
		protected JsArray<EventWrapInfo> mouseUpWrapper_;

		/// <summary>
		/// Install this field on a block.
		/// </summary>
		public virtual void init()
		{
			if (this.fieldGroup_ != null) {
				// Field has already been initialized once.
				return;
			}
			// Build the DOM.
			this.fieldGroup_ = Core.createSvgElement("g", new Dictionary<string, object>(), null);
			if (!this.visible_) {
				this.fieldGroup_.style.Display = Display.None;
			}
			this.borderRect_ = Core.createSvgElement("rect",
				new Dictionary<string, object>() {
					{ "rx", 4.0 },
					{ "ry", 4.0 },
					{ "x", -BlockSvg.SEP_SPACE_X / 2.0 },
					{ "y", 0.0 },
					{ "height", 16.0 } },
				this.fieldGroup_, this.sourceBlock_.workspace);
			/** @type {!Element} */
			this.textElement_ = Core.createSvgElement("text", new Dictionary<string, object>() {
					{ "class", "blocklyText" },
					{ "y", this.size_.height - 12.5} },
				this.fieldGroup_);

			this.updateEditable();
			this.sourceBlock_.getSvgRoot().AppendChild(this.fieldGroup_);
			this.mouseUpWrapper_ =
				Core.bindEventWithChecks_(this.fieldGroup_, "mouseup", this,
				new Action<MouseEvent>(this.onMouseUp_));
			// Force a render.
			this.updateTextNode_();
		}

		/// <summary>
		/// Dispose of all DOM objects belonging to this editable field.
		/// </summary>
		public virtual void dispose()
		{
			if (this.mouseUpWrapper_ != null) {
				Core.unbindEvent_(this.mouseUpWrapper_);
				this.mouseUpWrapper_ = null;
			}
			this.sourceBlock_ = null;
			goog.dom.removeNode(this.fieldGroup_);
			this.fieldGroup_ = null;
			this.textElement_ = null;
			this.borderRect_ = null;
			this.validator_ = null;
		}

		/// <summary>
		/// Add or remove the UI indicating if this field is editable or not.
		/// </summary>
		public void updateEditable()
		{
			var group = this.fieldGroup_;
			if (!this.EDITABLE || group == null) {
				return;
			}
			if (this.sourceBlock_.isEditable()) {
				Core.addClass_(group, "blocklyEditableText");
				Core.removeClass_(group, "blocklyNonEditableText");
				this.fieldGroup_.style.Cursor = this.CURSOR;
			}
			else {
				Core.addClass_(group, "blocklyNonEditableText");
				Core.removeClass_(group, "blocklyEditableText");
				this.fieldGroup_.style.Cursor = "";
			}
		}

		/// <summary>
		/// Gets whether this editable field is visible or not.
		/// </summary>
		/// <returns>True if visible.</returns>
		public bool isVisible()
		{
			return this.visible_;
		}

		/// <summary>
		/// Sets whether this editable field is visible or not.
		/// </summary>
		/// <param name="visible">True if visible.</param>
		public void setVisible(bool visible)
		{
			if (this.visible_ == visible) {
				return;
			}
			this.visible_ = visible;
			var root = this.getSvgRoot();
			if (root != null) {
				root.style.Display = visible ? Display.Block : Display.None;
				this.render_();
			}
		}

		/// <summary>
		/// Sets a new validation function for editable fields.
		/// </summary>
		/// <param name="handler">New validation function, or null.</param>
		public virtual void setValidator(Func<Field, string, object> handler)
		{
			this.validator_ = handler;
		}

		/// <summary>
		/// Gets the validation function for editable fields.
		/// </summary>
		/// <returns>Validation function, or null.</returns>
		public Func<Field, string, object> getValidator()
		{
			return this.validator_;
		}

		/// <summary>
		/// Validates a change.  Does nothing.  Subclasses may override this.
		/// </summary>
		/// <param name="text">The user's text.</param>
		/// <returns>No change needed.</returns>
		public virtual object classValidator(string text)
		{
			return text;
		}

		/// <summary>
		/// Calls the validation function for this field, as well as all the validation
		/// function for the field's class and its parents.
		/// </summary>
		/// <param name="text">Proposed text.</param>
		/// <returns>Revised text, or null if invalid.</returns>
		public string callValidator(string text)
		{
			var classResult = this.classValidator(text);
			if (classResult == null) {
				// Class validator rejects value.  Game over.
				return null;
			}
			else if (classResult is string) {
				text = (string)classResult;
			}
			var userValidator = this.getValidator();
			if (userValidator != null) {
				var userResult = userValidator(this, text);
				if (userResult == null) {
					// User validator rejects value.  Game over.
					return null;
				}
				else if (userResult != Script.Undefined) {
					text = userResult.ToString();
				}
			}
			return text;
		}

		/// <summary>
		/// Gets the group element for this editable field.
		/// Used for measuring the size and for positioning.
		/// </summary>
		/// <returns>The group element.</returns>
		public virtual SVGElement getSvgRoot()
		{
			return this.fieldGroup_;
		}

		/// <summary>
		/// Draws the border with the correct width.
		/// Saves the computed width in a property.
		/// </summary>
		protected virtual void render_()
		{
			double width;

			if (this.visible_ && this.textElement_ != null) {
				var key = this.textElement_.TextContent + "\n" +
					this.textElement_.className.baseVal;
				if (Field.cacheWidths_ != null && Field.cacheWidths_.ContainsKey(key)) {
					width = Field.cacheWidths_[key];
				}
				else {
					try {
						width = this.textElement_.getComputedTextLength();
					}
					catch (Exception) {
						// MSIE 11 is known to throw "Unexpected call to method or property
						// access." if Blockly is hidden.
						width = this.textElement_.TextContent.Length * 8;
					}
					if (Field.cacheWidths_ != null) {
						Field.cacheWidths_[key] = width;
					}
				}
				if (this.borderRect_ != null) {
					this.borderRect_.SetAttribute("width",
						(width + BlockSvg.SEP_SPACE_X).ToString());
				}
			}
			else {
				width = 0;
			}
			this.size_.width = width;
		}

		/// <summary>
		/// Start caching field widths.  Every call to this function MUST also call
		/// stopCache.  Caches must not survive between execution threads.
		/// </summary>
		public static void startCache()
		{
			Field.cacheReference_++;
			if (Field.cacheWidths_ == null) {
				Field.cacheWidths_ = new Dictionary<string, double>();
			}
		}

		/// <summary>
		/// Stop caching field widths.  Unless caching was already on when the
		/// corresponding call to startCache was made.
		/// </summary>
		public static void stopCache()
		{
			Field.cacheReference_--;
			if (Field.cacheReference_ == 0) {
				Field.cacheWidths_ = null;
			}
		}

		/// <summary>
		/// Returns the height and width of the field.
		/// </summary>
		/// <returns>Height and width.</returns>
		public goog.math.Size getSize()
		{
			if (this.size_.width == 0) {
				this.render_();
			}
			return this.size_;
		}

		/// <summary>
		/// Returns the height and width of the field,
		/// accounting for the workspace scaling.
		/// </summary>
		/// <returns>Height and width.</returns>
		internal goog.math.Size getScaledBBox_()
		{
			var bBox = this.borderRect_.getBBox();
			// Create new object, as getBBox can return an uneditable SVGRect in IE.
			return new goog.math.Size(bBox.width * ((WorkspaceSvg)this.sourceBlock_.workspace).scale,
									  bBox.height * ((WorkspaceSvg)this.sourceBlock_.workspace).scale);
		}

		/// <summary>
		/// Get the text from this field.
		/// </summary>
		/// <returns>Current text.</returns>
		public virtual string getText()
		{
			return this.text_;
		}

		/// <summary>
		/// Set the text in this field.  Trigger a rerender of the source block.
		/// </summary>
		/// <param name="text">New text.</param>
		public virtual void setText(string text)
		{
			if (text == null) {
				// No change if null.
				return;
			}
			text = text == null ? "" : text;
			if (text == this.text_) {
				// No change.
				return;
			}
			this.text_ = text;
			this.updateTextNode_();

			if (this.sourceBlock_ != null && this.sourceBlock_.rendered) {
				this.sourceBlock_.render();
				this.sourceBlock_.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Update the text node of this field to display the current text.
		/// </summary>
		internal void updateTextNode_()
		{
			if (this.textElement_ == null) {
				// Not rendered yet.
				return;
			}
			var text = this.text_;
			if (text.Length > this.maxDisplayLength) {
				// Truncate displayed string and add an ellipsis ('...').
				text = text.Substring(0, this.maxDisplayLength - 2) + '\u2026';
			}
			// Empty the text element.
			goog.dom.removeChildren(this.textElement_);
			// Replace whitespace with non-breaking spaces so the text doesn't collapse.
			text = text.Replace(new Regex(@"\s ", RegexOptions.Multiline), Field.NBSP);
			if (this.sourceBlock_.RTL && text != null) {
				// The SVG is LTR, force text to be RTL.
				text += '\u200F';
			}
			if (String.IsNullOrEmpty(text)) {
				// Prevent the field from disappearing if empty.
				text = Field.NBSP;
			}
			var textNode = Document.CreateTextNode(text);
			this.textElement_.AppendChild(textNode);

			// Cached width is obsolete.  Clear it.
			this.size_.width = 0;
		}

		/// <summary>
		/// By default there is no difference between the human-readable text and
		/// the language-neutral values.  Subclasses (such as dropdown) may define this.
		/// </summary>
		/// <returns>Current text.</returns>
		public virtual string getValue()
		{
			return this.getText();
		}

		/// <summary>
		/// By default there is no difference between the human-readable text and
		/// the language-neutral values.  Subclasses (such as dropdown) may define this.
		/// </summary>
		/// <param name="newText">New text.</param>
		public virtual void setValue(string newText)
		{
			if (newText == null) {
				// No change if null.
				return;
			}
			var oldText = this.getValue();
			if (oldText == newText) {
				return;
			}
			if (this.sourceBlock_ != null && Events.isEnabled()) {
				Events.fire(new Events.Change(
					this.sourceBlock_, "field", this.name, oldText, newText));
			}
			this.setText(newText);
		}

		/// <summary>
		/// Handle a mouse up event on an editable field.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void onMouseUp_(MouseEvent e)
		{
			if ((goog.userAgent.IPHONE || goog.userAgent.IPAD) &&
				!goog.userAgent.isVersionOrHigher("537.51.2") &&
				e.LayerX != 0 && e.LayerY != 0) {
				// Old iOS spawns a bogus event on the next touch after a 'prompt()' edit.
				// Unlike the real events, these have a layerX and layerY set.
				return;
			}
			else if (Core.isRightButton(e)) {
				// Right-click.
				return;
			}
			else if (((WorkspaceSvg)this.sourceBlock_.workspace).isDragging()) {
				// Drag operation is concluding.  Don't open the editor.
				return;
			}
			else if (this.sourceBlock_.isEditable()) {
				// Non-abstract sub-classes must define a showEditor_ method.
				this.showEditor_();
				// The field is handling the touch, but we also want the blockSvg onMouseUp
				// handler to fire, so we will leave the touch identifier as it is.
				// The next onMouseUp is responsible for nulling it out.
			}
		}

		/// <summary>
		/// Change the tooltip text for this field.
		/// </summary>
		/// <param name="newTip">newTip Text for tooltip or a parent element to
		/// link to for its tooltip.</param>
		public virtual void setTooltip(BlockSvg newTip)
		{
			// Non-abstract sub-classes may wish to implement this.  See FieldLabel.
		}

		/// <summary>
		/// Return the absolute coordinates of the top-left corner of this field.
		/// The origin (0,0) is the top-left corner of the page body.
		/// </summary>
		/// <returns>Object with .x and .y properties.</returns>
		internal goog.math.Coordinate getAbsoluteXY_()
		{
			return goog.style.getPageOffset(this.borderRect_);
		}

		public virtual void showEditor_(bool opt_quietInput = false)
		{
		}
	}
}
