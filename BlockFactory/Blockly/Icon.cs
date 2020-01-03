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
 * @fileoverview Object representing an icon on a block.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public abstract class Icon
	{
		internal BlockSvg block_;

		/// <summary>
		/// Class for an icon.
		/// </summary>
		/// <param name="block">The block associated with this icon.</param>
		public Icon(BlockSvg block)
		{
			block_ = block;
		}

		/// <summary>
		/// Does this icon get hidden when the block is collapsed.
		/// </summary>
		public bool collapseHidden = true;

		/// <summary>
		///  Height and width of icons.
		/// </summary>
		public int SIZE = 17;

		/// <summary>
		/// Bubble UI (if visible).
		/// </summary>
		protected Bubble bubble_;

		/// <summary>
		/// Absolute coordinate of icon's center.
		/// </summary>
		protected goog.math.Coordinate iconXY_;

		protected SVGElement iconGroup_;

		/// <summary>
		/// Create the icon on the block.
		/// </summary>
		internal void createIcon()
		{
			if (this.iconGroup_ != null) {
				// Icon already exists.
				return;
			}
			/* Here's the markup that will be generated:
			<g class="blocklyIconGroup">
			  ...
			</g>
			*/
			this.iconGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{ "class", "blocklyIconGroup" } }, null);
			if (this.block_.isInFlyout) {
				Core.addClass_(this.iconGroup_, "blocklyIconGroupReadonly");
			}
			this.drawIcon_(this.iconGroup_);

			this.block_.getSvgRoot().AppendChild(this.iconGroup_);
			Core.bindEventWithChecks_(this.iconGroup_, "mouseup", this,
				new Action<MouseEvent>(this.iconClick_));
			this.updateEditable();
		}

		protected abstract void drawIcon_(SVGElement group);

		/// <summary>
		/// Dispose of this icon.
		/// </summary>
		public virtual void dispose()
		{
			// Dispose of and unlink the icon.
			goog.dom.removeNode(this.iconGroup_);
			this.iconGroup_ = null;
			// Dispose of and unlink the bubble.
			this.setVisible(false);
			this.block_ = null;
		}

		/// <summary>
		/// Add or remove the UI indicating if this icon may be clicked or not.
		/// </summary>
		public virtual void updateEditable()
		{
		}

		/// <summary>
		/// Is the associated bubble visible?
		/// </summary>
		/// <returns>True if the bubble is visible.</returns>
		public bool isVisible()
		{
			return this.bubble_ != null;
		}

		internal abstract void setVisible(bool visible);

		/// <summary>
		/// Clicking on the icon toggles if the bubble is visible.
		/// </summary>
		/// <param name="e">Mouse click event.</param>
		protected virtual void iconClick_(MouseEvent e)
		{
			if (((WorkspaceSvg)this.block_.workspace).isDragging()) {
				// Drag operation is concluding.  Don't open the editor.
				return;
			}
			if (!this.block_.isInFlyout && !Core.isRightButton(e)) {
				this.setVisible(!this.isVisible());
			}
		}

		/// <summary>
		/// Change the colour of the associated bubble to match its block.
		/// </summary>
		internal void updateColour()
		{
			if (this.isVisible()) {
				this.bubble_.setColour(this.block_.getColour());
			}
		}

		/// <summary>
		/// Render the icon.
		/// </summary>
		/// <param name="cursorX">Horizontal offset at which to position the icon.</param>
		/// <returns>Horizontal offset for next item to draw.</returns>
		internal int renderIcon(int cursorX)
		{
			if (this.collapseHidden && this.block_.isCollapsed()) {
				this.iconGroup_.SetAttribute("display", "none");
				return cursorX;
			}
			this.iconGroup_.SetAttribute("display", "block");

			var TOP_MARGIN = 5;
			var width = this.SIZE;
			if (this.block_.RTL) {
				cursorX -= width;
			}
			this.iconGroup_.SetAttribute("transform",
				"translate(" + cursorX + "," + TOP_MARGIN + ")");
			this.computeIconLocation();
			if (this.block_.RTL) {
				cursorX -= BlockSvg.SEP_SPACE_X;
			}
			else {
				cursorX += width + BlockSvg.SEP_SPACE_X;
			}
			return cursorX;
		}

		/// <summary>
		/// Notification that the icon has moved.  Update the arrow accordingly.
		/// </summary>
		/// <param name="xy">Absolute location.</param>
		internal void setIconLocation(goog.math.Coordinate xy)
		{
			this.iconXY_ = xy;
			if (this.isVisible()) {
				this.bubble_.setAnchorLocation(xy);
			}
		}

		/// <summary>
		/// Notification that the icon has moved, but we don't really know where.
		/// Recompute the icon's location from scratch.
		/// </summary>
		internal void computeIconLocation()
		{
			// Find coordinates for the centre of the icon and update the arrow.
			var blockXY = this.block_.getRelativeToSurfaceXY();
			var iconXY = Core.getRelativeXY_(this.iconGroup_);
			var newXY = new goog.math.Coordinate(
				blockXY.x + iconXY.x + this.SIZE / 2,
				blockXY.y + iconXY.y + this.SIZE / 2);
			if (!goog.math.Coordinate.equals(this.getIconLocation(), newXY)) {
				this.setIconLocation(newXY);
			}
		}

		internal goog.math.Coordinate getIconLocation()
		{
			return this.iconXY_;
		}

		public virtual string getText()
		{
			return null;
		}

		public virtual void setText(string text, string opt_pid = null)
		{
		}
	}
}
