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
 * @fileoverview Colour input field.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Text.RegularExpressions;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldColour : Field
	{
		/// <summary>
		/// Class for a colour input field.
		/// </summary>
		/// <param name="colour">The initial colour in '#rrggbb' format.</param>
		/// <param name="opt_validator">A function that is executed when a new
		/// colour is selected.  Its sole argument is the new colour value.  Its
		/// return value becomes the selected colour, unless it is undefined, in
		/// which case the new colour stands, or it is null, in which case the change
		/// is aborted.</param>
		public FieldColour(string colour, Func<Field, string, object> opt_validator = null)
			: base(colour, opt_validator)
		{
			this.setText(Field.NBSP + Field.NBSP + Field.NBSP);
		}

		/// <summary>
		/// By default use the global constants for colours.
		/// </summary>
		private string[] colours_;

		private string colour_;

		/// <summary>
		/// By default use the global constants for columns.
		/// </summary>
		private int? columns_;

		/// <summary>
		/// Install this field on a block.
		/// </summary>
		public override void init()
		{
			base.init();
			this.borderRect_.style.FillOpacity = "1";
			this.setValue(this.getValue());
		}

		/// <summary>
		/// Mouse cursor style when over the hotspot that initiates the editor.
		/// </summary>
		public new string CURSOR = "default";

		/// <summary>
		/// Close the colour picker if this input is being deleted.
		/// </summary>
		public override void dispose()
		{
			WidgetDiv.hideIfOwner(this);
			base.dispose();
		}

		/// <summary>
		/// Return the current colour.
		/// </summary>
		/// <returns>Current colour in '#rrggbb' format.</returns>
		public override string getValue()
		{
			return this.colour_;
		}

		/// <summary>
		/// Set the colour.
		/// </summary>
		/// <param name="colour">The new colour in '#rrggbb' format.</param>
		public override void setValue(string colour)
		{
			if (this.sourceBlock_ != null && Events.isEnabled() &&
				this.colour_ != colour) {
				Events.fire(new Events.Change(
					this.sourceBlock_, "field", this.name, this.colour_, colour));
			}
			this.colour_ = colour;
			if (this.borderRect_ != null) {
				this.borderRect_.style.Fill = colour;
			}
		}

		/// <summary>
		/// Get the text from this field.  Used when the block is collapsed.
		/// </summary>
		/// <returns>Current text.</returns>
		public override string getText()
		{
			var colour = this.colour_;
			// Try to use #rgb format if possible, rather than #rrggbb.
			var m = colour.Match(new Regex(@"^#(.)\1(.)\2(.)\3$"));
			if (m != null) {
				colour = '#' + m[1] + m[2] + m[3];
			}
			return colour;
		}

		/// <summary>
		/// An array of colour strings for the palette.
		/// See bottom of this page for the default:
		/// http://docs.closure-library.googlecode.com/git/closure_goog.ui_colorpicker.js.source.html
		/// </summary>
		public static string[] COLOURS = goog.ui.ColorPicker.SIMPLE_GRID_COLORS;

		/// <summary>
		/// Number of columns in the palette.
		/// </summary>
		public const int COLUMNS = 7;

		/// <summary>
		/// Set a custom colour grid for this field.
		/// </summary>
		/// <param name="colours">Array of colours for this block,
		/// or null to use default (FieldColour.COLOURS).</param>
		/// <returns>Returns itself (for method chaining).</returns>
		public FieldColour setColours(string[] colours)
		{
			this.colours_ = colours;
			return this;
		}

		/// <summary>
		/// Set a custom grid size for this field.
		/// </summary>
		/// <param name="columns">Number of columns for this block,
		/// or 0 to use default (FieldColour.COLUMNS).</param>
		/// <returns>Returns itself (for method chaining).</returns>
		public FieldColour setColumns(int columns)
		{
			this.columns_ = columns;
			return this;
		}

		private static goog.events.Listener changeEventKey_;

		/// <summary>
		/// Create a palette under the colour field.
		/// </summary>
		public override void showEditor_(bool opt_quietInput)
		{
			WidgetDiv.show(this, this.sourceBlock_.RTL,
				new Action(FieldColour.widgetDispose_));
			// Create the palette using Closure.
			var picker = new goog.ui.ColorPicker();
			picker.setSize(this.columns_ ?? FieldColour.COLUMNS);
			picker.setColors(new JsArray<string>(this.colours_ ?? FieldColour.COLOURS));

			// Position the palette to line up with the field.
			// Record windowSize and scrollOffset before adding the palette.
			var windowSize = goog.dom.getViewportSize();
			var scrollOffset = goog.style.getViewportPageOffset(Document.Instance);
			var xy = this.getAbsoluteXY_();
			var borderBBox = this.getScaledBBox_();
			var div = WidgetDiv.DIV;
			picker.render(div);
			picker.setSelectedColor(this.getValue());
			// Record paletteSize after adding the palette.
			var paletteSize = goog.style.getSize(picker.getElement());

			// Flip the palette vertically if off the bottom.
			if (xy.y + paletteSize.height + borderBBox.height >=
				windowSize.height + scrollOffset.y) {
				xy.y -= paletteSize.height - 1;
			}
			else {
				xy.y += borderBBox.height - 1;
			}
			if (this.sourceBlock_.RTL) {
				xy.x += borderBBox.width;
				xy.x -= paletteSize.width;
				// Don't go offscreen left.
				if (xy.x < scrollOffset.x) {
					xy.x = scrollOffset.x;
				}
			}
			else {
				// Don't go offscreen right.
				if (xy.x > windowSize.width + scrollOffset.x - paletteSize.width) {
					xy.x = windowSize.width + scrollOffset.x - paletteSize.width;
				}
			}
			WidgetDiv.position(xy.x, xy.y, windowSize, scrollOffset,
									   this.sourceBlock_.RTL);

			// Configure event handler.
			var thisField = this;
			FieldColour.changeEventKey_ = goog.events.listen(picker,
				goog.ui.ColorPicker.EventType.CHANGE,
				new Action<goog.events.Event>((e) => {
					string colour = ((goog.ui.ColorPicker)e.target).getSelectedColor() ?? "#000000";
					WidgetDiv.hide();
					if (thisField.sourceBlock_ != null) {
						// Call any validation function, and allow it to override.
						colour = thisField.callValidator(colour);
					}
					if (colour != null) {
						thisField.setValue(colour);
					}
				}));
		}

		/// <summary>
		/// Hide the colour palette.
		/// </summary>
		private static void widgetDispose_()
		{
			if (FieldColour.changeEventKey_ != null) {
				goog.events.unlistenByKey(FieldColour.changeEventKey_);
			}
		}
	}
}
