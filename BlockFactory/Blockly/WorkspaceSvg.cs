/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2014 Google Inc.
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
	* @fileoverview Object representing a workspace rendered as SVG.
	* @author fraser@google.com (Neil Fraser)
	*/
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public class WorkspaceSvg : Workspace
	{
		internal Toolbox toolbox_;

		public Func<Metrics> getMetrics;
		public Action<Metrics> setMetrics;

		/// <summary>
		/// Database of pre-loaded sounds.
		/// </summary>
		Dictionary<string, HTMLAudioElement> SOUNDS_ = new Dictionary<string, HTMLAudioElement>();

		/// <summary>
		/// Class for a workspace.  This is an onscreen area with optional trashcan,
		/// scrollbars, bubbles, and dragging.
		/// </summary>
		/// <param name="options">Dictionary of options.</param>
		public WorkspaceSvg(Options options)
			: base(options)
		{
			this.rendered = true;
			this.getMetrics =
				options.getMetrics ?? new Func<Metrics>(getTopLevelWorkspaceMetrics_);
			this.setMetrics =
				options.setMetrics ?? new Action<Metrics>(setTopLevelWorkspaceMetrics_);

			ConnectionDB.init(this);
		}

		/// <summary>
		/// A wrapper function called when a resize event occurs. You can pass the result to `unbindEvent_`.
		/// </summary>
		public JsArray<EventWrapInfo> resizeHandlerWrapper_;

		// <summary>
		// The render status of an SVG workspace.
		// Returns `true` for visible workspaces and `false` for non-visible, or headless, workspaces.
		// </summary>
		//public bool rendered;

		// <summary>
		// Is this workspace the surface for a flyout?
		// </summary>
		//public bool isFlyout;

		/// <summary>
		/// Is this workspace the surface for a mutator?
		/// </summary>
		//public bool isMutator;

		/// <summary>
		/// Is this workspace currently being dragged around?
		/// DRAG_NONE - No drag operation.
		/// DRAG_BEGIN - Still inside the initial DRAG_RADIUS.
		/// DRAG_FREE - Workspace has been dragged further than DRAG_RADIUS.
		/// </summary>
		internal int dragMode_ = Core.DRAG_NONE;

		/// <summary>
		/// Current horizontal scrolling offset.
		/// </summary>
		public double scrollX;

		/// <summary>
		/// Current vertical scrolling offset.
		/// </summary>
		public double scrollY;

		/// <summary>
		/// Horizontal scroll value when scrolling started.
		/// </summary>
		public double startScrollX;

		/// <summary>
		/// Vertical scroll value when scrolling started.
		/// </summary>
		public double startScrollY;

		/// <summary>
		/// Distance from mouse to object being dragged.
		/// </summary>
		public goog.math.Coordinate dragDeltaXY_;

		/// <summary>
		/// Current scale.
		/// </summary>
		public double scale = 1.0;

		/// <summary>
		/// The workspace's trashcan (if any).
		/// </summary>
		public Trashcan trashcan;

		/// <summary>
		/// This workspace's scrollbars, if they exist.
		/// </summary>
		public ScrollbarPair scrollbar;

		/// <summary>
		/// Time that the last sound was played.
		/// </summary>
		public Date lastSound_;

		/// <summary>
		/// Last known position of the page scroll.
		/// This is used to determine whether we have recalculated screen coordinate
		/// stuff since the page scrolled.
		/// </summary>
		private goog.math.Coordinate lastRecordedPageScroll_;

		/// <summary>
		/// Inverted screen CTM, for use in mouseToSvg.
		/// </summary>
		private SVGMatrix inverseScreenCTM_;

		/// <summary>
		/// Getter for the inverted screen CTM.
		/// </summary>
		/// <returns>The matrix to use in mouseToSvg</returns>
		public SVGMatrix getInverseScreenCTM()
		{
			return this.inverseScreenCTM_;
		}

		/// <summary>
		/// Update the inverted screen CTM.
		/// </summary>
		public void updateInverseScreenCTM()
		{
			var ctm = this.getParentSvg()?.getScreenCTM();
			if (ctm != null) {
				this.inverseScreenCTM_ = ctm.Inverse();
			}
		}

		/// <summary>
		/// Save resize handler data so we can delete it later in dispose.
		/// </summary>
		/// <param name="handler">Data that can be passed to unbindEvent_.</param>
		public void setResizeHandlerWrapper(JsArray<EventWrapInfo> handler)
		{
			this.resizeHandlerWrapper_ = handler;
		}

		internal SVGElement svgGroup_;
		private SVGElement svgBackground_;
		//private SVGElement gridPattern;

		/// <summary>
		/// Create the workspace DOM elements.
		/// </summary>
		/// <param name="opt_backgroundClass">opt_backgroundClass Either 'blocklyMainBackground' or
		/// 'blocklyMutatorBackground'.</param>
		/// <returns>The workspace's SVG group.</returns>
		public Element createDom(string opt_backgroundClass = null)
		{
			/**
			 * <g class="blocklyWorkspace">
			 *   <rect class="blocklyMainBackground" height="100%" width="100%"></rect>
			 *   [Trashcan and/or flyout may go here]
			 *   <g class="blocklyBlockCanvas"></g>
			 *   <g class="blocklyBubbleCanvas"></g>
			 *   [Scrollbars may go here]
			 * </g>
			 */
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyWorkspace"}}, null);
			if (opt_backgroundClass != null) {
				/** @type {SVGElement} */
				this.svgBackground_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"height", "100%" }, {"width", "100%" }, {"class", opt_backgroundClass} },
					this.svgGroup_);
				if (opt_backgroundClass == "blocklyMainBackground") {
					this.svgBackground_.style.Fill =
						"url(#" + this.options.gridPattern.Id + ")";
				}
			}
			/** @type {SVGElement} */
			this.svgBlockCanvas_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyBlockCanvas"} }, this.svgGroup_, this);
			/** @type {SVGElement} */
			this.svgBubbleCanvas_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyBubbleCanvas"} }, this.svgGroup_, this);
			var bottom = Scrollbar.scrollbarThickness;
			if (this.options.hasTrashcan) {
				bottom = this.addTrashcan_(bottom);
			}
			if (this.options.zoomOptions != null && this.options.zoomOptions.controls) {
				bottom = this.addZoomControls_(bottom);
			}

			if (!this.isFlyout) {
				Core.bindEventWithChecks_(this.svgGroup_, "mousedown", this,
					new Action<MouseEvent>(this.onMouseDown_));
				var thisWorkspace = this;
				Core.bindEventWithChecks_(this.svgGroup_, "touchstart", null,
					new Action<TouchEvent>((e) => { Core.longStart_(e, thisWorkspace); }));
				if (this.options.zoomOptions != null && this.options.zoomOptions.wheel) {
					// Mouse-wheel.
					Core.bindEventWithChecks_(this.svgGroup_, "wheel", this,
						new Action<MouseEvent>(this.onMouseWheel_));
				}
			}

			// Determine if there needs to be a category tree, or a simple list of
			// blocks.  This cannot be changed later, since the UI is very different.
			if (this.options.hasCategories) {
				this.toolbox_ = new Toolbox(this);
			}
			else if (this.options.languageTree != null) {
				this.addFlyout_();
			}
			this.updateGridPattern_();
			this.recordDeleteAreas();
			return this.svgGroup_;
		}

		/// <summary>
		/// Dispose of this workspace.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public new void dispose()
		{
			// Stop rerendering.
			this.rendered = false;
			base.dispose();
			if (this.svgGroup_ != null) {
				goog.dom.removeNode(this.svgGroup_);
				this.svgGroup_ = null;
			}
			this.svgBlockCanvas_ = null;
			this.svgBubbleCanvas_ = null;
			if (this.toolbox_ != null) {
				this.toolbox_.dispose();
				this.toolbox_ = null;
			}
			if (this.flyout_ != null) {
				this.flyout_.dispose();
				this.flyout_ = null;
			}
			if (this.trashcan != null) {
				this.trashcan.dispose();
				this.trashcan = null;
			}
			if (this.scrollbar != null) {
				this.scrollbar.dispose();
				this.scrollbar = null;
			}
			if (this.zoomControls_ != null) {
				this.zoomControls_.dispose();
				this.zoomControls_ = null;
			}
			if (this.options.parentWorkspace == null) {
				// Top-most workspace.  Dispose of the div that the
				// svg is injected into (i.e. injectionDiv).
				goog.dom.removeNode(this.getParentSvg().ParentNode);
			}
			if (this.resizeHandlerWrapper_ != null) {
				Core.unbindEvent_(this.resizeHandlerWrapper_);
				this.resizeHandlerWrapper_ = null;
			}
		}

		/// <summary>
		/// Obtain a newly created block.
		/// </summary>
		/// <param name="prototypeName">Name of the language object containing
		/// type-specific functions for this block.</param>
		/// <param name="opt_id">Optional ID.  Use this ID if provided, otherwise
		/// create a new id.</param>
		/// <returns>The created block.</returns>
		public BlockSvg newBlockSvg(string prototypeName, string opt_id = null)
		{
			var block = new BlockSvg(this, prototypeName);
			block.postCreate(opt_id);
			return block;
		}

		/// <summary>
		/// Add a trashcan.
		/// </summary>
		/// <param name="bottom">Distance from workspace bottom to bottom of trashcan.</param>
		/// <returns>Distance from workspace bottom to the top of trashcan.</returns>
		private double addTrashcan_(double bottom)
		{
			this.trashcan = new Trashcan(this);
			var svgTrashcan = this.trashcan.createDom();
			this.svgGroup_.InsertBefore(svgTrashcan, this.svgBlockCanvas_);
			return this.trashcan.init(bottom);
		}

		private ZoomControls zoomControls_;

		/// <summary>
		/// Add zoom controls.
		/// </summary>
		/// <param name="bottom">Distance from workspace bottom to bottom of controls.</param>
		/// <returns>Distance from workspace bottom to the top of controls.</returns>
		private double addZoomControls_(double bottom)
		{
			this.zoomControls_ = new ZoomControls(this);
			var svgZoomControls = this.zoomControls_.createDom();
			this.svgGroup_.AppendChild(svgZoomControls);
			return this.zoomControls_.init(bottom);
		}

		internal Flyout flyout_;

		/// <summary>
		/// Add a flyout.
		/// </summary>
		private void addFlyout_()
		{
			var workspaceOptions = new Options {
				disabledPatternId = this.options.disabledPatternId,
				parentWorkspace = this,
				RTL = this.RTL,
				oneBasedIndex = this.options.oneBasedIndex,
				horizontalLayout = this.horizontalLayout,
				toolboxPosition = this.options.toolboxPosition
			};
			/** @type {Blockly.Flyout} */
			this.flyout_ = new Flyout(workspaceOptions);
			this.flyout_.autoClose = false;
			var svgFlyout = this.flyout_.createDom();
			this.svgGroup_.InsertBefore(svgFlyout, this.svgBlockCanvas_);
		}

		/// <summary>
		/// Update items that use screen coordinate calculations
		/// because something has changed (e.g. scroll position, window size).
		/// </summary>
		private void updateScreenCalculations_()
		{
			this.updateInverseScreenCTM();
			this.recordDeleteAreas();
		}

		/// <summary>
		/// Resize the parts of the workspace that change when the workspace
		/// contents (e.g. block positions) change.  This will also scroll the
		/// workspace contents if needed.
		/// </summary>
		public void resizeContents()
		{
			if (this.scrollbar != null) {
				// TODO(picklesrus): Once rachel-fenichel's scrollbar refactoring
				// is complete, call the method that only resizes scrollbar
				// based on contents.
				this.scrollbar.resize();
			}
			this.updateInverseScreenCTM();
		}

		/// <summary>
		/// Resize and reposition all of the workspace chrome (toolbox,
		/// trash, scrollbars etc.)
		/// This should be called when something changes that
		/// requires recalculating dimensions and positions of the
		/// trash, zoom, toolbox, etc. (e.g.window resize).
		/// </summary>
		public void resize()
		{
			if (this.toolbox_ != null) {
				this.toolbox_.position();
			}
			if (this.flyout_ != null) {
				this.flyout_.position();
			}
			if (this.trashcan != null) {
				this.trashcan.position();
			}
			if (this.zoomControls_ != null) {
				this.zoomControls_.position();
			}
			if (this.scrollbar != null) {
				this.scrollbar.resize();
			}
			this.updateScreenCalculations_();
		}

		/// <summary>
		/// Resizes and repositions workspace chrome if the page has a new
		/// scroll position.
		/// </summary>
		internal void updateScreenCalculationsIfScrolled()
		{
			var currScroll = goog.dom.getDocumentScroll();
			if (!goog.math.Coordinate.equals(this.lastRecordedPageScroll_,
				currScroll)) {
				this.lastRecordedPageScroll_ = currScroll;
				this.updateScreenCalculations_();
			}
		}

		private SVGElement svgBlockCanvas_;

		/// <summary>
		/// Get the SVG element that forms the drawing surface.
		/// </summary>
		/// <returns>SVG element.</returns>
		public SVGElement getCanvas()
		{
			return this.svgBlockCanvas_;
		}

		private SVGElement svgBubbleCanvas_;

		/// <summary>
		/// Get the SVG element that forms the bubble surface.
		/// </summary>
		/// <returns>SVG element.</returns>
		public SVGElement getBubbleCanvas()
		{
			return this.svgBubbleCanvas_;
		}

		private SVGElement cachedParentSvg_;

		/// <summary>
		/// Get the SVG element that contains this workspace.
		/// </summary>
		/// <returns>SVG element.</returns>
		public SVGElement getParentSvg()
		{
			if (this.cachedParentSvg_ != null) {
				return this.cachedParentSvg_;
			}
			var element = (Element)this.svgGroup_;
			while (element != null) {
				if (element.TagName == "svg") {
					this.cachedParentSvg_ = (SVGElement)element;
					return (SVGElement)element;
				}
				element = (Element)element.ParentNode;
			}
			return null;
		}

		/// <summary>
		/// Translate this workspace to new coordinates.
		/// </summary>
		/// <param name="x">Horizontal translation.</param>
		/// <param name="y">Vertical translation.</param>
		public void translate(double x, double y)
		{
			var translation = "translate(" + x + "," + y + ") " +
				"scale(" + this.scale + ")";
			this.svgBlockCanvas_.SetAttribute("transform", translation);
			this.svgBubbleCanvas_.SetAttribute("transform", translation);
		}

		/// <summary>
		/// Returns the horizontal offset of the workspace.
		/// Intended for LTR/RTL compatibility in XML.
		/// </summary>
		/// <returns>Width.</returns>
		public override double getWidth()
		{
			var metrics = this.getMetrics();
			return metrics != null ? metrics.viewWidth / this.scale : 0;
		}

		/// <summary>
		/// Toggles the visibility of the workspace.
		/// Currently only intended for main workspace.
		/// </summary>
		/// <param name="isVisible">True if workspace should be visible.</param>
		public void setVisible(bool isVisible)
		{
			this.getParentSvg().style.Display = isVisible ? Display.Block : Display.None;
			if (this.toolbox_ != null) {
				// Currently does not support toolboxes in mutators.
				this.toolbox_.HtmlDiv.Style.Display = isVisible ? Display.Block : Display.None;
			}
			if (isVisible) {
				this.render();
				if (this.toolbox_ != null) {
					this.toolbox_.position();
				}
			}
			else {
				Core.hideChaff(true);
			}
		}

		/// <summary>
		/// Render all blocks in workspace.
		/// </summary>
		public void render()
		{
			// Generate list of all blocks.
			var blocks = this.getAllBlocks();
			// Render each block.
			for (var i = blocks.Length - 1; i >= 0; i--) {
				((BlockSvg)blocks[i]).render(false);
			}
		}

		private bool traceOn_;
		private JsArray<EventWrapInfo> traceWrapper_;

		/// <summary>
		/// Turn the visual trace functionality on or off.
		/// </summary>
		/// <param name="armed">True if the trace should be on.</param>
		public void traceOn(bool armed)
		{
			this.traceOn_ = armed;
			if (this.traceWrapper_ != null) {
				Core.unbindEvent_(this.traceWrapper_);
				this.traceWrapper_ = null;
			}
			if (armed) {
				this.traceWrapper_ = Core.bindEventWithChecks_(this.svgBlockCanvas_,
					"blocklySelectChange", null, new Action<Event>((e) => { this.traceOn_ = false; }));
			}
		}

		/// <summary>
		/// Highlight a block in the workspace.
		/// </summary>
		/// <param name="id">ID of block to find.</param>
		public void highlightBlock(string id)
		{
			if (this.traceOn_ && Core.dragMode_ != Core.DRAG_NONE) {
				// The blocklySelectChange event normally prevents this, but sometimes
				// there is a race condition on fast-executing apps.
				this.traceOn(false);
			}
			if (this.traceOn_) {
				return;
			}
			BlockSvg block = null;
			if (id != null) {
				block = (BlockSvg)this.getBlockById(id);
				if (block == null) {
					return;
				}
			}
			// Temporary turn off the listener for selection changes, so that we don't
			// trip the monitor for detecting user activity.
			this.traceOn(false);
			// Select the current block.
			if (block != null) {
				block.select();
			}
			else if (Core.selected != null) {
				Core.selected.unselect();
			}
			// Restore the monitor for user activity after the selection event has fired.
			var thisWorkspace = this;
			Window.SetTimeout(new Action(() => { thisWorkspace.traceOn(true); }), 1);
		}

		/// <summary>
		/// Paste the provided block onto the workspace.
		/// </summary>
		/// <param name="xmlBlock">XML block element.</param>
		public void paste(Element xmlBlock)
		{
			if (!this.rendered || xmlBlock.GetElementsByTagName("block").Length >=
				this.remainingCapacity()) {
				return;
			}
			Core.terminateDrag_();  // Dragging while pasting?  No.
			Events.disable();
			BlockSvg block;
			try {
				block = (BlockSvg)Xml.domToBlock(xmlBlock, this);
				// Move the duplicate to original position.
				var blockX = Script.ParseFloat(xmlBlock.GetAttribute("x"));
				var blockY = Script.ParseFloat(xmlBlock.GetAttribute("y"));
				if (!Double.IsNaN(blockX) && !Double.IsNaN(blockY)) {
					if (this.RTL) {
						blockX = -blockX;
					}
					// Offset block until not clobbering another block and not in connection
					// distance with neighbouring blocks.
					bool collide;
					do {
						collide = false;
						var allBlocks = this.getAllBlocks();
						foreach (var otherBlock in allBlocks) {
							var otherXY = otherBlock.getRelativeToSurfaceXY();
							if (System.Math.Abs(blockX - otherXY.x) <= 1 &&
								System.Math.Abs(blockY - otherXY.y) <= 1) {
								collide = true;
								break;
							}
						}
						if (!collide) {
							// Check for blocks in snap range to any of its connections.
							var connections = block.getConnections_(false);
							foreach (var connection in connections) {
								var neighbour = connection.closest(Core.SNAP_RADIUS,
									blockX, blockY);
								if (neighbour.connection != null) {
									collide = true;
									break;
								}
							}
						}
						if (collide) {
							if (this.RTL) {
								blockX -= Core.SNAP_RADIUS;
							}
							else {
								blockX += Core.SNAP_RADIUS;
							}
							blockY += Core.SNAP_RADIUS * 2;
						}
					} while (collide);
					block.moveBy(blockX, blockY);
				}
			}
			finally {
				Events.enable();
			}
			if (Events.isEnabled() && !block.isShadow()) {
				Events.fire(new Events.Create(block));
			}
			block.select();
		}

		/// <summary>
		/// Create a new variable with the given name.  Update the flyout to show the new
		/// variable immediately.
		/// TODO: #468
		/// </summary>
		/// <param name="name">The new variable's name.</param>
		public override void createVariable(string name)
		{
			base.createVariable(name);
			// Don't refresh the toolbox if there's a drag in progress.
			if (this.toolbox_ != null && this.toolbox_.flyout_ != null && Flyout.startFlyout_ == null) {
				this.toolbox_.refreshSelection();
			}
		}

		private goog.math.Rect deleteAreaTrash_;
		private goog.math.Rect deleteAreaToolbox_;

		/// <summary>
		/// Make a list of all the delete areas for this workspace.
		/// </summary>
		public void recordDeleteAreas()
		{
			if (this.trashcan != null) {
				this.deleteAreaTrash_ = this.trashcan.getClientRect();
			}
			else {
				this.deleteAreaTrash_ = null;
			}
			if (this.flyout_ != null) {
				this.deleteAreaToolbox_ = this.flyout_.getClientRect();
			}
			else if (this.toolbox_ != null) {
				this.deleteAreaToolbox_ = this.toolbox_.getClientRect();
			}
			else {
				this.deleteAreaToolbox_ = null;
			}
		}

		/// <summary>
		/// Is the mouse ev over a delete area (toolbox or non-closing flyout)?
		/// Opens or closes the trashcan and sets the cursor as a side effect.
		/// </summary>
		/// <param name="e">Mouse move ev.</param>
		/// <returns></returns>
		public bool isDeleteArea(MouseEvent e)
		{
			var xy = new goog.math.Coordinate(e.ClientX, e.ClientY);
			if (this.deleteAreaTrash_ != null) {
				if (this.deleteAreaTrash_.contains(xy)) {
					this.trashcan.setOpen_(true);
					Css.setCursor(Css.Cursor.DELETE);
					return true;
				}
				this.trashcan.setOpen_(false);
			}
			if (this.deleteAreaToolbox_ != null) {
				if (this.deleteAreaToolbox_.contains(xy)) {
					Css.setCursor(Css.Cursor.DELETE);
					return true;
				}
			}
			Css.setCursor(Css.Cursor.CLOSED);
			return false;
		}

		internal double startDragMouseX;
		internal double startDragMouseY;
		internal Metrics startDragMetrics;
		private bool resizesEnabled_;

		/// <summary>
		/// Handle a mouse-down on SVG drawing surface.
		/// </summary>
		/// <param name="e">Mouse down event.</param>
		internal void onMouseDown_(MouseEvent e)
		{
			this.markFocused(e);
			if (Core.isTargetInput_(e)) {
				Touch.clearTouchIdentifier();
				return;
			}
			Core.terminateDrag_();  // In case mouse-up event was lost.
			Core.hideChaff();
			var isTargetWorkspace = e.Target != null && ((Node)e.Target).NodeName != null &&
				(((Node)e.Target).NodeName.ToLowerCase() == "svg" ||
				e.Target == this.svgBackground_);
			if (isTargetWorkspace && Core.selected != null && !this.options.readOnly) {
				// Clicking on the document clears the selection.
				Core.selected.unselect();
			}
			if (Core.isRightButton(e)) {
				// Right-click.
				this.showContextMenu_(e);
				// Since this was a click, not a drag, end the gesture immediately.
				Touch.clearTouchIdentifier();
			}
			else if (this.scrollbar != null) {
				this.dragMode_ = Core.DRAG_BEGIN;
				// Record the current mouse position.
				this.startDragMouseX = e.ClientX;
				this.startDragMouseY = e.ClientY;
				this.startDragMetrics = this.getMetrics();
				this.startScrollX = this.scrollX;
				this.startScrollY = this.scrollY;

				// If this is a touch event then bind to the mouseup so workspace drag mode
				// is turned off and double move events are not performed on a block.
				// See comment in inject.js Blockly.init_ as to why mouseup events are
				// bound to the document instead of the SVG's surface.
				if (Touch.TOUCH_MAP.ContainsKey("mouseup")) {
					Touch.onTouchUpWrapper_ = Touch.onTouchUpWrapper_ ?? new JsArray<EventWrapInfo>();
					Touch.onTouchUpWrapper_.PushRange(
						Core.bindEventWithChecks_(Document.Instance, "mouseup", null,
						new Action<Event>(Core.onMouseUp_)));
				}
				Core.onMouseMoveWrapper_ = Core.onMouseMoveWrapper_ ?? new JsArray<EventWrapInfo>();
				Core.onMouseMoveWrapper_.PushRange(
					Core.bindEventWithChecks_(Document.Instance, "mousemove", null,
					new Action<MouseEvent>(Core.onMouseMove_)));
			}
			// This event has been handled.  No need to bubble up to the document.
			e.StopPropagation();
			e.PreventDefault();
		}

		/// <summary>
		/// Start tracking a drag of an object on this workspace.
		/// </summary>
		/// <param name="e">Mouse down ev.</param>
		/// <param name="xy">Starting location of object.</param>
		public void startDrag(MouseEvent e, goog.math.Coordinate xy)
		{
			// Record the starting offset between the bubble's location and the mouse.
			var point = Core.mouseToSvg(e, this.getParentSvg(),
				this.getInverseScreenCTM());
			// Fix scale of mouse event.
			point.x /= this.scale;
			point.y /= this.scale;
			this.dragDeltaXY_ = goog.math.Coordinate.difference(xy, new goog.math.Coordinate(point.x, point.y));
		}

		/// <summary>
		/// Track a drag of an object on this workspace.
		/// </summary>
		/// <param name="e">Mouse move ev.</param>
		/// <returns>New location of object.</returns>
		public goog.math.Coordinate moveDrag(MouseEvent e)
		{
			var point = Core.mouseToSvg(e, this.getParentSvg(),
				this.getInverseScreenCTM());
			// Fix scale of mouse event.
			point.x /= this.scale;
			point.y /= this.scale;
			return goog.math.Coordinate.sum(this.dragDeltaXY_, new goog.math.Coordinate(point.x, point.y));
		}

		/// <summary>
		/// Is the user currently dragging a block or scrolling the flyout/workspace?
		/// </summary>
		/// <returns>True if currently dragging or scrolling.</returns>
		public bool isDragging()
		{
			return Core.dragMode_ == Core.DRAG_FREE ||
				(Flyout.startFlyout_ != null &&
					Flyout.startFlyout_.dragMode_ == Core.DRAG_FREE) ||
				this.dragMode_ == Core.DRAG_FREE;
		}

		/// <summary>
		/// Handle a mouse-wheel on SVG drawing surface.
		/// </summary>
		/// <param name="e">Mouse wheel event.</param>
		private void onMouseWheel_(MouseEvent e)
		{
			// TODO: Remove terminateDrag and compensate for coordinate skew during zoom.
			Core.terminateDrag_();
			var delta = e.DeltaY > 0 ? -1 : 1;
			var position = Core.mouseToSvg(e, this.getParentSvg(),
				this.getInverseScreenCTM());
			this.zoom(position.x, position.y, delta);
			e.PreventDefault();
		}

		/// <summary>
		/// Calculate the bounding box for the blocks on the workspace.
		/// </summary>
		/// <returns>Contains the position and size of the bounding box
		/// containing the blocks on the workspace.
		/// {x: 0, y: 0, width: 0, height: 0}</returns>
		public SVGRect getBlocksBoundingBox()
		{
			var topBlocks = this.getTopBlocks(false);
			// There are no blocks, return empty rectangle.
			if (topBlocks.Length == 0) {
				return new SVGRect { x = 0, y = 0, width = 0, height = 0 };
			}

			// Initialize boundary using the first block.
			var boundary = ((BlockSvg)topBlocks[0]).getBoundingRectangle();

			// Start at 1 since the 0th block was used for initialization
			for (var i = 1; i < topBlocks.Length; i++) {
				var blockBoundary = ((BlockSvg)topBlocks[i]).getBoundingRectangle();
				if (blockBoundary.topLeft.x < boundary.topLeft.x) {
					boundary.topLeft.x = blockBoundary.topLeft.x;
				}
				if (blockBoundary.bottomRight.x > boundary.bottomRight.x) {
					boundary.bottomRight.x = blockBoundary.bottomRight.x;
				}
				if (blockBoundary.topLeft.y < boundary.topLeft.y) {
					boundary.topLeft.y = blockBoundary.topLeft.y;
				}
				if (blockBoundary.bottomRight.y > boundary.bottomRight.y) {
					boundary.bottomRight.y = blockBoundary.bottomRight.y;
				}
			}
			return new SVGRect {
				x = boundary.topLeft.x,
				y = boundary.topLeft.y,
				width = boundary.bottomRight.x - boundary.topLeft.x,
				height = boundary.bottomRight.y - boundary.topLeft.y
			};
		}

		/// <summary>
		/// Clean up the workspace by ordering all the blocks in a column.
		/// </summary>
		public void cleanUp()
		{
			Events.setGroup(true);
			var topBlocks = this.getTopBlocks(true);
			var cursorY = 0.0;
			foreach (BlockSvg block in topBlocks) {
				var xy = block.getRelativeToSurfaceXY();
				block.moveBy(-xy.x, cursorY - xy.y);
				block.snapToGrid();
				cursorY = block.getRelativeToSurfaceXY().y +
					block.getHeightWidth().height + BlockSvg.MIN_BLOCK_Y;
			}
			Events.setGroup(false);
			// Fire an event to allow scrollbars to resize.
			this.resizeContents();
		}

		/// <summary>
		/// Show the context menu for the workspace.
		/// </summary>
		/// <param name="e">Mouse event.</param>
		private void showContextMenu_(MouseEvent e)
		{
			if (this.options.readOnly || this.isFlyout) {
				return;
			}
			var menuOptions = new JsArray<ContextMenuOption>();
			var topBlocks = this.getTopBlocks(true);
			var eventGroup = Core.genUid();

			// Options to undo/redo previous action.
			var undoOption = new ContextMenuOption();
			undoOption.text = Msg.UNDO;
			undoOption.enabled = this.undoStack_.Length > 0;
			undoOption.callback = (ev) => { this.undo(false); };
			menuOptions.Push(undoOption);
			var redoOption = new ContextMenuOption();
			redoOption.text = Msg.REDO;
			redoOption.enabled = this.redoStack_.Length > 0;
			redoOption.callback = (ev) => { this.undo(true); };
			menuOptions.Push(redoOption);

			// Option to clean up blocks.
			if (this.scrollbar != null) {
				var cleanOption = new ContextMenuOption();
				cleanOption.text = Msg.CLEAN_UP;
				cleanOption.enabled = topBlocks.Length > 1;
				cleanOption.callback = (ev) => { this.cleanUp(); };
				menuOptions.Push(cleanOption);
			}

			// Add a little animation to collapsing and expanding.
			var DELAY = 10;
			if (this.options.collapse) {
				var hasCollapsedBlocks = false;
				var hasExpandedBlocks = false;
				for (var i = 0; i < topBlocks.Length; i++) {
					var block = topBlocks[i];
					while (block != null) {
						if (block.isCollapsed()) {
							hasCollapsedBlocks = true;
						}
						else {
							hasExpandedBlocks = true;
						}
						block = block.getNextBlock();
					}
				}

				/**
				 * Option to collapse or expand top blocks.
				 * @param {boolean} shouldCollapse Whether a block should collapse.
				 * @private
				 */
				var toggleOption = new Action<bool>((shouldCollapse) => {
					var ms = 0;
					for (var i = 0; i < topBlocks.Length; i++) {
						var block = topBlocks[i];
						while (block != null) {
							var b = block;
							Window.SetTimeout(new Action(() => b.setCollapsed(shouldCollapse)), ms);
							block = block.getNextBlock();
							ms += DELAY;
						}
					}
				});

				// Option to collapse top blocks.
				var collapseOption = new ContextMenuOption {
					enabled = hasExpandedBlocks,
					text = Msg.COLLAPSE_ALL,
					callback = (ev) => {
						toggleOption(true);
					}
				};
				menuOptions.Push(collapseOption);

				// Option to expand top blocks.
				var expandOption = new ContextMenuOption {
					enabled = hasCollapsedBlocks,
					text = Msg.EXPAND_ALL,
					callback = (ev) => {
						toggleOption(false);
					}
				};
				menuOptions.Push(expandOption);
			}

			// Option to delete all blocks.
			// Count the number of blocks that are deletable.
			var deleteList = new JsArray<Block>();
			Action<Block> addDeletableBlocks = null;
			addDeletableBlocks = new Action<Block>((block) => {
				if (block.isDeletable()) {
					deleteList.PushRange(block.getDescendants());
				}
				else {
					var children = block.getChildren();
					for (var i = 0; i < children.Length; i++) {
						addDeletableBlocks(children[i]);
					}
				}
			});
			for (var i = 0; i < topBlocks.Length; i++) {
				addDeletableBlocks(topBlocks[i]);
			}

			Action deleteNext = null;
			deleteNext = new Action(() => {
				Events.setGroup(eventGroup);
				var block = (BlockSvg)deleteList.Shift();
				if (block != null) {
					if (block.workspace != null) {
						block.dispose(false, true);
						Window.SetTimeout(deleteNext, DELAY);
					}
					else {
						deleteNext();
					}
				}
				Events.setGroup(false);
			});

			var deleteOption = new ContextMenuOption {
				text = deleteList.Length == 1 ? Msg.DELETE_BLOCK :
					Msg.DELETE_X_BLOCKS.Replace("%1", deleteList.Length.ToString()),
				enabled = deleteList.Length > 0,
				callback = (ev) => {
					if (deleteList.Length < 2 ||
						Window.Confirm(Msg.DELETE_ALL_BLOCKS.Replace("%1",
						deleteList.Length.ToString()))) {
						deleteNext();
					}
				}
			};
			menuOptions.Push(deleteOption);

			ContextMenu.show(e, menuOptions, this.RTL);
		}

		/// <summary>
		/// Load an audio file.  Cache it, ready for instantaneous playing.
		/// </summary>
		/// <param name="filenames">List of file types in decreasing order of
		/// preference (i.e. increasing size).  E.g. ['media/go.mp3', 'media/go.wav']
		/// Filenames include path from Blockly's root.  File extensions matter.</param>
		/// <param name="name">Name of sound.</param>
		internal void loadAudio_(string[] filenames, string name)
		{
			HTMLAudioElement audioTest;
			if (filenames.Length == 0) {
				return;
			}
			try {
				audioTest = Window.NewAudio();
			}
			catch (Exception) {
				// No browser support for HTMLAudioElement.
				// IE can throw an error even if the HTMLAudioElement object exists.
				return;
			}
			HTMLAudioElement sound = null;
			for (var i = 0; i < filenames.Length; i++) {
				var filename = filenames[i];
				var ext = filename.Match(new Regex(@"\.(\w+)$"));
				if (ext != null && audioTest.CanPlayType("audio/" + ext[1]) != null) {
					// Found an audio format we can play.
					sound = Window.NewAudio(filename);
					break;
				}
			}
			if (sound != null /*&& sound.Play*/) {
				this.SOUNDS_[name] = sound;
			}
		}

		/// <summary>
		/// Preload all the audio files so that they play quickly when asked for.
		/// </summary>
		internal void preloadAudio_()
		{
			foreach (var name in this.SOUNDS_.Keys) {
				var sound = this.SOUNDS_[name];
				sound.Volume = .01f;
				sound.Play();
				sound.Pause();
				// iOS can only process one sound at a time.  Trying to load more than one
				// corrupts the earlier ones.  Just load one and leave the others uncached.
				if (goog.userAgent.IPAD || goog.userAgent.IPHONE) {
					break;
				}
			}
		}

		/// <summary>
		/// Play a named sound at specified volume.  If volume is not specified,
		/// use full volume (1).
		/// </summary>
		/// <param name="name">Name of sound.</param>
		/// <param name="opt_volume">Volume of sound (0-1).</param>
		public void playAudio(string name, double opt_volume = 1)
		{
			HTMLAudioElement sound;
			if (this.SOUNDS_.TryGetValue(name, out sound) && sound != null) {
				// Don't play one sound on top of another.
				var now = new Date();
				if (now - this.lastSound_ < Core.SOUND_LIMIT) {
					return;
				}
				this.lastSound_ = now;
				HTMLAudioElement mySound;
				var ie9 = goog.userAgent.DOCUMENT_MODE != 0 &&
						  goog.userAgent.DOCUMENT_MODE == 9;
				if (ie9 || goog.userAgent.IPAD || goog.userAgent.ANDROID) {
					// Creating a new audio node causes lag in IE9, Android and iPad. Android
					// and IE9 refetch the file from the server, iPad uses a singleton audio
					// node which must be deleted and recreated for each new audio tag.
					mySound = sound;
				}
				else {
					mySound = (HTMLAudioElement)sound.CloneNode();
				}
				mySound.Volume = opt_volume;
				mySound.Play();
			}
			else if (this.options.parentWorkspace != null) {
				// Maybe a workspace on a lower level knows about this sound.
				((WorkspaceSvg)this.options.parentWorkspace).playAudio(name, opt_volume);
			}
		}

		/// <summary>
		/// Modify the block tree on the existing toolbox.
		/// </summary>
		/// <param name="tree">DOM tree of blocks, or text representation of same.</param>
		public void updateToolbox(Union<string, Element> _tree)
		{
			Element tree = Options.parseToolboxTree(_tree);
			if (tree == null) {
				if (this.options.languageTree != null) {
					throw new Exception("Can't nullify an existing toolbox.");
				}
				return;  // No change (null to null).
			}
			if (this.options.languageTree == null) {
				throw new Exception("Existing toolbox is null.  Can\'t create new toolbox.");
			}
			if (tree.GetElementsByTagName("category").Length != 0) {
				if (this.toolbox_ == null) {
					throw new Exception("Existing toolbox has no categories.  Can\'t change mode.");
				}
				this.options.languageTree = tree;
				this.toolbox_.populate_(tree);
				this.toolbox_.addColour_();
			}
			else {
				if (this.flyout_ == null) {
					throw new Exception("Existing toolbox has categories.  Can\'t change mode.");
				}
				this.options.languageTree = tree;
				this.flyout_.show(new JsArray<Node>(tree.ChildNodes));
			}
		}

		/// <summary>
		/// Mark this workspace as the currently focused main workspace.
		/// </summary>
		public void markFocused(Event e)
		{
			if (this.options.parentWorkspace != null) {
				((WorkspaceSvg)this.options.parentWorkspace).markFocused(e);
			}
			else {
				Core.mainWorkspace = this;
			}
		}

		/// <summary>
		/// Zooming the blocks centered in (x, y) coordinate with zooming in or out.
		/// </summary>
		/// <param name="x">X coordinate of center.</param>
		/// <param name="y">Y coordinate of center.</param>
		/// <param name="type">Type of zooming (-1 zooming out and 1 zooming in).</param>
		public void zoom(double x, double y, double type)
		{
			var speed = this.options.zoomOptions.scaleSpeed;
			var metrics = this.getMetrics();
			var center = this.getParentSvg().createSVGPoint();
			center.x = x;
			center.y = y;
			center = center.matrixTransform(this.getCanvas().getCTM().Inverse());
			x = center.x;
			y = center.y;
			var canvas = this.getCanvas();
			// Scale factor.
			var scaleChange = (type == 1) ? speed : 1 / speed;
			// Clamp scale within valid range.
			var newScale = this.scale * scaleChange;
			if (newScale > this.options.zoomOptions.maxScale) {
				scaleChange = this.options.zoomOptions.maxScale / this.scale;
			}
			else if (newScale < this.options.zoomOptions.minScale) {
				scaleChange = this.options.zoomOptions.minScale / this.scale;
			}
			if (this.scale == newScale) {
				return;  // No change in zoom.
			}
			if (this.scrollbar != null) {
				var matrix = canvas.getCTM()
					.Translate((float)(x * (1 - scaleChange)), (float)(y * (1 - scaleChange)))
					.Scale((float)scaleChange);
				// newScale and matrix.a should be identical (within a rounding error).
				this.scrollX = matrix.e - metrics.absoluteLeft;
				this.scrollY = matrix.f - metrics.absoluteTop;
			}
			this.setScale(newScale);
		}

		/// <summary>
		/// Zooming the blocks centered in the center of view with zooming in or out.
		/// </summary>
		/// <param name="type"></param>
		public void zoomCenter(double type)
		{
			var metrics = this.getMetrics();
			var x = metrics.viewWidth / 2;
			var y = metrics.viewHeight / 2;
			this.zoom(x, y, type);
		}

		/// <summary>
		/// Zoom the blocks to fit in the workspace if possible.
		/// </summary>
		public void zoomToFit()
		{
			var metrics = this.getMetrics();
			var blocksBox = this.getBlocksBoundingBox();
			var blocksWidth = blocksBox.width;
			var blocksHeight = blocksBox.height;
			if (blocksWidth == 0.0) {
				return;  // Prevents zooming to infinity.
			}
			var workspaceWidth = metrics.viewWidth;
			var workspaceHeight = metrics.viewHeight;
			if (this.flyout_ != null) {
				workspaceWidth -= this.flyout_.width_;
			}
			if (this.scrollbar == null) {
				// Orgin point of 0,0 is fixed, blocks will not scroll to center.
				blocksWidth += metrics.contentLeft;
				blocksHeight += metrics.contentTop;
			}
			var ratioX = workspaceWidth / blocksWidth;
			var ratioY = workspaceHeight / blocksHeight;
			this.setScale(System.Math.Min(ratioX, ratioY));
			this.scrollCenter();
		}

		/// <summary>
		/// Center the workspace.
		/// </summary>
		public void scrollCenter()
		{
			if (this.scrollbar == null) {
				// Can't center a non-scrolling workspace.
				return;
			}
			var metrics = this.getMetrics();
			var x = (metrics.contentWidth - metrics.viewWidth) / 2;
			if (this.flyout_ != null) {
				x -= this.flyout_.width_ / 2;
			}
			var y = (metrics.contentHeight - metrics.viewHeight) / 2;
			this.scrollbar.set(x, y);
		}

		/// <summary>
		/// Set the workspace's zoom factor.
		/// </summary>
		/// <param name="newScale">Zoom factor.</param>
		public void setScale(double newScale)
		{
			if (this.options.zoomOptions.maxScale != 0.0 &&
				newScale > this.options.zoomOptions.maxScale) {
				newScale = this.options.zoomOptions.maxScale;
			}
			else if (this.options.zoomOptions.minScale != 0.0 &&
			  newScale < this.options.zoomOptions.minScale) {
				newScale = this.options.zoomOptions.minScale;
			}
			this.scale = newScale;
			this.updateGridPattern_();
			if (this.scrollbar != null) {
				this.scrollbar.resize();
			}
			else {
				this.translate(this.scrollX, this.scrollY);
			}
			Core.hideChaff(false);
			if (this.flyout_ != null) {
				// No toolbox, resize flyout.
				this.flyout_.reflow();
			}
		}

		/// <summary>
		/// Updates the grid pattern.
		/// </summary>
		private void updateGridPattern_()
		{
			if (this.options.gridPattern == null) {
				return;  // No grid.
			}
			// MSIE freaks if it sees a 0x0 pattern, so set empty patterns to 100x100.
			var safeSpacing = (this.options.gridOptions.spacing * this.scale);
			if (safeSpacing == 0.0) safeSpacing = 100.0;
			this.options.gridPattern.SetAttribute("width", safeSpacing.ToString());
			this.options.gridPattern.SetAttribute("height", safeSpacing.ToString());
			var half = System.Math.Floor(this.options.gridOptions.spacing / 2) + 0.5;
			var start = half - this.options.gridOptions.length / 2;
			var end = half + this.options.gridOptions.length / 2;
			var line1 = (SVGElement)this.options.gridPattern.FirstChild;
			var line2 = (SVGElement)line1?.NextSibling;
			half *= this.scale;
			start *= this.scale;
			end *= this.scale;
			if (line1 != null) {
				line1.SetAttribute("stroke-width", this.scale.ToString());
				line1.SetAttribute("x1", start.ToString());
				line1.SetAttribute("y1", half.ToString());
				line1.SetAttribute("x2", end.ToString());
				line1.SetAttribute("y2", half.ToString());
			}
			if (line2 != null) {
				line2.SetAttribute("stroke-width", this.scale.ToString());
				line2.SetAttribute("x1", half.ToString());
				line2.SetAttribute("y1", start.ToString());
				line2.SetAttribute("x2", half.ToString());
				line2.SetAttribute("y2", end.ToString());
			}
		}

		/// <summary>
		/// Return an object with all the metrics required to size scrollbars for a
		/// top level workspace.  The following properties are computed:
		/// .viewHeight: Height of the visible rectangle,
		/// .viewWidth: Width of the visible rectangle,
		/// .contentHeight: Height of the contents,
		/// .contentWidth: Width of the content,
		/// .viewTop: Offset of top edge of visible rectangle from parent,
		/// .viewLeft: Offset of left edge of visible rectangle from parent,
		/// .contentTop: Offset of the top-most content from the y=0 coordinate,
		/// .contentLeft: Offset of the left-most content from the x=0 coordinate.
		/// .absoluteTop: Top-edge of view.
		/// .absoluteLeft: Left-edge of view.
		/// .toolboxWidth: Width of toolbox, if it exists.  Otherwise zero.
		/// .toolboxHeight: Height of toolbox, if it exists.  Otherwise zero.
		/// .flyoutWidth: Width of the flyout if it is always open.  Otherwise zero.
		/// .flyoutHeight: Height of flyout if it is always open.  Otherwise zero.
		/// .toolboxPosition: Top, bottom, left or right.
		/// </summary>
		/// <returns>Contains size and position metrics of a top level
		/// workspace.</returns>
		private Metrics getTopLevelWorkspaceMetrics_()
		{
			var svgSize = Core.svgSize(this.getParentSvg());
			if (this.toolbox_ != null) {
				if (this.toolboxPosition == Core.TOOLBOX_AT_TOP ||
					this.toolboxPosition == Core.TOOLBOX_AT_BOTTOM) {
					svgSize.height -= this.toolbox_.getHeight();
				}
				else if (this.toolboxPosition == Core.TOOLBOX_AT_LEFT ||
				  this.toolboxPosition == Core.TOOLBOX_AT_RIGHT) {
					svgSize.width -= this.toolbox_.getWidth();
				}
			}
			// Set the margin to match the flyout's margin so that the workspace does
			// not jump as blocks are added.
			var MARGIN = Flyout.CORNER_RADIUS - 1;
			var viewWidth = svgSize.width - MARGIN;
			var viewHeight = svgSize.height - MARGIN;
			var blockBox = this.getBlocksBoundingBox();

			// Fix scale.
			var contentWidth = blockBox.width * this.scale;
			var contentHeight = blockBox.height * this.scale;
			var contentX = blockBox.x * this.scale;
			var contentY = blockBox.y * this.scale;
			double leftEdge;
			double rightEdge;
			double topEdge;
			double bottomEdge;
			if (this.scrollbar != null) {
				// Add a border around the content that is at least half a screenful wide.
				// Ensure border is wide enough that blocks can scroll over entire screen.
				leftEdge = System.Math.Min(contentX - viewWidth / 2,
										contentX + contentWidth - viewWidth);
				rightEdge = System.Math.Max(contentX + contentWidth + viewWidth / 2,
										contentX + viewWidth);
				topEdge = System.Math.Min(contentY - viewHeight / 2,
										contentY + contentHeight - viewHeight);
				bottomEdge = System.Math.Max(contentY + contentHeight + viewHeight / 2,
										contentY + viewHeight);
			}
			else {
				leftEdge = blockBox.x;
				rightEdge = leftEdge + blockBox.width;
				topEdge = blockBox.y;
				bottomEdge = topEdge + blockBox.height;
			}
			var absoluteLeft = 0.0;
			if (this.toolbox_ != null && this.toolboxPosition == Core.TOOLBOX_AT_LEFT) {
				absoluteLeft = this.toolbox_.getWidth();
			}
			var absoluteTop = 0.0;
			if (this.toolbox_ != null && this.toolboxPosition == Core.TOOLBOX_AT_TOP) {
				absoluteTop = this.toolbox_.getHeight();
			}

			var metrics = new Metrics {
				viewHeight = svgSize.height,
				viewWidth = svgSize.width,
				contentHeight = bottomEdge - topEdge,
				contentWidth = rightEdge - leftEdge,
				viewTop = -this.scrollY,
				viewLeft = -this.scrollX,
				contentTop = topEdge,
				contentLeft = leftEdge,
				absoluteTop = absoluteTop,
				absoluteLeft = absoluteLeft,
				toolboxWidth = this.toolbox_ != null ? this.toolbox_.getWidth() : 0,
				toolboxHeight = this.toolbox_ != null ? this.toolbox_.getHeight() : 0,
				flyoutWidth = this.flyout_ != null ? this.flyout_.getWidth() : 0,
				flyoutHeight = this.flyout_ != null ? this.flyout_.getHeight() : 0,
				toolboxPosition = this.toolboxPosition
			};
			return metrics;
		}

		/// <summary>
		/// Sets the X/Y translations of a top level workspace to match the scrollbars.
		/// </summary>
		/// <param name="xyRatio">Contains an x and/or y property which is a float
		/// between 0 and 1 specifying the degree of scrolling.</param>
		private void setTopLevelWorkspaceMetrics_(Metrics xyRatio)
		{
			if (this.scrollbar == null) {
				throw new Exception("Attempt to set top level workspace scroll without scrollbars.");
			}
			var metrics = this.getMetrics();
			if (xyRatio.x != 0.0) {
				this.scrollX = -metrics.contentWidth * xyRatio.x - metrics.contentLeft;
			}
			if (xyRatio.y != 0.0) {
				this.scrollY = -metrics.contentHeight * xyRatio.y - metrics.contentTop;
			}
			var x = this.scrollX + metrics.absoluteLeft;
			var y = this.scrollY + metrics.absoluteTop;
			this.translate(x, y);
			if (this.options.gridPattern != null) {
				this.options.gridPattern.SetAttribute("x", x.ToString());
				this.options.gridPattern.SetAttribute("y", y.ToString());
				if (goog.userAgent.IE) {
					// IE doesn't notice that the x/y offsets have changed.  Force an update.
					this.updateGridPattern_();
				}
			}
		}

		/// <summary>
		/// Update whether this workspace has resizes enabled.
		/// If enabled, workspace will resize when appropriate.
		/// If disabled, workspace will not resize until re-enabled.
		/// Use to avoid resizing during a batch operation, for performance.
		/// </summary>
		/// <param name="enabled">Whether resizes should be enabled.</param>
		public void setResizesEnabled(bool enabled)
		{
			var reenabled = (!this.resizesEnabled_ && enabled);
			this.resizesEnabled_ = enabled;
			if (reenabled) {
				// Newly enabled.  Trigger a resize.
				this.resizeContents();
			}
		}

		/// <summary>
		/// Dispose of all blocks in workspace, with an optimization to prevent resizes.
		/// </summary>
		public override void clear()
		{
			this.setResizesEnabled(false);
			base.clear();
			this.setResizesEnabled(true);
		}
	}
}
