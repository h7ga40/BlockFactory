// Copyright 2007 The Closure Library Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/**
 * @fileoverview A color picker component.  A color picker can compose several
 * instances of goog.ui.ColorPalette.
 *
 * NOTE: The ColorPicker is in a state of transition towards the common
 * component/control/container interface we are developing.  If the API changes
 * we will do our best to update your code.  The end result will be that a
 * color picker will compose multiple color palettes.  In the simple case this
 * will be one grid, but may consistute 3 distinct grids, a custom color picker
 * or even a color wheel.
 *
 */
using System;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class ColorPicker : Component
	{
		/// <summary>
		/// The color palette used inside the color picker.
		/// </summary>
		private ColorPalette colorPalette_;

		public ColorPicker(goog.dom.DomHelper opt_domHelper = null, goog.ui.ColorPalette opt_colorPalette = null)
			: base(opt_domHelper)
		{
			this.colorPalette_ = opt_colorPalette;

			this.getHandler().listen(
				this, goog.ui.Component.EventType.ACTION, new Action<events.Event>(this.onColorPaletteAction_));
		}

		/// <summary>
		/// Default number of columns in the color palette. May be overridden by calling
		/// setSize.
		/// </summary>
		public static int DEFAULT_NUM_COLS = 5;

		/// <summary>
		/// Constants for event names.
		/// </summary>
		public static new class EventType
		{
			public static readonly string CHANGE = "change";
		}

		/// <summary>
		/// Whether the component is focusable.
		/// </summary>
		private bool focusable_ = true;

		/// <summary>
		/// Gets the array of colors displayed by the color picker.
		/// Modifying this array will lead to unexpected behavior.
		/// </summary>
		/// <returns>The colors displayed by this widget.</returns>
		public JsArray<string> getColors()
		{
			return this.colorPalette_ != null ? this.colorPalette_.getColors() : null;
		}

		/// <summary>
		/// Sets the array of colors to be displayed by the color picker.
		/// </summary>
		/// <param name="colors">The array of colors to be added.</param>
		public void setColors(JsArray<string> colors)
		{
			// TODO(user): Don't add colors directly, we should add palettes and the
			// picker should support multiple palettes.
			if (this.colorPalette_ == null) {
				this.createColorPalette_(colors);
			}
			else {
				this.colorPalette_.setColors(colors);
			}
		}


		/// <summary>
		/// Sets the array of colors to be displayed by the color picker.
		/// </summary>
		/// <param name="colors">The array of colors to be added.</param>
		/// @deprecated Use setColors.
		public void addColors(JsArray<string> colors)
		{
			this.setColors(colors);
		}


		/// <summary>
		/// Sets the size of the palette.  Will throw an error after the picker has been
		/// rendered.
		/// </summary>
		/// <param name="size">The size of the grid.</param>
		public void setSize(Union<goog.math.Size, int> size)
		{
			// TODO(user): The color picker should contain multiple palettes which will
			// all be resized at this point.
			if (this.colorPalette_ == null) {
				this.createColorPalette_(new JsArray<string>());
			}
			this.colorPalette_.setSize(size);
		}


		/// <summary>
		/// Gets the number of columns displayed.
		/// </summary>
		/// <returns>The size of the grid.</returns>
		public goog.math.Size getSize()
		{
			return this.colorPalette_ != null ? this.colorPalette_.getSize() : null;
		}


		/// <summary>
		/// Sets the number of columns.  Will throw an error after the picker has been
		/// rendered.
		/// </summary>
		/// <param name="n">The number of columns.</param>
		/// @deprecated Use setSize.
		public void setColumnCount(int n)
		{
			this.setSize(n);
		}


		/// <summary>
		/// </summary>
		/// <returns>The index of the color selected.</returns>
		public int getSelectedIndex()
		{
			return this.colorPalette_ != null ? this.colorPalette_.getSelectedIndex() : -1;
		}


		/// <summary>
		/// Sets which color is selected. A value that is out-of-range means that no
		/// color is selected.
		/// </summary>
		/// <param name="ind">The index in this.colors_ of the selected color.</param>
		public void setSelectedIndex(int ind)
		{
			if (this.colorPalette_ != null) {
				this.colorPalette_.setSelectedIndex(ind);
			}
		}


		/// <summary>
		/// Gets the color that is currently selected in this color picker.
		/// </summary>
		/// <returns>The hex string of the color selected, or null if no
		/// color is selected.<returns>
		public string getSelectedColor()
		{
			return this.colorPalette_ != null ? this.colorPalette_.getSelectedColor() : null;
		}


		/// <summary>
		/// Sets which color is selected.  Noop if the color palette hasn't been created
		/// yet.
		/// </summary>
		/// <param name="color">The selected color.</param>
		public void setSelectedColor(string color)
		{
			// TODO(user): This will set the color in the first available palette that
			// contains it
			if (this.colorPalette_ != null) {
				this.colorPalette_.setSelectedColor(color);
			}
		}


		/// <summary>
		/// Returns true if the component is focusable, false otherwise.  The default
		/// is true.  Focusable components always have a tab index and allocate a key
		/// handler to handle keyboard events while focused.
		/// </summary>
		/// <returns>True iff the component is focusable.</returns>
		public bool isFocusable()
		{
			return this.focusable_;
		}


		/// <summary>
		/// Sets whether the component is focusable.  The default is true.
		/// Focusable components always have a tab index and allocate a key handler to
		/// handle keyboard events while focused.
		/// </summary>
		/// <param name="focusable">True iff the component is focusable.</param>
		public void setFocusable(bool focusable)
		{
			this.focusable_ = focusable;
			if (this.colorPalette_ != null) {
				this.colorPalette_.setSupportedState(
					goog.ui.Component.State.FOCUSED, focusable);
			}
		}


		/// <summary>
		/// ColorPickers cannot be used to decorate pre-existing html, since the
		/// structure they build is fairly complicated.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Returns always false.</returns>
		public override bool canDecorate(Element element)
		{
			return false;
		}


		/// <summary>
		/// Renders the color picker inside the provided element. This will override the
		/// current content of the element.
		/// </summary>
		public override void enterDocument()
		{
			base.enterDocument();
			if (this.colorPalette_ != null) {
				this.colorPalette_.render(this.getElement());
			}
			//this.getElement().unselectable = "on";
			this.getElement().SetAttribute("unselectable", "on");
		}


		public override void disposeInternal()
		{
			base.disposeInternal();
			if (this.colorPalette_ != null) {
				this.colorPalette_.dispose();
				this.colorPalette_ = null;
			}
		}


		/// <summary>
		/// Sets the focus to the color picker's palette.
		/// </summary>
		public void focus()
		{
			if (this.colorPalette_ != null) {
				this.colorPalette_.getElement().Focus();
			}
		}


		/// <summary>
		/// Handles actions from the color palette.
		/// </summary>
		/// <param name="e">The event.</param>
		private void onColorPaletteAction_(goog.events.Event e)
		{
			e.stopPropagation();
			this.dispatchEvent(goog.ui.ColorPicker.EventType.CHANGE);
		}


		/// <summary>
		/// Create a color palette for the color picker.
		/// </summary>
		/// <param name="colors">Array of colors.</param>
		private void createColorPalette_(JsArray<string> colors)
		{
			// TODO(user): The color picker should eventually just contain a number of
			// palettes and manage the interactions between them.  This will go away then.
			var cp = new goog.ui.ColorPalette(colors, null, this.getDomHelper());
			cp.setSize(goog.ui.ColorPicker.DEFAULT_NUM_COLS);
			cp.setSupportedState(goog.ui.Component.State.FOCUSED, this.focusable_);
			// TODO(user): Use addChild(cp, true) and remove calls to render.
			this.addChild(cp);
			this.colorPalette_ = cp;
			if (this.isInDocument()) {
				this.colorPalette_.render(this.getElement());
			}
		}


		/// <summary>
		/// Returns an unrendered instance of the color picker.  The colors and layout
		/// are a simple color grid, the same as the old Gmail color picker.
		/// </summary>
		/// <param name="opt_domHelper">Optional DOM helper.</param>
		/// <returns>The unrendered instance.</returns>
		public static goog.ui.ColorPicker createSimpleColorGrid(goog.dom.DomHelper opt_domHelper = null)
		{
			var cp = new goog.ui.ColorPicker(opt_domHelper);
			cp.setSize(7);
			cp.setColors(new JsArray<string>(goog.ui.ColorPicker.SIMPLE_GRID_COLORS));
			return cp;
		}


		/// <summary>
		/// Array of colors for a 7-cell wide simple-grid color picker.
		/// </summary>
		public static string[] SIMPLE_GRID_COLORS = new string[]{
			// grays
			"#ffffff", "#cccccc", "#c0c0c0", "#999999", "#666666", "#333333", "#000000",
			// reds
			"#ffcccc", "#ff6666", "#ff0000", "#cc0000", "#990000", "#660000", "#330000",
			// oranges
			"#ffcc99", "#ff9966", "#ff9900", "#ff6600", "#cc6600", "#993300", "#663300",
			// yellows
			"#ffff99", "#ffff66", "#ffcc66", "#ffcc33", "#cc9933", "#996633", "#663333",
			// olives
			"#ffffcc", "#ffff33", "#ffff00", "#ffcc00", "#999900", "#666600", "#333300",
			// greens
			"#99ff99", "#66ff99", "#33ff33", "#33cc00", "#009900", "#006600", "#003300",
			// turquoises
			"#99ffff", "#33ffff", "#66cccc", "#00cccc", "#339999", "#336666", "#003333",
			// blues
			"#ccffff", "#66ffff", "#33ccff", "#3366ff", "#3333ff", "#000099", "#000066",
			// purples
			"#ccccff", "#9999ff", "#6666cc", "#6633ff", "#6600cc", "#333399", "#330099",
			// violets
			"#ffccff", "#ff99ff", "#cc66cc", "#cc33cc", "#993399", "#663366", "#330033"
		};
	}
}
