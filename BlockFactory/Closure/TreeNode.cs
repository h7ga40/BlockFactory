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
 * @fileoverview Definition of the goog.ui.tree.TreeNode class.
 *
 * @author arv@google.com (Erik Arvidsson)
 * @author eae@google.com (Emil A Eklund)
 *
 * This is a based on the webfx tree control. See file comment in
 * treecontrol.js.
 */

using System;
using Bridge;
using goog.html;

namespace goog.ui.tree
{
	public class TreeNode : BaseNode
	{
		/// <summary>
		/// A single node in the tree.
		/// </summary>
		/// <param name="content">The content of the node label.</param>
		///     Strings are treated as plain-text and will be HTML escaped.
		/// <param name="opt_config">The configuration for the tree. See</param>
		///    goog.ui.tree.TreeControl.defaultConfig. If not specified, a default config
		///    will be used.
		/// <param name="opt_domHelper">Optional DOM helper.</param>
		public TreeNode(Union<string, SafeHtml> content, Config opt_config = null, goog.dom.DomHelper opt_domHelper = null)
			: base(content, opt_config, opt_domHelper)
		{
		}

		/// <summary>
		/// Returns the tree.
		/// </summary>
		/// <returns>The tree.</returns>
		public override TreeControl getTree()
		{
			if (this.tree != null) {
				return this.tree;
			}
			var parent = this.getParent();
			if (parent != null) {
				var tree = ((BaseNode)parent).getTree();
				if (tree != null) {
					this.setTreeInternal(tree);
					return tree;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the source for the icon.
		/// </summary>
		/// <returns>Src for the icon.</returns>
		public override string getCalculatedIconClass()
		{
			var expanded = this.getExpanded();
			var expandedIconClass = this.getExpandedIconClass();
			if (expanded && !String.IsNullOrEmpty(expandedIconClass)) {
				return expandedIconClass;
			}
			var iconClass = this.getIconClass();
			if (!expanded && iconClass != null) {
				return iconClass;
			}

			// fall back on default icons
			var config = this.getConfig();
			if (this.hasChildren()) {
				if (expanded && config.cssExpandedFolderIcon != null) {
					return config.cssTreeIcon + " " + config.cssExpandedFolderIcon;
				}
				else if (!expanded && config.cssCollapsedFolderIcon != null) {
					return config.cssTreeIcon + " " + config.cssCollapsedFolderIcon;
				}
			}
			else {
				if (config.cssFileIcon != null) {
					return config.cssTreeIcon + " " + config.cssFileIcon;
				}
			}
			return "";
		}
	}
}
