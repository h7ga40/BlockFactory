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
 * @fileoverview Definition of the goog.ui.tree.BaseNode class.
 *
 * @author arv@google.com (Erik Arvidsson)
 * @author eae@google.com (Emil A Eklund)
 *
 * This is a based on the webfx tree control. It since been updated to add
 * typeahead support, as well as accessibility support using ARIA framework.
 * See file comment in treecontrol.js.
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using goog.html;

namespace goog.ui.tree
{
	public abstract class BaseNode : Component
	{
		public class Config
		{
			public int indentWidth;
			public string cssRoot;
			public string cssHideRoot;
			public string cssItem;
			public string cssChildren;
			public string cssChildrenNoLines;
			public string cssExpandTreeIcon;
			public string cssTreeRow;
			public string cssItemLabel;
			public string cssTreeIcon;
			public string cssExpandTreeIconPlus;
			public string cssExpandTreeIconMinus;
			public string cssExpandTreeIconTPlus;
			public string cssExpandTreeIconTMinus;
			public string cssExpandTreeIconLPlus;
			public string cssExpandTreeIconLMinus;
			public string cssExpandTreeIconT;
			public string cssExpandTreeIconL;
			public string cssExpandTreeIconBlank;
			public string cssExpandedFolderIcon;
			public string cssCollapsedFolderIcon;
			public string cssFileIcon;
			public string cssExpandedRootIcon;
			public string cssCollapsedRootIcon;
			public string cssSelectedRow;
			public string cleardotPath;
		}

		public new class EventType
		{
			public const string BEFORE_EXPAND = "beforeexpand";
			public const string EXPAND = "expand";
			public const string BEFORE_COLLAPSE = "beforecollapse";
			public const string COLLAPSE = "collapse";
		}

		protected TreeControl tree;
		internal bool isUserCollapsible_;
		private Config config_;
		private bool expanded_;
		protected BaseNode nextSibling_;
		protected BaseNode previousSibling_;
		protected BaseNode firstChild_;
		protected BaseNode lastChild_;
		private int depth_;
		private string toolTip_;
		private html.SafeHtml html_;
		private html.SafeHtml afterLabelHtml_;
		private bool selected_;
		private string expandedIconClass_;
		private string iconClass_;

		public BaseNode(Union<string, SafeHtml> content, Config opt_config = null,
			goog.dom.DomHelper opt_domHelper = null)
			: base(opt_domHelper)
		{
			this.config_ = opt_config ?? goog.ui.tree.BaseNode.defaultConfig;
			this.html_ = goog.html.SafeHtml.htmlEscapePreservingNewlines(content);
			this.afterLabelHtml_ = goog.html.SafeHtml.EMPTY;
			this.isUserCollapsible_ = true;
			this.depth_ = -1;
		}


		/// <summary>
		/// Map of nodes in existence. Needed to route events to the appropriate nodes.
		/// Nodes are added to the map at {@link #enterDocument} time and removed at
		/// </summary>
		/// {@link #exitDocument} time.
		protected static Dictionary<string, BaseNode> allNodes = new Dictionary<string, BaseNode>();

		public override void disposeInternal()
		{
			base.disposeInternal();
			if (this.tree != null) {
				this.tree.removeNode(this);
				this.tree = null;
			}
			this.setElementInternal(null);
		}

		/// <summary>
		/// Adds roles and states.
		/// </summary>
		protected virtual void initAccessibility()
		{
			var el = this.getElement();
			if (el != null) {
				// Set an id for the label
				var label = this.getLabelElement();
				if (label != null && String.IsNullOrEmpty(label.Id)) {
					label.Id = this.getId() + ".label";
				}

				goog.a11y.aria.setRole(el, goog.a11y.aria.Role.TREEITEM);
				goog.a11y.aria.setState(el, goog.a11y.aria.State.SELECTED, false);
				goog.a11y.aria.setState(el, goog.a11y.aria.State.LEVEL, this.getDepth());
				if (label != null) {
					goog.a11y.aria.setState(el, goog.a11y.aria.State.LABELLEDBY, label.Id);
				}

				var img = this.getIconElement();
				if (img != null) {
					goog.a11y.aria.setRole(img, a11y.aria.Role.PRESENTATION);
				}
				var ei = this.getExpandIconElement();
				if (ei != null) {
					goog.a11y.aria.setRole(ei, a11y.aria.Role.PRESENTATION);
				}

				var ce = this.getChildrenElement();
				if (ce != null) {
					goog.a11y.aria.setRole(ce, a11y.aria.Role.GROUP);

					// In case the children will be created lazily.
					if (ce.HasChildNodes()) {
						// Only set aria-expanded if the node has children (can be expanded).
						goog.a11y.aria.setState(el, goog.a11y.aria.State.EXPANDED, false);

						// do setsize for each child
						var count = this.getChildCount();
						for (var i = 1; i <= count; i++) {
							var child = this.getChildAt(i - 1).getElement();
							goog.asserts.assert(child != null, "The child element cannot be null");
							goog.a11y.aria.setState(child, a11y.aria.State.SETSIZE, count);
							goog.a11y.aria.setState(child, a11y.aria.State.POSINSET, i);
						}
					}
				}
			}
		}

		public override void createDom()
		{
			var element = (HTMLElement)this.getDomHelper().safeHtmlToNode(this.toSafeHtml());
			this.setElementInternal(element);
		}

		public override void enterDocument()
		{
			base.enterDocument();
			goog.ui.tree.BaseNode.allNodes[this.getId()] = this;
			this.initAccessibility();
		}

		public override void exitDocument()
		{
			base.exitDocument();
			goog.ui.tree.BaseNode.allNodes.Remove(this.getId());
		}

		/// <summary>
		/// The method assumes that the child doesn't have parent node yet.
		/// The {@code opt_render} argument is not used. If the parent node is expanded,
		/// the child node's state will be the same as the parent's. Otherwise the
		/// child's DOM tree won't be created.
		/// </summary>
		public override void addChildAt(Component child_, int index, bool opt_render = false)
		{
			goog.asserts.assert(child_.getParent() == null);
			goog.asserts.assertInstanceof(child_, typeof(goog.ui.tree.BaseNode));
			var child = (goog.ui.tree.BaseNode)child_;
			var prevNode = (goog.ui.tree.BaseNode)this.getChildAt(index - 1);
			var nextNode = (goog.ui.tree.BaseNode)this.getChildAt(index);

			base.addChildAt(child, index, opt_render);

			child.previousSibling_ = prevNode;
			child.nextSibling_ = nextNode;

			if (prevNode != null) {
				prevNode.nextSibling_ = child;
			}
			else {
				this.firstChild_ = child;
			}
			if (nextNode != null) {
				nextNode.previousSibling_ = child;
			}
			else {
				this.lastChild_ = child;
			}

			var tree = this.getTree();
			if (tree != null) {
				child.setTreeInternal(tree);
			}

			child.setDepth_(this.getDepth() + 1);

			var el = this.getElement();
			if (el != null) {
				this.updateExpandIcon();
				goog.a11y.aria.setState(
					el, goog.a11y.aria.State.EXPANDED, this.getExpanded());
				if (this.getExpanded()) {
					var childrenEl = this.getChildrenElement();
					if (child.getElement() == null) {
						child.createDom();
					}
					var childElement = child.getElement();
					var nextElement = nextNode != null ? nextNode.getElement() : null;
					childrenEl.InsertBefore(childElement, nextElement);

					if (this.isInDocument()) {
						child.enterDocument();
					}

					if (nextNode == null) {
						if (prevNode != null) {
							prevNode.updateExpandIcon();
						}
						else {
							goog.style.setElementShown(childrenEl, true);
							this.setExpanded(this.getExpanded());
						}
					}
				}
			}
		}

		/// <summary>
		/// Adds a node as a child to the current node.
		/// </summary>
		/// <param name="child">The child to add.</param>
		/// <param name="opt_before">If specified, the new child is</param>
		///    added as a child before this one. If not specified, it's appended to the
		///    end.
		/// <returns>The added child.</returns>
		public BaseNode add(BaseNode child, BaseNode opt_before = null)
		{
			goog.asserts.assert(
				opt_before == null || opt_before.getParent() == this,
				"Can only add nodes before siblings");
			if (child.getParent() != null) {
				child.getParent().removeChild(child);
			}
			this.addChildAt(
				child, opt_before != null ? this.indexOfChild(opt_before) : this.getChildCount());
			return child;
		}

		/// <summary>
		/// Removes a child. The caller is responsible for disposing the node.
		/// </summary>
		/// <param name="childNode">The child to remove. Must be a</param>
		///     {@link goog.ui.tree.BaseNode}.
		/// <param name="opt_unrender">Unused. The child will always be unrendered.</param>
		/// <returns>The child that was removed.</returns>
		public override Component removeChild(
			Union<string, Component> childNode, bool opt_unrender = false)
		{
			// In reality, this only accepts BaseNodes.
			var child = (goog.ui.tree.BaseNode)(childNode);

			// if we remove selected or tree with the selected we should select this
			var tree = this.getTree();
			var selectedNode = tree != null ? tree.getSelectedItem() : null;
			if (selectedNode == child || child.contains(selectedNode)) {
				if (tree.hasFocus()) {
					this.select();
					goog.Timer.callOnce(this.onTimeoutSelect_, 10, this);
				}
				else {
					this.select();
				}
			}

			base.removeChild(child);

			if (this.lastChild_ == child) {
				this.lastChild_ = child.previousSibling_;
			}
			if (this.firstChild_ == child) {
				this.firstChild_ = child.nextSibling_;
			}
			if (child.previousSibling_ != null) {
				child.previousSibling_.nextSibling_ = child.nextSibling_;
			}
			if (child.nextSibling_ != null) {
				child.nextSibling_.previousSibling_ = child.previousSibling_;
			}

			var wasLast = child.isLastSibling();

			child.tree = null;
			child.depth_ = -1;

			if (tree != null) {
				// Tell the tree control that the child node is now removed.
				tree.removeNode(child);

				if (this.isInDocument()) {
					var childrenEl = this.getChildrenElement();

					if (child.isInDocument()) {
						var childEl = child.getElement();
						childrenEl.RemoveChild(childEl);

						child.exitDocument();
					}

					if (wasLast) {
						var newLast = this.getLastChild();
						if (newLast != null) {
							newLast.updateExpandIcon();
						}
					}
					if (!this.hasChildren()) {
						childrenEl.Style.Display = Display.None;
						this.updateExpandIcon();
						this.updateIcon_();

						var el = this.getElement();
						if (el != null) {
							goog.a11y.aria.removeState(el, goog.a11y.aria.State.EXPANDED);
						}
					}
				}
			}

			return child;
		}

		/// <summary>
		/// </summary>
		/// @deprecated Use {@link #removeChild}.
		public goog.ui.Component remove(Union<string, goog.ui.Component> child, bool opt_unrender = false)
		{
			return base.removeChild(child, opt_unrender);
		}

		/// <summary>
		/// Handler for setting focus asynchronously.
		/// </summary>
		private void onTimeoutSelect_()
		{
			this.select();
		}

		/// <summary>
		/// Returns the tree.
		/// </summary>
		public abstract goog.ui.tree.TreeControl getTree();

		/// <summary>
		/// Returns the depth of the node in the tree. Should not be overridden.
		/// </summary>
		/// <returns>The non-negative depth of this node (the root is zero).</returns>
		public virtual int getDepth()
		{
			var depth = this.depth_;
			if (depth < 0) {
				depth = this.computeDepth_();
				this.setDepth_(depth);
			}
			return depth;
		}

		/// <summary>
		/// Computes the depth of the node in the tree.
		/// Called only by getDepth, when the depth hasn't already been cached.
		/// </summary>
		/// <returns>The non-negative depth of this node (the root is zero).</returns>
		private int computeDepth_()
		{
			var parent = this.getParent();
			if (parent != null) {
				return ((BaseNode)parent).getDepth() + 1;
			}
			else {
				return 0;
			}
		}

		/// <summary>
		/// Changes the depth of a node (and all its descendants).
		/// </summary>
		/// <param name="depth">The new nesting depth; must be non-negative.</param>
		private void setDepth_(int depth)
		{
			if (depth != this.depth_) {
				this.depth_ = depth;
				var row = this.getRowElement();
				if (row != null) {
					var indent = this.getPixelIndent_() + "px";
					if (this.isRightToLeft()) {
						row.Style.PaddingRight = indent;
					}
					else {
						row.Style.PaddingLeft = indent;
					}
				}
				this.forEachChild((child) => { ((BaseNode)child).setDepth_(depth + 1); });
			}
		}

		/// <summary>
		/// Returns true if the node is a descendant of this node
		/// </summary>
		/// <param name="node">The node to check.</param>
		/// <returns>True if the node is a descendant of this node, false
		///     otherwise.</returns>
		private bool contains(Component node)
		{
			var current = node;
			while (current != null) {
				if (current == this) {
					return true;
				}
				current = current.getParent();
			}
			return false;
		}


		/// <summary>
		/// Returns the children of this node.
		/// </summary>
		/// <returns>The children.</returns>
		public JsArray<BaseNode> getChildren()
		{
			var children = new JsArray<BaseNode>();
			this.forEachChild((child) => { children.Push((BaseNode)child); });
			return children;
		}

		/// <summary>
		/// The first child of this node.
		/// </summary>
		/// <returns>The first child of this node.</returns>
		public BaseNode getFirstChild()
		{
			return (BaseNode)this.getChildAt(0);
		}

		/// <summary>
		/// </summary>
		/// <returns>The last child of this node.</returns>
		public BaseNode getLastChild()
		{
			return (BaseNode)this.getChildAt(this.getChildCount() - 1);
		}

		/// <summary>
		/// </summary>
		/// <returns>The previous sibling of this node.</returns>
		public BaseNode getPreviousSibling()
		{
			return this.previousSibling_;
		}

		/// <summary>
		/// </summary>
		/// <returns>The next sibling of this node.</returns>
		public BaseNode getNextSibling()
		{
			return this.nextSibling_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the node is the last sibling.</returns>
		public bool isLastSibling()
		{
			return this.nextSibling_ == null;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the node is selected.</returns>
		public bool isSelected()
		{
			return this.selected_;
		}

		/// <summary>
		/// Selects the node.
		/// </summary>
		public void select()
		{
			var tree = this.getTree();
			if (tree != null) {
				tree.setSelectedItem(this);
			}
		}

		/// <summary>
		/// Originally it was intended to deselect the node but never worked.
		/// </summary>
		/// @deprecated Use {@code tree.setSelectedItem(null)}.
		public void deselect()
		{
		}

		/// <summary>
		/// Called from the tree to instruct the node change its selection state.
		/// </summary>
		/// <param name="selected">The new selection state.</param>
		public void setSelectedInternal(bool selected)
		{
			if (this.selected_ == selected) {
				return;
			}
			this.selected_ = selected;

			this.updateRow();

			var el = this.getElement();
			if (el != null) {
				goog.a11y.aria.setState(el, goog.a11y.aria.State.SELECTED, selected);
				if (selected) {
					var treeElement = this.getTree().getElement();
					goog.asserts.assert(
						treeElement != null, "The DOM element for the tree cannot be null");
					goog.a11y.aria.setState(treeElement, goog.a11y.aria.State.ACTIVEDESCENDANT, this.getId());
				}
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the node is expanded.</returns>
		public virtual bool getExpanded()
		{
			return this.expanded_;
		}

		/// <summary>
		/// Sets the node to be expanded internally, without state change events.
		/// </summary>
		/// <param name="expanded">Whether to expand or close the node.</param>
		public void setExpandedInternal(bool expanded)
		{
			this.expanded_ = expanded;
		}

		/// <summary>
		/// Sets the node to be expanded.
		/// </summary>
		/// <param name="expanded">Whether to expand or close the node.</param>
		public virtual void setExpanded(bool expanded)
		{
			var isStateChange = expanded != this.expanded_;
			if (isStateChange) {
				// Only fire events if the expanded state has actually changed.
				var prevented = !this.dispatchEvent(
					expanded ? goog.ui.tree.BaseNode.EventType.BEFORE_EXPAND :
							   goog.ui.tree.BaseNode.EventType.BEFORE_COLLAPSE);
				if (prevented) return;
			}
			HTMLElement ce;
			this.expanded_ = expanded;
			var tree = this.getTree();
			var el = this.getElement();

			if (this.hasChildren()) {
				if (!expanded && tree != null && this.contains(tree.getSelectedItem())) {
					this.select();
				}

				if (el != null) {
					ce = this.getChildrenElement();
					if (ce != null) {
						goog.style.setElementShown(ce, expanded);
						goog.a11y.aria.setState(el, goog.a11y.aria.State.EXPANDED, expanded);

						// Make sure we have the HTML for the children here.
						if (expanded && this.isInDocument() && !ce.HasChildNodes()) {
							var children = new JsArray<goog.html.SafeHtml>();
							this.forEachChild((child) => {
								children.Push(((BaseNode)child).toSafeHtml());
							});
							goog.dom.safe.setInnerHtml(ce, goog.html.SafeHtml.concat(children));
							this.forEachChild((child) => { child.enterDocument(); });
						}
					}
					this.updateExpandIcon();
				}
			}
			else {
				ce = this.getChildrenElement();
				if (ce != null) {
					goog.style.setElementShown(ce, false);
				}
			}
			if (el != null) {
				this.updateIcon_();
			}

			if (isStateChange) {
				this.dispatchEvent(
					expanded ? goog.ui.tree.BaseNode.EventType.EXPAND :
							   goog.ui.tree.BaseNode.EventType.COLLAPSE);
			}
		}

		/// <summary>
		/// Toggles the expanded state of the node.
		/// </summary>
		public void toggle()
		{
			this.setExpanded(!this.getExpanded());
		}

		/// <summary>
		/// Expands the node.
		/// </summary>
		public void expand()
		{
			this.setExpanded(true);
		}

		/// <summary>
		/// Collapses the node.
		/// </summary>
		public void collapse()
		{
			this.setExpanded(false);
		}

		/// <summary>
		/// Collapses the children of the node.
		/// </summary>
		public void collapseChildren()
		{
			this.forEachChild((child) => { ((BaseNode)child).collapseAll(); });
		}

		/// <summary>
		/// Collapses the children and the node.
		/// </summary>
		public void collapseAll()
		{
			this.collapseChildren();
			this.collapse();
		}

		/// <summary>
		/// Expands the children of the node.
		/// </summary>
		public void expandChildren()
		{
			this.forEachChild((child) => { ((BaseNode)child).expandAll(); });
		}

		/// <summary>
		/// Expands the children and the node.
		/// </summary>
		public void expandAll()
		{
			this.expandChildren();
			this.expand();
		}

		/// <summary>
		/// Expands the parent chain of this node so that it is visible.
		/// </summary>
		public virtual void reveal()
		{
			var parent = this.getParent();
			if (parent != null) {
				((BaseNode)parent).setExpanded(true);
				((BaseNode)parent).reveal();
			}
		}

		/// <summary>
		/// Sets whether the node will allow the user to collapse it.
		/// </summary>
		/// <param name="isCollapsible">Whether to allow node collapse.</param>
		public void setIsUserCollapsible(bool isCollapsible)
		{
			this.isUserCollapsible_ = isCollapsible;
			if (!this.isUserCollapsible_) {
				this.expand();
			}
			if (this.getElement() != null) {
				this.updateExpandIcon();
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the node is collapsible by user actions.</returns>
		public bool isUserCollapsible()
		{
			return this.isUserCollapsible_;
		}

		/// <summary>
		/// Creates HTML for the node.
		/// </summary>
		public html.SafeHtml toSafeHtml()
		{
			var tree = this.getTree();
			var hideLines = !tree.getShowLines() ||
				tree == this.getParent() && !tree.getShowRootLines();

			var childClass =
				hideLines ? this.config_.cssChildrenNoLines : this.config_.cssChildren;

			var nonEmptyAndExpanded = this.getExpanded() && this.hasChildren();

			var attributes = new Dictionary<string, object>() {
				{ "class", childClass },
				{ "style", this.getLineStyle() }
			};

			var content = new JsArray<goog.html.SafeHtml>();
			if (nonEmptyAndExpanded) {
				// children
				this.forEachChild((child) => { content.Push(((BaseNode)child).toSafeHtml()); });
			}

			var children = goog.html.SafeHtml.create("div", attributes, content);

			attributes = new Dictionary<string, object>() {
				{ "class", this.config_.cssItem },
				{ "id", this.getId() }
			};
			return goog.html.SafeHtml.create("div", attributes, new html.SafeHtml[] { this.getRowSafeHtml(), children });
		}

		/// <summary>
		/// </summary>
		/// <returns>The pixel indent of the row.</returns>
		private int getPixelIndent_()
		{
			return System.Math.Max(0, (this.getDepth() - 1) * this.config_.indentWidth);
		}

		/// <summary>
		/// </summary>
		/// <returns>The html for the row.</returns>
		public html.SafeHtml getRowSafeHtml()
		{
			var style = new Dictionary<string, object>();
			style["padding-" + (this.isRightToLeft() ? "right" : "left")] =
				this.getPixelIndent_() + "px";
			var attributes = new Dictionary<string, object>(){
				{ "class", this.getRowClassName() },
				{ "style", style }
			};
			var content = new html.SafeHtml[] {
				this.getExpandIconSafeHtml(), this.getIconSafeHtml(),
				this.getLabelSafeHtml()
			};
			return goog.html.SafeHtml.create("div", attributes, content);
		}

		/// <summary>
		/// </summary>
		/// <returns>The class name for the row.</returns>
		public virtual string getRowClassName()
		{
			string selectedClass;
			if (this.isSelected()) {
				selectedClass = " " + this.config_.cssSelectedRow;
			}
			else {
				selectedClass = "";
			}
			return this.config_.cssTreeRow + selectedClass;
		}

		/// <summary>
		/// </summary>
		/// <returns>The html for the label.</returns>
		public html.SafeHtml getLabelSafeHtml()
		{
			var attributes = new Dictionary<string, object>() {
				{ "title", this.getToolTip() ?? null },
				{ "class", this.config_.cssItemLabel }
			};
			var html = goog.html.SafeHtml.create(
				"span", attributes, this.getSafeHtml());
			return goog.html.SafeHtml.concat(
				html,
				goog.html.SafeHtml.create("span", new Dictionary<string, object>(), this.getAfterLabelSafeHtml()));
		}

		/// <summary>
		/// Returns the html that appears after the label. This is useful if you want to
		/// put extra UI on the row of the label but not inside the anchor tag.
		/// </summary>
		/// <returns>The html.
		///  @final</returns>
		public string getAfterLabelHtml()
		{
			return goog.html.SafeHtml.unwrap(this.getAfterLabelSafeHtml());
		}

		/// <summary>
		/// Returns the html that appears after the label. This is useful if you want to
		/// put extra UI on the row of the label but not inside the anchor tag.
		/// </summary>
		/// <returns>The html.</returns>
		private goog.html.SafeHtml getAfterLabelSafeHtml()
		{
			return this.afterLabelHtml_;
		}

		/// <summary>
		/// Sets the html that appears after the label. This is useful if you want to
		/// put extra UI on the row of the label but not inside the anchor tag.
		/// </summary>
		/// <param name="html">The html.</param>
		public void setAfterLabelSafeHtml(goog.html.SafeHtml html)
		{
			this.afterLabelHtml_ = html;
			var el = this.getAfterLabelElement();
			if (el != null) {
				goog.dom.safe.setInnerHtml(el, html);
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>The html for the icon.</returns>
		public html.SafeHtml getIconSafeHtml()
		{
			var attributes = new Dictionary<string, object> {
				{ "class", this.getCalculatedIconClass() },
				{ "style", new Dictionary<string, object> { { "display", "inline-block" } } }
			};
			return goog.html.SafeHtml.create("span", attributes);
		}

		/// <summary>
		/// Gets the calculated icon class.
		/// </summary>
		public abstract string getCalculatedIconClass();

		/// <summary>
		/// </summary>
		/// <returns>The source for the icon.</returns>
		public virtual html.SafeHtml getExpandIconSafeHtml()
		{
			var attributes = new Dictionary<string, object>(){
				{ "class", this.getExpandIconClass() },
				{ "type", "expand" },
				{ "style", new Dictionary<string, object> { { "display", "inline-block" } } }
			};
			return goog.html.SafeHtml.create("span", attributes);
		}

		/// <summary>
		/// </summary>
		/// <returns>The class names of the icon used for expanding the node.</returns>
		public string getExpandIconClass()
		{
			var tree = this.getTree();
			var hideLines = !tree.getShowLines() ||
				tree == this.getParent() && !tree.getShowRootLines();

			var config = this.config_;
			var sb = new System.Text.StringBuilder();
			sb.Append(config.cssTreeIcon + " " + config.cssExpandTreeIcon + " ");

			if (this.hasChildren()) {
				var bits = 0;
				/*
				  Bitmap used to determine which icon to use
				  1  Plus
				  2  Minus
				  4  T Line
				  8  L Line
				*/

				if (tree.getShowExpandIcons() && this.isUserCollapsible_) {
					if (this.getExpanded()) {
						bits = 2;
					}
					else {
						bits = 1;
					}
				}

				if (!hideLines) {
					if (this.isLastSibling()) {
						bits += 4;
					}
					else {
						bits += 8;
					}
				}

				switch (bits) {
				case 1:
					sb.Append(config.cssExpandTreeIconPlus);
					break;
				case 2:
					sb.Append(config.cssExpandTreeIconMinus);
					break;
				case 4:
					sb.Append(config.cssExpandTreeIconL);
					break;
				case 5:
					sb.Append(config.cssExpandTreeIconLPlus);
					break;
				case 6:
					sb.Append(config.cssExpandTreeIconLMinus);
					break;
				case 8:
					sb.Append(config.cssExpandTreeIconT);
					break;
				case 9:
					sb.Append(config.cssExpandTreeIconTPlus);
					break;
				case 10:
					sb.Append(config.cssExpandTreeIconTMinus);
					break;
				default:  // 0
					sb.Append(config.cssExpandTreeIconBlank);
					break;
				}
			}
			else {
				if (hideLines) {
					sb.Append(config.cssExpandTreeIconBlank);
				}
				else if (this.isLastSibling()) {
					sb.Append(config.cssExpandTreeIconL);
				}
				else {
					sb.Append(config.cssExpandTreeIconT);
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// </summary>
		/// <returns>The line style.</returns>
		public goog.html.SafeStyle getLineStyle()
		{
			var nonEmptyAndExpanded = this.getExpanded() && this.hasChildren();
			return goog.html.SafeStyle.create(new Dictionary<string, object>() {
						{ "background-position", this.getBackgroundPosition() },
						{ "display", nonEmptyAndExpanded ? null : "none" }
					});
		}

		/// <summary>
		/// </summary>
		/// <returns>The background position style value.</returns>
		public string getBackgroundPosition()
		{
			return this.isLastSibling() ? "-100" : ((this.getDepth() - 1) *
				this.config_.indentWidth).ToString() + "px 0";
		}

		/// <summary>
		/// </summary>
		/// <returns>The element for the tree node.</returns>
		public override HTMLElement getElement()
		{
			var el = base.getElement();
			if (el == null) {
				el = (HTMLElement)this.getDomHelper().getElement(this.getId());
				this.setElementInternal(el);
			}
			return el;
		}

		/// <summary>
		/// </summary>
		/// <returns>The row is the div that is used to draw the node without
		///      the children.</returns>
		public HTMLElement getRowElement()
		{
			var el = this.getElement();
			return el != null ? (HTMLElement)el.FirstChild : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>The expanded icon element.</returns>
		public virtual HTMLElement getExpandIconElement()
		{
			var el = this.getRowElement();
			return el != null ? (HTMLElement)el.FirstChild : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>The icon element.</returns>
		public virtual HTMLElement getIconElement()
		{
			var el = this.getRowElement();
			return el != null ? (HTMLElement)el.ChildNodes[1] : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>The label element.</returns>
		public HTMLElement getLabelElement()
		{
			var el = this.getRowElement();
			// TODO: find/fix race condition that requires us to add
			// the lastChild check
			return el != null && el.LastChild != null ?
				(HTMLElement)el.LastChild.PreviousSibling : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>The element after the label.</returns>
		public HTMLElement getAfterLabelElement()
		{
			var el = this.getRowElement();
			return el != null ? (HTMLElement)el.LastChild : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>The div containing the children.</returns>
		public HTMLElement getChildrenElement()
		{
			var el = this.getElement();
			return el != null ? (HTMLElement)el.LastChild : null;
		}

		/// <summary>
		/// </summary>
		/// Sets the icon class for the node.
		/// <param name="s">The icon class.</param>
		public void setIconClass(string s)
		{
			this.iconClass_ = s;
			if (this.isInDocument()) {
				this.updateIcon_();
			}
		}

		/// <summary>
		/// </summary>
		/// Gets the icon class for the node.
		/// <returns>s The icon source.</returns>
		public string getIconClass()
		{
			return this.iconClass_;
		}

		/// <summary>
		/// </summary>
		/// Sets the icon class for when the node is expanded.
		/// <param name="s">The expanded icon class.</param>
		public void setExpandedIconClass(string s)
		{
			this.expandedIconClass_ = s;
			if (this.isInDocument()) {
				this.updateIcon_();
			}
		}

		/// <summary>
		/// </summary>
		/// Gets the icon class for when the node is expanded.
		/// <returns>The class.</returns>
		public string getExpandedIconClass()
		{
			return this.expandedIconClass_;
		}

		/// <summary>
		/// </summary>
		/// Sets the text of the label.
		/// <param name="s">The plain text of the label.</param>
		public void setText(string s)
		{
			this.setSafeHtml(goog.html.SafeHtml.htmlEscape(s));
		}

		/// <summary>
		/// Returns the text of the label. If the text was originally set as HTML, the
		/// return value is unspecified.
		/// </summary>
		/// <returns>The plain text of the label.</returns>
		internal string getText()
		{
			return goog.@string.unescapeEntities(goog.html.SafeHtml.unwrap(this.html_));
		}

		/// <summary>
		/// Sets the HTML of the label.
		/// </summary>
		/// <param name="html">The HTML object for the label.</param>
		public void setSafeHtml(SafeHtml html)
		{
			this.html_ = html;
			var el = this.getLabelElement();
			if (el != null) {
				goog.dom.safe.setInnerHtml(el, html);
			}
			var tree = this.getTree();
			if (tree != null) {
				// Tell the tree control about the updated label text.
				tree.setNode(this);
			}
		}


		/// <summary>
		/// Returns the html of the label.
		/// </summary>
		/// <returns>The html string of the label.</returns>
		public string getHtml()
		{
			return goog.html.SafeHtml.unwrap(this.getSafeHtml());
		}

		/// <summary>
		/// Returns the html of the label.
		/// </summary>
		/// <returns>The html string of the label.</returns>
		public html.SafeHtml getSafeHtml()
		{
			return this.html_;
		}

		/// <summary>
		/// Sets the text of the tooltip.
		/// </summary>
		/// <param name="s">The tooltip text to set.</param>
		public void setToolTip(string s)
		{
			this.toolTip_ = s;
			var el = this.getLabelElement();
			if (el != null) {
				el.Title = s;
			}
		}

		/// <summary>
		/// Returns the text of the tooltip.
		/// </summary>
		/// <returns>The tooltip text.</returns>
		private string getToolTip()
		{
			return this.toolTip_;
		}

		/// <summary>
		/// Updates the row styles.
		/// </summary>
		public void updateRow()
		{
			var rowEl = this.getRowElement();
			if (rowEl != null) {
				rowEl.ClassName = this.getRowClassName();
			}
		}

		/// <summary>
		/// Updates the expand icon of the node.
		/// </summary>
		public virtual void updateExpandIcon()
		{
			var img = this.getExpandIconElement();
			if (img != null) {
				img.ClassName = this.getExpandIconClass();
			}
			var cel = this.getChildrenElement();
			if (cel != null) {
				cel.Style.BackgroundPosition = this.getBackgroundPosition();
			}
		}

		/// <summary>
		/// Updates the icon of the node. Assumes that this.getElement() is created.
		/// </summary>
		private void updateIcon_()
		{
			this.getIconElement().ClassName = this.getCalculatedIconClass();
		}

		/// <summary>
		/// Handles mouse down event.
		/// </summary>
		/// <param name="e">The browser event.</param>
		public virtual void onMouseDown(goog.events.BrowserEvent e)
		{
			var el = (HTMLElement)e.target;
			// expand icon
			var type = el.GetAttribute("type");
			if (type == "expand" && this.hasChildren()) {
				if (this.isUserCollapsible_) {
					this.toggle();
				}
				return;
			}

			this.select();
			this.updateRow();
		}

		/// <summary>
		/// Handles a click event.
		/// </summary>
		/// <param name="e">The browser event.</param>
		internal void onClick_(goog.events.BrowserEvent e)
		{
			e.preventDefault();
		}

		/// <summary>
		/// Handles a double click event.
		/// </summary>
		/// <param name="e">The browser event.</param>
		internal virtual void onDoubleClick_(goog.events.BrowserEvent e)
		{
			var el = (HTMLElement)e.target;
			// expand icon
			var type = el.GetAttribute("type");
			if (type == "expand" && this.hasChildren()) {
				return;
			}

			if (this.isUserCollapsible_) {
				this.toggle();
			}
		}

		/// <summary>
		/// Handles a key down event.
		/// </summary>
		/// <param name="e">The browser event.</param>
		/// <returns>The handled value.</returns>
		public virtual bool onKeyDown(goog.events.BrowserEvent e)
		{
			var handled = true;
			switch ((goog.events.KeyCodes)e.keyCode) {
			case goog.events.KeyCodes.RIGHT:
				if (e.altKey) {
					break;
				}
				if (this.hasChildren()) {
					if (!this.getExpanded()) {
						this.setExpanded(true);
					}
					else {
						this.getFirstChild().select();
					}
				}
				break;

			case goog.events.KeyCodes.LEFT:
				if (e.altKey) {
					break;
				}
				if (this.hasChildren() && this.getExpanded() && this.isUserCollapsible_) {
					this.setExpanded(false);
				}
				else {
					var parent = this.getParent();
					var tree = this.getTree();
					// don't go to root if hidden
					if (parent != null && (tree.getShowRootNode() || parent != tree)) {
						((BaseNode)parent).select();
					}
				}
				break;

			case goog.events.KeyCodes.DOWN:
				var nextNode = this.getNextShownNode();
				if (nextNode != null) {
					((BaseNode)nextNode).select();
				}
				break;

			case goog.events.KeyCodes.UP:
				var previousNode = this.getPreviousShownNode();
				if (previousNode != null) {
					previousNode.select();
				}
				break;

			default:
				handled = false;
				break;
			}

			if (handled) {
				e.preventDefault();
				var tree = this.getTree();
				if (tree != null) {
					// clear type ahead buffer as user navigates with arrow keys
					tree.clearTypeAhead();
				}
			}

			return handled;
		}

		/// <summary>
		/// </summary>
		/// <returns>The last shown descendant.</returns>
		public BaseNode getLastShownDescendant()
		{
			if (!this.getExpanded() || !this.hasChildren()) {
				return this;
			}
			// we know there is at least 1 child
			return this.getLastChild().getLastShownDescendant();
		}

		/// <summary>
		/// </summary>
		/// <returns>The next node to show or null if there isn"t
		///      a next node to show.</returns>
		public BaseNode getNextShownNode()
		{
			if (this.hasChildren() && this.getExpanded()) {
				return this.getFirstChild();
			}
			else {
				var parent = this;
				BaseNode next;
				while (parent != this.getTree()) {
					next = parent.getNextSibling();
					if (next != null) {
						return next;
					}
					parent = (BaseNode)parent.getParent();
				}
				return null;
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>The previous node to show.</returns>
		public BaseNode getPreviousShownNode()
		{
			var ps = this.getPreviousSibling();
			if (ps != null) {
				return ps.getLastShownDescendant();
			}
			var parent = this.getParent();
			var tree = this.getTree();
			if (!tree.getShowRootNode() && parent == tree) {
				return null;
			}
			// The root is the first node.
			if (this == tree) {
				return null;
			}
			return (BaseNode)parent;
		}

		/// <summary>
		/// </summary>
		/// <returns>Data set by the client.</returns>
		/// @deprecated Use {@link #getModel} instead.
		public virtual object getClientData()
		{
			return null;
		}


		/// <summary>
		/// Sets client data to associate with the node.
		/// </summary>
		/// <param name="data">The client data to associate with the node.</param>
		/// @deprecated Use {@link #setModel} instead.
		public virtual void setClientData(object data)
		{
		}


		/// <summary>
		/// </summary>
		/// <returns>The configuration for the tree.</returns>
		public Config getConfig()
		{
			return this.config_;
		}

		/// <summary>
		/// Internal method that is used to set the tree control on the node.
		/// </summary>
		/// <param name="tree">The tree control.</param>
		public void setTreeInternal(TreeControl tree)
		{
			if (this.tree != tree) {
				this.tree = tree;
				// Add new node to the type ahead node map.
				tree.setNode(this);
				this.forEachChild((child) => { ((BaseNode)child).setTreeInternal(tree); });
			}
		}

		/// <summary>
		/// A default configuration for the tree.
		/// </summary>
		public static readonly Config defaultConfig = defaultConfig = new Config {
			indentWidth = 19,
			cssRoot = goog.Css.getCssName("goog-tree-root") + " " +
				goog.Css.getCssName("goog-tree-item"),
			cssHideRoot = goog.Css.getCssName("goog-tree-hide-root"),
			cssItem = goog.Css.getCssName("goog-tree-item"),
			cssChildren = goog.Css.getCssName("goog-tree-children"),
			cssChildrenNoLines = goog.Css.getCssName("goog-tree-children-nolines"),
			cssTreeRow = goog.Css.getCssName("goog-tree-row"),
			cssItemLabel = goog.Css.getCssName("goog-tree-item-label"),
			cssTreeIcon = goog.Css.getCssName("goog-tree-icon"),
			cssExpandTreeIcon = goog.Css.getCssName("goog-tree-expand-icon"),
			cssExpandTreeIconPlus = goog.Css.getCssName("goog-tree-expand-icon-plus"),
			cssExpandTreeIconMinus = goog.Css.getCssName("goog-tree-expand-icon-minus"),
			cssExpandTreeIconTPlus = goog.Css.getCssName("goog-tree-expand-icon-tplus"),
			cssExpandTreeIconTMinus = goog.Css.getCssName("goog-tree-expand-icon-tminus"),
			cssExpandTreeIconLPlus = goog.Css.getCssName("goog-tree-expand-icon-lplus"),
			cssExpandTreeIconLMinus = goog.Css.getCssName("goog-tree-expand-icon-lminus"),
			cssExpandTreeIconT = goog.Css.getCssName("goog-tree-expand-icon-t"),
			cssExpandTreeIconL = goog.Css.getCssName("goog-tree-expand-icon-l"),
			cssExpandTreeIconBlank = goog.Css.getCssName("goog-tree-expand-icon-blank"),
			cssExpandedFolderIcon = goog.Css.getCssName("goog-tree-expanded-folder-icon"),
			cssCollapsedFolderIcon = goog.Css.getCssName("goog-tree-collapsed-folder-icon"),
			cssFileIcon = goog.Css.getCssName("goog-tree-file-icon"),
			cssExpandedRootIcon = goog.Css.getCssName("goog-tree-expanded-folder-icon"),
			cssCollapsedRootIcon = goog.Css.getCssName("goog-tree-collapsed-folder-icon"),
			cssSelectedRow = goog.Css.getCssName("selected")
		};
	}
}
