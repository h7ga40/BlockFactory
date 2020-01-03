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
 * @fileoverview Methods for graphically rendering a block as SVG.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public partial class BlockSvg : Block
	{
		internal SVGElement svgGroup_;
		private SVGElement svgPathDark_;
		internal SVGElement svgPath_;
		private SVGElement svgPathLight_;
		internal SVGElement flyoutRect_;

		/// <summary>
		/// Class for a block's SVG representation.
		/// Not normally called directly, workspace.newBlock() is preferred.
		/// </summary>
		/// <param name="workspace">The block's workspace.</param>
		/// <param name="prototypeName">Name of the language object containing
		/// type-specific functions for this block.</param>
		/// <param name="opt_id">Optional ID.  Use this ID if provided, otherwise
		/// create a new id.</param>
		public BlockSvg(Workspace workspace, string prototypeName)
			: base(workspace, prototypeName)
		{
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>(), null);
			this.svgPathDark_ = Core.createSvgElement("path", new Dictionary<string, object>() {
				{ "class", "blocklyPathDark" }, { "transform", "translate(1,1)" } },
				this.svgGroup_);
			this.svgPath_ = Core.createSvgElement("path", new Dictionary<string, object>() {
				{ "class", "blocklyPath"} },
				this.svgGroup_);
			this.svgPathLight_ = Core.createSvgElement("path", new Dictionary<string, object>() {
				{ "class", "blocklyPathLight" } }, this.svgGroup_);
			this.svgPath_.tooltip = this;
			this.rendered = false;

			Tooltip.bindMouseEvents(this.svgPath_);
		}

		/// <summary>
		/// Height of this block, not including any statement blocks above or below.
		/// </summary>
		public double height = 0;

		/// <summary>
		/// Width of this block, including any connected value blocks.
		/// </summary>
		public double width = 0;

		/// <summary>
		/// Original location of block being dragged.
		/// </summary>
		private goog.math.Coordinate dragStartXY_ = null;

		/// <summary>
		/// Constant for identifying rows that are to be rendered inline.
		/// Don't collide with Blockly.INPUT_VALUE and friends.
		/// </summary>
		public const int INLINE = -1;

		private bool eventsInit_;

		/// <summary>
		/// Create and initialize the SVG representation of the block.
		/// May be called more than once.
		/// </summary>
		public void initSvg()
		{
			goog.asserts.assert(this.workspace.rendered, "Workspace is headless.");
			foreach (var input in this.inputList) {
				input.init();
			}
			var icons = this.getIcons();
			for (var i = 0; i < icons.Length; i++) {
				icons[i].createIcon();
			}
			this.updateColour();
			this.updateMovable();
			if (!this.workspace.options.readOnly && !this.eventsInit_) {
				Core.bindEventWithChecks_(this.getSvgRoot(), "mousedown", this,
					new Action<MouseEvent>(this.onMouseDown_));
				var thisBlock = this;
				Core.bindEventWithChecks_(this.getSvgRoot(), "touchstart", null,
					new Action<TouchEvent>((e) => { Core.longStart_(e, thisBlock); }));
			}
			this.eventsInit_ = true;

			if (this.getSvgRoot().ParentNode == null) {
				((WorkspaceSvg)this.workspace).getCanvas().AppendChild(this.getSvgRoot());
			}
		}

		/// <summary>
		/// Select this block.  Highlight it visually.
		/// </summary>
		public void select()
		{
			if (this.isShadow() && this.getParent() != null) {
				// Shadow blocks should not be selected.
				((BlockSvg)this.getParent()).select();
				return;
			}
			if (Core.selected == this) {
				return;
			}
			string oldId = null;
			if (Core.selected != null) {
				oldId = Core.selected.id;
				// Unselect any previously selected block.
				Events.disable();
				try {
					Core.selected.unselect();
				}
				finally {
					Events.enable();
				}
			}
			var ev = new Events.Ui(null, "selected", oldId, this.id);
			ev.workspaceId = this.workspace.id;
			Events.fire(ev);
			Core.selected = this;
			this.addSelect();
		}

		/// <summary>
		/// Unselect this block.  Remove its highlighting.
		/// </summary>
		public void unselect()
		{
			if (Core.selected != this) {
				return;
			}
			var ev = new Events.Ui(null, "selected", this.id, null);
			ev.workspaceId = this.workspace.id;
			Events.fire(ev);
			Core.selected = null;
			this.removeSelect();
		}

		/// <summary>
		/// Block's mutator icon (if any).
		/// </summary>
		public Mutator mutator = null;

		/// <summary>
		/// Block's comment icon (if any).
		/// </summary>
		public Comment comment = null;

		/// <summary>
		/// Block's warning icon (if any).
		/// </summary>
		public Warning warning = null;

		/// <summary>
		/// Returns a list of mutator, comment, and warning icons.
		/// </summary>
		/// <returns>List of icons.</returns>
		public Icon[] getIcons()
		{
			var icons = new JsArray<Icon>();
			if (this.mutator != null) {
				icons.Push(this.mutator);
			}
			if (this.comment != null) {
				icons.Push(this.comment);
			}
			if (this.warning != null) {
				icons.Push(this.warning);
			}
			return icons;
		}

		/// <summary>
		/// Wrapper function called when a mouseUp occurs during a drag operation.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseUpWrapper_;

		/// <summary>
		/// Wrapper function called when a mouseMove occurs during a drag operation.
		/// </summary>
		private static JsArray<EventWrapInfo> onMouseMoveWrapper_;

		/// <summary>
		/// Stop binding to the global mouseup and mousemove events.
		/// </summary>
		public static void terminateDrag()
		{
			BlockSvg.disconnectUiStop_();
			if (BlockSvg.onMouseUpWrapper_ != null) {
				Core.unbindEvent_(BlockSvg.onMouseUpWrapper_);
				BlockSvg.onMouseUpWrapper_ = null;
			}
			if (BlockSvg.onMouseMoveWrapper_ != null) {
				Core.unbindEvent_(BlockSvg.onMouseMoveWrapper_);
				BlockSvg.onMouseMoveWrapper_ = null;
			}
			var selected = Core.selected;
			if (Core.dragMode_ == Core.DRAG_FREE) {
				// Terminate a drag operation.
				if (selected != null) {
					// Update the connection locations.
					var xy = selected.getRelativeToSurfaceXY();
					var dxy = goog.math.Coordinate.difference(xy, selected.dragStartXY_);
					var ev = new Events.Move(selected);
					ev.oldCoordinate = selected.dragStartXY_;
					ev.recordNew();
					Events.fire(ev);

					selected.moveConnections_(dxy.x, dxy.y);
					Script.Delete(ref ((BlockSvg)selected).draggedBubbles_);
					selected.setDragging_(false);
					selected.render();
					((WorkspaceSvg)selected.workspace).setResizesEnabled(true);
					// Ensure that any stap and bump are part of this move's event group.
					var group = Events.getGroup();
					Window.SetTimeout(() => {
						Events.setGroup(group);
						selected.snapToGrid();
						Events.setGroup(false);
					}, Core.BUMP_DELAY / 2);
					Window.SetTimeout(() => {
						Events.setGroup(group);
						selected.bumpNeighbours_();
						Events.setGroup(false);
					}, Core.BUMP_DELAY);
					// Fire an event to allow scrollbars to resize.
					((WorkspaceSvg)selected.workspace).resizeContents();
				}
			}
			Core.dragMode_ = Core.DRAG_NONE;
			Css.setCursor(Css.Cursor.OPEN);
		}

		/// <summary>
		/// Set parent of this block to be a new block or null.
		/// </summary>
		/// <param name="newParent">New parent block.</param>
		public override void setParent(Block newParent)
		{
			if (newParent == this.parentBlock_) {
				return;
			}
			var svgRoot = this.getSvgRoot();
			if (this.parentBlock_ != null && svgRoot != null) {
				// Move this block up the DOM.  Keep track of x/y translations.
				var xy = this.getRelativeToSurfaceXY();
				((WorkspaceSvg)this.workspace).getCanvas().AppendChild(svgRoot);
				svgRoot.SetAttribute("transform", "translate(" + xy.x + "," + xy.y + ")");
			}

			Field.startCache();
			base.setParent(newParent);
			Field.stopCache();

			if (newParent != null) {
				var oldXY = this.getRelativeToSurfaceXY();
				((BlockSvg)newParent).getSvgRoot().AppendChild(svgRoot);
				var newXY = this.getRelativeToSurfaceXY();
				// Move the connections to match the child's new position.
				this.moveConnections_(newXY.x - oldXY.x, newXY.y - oldXY.y);
			}
		}

		/// <summary>
		/// Return the coordinates of the top-left corner of this block relative to the
		/// drawing surface's origin (0,0).
		/// </summary>
		/// <returns>Object with .x and .y properties.</returns>
		public override goog.math.Coordinate getRelativeToSurfaceXY()
		{
			var x = 0.0;
			var y = 0.0;
			var end = ((WorkspaceSvg)this.workspace).getCanvas();
			var element = (Element)this.getSvgRoot();
			if (element != null) {
				do {
					// Loop through this block and every parent.
					var xy = Core.getRelativeXY_(element);
					x += xy.x;
					y += xy.y;
					element = (Element)element.ParentNode;
				} while (element != null && element != end);
			}
			return new goog.math.Coordinate(x, y);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		public override void moveBy(double dx, double dy)
		{
			goog.asserts.assert(this.parentBlock_ == null, "Block has parent.");
			var ev = new Events.Move(this);
			var xy = this.getRelativeToSurfaceXY();
			this.getSvgRoot().SetAttribute("transform",
				"translate(" + (xy.x + dx) + "," + (xy.y + dy) + ")");
			this.moveConnections_(dx, dy);
			ev.recordNew();
			((WorkspaceSvg)this.workspace).resizeContents();
			Events.fire(ev);
		}

		/// <summary>
		/// Snap this block to the nearest grid point.
		/// </summary>
		public void snapToGrid()
		{
			if (this.workspace == null) {
				return;  // Deleted block.
			}
			if (Core.dragMode_ != Core.DRAG_NONE) {
				return;  // Don't bump blocks during a drag.
			}
			if (this.getParent() != null) {
				return;  // Only snap top-level blocks.
			}
			if (this.isInFlyout) {
				return;  // Don't move blocks around in a flyout.
			}
			if (this.workspace.options.gridOptions == null ||
				!this.workspace.options.gridOptions.snap) {
				return;  // Config says no snapping.
			}
			var spacing = this.workspace.options.gridOptions.spacing;
			var half = spacing / 2;
			var xy = this.getRelativeToSurfaceXY();
			var dx = System.Math.Round((xy.x - half) / spacing) * spacing + half - xy.x;
			var dy = System.Math.Round((xy.y - half) / spacing) * spacing + half - xy.y;
			dx = System.Math.Round(dx);
			dy = System.Math.Round(dy);
			if (dx != 0 || dy != 0) {
				this.moveBy(dx, dy);
			}
		}

		/// <summary>
		/// Returns a bounding box describing the dimensions of this block
		/// and any blocks stacked below it.
		/// </summary>
		/// <returns>Object with height and width properties.</returns>
		public goog.math.Size getHeightWidth()
		{
			var height = this.height;
			var width = this.width;
			// Recursively add size of subsequent blocks.
			var nextBlock = this.getNextBlock();
			if (nextBlock != null) {
				var nextHeightWidth = ((BlockSvg)nextBlock).getHeightWidth();
				height += nextHeightWidth.height - 4;  // Height of tab.
				width = System.Math.Max(width, nextHeightWidth.width);
			}
			else if (this.nextConnection == null && this.outputConnection == null) {
				// Add a bit of margin under blocks with no bottom tab.
				height += 2;
			}
			return new goog.math.Size() { height = height, width = width };
		}

		/// <summary>
		/// Returns the coordinates of a bounding box describing the dimensions of this
		/// block and any blocks stacked below it.
		/// </summary>
		/// <returns>Object with top left and bottom right coordinates of the bounding box.</returns>
		public Rectangle getBoundingRectangle()
		{
			var blockXY = this.getRelativeToSurfaceXY();
			var tab = this.outputConnection != null ? BlockSvg.TAB_WIDTH : 0;
			var blockBounds = this.getHeightWidth();
			goog.math.Coordinate topLeft;
			goog.math.Coordinate bottomRight;
			if (this.RTL) {
				// Width has the tab built into it already so subtract it here.
				topLeft = new goog.math.Coordinate(blockXY.x - (blockBounds.width - tab),
					blockXY.y);
				// Add the width of the tab/puzzle piece knob to the x coordinate
				// since X is the corner of the rectangle, not the whole puzzle piece.
				bottomRight = new goog.math.Coordinate(blockXY.x + tab,
					blockXY.y + blockBounds.height);
			}
			else {
				// Subtract the width of the tab/puzzle piece knob to the x coordinate
				// since X is the corner of the rectangle, not the whole puzzle piece.
				topLeft = new goog.math.Coordinate(blockXY.x - tab, blockXY.y);
				// Width has the tab built into it already so subtract it here.
				bottomRight = new goog.math.Coordinate(blockXY.x + blockBounds.width - tab,
					blockXY.y + blockBounds.height);
			}
			return new Rectangle() { topLeft = topLeft, bottomRight = bottomRight };
		}

		/// <summary>
		/// Set whether the block is collapsed or not.
		/// </summary>
		/// <param name="collapsed">True if collapsed.</param>
		public override void setCollapsed(bool collapsed)
		{
			if (this.collapsed_ == collapsed) {
				return;
			}
			var renderList = new JsArray<Block>();
			// Show/hide the inputs.
			foreach (var input in this.inputList) {
				Array.ForEach(input.setVisible(!collapsed), (i) => renderList.Push(i));
			}

			var COLLAPSED_INPUT_NAME = "_TEMP_COLLAPSED_INPUT";
			if (collapsed) {
				var icons = this.getIcons();
				for (var i = 0; i < icons.Length; i++) {
					icons[i].setVisible(false);
				}
				var text = this.toString(Core.COLLAPSE_CHARS);
				this.appendDummyInput(COLLAPSED_INPUT_NAME).appendField(text).init();
			}
			else {
				this.removeInput(COLLAPSED_INPUT_NAME);
				// Clear any warnings inherited from enclosed blocks.
				this.setWarningText(null);
			}
			base.setCollapsed(collapsed);

			if (renderList.Length == 0) {
				// No child blocks, just render this block.
				renderList.Push(this);
			}
			if (this.rendered) {
				foreach (BlockSvg block in renderList) {
					block.render();
				}
				// Don't bump neighbours.
				// Although bumping neighbours would make sense, users often collapse
				// all their functions and store them next to each other.  Expanding and
				// bumping causes all their definitions to go out of alignment.
			}
		}

		/// <summary>
		/// Open the next (or previous) FieldTextInput.
		/// </summary>
		/// <param name="start">Current location.</param>
		/// <param name="forward">If true go forward, otherwise backward.</param>
		public void tab(Union<Field, BlockSvg> start, bool forward)
		{
			// This function need not be efficient since it runs once on a keypress.
			// Create an ordered list of all text fields and connected inputs.
			var list = new JsArray<Union<Field, BlockSvg>>();
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					if (field is FieldTextInput) {
						// TODO: Also support dropdown fields.
						list.Push(field);
					}
				}
				if (input.connection != null) {
					var block = (BlockSvg)input.connection.targetBlock();
					if (block != null) {
						list.Push(block);
					}
				}
			}
			var i = Array.IndexOf(list, start.Value);
			if (i == -1) {
				// No start location, start at the beginning or end.
				i = forward ? -1 : list.Length;
			}
			i = forward ? i + 1 : i - 1;
			var target = (i >= 0 && i < list.Length) ? list[i] : null;
			if (target == null) {
				// Ran off of list.
				var parent = (BlockSvg)this.getParent();
				if (parent != null) {
					parent.tab(this, forward);
				}
			}
			else if (target.Is<Field>()) {
				target.As<Field>().showEditor_();
			}
			else {
				target.As<BlockSvg>().tab(null, forward);
			}
		}

		private JsArray<goog.math.Coordinate> draggedBubbles_;

		/// <summary>
		/// Handle a mouse-down on an SVG block.
		/// </summary>
		/// <param name="e">Mouse down event or touch start event.</param>
		internal void onMouseDown_(MouseEvent e)
		{
			if (this.workspace.options.readOnly) {
				return;
			}
			if (this.isInFlyout) {
				// longStart's simulation of right-clicks for longpresses on touch devices
				// calls the onMouseDown_ function defined on the prototype of the object
				// the was longpressed (in this case, a Blockly.BlockSvg).  In this case
				// that behaviour is wrong, because Blockly.Flyout.prototype.blockMouseDown
				// should be called for a mousedown on a block in the flyout, which blocks
				// execution of the block's onMouseDown_ function.
				if (e.Type == "touchstart" && Core.isRightButton(e)) {
					Flyout.blockRightClick_(e, this);
					e.StopPropagation();
					e.PreventDefault();
				}
				return;
			}
			if (this.isInMutator) {
				// Mutator's coordinate system could be out of date because the bubble was
				// dragged, the block was moved, the parent workspace zoomed, etc.
				((WorkspaceSvg)this.workspace).resize();
			}

			((WorkspaceSvg)this.workspace).updateScreenCalculationsIfScrolled();
			((WorkspaceSvg)this.workspace).markFocused(e);
			Core.terminateDrag_();
			this.select();
			Core.hideChaff();
			if (Core.isRightButton(e)) {
				// Right-click.
				this.showContextMenu_(e);
				// Click, not drag, so stop waiting for other touches from this identifier.
				Touch.clearTouchIdentifier();
			}
			else if (!this.isMovable()) {
				// Allow immovable blocks to be selected and context menued, but not
				// dragged.  Let this event bubble up to document, so the workspace may be
				// dragged instead.
				return;
			}
			else {
				if (Events.getGroup() == null) {
					Events.setGroup(true);
				}
				// Left-click (or middle click)
				Css.setCursor(Css.Cursor.CLOSED);

				this.dragStartXY_ = this.getRelativeToSurfaceXY();
				((WorkspaceSvg)this.workspace).startDrag(e, this.dragStartXY_);

				Core.dragMode_ = Core.DRAG_STICKY;
				BlockSvg.onMouseUpWrapper_ = Core.bindEventWithChecks_(Document.Instance,
					"mouseup", this, new Action<MouseEvent>(this.onMouseUp_));
				BlockSvg.onMouseMoveWrapper_ = Core.bindEventWithChecks_(
					Document.Instance, "mousemove", this, new Action<MouseEvent>(this.onMouseMove_));
				// Build a list of bubbles that need to be moved and where they started.
				this.draggedBubbles_ = new JsArray<goog.math.Coordinate>();
				var descendants = this.getDescendants();
				foreach (var descendant in descendants) {
					var icons = ((BlockSvg)descendant).getIcons();
					for (var j = 0; j < icons.Length; j++) {
						var data = icons[j].getIconLocation();
						data["bubble"] = icons[j];
						this.draggedBubbles_.Push(data);
					}
				}
			}
			// This event has been handled.  No need to bubble up to the document.
			e.StopPropagation();
			e.PreventDefault();
		}

		/// <summary>
		/// Handle a mouse-up anywhere in the SVG pane.  Is only registered when a
		/// block is clicked.  We can't use mouseUp on the block since a fast-moving
		/// cursor can briefly escape the block before it catches up.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void onMouseUp_(MouseEvent e)
		{
			Touch.clearTouchIdentifier();
			if (Core.dragMode_ != Core.DRAG_FREE &&
				!WidgetDiv.isVisible()) {
				Events.fire(
					new Events.Ui(this, "click", null, null));
			}
			Core.terminateDrag_();
			if (Core.selected != null && Core.highlightedConnection_ != null) {
				// Connect two blocks together.
				Core.localConnection_.connect(Core.highlightedConnection_);
				if (this.rendered) {
					// Trigger a connection animation.
					// Determine which connection is inferior (lower in the source stack).
					var inferiorConnection = Core.localConnection_.isSuperior() ?
						Core.highlightedConnection_ : Core.localConnection_;
					((BlockSvg)inferiorConnection.getSourceBlock()).connectionUiEffect();
				}
				if (((WorkspaceSvg)this.workspace).trashcan != null) {
					// Don't throw an object in the trash can if it just got connected.
					((WorkspaceSvg)this.workspace).trashcan.close();
				}
			}
			else if (this.getParent() == null && Core.selected.isDeletable() &&
			  ((WorkspaceSvg)this.workspace).isDeleteArea(e)) {
				var trashcan = ((WorkspaceSvg)this.workspace).trashcan;
				if (trashcan != null) {
					goog.Timer.callOnce(trashcan.close, 100, trashcan);
				}
				Core.selected.dispose(false, true);
			}
			if (Core.highlightedConnection_ != null) {
				((RenderedConnection)Core.highlightedConnection_).unhighlight();
				Core.highlightedConnection_ = null;
			}
			Css.setCursor(Css.Cursor.OPEN);
			if (!WidgetDiv.isVisible()) {
				Events.setGroup(false);
			}
		}

		/// <summary>
		/// Load the block's help page in a new window.
		/// </summary>
		private void showHelp_()
		{
			var url = this.helpUrl.Is<Func<string>>() ? this.helpUrl.As<Func<string>>()() : this.helpUrl.As<string>();
			if (!String.IsNullOrEmpty(url)) {
				Window.Open(url);
			}
		}

		public virtual void customContextMenu(JsArray<ContextMenuOption> options)
		{
		}

		/// <summary>
		/// Show the context menu for this block.
		/// </summary>
		internal void showContextMenu_(MouseEvent e)
		{
			if (this.workspace.options.readOnly || !this.contextMenu) {
				return;
			}
			// Save the current block in a variable for use in closures.
			var block = this;
			var menuOptions = new JsArray<ContextMenuOption>();

			if (this.isDeletable() && this.isMovable() && !block.isInFlyout) {
				// Option to duplicate this block.
				var duplicateOption = new ContextMenuOption {
					text = Msg.DUPLICATE_BLOCK,
					enabled = true,
					callback = (ev) => {
						Core.duplicate_(block);
					}
				};
				if (this.getDescendants().Length > this.workspace.remainingCapacity()) {
					duplicateOption.enabled = false;
				}
				menuOptions.Push(duplicateOption);

				if (this.isEditable() && !this.collapsed_ &&
					this.workspace.options.comments) {
					// Option to add/remove a comment.
					var commentOption = new ContextMenuOption { enabled = !goog.userAgent.IE };
					if (this.comment != null) {
						commentOption.text = Msg.REMOVE_COMMENT;
						commentOption.callback = (ev) => {
							block.setCommentText(null);
						};
					}
					else {
						commentOption.text = Msg.ADD_COMMENT;
						commentOption.callback = (ev) => {
							block.setCommentText("");
						};
					}
					menuOptions.Push(commentOption);
				}

				// Option to make block inline.
				if (!this.collapsed_) {
					for (var i = 1; i < this.inputList.Length; i++) {
						if (this.inputList[i - 1].type != Core.NEXT_STATEMENT &&
							this.inputList[i].type != Core.NEXT_STATEMENT) {
							// Only display this option if there are two value or dummy inputs
							// next to each other.
							var inlineOption = new ContextMenuOption { enabled = true };
							var isInline = this.getInputsInline();
							inlineOption.text = isInline ?
								Msg.EXTERNAL_INPUTS : Msg.INLINE_INPUTS;
							inlineOption.callback = (ev) => {
								block.setInputsInline(!isInline);
							};
							menuOptions.Push(inlineOption);
							break;
						}
					}
				}

				if (this.workspace.options.collapse) {
					// Option to collapse/expand block.
					if (this.collapsed_) {
						var expandOption = new ContextMenuOption { enabled = true };
						expandOption.text = Msg.EXPAND_BLOCK;
						expandOption.callback = (ev) => {
							block.setCollapsed(false);
						};
						menuOptions.Push(expandOption);
					}
					else {
						var collapseOption = new ContextMenuOption { enabled = true };
						collapseOption.text = Msg.COLLAPSE_BLOCK;
						collapseOption.callback = (ev) => {
							block.setCollapsed(true);
						};
						menuOptions.Push(collapseOption);
					}
				}

				if (this.workspace.options.disable) {
					// Option to disable/enable block.
					var disableOption = new ContextMenuOption {
						text = this.disabled ?
						  Msg.ENABLE_BLOCK : Msg.DISABLE_BLOCK,
						enabled = !this.getInheritedDisabled(),
						callback = (ev) => {
							block.setDisabled(!block.disabled);
						}
					};
					menuOptions.Push(disableOption);
				}

				// Option to delete this block.
				// Count the number of blocks that are nested in this block.
				var descendantCount = this.getDescendants().Length;
				var nextBlock = this.getNextBlock();
				if (nextBlock != null) {
					// Blocks in the current stack would survive this block's deletion.
					descendantCount -= nextBlock.getDescendants().Length;
				}
				var deleteOption = new ContextMenuOption {
					text = descendantCount == 1 ? Msg.DELETE_BLOCK :
					  Msg.DELETE_X_BLOCKS.Replace("%1", descendantCount.ToString()),
					enabled = true,
					callback = (ev) => {
						Events.setGroup(true);
						block.dispose(true, true);
						Events.setGroup(false);
					}
				};
				menuOptions.Push(deleteOption);
			}

			// Option to get help.
			var url = this.helpUrl.Is<Func<string>>() ? this.helpUrl.As<Func<string>>()() : this.helpUrl.As<string>();
			var helpOption = new ContextMenuOption { enabled = !String.IsNullOrEmpty(url) };
			helpOption.text = Msg.HELP;
			helpOption.callback = (ev) => {
				block.showHelp_();
			};
			menuOptions.Push(helpOption);

			// Allow the block to add or modify menuOptions.
			if (/*this.customContextMenu != null &&*/ !block.isInFlyout) {
				this.customContextMenu(menuOptions);
			}

			ContextMenu.show(e, menuOptions, this.RTL);
			ContextMenu.currentBlock = this;
		}

		/// <summary>
		/// Move the connections for this block and all blocks attached under it.
		/// Also update any attached bubbles.
		/// </summary>
		/// <param name="dx">Horizontal offset from current location.</param>
		/// <param name="dy">Vertical offset from current location.</param>
		internal void moveConnections_(double dx, double dy)
		{
			if (!this.rendered) {
				// Rendering is required to lay out the blocks.
				// This is probably an invisible block attached to a collapsed block.
				return;
			}
			var myConnections = this.getConnections_(false);
			foreach (var i in myConnections) {
				i.moveBy(dx, dy);
			}
			var icons = this.getIcons();
			foreach (var i in icons) {
				i.computeIconLocation();
			}

			// Recurse through all blocks attached under this one.
			foreach (BlockSvg i in childBlocks_) {
				i.moveConnections_(dx, dy);
			}
		}

		/// <summary>
		/// Recursively adds or removes the dragging class to this node and its children.
		/// </summary>
		/// <param name="adding">True if adding, false if removing.</param>
		internal void setDragging_(bool adding)
		{
			if (adding) {
				var group = this.getSvgRoot();
				group["translate_"] = "";
				group["skew_"] = "";
				this.addDragging();
				Array.ForEach((Connection[])this.getConnections_(true), (i) => Core.draggingConnections_.Push(i));
			}
			else {
				this.removeDragging();
				Core.draggingConnections_ = new JsArray<Connection>();
			}
			// Recurse through all blocks attached under this one.
			for (var i = 0; i < this.childBlocks_.Length; i++) {
				((BlockSvg)this.childBlocks_[i]).setDragging_(adding);
			}
		}

		/// <summary>
		/// Drag this block to follow the mouse.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		private void onMouseMove_(MouseEvent e)
		{
			if (e.Type == "mousemove" && e.ClientX <= 1 && e.ClientY == 0 &&
				e.Button == 0) {
				/* HACK:
				 Safari Mobile 6.0 and Chrome for Android 18.0 fire rogue mousemove
				 events on certain touch actions. Ignore events with these signatures.
				 This may result in a one-pixel blind spot in other browsers,
				 but this shouldn't be noticeable. */
				e.StopPropagation();
				return;
			}

			var oldXY = this.getRelativeToSurfaceXY();
			var newXY = ((WorkspaceSvg)this.workspace).moveDrag(e);

			if (Core.dragMode_ == Core.DRAG_STICKY) {
				// Still dragging within the sticky DRAG_RADIUS.
				var dr = goog.math.Coordinate.distance(oldXY, newXY) * ((WorkspaceSvg)this.workspace).scale;
				if (dr > Core.DRAG_RADIUS) {
					// Switch to unrestricted dragging.
					Core.dragMode_ = Core.DRAG_FREE;
					Core.longStop_(e);
					((WorkspaceSvg)this.workspace).setResizesEnabled(false);
					if (this.parentBlock_ != null) {
						// Push this block to the very top of the stack.
						this.unplug();
						var group = this.getSvgRoot();
						group["translate_"] = "translate(" + newXY.x + "," + newXY.y + ")";
						this.disconnectUiEffect();
					}
					this.setDragging_(true);
				}
			}
			if (Core.dragMode_ == Core.DRAG_FREE) {
				// Unrestricted dragging.
				var dxy = goog.math.Coordinate.difference(oldXY, this.dragStartXY_);
				var group = this.getSvgRoot();
				group["translate_"] = "translate(" + newXY.x + "," + newXY.y + ")";
				group.SetAttribute("transform", (string)group["translate_"] + (string)group["skew_"]);
				// Drag all the nested bubbles.
				for (var i = 0; i < this.draggedBubbles_.Length; i++) {
					var commentData = this.draggedBubbles_[i];
					((Icon)commentData["bubble"]).setIconLocation(
						goog.math.Coordinate.sum(commentData, dxy));
				}

				// Check to see if any of this block's connections are within range of
				// another block's connection.
				var myConnections = this.getConnections_(false);
				// Also check the last connection on this stack
				var lastOnStack = this.lastConnectionInStack_();
				if (lastOnStack != null && lastOnStack != this.nextConnection) {
					myConnections.Push((RenderedConnection)lastOnStack);
				}
				Connection closestConnection = null;
				Connection localConnection = null;
				var radiusConnection = (double)Core.SNAP_RADIUS;
				for (var i = 0; i < myConnections.Length; i++) {
					var myConnection = myConnections[i];
					var neighbour = myConnection.closest(radiusConnection, dxy.x, dxy.y);
					if (neighbour.connection != null) {
						closestConnection = neighbour.connection;
						localConnection = myConnection;
						radiusConnection = neighbour.radius;
					}
				}

				// Remove connection highlighting if needed.
				if (Core.highlightedConnection_ != null &&
					Core.highlightedConnection_ != closestConnection) {
					((RenderedConnection)Core.highlightedConnection_).unhighlight();
					Core.highlightedConnection_ = null;
					Core.localConnection_ = null;
				}
				// Add connection highlighting if needed.
				if (closestConnection != null &&
					closestConnection != Core.highlightedConnection_) {
					((RenderedConnection)closestConnection).highlight();
					Core.highlightedConnection_ = closestConnection;
					Core.localConnection_ = localConnection;
				}
				// Provide visual indication of whether the block will be deleted if
				// dropped here.
				if (this.isDeletable()) {
					((WorkspaceSvg)this.workspace).isDeleteArea(e);
				}
			}
			// This event has been handled.  No need to bubble up to the document.
			e.StopPropagation();
			e.PreventDefault();
		}

		/// <summary>
		/// Add or remove the UI indicating if this block is movable or not.
		/// </summary>
		public void updateMovable()
		{
			if (this.isMovable()) {
				Core.addClass_(this.svgGroup_, "blocklyDraggable");
			}
			else {
				Core.removeClass_(this.svgGroup_, "blocklyDraggable");
			}
		}

		/// <summary>
		/// Set whether this block is movable or not.
		/// </summary>
		/// <param name="movable">True if movable.</param>
		public override void setMovable(bool movable)
		{
			base.setMovable(movable);
			this.updateMovable();
		}

		/// <summary>
		/// Set whether this block is editable or not.
		/// </summary>
		/// <param name="editable">True if editable.</param>
		public override void setEditable(bool editable)
		{
			base.setEditable(editable);
			var icons = this.getIcons();
			for (var i = 0; i < icons.Length; i++) {
				icons[i].updateEditable();
			}
		}

		/// <summary>
		/// Set whether this block is a shadow block or not.
		/// </summary>
		/// <param name="shadow">True if a shadow.</param>
		public override void setShadow(bool shadow)
		{
			base.setShadow(shadow);
			this.updateColour();
		}

		/// <summary>
		/// Return the root node of the SVG or null if none exists.
		/// </summary>
		/// <returns>The root SVG node (probably a group).</returns>
		public SVGElement getSvgRoot()
		{
			return this.svgGroup_;
		}

		/// <summary>
		/// Dispose of this block.
		/// </summary>
		/// <param name="healStack">If true, then try to heal any gap by connecting
		/// the next statement with the previous statement.  Otherwise, dispose of
		/// all children of this block.</param>
		/// <param name="animate">If true, show a disposal animation and sound.</param>
		public override void dispose(bool healStack, bool animate = false)
		{
			if (this.workspace == null) {
				// The block has already been deleted.
				return;
			}
			Tooltip.hide();
			Field.startCache();
			// Save the block's workspace temporarily so we can resize the
			// contents once the block is disposed.
			var blockWorkspace = this.workspace;
			// If this block is being dragged, unlink the mouse events.
			if (Core.selected == this) {
				this.unselect();
				Core.terminateDrag_();
			}
			// If this block has a context menu open, close it.
			if (ContextMenu.currentBlock == this) {
				ContextMenu.hide();
			}

			if (animate && this.rendered) {
				this.unplug(healStack);
				this.disposeUiEffect();
			}
			// Stop rerendering.
			this.rendered = false;

			Events.disable();
			try {
				var icons = this.getIcons();
				for (var i = 0; i < icons.Length; i++) {
					icons[i].dispose();
				}
			}
			finally {
				Events.enable();
			}
			base.dispose(healStack);

			goog.dom.removeNode(this.svgGroup_);
			((WorkspaceSvg)blockWorkspace).resizeContents();
			// Sever JavaScript to DOM connections.
			this.svgGroup_ = null;
			this.svgPath_ = null;
			this.svgPathLight_ = null;
			this.svgPathDark_ = null;
			Field.stopCache();
		}

		/// <summary>
		/// Play some UI effects (sound, animation) when disposing of a block.
		/// </summary>
		public void disposeUiEffect()
		{
			((WorkspaceSvg)this.workspace).playAudio("delete");

			var xy = Core.getSvgXY_(this.svgGroup_, (WorkspaceSvg)this.workspace);
			// Deeply clone the current block.
			var clone = (SVGElement)this.svgGroup_.CloneNode(true);
			clone["translateX_"] = xy.x;
			clone["translateY_"] = xy.y;
			clone.SetAttribute("transform",
				"translate(" + clone["translateX_"] + ", " + clone["translateY_"] + ")");
			((WorkspaceSvg)this.workspace).getParentSvg().AppendChild(clone);
			clone["bBox_"] = clone.getBBox();
			// Start the animation.
			BlockSvg.disposeUiStep_(clone, this.RTL, new Date(),
				((WorkspaceSvg)this.workspace).scale);
		}

		/// <summary>
		/// Animate a cloned block and eventually dispose of it.
		/// This is a class method, not an instace method since the original block has
		/// been destroyed and is no longer accessible.
		/// </summary>
		/// <param name="clone">SVG element to animate and dispose of.</param>
		/// <param name="rtl">True if RTL, false if LTR.</param>
		/// <param name="start">Date of animation's start.</param>
		/// <param name="workspaceScale">Scale of workspace.</param>
		private static void disposeUiStep_(SVGElement clone, bool rtl, Date start, double workspaceScale)
		{
			var ms = new Date() - start;
			var percent = ms / 150;
			if (percent > 1) {
				goog.dom.removeNode(clone);
			}
			else {
				var x = (double)clone["translateX_"] +
					(rtl ? -1 : 1) * ((SVGRect)clone["bBox_"]).width * workspaceScale / 2 * percent;
				var y = (double)clone["translateY_"] + ((SVGRect)clone["bBox_"]).height * workspaceScale * percent;
				var scale = (1 - percent) * workspaceScale;
				clone.SetAttribute("transform", "translate(" + x + "," + y + ")" +
					" scale(" + scale + ")");
				var closure = new Action(() => {
					BlockSvg.disposeUiStep_(clone, rtl, start, workspaceScale);
				});
				Window.SetTimeout(closure, 10);
			}
		}

		/// <summary>
		/// Play some UI effects (sound, ripple) after a connection has been established.
		/// </summary>
		public void connectionUiEffect()
		{
			((WorkspaceSvg)this.workspace).playAudio("click");
			if (((WorkspaceSvg)this.workspace).scale < 1) {
				return;  // Too small to care about visual effects.
			}
			// Determine the absolute coordinates of the inferior block.
			var xy = Core.getSvgXY_(this.svgGroup_, ((WorkspaceSvg)this.workspace));
			// Offset the coordinates based on the two connection types, fix scale.
			if (this.outputConnection != null) {
				xy.x += (this.RTL ? 3 : -3) * ((WorkspaceSvg)this.workspace).scale;
				xy.y += 13 * ((WorkspaceSvg)this.workspace).scale;
			}
			else if (this.previousConnection != null) {
				xy.x += (this.RTL ? -23 : 23) * ((WorkspaceSvg)this.workspace).scale;
				xy.y += 3 * ((WorkspaceSvg)this.workspace).scale;
			}
			var ripple = Core.createSvgElement("circle", new Dictionary<string, object>() {
				{ "cx", xy.x }, { "cy", xy.y }, { "r", 0 }, { "fill", "none" },
				{ "stroke", "#888" }, { "stroke-width", 10 } },
				((WorkspaceSvg)this.workspace).getParentSvg());
			// Start the animation.
			BlockSvg.connectionUiStep_(ripple, new Date(), ((WorkspaceSvg)this.workspace).scale);
		}

		/// <summary>
		/// Expand a ripple around a connection.
		/// </summary>
		/// <param name="ripple">Element to animate.</param>
		/// <param name="start">Date of animation's start.</param>
		/// <param name="workspaceScale">Scale of workspace.</param>
		private static void connectionUiStep_(SVGElement ripple, Date start, double workspaceScale)
		{
			var ms = new Date() - start;
			var percent = ms / 150;
			if (percent > 1) {
				goog.dom.removeNode(ripple);
			}
			else {
				ripple.SetAttribute("r", (percent * 25 * workspaceScale).ToString());
				ripple.style.Opacity = (1 - percent).ToString();
				var closure = new Action(() => {
					BlockSvg.connectionUiStep_(ripple, start, workspaceScale);
				});
				BlockSvg.disconnectUiStop_pid_ = Window.SetTimeout(closure, 10);
			}
		}

		/// <summary>
		/// Play some UI effects (sound, animation) when disconnecting a block.
		/// </summary>
		public void disconnectUiEffect()
		{
			((WorkspaceSvg)this.workspace).playAudio("disconnect");
			if (((WorkspaceSvg)this.workspace).scale < 1) {
				return;  // Too small to care about visual effects.
			}
			// Horizontal distance for bottom of block to wiggle.
			var DISPLACEMENT = 10;
			// Scale magnitude of skew to height of block.
			var height = this.getHeightWidth().height;
			var magnitude = System.Math.Atan(DISPLACEMENT / height) / System.Math.PI * 180;
			if (!this.RTL) {
				magnitude *= -1;
			}
			// Start the animation.
			BlockSvg.disconnectUiStep_(this.svgGroup_, magnitude, new Date());
		}

		/// <summary>
		/// Animate a brief wiggle of a disconnected block.
		/// </summary>
		/// <param name="group">SVG element to animate.</param>
		/// <param name="magnitude">Maximum degrees skew (reversed for RTL).</param>
		/// <param name="start">Date of animation's start.</param>
		private static void disconnectUiStep_(SVGElement group, double magnitude, Date start)
		{
			var DURATION = 200;  // Milliseconds.
			var WIGGLES = 3;  // Half oscillations.

			var ms = new Date() - start;
			var percent = ms / DURATION;

			if (percent > 1) {
				group["skew_"] = "";
			}
			else {
				var skew = System.Math.Round(System.Math.Sin(percent * System.Math.PI * WIGGLES) *
					(1 - percent) * magnitude);
				group["skew_"] = "skewX(" + skew + ")";
				var closure = new Action(() => {
					BlockSvg.disconnectUiStep_(group, magnitude, start);
				});
				BlockSvg.disconnectUiStop_group = group;
				BlockSvg.disconnectUiStop_pid_ = Window.SetTimeout(closure, 10);
			}
			group.SetAttribute("transform", (string)group["translate_"] + (string)group["skew_"]);
		}

		/// <summary>
		/// Stop the disconnect UI animation immediately.
		/// </summary>
		private static void disconnectUiStop_()
		{
			if (BlockSvg.disconnectUiStop_group != null) {
				Window.ClearTimeout(BlockSvg.disconnectUiStop_pid_);
				var group = BlockSvg.disconnectUiStop_group;
				group["skew_"] = "";
				group.SetAttribute("transform", group["translate_"].ToString());
				BlockSvg.disconnectUiStop_group = null;
			}
		}

		/// <summary>
		/// PID of disconnect UI animation.  There can only be one at a time.
		/// </summary>
		private static int disconnectUiStop_pid_ = 0;

		/// <summary>
		/// SVG group of wobbling block.  There can only be one at a time.
		/// </summary>
		private static Element disconnectUiStop_group = null;

		/// <summary>
		/// Change the colour of a block.
		/// </summary>
		public void updateColour()
		{
			if (this.disabled) {
				// Disabled blocks don't have colour.
				return;
			}
			var hexColour = this.getColour();
			var rgb = goog.color.hexToRgb(hexColour);
			if (this.isShadow()) {
				rgb = goog.color.lighten(rgb, 0.6);
				hexColour = goog.color.rgbArrayToHex(rgb);
				this.svgPathLight_.style.Display = Display.None;
				this.svgPathDark_.SetAttribute("fill", hexColour);
			}
			else {
				this.svgPathLight_.style.Display = Display.Blank;
				var hexLight = goog.color.rgbArrayToHex(goog.color.lighten(rgb, 0.3));
				var hexDark = goog.color.rgbArrayToHex(goog.color.darken(rgb, 0.2));
				this.svgPathLight_.SetAttribute("stroke", hexLight);
				this.svgPathDark_.SetAttribute("fill", hexDark);
			}
			this.svgPath_.SetAttribute("fill", hexColour);

			var icons = this.getIcons();
			for (var i = 0; i < icons.Length; i++) {
				icons[i].updateColour();
			}

			// Bump every dropdown to change its colour.
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					field.setText(null);
				}
			}
		}

		/// <summary>
		/// Enable or disable a block.
		/// </summary>
		public void updateDisabled()
		{
			var hasClass = Core.hasClass_(this.svgGroup_, "blocklyDisabled");
			if (this.disabled || this.getInheritedDisabled()) {
				if (!hasClass) {
					Core.addClass_(this.svgGroup_, "blocklyDisabled");
					this.svgPath_.SetAttribute("fill",
						"url(#" + this.workspace.options.disabledPatternId + ")");
				}
			}
			else {
				if (hasClass) {
					Core.removeClass_(this.svgGroup_, "blocklyDisabled");
					this.updateColour();
				}
			}
			var children = this.getChildren();
			foreach (BlockSvg child in children) {
				child.updateDisabled();
			}
		}

		/// <summary>
		/// Returns the comment on this block (or '' if none).
		/// </summary>
		/// <returns>Block's comment.</returns>
		public override string getCommentText()
		{
			if (this.comment != null) {
				var comment = this.comment.getText();
				// Trim off trailing whitespace.
				return comment.Replace(new Regex(@"\s+$"), "").Replace(new Regex(@" +\n", RegexOptions.Multiline), "\n");
			}
			return "";
		}

		/// <summary>
		/// Set this block's comment text.
		/// </summary>
		/// <param name="text">The text, or null to delete.</param>
		public override void setCommentText(string text)
		{
			var changedState = false;
			if (text != null) {
				if (this.comment == null) {
					this.comment = new Comment(this);
					changedState = true;
				}
				this.comment.setText(text);
			}
			else {
				if (this.comment != null) {
					this.comment.dispose();
					changedState = true;
				}
			}
			if (changedState && this.rendered) {
				this.render();
				// Adding or removing a comment icon will cause the block to change shape.
				this.bumpNeighbours_();
			}
		}

		private Dictionary<string, int> setWarningText_pid_;

		/// <summary>
		/// Set this block's warning text.
		/// </summary>
		/// <param name="text">The text, or null to delete.</param>
		/// <param name="opt_id">An optional ID for the warning text to be able to
		/// maintain multiple warnings.</param>
		public override void setWarningText(string text, string opt_id = null)
		{
			if (this.setWarningText_pid_ == null) {
				// Create a database of warning PIDs.
				// Only runs once per block (and only those with warnings).
				this.setWarningText_pid_ = new Dictionary<string, int>();
			}
			var id = opt_id ?? "";
			if (!String.IsNullOrEmpty(id)) {
				// Kill all previous pending processes, this edit supercedes them all.
				foreach (var n in this.setWarningText_pid_.Keys) {
					Window.ClearTimeout(this.setWarningText_pid_[n]);
					this.setWarningText_pid_.Remove(n);
					//Script.Delete(this.setWarningText_pid_[n]);
				}
			}
			else if (this.setWarningText_pid_.ContainsKey(id)) {
				// Only queue up the latest change.  Kill any earlier pending process.
				Window.ClearTimeout(this.setWarningText_pid_[id]);
				this.setWarningText_pid_.Remove(id);
				//Script.Delete(this.setWarningText_pid_[id]);
			}
			if (Core.dragMode_ == Core.DRAG_FREE) {
				// Don't change the warning text during a drag.
				// Wait until the drag finishes.
				var thisBlock = this;
				this.setWarningText_pid_[id] = Window.SetTimeout(() => {
					if (thisBlock.workspace != null) {  // Check block wasn't deleted.
						thisBlock.setWarningText_pid_.Remove(id);
						//Script.Delete(thisBlock.setWarningText_pid_[id]);
						thisBlock.setWarningText(text, id);
					}
				}, 100);
				return;
			}
			if (this.isInFlyout) {
				text = null;
			}

			// Bubble up to add a warning on top-most collapsed block.
			var parent = this.getSurroundParent();
			Block collapsedParent = null;
			while (parent != null) {
				if (parent.isCollapsed()) {
					collapsedParent = parent;
				}
				parent = parent.getSurroundParent();
			}
			if (collapsedParent != null) {
				collapsedParent.setWarningText(text, "collapsed " + this.id + " " + id);
			}

			var changedState = false;
			if (text != null) {
				if (this.warning == null) {
					this.warning = new Warning(this);
					changedState = true;
				}
				this.warning.setText(text, id);
			}
			else {
				// Dispose all warnings if no id is given.
				if (this.warning != null && String.IsNullOrEmpty(id)) {
					this.warning.dispose();
					changedState = true;
				}
				else if (this.warning != null) {
					var oldText = this.warning.getText();
					this.warning.setText("", id);
					var newText = this.warning.getText();
					if (newText == null) {
						this.warning.dispose();
					}
					changedState = oldText == newText;
				}
			}
			if (changedState && this.rendered) {
				this.render();
				// Adding or removing a warning icon will cause the block to change shape.
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Give this block a mutator dialog.
		/// </summary>
		/// <param name="mutator">A mutator dialog instance or null to remove.</param>
		public override void setMutator(Mutator mutator)
		{
			if (this.mutator != null && this.mutator != mutator) {
				this.mutator.dispose();
			}
			if (mutator != null) {
				mutator.block_ = this;
				this.mutator = mutator;
				mutator.createIcon();
			}
		}

		/// <summary>
		/// Set whether the block is disabled or not.
		/// </summary>
		/// <param name="disabled">True if disabled.</param>
		public override void setDisabled(bool disabled)
		{
			if (this.disabled != disabled) {
				base.setDisabled(disabled);
				if (this.rendered) {
					this.updateDisabled();
				}
			}
		}

		/// <summary>
		/// Select this block.  Highlight it visually.
		/// </summary>
		public void addSelect(Event e = null)
		{
			Core.addClass_(this.svgGroup_, "blocklySelected");
			// Move the selected block to the top of the stack.
			var block = this;
			do {
				var root = block.getSvgRoot();
				root.ParentNode.AppendChild(root);
				block = (BlockSvg)block.getParent();
			} while (block != null);
		}

		/// <summary>
		/// Unselect this block.  Remove its highlighting.
		/// </summary>
		public void removeSelect(Event e = null)
		{
			Core.removeClass_(this.svgGroup_, "blocklySelected");
		}

		/// <summary>
		/// Adds the dragging class to this block.
		/// Also disables the highlights/shadows to improve performance.
		/// </summary>
		public void addDragging()
		{
			Core.addClass_(this.svgGroup_, "blocklyDragging");
		}

		/// <summary>
		/// Removes the dragging class from this block.
		/// </summary>
		public void removeDragging()
		{
			Core.removeClass_(this.svgGroup_, "blocklyDragging");
		}

		/// <summary>
		/// Change the colour of a block.
		/// </summary>
		/// <param name="colour">HSV hue value, or #RRGGBB string.</param>
		public override void setColour(Union<int, string> colour)
		{
			base.setColour(colour);

			if (this.rendered) {
				this.updateColour();
			}
		}

		/// <summary>
		/// Set whether this block can chain onto the bottom of another block.
		/// </summary>
		/// <param name="newBoolean">True if there can be a previous statement.</param>
		/// <param name="opt_check">Statement type or
		/// list of statement types.  Null/undefined if any type could be connected.</param>
		public override void setPreviousStatement(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			base.setPreviousStatement(newBoolean, opt_check);

			if (this.rendered) {
				this.render();
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Set whether another block can chain onto the bottom of this block.
		/// </summary>
		/// <param name="newBoolean">True if there can be a next statement.</param>
		/// <param name="opt_check">Statement type or
		/// list of statement types.  Null/undefined if any type could be connected.</param>
		public override void setNextStatement(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			base.setNextStatement(newBoolean, opt_check);

			if (this.rendered) {
				this.render();
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Set whether this block returns a value.
		/// </summary>
		/// <param name="newBoolean">True if there is an output.</param>
		/// <param name="opt_check">Returned type or list
		/// of returned types.  Null or undefined if any type could be returned
		/// (e.g. variable get).</param>
		public override void setOutput(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			base.setOutput(newBoolean, opt_check);

			if (this.rendered) {
				this.render();
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Set whether value inputs are arranged horizontally or vertically.
		/// </summary>
		/// <param name="newBoolean">True if inputs are horizontal.</param>
		public override void setInputsInline(bool newBoolean)
		{
			base.setInputsInline(newBoolean);

			if (this.rendered) {
				this.render();
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Remove an input from this block.
		/// </summary>
		/// <param name="name">The name of the input.</param>
		/// <param name="opt_quiet">True to prevent error if input is not present.</param>
		public override void removeInput(string name, bool opt_quiet = false)
		{
			base.removeInput(name, opt_quiet);

			if (this.rendered) {
				this.render();
				// Removing an input will cause the block to change shape.
				this.bumpNeighbours_();
			}
		}

		/// <summary>
		/// Move a numbered input to a different location on this block.
		/// </summary>
		/// <param name="inputIndex">Index of the input to move.</param>
		/// <param name="refIndex">Index of input that should be after the moved input.</param>
		public override void moveNumberedInputBefore(int inputIndex, int refIndex)
		{
			base.moveNumberedInputBefore(inputIndex, refIndex);

			if (this.rendered) {
				this.render();
				// Moving an input will cause the block to change shape.
				this.bumpNeighbours_();
			}
		}

		protected override Input appendInput_(int type, string name)
		{
			var input = base.appendInput_(type, name);

			if (this.rendered) {
				this.render();
				// Adding an input will cause the block to change shape.
				this.bumpNeighbours_();
			}
			return input;
		}

		/// <summary>
		/// Returns connections originating from this block.
		/// </summary>
		/// <param name="all">If true, return all connections even hidden ones.
		/// Otherwise, for a non-rendered block return an empty list, and for a
		/// collapsed block don't return inputs connections.</param>
		/// <returns>Array of connections.</returns>
		private Connection[] getConnections_(Connection[] all)
		{
			var myConnections = new JsArray<Connection>();
			if (all != null || this.rendered) {
				if (this.outputConnection != null) {
					myConnections.Push(this.outputConnection);
				}
				if (this.previousConnection != null) {
					myConnections.Push(this.previousConnection);
				}
				if (this.nextConnection != null) {
					myConnections.Push(this.nextConnection);
				}
				if (all != null || !this.collapsed_) {
					foreach (var input in this.inputList) {
						if (input.connection != null) {
							myConnections.Push(input.connection);
						}
					}
				}
			}
			return myConnections;
		}

		/// <summary>
		/// Create a connection of the specified type.
		/// </summary>
		/// <param name="type">The type of the connection to create.</param>
		/// <returns>A new connection of the specified type.</returns>
		public override RenderedConnection makeConnection_(int type)
		{
			return new RenderedConnection(this, type);
		}
	}
}
