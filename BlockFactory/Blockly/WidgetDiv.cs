/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2013 Google Inc.
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
 * @fileoverview A div that floats on top of Blockly.  This singleton contains
 *     temporary HTML UI widgets that the user is currently interacting with.
 *     E.g. text input areas, colour pickers, context menus.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public static class WidgetDiv
	{
		/// <summary>
		/// The HTML container.  Set once by Blockly.WidgetDiv.createDom.
		/// </summary>
		public static HTMLDivElement DIV;

		/// <summary>
		/// The object currently using this container.
		/// </summary>
		private static object owner_;

		/// <summary>
		/// Optional cleanup function set by whichever object uses the widget.
		/// </summary>
		private static Action dispose_;

		/// <summary>
		/// Create the widget div and inject it onto the page.
		/// </summary>
		public static void createDom()
		{
			if (WidgetDiv.DIV != null) {
				return;  // Already created.
			}
			// Create an HTML container for popup overlays (e.g. editor widgets).
			WidgetDiv.DIV = (HTMLDivElement)
				goog.dom.createDom(goog.dom.TagName.DIV, "blocklyWidgetDiv");
			Document.Body.AppendChild(WidgetDiv.DIV);
		}

		/// <summary>
		/// Initialize and display the widget div.  Close the old one if needed.
		/// </summary>
		/// <param name="newOwner">The object that will be using this container.</param>
		/// <param name="rtl">Right-to-left (true) or left-to-right (false).</param>
		/// <param name="dispose">Optional cleanup function to be run when the widget
		/// is closed.</param>
		public static void show(object newOwner, bool rtl, Action dispose)
		{
			WidgetDiv.hide();
			WidgetDiv.owner_ = newOwner;
			WidgetDiv.dispose_ = dispose;
			// Temporarily move the widget to the top of the screen so that it does not
			// cause a scrollbar jump in Firefox when displayed.
			var xy = goog.style.getViewportPageOffset(Document.Instance);
			WidgetDiv.DIV.Style.Top = xy.y + "px";
			WidgetDiv.DIV.Style.Direction = rtl ? Direction.Rtl : Direction.Ltr;
			WidgetDiv.DIV.Style.Display = Display.Block;
		}

		/// <summary>
		/// Destroy the widget and hide the div.
		/// </summary>
		public static void hide(Event e = null)
		{
			if (WidgetDiv.owner_ != null) {
				WidgetDiv.owner_ = null;
				WidgetDiv.DIV.Style.Display = Display.None;
				WidgetDiv.DIV.Style.Left = "";
				WidgetDiv.DIV.Style.Top = "";
				if (WidgetDiv.dispose_ != null) WidgetDiv.dispose_();
				WidgetDiv.dispose_ = null;
				goog.dom.removeChildren(WidgetDiv.DIV);
			}
		}

		/// <summary>
		/// Is the container visible?
		/// </summary>
		/// <returns>True if visible.</returns>
		public static bool isVisible()
		{
			return WidgetDiv.owner_ != null;
		}

		/// <summary>
		/// Destroy the widget and hide the div if it is being used by the specified
		/// object.
		/// </summary>
		/// <param name="oldOwner">The object that was using this container.</param>
		public static void hideIfOwner(object oldOwner)
		{
			if (WidgetDiv.owner_ == oldOwner) {
				WidgetDiv.hide();
			}
		}

		/// <summary>
		/// Position the widget at a given location.  Prevent the widget from going
		/// offscreen top or left (right in RTL).
		/// </summary>
		/// <param name="anchorX">Horizontal location (window coorditates, not body).</param>
		/// <param name="anchorY">Vertical location (window coorditates, not body).</param>
		/// <param name="windowSize">Height/width of window.</param>
		/// <param name="scrollOffset">X/y of window scrollbars.</param>
		/// <param name="rtl">True if RTL, false if LTR.</param>
		public static void position(double anchorX, double anchorY, goog.math.Size windowSize,
			goog.math.Coordinate scrollOffset, bool rtl)
		{
			// Don't let the widget go above the top edge of the window.
			if (anchorY < scrollOffset.y) {
				anchorY = scrollOffset.y;
			}
			if (rtl) {
				// Don't let the widget go right of the right edge of the window.
				if (anchorX > windowSize.width + scrollOffset.x) {
					anchorX = windowSize.width + scrollOffset.x;
				}
			}
			else {
				// Don't let the widget go left of the left edge of the window.
				if (anchorX < scrollOffset.x) {
					anchorX = scrollOffset.x;
				}
			}
			WidgetDiv.DIV.Style.Left = anchorX + "px";
			WidgetDiv.DIV.Style.Top = anchorY + "px";
			WidgetDiv.DIV.Style.Height = windowSize.height + "px";
		}
	}
}
