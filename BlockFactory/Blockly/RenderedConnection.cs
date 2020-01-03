/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2016 Google Inc.
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
 * @fileoverview Components for creating connections between blocks.
 * @author fenichel@google.com (Rachel Fenichel)
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace Blockly
{
	public class RenderedConnection : Connection
	{
		private goog.math.Coordinate offsetInBlock_;

		/// <summary>
		/// Class for a connection between blocks that may be rendered on screen.
		/// </summary>
		/// <param name="source">The block establishing this connection.</param>
		/// <param name="type">The type of the connection.</param>
		public RenderedConnection(Block source, int type)
			: base(source, type)
		{
			this.offsetInBlock_ = new goog.math.Coordinate(0, 0);
		}

		/// <summary>
		/// Returns the distance between this connection and another connection.
		/// </summary>
		/// <param name="otherConnection">The other connection to measure
		/// the distance to.</param>
		/// <returns>The distance between connections.</returns>
		public double distanceFrom(Connection otherConnection)
		{
			var xDiff = this.x_ - otherConnection.x_;
			var yDiff = this.y_ - otherConnection.y_;
			return System.Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
		}

		/// <summary>
		/// Move the block(s) belonging to the connection to a point where they don't
		/// visually interfere with the specified connection.
		/// </summary>
		/// <param name="staticConnection">The connection to move away
		/// from.</param>
		internal void bumpAwayFrom_(Connection staticConnection)
		{
			if (Core.dragMode_ != Core.DRAG_NONE) {
				// Don't move blocks around while the user is doing the same.
				return;
			}
			// Move the root block.
			var rootBlock = (BlockSvg)this.sourceBlock_.getRootBlock();
			if (rootBlock.isInFlyout) {
				// Don't move blocks around in a flyout.
				return;
			}
			var reverse = false;
			if (!rootBlock.isMovable()) {
				// Can't bump an uneditable block away.
				// Check to see if the other block is movable.
				rootBlock = (BlockSvg)staticConnection.getSourceBlock().getRootBlock();
				if (!rootBlock.isMovable()) {
					return;
				}
				// Swap the connections and move the 'static' connection instead.
				staticConnection = this;
				reverse = true;
			}
			// Raise it to the top for extra visibility.
			var selected = Core.selected == rootBlock;
			if (!selected) rootBlock.addSelect();
			var dx = (staticConnection.x_ + Core.SNAP_RADIUS) - this.x_;
			var dy = (staticConnection.y_ + Core.SNAP_RADIUS) - this.y_;
			if (reverse) {
				// When reversing a bump due to an uneditable block, bump up.
				dy = -dy;
			}
			if (rootBlock.RTL) {
				dx = -dx;
			}
			rootBlock.moveBy(dx, dy);
			if (!selected) rootBlock.removeSelect();
		}

		/// <summary>
		/// Change the connection's coordinates.
		/// </summary>
		/// <param name="x">New absolute x coordinate.</param>
		/// <param name="y">New absolute y coordinate.</param>
		public void moveTo(double x, double y)
		{
			// Remove it from its old location in the database (if already present)
			if (this.inDB_) {
				this.db_.removeConnection_(this);
			}
			this.x_ = x;
			this.y_ = y;
			// Insert it into its new location in the database.
			if (!this.hidden_) {
				this.db_.addConnection(this);
			}
		}

		/// <summary>
		/// Change the connection's coordinates.
		/// </summary>
		/// <param name="dx">Change to x coordinate.</param>
		/// <param name="dy">Change to y coordinate.</param>
		public void moveBy(double dx, double dy)
		{
			this.moveTo(this.x_ + dx, this.y_ + dy);
		}

		/// <summary>
		/// Move this connection to the location given by its offset within the block and
		/// the coordinate of the block's top left corner.
		/// </summary>
		/// <param name="blockTL">The coordinate of the top left corner
		/// of the block.</param>
		public void moveToOffset(goog.math.Coordinate blockTL)
		{
			this.moveTo(blockTL.x + this.offsetInBlock_.x,
				blockTL.y + this.offsetInBlock_.y);
		}

		/// <summary>
		/// Set the offset of this connection relative to the top left of its block.
		/// </summary>
		/// <param name="x">The new relative x.</param>
		/// <param name="y">The new relative y.</param>
		public void setOffsetInBlock(double x, double y)
		{
			this.offsetInBlock_.x = x;
			this.offsetInBlock_.y = y;
		}

		/// <summary>
		/// Move the blocks on either side of this connection right next to each other.
		/// </summary>
		internal void tighten_()
		{
			var dx = this.targetConnection.x_ - this.x_;
			var dy = this.targetConnection.y_ - this.y_;
			if (dx != 0 || dy != 0) {
				var block = (BlockSvg)this.targetBlock();
				var svgRoot = block.getSvgRoot();
				if (svgRoot == null) {
					throw new Exception("block is not rendered.");
				}
				var xy = Core.getRelativeXY_(svgRoot);
				block.getSvgRoot().SetAttribute("transform",
					"translate(" + (xy.x - dx) + "," + (xy.y - dy) + ")");
				block.moveConnections_(-dx, -dy);
			}
		}

		public class Closest
		{
			public Connection connection;
			public double radius;
		}

		/// <summary>
		/// Find the closest compatible connection to this connection.
		/// </summary>
		/// <param name="maxLimit">The maximum radius to another connection.</param>
		/// <param name="dx">Horizontal offset between this connection's location
		/// in the database and the current location (as a result of dragging).</param>
		/// <param name="dy">Vertical offset between this connection's location
		/// in the database and the current location (as a result of dragging).</param>
		/// <returns>Contains two
		/// properties: 'connection' which is either another connection or null,
		/// and 'radius' which is the distance.</returns>
		public Closest closest(double maxLimit, double dx, double dy)
		{
			return this.dbOpposite_.searchForClosest(this, maxLimit, new goog.math.Coordinate(dx, dy));
		}

		/// <summary>
		/// Add highlighting around this connection.
		/// </summary>
		public void highlight()
		{
			string steps;
			if (this.type == Core.INPUT_VALUE || this.type == Core.OUTPUT_VALUE) {
				steps = "m 0,0 " + BlockSvg.TAB_PATH_DOWN + " v 5";
			}
			else {
				steps = "m -20,0 h 5 " + BlockSvg.NOTCH_PATH_LEFT + " h 5";
			}
			var xy = this.sourceBlock_.getRelativeToSurfaceXY();
			var x = this.x_ - xy.x;
			var y = this.y_ - xy.y;
			Connection.highlightedPath_ = Core.createSvgElement("path", new Dictionary<string, object>() {
					{ "class", "blocklyHighlightedConnectionPath" },
					{ "d", steps },
					{ "transform", "translate(" + x + "," + y + ")" +
						(this.sourceBlock_.RTL ? " scale(-1 1)" : "") }
				},
				((BlockSvg)this.sourceBlock_).getSvgRoot());
		}

		/// <summary>
		/// Unhide this connection, as well as all down-stream connections on any block
		/// attached to this connection.  This happens when a block is expanded.
		/// Also unhides down-stream comments.
		/// </summary>
		/// <returns>List of blocks to render.</returns>
		public Block[] unhideAll()
		{
			this.setHidden(false);
			// All blocks that need unhiding must be unhidden before any rendering takes
			// place, since rendering requires knowing the dimensions of lower blocks.
			// Also, since rendering a block renders all its parents, we only need to
			// render the leaf nodes.
			var renderList = new JsArray<Block>();
			if (this.type != Core.INPUT_VALUE && this.type != Core.NEXT_STATEMENT) {
				// Only spider down.
				return renderList;
			}
			var block = this.targetBlock();
			if (block != null) {
				JsArray<RenderedConnection> connections;
				if (block.isCollapsed()) {
					// This block should only be partially revealed since it is collapsed.
					connections = new JsArray<RenderedConnection>();
					if (block.outputConnection != null) connections.Push(block.outputConnection);
					if (block.nextConnection != null) connections.Push(block.nextConnection);
					if (block.previousConnection != null) connections.Push(block.previousConnection);
				}
				else {
					// Show all connections of this block.
					connections = block.getConnections_(true);
				}
				for (var i = 0; i < connections.Length; i++) {
					renderList.PushRange(connections[i].unhideAll());
				}
				if (renderList.Length == 0) {
					// Leaf block.
					renderList[0] = block;
				}
			}
			return renderList;
		}

		/// <summary>
		/// Remove the highlighting around this connection.
		/// </summary>
		public void unhighlight()
		{
			goog.dom.removeNode(Connection.highlightedPath_);
			Script.Delete(ref Connection.highlightedPath_);
		}

		/// <summary>
		/// Set whether this connections is hidden (not tracked in a database) or not.
		/// </summary>
		/// <param name="hidden">True if connection is hidden.</param>
		public void setHidden(bool hidden)
		{
			this.hidden_ = hidden;
			if (hidden && this.inDB_) {
				this.db_.removeConnection_(this);
			}
			else if (!hidden && !this.inDB_) {
				this.db_.addConnection(this);
			}
		}

		/// <summary>
		/// Hide this connection, as well as all down-stream connections on any block
		/// attached to this connection.  This happens when a block is collapsed.
		/// Also hides down-stream comments.
		/// </summary>
		public void hideAll()
		{
			this.setHidden(true);
			if (this.targetConnection != null) {
				var blocks = this.targetBlock().getDescendants();
				for (var i = 0; i < blocks.Length; i++) {
					var block = (BlockSvg)blocks[i];
					// Hide all connections of all children.
					var connections = block.getConnections_(true);
					for (var j = 0; j < connections.Length; j++) {
						connections[j].setHidden(true);
					}
					// Close all bubbles of all children.
					var icons = block.getIcons();
					for (var j = 0; j < icons.Length; j++) {
						icons[j].setVisible(false);
					}
				}
			}
		}

		/// <summary>
		/// Check if the two connections can be dragged to connect to each other.
		/// </summary>
		/// <param name="candidate">A nearby connection to check.</param>
		/// <param name="maxRadius">The maximum radius allowed for connections.</param>
		/// <returns>True if the connection is allowed, false otherwise.</returns>
		public bool isConnectionAllowed(Connection candidate,
			double maxRadius)
		{
			if (this.distanceFrom(candidate) > maxRadius) {
				return false;
			}

			return base.isConnectionAllowed(candidate);
		}

		/// <summary>
		/// Disconnect two blocks that are connected by this connection.
		/// </summary>
		protected override void disconnectInternal_(Block parentBlock_, Block childBlock_)
		{
			BlockSvg parentBlock = (BlockSvg)parentBlock_, childBlock = (BlockSvg)childBlock_;
			base.disconnectInternal_(parentBlock, childBlock);
			// Rerender the parent so that it may reflow.
			if (parentBlock.rendered) {
				parentBlock.render();
			}
			if (childBlock.rendered) {
				childBlock.updateDisabled();
				childBlock.render();
			}
		}

		/// <summary>
		/// Respawn the shadow block if there was one connected to the this connection.
		/// Render/rerender blocks as needed.
		/// </summary>
		protected override void respawnShadow_()
		{
			var parentBlock = (BlockSvg)this.getSourceBlock();
			// Respawn the shadow block if there is one.
			var shadow = this.getShadowDom();
			if (parentBlock.workspace != null && shadow != null && Events.recordUndo) {
				base.respawnShadow_();
				var blockShadow = (BlockSvg)this.targetBlock();
				if (blockShadow == null) {
					throw new Exception("Couldn\'t respawn the shadow block that should exist here.");
				}
				blockShadow.initSvg();
				blockShadow.render(false);
				if (parentBlock.rendered) {
					parentBlock.render();
				}
			}
		}

		/// <summary>
		/// Find all nearby compatible connections to this connection.
		/// Type checking does not apply, since this function is used for bumping.
		/// </summary>
		/// <param name="maxLimit">The maximum radius to another connection.</param>
		/// <returns>List of connections.</returns>
		internal Connection[] neighbours_(double maxLimit)
		{
			return this.dbOpposite_.getNeighbours(this, maxLimit);
		}

		/// <summary>
		/// Connect two connections together.  This is the connection on the superior
		/// block.  Rerender blocks as needed.
		/// </summary>
		/// <param name="childConnection">Connection on inferior block.</param>
		protected override void connect_(Connection childConnection)
		{
			base.connect_(childConnection);

			var parentConnection = this;
			var parentBlock = (BlockSvg)parentConnection.getSourceBlock();
			var childBlock = (BlockSvg)childConnection.getSourceBlock();

			if (parentBlock.rendered) {
				parentBlock.updateDisabled();
			}
			if (childBlock.rendered) {
				childBlock.updateDisabled();
			}
			if (parentBlock.rendered && childBlock.rendered) {
				if (parentConnection.type == Core.NEXT_STATEMENT ||
					parentConnection.type == Core.PREVIOUS_STATEMENT) {
					// Child block may need to square off its corners if it is in a stack.
					// Rendering a child will render its parent.
					childBlock.render();
				}
				else {
					// Child block does not change shape.  Rendering the parent node will
					// move its connected children into position.
					parentBlock.render();
				}
			}
		}
	}
}
