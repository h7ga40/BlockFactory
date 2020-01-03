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
 * @fileoverview Object representing a mutator dialog.  A mutator allows the
 * user to change the shape of a block using a nested blocks editor.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Mutator : Icon
	{
		private string[] quarkNames_;
		public Workspace workspace_;

		/// <summary>
		/// Class for a mutator dialog.
		/// </summary>
		/// <param name="quarkNames">List of names of sub-blocks for flyout.</param>
		public Mutator(string[] quarkNames)
			: base(null)
		{
			this.quarkNames_ = quarkNames;
		}

		/// <summary>
		/// Width of workspace.
		/// </summary>
		private double workspaceWidth_;

		/// <summary>
		/// Height of workspace.
		/// </summary>
		private double workspaceHeight_;

		/// <summary>
		/// Draw the mutator icon.
		/// </summary>
		/// <param name="group">The icon group.</param>
		protected override void drawIcon_(SVGElement group)
		{
			// Square with rounded corners.
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"class", "blocklyIconShape" },
					{ "rx", "4" }, {"ry", "4" },
					{ "height", "16" }, {"width", "16"}},
				 group);
			// Gear teeth.
			Core.createSvgElement("path", new Dictionary<string, object>() {
					{"class", "blocklyIconSymbol" },
					{ "d", "m4.203,7.296 0,1.368 -0.92,0.677 -0.11,0.41 0.9,1.559 0.41,0.11 1.043,-0.457 1.187,0.683 0.127,1.134 0.3,0.3 1.8,0 0.3,-0.299 0.127,-1.138 1.185,-0.682 1.046,0.458 0.409,-0.11 0.9,-1.559 -0.11,-0.41 -0.92,-0.677 0,-1.366 0.92,-0.677 0.11,-0.41 -0.9,-1.559 -0.409,-0.109 -1.046,0.458 -1.185,-0.682 -0.127,-1.138 -0.3,-0.299 -1.8,0 -0.3,0.3 -0.126,1.135 -1.187,0.682 -1.043,-0.457 -0.41,0.11 -0.899,1.559 0.108,0.409z"}},
				 group);
			// Axle hole.
			Core.createSvgElement("circle", new Dictionary<string, object>() {
					{"class", "blocklyIconShape" }, {"r", "2.7" }, {"cx", "8" }, {"cy", "8"}},
				group);
		}

		/// <summary>
		/// Clicking on the icon toggles if the mutator bubble is visible.
		/// Disable if block is uneditable.
		/// </summary>
		/// <param name="e">Mouse click event.</param>
		protected override void iconClick_(MouseEvent e)
		{
			if (this.block_.isEditable()) {
				base.iconClick_(e);
			}
		}

		private SVGElement svgDialog_;

		/// <summary>
		/// Create the editor for the mutator's bubble.
		/// </summary>
		/// <returns>The top-level node of the editor.</returns>
		private SVGElement createEditor_()
		{
			/* Create the editor.  Here's the markup that will be generated:
			<svg>
			  [Workspace]
			</svg>
			*/
			this.svgDialog_ = Core.createSvgElement("svg", new Dictionary<string, object>() {
					{"x", Bubble.BORDER_WIDTH }, {"y", Bubble.BORDER_WIDTH}},
				null);
			// Convert the list of names into a list of XML objects for the flyout.
			Element quarkXml;
			if (this.quarkNames_.Length != 0) {
				quarkXml = (Element)goog.dom.createDom("xml");
				foreach (var quarkName in this.quarkNames_) {
					var block = (Element)goog.dom.createDom("block");
					block.SetAttribute("type", quarkName);
					quarkXml.AppendChild(block);
				}
			}
			else {
				quarkXml = null;
			}
			var workspaceOptions = new Options {
				languageTree = quarkXml,
				parentWorkspace = this.block_.workspace,
				pathToMedia = this.block_.workspace.options.pathToMedia,
				RTL = this.block_.RTL,
				toolboxPosition = this.block_.RTL ? Core.TOOLBOX_AT_RIGHT :
				  Core.TOOLBOX_AT_LEFT,
				horizontalLayout = false,
				getMetrics = new Func<Metrics>(() => this.getFlyoutMetrics_()),
				setMetrics = null
			};
			this.workspace_ = new WorkspaceSvg(workspaceOptions);
			this.workspace_.MAX_UNDO = 0;
			this.workspace_.isMutator = true;
			this.svgDialog_.AppendChild(
				((WorkspaceSvg)this.workspace_).createDom("blocklyMutatorBackground"));
			return this.svgDialog_;
		}

		public override void updateEditable()
		{
			if (!this.block_.isInFlyout) {
				if (this.block_.isEditable()) {
					if (this.iconGroup_ != null) {
						Core.removeClass_(this.iconGroup_, "blocklyIconGroupReadonly");
					}
				}
				else {
					// Close any mutator bubble.  Icon is not clickable.
					this.setVisible(false);
					if (this.iconGroup_ != null) {
						Core.addClass_(/** @type {!Element} */ (this.iconGroup_),
										  "blocklyIconGroupReadonly");
					}
				}
			}
			// Default behaviour for an icon.
			base.updateEditable();
		}

		/// <summary>
		/// Callback function triggered when the bubble has resized.
		/// Resize the workspace accordingly.
		/// </summary>
		private void resizeBubble_()
		{
			var doubleBorderWidth = 2 * Bubble.BORDER_WIDTH;
			var workspaceSize = ((WorkspaceSvg)this.workspace_).getCanvas().getBBox();
			double width;
			if (this.block_.RTL) {
				width = -workspaceSize.x;
			}
			else {
				width = workspaceSize.width + workspaceSize.x;
			}
			var height = workspaceSize.height + doubleBorderWidth * 3;
			if (((WorkspaceSvg)this.workspace_).flyout_ != null) {
				var flyoutMetrics = ((WorkspaceSvg)this.workspace_).flyout_.getMetrics_();
				height = System.Math.Max(height, flyoutMetrics.contentHeight + 20);
			}
			width += doubleBorderWidth * 3;
			// Only resize if the size difference is significant.  Eliminates shuddering.
			if (System.Math.Abs(this.workspaceWidth_ - width) > doubleBorderWidth ||
				System.Math.Abs(this.workspaceHeight_ - height) > doubleBorderWidth) {
				// Record some layout information for getFlyoutMetrics_.
				this.workspaceWidth_ = width;
				this.workspaceHeight_ = height;
				// Resize the bubble.
				this.bubble_.setBubbleSize(width + doubleBorderWidth,
										   height + doubleBorderWidth);
				this.svgDialog_.SetAttribute("width", this.workspaceWidth_.ToString());
				this.svgDialog_.SetAttribute("height", this.workspaceHeight_.ToString());
			}

			if (this.block_.RTL) {
				// Scroll the workspace to always left-align.
				var translation = "translate(" + this.workspaceWidth_ + ",0)";
				((WorkspaceSvg)this.workspace_).getCanvas().SetAttribute("transform", translation);
			}
			((WorkspaceSvg)this.workspace_).resize();
		}

		private Block rootBlock_;
		private Action<Events.Abstract> sourceListener_;

		internal override void setVisible(bool visible)
		{
			if (visible == this.isVisible()) {
				// No change.
				return;
			}
			Events.fire(
				new Events.Ui(this.block_, "mutatorOpen", (!visible).ToString(), visible.ToString()));
			if (visible) {
				// Create the bubble.
				this.bubble_ = new Bubble((WorkspaceSvg)this.block_.workspace,
					this.createEditor_(), this.block_.svgPath_, this.iconXY_, 0.0, 0.0);
				var tree = this.workspace_.options.languageTree;
				if (tree != null) {
					((WorkspaceSvg)this.workspace_).flyout_.init((WorkspaceSvg)this.workspace_);
					((WorkspaceSvg)this.workspace_).flyout_.show(new JsArray<Node>(tree.ChildNodes));
				}

				this.rootBlock_ = this.block_.decompose(this.workspace_);
				var blocks = this.rootBlock_.getDescendants();
				foreach (BlockSvg child in blocks) {
					child.render();
				}
				// The root block should not be dragable or deletable.
				this.rootBlock_.setMovable(false);
				this.rootBlock_.setDeletable(false);
				double margin, x;
				if (((WorkspaceSvg)this.workspace_).flyout_ != null) {
					margin = Flyout.CORNER_RADIUS * 2;
					x = ((WorkspaceSvg)this.workspace_).flyout_.width_ + margin;
				}
				else {
					margin = 16;
					x = margin;
				}
				if (this.block_.RTL) {
					x = -x;
				}
				this.rootBlock_.moveBy(x, margin);
				// Save the initial connections, then listen for further changes.
				if (true/*this.block_.saveConnections != null*/) {
					var thisMutator = this;
					this.block_.saveConnections(this.rootBlock_);
					this.sourceListener_ = new Action<Events.Abstract>((e) => {
						thisMutator.block_.saveConnections(thisMutator.rootBlock_);
					});
					this.block_.workspace.addChangeListener(this.sourceListener_);
				}
				this.resizeBubble_();
				// When the mutator's workspace changes, update the source block.
				this.workspace_.addChangeListener((e) => { workspaceChanged_(); });
				this.updateColour();
			}
			else {
				// Dispose of the bubble.
				this.svgDialog_ = null;
				this.workspace_.dispose();
				this.workspace_ = null;
				this.rootBlock_ = null;
				this.bubble_.dispose();
				this.bubble_ = null;
				this.workspaceWidth_ = 0;
				this.workspaceHeight_ = 0;
				if (this.sourceListener_ != null) {
					this.block_.workspace.removeChangeListener(this.sourceListener_);
					this.sourceListener_ = null;
				}
			}
		}

		/// <summary>
		/// Update the source block when the mutator's blocks are changed.
		/// Bump down any block that's too high.
		/// Fired whenever a change is made to the mutator's workspace.
		/// </summary>
		private void workspaceChanged_()
		{
			if (this.workspace_ == null)
				return;

			if (Core.dragMode_ == Core.DRAG_NONE) {
				var blocks = this.workspace_.getTopBlocks(false);
				var MARGIN = 20;
				foreach (BlockSvg block in blocks) {
					var blockXY = block.getRelativeToSurfaceXY();
					var blockHW = block.getHeightWidth();
					if (blockXY.y + blockHW.height < MARGIN) {
						// Bump any block that's above the top back inside.
						block.moveBy(0, MARGIN - blockHW.height - blockXY.y);
					}
				}
			}

			// When the mutator's workspace changes, update the source block.
			if (this.rootBlock_.workspace == this.workspace_) {
				Events.setGroup(true);
				var block = this.block_;
				var oldMutationDom = block.mutationToDom();
				var oldMutation = oldMutationDom != null && Xml.domToText(oldMutationDom) != null;
				// Switch off rendering while the source block is rebuilt.
				var savedRendered = block.rendered;
				block.rendered = false;
				// Allow the source block to rebuild itself.
				block.compose(this.rootBlock_);
				// Restore rendering and show the changes.
				block.rendered = savedRendered;
				// Mutation may have added some elements that need initalizing.
				block.initSvg();
				var newMutationDom = block.mutationToDom();
				var newMutation = newMutationDom != null && Xml.domToText(newMutationDom) != null;
				if (oldMutation != newMutation) {
					Events.fire(new Events.Change(
						block, "mutation", null, oldMutation.ToString(), newMutation.ToString()));
					// Ensure that any bump is part of this mutation's event group.
					var group = Events.getGroup();
					Window.SetTimeout(() => {
						Events.setGroup(group);
						block.bumpNeighbours_();
						Events.setGroup(false);
					}, Core.BUMP_DELAY);
				}
				if (block.rendered) {
					block.render();
				}
				this.resizeBubble_();
				Events.setGroup(false);
			}
		}

		/// <summary>
		/// Return an object with all the metrics required to size scrollbars for the
		/// mutator flyout.  The following properties are computed:
		/// .viewHeight: Height of the visible rectangle,
		/// .viewWidth: Width of the visible rectangle,
		/// .absoluteTop: Top-edge of view.
		/// .absoluteLeft: Left-edge of view.
		/// </summary>
		/// <returns>Contains size and position metrics of mutator dialog's
		/// workspace.</returns>
		private Metrics getFlyoutMetrics_()
		{
			return new Metrics {
				viewHeight = this.workspaceHeight_,
				viewWidth = this.workspaceWidth_,
				absoluteTop = 0,
				absoluteLeft = 0
			};
		}

		/// <summary>
		/// Dispose of this mutator.
		/// </summary>
		public override void dispose()
		{
			this.block_.mutator = null;
			base.dispose();
		}

		/// <summary>
		/// Reconnect an block to a mutated input.
		/// </summary>
		/// <param name="connectionChild">Connection on child block.</param>
		/// <param name="block">Parent block.</param>
		/// <param name="inputName">Name of input on parent block.</param>
		/// <returns>True iff a reconnection was made, false otherwise.</returns>
		internal static bool reconnect(Connection connectionChild, Block block, string inputName)
		{
			if (connectionChild == null || connectionChild.getSourceBlock().workspace == null) {
				return false;  // No connection or block has been deleted.
			}
			var connectionParent = block.getInput(inputName).connection;
			var currentParent = connectionChild.targetBlock();
			if ((currentParent == null || currentParent == block) &&
				connectionParent.targetConnection != connectionChild) {
				if (connectionParent.isConnected()) {
					// There's already something connected here.  Get rid of it.
					connectionParent.disconnect();
				}
				connectionParent.connect(connectionChild);
				return true;
			}
			return false;
		}
	}
}
