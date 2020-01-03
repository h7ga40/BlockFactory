// Copyright 2007 The Closure Library Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/**
 * @fileoverview Definition of the goog.ui.tree.TreeControl class, which
 * provides a way to view a hierarchical set of data.
 *
 * @author arv@google.com (Erik Arvidsson)
 * @author eae@google.com (Emil A Eklund)
 *
 * This is a based on the webfx tree control. It since been updated to add
 * typeahead support, as well as accessibility support using ARIA framework.
 *
 * @see ../../demos/tree/demo.html
 */

using System;
using Bridge;
using Bridge.Html5;
using goog.html;

namespace goog.ui.tree
{
	public class TreeControl : TreeNode
	{
		protected TreeNode selectedItem_;
		private TypeAhead typeAhead_;
		private log.Logger logger_;
		private bool showLines_;
		private bool showExpandIcons_;
		private bool showRootNode_;
		private bool showRootLines_;
		private bool focused_;
		protected TreeNode focusedNode_;
		private events.KeyHandler keyHandler_;
		private events.FocusHandler focusHandler_;

		public TreeControl(html.SafeHtml html, Config opt_config = null, goog.dom.DomHelper opt_domHelper = null)
			: base(html, opt_config, opt_domHelper)
		{
			// The root is open and selected by default.
			this.setExpandedInternal(true);
			this.setSelectedInternal(true);

			this.selectedItem_ = this;

			this.typeAhead_ = new goog.ui.tree.TypeAhead();
			this.logger_ = goog.log.getLogger("this");
			this.showLines_ = true;
			this.showExpandIcons_ = true;
			this.showRootNode_ = true;
			this.showRootLines_ = true;

			if (goog.userAgent.IE) {
				try {
					// works since IE6SP1
					Document.ExecCommand("BackgroundImageCache", false, true);
				}
				catch (Exception) {
					goog.log.warning(this.logger_, "Failed to enable background image cache");
				}
			}
		}

		public override TreeControl getTree()
		{
			return this;
		}

		public override int getDepth()
		{
			return 0;
		}

		/// <summary>
		/// Expands the parent chain of this node so that it is visible.
		/// </summary>
		public override void reveal()
		{
			// always expanded by default
			// needs to be overriden so that we don't try to reveal our parent
			// which is a generic component
		}

		/// <summary>
		/// Handles focus on the tree.
		/// </summary>
		/// <param name="e">The browser event.</param>
		private void handleFocus_(goog.events.BrowserEvent e)
		{
			this.focused_ = true;
			var el = this.getElement();
			goog.asserts.assert(el != null);
			goog.dom.classlist.add(el, goog.Css.getCssName("focused"));

			if (this.selectedItem_ != null) {
				this.selectedItem_.select();
			}
		}

		/// <summary>
		/// Handles blur on the tree.
		/// </summary>
		/// <param name="e">The browser event.</param>
		private void handleBlur_(goog.events.BrowserEvent e)
		{
			this.focused_ = false;
			var el = this.getElement();
			goog.asserts.assert(el != null);
			goog.dom.classlist.remove(el, goog.Css.getCssName("focused"));
		}

		/// <summary>
		/// Whether the tree has keyboard focus.
		/// </summary>
		/// <returns>Whether the tree has keyboard focus.</returns>
		public bool hasFocus()
		{
			return this.focused_;
		}

		public override bool getExpanded()
		{
			return !this.showRootNode_ ||
				base.getExpanded();
		}

		public override void setExpanded(bool expanded)
		{
			if (!this.showRootNode_) {
				this.setExpandedInternal(expanded);
			}
			else {
				base.setExpanded(expanded);
			}
		}

		public override html.SafeHtml getExpandIconSafeHtml()
		{
			// no expand icon for root element
			return goog.html.SafeHtml.EMPTY;
		}

		public override HTMLElement getIconElement()
		{
			var el = this.getRowElement();
			return el != null ? (HTMLElement)el.FirstChild : null;
		}

		public override HTMLElement getExpandIconElement()
		{
			// no expand icon for root element
			return null;
		}

		public override void updateExpandIcon()
		{
			// no expand icon
		}

		public override string getRowClassName()
		{
			return base.getRowClassName() +
				(this.showRootNode_ ? "" : " " + this.getConfig().cssHideRoot);
		}

		/// <summary>
		/// Returns the source for the icon.
		/// </summary>
		/// <returns>Src for the icon.</returns>
		public override string getCalculatedIconClass()
		{
			var expanded = this.getExpanded();
			var expandedIconClass = this.getExpandedIconClass();
			if (expanded && expandedIconClass != null) {
				return expandedIconClass;
			}
			var iconClass = this.getIconClass();
			if (!expanded && iconClass != null) {
				return iconClass;
			}

			// fall back on default icons
			var config = this.getConfig();
			if (expanded && config.cssExpandedRootIcon != null) {
				return config.cssTreeIcon + " " + config.cssExpandedRootIcon;
			}
			else if (!expanded && config.cssCollapsedRootIcon != null) {
				return config.cssTreeIcon + " " + config.cssCollapsedRootIcon;
			}
			return "";
		}

		/// <summary>
		/// Sets the selected item.
		/// </summary>
		/// <param name="node">The item to select.</param>
		public virtual void setSelectedItem(BaseNode node)
		{
			if (this.selectedItem_ == node) {
				return;
			}

			var hadFocus = false;
			if (this.selectedItem_ != null) {
				hadFocus = this.selectedItem_ == this.focusedNode_;
				this.selectedItem_.setSelectedInternal(false);
			}

			this.selectedItem_ = (TreeNode)node;

			if (node != null) {
				node.setSelectedInternal(true);
				if (hadFocus) {
					node.select();
				}
			}

			this.dispatchEvent(goog.events.EventType.CHANGE);
		}

		/// <summary>
		/// Returns the selected item.
		/// </summary>
		/// <returns>The currently selected item.</returns>
		public TreeNode getSelectedItem()
		{
			return this.selectedItem_;
		}

		/// <summary>
		/// Sets whether to show lines.
		/// </summary>
		/// <param name="b">Whether to show lines.</param>
		public void setShowLines(bool b)
		{
			if (this.showLines_ != b) {
				this.showLines_ = b;
				if (this.isInDocument()) {
					this.updateLinesAndExpandIcons_();
				}
			}
		}

		/// <summary>
		/// Whether to show lines.
		/// </summary>
		/// <returns>Whether to show lines.</returns>
		public bool getShowLines()
		{
			return this.showLines_;
		}

		/// <summary>
		/// Updates the lines after the tree has been drawn.
		/// </summary>
		private void updateLinesAndExpandIcons_()
		{
			var tree = this;
			var showLines = tree.getShowLines();
			var showRootLines = tree.getShowRootLines();

			/**
			 * Recursively walk through all nodes and update the class names of the
			 * expand icon and the children element.
			 * @param {!goog.ui.tree.BaseNode} node
			 */
			Action<Component> updateShowLines = null;
			updateShowLines = new Action<Component>((node) => {
				var childrenEl = ((BaseNode)node).getChildrenElement();
				if (childrenEl != null) {
					var hideLines = !showLines || tree == node.getParent() && !showRootLines;
					var childClass = hideLines ? ((BaseNode)node).getConfig().cssChildrenNoLines :
												 ((BaseNode)node).getConfig().cssChildren;
					childrenEl.ClassName = childClass;

					var expandIconEl = ((BaseNode)node).getExpandIconElement();
					if (expandIconEl != null) {
						expandIconEl.ClassName = ((BaseNode)node).getExpandIconClass();
					}
				}
				node.forEachChild(updateShowLines);
			});
			updateShowLines(this);
		}

		/// <summary>
		/// Sets whether to show root lines.
		/// </summary>
		/// <param name="b">Whether to show root lines.</param>
		public void setShowRootLines(bool b)
		{
			if (this.showRootLines_ != b) {
				this.showRootLines_ = b;
				if (this.isInDocument()) {
					this.updateLinesAndExpandIcons_();
				}
			}
		}

		/// <summary>
		/// Whether to show root lines.
		/// </summary>
		/// <returns>Whether to show root lines.</returns>
		public bool getShowRootLines()
		{
			return this.showRootLines_;
		}

		/// <summary>
		/// Sets whether to show expand icons.
		/// </summary>
		/// <param name="b">Whether to show expand icons.</param>
		public void setShowExpandIcons(bool b)
		{
			if (this.showExpandIcons_ != b) {
				this.showExpandIcons_ = b;
				if (this.isInDocument()) {
					this.updateLinesAndExpandIcons_();
				}
			}
		}

		/// <summary>
		/// Whether to show expand icons.
		/// </summary>
		/// <returns>Whether to show expand icons.</returns>
		public bool getShowExpandIcons()
		{
			return this.showExpandIcons_;
		}

		/// <summary>
		/// Sets whether to show the root node.
		/// </summary>
		/// <param name="b">Whether to show the root node.</param>
		public void setShowRootNode(bool b)
		{
			if (this.showRootNode_ != b) {
				this.showRootNode_ = b;
				if (this.isInDocument()) {
					var el = this.getRowElement();
					if (el != null) {
						el.ClassName = this.getRowClassName();
					}
				}
				// Ensure that we do not hide the selected item.
				if (!b && this.getSelectedItem() == this && this.getFirstChild() != null) {
					this.setSelectedItem(this.getFirstChild());
				}
			}
		}

		/// <summary>
		/// Whether to show the root node.
		/// </summary>
		/// <returns>Whether to show the root node.</returns>
		public bool getShowRootNode()
		{
			return this.showRootNode_;
		}

		/// <summary>
		/// Add roles and states.
		/// </summary>
		protected override void initAccessibility()
		{
			base.initAccessibility();

			var elt = this.getElement();
			goog.asserts.assert(elt != null, "The DOM element for the tree cannot be null.");
			goog.a11y.aria.setRole(elt, a11y.aria.Role.TREE);
			goog.a11y.aria.setState(elt, a11y.aria.State.LABELLEDBY, this.getLabelElement().Id);
		}

		public override void enterDocument()
		{
			base.enterDocument();
			var el = this.getElement();
			el.ClassName = this.getConfig().cssRoot;
			el.SetAttribute("hideFocus", "true");
			this.attachEvents_();
			this.initAccessibility();
		}

		public override void exitDocument()
		{
			base.exitDocument();
			this.detachEvents_();
		}

		/// <summary>
		/// Adds the event listeners to the tree.
		/// </summary>
		private void attachEvents_()
		{
			var el = this.getElement();
			el.TabIndex = 0;

			var kh = this.keyHandler_ = new goog.events.KeyHandler(el);
			var fh = this.focusHandler_ = new goog.events.FocusHandler(el);

			this.getHandler()
				.listen(fh, goog.events.FocusHandler.EventType.FOCUSOUT, new Action<goog.events.BrowserEvent>(this.handleBlur_))
				.listen(fh, goog.events.FocusHandler.EventType.FOCUSIN, new Action<goog.events.BrowserEvent>(this.handleFocus_))
				.listen(kh, goog.events.KeyHandler.EventType.KEY, new Func<goog.events.BrowserEvent, bool>(this.handleKeyEvent))
				.listen(el, goog.events.EventType.MOUSEDOWN, new Action<goog.events.BrowserEvent>(this.handleMouseEvent_))
				.listen(el, goog.events.EventType.CLICK, new Action<goog.events.BrowserEvent>(this.handleMouseEvent_))
				.listen(el, goog.events.EventType.DBLCLICK, new Action<goog.events.BrowserEvent>(this.handleMouseEvent_));
		}

		/// <summary>
		/// Removes the event listeners from the tree.
		/// </summary>
		private void detachEvents_()
		{
		}

		/// <summary>
		/// Handles mouse events.
		/// </summary>
		/// <param name="e">The browser event.</param>
		private void handleMouseEvent_(goog.events.BrowserEvent e)
		{
			goog.log.fine(this.logger_, "Received event " + e.type);
			var node = this.getNodeFromEvent_(e);
			if (node != null) {
				switch (e.type) {
				case goog.events.EventType.MOUSEDOWN:
					node.onMouseDown(e);
					break;
				case goog.events.EventType.CLICK:
					node.onClick_(e);
					break;
				case goog.events.EventType.DBLCLICK:
					node.onDoubleClick_(e);
					break;
				}
			}
		}

		/// <summary>
		/// Handles key down on the tree.
		/// </summary>
		/// <param name="e">The browser event.</param>
		/// <returns>The handled value.</returns>
		public bool handleKeyEvent(goog.events.BrowserEvent e)
		{
			var handled = false;

			// Handle typeahead and navigation keystrokes.
			handled = this.typeAhead_.handleNavigation(e) ||
				(this.selectedItem_ != null && this.selectedItem_.onKeyDown(e)) ||
				this.typeAhead_.handleTypeAheadChar(e);

			if (handled) {
				e.preventDefault();
			}

			return handled;
		}

		/// <summary>
		/// Finds the containing node given an event.
		/// </summary>
		/// <param name="e">The browser event.</param>
		/// <returns>The containing node or null if no node is</returns>
		protected BaseNode getNodeFromEvent_(goog.events.BrowserEvent e)
		{
			// find the right node
			var target = (HTMLElement)e.target;
			while (target != null) {
				var id = target.Id;
				if (id != null && goog.ui.tree.BaseNode.allNodes.TryGetValue(id, out var node)) {
					return node;
				}
				if (target == this.getElement()) {
					break;
				}
				target = (HTMLElement)target.ParentNode;
			}
			return null;
		}

		/// <summary>
		/// Creates a new tree node using the same config as the root.
		/// </summary>
		/// <param name="opt_content">The content of the node label. Strings are
		/// treated as plain-text and will be HTML escaped. To set SafeHtml content,
		/// omit opt_content and call setSafeHtml on the resulting node.</param>
		/// <returns>The new item.</returns>
		public virtual TreeNode createNode(string opt_content = null)
		{
			return new goog.ui.tree.TreeNode(opt_content != null ? new Union<string, SafeHtml>(opt_content) : goog.html.SafeHtml.EMPTY,
				this.getConfig(), this.getDomHelper());
		}

		/// <summary>
		/// Allows the caller to notify that the given node has been added or just had
		/// been updated in the tree.
		/// </summary>
		/// <param name="node">New node being added or existing node
		/// that just had been updated.</param>
		public void setNode(BaseNode node)
		{
			this.typeAhead_.setNodeInMap(node);
		}

		/// <summary>
		/// Allows the caller to notify that the given node is being removed from the
		/// tree.
		/// </summary>
		/// <param name="node">Node being removed.</param>
		public void removeNode(BaseNode node)
		{
			this.typeAhead_.removeNodeFromMap(node);
		}

		/// <summary>
		/// Clear the typeahead buffer.
		/// </summary>
		public void clearTypeAhead()
		{
			this.typeAhead_.clear();
		}

		/// <summary>
		/// A default configuration for the tree.
		/// </summary>
		public static readonly new object defaultConfig = goog.ui.tree.BaseNode.defaultConfig;
	}
}
