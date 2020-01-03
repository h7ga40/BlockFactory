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
 * @fileoverview Angle input field.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldAngle : FieldTextInput
	{
		private SVGElement symbol_;

		/// <summary>
		/// Class for an editable angle field.
		/// </summary>
		/// <param name="text">The initial content of the field.</param>
		/// <param name="opt_validator">An optional function that is called
		/// to validate any constraints on what the user entered.  Takes the new
		/// text as an argument and returns the accepted text or null to abort
		/// the change.</param>
		public FieldAngle(string text, Func<Field, string, object> opt_validator = null)
			: base(text, opt_validator)
		{
			// Add degree symbol: "360°" (LTR) or "°360" (RTL)
			this.symbol_ = Core.createSvgElement("tspan", new Dictionary<string, object>(), null);
			this.symbol_.AppendChild(Document.CreateTextNode("\u00B0"));
		}

		/// <summary>
		/// Sets a new change handler for angle field.
		/// </summary>
		/// <param name="handler">New change handler, or null.</param>
		public override void setValidator(Func<Field, string, object> handler)
		{
			base.setValidator(handler);
		}

		/// <summary>
		/// Round angles to the nearest 15 degrees when using mouse.
		/// Set to 0 to disable rounding.
		/// </summary>
		public const int ROUND = 15;

		/// <summary>
		/// Half the width of protractor image.
		/// </summary>
		public const int HALF = 100 / 2;

		// The following two settings work together to set the behaviour of the angle
		// picker.  While many combinations are possible, two modes are typical:
		// Math mode.
		//   0 deg is right, 90 is up.  This is the style used by protractors.
		//   FieldAngle.CLOCKWISE = false;
		//   FieldAngle.OFFSET = 0;
		// Compass mode.
		//   0 deg is up, 90 is right.  This is the style used by maps.
		//   FieldAngle.CLOCKWISE = true;
		//   FieldAngle.OFFSET = 90;
		//

		/// <summary>
		/// Angle increases clockwise (true) or counterclockwise (false).
		/// </summary>
		public static bool CLOCKWISE = false;

		/// <summary>
		/// Offset the location of 0 degrees (and all angles) by a constant.
		/// Usually either 0 (0 = right) or 90 (0 = up).
		/// </summary>
		public const int OFFSET = 0;

		/// <summary>
		/// Maximum allowed angle before wrapping.
		/// Usually either 360 (for 0 to 359.9) or 180 (for -179.9 to 180).
		/// </summary>
		public const int WRAP = 360;

		/// <summary>
		/// Radius of protractor circle.  Slightly smaller than protractor size since
		/// otherwise SVG crops off half the border at the edges.
		/// </summary>
		public const int RADIUS = HALF - 1;

		private JsArray<EventWrapInfo> clickWrapper_;
		private JsArray<EventWrapInfo> moveWrapper1_;
		private JsArray<EventWrapInfo> moveWrapper2_;

		/// <summary>
		/// Clean up this FieldAngle, as well as the inherited FieldTextInput.
		/// </summary>
		/// <returns>Closure to call on destruction of the WidgetDiv.</returns>
		private Action dispose_()
		{
			var thisField = this;
			return () => {
				thisField.dispose_();
				thisField.gauge_ = null;
				if (thisField.clickWrapper_ != null) {
					Core.unbindEvent_(thisField.clickWrapper_);
				}
				if (thisField.moveWrapper1_ != null) {
					Core.unbindEvent_(thisField.moveWrapper1_);
				}
				if (thisField.moveWrapper2_ != null) {
					Core.unbindEvent_(thisField.moveWrapper2_);
				}
			};
		}

		private SVGElement gauge_;
		private SVGElement line_;

		/// <summary>
		/// Show the inline free-text editor on top of the text.
		/// </summary>
		public override void showEditor_(bool opt_quietInput)
		{
			var noFocus =
				goog.userAgent.MOBILE || goog.userAgent.ANDROID || goog.userAgent.IPAD;
			// Mobile browsers have issues with in-line textareas (focus & keyboards).
			base.showEditor_(noFocus);
			var div = WidgetDiv.DIV;
			if (div.FirstChild == null) {
				// Mobile interface uses window.prompt.
				return;
			}
			// Build the SVG DOM.
			var svg = Core.createSvgElement("svg", new Dictionary<string, object>() {
				{ "xmlns", "http://www.w3.org/2000/svg"},
				{ "xmlns:html", "http://www.w3.org/1999/xhtml" },
				{ "xmlns:xlink", "http://www.w3.org/1999/xlink" },
				{ "version", "1.1" },
				{ "height", (FieldAngle.HALF * 2) + "px" },
				{ "width", (FieldAngle.HALF * 2) + "px" }
				}, div);
			var circle = Core.createSvgElement("circle", new Dictionary<string, object>() {
				{ "cx", FieldAngle.HALF }, {"cy", FieldAngle.HALF },
				{ "r", FieldAngle.RADIUS },
				{ "class", "blocklyAngleCircle" }
			}, svg);
			this.gauge_ = Core.createSvgElement("path", new Dictionary<string, object>() {
				{"class", "blocklyAngleGauge"} }, svg);
			this.line_ = Core.createSvgElement("line", new Dictionary<string, object>() {
				{"x1", FieldAngle.HALF },
				{ "y1", FieldAngle.HALF },
				{ "class", "blocklyAngleLine" } }, svg);
			// Draw markers around the edge.
			for (var angle = 0; angle < 360; angle += 15) {
				Core.createSvgElement("line", new Dictionary<string, object>() {
				{ "x1", FieldAngle.HALF + FieldAngle.RADIUS },
				{ "y1", FieldAngle.HALF },
				{ "x2", FieldAngle.HALF + FieldAngle.RADIUS - (angle % 45 == 0 ? 10 : 5) },
				{ "y2", FieldAngle.HALF },
				{ "class", "blocklyAngleMarks" },
				{ "transform", "rotate(" + angle + "," + FieldAngle.HALF + "," + FieldAngle.HALF + ")" }
			}, svg);
			}
			svg.style.MarginLeft = (15 - FieldAngle.RADIUS) + "px";

			// The angle picker is different from other fields in that it updates on
			// mousemove even if it's not in the middle of a drag.  In future we may
			// change this behavior.  For now, using bindEvent_ instead of
			// bindEventWithChecks_ allows it to work without a mousedown/touchstart.
			this.clickWrapper_ =
				Core.bindEvent_(svg, "click", this, new Action<Event>(WidgetDiv.hide));
			this.moveWrapper1_ =
				Core.bindEvent_(circle, "mousemove", this, new Action<MouseEvent>(this.onMouseMove));
			this.moveWrapper2_ =
				Core.bindEvent_(this.gauge_, "mousemove", this,
				new Action<MouseEvent>(this.onMouseMove));
			this.updateGraph_();
		}

		/// <summary>
		/// Set the angle to match the mouse's position.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		public void onMouseMove(MouseEvent e)
		{
			var bBox = this.gauge_.ownerSVGElement.GetBoundingClientRect();
			var dx = e.ClientX - bBox.Left - FieldAngle.HALF;
			var dy = e.ClientY - bBox.Top - FieldAngle.HALF;
			var angle = System.Math.Atan(-dy / dx);
			if (Double.IsNaN(angle)) {
				// This shouldn't happen, but let's not let this error propogate further.
				return;
			}
			angle = goog.math.toDegrees(angle);
			// 0: East, 90: North, 180: West, 270: South.
			if (dx < 0) {
				angle += 180;
			}
			else if (dy > 0) {
				angle += 360;
			}
			if (FieldAngle.CLOCKWISE) {
				angle = FieldAngle.OFFSET + 360 - angle;
			}
			else {
				angle -= FieldAngle.OFFSET;
			}
			if (FieldAngle.ROUND != 0) {
				angle = System.Math.Round(angle / FieldAngle.ROUND) *
					FieldAngle.ROUND;
			}
			var angles = this.callValidator(angle.ToString());
			FieldTextInput.htmlInput_.Value = angles;
			this.setValue(angles);
			this.validate_();
			this.resizeEditor_();
		}

		/// <summary>
		/// Insert a degree symbol.
		/// </summary>
		/// <param name="text">New text.</param>
		public override void setText(string text)
		{
			base.setText(text);
			if (this.textElement_ == null) {
				// Not rendered yet.
				return;
			}
			this.updateGraph_();
			// Insert degree symbol.
			if (this.sourceBlock_.RTL) {
				this.textElement_.InsertBefore(this.symbol_, this.textElement_.FirstChild);
			}
			else {
				this.textElement_.AppendChild(this.symbol_);
			}
			// Cached width is obsolete.  Clear it.
			this.size_.width = 0;
		}

		/// <summary>
		/// Redraw the graph with the current angle.
		/// </summary>
		public void updateGraph_()
		{
			if (this.gauge_ == null) {
				return;
			}
			var angleDegrees = Script.ParseFloat(this.getText()) + FieldAngle.OFFSET;
			var angleRadians = goog.math.toRadians(angleDegrees);
			var path = new JsArray<string> { "M " + FieldAngle.HALF + "," + FieldAngle.HALF };
			double x2 = FieldAngle.HALF;
			double y2 = FieldAngle.HALF;
			if (!Double.IsNaN(angleRadians)) {
				var angle1 = goog.math.toRadians(FieldAngle.OFFSET);
				var x1 = System.Math.Cos(angle1) * FieldAngle.RADIUS;
				var y1 = System.Math.Sin(angle1) * -FieldAngle.RADIUS;
				if (FieldAngle.CLOCKWISE) {
					angleRadians = 2 * angle1 - angleRadians;
				}
				x2 += System.Math.Cos(angleRadians) * FieldAngle.RADIUS;
				y2 -= System.Math.Sin(angleRadians) * FieldAngle.RADIUS;
				// Don't ask how the flag calculations work.  They just do.
				var largeFlag = System.Math.Abs(System.Math.Floor((angleRadians - angle1) / System.Math.PI) % 2);
				if (FieldAngle.CLOCKWISE) {
					largeFlag = 1 - largeFlag;
				}
				var sweepFlag = FieldAngle.CLOCKWISE ? 1 : 0;
				path.Push(" l " + x1 + "," + y1 +
					" A " + FieldAngle.RADIUS + "," + FieldAngle.RADIUS +
					" 0 " + largeFlag + " " + sweepFlag + " " + x2 + "," + y2 + " z");
			}
			this.gauge_.SetAttribute("d", path.Join(""));
			this.line_.SetAttribute("x2", x2.ToString());
			this.line_.SetAttribute("y2", y2.ToString());
		}

		/// <summary>
		/// Ensure that only an angle may be entered.
		/// </summary>
		/// <param name="text">The user's text.</param>
		/// <returns>A string representing a valid angle, or null if invalid.</returns>
		public override object classValidator(string text)
		{
			if (text == null) {
				return null;
			}
			var n = Script.ParseFloat(text ?? "0");
			if (Double.IsNaN(n)) {
				return null;
			}
			n = n % 360;
			if (n < 0) {
				n += 360;
			}
			if (n > FieldAngle.WRAP) {
				n -= 360;
			}
			return n.ToString();
		}
	}
}
