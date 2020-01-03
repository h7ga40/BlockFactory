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
 * @fileoverview Toolbox from whence to create blocks.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;
using goog;
using goog.html;
using goog.ui.tree;
using static goog.ui.tree.BaseNode;

namespace Blockly
{
	public class Toolbox
	{
		internal Flyout flyout_;
		internal HTMLDivElement HtmlDiv;

		private WorkspaceSvg workspace_;
		/// <summary>
		/// Is RTL vs LTR.
		/// </summary>
		public bool RTL;
		/// <summary>
		/// Whether the toolbox should be laid out horizontally.
		/// </summary>
		private bool horizontalLayout_;
		/// <summary>
		/// Position of the toolbox and flyout relative to the workspace.
		/// </summary>
		public double toolboxPosition;
		/// <summary>
		/// Configuration constants for Closure's tree UI.
		/// </summary>
		private Config config_;
		/// <summary>
		/// Configuration constants for tree separator.
		/// </summary>
		private Config treeSeparatorConfig_;

		/// <summary>
		/// Class for a Toolbox.
		/// Creates the toolbox's DOM.
		/// </summary>
		/// <param name="workspaceSvg">The workspace in which to create new</param>
		public Toolbox(WorkspaceSvg workspace)
		{
			this.workspace_ = workspace;
			this.RTL = workspace.options.RTL;
			this.horizontalLayout_ = workspace.options.horizontalLayout;
			this.toolboxPosition = workspace.options.toolboxPosition;
			this.config_ = new Config {
				indentWidth = 19,
				cssRoot = "blocklyTreeRoot",
				cssHideRoot = "blocklyHidden",
				cssItem = "",
				cssTreeRow = "blocklyTreeRow",
				cssItemLabel = "blocklyTreeLabel",
				cssTreeIcon = "blocklyTreeIcon",
				cssExpandedFolderIcon = "blocklyTreeIconOpen",
				cssFileIcon = "blocklyTreeIconNone",
				cssSelectedRow = "blocklyTreeSelected"
			};

			this.treeSeparatorConfig_ = new Config {
				cssTreeRow = "blocklyTreeSeparator"
			};

			if (this.horizontalLayout_) {
				this.config_.cssTreeRow =
					this.config_.cssTreeRow +
					(workspace.RTL ?
					" blocklyHorizontalTreeRtl" : " blocklyHorizontalTree");

				this.treeSeparatorConfig_.cssTreeRow =
					"blocklyTreeSeparatorHorizontal " +
					(workspace.RTL ?
					"blocklyHorizontalTreeRtl" : "blocklyHorizontalTree");
				this.config_.cssTreeIcon = "";
			}
		}

		/// <summary>
		/// Width of the toolbox, which changes only in vertical layout.
		/// </summary>
		public double width;

		/// <summary>
		/// Height of the toolbox, which changes only in horizontal layout.
		/// </summary>
		public double height;

		/// <summary>
		/// The SVG group currently selected.
		/// </summary>
		private SVGElement selectedOption_;

		/// <summary>
		/// The tree node most recently selected.
		/// </summary>
		private goog.ui.tree.BaseNode lastCategory_;

		private TreeControl tree_;

		/// <summary>
		/// Initializes the toolbox.
		/// </summary>
		public void init()
		{
			var workspace = this.workspace_;
			var svg = this.workspace_.getParentSvg();

			// Create an HTML container for the Toolbox menu.
			this.HtmlDiv = (HTMLDivElement)
				goog.dom.createDom(goog.dom.TagName.DIV, "blocklyToolboxDiv");
			this.HtmlDiv.SetAttribute("dir", workspace.RTL ? "RTL" : "LTR");
			svg.ParentNode.InsertBefore(this.HtmlDiv, svg);

			// Clicking on toolbox closes popups.
			Core.bindEventWithChecks_(this.HtmlDiv, "mousedown", null,
				new Action<MouseEvent>((e) => {
					if (Core.isRightButton(e) || e.Target == this.HtmlDiv) {
						// Close flyout.
						Core.hideChaff(false);
					}
					else {
						// Just close popups.
						Core.hideChaff(true);
					}
					Touch.clearTouchIdentifier();  // Don't block future drags.
				}));
			var workspaceOptions = new Options {
				disabledPatternId = workspace.options.disabledPatternId,
				parentWorkspace = workspace,
				RTL = workspace.RTL,
				oneBasedIndex = workspace.options.oneBasedIndex,
				horizontalLayout = workspace.horizontalLayout,
				toolboxPosition = workspace.options.toolboxPosition
			};
			/**
			 * @type {!Blockly.Flyout}
			 * @private
			 */
			this.flyout_ = new Flyout(workspaceOptions);
			goog.dom.insertSiblingAfter(this.flyout_.createDom(), workspace.svgGroup_);
			this.flyout_.init(workspace);

			this.config_.cleardotPath = workspace.options.pathToMedia + "1x1.gif";
			this.config_.cssCollapsedFolderIcon =
				"blocklyTreeIconClosed" + (workspace.RTL ? "Rtl" : "Ltr");
			var tree = new Toolbox.TreeControl(this, this.config_);
			this.tree_ = tree;
			tree.setShowRootNode(false);
			tree.setShowLines(false);
			tree.setShowExpandIcons(false);
			tree.setSelectedItem(null);
			var openNode = this.populate_(workspace.options.languageTree);
			tree.render(this.HtmlDiv);
			if (openNode != null) {
				tree.setSelectedItem(openNode);
			}
			this.addColour_();
			this.position();
		}

		/// <summary>
		/// Dispose of this toolbox.
		/// </summary>
		public void dispose()
		{
			this.flyout_.dispose();
			this.tree_.dispose();
			goog.dom.removeNode(this.HtmlDiv);
			this.workspace_ = null;
			this.lastCategory_ = null;
		}

		/// <summary>
		/// Get the width of the toolbox.
		/// </summary>
		/// <returns>The width of the toolbox.</returns>
		public double getWidth()
		{
			return this.width;
		}

		/// <summary>
		/// Get the height of the toolbox.
		/// </summary>
		/// <returns>The width of the toolbox.</returns>
		public double getHeight()
		{
			return this.height;
		}

		/// <summary>
		/// Move the toolbox to the edge.
		/// </summary>
		public void position()
		{
			var treeDiv = this.HtmlDiv;
			if (treeDiv == null) {
				// Not initialized yet.
				return;
			}
			var svg = this.workspace_.getParentSvg();
			var svgPosition = goog.style.getPageOffset(svg);
			var svgSize = Core.svgSize(svg);
			if (this.horizontalLayout_) {
				treeDiv.Style.Left = "0";
				treeDiv.Style.Height = "auto";
				treeDiv.Style.Width = svgSize.width + "px";
				this.height = treeDiv.OffsetHeight;
				if (this.toolboxPosition == Core.TOOLBOX_AT_TOP) {  // Top
					treeDiv.Style.Top = "0";
				}
				else {  // Bottom
					treeDiv.Style.Bottom = "0";
				}
			}
			else {
				if (this.toolboxPosition == Core.TOOLBOX_AT_RIGHT) {  // Right
					treeDiv.Style.Right = "0";
				}
				else {  // Left
					treeDiv.Style.Left = "0";
				}
				treeDiv.Style.Height = svgSize.height + "px";
				this.width = treeDiv.OffsetWidth;
			}
			this.flyout_.position();
		}

		private bool hasColours_;

		/// <summary>
		/// Fill the toolbox with categories and blocks.
		/// </summary>
		/// <param name="newTree">DOM tree of blocks.</param>
		internal goog.ui.tree.BaseNode populate_(Element newTree)
		{
			this.tree_.removeChildren();  // Delete any existing content.
			this.tree_.blocks = new JsArray<Node>();
			this.hasColours_ = false;
			var openNode =
			  this.syncTrees_(newTree, this.tree_, this.workspace_.options.pathToMedia);

			if (this.tree_.blocks.As<JsArray<Node>>()?.Length != 0) {
				throw new Exception("Toolbox cannot have both blocks and categories in the root level.");
			}

			// Fire a resize event since the toolbox may have changed width and height.
			this.workspace_.resizeContents();
			return openNode;
		}

		/// <summary>
		/// Sync trees of the toolbox.
		/// </summary>
		/// <param name="treeIn">DOM tree of blocks.</param>
		/// <param name="treeOut"></param>
		/// <param name="pathToMedia"></param>
		/// <returns>Tree node to open at startup (or null).</returns>
		private goog.ui.tree.BaseNode syncTrees_(Element treeIn, ITreeNode treeOut, string pathToMedia)
		{
			goog.ui.tree.BaseNode openNode = null;
			Element lastElement = null;
			foreach (var childIn_ in treeIn.ChildNodes) {
				if (!(childIn_ is Element)) {
					// Skip over text.
					continue;
				}
				Element childIn = (Element)childIn_;
				switch (childIn.TagName.ToUpperCase()) {
				case "CATEGORY":
					var childOut = (ITreeNode)this.tree_.createNode(childIn.GetAttribute("name"));
					childOut.blocks = new JsArray<Node>();
					treeOut.add((BaseNode)childOut);
					var custom = childIn.GetAttribute("custom");
					if (custom != null) {
						// Variables and procedures are special dynamic categories.
						childOut.blocks = custom;
					}
					else {
						var newOpenNode = this.syncTrees_(childIn, childOut, pathToMedia);
						if (newOpenNode != null) {
							openNode = newOpenNode;
						}
					}
					var colour = childIn.GetAttribute("colour");
					if (colour is string) {
						if (colour.Match(new Regex(@"^#[0-9a-fA-F]{6}$")) != null) {
							childOut.hexColour = colour;
						}
						else {
							childOut.hexColour = Core.hueToRgb(Script.ParseFloat(colour));
						}
						this.hasColours_ = true;
					}
					else {
						childOut.hexColour = "";
					}
					if (childIn.GetAttribute("expanded") == "true") {
						if (childOut.blocks.Is<JsArray<Node>>() && childOut.blocks.As<JsArray<Node>>().Length != 0) {
							// This is a category that directly contians blocks.
							// After the tree is rendered, open this category and show flyout.
							openNode = (BaseNode)childOut;
						}
						childOut.setExpanded(true);
					}
					else {
						childOut.setExpanded(false);
					}
					lastElement = childIn;
					break;
				case "SEP":
					if (lastElement != null) {
						if (lastElement.TagName.ToUpperCase() == "CATEGORY") {
							// Separator between two categories.
							// <sep></sep>
							treeOut.add(new Toolbox.TreeSeparator(
								this.treeSeparatorConfig_));
						}
						else {
							// Change the gap between two blocks.
							// <sep gap="36"></sep>
							// The default gap is 24, can be set larger or smaller.
							// Note that a deprecated method is to add a gap to a block.
							// <block type="math_arithmetic" gap="8"></block>
							var newGap = Script.ParseFloat(childIn.GetAttribute("gap"));
							if (!Double.IsNaN(newGap) && lastElement != null) {
								lastElement.SetAttribute("gap", newGap.ToString());
							}
						}
					}
					break;
				case "BLOCK":
				case "SHADOW":
					treeOut.blocks.As<JsArray<Node>>().Push(childIn);
					lastElement = childIn;
					break;
				}
			}
			return openNode;
		}

		/// <summary>
		/// Recursively add colours to this toolbox.
		/// </summary>
		/// <param name="opt_tree">Starting point of tree.
		/// Defaults to the root node.</param>
		internal void addColour_(goog.ui.tree.BaseNode opt_tree = null)
		{
			var tree = opt_tree != null ? opt_tree : (goog.ui.tree.BaseNode)this.tree_;
			var children = tree.getChildren();
			foreach (ITreeNode child in children) {
				var element = child.getRowElement();
				if (element != null) {
					string border;
					if (this.hasColours_) {
						border = "8px solid " + (child.hexColour ?? "#ddd");
					}
					else {
						border = "none";
					}
					if (this.workspace_.RTL) {
						element.Style.BorderRight = border;
					}
					else {
						element.Style.BorderLeft = border;
					}
				}
				this.addColour_((BaseNode)child);
			}
		}

		/// <summary>
		/// Unhighlight any previously specified option.
		/// </summary>
		public void clearSelection()
		{
			this.tree_.setSelectedItem(null);
		}

		/// <summary>
		/// Return the deletion rectangle for this toolbox.
		/// </summary>
		/// <returns>Rectangle in which to delete.</returns>
		public goog.math.Rect getClientRect()
		{
			if (this.HtmlDiv == null) {
				return null;
			}

			// BIG_NUM is offscreen padding so that blocks dragged beyond the toolbox
			// area are still deleted.  Must be smaller than Infinity, but larger than
			// the largest screen size.
			var BIG_NUM = 10000000;
			var toolboxRect = this.HtmlDiv.GetBoundingClientRect();

			var x = toolboxRect.Left;
			var y = toolboxRect.Top;
			var width = toolboxRect.Width;
			var height = toolboxRect.Height;

			// Assumes that the toolbox is on the SVG edge.  If this changes
			// (e.g. toolboxes in mutators) then this code will need to be more complex.
			if (this.toolboxPosition == Core.TOOLBOX_AT_LEFT) {
				return new goog.math.Rect(-BIG_NUM, -BIG_NUM, BIG_NUM + x + width,
					2 * BIG_NUM);
			}
			else if (this.toolboxPosition == Core.TOOLBOX_AT_RIGHT) {
				return new goog.math.Rect(x, -BIG_NUM, BIG_NUM + width, 2 * BIG_NUM);
			}
			else if (this.toolboxPosition == Core.TOOLBOX_AT_TOP) {
				return new goog.math.Rect(-BIG_NUM, -BIG_NUM, 2 * BIG_NUM,
					BIG_NUM + y + height);
			}
			else {  // Bottom
				return new goog.math.Rect(0, y, 2 * BIG_NUM, BIG_NUM + width);
			}
		}

		/// <summary>
		/// Update the flyout's contents without closing it.  Should be used in response
		/// to a change in one of the dynamic categories, such as variables or
		/// procedures.
		/// </summary>
		public void refreshSelection()
		{
			var selectedItem = (ITreeNode)this.tree_.getSelectedItem();
			if (selectedItem != null && selectedItem.blocks != null) {
				this.flyout_.show(selectedItem.blocks);
			}
		}

		public interface ITreeNode
		{
			Union<string, JsArray<Node>, NodeList> blocks { get; set; }
			string hexColour { get; set; }
			BaseNode add(BaseNode child, BaseNode opt_before = null);
			HTMLElement getRowElement();
			void setExpanded(bool v);
		}

		// Extending Closure's Tree UI.

		public class TreeControl : goog.ui.tree.TreeControl, ITreeNode
		{
			private Toolbox toolbox_;
			private Config config_;
			public Union<string, JsArray<Node>, NodeList> blocks { get; set; }
			public string hexColour { get; set; }

			/// <summary>
			/// Extention of a TreeControl object that uses a custom tree node.
			/// </summary>
			/// <param name="toolbox">The parent toolbox for this tree.</param>
			/// <param name="config">The configuration for the tree. See
			/// goog.ui.tree.TreeControl.DefaultConfig.</param>
			public TreeControl(Toolbox toolbox, Config config)
				: base(goog.html.SafeHtml.EMPTY, config)
			{
				this.toolbox_ = toolbox;
				this.config_ = config;
			}

			/// <summary>
			/// Adds touch handling to TreeControl.
			/// </summary>
			public override void enterDocument()
			{
				base.enterDocument();

				var el = this.getElement();
				// Add touch handler.
				if (goog.events.BrowserFeature.TOUCH_ENABLED) {
					Core.bindEventWithChecks_(el, goog.events.EventType.TOUCHSTART, this,
						new Action<TouchEvent>(this.handleTouchEvent_));
				}
			}

			/// <summary>
			/// Handles touch events.
			/// </summary>
			/// <param name="e">The browser event.</param>
			private void handleTouchEvent_(TouchEvent e)
			{
				var be = new goog.events.BrowserEvent(e);
				e.PreventDefault();
				var node = this.getNodeFromEvent_(be);
				if (node != null && e.Type == goog.events.EventType.TOUCHSTART) {
					// Fire asynchronously since onMouseDown takes long enough that the browser
					// would fire the default mouse event before this method returns.
					Window.SetTimeout(() => {
						node.onMouseDown(be);  // Same behaviour for click and touch.
					}, 1);
				}
			}

			/// <summary>
			/// Creates a new tree node using a custom tree node.
			/// </summary>
			/// <param name="opt_html">The HTML content of the node label.</param>
			/// <returns>The new item.</returns>
			public override goog.ui.tree.TreeNode createNode(string opt_html = null)
			{
				return new Toolbox.TreeNode(this.toolbox_, opt_html != null ?
					goog.html.SafeHtml.htmlEscape(opt_html) : goog.html.SafeHtml.EMPTY,
					this.getConfig(), this.getDomHelper());
			}

			/// <summary>
			/// Display/hide the flyout when an item is selected.
			/// </summary>
			/// <param name="node">The item to select.</param>
			public override void setSelectedItem(goog.ui.tree.BaseNode node)
			{
				var toolbox = this.toolbox_;
				if (node == this.selectedItem_ || node == toolbox.tree_) {
					return;
				}
				if (toolbox.lastCategory_ != null) {
					toolbox.lastCategory_.getRowElement().Style.BackgroundColor = "";
				}
				if (node is TreeNode) {
					var treeNode = (TreeNode)node;
					var hexColour = treeNode.hexColour ?? "#57e";
					treeNode.getRowElement().Style.BackgroundColor = hexColour;
					// Add colours to child nodes which may have been collapsed and thus
					// not rendered.
					toolbox.addColour_(treeNode);
				}
				else if (node is TreeControl) {
					var treeControl = (TreeControl)node;
					var hexColour = treeControl.hexColour ?? "#57e";
					treeControl.getRowElement().Style.BackgroundColor = hexColour;
					// Add colours to child nodes which may have been collapsed and thus
					// not rendered.
					toolbox.addColour_(treeControl);
				}
				var oldNode = this.getSelectedItem();
				base.setSelectedItem(node);
				if (node != null && node is ITreeNode itreeNode && itreeNode.blocks.As<JsArray<Node>>()?.Length != 0) {
					toolbox.flyout_.show(itreeNode.blocks);
					// Scroll the flyout to the top if the category has changed.
					if (toolbox.lastCategory_ != node) {
						toolbox.flyout_.scrollToStart();
					}
				}
				else {
					// Hide the flyout.
					toolbox.flyout_.hide();
				}
				if (oldNode != node && oldNode != this) {
					var ev = new Events.Ui(null, "category",
						oldNode == null ? null : oldNode.getHtml(), node == null ? null : node.getHtml());
					ev.workspaceId = toolbox.workspace_.id;
					Events.fire(ev);
				}
				if (node != null) {
					toolbox.lastCategory_ = node;
				}
			}
		}

		public class TreeNode : goog.ui.tree.TreeNode, ITreeNode
		{
			private bool horizontalLayout_;
			public Union<string, JsArray<Node>, NodeList> blocks { get; set; }
			public string hexColour { get; set; }

			public TreeNode(Toolbox toolbox, goog.html.SafeHtml html,
				Config opt_config = null, goog.dom.DomHelper opt_domHelper = null)
				: base(html, opt_config, opt_domHelper)
			{
				if (toolbox != null) {
					this.horizontalLayout_ = toolbox.horizontalLayout_;
					var resize = new Action<Bridge.Html5.Event>((e) => {
						// Even though the div hasn't changed size, the visible workspace
						// surface of the workspace has, so we may need to reposition everything.
						Core.svgResize(toolbox.workspace_);
					});
					// Fire a resize event since the toolbox may have changed width.
					goog.events.listen(toolbox.tree_,
						goog.ui.tree.BaseNode.EventType.EXPAND, resize);
					goog.events.listen(toolbox.tree_,
						goog.ui.tree.BaseNode.EventType.COLLAPSE, resize);
				}
			}

			public override goog.html.SafeHtml getExpandIconSafeHtml()
			{
				return goog.html.SafeHtml.create("span");
			}

			/// <summary>
			/// Expand or collapse the node on mouse click.
			/// </summary>
			/// <param name="e">The browser event.</param>
			public override void onMouseDown(goog.events.BrowserEvent e)
			{
				// Expand icon.
				if (this.hasChildren() && this.isUserCollapsible_) {
					this.toggle();
					this.select();
				}
				else if (this.isSelected()) {
					this.getTree().setSelectedItem(null);
				}
				else {
					this.select();
				}
				this.updateRow();
			}

			/// <summary>
			/// Supress the inherited double-click behaviour.
			/// </summary>
			/// <param name="e">The browser event.</param>
			internal override void onDoubleClick_(goog.events.BrowserEvent e)
			{
				// NOP.
			}

			/// <summary>
			/// Remap event.keyCode in horizontalLayout so that arrow
			/// keys work properly and call original onKeyDown handler.
			/// </summary>
			/// <param name="e">The browser event.</param>
			/// <returns>The handled value.</returns>
			public override bool onKeyDown(goog.events.BrowserEvent e)
			{
				if (this.horizontalLayout_) {
					var map = new Dictionary<goog.events.KeyCodes, goog.events.KeyCodes>();
					map[goog.events.KeyCodes.RIGHT] = goog.events.KeyCodes.DOWN;
					map[goog.events.KeyCodes.LEFT] = goog.events.KeyCodes.UP;
					map[goog.events.KeyCodes.UP] = goog.events.KeyCodes.LEFT;
					map[goog.events.KeyCodes.DOWN] = goog.events.KeyCodes.RIGHT;

					var newKeyCode = map.ContainsKey((goog.events.KeyCodes)e.keyCode);
					e.keyCode = (int)(newKeyCode ? map[(goog.events.KeyCodes)e.keyCode] : (goog.events.KeyCodes)e.keyCode);
				}
				return base.onKeyDown(e);
			}
		}

		public class TreeSeparator : Toolbox.TreeNode
		{
			/// <summary>
			/// A blank separator node in the tree.
			/// </summary>
			/// <param name="config">The configuration for the tree. See
			/// goog.ui.tree.TreeControl.DefaultConfig. If not specified, a default config
			/// will be used.</param>
			public TreeSeparator(Config config)
				: base(null, SafeHtml.EMPTY, config)
			{
			}
		}
	}
}
