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
 * @fileoverview A control for representing a palette of colors, that the user
 * can highlight or select via the keyboard or the mouse.
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class ColorPalette : Palette
	{
		/// <summary>
		/// Array of colors to show in the palette.
		/// </summary>
		private JsArray<string> colors_;

		/// <summary>
		/// A color palette is a grid of color swatches that the user can highlight or
		/// select via the keyboard or the mouse.  The selection state of the palette is
		/// controlled by a selection model.  When the user makes a selection, the
		/// component fires an ACTION event.  Event listeners may retrieve the selected
		/// color using the {@link #getSelectedColor} method.
		/// </summary>
		/// <param name="opt_colors">Array of colors in any valid CSS color</param>
		///     format.
		/// <param name="opt_renderer">Renderer used to render or</param>
		///     decorate the palette; defaults to {@link goog.ui.PaletteRenderer}.
		/// <param name="opt_domHelper">Optional DOM helper, used for</param>
		///     document interaction.
		public ColorPalette(JsArray<string> opt_colors = null,
			goog.ui.PaletteRenderer opt_renderer = null, goog.dom.DomHelper opt_domHelper = null)
			: base(null, opt_renderer ?? goog.ui.PaletteRenderer.getInstance(), opt_domHelper)
		{
			this.colors_ = opt_colors ?? new JsArray<string>();

			// Set the colors separately from the super call since we need the correct
			// DomHelper to be initialized for this class.
			this.setColors(this.colors_);
		}
		/// <summary>
		/// Array of normalized colors. Initialized lazily as often never needed.
		/// </summary>
		private JsArray<string> normalizedColors_ = null;

		/// <summary>
		/// Array of labels for the colors. Will be used for the tooltips and
		/// accessibility.
		/// </summary>
		private JsArray<string> labels_ = null;

		/// <summary>
		/// Returns the array of colors represented in the color palette.
		/// </summary>
		/// <returns>Array of colors.</returns>
		public JsArray<string> getColors()
		{
			return this.colors_;
		}

		/// <summary>
		/// Sets the colors that are contained in the palette.
		/// </summary>
		/// <param name="colors">Array of colors in any valid CSS color format.</param>
		/// <param name="opt_labels">The array of labels to be used as</param>
		///        tooltips. When not provided, the color value will be used.
		public void setColors(JsArray<string> colors, JsArray<string> opt_labels = null)
		{
			this.colors_ = colors;
			this.labels_ = opt_labels;
			this.normalizedColors_ = null;
			this.setContent(new ControlContent(this.createColorNodes()));
		}

		/// <summary>
		/// </summary>
		/// <returns>The current selected color in hex, or null.</returns>
		public string getSelectedColor()
		{
			var selectedItem = (HTMLElement)(this.getSelectedItem());
			if (selectedItem != null) {
				var color = goog.style.getStyle(selectedItem, "background-color");
				return goog.ui.ColorPalette.parseColor_(color);
			}
			else {
				return null;
			}
		}

		/// <summary>
		/// Sets the selected color.  Clears the selection if the argument is null or
		/// can't be parsed as a color.
		/// </summary>
		/// <param name="color">The color to set as selected; null clears the</param>
		///     selection.
		public void setSelectedColor(string color)
		{
			var hexColor = goog.ui.ColorPalette.parseColor_(color);
			if (this.normalizedColors_ == null) {
				this.normalizedColors_ = new JsArray<string>(this.colors_.Select((clr) => {
					return goog.ui.ColorPalette.parseColor_(clr);
				}));
			}
			this.setSelectedIndex(
				hexColor != null ? this.normalizedColors_.IndexOf(hexColor) : -1);
		}

		/// <summary>
		/// </summary>
		/// <returns>An array of DOM nodes for each color.</returns>
		protected JsArray<Node> createColorNodes()
		{
			return new JsArray<Node>(this.colors_.Select((color, index) => {
				var swatch = this.getDomHelper().createDom(goog.dom.TagName.DIV, new Dictionary<string, string> {
					{ "class", le.getCssName(this.getRenderer().getCssClass(), "colorswatch") },
					{ "style", "background-color:" + color}
				});
				if (this.labels_ != null && this.labels_[index] != null) {
					swatch.Title = this.labels_[index];
				}
				else {
					swatch.Title = color[0] == '#' ?
						"RGB (" + join_(goog.color.hexToRgb(color), ", ") + ")" :
						color;
				}
				return swatch;
			}));
		}

		private string join_(int[] ints, string dem)
		{
			var sb = new JsArray<string>();
			foreach (int i in ints) {
				sb.Push(i.ToString());
			}
			return sb.Join(dem);
		}

		/// <summary>
		/// Takes a string, attempts to parse it as a color spec, and returns a
		/// normalized hex color spec if successful (null otherwise).
		/// </summary>
		/// <param name="color">String possibly containing a color spec; may be null.</param>
		/// <returns>Normalized hex color spec, or null if the argument can"t
		/// be parsed as a color.<returns>
		private static string parseColor_(string color)
		{
			if (!String.IsNullOrEmpty(color)) {
				try {
					return goog.color.parse(color).hex;
				}
				catch (Exception) {
					// Fall through.
				}
			}
			return null;
		}
	}
}
