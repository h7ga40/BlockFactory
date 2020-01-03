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
 * @fileoverview Library for creating scrollbars.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class ScrollbarPair
	{
		WorkspaceSvg workspace_;
		public Scrollbar hScroll;
		public Scrollbar vScroll;
		private SVGElement corner_;

		public ScrollbarPair(WorkspaceSvg workspace)
		{
			this.workspace_ = workspace;
			this.hScroll = new Scrollbar(workspace, true, true);
			this.vScroll = new Scrollbar(workspace, false, true);
			this.corner_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"height", Scrollbar.scrollbarThickness },
				{ "width", Scrollbar.scrollbarThickness },
				{ "class", "blocklyScrollbarBackground"}}, null);
			Scrollbar.insertAfter_(this.corner_, workspace.getBubbleCanvas());
		}

		/// <summary>
		/// Previously recorded metrics from the workspace.
		/// </summary>
		private Metrics oldHostMetrics_;

		/// <summary>
		/// Dispose of this pair of scrollbars.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public void dispose()
		{
			goog.dom.removeNode(this.corner_);
			this.corner_ = null;
			this.workspace_ = null;
			this.oldHostMetrics_ = null;
			this.hScroll.dispose();
			this.hScroll = null;
			this.vScroll.dispose();
			this.vScroll = null;
		}

		/// <summary>
		/// Recalculate both of the scrollbars' locations and lengths.
		/// Also reposition the corner rectangle.
		/// </summary>
		public void resize()
		{
			// Look up the host metrics once, and use for both scrollbars.
			var hostMetrics = this.workspace_.getMetrics();
			if (hostMetrics == null) {
				// Host element is likely not visible.
				return;
			}

			// Only change the scrollbars if there has been a change in metrics.
			var resizeH = false;
			var resizeV = false;
			if (this.oldHostMetrics_ == null ||
				this.oldHostMetrics_.viewWidth != hostMetrics.viewWidth ||
				this.oldHostMetrics_.viewHeight != hostMetrics.viewHeight ||
				this.oldHostMetrics_.absoluteTop != hostMetrics.absoluteTop ||
				this.oldHostMetrics_.absoluteLeft != hostMetrics.absoluteLeft) {
				// The window has been resized or repositioned.
				resizeH = true;
				resizeV = true;
			}
			else {
				// Has the content been resized or moved?
				if (this.oldHostMetrics_ == null ||
					this.oldHostMetrics_.contentWidth != hostMetrics.contentWidth ||
					this.oldHostMetrics_.viewLeft != hostMetrics.viewLeft ||
					this.oldHostMetrics_.contentLeft != hostMetrics.contentLeft) {
					resizeH = true;
				}
				if (this.oldHostMetrics_ == null ||
					this.oldHostMetrics_.contentHeight != hostMetrics.contentHeight ||
					this.oldHostMetrics_.viewTop != hostMetrics.viewTop ||
					this.oldHostMetrics_.contentTop != hostMetrics.contentTop) {
					resizeV = true;
				}
			}
			if (resizeH) {
				this.hScroll.resize(hostMetrics);
			}
			if (resizeV) {
				this.vScroll.resize(hostMetrics);
			}

			// Reposition the corner square.
			if (this.oldHostMetrics_ == null ||
				this.oldHostMetrics_.viewWidth != hostMetrics.viewWidth ||
				this.oldHostMetrics_.absoluteLeft != hostMetrics.absoluteLeft) {
				this.corner_.SetAttribute("x", this.vScroll.position_.x.ToString());
			}
			if (this.oldHostMetrics_ == null ||
				this.oldHostMetrics_.viewHeight != hostMetrics.viewHeight ||
				this.oldHostMetrics_.absoluteTop != hostMetrics.absoluteTop) {
				this.corner_.SetAttribute("y", this.hScroll.position_.y.ToString());
			}

			// Cache the current metrics to potentially short-cut the next resize event.
			this.oldHostMetrics_ = hostMetrics;
		}

		/// <summary>
		/// Set the sliders of both scrollbars to be at a certain position.
		/// </summary>
		/// <param name="x">Horizontal scroll value.</param>
		/// <param name="y">Vertical scroll value.</param>
		public void set(double x, double y)
		{
			// This function is equivalent to:
			//   this.hScroll.set(x);
			//   this.vScroll.set(y);
			// However, that calls setMetrics twice which causes a chain of
			// getAttribute->setAttribute->getAttribute resulting in an extra layout pass.
			// Combining them speeds up rendering.
			var xyRatio = new Metrics();

			var hHandlePosition = x * this.hScroll.ratio_;
			var vHandlePosition = y * this.vScroll.ratio_;

			var hBarLength = this.hScroll.scrollViewSize_;
			var vBarLength = this.vScroll.scrollViewSize_;

			xyRatio.x = this.getRatio_(hHandlePosition, hBarLength);
			xyRatio.y = this.getRatio_(vHandlePosition, vBarLength);
			this.workspace_.setMetrics(xyRatio);

			this.hScroll.setHandlePosition(hHandlePosition);
			this.vScroll.setHandlePosition(vHandlePosition);
		}

		/// <summary>
		/// Helper to calculate the ratio of handle position to scrollbar view size.
		/// </summary>
		/// <param name="handlePosition">The value of the handle.</param>
		/// <param name="viewSize">The total size of the scrollbar's view.</param>
		/// <returns>Ratio.</returns>
		private double getRatio_(double handlePosition, double viewSize)
		{
			var ratio = handlePosition / viewSize;
			if (Double.IsNaN(ratio)) {
				return 0;
			}
			return ratio;
		}
	}

	// --------------------------------------------------------------------

	public class Scrollbar
	{
		private WorkspaceSvg workspace_;
		private bool horizontal_;
		private bool pair_;
		private Metrics oldHostMetrics_;
		/// <summary>
		/// The upper left corner of the scrollbar's svg group.
		/// </summary>
		internal goog.math.Coordinate position_;
		private SVGElement svgBackground_;

		public Scrollbar(WorkspaceSvg workspace, bool horizontal, bool opt_pair = false)
		{
			this.workspace_ = workspace;
			this.pair_ = opt_pair;
			this.horizontal_ = horizontal;
			this.oldHostMetrics_ = null;

			this.createDom_();

			this.position_ = new goog.math.Coordinate(0, 0);

			if (horizontal) {
				this.svgBackground_.SetAttribute("height",
					Scrollbar.scrollbarThickness.ToString());
				this.svgHandle_.SetAttribute("height",
					(Scrollbar.scrollbarThickness - 5).ToString());
				this.svgHandle_.SetAttribute("y", 2.5.ToString());

				this.lengthAttribute_ = "width";
				this.positionAttribute_ = "x";
			}
			else {
				this.svgBackground_.SetAttribute("width",
					Scrollbar.scrollbarThickness.ToString());
				this.svgHandle_.SetAttribute("width",
					(Scrollbar.scrollbarThickness - 5).ToString());
				this.svgHandle_.SetAttribute("x", 2.5.ToString());

				this.lengthAttribute_ = "height";
				this.positionAttribute_ = "y";
			}
			var scrollbar = this;
			this.onMouseDownBarWrapper_ = Core.bindEventWithChecks_(
				this.svgBackground_, "mousedown", scrollbar, new Action<MouseEvent>(scrollbar.onMouseDownBar_));
			this.onMouseDownHandleWrapper_ = Core.bindEventWithChecks_(this.svgHandle_,
				"mousedown", scrollbar, new Action<MouseEvent>(scrollbar.onMouseDownHandle_));
		}

		/// <summary>
		/// The size of the area within which the scrollbar handle can move.
		/// </summary>
		internal double scrollViewSize_;

		/// <summary>
		/// The length of the scrollbar handle.
		/// </summary>
		private double handleLength_;

		/// <summary>
		/// The offset of the start of the handle from the start of the scrollbar range.
		/// </summary>
		private double handlePosition_;

		/// <summary>
		/// Whether the scrollbar handle is visible.
		/// </summary>
		private bool isVisible_ = true;

		/// <summary>
		/// Width of vertical scrollbar or height of horizontal scrollbar.
		/// Increase the size of scrollbars on touch devices.
		/// Don't define if there is no document object (e.g. node.js).
		/// </summary>
		public static double scrollbarThickness = 15;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="first">An object containing computed measurements of a
		/// workspace.</param>
		/// <param name="second">Another object containing computed measurements of a
		/// workspace.</param>
		/// <returns>Whether the two sets of metrics are equivalent.</returns>
		private static bool metricsAreEquivalent_(Metrics first, Metrics second)
		{
			if (first == null || second == null) {
				return false;
			}

			if (first.viewWidth != second.viewWidth ||
				first.viewHeight != second.viewHeight ||
				first.viewLeft != second.viewLeft ||
				first.viewTop != second.viewTop ||
				first.absoluteTop != second.absoluteTop ||
				first.absoluteLeft != second.absoluteLeft ||
				first.contentWidth != second.contentWidth ||
				first.contentHeight != second.contentHeight ||
				first.contentLeft != second.contentLeft ||
				first.contentTop != second.contentTop) {
				return false;
			}

			return true;
		}

		private SVGElement svgGroup_;
		private SVGElement svgHandle_;
		private JsArray<EventWrapInfo> onMouseDownBarWrapper_;
		private JsArray<EventWrapInfo> onMouseDownHandleWrapper_;

		/// <summary>
		/// Dispose of this scrollbar.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public void dispose()
		{
			this.cleanUp_();
			Core.unbindEvent_(this.onMouseDownBarWrapper_);
			this.onMouseDownBarWrapper_ = null;
			Core.unbindEvent_(this.onMouseDownHandleWrapper_);
			this.onMouseDownHandleWrapper_ = null;

			goog.dom.removeNode(this.svgGroup_);
			this.svgGroup_ = null;
			this.svgBackground_ = null;
			this.svgHandle_ = null;
			this.workspace_ = null;
		}

		/// <summary>
		/// Set the length of the scrollbar's handle and change the SVG attribute
		/// accordingly.
		/// </summary>
		/// <param name="newLength">The new scrollbar handle length.</param>
		private void setHandleLength_(double newLength)
		{
			this.handleLength_ = newLength;
			this.svgHandle_.SetAttribute(this.lengthAttribute_, this.handleLength_.ToString());
		}

		/// <summary>
		/// Set the offset of the scrollbar's handle and change the SVG attribute
		/// accordingly.
		/// </summary>
		/// <param name="newPosition"></param>
		public void setHandlePosition(double newPosition)
		{
			this.handlePosition_ = newPosition;
			this.svgHandle_.SetAttribute(this.positionAttribute_, this.handlePosition_.ToString());
		}

		/// <summary>
		/// Set the size of the scrollbar's background and change the SVG attribute
		/// accordingly.
		/// </summary>
		/// <param name="newSize"></param>
		private void setScrollViewSize_(double newSize)
		{
			this.scrollViewSize_ = newSize;
			this.svgBackground_.SetAttribute(this.lengthAttribute_, this.scrollViewSize_.ToString());
		}

		/// <summary>
		/// Set the position of the scrollbar's svg group.
		/// </summary>
		/// <param name="x">The new x coordinate.</param>
		/// <param name="y">The new y coordinate.</param>
		public void setPosition(double x, double y)
		{
			this.position_.x = x;
			this.position_.y = y;

			this.svgGroup_.SetAttribute("transform",
				"translate(" + this.position_.x + "," + this.position_.y + ")");
		}

		/// <summary>
		/// Recalculate the scrollbar's location and its length.
		/// </summary>
		/// <param name="opt_metrics">A data structure of from the describing all the
		/// required dimensions.  If not provided, it will be fetched from the host
		/// object.</param>
		public void resize(Metrics opt_metrics = null)
		{
			// Determine the location, height and width of the host element.
			var hostMetrics = opt_metrics;
			if (hostMetrics == null) {
				hostMetrics = this.workspace_.getMetrics();
				if (hostMetrics == null) {
					// Host element is likely not visible.
					return;
				}
			}

			if (Scrollbar.metricsAreEquivalent_(hostMetrics,
				this.oldHostMetrics_)) {
				return;
			}
			this.oldHostMetrics_ = hostMetrics;

			/* hostMetrics is an object with the following properties.
			 * .viewHeight: Height of the visible rectangle,
			 * .viewWidth: Width of the visible rectangle,
			 * .contentHeight: Height of the contents,
			 * .contentWidth: Width of the content,
			 * .viewTop: Offset of top edge of visible rectangle from parent,
			 * .viewLeft: Offset of left edge of visible rectangle from parent,
			 * .contentTop: Offset of the top-most content from the y=0 coordinate,
			 * .contentLeft: Offset of the left-most content from the x=0 coordinate,
			 * .absoluteTop: Top-edge of view.
			 * .absoluteLeft: Left-edge of view.
			 */
			if (this.horizontal_) {
				this.resizeHorizontal_(hostMetrics);
			}
			else {
				this.resizeVertical_(hostMetrics);
			}
			// Resizing may have caused some scrolling.
			this.onScroll_();
		}

		/// <summary>
		/// Recalculate a horizontal scrollbar's location and length.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		private void resizeHorizontal_(Metrics hostMetrics)
		{
			// TODO: Inspect metrics to determine if we can get away with just a content
			// resize.
			this.resizeViewHorizontal(hostMetrics);
		}

		/// <summary>
		/// Recalculate a horizontal scrollbar's location on the screen and path length.
		/// This should be called when the layout or size of the window has changed.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		public void resizeViewHorizontal(Metrics hostMetrics)
		{
			var viewSize = hostMetrics.viewWidth - 1;
			if (this.pair_) {
				// Shorten the scrollbar to make room for the corner square.
				viewSize -= Scrollbar.scrollbarThickness;
			}
			this.setScrollViewSize_(System.Math.Max(0, viewSize));

			var xCoordinate = hostMetrics.absoluteLeft + 0.5;
			if (this.pair_ && this.workspace_.RTL) {
				xCoordinate += Scrollbar.scrollbarThickness;
			}

			// Horizontal toolbar should always be just above the bottom of the workspace.
			var yCoordinate = hostMetrics.absoluteTop + hostMetrics.viewHeight -
				Scrollbar.scrollbarThickness - 0.5;
			this.setPosition(xCoordinate, yCoordinate);

			// If the view has been resized, a content resize will also be necessary.  The
			// reverse is not true.
			this.resizeContentHorizontal(hostMetrics);
		}

		internal double ratio_;

		/// <summary>
		/// Recalculate a horizontal scrollbar's location within its path and length.
		/// This should be called when the contents of the workspace have changed.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		public void resizeContentHorizontal(Metrics hostMetrics)
		{
			if (!this.pair_) {
				// Only show the scrollbar if needed.
				// Ideally this would also apply to scrollbar pairs, but that's a bigger
				// headache (due to interactions with the corner square).
				this.setVisible(this.scrollViewSize_ < hostMetrics.contentWidth);
			}

			this.ratio_ = this.scrollViewSize_ / hostMetrics.contentWidth;
			if (this.ratio_ == (double)Double.NegativeInfinity || this.ratio_ == (double)Double.PositiveInfinity ||
				Double.IsNaN(this.ratio_)) {
				this.ratio_ = 0;
			}

			var handleLength = hostMetrics.viewWidth * this.ratio_;
			this.setHandleLength_(System.Math.Max(0, handleLength));

			var handlePosition = (hostMetrics.viewLeft - hostMetrics.contentLeft) *
				this.ratio_;
			this.setHandlePosition(this.constrainHandle_(handlePosition));
		}

		/// <summary>
		/// Recalculate a vertical scrollbar's location and length.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		private void resizeVertical_(Metrics hostMetrics)
		{
			// TODO: Inspect metrics to determine if we can get away with just a content
			// resize.
			this.resizeViewVertical(hostMetrics);
		}

		/// <summary>
		/// Recalculate a vertical scrollbar's location on the screen and path length.
		/// This should be called when the layout or size of the window has changed.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		public void resizeViewVertical(Metrics hostMetrics)
		{
			var viewSize = hostMetrics.viewHeight - 1;
			if (this.pair_) {
				// Shorten the scrollbar to make room for the corner square.
				viewSize -= Scrollbar.scrollbarThickness;
			}
			this.setScrollViewSize_(System.Math.Max(0, viewSize));

			var xCoordinate = hostMetrics.absoluteLeft + 0.5;
			if (!this.workspace_.RTL) {
				xCoordinate += hostMetrics.viewWidth -
					Scrollbar.scrollbarThickness - 1;
			}
			var yCoordinate = hostMetrics.absoluteTop + 0.5;
			this.setPosition(xCoordinate, yCoordinate);

			// If the view has been resized, a content resize will also be necessary.  The
			// reverse is not true.
			this.resizeContentVertical(hostMetrics);
		}

		/// <summary>
		/// Recalculate a vertical scrollbar's location within its path and length.
		/// This should be called when the contents of the workspace have changed.
		/// </summary>
		/// <param name="hostMetrics">A data structure describing all the
		/// required dimensions, possibly fetched from the host object.</param>
		public void resizeContentVertical(Metrics hostMetrics)
		{
			if (!this.pair_) {
				// Only show the scrollbar if needed.
				this.setVisible(this.scrollViewSize_ < hostMetrics.contentHeight);
			}

			this.ratio_ = this.scrollViewSize_ / hostMetrics.contentHeight;
			if (this.ratio_ == Double.NegativeInfinity || this.ratio_ == Double.PositiveInfinity ||
				Double.IsNaN(this.ratio_)) {
				this.ratio_ = 0;
			}

			var handleLength = hostMetrics.viewHeight * this.ratio_;
			this.setHandleLength_(System.Math.Max(0, handleLength));

			var handlePosition = (hostMetrics.viewTop - hostMetrics.contentTop) *
				this.ratio_;
			this.setHandlePosition(this.constrainHandle_(handlePosition));
		}

		/// <summary>
		/// Create all the DOM elements required for a scrollbar.
		/// The resulting widget is not sized.
		/// </summary>
		private void createDom_()
		{
			/* Create the following DOM:
			<g class="blocklyScrollbarHorizontal">
			  <rect class="blocklyScrollbarBackground" />
			  <rect class="blocklyScrollbarHandle" rx="8" ry="8" />
			</g>
			*/
			var className = "blocklyScrollbar" +
				(this.horizontal_ ? "Horizontal" : "Vertical");
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() { { "class", className } }, null);
			this.svgBackground_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"class", "blocklyScrollbarBackground"} }, this.svgGroup_);
			var radius = System.Math.Floor((Scrollbar.scrollbarThickness - 5) / 2);
			this.svgHandle_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"class", "blocklyScrollbarHandle" }, {"rx", radius }, {"ry", radius} },
				this.svgGroup_);
			Scrollbar.insertAfter_(this.svgGroup_,
										   this.workspace_.getBubbleCanvas());
		}

		/// <summary>
		/// Is the scrollbar visible.  Non-paired scrollbars disappear when they aren't
		/// needed.
		/// </summary>
		/// <returns>True if visible.</returns>
		public bool isVisible()
		{
			return this.isVisible_;
		}

		/// <summary>
		/// Set whether the scrollbar is visible.
		/// Only applies to non-paired scrollbars.
		/// </summary>
		/// <param name="visible">True if visible.</param>
		public void setVisible(bool visible)
		{
			if (visible == this.isVisible()) {
				return;
			}
			// Ideally this would also apply to scrollbar pairs, but that's a bigger
			// headache (due to interactions with the corner square).
			if (this.pair_) {
				throw new Exception("Unable to toggle visibility of paired scrollbars.");
			}

			this.isVisible_ = visible;

			if (visible) {
				this.svgGroup_.SetAttribute("display", "block");
			}
			else {
				// Hide the scrollbar.
				this.workspace_.setMetrics(new Metrics { x = 0, y = 0 });
				this.svgGroup_.SetAttribute("display", "none");
			}
		}

		/// <summary>
		/// Scroll by one pageful.
		/// Called when scrollbar background is clicked.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		private void onMouseDownBar_(MouseEvent e)
		{
			Touch.clearTouchIdentifier();  // This is really a click.
			this.cleanUp_();
			if (Core.isRightButton(e)) {
				// Right-click.
				// Scrollbars have no context menu.
				e.StopPropagation();
				return;
			}
			var mouseXY = Core.mouseToSvg(e, this.workspace_.getParentSvg(),
				this.workspace_.getInverseScreenCTM());
			var mouseLocation = this.horizontal_ ? mouseXY.x : mouseXY.y;

			var handleXY = Core.getSvgXY_(this.svgHandle_, this.workspace_);
			var handleStart = this.horizontal_ ? handleXY.x : handleXY.y;
			var handlePosition = this.handlePosition_;

			var pageLength = this.handleLength_ * 0.95;
			if (mouseLocation <= handleStart) {
				// Decrease the scrollbar's value by a page.
				handlePosition -= pageLength;
			}
			else if (mouseLocation >= handleStart + this.handleLength_) {
				// Increase the scrollbar's value by a page.
				handlePosition += pageLength;
			}

			this.setHandlePosition(this.constrainHandle_(handlePosition));

			this.onScroll_();
			e.StopPropagation();
			e.PreventDefault();
		}

		public double startDragHandle;
		public double startDragMouse;
		private static JsArray<EventWrapInfo> onMouseUpWrapper_;
		private static JsArray<EventWrapInfo> onMouseMoveWrapper_;
		private string lengthAttribute_;
		private string positionAttribute_;

		/// <summary>
		/// Start a dragging operation.
		/// Called when scrollbar handle is clicked.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		private void onMouseDownHandle_(MouseEvent e)
		{
			this.cleanUp_();
			if (Core.isRightButton(e)) {
				// Right-click.
				// Scrollbars have no context menu.
				e.StopPropagation();
				return;
			}
			// Look up the current translation and record it.
			this.startDragHandle = this.handlePosition_;
			// Record the current mouse position.
			this.startDragMouse = this.horizontal_ ? e.ClientX : e.ClientY;
			Scrollbar.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mouseup", this, new Action<Event>(this.onMouseUpHandle_));
			Scrollbar.onMouseMoveWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mousemove", this, new Action<MouseEvent>(this.onMouseMoveHandle_));
			e.StopPropagation();
			e.PreventDefault();
		}

		/// <summary>
		/// Drag the scrollbar's handle.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void onMouseMoveHandle_(MouseEvent e)
		{
			var currentMouse = this.horizontal_ ? e.ClientX : e.ClientY;
			var mouseDelta = currentMouse - this.startDragMouse;
			var handlePosition = this.startDragHandle + mouseDelta;
			// Position the bar.
			this.setHandlePosition(this.constrainHandle_(handlePosition));
			this.onScroll_();
		}

		/// <summary>
		/// Release the scrollbar handle and reset state accordingly.
		/// </summary>
		private void onMouseUpHandle_(Event e)
		{
			Touch.clearTouchIdentifier();
			this.cleanUp_();
		}

		/// <summary>
		/// Hide chaff and stop binding to mouseup and mousemove events.  Call this to
		/// wrap up lose ends associated with the scrollbar.
		/// </summary>
		public void cleanUp_()
		{
			Core.hideChaff(true);
			if (Scrollbar.onMouseUpWrapper_ != null) {
				Core.unbindEvent_(Scrollbar.onMouseUpWrapper_);
				Scrollbar.onMouseUpWrapper_ = null;
			}
			if (Scrollbar.onMouseMoveWrapper_ != null) {
				Core.unbindEvent_(Scrollbar.onMouseMoveWrapper_);
				Scrollbar.onMouseMoveWrapper_ = null;
			}
		}

		/// <summary>
		/// Constrain the handle's position within the minimum (0) and maximum
		/// (length of scrollbar) values allowed for the scrollbar.
		/// </summary>
		/// <param name="value">Value that is potentially out of bounds.</param>
		/// <returns>Constrained value.</returns>
		private double constrainHandle_(double value)
		{
			if (value <= 0 || Double.IsNaN(value) || this.scrollViewSize_ < this.handleLength_) {
				value = 0;
			}
			else {
				value = System.Math.Min(value, this.scrollViewSize_ - this.handleLength_);
			}
			return value;
		}

		/// <summary>
		/// Called when scrollbar is moved.
		/// </summary>
		private void onScroll_()
		{
			var ratio = this.handlePosition_ / this.scrollViewSize_;
			if (Double.IsNaN(ratio)) {
				ratio = 0;
			}
			var xyRatio = new Metrics();
			if (this.horizontal_) {
				xyRatio.x = ratio;
			}
			else {
				xyRatio.y = ratio;
			}
			this.workspace_.setMetrics(xyRatio);
		}

		/// <summary>
		/// Set the scrollbar slider's position.
		/// </summary>
		/// <param name="value">The distance from the top/left end of the bar.</param>
		public void set(double value)
		{
			this.setHandlePosition(this.constrainHandle_(value * this.ratio_));
			this.onScroll_();
		}

		/// <summary>
		/// Insert a node after a reference node.
		/// Contrast with node.insertBefore function.
		/// </summary>
		/// <param name="newNode">New element to insert.</param>
		/// <param name="refNode">Existing element to precede new node.</param>
		internal static void insertAfter_(SVGElement newNode, SVGElement refNode)
		{
			var siblingNode = refNode.NextSibling;
			var parentNode = refNode.ParentNode;
			if (parentNode == null) {
				throw new Exception("Reference node has no parent.");
			}
			if (siblingNode != null) {
				parentNode.InsertBefore(newNode, siblingNode);
			}
			else {
				parentNode.AppendChild(newNode);
			}
		}
	}
}
