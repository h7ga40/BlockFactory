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
 * @fileoverview Object representing a UI bubble.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public class Bubble
	{
		private WorkspaceSvg workspace_;
		private SVGElement content_;
		private SVGElement shape_;
		private double arrow_radians_;

		/// <summary>
		/// Class for UI bubble.
		/// </summary>
		/// <param name="workspace">The workspace on which to draw the
		/// bubble.</param>
		/// <param name="content">SVG content for the bubble.</param>
		/// <param name="shape">SVG element to avoid eclipsing.</param>
		/// <param name="anchorXY">Absolute position of bubble's anchor
		/// point.</param>
		/// <param name="bubbleWidth">Width of bubble, or null if not resizable.</param>
		/// <param name="bubbleHeight">Height of bubble, or null if not resizable.</param>
		public Bubble(WorkspaceSvg workspace, SVGElement content, SVGElement shape,
			goog.math.Coordinate anchorXY, double bubbleWidth, double bubbleHeight)
		{
			this.workspace_ = workspace;
			this.content_ = content;
			this.shape_ = shape;

			var angle = Bubble.ARROW_ANGLE;
			if (this.workspace_.RTL) {
				angle = -angle;
			}
			this.arrow_radians_ = goog.math.toRadians(angle);

			var canvas = workspace.getBubbleCanvas();
			canvas.AppendChild(this.createDom_(content, !(bubbleWidth != 0.0 && bubbleHeight != 0.0)));

			this.setAnchorLocation(anchorXY);
			if (bubbleWidth == 0.0 || bubbleHeight == 0.0) {
				var bBox = /** @type {SVGLocatable} */ (this.content_).getBBox();
				bubbleWidth = bBox.width + 2 * Bubble.BORDER_WIDTH;
				bubbleHeight = bBox.height + 2 * Bubble.BORDER_WIDTH;
			}
			this.setBubbleSize(bubbleWidth, bubbleHeight);

			// Render the bubble.
			this.positionBubble_();
			this.renderArrow_();
			this.rendered_ = true;

			if (!workspace.options.readOnly) {
				Core.bindEventWithChecks_(this.bubbleBack_, "mousedown", this,
								new Action<MouseEvent>(this.bubbleMouseDown_));
				if (this.resizeGroup_ != null) {
					Core.bindEventWithChecks_(this.resizeGroup_, "mousedown", this,
								new Action<MouseEvent>(this.resizeMouseDown_));
				}
			}
		}

		/// <summary>
		/// Width of the border around the bubble.
		/// </summary>
		public static int BORDER_WIDTH = 6;

		/// <summary>
		/// Determines the thickness of the base of the arrow in relation to the size
		/// of the bubble.  Higher numbers result in thinner arrows.
		/// </summary>
		public static int ARROW_THICKNESS = 5;

		/// <summary>
		/// The number of degrees that the arrow bends counter-clockwise.
		/// </summary>
		public static int ARROW_ANGLE = 20;

		/// <summary>
		/// The sharpness of the arrow's bend.  Higher numbers result in smoother arrows.
		/// </summary>
		public static int ARROW_BEND = 4;

		/// <summary>
		/// Distance between arrow point and anchor point.
		/// </summary>
		public static int ANCHOR_RADIUS = 8;

		/// <summary>
		/// Wrapper function called when a mouseUp occurs during a drag operation.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseUpWrapper_;

		/// <summary>
		/// Wrapper function called when a mouseMove occurs during a drag operation.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseMoveWrapper_;

		/// <summary>
		/// Function to call on resize of bubble.
		/// </summary>
		private Action resizeCallback_;

		/// <summary>
		/// Stop binding to the global mouseup and mousemove events.
		/// </summary>
		private static void unbindDragEvents_()
		{
			if (Bubble.onMouseUpWrapper_ != null) {
				Core.unbindEvent_(Bubble.onMouseUpWrapper_);
				Bubble.onMouseUpWrapper_ = null;
			}
			if (Bubble.onMouseMoveWrapper_ != null) {
				Core.unbindEvent_(Bubble.onMouseMoveWrapper_);
				Bubble.onMouseMoveWrapper_ = null;
			}
		}

		/// <summary>
		/// Handle a mouse-up event while dragging a bubble's border or resize handle.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private static void bubbleMouseUp_(MouseEvent e)
		{
			Touch.clearTouchIdentifier();
			Css.setCursor(Css.Cursor.OPEN);
			Bubble.unbindDragEvents_();
		}

		/// <summary>
		/// Flag to stop incremental rendering during construction.
		/// </summary>
		private bool rendered_;

		/// <summary>
		/// Absolute coordinate of anchor point.
		/// </summary>
		private goog.math.Coordinate anchorXY_;

		/// <summary>
		/// Relative X coordinate of bubble with respect to the anchor's centre.
		/// In RTL mode the initial value is negated.
		/// </summary>
		private double relativeLeft_;

		/// <summary>
		/// Relative Y coordinate of bubble with respect to the anchor's centre.
		/// </summary>
		private double relativeTop_;

		/// <summary>
		/// Width of bubble.
		/// </summary>
		private double width_;

		/// <summary>
		/// Height of bubble.
		/// </summary>
		private double height_;

		/// <summary>
		/// Automatically position and reposition the bubble.
		/// </summary>
		private bool autoLayout_;

		private SVGElement bubbleGroup_;
		private SVGElement bubbleArrow_;
		private SVGElement bubbleBack_;
		private SVGElement resizeGroup_;

		/// <summary>
		/// Create the bubble's DOM.
		/// </summary>
		/// <param name="content">SVG content for the bubble.</param>
		/// <param name="hasResize">Add diagonal resize gripper if true.</param>
		/// <returns>The bubble's SVG group.</returns>
		private SVGElement createDom_(SVGElement content, bool hasResize)
		{
			/* Create the bubble.  Here's the markup that will be generated:
			<g>
			  <g filter="url(#blocklyEmbossFilter837493)">
				<path d="... Z" />
				<rect class="blocklyDraggable" rx="8" ry="8" width="180" height="180"/>
			  </g>
			  <g transform="translate(165, 165)" class="blocklyResizeSE">
				<polygon points="0,15 15,15 15,0"/>
				<line class="blocklyResizeLine" x1="5" y1="14" x2="14" y2="5"/>
				<line class="blocklyResizeLine" x1="10" y1="14" x2="14" y2="10"/>
			  </g>
			  [...content goes here...]
			</g>
			*/
			this.bubbleGroup_ = Core.createSvgElement("g", new Dictionary<string, object>(), null);
			var filter = new Dictionary<string, object>() {
				{"filter", "url(#" + this.workspace_.options.embossFilterId + ")"} };
			if (goog.userAgent.getUserAgentString().IndexOf("JavaFX") != -1) {
				// Multiple reports that JavaFX can't handle filters.  UserAgent:
				// Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.44
				//     (KHTML, like Gecko) JavaFX/8.0 Safari/537.44
				// https://github.com/google/blockly/issues/99
				filter = new Dictionary<string, object>();
			}
			var bubbleEmboss = Core.createSvgElement("g",
				filter, this.bubbleGroup_);
			this.bubbleArrow_ = Core.createSvgElement("path", new Dictionary<string, object>(), bubbleEmboss);
			this.bubbleBack_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"class", "blocklyDraggable" }, {"x", 0 }, {"y", 0 },
					{ "rx", Bubble.BORDER_WIDTH }, {"ry", Bubble.BORDER_WIDTH}},
				bubbleEmboss);
			if (hasResize) {
				this.resizeGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
						{"class", this.workspace_.RTL ? "blocklyResizeSW" : "blocklyResizeSE"}},
					this.bubbleGroup_);
				var resizeSize = 2 * Bubble.BORDER_WIDTH;
				Core.createSvgElement("polygon", new Dictionary<string, object>() {
						{"points", "0,x x,x x,0".Replace(new Regex(@"x", RegexOptions.Multiline), resizeSize.ToString())}},
					this.resizeGroup_);
				Core.createSvgElement("line", new Dictionary<string, object>() {
						{"class", "blocklyResizeLine" },
						{ "x1", resizeSize / 3 }, {"y1", resizeSize - 1 },
						{ "x2", resizeSize - 1 }, {"y2", resizeSize / 3}}, this.resizeGroup_);
				Core.createSvgElement("line", new Dictionary<string, object>() {
						{"class", "blocklyResizeLine" },
						{ "x1", resizeSize * 2 / 3 }, {"y1", resizeSize - 1 },
						{ "x2", resizeSize - 1 }, {"y2", resizeSize * 2 / 3}}, this.resizeGroup_);
			}
			else {
				this.resizeGroup_ = null;
			}
			this.bubbleGroup_.AppendChild(content);
			return this.bubbleGroup_;
		}

		/// <summary>
		/// Handle a mouse-down on bubble's border.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		private void bubbleMouseDown_(MouseEvent e)
		{
			this.promote_();
			Bubble.unbindDragEvents_();
			if (Core.isRightButton(e)) {
				// No right-click.
				e.StopPropagation();
				return;
			}
			else if (Core.isTargetInput_(e)) {
				// When focused on an HTML text input widget, don't trap any events.
				return;
			}
			// Left-click (or middle click)
			Css.setCursor(Css.Cursor.CLOSED);

			this.workspace_.startDrag(e, new goog.math.Coordinate(
				this.workspace_.RTL ? -this.relativeLeft_ : this.relativeLeft_,
				this.relativeTop_));

			Bubble.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mouseup", this, new Action<MouseEvent>(Bubble.bubbleMouseUp_));
			Bubble.onMouseMoveWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mousemove", this, new Action<MouseEvent>(this.bubbleMouseMove_));
			Core.hideChaff();
			// This event has been handled.  No need to bubble up to the document.
			e.StopPropagation();
		}

		/// <summary>
		/// Drag this bubble to follow the mouse.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		private void bubbleMouseMove_(MouseEvent e)
		{
			this.autoLayout_ = false;
			var newXY = this.workspace_.moveDrag(e);
			this.relativeLeft_ = this.workspace_.RTL ? -newXY.x : newXY.x;
			this.relativeTop_ = newXY.y;
			this.positionBubble_();
			this.renderArrow_();
		}

		/// <summary>
		/// Handle a mouse-down on bubble's resize corner.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		private void resizeMouseDown_(MouseEvent e)
		{
			this.promote_();
			Bubble.unbindDragEvents_();
			if (Core.isRightButton(e)) {
				// No right-click.
				e.StopPropagation();
				return;
			}
			// Left-click (or middle click)
			Css.setCursor(Css.Cursor.CLOSED);

			this.workspace_.startDrag(e, new goog.math.Coordinate(
				this.workspace_.RTL ? -this.width_ : this.width_, this.height_));

			Bubble.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mouseup", null, new Action<MouseEvent>(Bubble.bubbleMouseUp_));
			Bubble.onMouseMoveWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mousemove", this, new Action<MouseEvent>(this.resizeMouseMove_));
			Core.hideChaff();
			// This event has been handled.  No need to bubble up to the document.
			e.StopPropagation();
		}

		/// <summary>
		/// Resize this bubble to follow the mouse.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		private void resizeMouseMove_(MouseEvent e)
		{
			this.autoLayout_ = false;
			var newXY = this.workspace_.moveDrag(e);
			this.setBubbleSize(this.workspace_.RTL ? -newXY.x : newXY.x, newXY.y);
			if (this.workspace_.RTL) {
				// RTL requires the bubble to move its left edge.
				this.positionBubble_();
			}
		}

		/// <summary>
		/// Register a function as a callback event for when the bubble is resized.
		/// </summary>
		/// <param name="callback">The function to call on resize.</param>
		public void registerResizeEvent(Action callback)
		{
			this.resizeCallback_ = callback;
		}

		/// <summary>
		/// Move this bubble to the top of the stack.
		/// </summary>
		internal void promote_()
		{
			var svgGroup = this.bubbleGroup_.ParentNode;
			svgGroup.AppendChild(this.bubbleGroup_);
		}

		/// <summary>
		/// Notification that the anchor has moved.
		/// Update the arrow and bubble accordingly.
		/// </summary>
		/// <param name="xy">Absolute location.</param>
		public void setAnchorLocation(goog.math.Coordinate xy)
		{
			this.anchorXY_ = xy;
			if (this.rendered_) {
				this.positionBubble_();
			}
		}

		/// <summary>
		/// Position the bubble so that it does not fall off-screen.
		/// </summary>
		private void layoutBubble_()
		{
			// Compute the preferred bubble location.
			var relativeLeft = -this.width_ / 4;
			var relativeTop = -this.height_ - BlockSvg.MIN_BLOCK_Y;
			// Prevent the bubble from being off-screen.
			var metrics = this.workspace_.getMetrics();
			metrics.viewWidth /= this.workspace_.scale;
			metrics.viewLeft /= this.workspace_.scale;
			var anchorX = this.anchorXY_.x;
			if (this.workspace_.RTL) {
				if (anchorX - metrics.viewLeft - relativeLeft - this.width_ <
						Scrollbar.scrollbarThickness) {
					// Slide the bubble right until it is onscreen.
					relativeLeft = anchorX - metrics.viewLeft - this.width_ -
						Scrollbar.scrollbarThickness;
				}
				else if (anchorX - metrics.viewLeft - relativeLeft >
						 metrics.viewWidth) {
					// Slide the bubble left until it is onscreen.
					relativeLeft = anchorX - metrics.viewLeft - metrics.viewWidth;
				}
			}
			else {
				if (anchorX + relativeLeft < metrics.viewLeft) {
					// Slide the bubble right until it is onscreen.
					relativeLeft = metrics.viewLeft - anchorX;
				}
				else if (metrics.viewLeft + metrics.viewWidth <
					anchorX + relativeLeft + this.width_ +
					BlockSvg.SEP_SPACE_X +
					Scrollbar.scrollbarThickness) {
					// Slide the bubble left until it is onscreen.
					relativeLeft = metrics.viewLeft + metrics.viewWidth - anchorX -
						this.width_ - Scrollbar.scrollbarThickness;
				}
			}
			if (this.anchorXY_.y + relativeTop < metrics.viewTop) {
				// Slide the bubble below the block.
				var bBox = /** @type {SVGLocatable} */ (this.shape_).getBBox();
				relativeTop = bBox.height;
			}
			this.relativeLeft_ = relativeLeft;
			this.relativeTop_ = relativeTop;
		}

		/// <summary>
		/// Move the bubble to a location relative to the anchor's centre.
		/// </summary>
		private void positionBubble_()
		{
			var left = this.anchorXY_.x;
			if (this.workspace_.RTL) {
				left -= this.relativeLeft_ + this.width_;
			}
			else {
				left += this.relativeLeft_;
			}
			var top = this.relativeTop_ + this.anchorXY_.y;
			this.bubbleGroup_.SetAttribute("transform",
				"translate(" + left + "," + top + ")");
		}

		/// <summary>
		/// Get the dimensions of this bubble.
		/// </summary>
		/// <returns>Object with width and height properties.</returns>
		public goog.math.Size getBubbleSize()
		{
			return new goog.math.Size { width = this.width_, height = this.height_ };
		}

		/// <summary>
		/// Size this bubble.
		/// </summary>
		/// <param name="width">Width of the bubble.</param>
		/// <param name="height">Height of the bubble.</param>
		public void setBubbleSize(double width, double height)
		{
			var doubleBorderWidth = 2 * Bubble.BORDER_WIDTH;
			// Minimum size of a bubble.
			width = System.Math.Max(width, doubleBorderWidth + 45);
			height = System.Math.Max(height, doubleBorderWidth + 20);
			this.width_ = width;
			this.height_ = height;
			this.bubbleBack_.SetAttribute("width", width.ToString());
			this.bubbleBack_.SetAttribute("height", height.ToString());
			if (this.resizeGroup_ != null) {
				if (this.workspace_.RTL) {
					// Mirror the resize group.
					var resizeSize = 2 * Bubble.BORDER_WIDTH;
					this.resizeGroup_.SetAttribute("transform", "translate(" +
						resizeSize + "," + (height - doubleBorderWidth) + ") scale(-1 1)");
				}
				else {
					this.resizeGroup_.SetAttribute("transform", "translate(" +
						(width - doubleBorderWidth) + "," +
						(height - doubleBorderWidth) + ")");
				}
			}
			if (this.rendered_) {
				if (this.autoLayout_) {
					this.layoutBubble_();
				}
				this.positionBubble_();
				this.renderArrow_();
			}
			// Allow the contents to resize.
			if (this.resizeCallback_ != null) {
				this.resizeCallback_();
			}
		}

		/// <summary>
		/// Draw the arrow between the bubble and the origin.
		/// </summary>
		private void renderArrow_()
		{
			var steps = new JsArray<string>();
			// Find the relative coordinates of the center of the bubble.
			var relBubbleX = this.width_ / 2;
			var relBubbleY = this.height_ / 2;
			// Find the relative coordinates of the center of the anchor.
			var relAnchorX = -this.relativeLeft_;
			var relAnchorY = -this.relativeTop_;
			if (relBubbleX == relAnchorX && relBubbleY == relAnchorY) {
				// Null case.  Bubble is directly on top of the anchor.
				// Short circuit this rather than wade through divide by zeros.
				steps.Push("M " + relBubbleX + "," + relBubbleY);
			}
			else {
				// Compute the angle of the arrow's line.
				var rise = relAnchorY - relBubbleY;
				var run = relAnchorX - relBubbleX;
				if (this.workspace_.RTL) {
					run *= -1;
				}
				var hypotenuse = System.Math.Sqrt(rise * rise + run * run);
				var angle = System.Math.Acos(run / hypotenuse);
				if (rise < 0) {
					angle = 2 * System.Math.PI - angle;
				}
				// Compute a line perpendicular to the arrow.
				var rightAngle = angle + System.Math.PI / 2;
				if (rightAngle > System.Math.PI * 2) {
					rightAngle -= System.Math.PI * 2;
				}
				var rightRise = System.Math.Sin(rightAngle);
				var rightRun = System.Math.Cos(rightAngle);

				// Calculate the thickness of the base of the arrow.
				var bubbleSize = this.getBubbleSize();
				var thickness = (bubbleSize.width + bubbleSize.height) /
								Bubble.ARROW_THICKNESS;
				thickness = System.Math.Min(thickness, System.Math.Min(bubbleSize.width, bubbleSize.height)) / 4;

				// Back the tip of the arrow off of the anchor.
				var backoffRatio = 1 - Bubble.ANCHOR_RADIUS / hypotenuse;
				relAnchorX = relBubbleX + backoffRatio * run;
				relAnchorY = relBubbleY + backoffRatio * rise;

				// Coordinates for the base of the arrow.
				var baseX1 = relBubbleX + thickness * rightRun;
				var baseY1 = relBubbleY + thickness * rightRise;
				var baseX2 = relBubbleX - thickness * rightRun;
				var baseY2 = relBubbleY - thickness * rightRise;

				// Distortion to curve the arrow.
				var swirlAngle = angle + this.arrow_radians_;
				if (swirlAngle > System.Math.PI * 2) {
					swirlAngle -= System.Math.PI * 2;
				}
				var swirlRise = System.Math.Sin(swirlAngle) *
					hypotenuse / Bubble.ARROW_BEND;
				var swirlRun = System.Math.Cos(swirlAngle) *
					hypotenuse / Bubble.ARROW_BEND;

				steps.Push("M" + baseX1 + "," + baseY1);
				steps.Push("C" + (baseX1 + swirlRun) + "," + (baseY1 + swirlRise) +
						   " " + relAnchorX + "," + relAnchorY +
						   " " + relAnchorX + "," + relAnchorY);
				steps.Push("C" + relAnchorX + "," + relAnchorY +
						   " " + (baseX2 + swirlRun) + "," + (baseY2 + swirlRise) +
						   " " + baseX2 + "," + baseY2);
			}
			steps.Push("z");
			this.bubbleArrow_.SetAttribute("d", steps.Join(" "));
		}

		/// <summary>
		/// Change the colour of a bubble.
		/// </summary>
		/// <param name="hexColour">Hex code of colour.</param>
		public void setColour(string hexColour)
		{
			this.bubbleBack_.SetAttribute("fill", hexColour);
			this.bubbleArrow_.SetAttribute("fill", hexColour);
		}

		/// <summary>
		/// Dispose of this bubble.
		/// </summary>
		public void dispose()
		{
			Bubble.unbindDragEvents_();
			// Dispose of and unlink the bubble.
			goog.dom.removeNode(this.bubbleGroup_);
			this.bubbleGroup_ = null;
			this.bubbleArrow_ = null;
			this.bubbleBack_ = null;
			this.resizeGroup_ = null;
			this.workspace_ = null;
			this.content_ = null;
			this.shape_ = null;
		}
	}
}
