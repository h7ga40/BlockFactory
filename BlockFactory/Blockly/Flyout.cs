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
 * @fileoverview Flyout tray containing blocks which may be created.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Flyout
	{
		WorkspaceSvg workspace_;
		private bool RTL;
		/// <summary>
		/// Flyout should be laid out horizontally vs vertically.
		/// </summary>
		private bool horizontalLayout_;
		/// <summary>
		/// Position of the toolbox and flyout relative to the workspace.
		/// </summary>
		private int toolboxPosition_;
		/// <summary>
		/// Opaque data that can be passed to Blockly.unbindEvent_.
		/// </summary>
		private JsArray<EventWrapInfo> eventWrappers_;
		/// <summary>
		/// List of background buttons that lurk behind each block to catch clicks
		/// landing in the blocks' lakes and bays.
		/// </summary>
		private JsArray<SVGElement> backgroundButtons_;
		/// <summary>
		/// List of visible buttons.
		/// </summary>
		private JsArray<FlyoutButton> buttons_;
		/// <summary>
		/// List of event listeners.
		/// </summary>
		private JsArray<JsArray<EventWrapInfo>> listeners_;
		/// <summary>
		/// List of blocks that should always be disabled.
		/// </summary>
		private JsArray<Block> permanentlyDisabled_;
		/// <summary>
		/// y coordinate of mousedown - used to calculate scroll distances.
		/// </summary>
		private double startDragMouseY_;
		/// <summary>
		/// x coordinate of mousedown - used to calculate scroll distances.
		/// </summary>
		private double startDragMouseX_;
		/// <summary>
		/// When a flyout drag is in progress, this is a reference to the flyout being
		/// dragged. This is used by Flyout.terminateDrag_ to reset dragMode_.
		/// </summary>
		internal static Flyout startFlyout_;
		/// <summary>
		/// Event that started a drag. Used to determine the drag distance/direction and
		/// also passed to BlockSvg.onMouseDown_() after creating a new block.
		/// </summary>
		private static MouseEvent startDownEvent_;
		/// <summary>
		/// Flyout block where the drag/click was initiated. Used to fire click events or
		/// create a new block.
		/// </summary>
		private static BlockSvg startBlock_;
		/// <summary>
		/// Wrapper function called when a mouseup occurs during a background or block
		/// drag operation.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseUpWrapper_;
		/// <summary>
		/// Wrapper function called when a mousemove occurs during a background drag.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseMoveWrapper_;
		/// <summary>
		/// Wrapper function called when a mousemove occurs during a block drag.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseMoveBlockWrapper_;
		/// <summary>
		/// Does the flyout automatically close when a block is created?
		/// </summary>
		internal bool autoClose = true;
		/// <summary>
		/// Corner radius of the flyout background.
		/// </summary>
		public const double CORNER_RADIUS = 8.0;
		private double CORNER_RADIUS_ = CORNER_RADIUS;
		/// <summary>
		/// Number of pixels the mouse must move before a drag/scroll starts. Because the
		/// drag-intention is determined when this is reached, it is larger than
		/// Blockly.DRAG_RADIUS so that the drag-direction is clearer.
		/// </summary>
		public double DRAG_RADIUS = 10.0;
		/// <summary>
		/// Margin around the edges of the blocks in the flyout.
		/// </summary>
		public double MARGIN;
		/// <summary>
		/// Gap between items in horizontal flyouts. Can be overridden with the "sep"
		/// element.
		/// </summary>
		public double GAP_X;
		/// <summary>
		/// Gap between items in vertical flyouts. Can be overridden with the "sep"
		/// element.
		/// </summary>
		public double GAP_Y;
		/// <summary>
		/// Top/bottom padding between scrollbar and edge of flyout background.
		/// </summary>
		public double SCROLLBAR_PADDING = 2;
		/// <summary>
		/// Width of flyout.
		/// </summary>
		internal double width_ = 0;
		/// <summary>
		/// Height of flyout.
		/// </summary>
		internal double height_ = 0;
		/// <summary>
		/// Is the flyout dragging (scrolling)?
		/// DRAG_NONE - no drag is ongoing or state is undetermined.
		/// DRAG_STICKY - still within the sticky drag radius.
		/// DRAG_FREE - in scroll mode (never create a new block).
		/// </summary>
		internal int dragMode_ = Core.DRAG_NONE;
		/// <summary>
		/// Range of a drag angle from a flyout considered "dragging toward workspace".
		/// Drags that are within the bounds of this many degrees from the orthogonal
		/// line to the flyout edge are considered to be "drags toward the workspace".
		/// Example:
		/// Flyout                                                  Edge   Workspace
		/// [block] /  <-within this angle, drags "toward workspace" |
		/// [block] ---- orthogonal to flyout boundary ----          |
		/// [block] \                                                |
		/// The angle is given in degrees from the orthogonal.
		/// 
		/// This is used to know when to create a new block and when to scroll the
		/// flyout. Setting it to 360 means that all drags create a new block.
		/// </summary>
		private double dragAngleRange_ = 70;

		public Flyout(Options workspaceOptions)
		{
			workspaceOptions.getMetrics = () => getMetrics_();
			workspaceOptions.setMetrics = (m) => setMetrics_(m);

			this.workspace_ = new WorkspaceSvg(workspaceOptions);
			this.workspace_.MAX_UNDO = 0;
			this.workspace_.isFlyout = true;

			this.RTL = workspaceOptions.RTL;
			this.horizontalLayout_ = workspaceOptions.horizontalLayout;
			this.toolboxPosition_ = workspaceOptions.toolboxPosition;
			this.eventWrappers_ = new JsArray<EventWrapInfo>();
			this.backgroundButtons_ = new JsArray<SVGElement>();
			this.buttons_ = new JsArray<FlyoutButton>();
			this.listeners_ = new JsArray<JsArray<EventWrapInfo>>();
			this.permanentlyDisabled_ = new JsArray<Block>();
			this.startDragMouseY_ = 0.0;
			this.startDragMouseX_ = 0.0;
			MARGIN = CORNER_RADIUS_;
			GAP_X = MARGIN * 3;
			GAP_Y = MARGIN * 3;
		}

		private SVGElement svgGroup_;
		private SVGElement svgBackground_;

		/// <summary>
		/// Creates the flyout's DOM.  Only needs to be called once.
		/// </summary>
		/// <returns>The flyout's SVG group.</returns>
		public SVGElement createDom()
		{
			/*
			<g>
			  <path class="blocklyFlyoutBackground"/>
			  <g class="blocklyFlyout"></g>
			</g>
			*/
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyFlyout"} }, null);
			this.svgBackground_ = Core.createSvgElement("path", new Dictionary<string, object>() {
				{"class", "blocklyFlyoutBackground"}}, this.svgGroup_);
			this.svgGroup_.AppendChild(this.workspace_.createDom());
			return this.svgGroup_;
		}

		private WorkspaceSvg targetWorkspace_;
		private Scrollbar scrollbar_;

		/// <summary>
		/// Initializes the flyout.
		/// </summary>
		/// <param name="targetWorkspace">The workspace in which to create
		/// new blocks.</param>
		public void init(WorkspaceSvg targetWorkspace)
		{
			this.targetWorkspace_ = targetWorkspace;
			this.workspace_.targetWorkspace = targetWorkspace;
			// Add scrollbar.
			this.scrollbar_ = new Scrollbar(this.workspace_,
				this.horizontalLayout_, false);

			this.hide();

			this.eventWrappers_.PushRange(
				Core.bindEventWithChecks_(this.svgGroup_, "wheel", this, new Action<WheelEvent>(this.wheel_)));
			if (!this.autoClose) {
				this.filterWrapper_ = new Action<Events.Abstract>((e) => { this.filterForCapacity_(); });
				this.targetWorkspace_.addChangeListener(this.filterWrapper_);
			}
			// Dragging the flyout up and down.
			this.eventWrappers_.PushRange(
				Core.bindEventWithChecks_(this.svgGroup_, "mousedown", this,
				new Action<MouseEvent>(this.onMouseDown_)));
		}

		/// <summary>
		/// Dispose of this flyout.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public void dispose()
		{
			this.hide();
			Core.unbindEvent_(this.eventWrappers_);
			if (this.filterWrapper_ != null) {
				this.targetWorkspace_.removeChangeListener(this.filterWrapper_);
				this.filterWrapper_ = null;
			}
			if (this.scrollbar_ != null) {
				this.scrollbar_.dispose();
				this.scrollbar_ = null;
			}
			if (this.workspace_ != null) {
				this.workspace_.targetWorkspace = null;
				this.workspace_.dispose();
				this.workspace_ = null;
			}
			if (this.svgGroup_ != null) {
				goog.dom.removeNode(this.svgGroup_);
				this.svgGroup_ = null;
			}
			this.svgBackground_ = null;
			this.targetWorkspace_ = null;
		}

		/// <summary>
		/// Get the width of the flyout.
		/// </summary>
		/// <returns>The width of the flyout.</returns>
		public double getWidth()
		{
			return this.width_;
		}

		/// <summary>
		/// Get the height of the flyout.
		/// </summary>
		/// <returns>The width of the flyout.</returns>
		public double getHeight()
		{
			return this.height_;
		}

		/// <summary>
		/// Return an object with all the metrics required to size scrollbars for the
		/// flyout.  The following properties are computed:
		/// .viewHeight: Height of the visible rectangle,
		/// .viewWidth: Width of the visible rectangle,
		/// .contentHeight: Height of the contents,
		/// .contentWidth: Width of the contents,
		/// .viewTop: Offset of top edge of visible rectangle from parent,
		/// .contentTop: Offset of the top-most content from the y=0 coordinate,
		/// .absoluteTop: Top-edge of view.
		/// .viewLeft: Offset of the left edge of visible rectangle from parent,
		/// .contentLeft: Offset of the left-most content from the x=0 coordinate,
		/// .absoluteLeft: Left-edge of view.
		/// </summary>
		/// <returns>Contains size and position metrics of the flyout.</returns>
		internal Metrics getMetrics_()
		{
			if (!this.isVisible()) {
				// Flyout is hidden.
				return null;
			}

			SVGRect optionBox;
			try {
				optionBox = this.workspace_.getCanvas().getBBox();
			}
			catch (Exception) {
				// Firefox has trouble with hidden elements (Bug 528969).
				optionBox = new SVGRect { height = 0, y = 0, width = 0, x = 0 };
			}

			var absoluteTop = this.SCROLLBAR_PADDING;
			var absoluteLeft = this.SCROLLBAR_PADDING;
			double viewWidth, viewHeight;
			if (this.horizontalLayout_) {
				if (this.toolboxPosition_ == Core.TOOLBOX_AT_BOTTOM) {
					absoluteTop = 0;
				}
				viewHeight = this.height_;
				if (this.toolboxPosition_ == Core.TOOLBOX_AT_TOP) {
					viewHeight += this.MARGIN - this.SCROLLBAR_PADDING;
				}
				viewWidth = this.width_ - 2 * this.SCROLLBAR_PADDING;
			}
			else {
				absoluteLeft = 0;
				viewHeight = this.height_ - 2 * this.SCROLLBAR_PADDING;
				viewWidth = this.width_;
				if (!this.RTL) {
					viewWidth -= this.SCROLLBAR_PADDING;
				}
			}

			var metrics = new Metrics {
				viewHeight = viewHeight,
				viewWidth = viewWidth,
				contentHeight = (optionBox.height + 2 * this.MARGIN) * this.workspace_.scale,
				contentWidth = (optionBox.width + 2 * this.MARGIN) * this.workspace_.scale,
				viewTop = -this.workspace_.scrollY,
				viewLeft = -this.workspace_.scrollX,
				contentTop = optionBox.y,
				contentLeft = optionBox.x,
				absoluteTop = absoluteTop,
				absoluteLeft = absoluteLeft
			};
			return metrics;
		}

		/// <summary>
		/// Sets the translation of the flyout to match the scrollbars.
		/// </summary>
		/// <param name="xyRatio">Contains a y property which is a float
		/// between 0 and 1 specifying the degree of scrolling and a
		/// similar x property.</param>
		private void setMetrics_(Metrics xyRatio)
		{
			var metrics = this.getMetrics_();
			// This is a fix to an apparent race condition.
			if (metrics == null) {
				return;
			}
			if (!this.horizontalLayout_ && Script.IsFinite(xyRatio.y)) {
				this.workspace_.scrollY = -metrics.contentHeight * xyRatio.y;
			}
			else if (this.horizontalLayout_ && Script.IsFinite(xyRatio.x)) {
				this.workspace_.scrollX = -metrics.contentWidth * xyRatio.x;
			}

			this.workspace_.translate(this.workspace_.scrollX + metrics.absoluteLeft,
				this.workspace_.scrollY + metrics.absoluteTop);
		}

		/// <summary>
		/// Move the flyout to the edge of the workspace.
		/// </summary>
		public void position()
		{
			if (!this.isVisible()) {
				return;
			}
			var targetWorkspaceMetrics = this.targetWorkspace_.getMetrics();
			if (targetWorkspaceMetrics == null) {
				// Hidden components will return null.
				return;
			}
			var edgeWidth = this.horizontalLayout_ ?
				targetWorkspaceMetrics.viewWidth : this.width_;
			edgeWidth -= this.CORNER_RADIUS_;
			if (this.toolboxPosition_ == Core.TOOLBOX_AT_RIGHT) {
				edgeWidth *= -1;
			}

			this.setBackgroundPath_(edgeWidth,
				this.horizontalLayout_ ? this.height_ :
				targetWorkspaceMetrics.viewHeight);

			var x = targetWorkspaceMetrics.absoluteLeft;
			if (this.toolboxPosition_ == Core.TOOLBOX_AT_RIGHT) {
				x += targetWorkspaceMetrics.viewWidth;
				x -= this.width_;
			}

			var y = targetWorkspaceMetrics.absoluteTop;
			if (this.toolboxPosition_ == Core.TOOLBOX_AT_BOTTOM) {
				y += targetWorkspaceMetrics.viewHeight;
				y -= this.height_;
			}

			this.svgGroup_.SetAttribute("transform", "translate(" + x + "," + y + ")");

			// Record the height for Blockly.Flyout.getMetrics_, or width if the layout is
			// horizontal.
			if (this.horizontalLayout_) {
				this.width_ = targetWorkspaceMetrics.viewWidth;
			}
			else {
				this.height_ = targetWorkspaceMetrics.viewHeight;
			}

			// Update the scrollbar (if one exists).
			if (this.scrollbar_ != null) {
				this.scrollbar_.resize();
			}
		}

		/// <summary>
		/// Create and set the path for the visible boundaries of the flyout.
		/// </summary>
		/// <param name="width">The width of the flyout, not including the
		/// rounded corners.</param>
		/// <param name="height">The height of the flyout, not including
		/// rounded corners.</param>
		private void setBackgroundPath_(double width, double height)
		{
			if (this.horizontalLayout_) {
				this.setBackgroundPathHorizontal_(width, height);
			}
			else {
				this.setBackgroundPathVertical_(width, height);
			}
		}

		/// <summary>
		/// Create and set the path for the visible boundaries of the flyout in vertical
		/// mode.
		/// </summary>
		/// <param name="width">The width of the flyout, not including the
		/// rounded corners.</param>
		/// <param name="height">The height of the flyout, not including
		/// rounded corners.</param>
		private void setBackgroundPathVertical_(double width, double height)
		{
			var atRight = this.toolboxPosition_ == Core.TOOLBOX_AT_RIGHT;
			// Decide whether to start on the left or right.
			var path = new JsArray<string> { "M " + (atRight ? this.width_ : 0) + ",0" };
			// Top.
			path.Push("h", width.ToString());
			// Rounded corner.
			path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(),
				(atRight ? 0 : 1).ToString(),
				(atRight ? -this.CORNER_RADIUS_ : this.CORNER_RADIUS_).ToString(),
				this.CORNER_RADIUS_.ToString());
			// Side closest to workspace.
			path.Push("v", System.Math.Max(0, height - this.CORNER_RADIUS_ * 2).ToString());
			// Rounded corner.
			path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(),
				(atRight ? 0 : 1).ToString(),
				(atRight ? this.CORNER_RADIUS_ : -this.CORNER_RADIUS_).ToString(),
				this.CORNER_RADIUS_.ToString());
			// Bottom.
			path.Push("h", (-width).ToString());
			path.Push("z");
			this.svgBackground_.SetAttribute("d", path.Join(" "));
		}

		/// <summary>
		/// Create and set the path for the visible boundaries of the flyout in
		/// horizontal mode.
		/// </summary>
		/// <param name="width">The width of the flyout, not including the
		/// rounded corners.</param>
		/// <param name="height">The height of the flyout, not including
		/// rounded corners.</param>
		private void setBackgroundPathHorizontal_(double width, double height)
		{
			var atTop = this.toolboxPosition_ == Core.TOOLBOX_AT_TOP;
			// Start at top left.
			var path = new JsArray<string> { "M 0," + (atTop ? 0 : this.CORNER_RADIUS_) };

			if (atTop) {
				// Top.
				path.Push("h", (width + this.CORNER_RADIUS_).ToString());
				// Right.
				path.Push("v", height.ToString());
				// Bottom.
				path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(), 1.ToString(),
					(-this.CORNER_RADIUS_).ToString(), this.CORNER_RADIUS_.ToString());
				path.Push("h", (-1 * (width - this.CORNER_RADIUS_)).ToString());
				// Left.
				path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(), 1.ToString(),
					(-this.CORNER_RADIUS_).ToString(), (-this.CORNER_RADIUS_).ToString());
				path.Push("z");
			}
			else {
				// Top.
				path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(), 1.ToString(),
					this.CORNER_RADIUS_.ToString(), (-this.CORNER_RADIUS_).ToString());
				path.Push("h", (width - this.CORNER_RADIUS_).ToString());
				// Right.
				path.Push("a", this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString(), 0.ToString(), 0.ToString(), 1.ToString(),
					this.CORNER_RADIUS_.ToString(), this.CORNER_RADIUS_.ToString());
				path.Push("v", (height - this.CORNER_RADIUS_).ToString());
				// Bottom.
				path.Push("h", (-width - this.CORNER_RADIUS_).ToString());
				// Left.
				path.Push("z");
			}
			this.svgBackground_.SetAttribute("d", path.Join(" "));
		}

		/// <summary>
		/// Scroll the flyout to the top.
		/// </summary>
		public void scrollToStart()
		{
			this.scrollbar_.set((this.horizontalLayout_ && this.RTL) ? Double.PositiveInfinity : 0.0);
		}

		/// <summary>
		/// Scroll the flyout.
		/// </summary>
		/// <param name="e">Mouse wheel scroll event.</param>
		private void wheel_(WheelEvent e)
		{
			var delta = this.horizontalLayout_ ? e.DeltaX : e.DeltaY;

			if (delta > 0.0) {
				if (goog.userAgent.GECKO) {
					// Firefox's deltas are a tenth that of Chrome/Safari.
					delta *= 10;
				}
				var metrics = this.getMetrics_();
				var pos = this.horizontalLayout_ ? metrics.viewLeft + delta :
					metrics.viewTop + delta;
				var limit = this.horizontalLayout_ ?
					metrics.contentWidth - metrics.viewWidth :
					metrics.contentHeight - metrics.viewHeight;
				pos = System.Math.Min(pos, limit);
				pos = System.Math.Max(pos, 0);
				this.scrollbar_.set(pos);
			}

			// Don't scroll the page.
			e.PreventDefault();
			// Don't propagate mousewheel event (zooming).
			e.StopPropagation();
		}

		/// <summary>
		/// Is the flyout visible?
		/// </summary>
		/// <returns>True if visible.</returns>
		public bool isVisible()
		{
			return this.svgGroup_?.style.Display == Display.Block;
		}

		/// <summary>
		/// Hide and empty the flyout.
		/// </summary>
		public void hide()
		{
			if (!this.isVisible()) {
				return;
			}
			this.svgGroup_.style.Display = Display.None;
			// Delete all the event listeners.
			foreach (var listen in this.listeners_) {
				Core.unbindEvent_(listen);
			}
			this.listeners_.Clear();
			if (this.reflowWrapper_ != null) {
				this.workspace_.removeChangeListener(this.reflowWrapper_);
				this.reflowWrapper_ = null;
			}
			// Do NOT delete the blocks here.  Wait until Flyout.show.
			// https://neil.fraser.name/news/2014/08/09/
		}

		/// <summary>
		/// Show and populate the flyout.
		/// </summary>
		/// <param name="xmlList">List of blocks to show.
		/// Variables and procedures have a custom set of blocks.</param>
		public void show(Union<string, JsArray<Node>, NodeList> xmlList_)
		{
			IEnumerable<Node> xmlList;
			this.hide();
			this.clearOldBlocks_();

			if (flyoutCategory != null) {
				xmlList = flyoutCategory(xmlList_, this.workspace_.targetWorkspace);
			}
			else if (xmlList_.Is<JsArray<Node>>()) {
				xmlList = xmlList_.As<JsArray<Node>>();
			}
			else if (xmlList_.Is <NodeList>()) {
				xmlList = xmlList_.As<NodeList>();
			}
			else {
				xmlList = (new DOMParser()).ParseFromString(xmlList_.As<string>(), "text/xml").ChildNodes.ToArray();
			}

			this.svgGroup_.style.Display = Display.Block;
			// Create the blocks to be shown in this flyout.
			var contents = new JsArray<FlyoutContents>();
			var gaps = new JsArray<double>();
			this.permanentlyDisabled_.Clear();
			foreach (var xml_ in xmlList) {
				if (xml_ is Element xml) {
					var tagName = xml.TagName.ToUpperCase();
					var default_gap = this.horizontalLayout_ ? this.GAP_X : this.GAP_Y;
					if (tagName == "BLOCK") {
						var curBlock = (BlockSvg)Xml.domToBlock(xml, this.workspace_);
						if (curBlock.disabled) {
							// Record blocks that were initially disabled.
							// Do not enable these blocks as a result of capacity filtering.
							this.permanentlyDisabled_.Push(curBlock);
						}
						contents.Push(new FlyoutContents { type = "block", block = curBlock });
						gaps.Push(!Double.TryParse(xml.GetAttribute("gap"), out var gap) ? default_gap : gap);
					}
					else if (xml.TagName.ToUpperCase() == "SEP") {
						// Change the gap between two blocks.
						// <sep gap="36"></sep>
						// The default gap is 24, can be set larger or smaller.
						// This overwrites the gap attribute on the previous block.
						// Note that a deprecated method is to add a gap to a block.
						// <block type="math_arithmetic" gap="8"></block>
						var newGap = Script.ParseFloat(xml.GetAttribute("gap"));
						// Ignore gaps before the first block.
						if (!Double.IsNaN(newGap) && gaps.Length > 0) {
							gaps[gaps.Length - 1] = newGap;
						}
						else {
							gaps.Push(default_gap);
						}
					}
					else if ((tagName == "BUTTON") || (tagName == "LABEL")) {
						var label = xml.GetAttribute("text");
						var callbackKey = xml.GetAttribute("callbackKey");
						var curButton = new FlyoutButton(this.workspace_,
							this.targetWorkspace_, label, callbackKey, tagName == "LABEL");
						contents.Push(new FlyoutContents { type = "button", button = curButton });
						gaps.Push(default_gap);
					}
				}
			}

			this.layout_(contents, gaps);

			// IE 11 is an incompetent browser that fails to fire mouseout events.
			// When the mouse is over the background, deselect all blocks.
			var deselectAll = new Action<Event>((e) => {
				var topBlocks = this.workspace_.getTopBlocks(false);
				foreach (BlockSvg block in topBlocks) {
					block.removeSelect();
				}
			});

			this.listeners_.Push(Core.bindEventWithChecks_(this.svgBackground_,
				"mouseover", this, deselectAll));

			if (this.horizontalLayout_) {
				this.height_ = 0;
			}
			else {
				this.width_ = 0;
			}
			this.reflow();

			this.filterForCapacity_();

			// Correctly position the flyout's scrollbar when it opens.
			this.position();

			this.reflowWrapper_ = new Action<Events.Abstract>((e) => this.reflow());
			this.workspace_.addChangeListener(this.reflowWrapper_);
		}

		/// <summary>
		/// Lay out the blocks in the flyout.
		/// </summary>
		/// <param name="contents">The blocks and buttons to lay out.</param>
		/// <param name="gaps">The visible gaps between blocks.</param>
		private void layout_(FlyoutContents[] contents, double[] gaps)
		{
			this.workspace_.scale = this.targetWorkspace_.scale;
			var margin = this.MARGIN;
			var cursorX = this.RTL ? margin : margin + BlockSvg.TAB_WIDTH;
			var cursorY = margin;
			if (this.horizontalLayout_ && this.RTL) {
				contents.Reverse();
			}

			int i = 0;
			foreach (var item in contents) {
				if (item.type == "block") {
					var block = item.block;
					var allBlocks = block.getDescendants();
					foreach (var child in allBlocks) {
						// Mark blocks as being inside a flyout.  This is used to detect and
						// prevent the closure of the flyout if the user right-clicks on such a
						// block.
						child.isInFlyout = true;
					}
					block.render();
					var root = block.getSvgRoot();
					var blockHW = block.getHeightWidth();
					var tab = block.outputConnection != null ? BlockSvg.TAB_WIDTH : 0;
					if (this.horizontalLayout_) {
						cursorX += tab;
					}
					block.moveBy((this.horizontalLayout_ && this.RTL) ?
						cursorX + blockHW.width - tab : cursorX,
						cursorY);
					if (this.horizontalLayout_) {
						cursorX += (blockHW.width + gaps[i] - tab);
					}
					else {
						cursorY += blockHW.height + gaps[i];
					}

					// Create an invisible rectangle under the block to act as a button.  Just
					// using the block as a button is poor, since blocks have holes in them.
					var rect = Core.createSvgElement("rect", new Dictionary<string, object>() { { "fill-opacity", 0 } }, null);
					rect.tooltip = block;
					Tooltip.bindMouseEvents(rect);
					// Add the rectangles under the blocks, so that the blocks' tooltips work.
					this.workspace_.getCanvas().InsertBefore(rect, block.getSvgRoot());
					block.flyoutRect_ = rect;
					this.backgroundButtons_.Push(rect);

					this.addBlockListeners_(root, block, rect);
				}
				else if (item.type == "button") {
					var button = item.button;
					var buttonSvg = button.createDom();
					button.moveTo(cursorX, cursorY);
					button.show();
					Core.bindEventWithChecks_(buttonSvg, "mouseup", button,
						new Action<Event>(button.onMouseUp));

					this.buttons_.Push(button);
					if (this.horizontalLayout_) {
						cursorX += (button.width + gaps[i]);
					}
					else {
						cursorY += button.height + gaps[i];
					}
				}
				i++;
			}
		}

		/// <summary>
		/// Delete blocks and background buttons from a previous showing of the flyout.
		/// </summary>
		private void clearOldBlocks_()
		{
			// Delete any blocks from a previous showing.
			var oldBlocks = this.workspace_.getTopBlocks(false);
			foreach (BlockSvg block in oldBlocks) {
				if (block.workspace == this.workspace_) {
					block.dispose(false, false);
				}
			}
			// Delete any background buttons from a previous showing.
			foreach (var rect in this.backgroundButtons_) {
				goog.dom.removeNode(rect);
			}
			this.backgroundButtons_.Clear();

			foreach (var button in this.buttons_) {
				button.dispose();
			}
			this.buttons_.Clear();
		}

		/// <summary>
		/// Add listeners to a block that has been added to the flyout.
		/// </summary>
		/// <param name="root">The root node of the SVG group the block is in.</param>
		/// <param name="block">The block to add listeners for.</param>
		/// <param name="rect">The invisible rectangle under the block that acts as
		/// a button for that block.</param>
		private void addBlockListeners_(SVGElement root, BlockSvg block, SVGElement rect)
		{
			this.listeners_.Push(Core.bindEventWithChecks_(root, "mousedown", null,
				this.blockMouseDown_(block)));
			this.listeners_.Push(Core.bindEventWithChecks_(rect, "mousedown", null,
				this.blockMouseDown_(block)));
			this.listeners_.Push(Core.bindEvent_(root, "mouseover", block,
				new Action<Event>(block.addSelect)));
			this.listeners_.Push(Core.bindEvent_(root, "mouseout", block,
				new Action<Event>(block.removeSelect)));
			this.listeners_.Push(Core.bindEvent_(rect, "mouseover", block,
				new Action<Event>(block.addSelect)));
			this.listeners_.Push(Core.bindEvent_(rect, "mouseout", block,
				new Action<Event>(block.removeSelect)));
		}

		/// <summary>
		/// Actions to take when a block in the flyout is right-clicked.
		/// </summary>
		/// <param name="e">Event that triggered the right-click.  Could originate from
		/// a long-press in a touch environment.</param>
		/// <param name="block">The block that was clicked.</param>
		internal static void blockRightClick_(MouseEvent e, BlockSvg block)
		{
			Core.terminateDrag_();
			Core.hideChaff(true);
			block.showContextMenu_(e);
			// This was a right-click, so end the gesture immediately.
			Touch.clearTouchIdentifier();
		}

		/// <summary>
		/// Handle a mouse-down on an SVG block in a non-closing flyout.
		/// </summary>
		/// <param name="block">The flyout block to copy.</param>
		/// <returns>Function to call when block is clicked.</returns>
		private Action<MouseEvent> blockMouseDown_(BlockSvg block)
		{
			var flyout = this;
			return new Action<MouseEvent>((e) => {
				if (Core.isRightButton(e)) {
					Flyout.blockRightClick_(e, block);
				}
				else {
					Core.terminateDrag_();
					Core.hideChaff(true);
					// Left-click (or middle click)
					Css.setCursor(Css.Cursor.CLOSED);
					// Record the current mouse position.
					flyout.startDragMouseY_ = e.ClientY;
					flyout.startDragMouseX_ = e.ClientX;
					Flyout.startDownEvent_ = e;
					Flyout.startBlock_ = block;
					Flyout.startFlyout_ = flyout;
					Flyout.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
						"mouseup", flyout, new Action<MouseEvent>(flyout.onMouseUp_));
					Flyout.onMouseMoveBlockWrapper_ = Core.bindEventWithChecks_(
						Document.Instance, "mousemove", flyout, new Action<MouseEvent>(flyout.onMouseMoveBlock_));
				}
				// This event has been handled.  No need to bubble up to the document.
				e.StopPropagation();
				e.PreventDefault();
			});
		}

		/// <summary>
		/// Mouse down on the flyout background.  Start a vertical scroll drag.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		private void onMouseDown_(MouseEvent e)
		{
			if (Core.isRightButton(e)) {
				// Don't start drags with right clicks.
				Touch.clearTouchIdentifier();
				return;
			}
			Core.hideChaff(true);
			this.dragMode_ = Core.DRAG_FREE;
			this.startDragMouseY_ = e.ClientY;
			this.startDragMouseX_ = e.ClientX;
			Flyout.startFlyout_ = this;
			Flyout.onMouseMoveWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mousemove", this, new Action<MouseEvent>(this.onMouseMove_));
			Flyout.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
				"mouseup", null, new Action<MouseEvent>((ev) => Flyout.terminateDrag_()));
			// This event has been handled.  No need to bubble up to the document.
			e.PreventDefault();
			e.StopPropagation();
		}

		/// <summary>
		/// Handle a mouse-up anywhere in the SVG pane.  Is only registered when a
		/// block is clicked.  We can't use mouseUp on the block since a fast-moving
		/// cursor can briefly escape the block before it catches up.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void onMouseUp_(MouseEvent e)
		{
			if (!this.workspace_.isDragging()) {
				// This was a click, not a drag.  End the gesture.
				Touch.clearTouchIdentifier();
				if (this.autoClose) {
					this.createBlockFunc_(Flyout.startBlock_)(
						Flyout.startDownEvent_);
				}
				else if (!WidgetDiv.isVisible()) {
					Events.fire(
						new Events.Ui(Flyout.startBlock_, "click", null, null));
				}
			}
			Core.terminateDrag_();
		}

		/// <summary>
		/// Handle a mouse-move to vertically drag the flyout.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		private void onMouseMove_(MouseEvent e)
		{
			var metrics = this.getMetrics_();
			if (this.horizontalLayout_) {
				if (metrics.contentWidth - metrics.viewWidth < 0) {
					return;
				}
				var dx = e.ClientX - this.startDragMouseX_;
				this.startDragMouseX_ = e.ClientX;
				var x = metrics.viewLeft - dx;
				x = goog.math.clamp(x, 0, metrics.contentWidth - metrics.viewWidth);
				this.scrollbar_.set(x);
			}
			else {
				if (metrics.contentHeight - metrics.viewHeight < 0) {
					return;
				}
				var dy = e.ClientY - this.startDragMouseY_;
				this.startDragMouseY_ = e.ClientY;
				var y = metrics.viewTop - dy;
				y = goog.math.clamp(y, 0, metrics.contentHeight - metrics.viewHeight);
				this.scrollbar_.set(y);
			}
		}

		/// <summary>
		/// Mouse button is down on a block in a non-closing flyout.  Create the block
		/// if the mouse moves beyond a small radius.  This allows one to play with
		/// fields without instantiating blocks that instantly self-destruct.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		private void onMouseMoveBlock_(MouseEvent e)
		{
			if (e.Type == "mousemove" && e.ClientX <= 1 && e.ClientY == 0 &&
				e.Button == 0) {
				/* HACK:
				 Safari Mobile 6.0 and Chrome for Android 18.0 fire rogue mousemove events
				 on certain touch actions. Ignore events with these signatures.
				 This may result in a one-pixel blind spot in other browsers,
				 but this shouldn't be noticeable. */
				e.StopPropagation();
				return;
			}
			var dx = e.ClientX - Flyout.startDownEvent_.ClientX;
			var dy = e.ClientY - Flyout.startDownEvent_.ClientY;

			var createBlock = this.determineDragIntention_(dx, dy);
			if (createBlock) {
				Core.longStop_(e);
				this.createBlockFunc_(Flyout.startBlock_)(
					Flyout.startDownEvent_);
			}
			else if (this.dragMode_ == Core.DRAG_FREE) {
				Core.longStop_(e);
				// Do a scroll.
				this.onMouseMove_(e);
			}
			e.StopPropagation();
		}

		/// <summary>
		/// Determine the intention of a drag.
		/// Updates dragMode_ based on a drag delta and the current mode,
		/// and returns true if we should create a new block.
		/// </summary>
		/// <param name="dx">X delta of the drag.</param>
		/// <param name="dy">Y delta of the drag.</param>
		/// <returns>True if a new block should be created.</returns>
		private bool determineDragIntention_(double dx, double dy)
		{
			if (this.dragMode_ == Core.DRAG_FREE) {
				// Once in free mode, always stay in free mode and never create a block.
				return false;
			}
			var dragDistance = System.Math.Sqrt(dx * dx + dy * dy);
			if (dragDistance < this.DRAG_RADIUS) {
				// Still within the sticky drag radius.
				this.dragMode_ = Core.DRAG_STICKY;
				return false;
			}
			else {
				if (this.isDragTowardWorkspace_(dx, dy) || !this.scrollbar_.isVisible()) {
					// Immediately create a block.
					return true;
				}
				else {
					// Immediately move to free mode - the drag is away from the workspace.
					this.dragMode_ = Core.DRAG_FREE;
					return false;
				}
			}
		}

		/// <summary>
		/// Determine if a drag delta is toward the workspace, based on the position
		/// and orientation of the flyout. This is used in determineDragIntention_ to
		/// determine if a new block should be created or if the flyout should scroll.
		/// </summary>
		/// <param name="dx">X delta of the drag.</param>
		/// <param name="dy">Y delta of the drag.</param>
		/// <returns>True if the drag is toward the workspace.</returns>
		private bool isDragTowardWorkspace_(double dx, double dy)
		{
			// Direction goes from -180 to 180, with 0 toward the right and 90 on top.
			var dragDirection = System.Math.Atan2(dy, dx) / System.Math.PI * 180;

			var draggingTowardWorkspace = false;
			var range = this.dragAngleRange_;
			if (this.horizontalLayout_) {
				if (this.toolboxPosition_ == Core.TOOLBOX_AT_TOP) {
					// Horizontal at top.
					if (dragDirection < 90 + range && dragDirection > 90 - range) {
						draggingTowardWorkspace = true;
					}
				}
				else {
					// Horizontal at bottom.
					if (dragDirection > -90 - range && dragDirection < -90 + range) {
						draggingTowardWorkspace = true;
					}
				}
			}
			else {
				if (this.toolboxPosition_ == Core.TOOLBOX_AT_LEFT) {
					// Vertical at left.
					if (dragDirection < range && dragDirection > -range) {
						draggingTowardWorkspace = true;
					}
				}
				else {
					// Vertical at right.
					if (dragDirection < -180 + range || dragDirection > 180 - range) {
						draggingTowardWorkspace = true;
					}
				}
			}
			return draggingTowardWorkspace;
		}

		/// <summary>
		/// Create a copy of this block on the workspace.
		/// </summary>
		/// <param name="originBlock">The flyout block to copy.</param>
		/// <returns>Function to call when block is clicked.</returns>
		private Action<MouseEvent> createBlockFunc_(BlockSvg originBlock)
		{
			var flyout = this;
			return new Action<MouseEvent>((e) => {
				if (Core.isRightButton(e)) {
					// Right-click.  Don't create a block, let the context menu show.
					return;
				}
				if (originBlock.disabled) {
					// Beyond capacity.
					return;
				}
				Events.disable();
				BlockSvg block;
				try {
					block = flyout.placeNewBlock_(originBlock);
				}
				finally {
					Events.enable();
				}
				if (Events.isEnabled()) {
					Events.setGroup(true);
					Events.fire(new Events.Create(block));
				}
				if (flyout.autoClose) {
					flyout.hide();
				}
				else {
					flyout.filterForCapacity_();
				}
				// Start a dragging operation on the new block.
				block.onMouseDown_(e);
				Core.dragMode_ = Core.DRAG_FREE;
				block.setDragging_(true);
				flyout.workspace_.setResizesEnabled(false);
			});
		}

		/// <summary>
		/// Copy a block from the flyout to the workspace and position it correctly.
		/// </summary>
		/// <param name="originBlock">The flyout block to copy.</param>
		/// <returns>The new block in the main workspace.</returns>
		private BlockSvg placeNewBlock_(BlockSvg originBlock)
		{
			var targetWorkspace = this.targetWorkspace_;
			var svgRootOld = originBlock.getSvgRoot();
			if (svgRootOld == null) {
				throw new Exception("originBlock is not rendered.");
			}
			// Figure out where the original block is on the screen, relative to the upper
			// left corner of the main workspace.
			var xyOld = Core.getSvgXY_(svgRootOld, targetWorkspace);
			// Take into account that the flyout might have been scrolled horizontally
			// (separately from the main workspace).
			// Generally a no-op in vertical mode but likely to happen in horizontal
			// mode.
			var scrollX = this.workspace_.scrollX;
			var scale = this.workspace_.scale;
			xyOld.x += scrollX / scale - scrollX;
			// If the flyout is on the right side, (0, 0) in the flyout is offset to
			// the right of (0, 0) in the main workspace.  Add an offset to take that
			// into account.
			if (this.toolboxPosition_ == Core.TOOLBOX_AT_RIGHT) {
				scrollX = targetWorkspace.getMetrics().viewWidth - this.width_;
				scale = targetWorkspace.scale;
				// Scale the scroll (getSvgXY_ did not do this).
				xyOld.x += scrollX / scale - scrollX;
			}

			// Take into account that the flyout might have been scrolled vertically
			// (separately from the main workspace).
			// Generally a no-op in horizontal mode but likely to happen in vertical
			// mode.
			var scrollY = this.workspace_.scrollY;
			scale = this.workspace_.scale;
			xyOld.y += scrollY / scale - scrollY;
			// If the flyout is on the bottom, (0, 0) in the flyout is offset to be below
			// (0, 0) in the main workspace.  Add an offset to take that into account.
			if (this.toolboxPosition_ == Core.TOOLBOX_AT_BOTTOM) {
				scrollY = targetWorkspace.getMetrics().viewHeight - this.height_;
				scale = targetWorkspace.scale;
				xyOld.y += scrollY / scale - scrollY;
			}

			// Create the new block by cloning the block in the flyout (via XML).
			var xml = Xml.blockToDom(originBlock);
			var block = (BlockSvg)Xml.domToBlock(xml, targetWorkspace);
			var svgRootNew = block.getSvgRoot();
			if (svgRootNew == null) {
				throw new Exception("block is not rendered.");
			}
			// Figure out where the new block got placed on the screen, relative to the
			// upper left corner of the workspace.  This may not be the same as the
			// original block because the flyout's origin may not be the same as the
			// main workspace's origin.
			var xyNew = Core.getSvgXY_(svgRootNew, targetWorkspace);
			// Scale the scroll (getSvgXY_ did not do this).
			xyNew.x +=
				targetWorkspace.scrollX / targetWorkspace.scale - targetWorkspace.scrollX;
			xyNew.y +=
				targetWorkspace.scrollY / targetWorkspace.scale - targetWorkspace.scrollY;
			// If the flyout is collapsible and the workspace can't be scrolled.
			if (targetWorkspace.toolbox_ != null && targetWorkspace.scrollbar == null) {
				xyNew.x += targetWorkspace.toolbox_.getWidth() / targetWorkspace.scale;
				xyNew.y += targetWorkspace.toolbox_.getHeight() / targetWorkspace.scale;
			}

			// Move the new block to where the old block is.
			block.moveBy(xyOld.x - xyNew.x, xyOld.y - xyNew.y);
			return block;
		}

		/// <summary>
		/// Filter the blocks on the flyout to disable the ones that are above the
		/// capacity limit.
		/// </summary>
		private void filterForCapacity_()
		{
			var remainingCapacity = this.targetWorkspace_.remainingCapacity();
			var blocks = this.workspace_.getTopBlocks(false);
			foreach (var block in blocks) {
				if (Array.IndexOf(this.permanentlyDisabled_, block) == -1) {
					var allBlocks = block.getDescendants();
					block.setDisabled(allBlocks.Length > remainingCapacity);
				}
			}
		}

		/// <summary>
		/// Return the deletion rectangle for this flyout.
		/// </summary>
		/// <returns>Rectangle in which to delete.</returns>
		public goog.math.Rect getClientRect()
		{
			if (this.svgGroup_ == null) {
				return null;
			}

			var flyoutRect = this.svgGroup_.GetBoundingClientRect();
			// BIG_NUM is offscreen padding so that blocks dragged beyond the shown flyout
			// area are still deleted.  Must be larger than the largest screen size,
			// but be smaller than half Number.MAX_SAFE_INTEGER (not available on IE).
			var BIG_NUM = 1000000000;
			var x = flyoutRect.Left;
			var y = flyoutRect.Top;
			var width = flyoutRect.Width;
			var height = flyoutRect.Height;

			if (this.toolboxPosition_ == Core.TOOLBOX_AT_TOP) {
				return new goog.math.Rect(-BIG_NUM, y - BIG_NUM, BIG_NUM * 2,
					BIG_NUM + height);
			}
			else if (this.toolboxPosition_ == Core.TOOLBOX_AT_BOTTOM) {
				return new goog.math.Rect(-BIG_NUM, y, BIG_NUM * 2,
					BIG_NUM + height);
			}
			else if (this.toolboxPosition_ == Core.TOOLBOX_AT_LEFT) {
				return new goog.math.Rect(x - BIG_NUM, -BIG_NUM, BIG_NUM + width,
					BIG_NUM * 2);
			}
			else {  // Right
				return new goog.math.Rect(x, -BIG_NUM, BIG_NUM + width, BIG_NUM * 2);
			}
		}

		/// <summary>
		/// Stop binding to the global mouseup and mousemove events.
		/// </summary>
		internal static void terminateDrag_()
		{
			if (Flyout.startFlyout_ != null) {
				// User was dragging the flyout background, and has stopped.
				if (Flyout.startFlyout_.dragMode_ == Core.DRAG_FREE) {
					Touch.clearTouchIdentifier();
				}
				Flyout.startFlyout_.dragMode_ = Core.DRAG_NONE;
				Flyout.startFlyout_ = null;
			}
			if (Flyout.onMouseUpWrapper_ != null) {
				Core.unbindEvent_(Flyout.onMouseUpWrapper_);
				Flyout.onMouseUpWrapper_ = null;
			}
			if (Flyout.onMouseMoveBlockWrapper_ != null) {
				Core.unbindEvent_(Flyout.onMouseMoveBlockWrapper_);
				Flyout.onMouseMoveBlockWrapper_ = null;
			}
			if (Flyout.onMouseMoveWrapper_ != null) {
				Core.unbindEvent_(Flyout.onMouseMoveWrapper_);
				Flyout.onMouseMoveWrapper_ = null;
			}
			Flyout.startDownEvent_ = null;
			Flyout.startBlock_ = null;
		}

		/// <summary>
		/// Compute height of flyout.  Position button under each block.
		/// For RTL: Lay out the blocks right-aligned.
		/// </summary>
		/// <param name="blocks">The blocks to reflow.</param>
		public void reflowHorizontal(Block[] blocks)
		{
			this.workspace_.scale = this.targetWorkspace_.scale;
			var flyoutHeight = 0.0;
			foreach (BlockSvg block in blocks) {
				flyoutHeight = System.Math.Max(flyoutHeight, block.getHeightWidth().height);
			}
			flyoutHeight += this.MARGIN * 1.5;
			flyoutHeight *= this.workspace_.scale;
			flyoutHeight += Scrollbar.scrollbarThickness;
			if (this.height_ != flyoutHeight) {
				foreach (BlockSvg block in blocks) {
					var blockHW = block.getHeightWidth();
					if (block.flyoutRect_ != null) {
						block.flyoutRect_.SetAttribute("width", blockHW.width.ToString());
						block.flyoutRect_.SetAttribute("height", blockHW.height.ToString());
						// Rectangles behind blocks with output tabs are shifted a bit.
						var tab = block.outputConnection != null ? BlockSvg.TAB_WIDTH : 0;
						var blockXY = block.getRelativeToSurfaceXY();
						block.flyoutRect_.SetAttribute("y", blockXY.y.ToString());
						block.flyoutRect_.SetAttribute("x",
							(this.RTL ? blockXY.x - blockHW.width + tab : blockXY.x - tab).ToString());
						// For hat blocks we want to shift them down by the hat height
						// since the y coordinate is the corner, not the top of the hat.
						var hatOffset =
							block.startHat_ ? BlockSvg.START_HAT_HEIGHT : 0;
						if (hatOffset != 0) {
							block.moveBy(0, hatOffset);
						}
						block.flyoutRect_.SetAttribute("y", blockXY.y.ToString());
					}
				}
				// Record the height for .getMetrics_ and .position.
				this.height_ = flyoutHeight;
				// Call this since it is possible the trash and zoom buttons need
				// to move. e.g. on a bottom positioned flyout when zoom is clicked.
				this.targetWorkspace_.resize();
			}
		}

		/// <summary>
		/// Compute width of flyout.  Position button under each block.
		/// For RTL: Lay out the blocks right-aligned.
		/// </summary>
		/// <param name="blocks">The blocks to reflow.</param>
		public void reflowVertical(Block[] blocks)
		{
			this.workspace_.scale = this.targetWorkspace_.scale;
			var flyoutWidth = 0.0;
			foreach (BlockSvg block in blocks) {
				var width = block.getHeightWidth().width;
				if (block.outputConnection != null) {
					width -= BlockSvg.TAB_WIDTH;
				}
				flyoutWidth = System.Math.Max(flyoutWidth, width);
			}
			foreach (var button in this.buttons_) {
				flyoutWidth = System.Math.Max(flyoutWidth, button.width);
			}
			flyoutWidth += this.MARGIN * 1.5 + BlockSvg.TAB_WIDTH;
			flyoutWidth *= this.workspace_.scale;
			flyoutWidth += Scrollbar.scrollbarThickness;
			if (this.width_ != flyoutWidth) {
				foreach (BlockSvg block in blocks) {
					var blockHW = block.getHeightWidth();
					if (this.RTL) {
						// With the flyoutWidth known, right-align the blocks.
						var oldX = block.getRelativeToSurfaceXY().x;
						var newX = flyoutWidth / this.workspace_.scale - this.MARGIN;
						newX -= BlockSvg.TAB_WIDTH;
						block.moveBy(newX - oldX, 0);
					}
					if (block.flyoutRect_ != null) {
						block.flyoutRect_.SetAttribute("width", blockHW.width.ToString());
						block.flyoutRect_.SetAttribute("height", blockHW.height.ToString());
						// Blocks with output tabs are shifted a bit.
						var tab = block.outputConnection != null ? BlockSvg.TAB_WIDTH : 0;
						var blockXY = block.getRelativeToSurfaceXY();
						block.flyoutRect_.SetAttribute("x",
							(this.RTL ? blockXY.x - blockHW.width + tab : blockXY.x - tab).ToString());
						// For hat blocks we want to shift them down by the hat height
						// since the y coordinate is the corner, not the top of the hat.
						var hatOffset =
							block.startHat_ ? BlockSvg.START_HAT_HEIGHT : 0;
						if (hatOffset != 0) {
							block.moveBy(0, hatOffset);
						}
						block.flyoutRect_.SetAttribute("y", blockXY.y.ToString());
					}
				}
				// Record the width for .getMetrics_ and .position.
				this.width_ = flyoutWidth;
				// Call this since it is possible the trash and zoom buttons need
				// to move. e.g. on a bottom positioned flyout when zoom is clicked.
				this.targetWorkspace_.resize();
			}
		}

		private Action<Events.Abstract> reflowWrapper_;
		private Action<Events.Abstract> filterWrapper_;
		public Func<Union<string, JsArray<Node>, NodeList>, Workspace, JsArray<Node>> flyoutCategory;

		/// <summary>
		/// Reflow blocks and their buttons.
		/// </summary>
		public void reflow()
		{
			if (this.reflowWrapper_ != null) {
				this.workspace_.removeChangeListener(this.reflowWrapper_);
			}
			var blocks = this.workspace_.getTopBlocks(false);
			if (this.horizontalLayout_) {
				this.reflowHorizontal(blocks);
			}
			else {
				this.reflowVertical(blocks);
			}
			if (this.reflowWrapper_ != null) {
				this.workspace_.addChangeListener(this.reflowWrapper_);
			}
		}

		private class FlyoutContents
		{
			public string type;
			public BlockSvg block;
			public FlyoutButton button;
		}
	}
}
