/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2011 Google Inc.
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
 * @fileoverview Library to create tooltips for Blockly.
 * First, call Blockly.Tooltip.init() after onload.
 * Second, set the "tooltip" property on any SVG element that needs a tooltip.
 * If the tooltip is a string, then that message will be displayed.
 * If the tooltip is an SVG element, then that object's tooltip will be used.
 * Third, call Blockly.Tooltip.bindMouseEvents(e) passing the SVG element.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Tooltip
	{
		/// <summary>
		/// Is a tooltip currently showing?
		/// </summary>
		public static bool visible;

		/// <summary>
		/// Maximum width (in characters) of a tooltip.
		/// </summary>
		public static int LIMIT = 50;

		/// <summary>
		/// PID of suspended thread to clear tooltip on mouse out.
		/// </summary>
		private static int mouseOutPid_;

		/// <summary>
		/// PID of suspended thread to show the tooltip.
		/// </summary>
		private static int showPid_;

		/// <summary>
		/// Last observed X location of the mouse pointer (freezes when tooltip appears).
		/// </summary>
		private static double lastX_;

		/// <summary>
		/// Last observed Y location of the mouse pointer (freezes when tooltip appears).
		/// </summary>
		private static double lastY_;

		/// <summary>
		/// Current element being pointed at.
		/// </summary>
		private static Union<Element, Block> element_;

		private static Union<string, Delegate> tooltip_;

		/// <summary>
		/// Once a tooltip has opened for an element, that element is 'poisoned' and
		/// cannot respawn a tooltip until the pointer moves over a different element.
		/// </summary>
		private static Union<Element, Block> poisonedElement_;

		/// <summary>
		/// Horizontal offset between mouse cursor and tooltip.
		/// </summary>
		public static int OFFSET_X;

		/// <summary>
		/// Vertical offset between mouse cursor and tooltip.
		/// </summary>
		public static int OFFSET_Y = 10;

		/// <summary>
		/// Radius mouse can move before killing tooltip.
		/// </summary>
		public static int RADIUS_OK = 10;

		/// <summary>
		/// Delay before tooltip appears.
		/// </summary>
		public static int HOVER_MS = 750;

		/// <summary>
		/// Horizontal padding between tooltip and screen edge.
		/// </summary>
		public static int MARGINS = 5;

		/// <summary>
		/// The HTML container.  Set once by Tooltip.createDom.
		/// </summary>
		public static HTMLDivElement DIV;

		/// <summary>
		/// Create the tooltip div and inject it onto the page.
		/// </summary>
		public static void createDom()
		{
			if (Tooltip.DIV != null) {
				return;  // Already created.
			}
			// Create an HTML container for popup overlays (e.g. editor widgets).
			Tooltip.DIV = (HTMLDivElement)
				goog.dom.createDom(goog.dom.TagName.DIV, "blocklyTooltipDiv");
			Document.Body.AppendChild(Tooltip.DIV);
		}

		/// <summary>
		/// Binds the required mouse events onto an SVG element.
		/// </summary>
		/// <param name="element">SVG element onto which tooltip is to be bound.</param>
		public static void bindMouseEvents(Element element)
		{
			Core.bindEvent_(element, "mouseover", null,
				new Action<Event>(Tooltip.onMouseOver_));
			Core.bindEvent_(element, "mouseout", null,
				new Action<Event>(Tooltip.onMouseOut_));

			// Don't use bindEvent_ for mousemove since that would create a
			// corresponding touch handler, even though this only makes sense in the
			// context of a mouseover/mouseout.
			element.AddEventListener("mousemove", new Action<MouseEvent>(Tooltip.onMouseMove_), false);
		}

		/// <summary>
		/// Hide the tooltip if the mouse is over a different object.
		/// Initialize the tooltip to potentially appear for this object.
		/// </summary>
		/// <param name="e">Mouse event.</param>
		private static void onMouseOver_(Event e)
		{
			// If the tooltip is an object, treat it as a pointer to the next object in
			// the chain to look at.  Terminate when a string or function is found.
			object element = e.Target;
			object tooltip = null;
			for (; ; ) {
				if (element is SVGElement)
					tooltip = ((SVGElement)element).tooltip;
				else if (element is Block) {
					tooltip = ((Block)element).tooltip.Value;
				}
				else
					throw new Exception();
				if ((tooltip is string) || (tooltip is Delegate) || (tooltip is Func<string>)
					|| (tooltip is Func<object>))
					break;
				element = tooltip;
			}
			if (Tooltip.element_ != element) {
				Tooltip.hide();
				Tooltip.poisonedElement_ = null;
				Tooltip.element_ = new Union<Element, Block>(element);
				Tooltip.tooltip_ = new Union<string, Delegate>(tooltip);
			}
			// Forget about any immediately preceeding mouseOut event.
			Window.ClearTimeout(Tooltip.mouseOutPid_);
		}

		/// <summary>
		/// Hide the tooltip if the mouse leaves the object and enters the workspace.
		/// </summary>
		/// <param name="e">Mouse event.</param>
		private static void onMouseOut_(Event e)
		{
			// Moving from one element to another (overlapping or with no gap) generates
			// a mouseOut followed instantly by a mouseOver.  Fork off the mouseOut
			// event and kill it if a mouseOver is received immediately.
			// This way the task only fully executes if mousing into the void.
			Tooltip.mouseOutPid_ = Window.SetTimeout(() => {
				Tooltip.element_ = null;
				Tooltip.poisonedElement_ = null;
				Tooltip.hide();
			}, 1);
			Window.ClearTimeout(Tooltip.showPid_);
		}

		/// <summary>
		/// When hovering over an element, schedule a tooltip to be shown.  If a tooltip
		/// is already visible, hide it if the mouse strays out of a certain radius.
		/// </summary>
		/// <param name="e">Mouse event.</param>
		private static void onMouseMove_(MouseEvent e)
		{
			if (Tooltip.element_ == null || Tooltip.tooltip_ == null) {
				// No tooltip here to show.
				return;
			}
			else if (Core.dragMode_ != Core.DRAG_NONE) {
				// Don't display a tooltip during a drag.
				return;
			}
			else if (WidgetDiv.isVisible()) {
				// Don't display a tooltip if a widget is open (tooltip would be under it).
				return;
			}
			if (Tooltip.visible) {
				// Compute the distance between the mouse position when the tooltip was
				// shown and the current mouse position.  Pythagorean theorem.
				var dx = Tooltip.lastX_ - e.PageX;
				var dy = Tooltip.lastY_ - e.PageY;
				if (System.Math.Sqrt(dx * dx + dy * dy) > Tooltip.RADIUS_OK) {
					Tooltip.hide();
				}
			}
			else if (Tooltip.poisonedElement_ != Tooltip.element_) {
				// The mouse moved, clear any previously scheduled tooltip.
				Window.ClearTimeout(Tooltip.showPid_);
				// Maybe this time the mouse will stay put.  Schedule showing of tooltip.
				Tooltip.lastX_ = e.PageX;
				Tooltip.lastY_ = e.PageY;
				Tooltip.showPid_ =
					Window.SetTimeout(Tooltip.show_, Tooltip.HOVER_MS);
			}
		}

		/// <summary>
		/// Hide the tooltip.
		/// </summary>
		public static void hide()
		{
			if (Tooltip.visible) {
				Tooltip.visible = false;
				if (Tooltip.DIV != null) {
					Tooltip.DIV.Style.Display = Display.None;
				}
			}
			Window.ClearTimeout(Tooltip.showPid_);
		}

		/// <summary>
		/// Create the tooltip and show it.
		/// </summary>
		private static void show_()
		{
			Tooltip.poisonedElement_ = Tooltip.element_;
			if (Tooltip.DIV == null) {
				return;
			}
			// Erase all existing text.
			goog.dom.removeChildren(/** @type {!Element} */ (Tooltip.DIV));
			// Get the new text.
			var tip_ = Tooltip.tooltip_.Value;
			while (tip_ is Delegate) {
				tip_ = ((Delegate)tip_).DynamicInvoke();
			}
			var tip = Core.utils.wrap((string)tip_, Tooltip.LIMIT);
			// Create new text, line by line.
			var lines = tip.Split("\n");
			for (var i = 0; i < lines.Length; i++) {
				var div = Document.CreateElement<HTMLDivElement>("div");
				div.AppendChild(Document.CreateTextNode(lines[i]));
				Tooltip.DIV.AppendChild(div);
			}
			bool rtl = Tooltip.element_.Is<Element>() ?
				(bool)Tooltip.element_.As<Element>()["RTL"] : Tooltip.element_.As<Block>().RTL;
			var windowSize = goog.dom.getViewportSize();
			// Display the tooltip.
			Tooltip.DIV.Style.Direction = rtl ? Direction.Rtl : Direction.Ltr;
			Tooltip.DIV.Style.Display = Display.Block;
			Tooltip.visible = true;
			// Move the tooltip to just below the cursor.
			var anchorX = Tooltip.lastX_;
			if (rtl) {
				anchorX -= Tooltip.OFFSET_X + Tooltip.DIV.OffsetWidth;
			}
			else {
				anchorX += Tooltip.OFFSET_X;
			}
			var anchorY = Tooltip.lastY_ + Tooltip.OFFSET_Y;

			if (anchorY + Tooltip.DIV.OffsetHeight >
				windowSize.height + Window.ScrollY) {
				// Falling off the bottom of the screen; shift the tooltip up.
				anchorY -= Tooltip.DIV.OffsetHeight + 2 * Tooltip.OFFSET_Y;
			}
			if (rtl) {
				// Prevent falling off left edge in RTL mode.
				anchorX = System.Math.Max(Tooltip.MARGINS - Window.ScrollX, anchorX);
			}
			else {
				if (anchorX + Tooltip.DIV.OffsetWidth >
					windowSize.width + Window.ScrollX - 2 * Tooltip.MARGINS) {
					// Falling off the right edge of the screen;
					// clamp the tooltip on the edge.
					anchorX = windowSize.width - Tooltip.DIV.OffsetWidth -
						2 * Tooltip.MARGINS;
				}
			}
			Tooltip.DIV.Style.Top = anchorY + "px";
			Tooltip.DIV.Style.Left = anchorX + "px";
		}
	}
}
