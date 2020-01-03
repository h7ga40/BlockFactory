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
 * @fileoverview Provides the typeahead functionality for the tree class.
 *
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Linq;

namespace goog.ui.tree
{

	public class TypeAhead
	{
		/// <summary>
		/// Map of tree nodes to allow for quick access by characters in the label
		/// text.
		/// </summary>
		private Dictionary<string, JsArray<BaseNode>> nodeMap_;
		/// <summary>
		/// Buffer for storing typeahead characters.
		/// </summary>
		private string buffer_;
		/// <summary>
		/// Matching labels from the latest typeahead search.
		/// </summary>
		private string[] matchingLabels_;
		/// <summary>
		/// Matching nodes from the latest typeahead search. Used when more than
		/// one node is present with the same label text.
		/// </summary>
		private JsArray<BaseNode> matchingNodes_;
		/// <summary>
		/// Specifies the current index of the label from the latest typeahead search.
		/// </summary>
		private int matchingLabelIndex_;
		/// <summary>
		/// Specifies the index into matching nodes when more than one node is found
		/// with the same label.
		/// </summary>
		private int matchingNodeIndex_;

		public TypeAhead()
		{
			this.nodeMap_ = new Dictionary<string, JsArray<BaseNode>>();
			this.buffer_ = "";
			this.matchingLabels_ = null;
			this.matchingNodes_ = null;
			this.matchingLabelIndex_ = 0;
			this.matchingNodeIndex_ = 0;
		}

		public enum Offset
		{
			DOWN = 1,
			UP = -1
		}

		/// <summary>
		/// Handles navigation keys.
		/// </summary>
		/// <param name="e">The browser event.</param>
		/// <returns>The handled value.</returns>
		public bool handleNavigation(goog.events.BrowserEvent e)
		{
			var handled = false;

			switch ((goog.events.KeyCodes)e.keyCode) {
			// Handle ctrl+down, ctrl+up to navigate within typeahead results.
			case goog.events.KeyCodes.DOWN:
			case goog.events.KeyCodes.UP:
				if (e.ctrlKey) {
					this.jumpTo_(
						(goog.events.KeyCodes)e.keyCode == goog.events.KeyCodes.DOWN ?
							goog.ui.tree.TypeAhead.Offset.DOWN :
							goog.ui.tree.TypeAhead.Offset.UP);
					handled = true;
				}
				break;

			// Remove the last typeahead char.
			case goog.events.KeyCodes.BACKSPACE:
				var length = this.buffer_.Length - 1;
				handled = true;
				if (length > 0) {
					this.buffer_ = this.buffer_.Substring(0, length);
					this.jumpToLabel_(this.buffer_);
				}
				else if (length == 0) {
					// Clear the last character in typeahead.
					this.buffer_ = "";
				}
				else {
					handled = false;
				}
				break;

			// Clear typeahead buffer.
			case goog.events.KeyCodes.ESC:
				this.buffer_ = "";
				handled = true;
				break;
			}

			return handled;
		}

		/// <summary>
		/// Handles the character presses.
		/// </summary>
		/// <param name="e">The browser event.</param>
		/// <returns>The handled value.</returns>
		public bool handleTypeAheadChar(goog.events.BrowserEvent e)
		{
			var handled = false;

			if (!e.ctrlKey && !e.altKey) {
				// Since goog.structs.Trie.getKeys compares characters during
				// lookup, we should use charCode instead of keyCode where possible.
				// Convert to lowercase, typeahead is case insensitive.
				var ch = Char.ConvertFromUtf32(e.charCode != 0 ? e.charCode : e.keyCode).ToLowerCase();
				if (/*goog.string.isUnicode(ch) &&*/ (ch != " " || this.buffer_ != null)) {
					this.buffer_ += ch;
					handled = this.jumpToLabel_(this.buffer_);
				}
			}

			return handled;
		}

		/// <summary>
		/// Adds or updates the given node in the nodemap. The label text is used as a
		/// key and the node id is used as a value. In the case that the key already
		/// exists, such as when more than one node exists with the same label, then this
		/// function creates an array to hold the multiple nodes.
		/// </summary>
		/// <param name="node">Node to be added or updated.</param>
		public void setNodeInMap(goog.ui.tree.BaseNode node)
		{
			var labelText = node.getText();
			if (labelText != null &&
				!String.IsNullOrWhiteSpace(/*goog.string.makeSafe*/(labelText))) {
				// Typeahead is case insensitive, convert to lowercase.
				labelText = labelText.ToLowerCase();

				if (this.nodeMap_.TryGetValue(labelText, out var previousValue)) {
					// Found a previously created array, add the given node.
					previousValue.Push(node);
				}
				else {
					// Create a new array and set the array as value.
					var nodeList = new JsArray<BaseNode> { node };
					this.nodeMap_[labelText] = nodeList;
				}
			}
		}

		/// <summary>
		/// Removes the given node from the nodemap.
		/// </summary>
		/// <param name="node">Node to be removed.</param>
		public void removeNodeFromMap(goog.ui.tree.BaseNode node)
		{
			var labelText = node.getText();
			if (labelText != null &&
				!String.IsNullOrWhiteSpace(/*goog.string.makeSafe*/(labelText))) {
				labelText = labelText.ToLowerCase();

				if (this.nodeMap_.TryGetValue(labelText, out var nodeList)) {
					// Remove the node's descendants from the nodemap.
					var count = node.getChildCount();
					for (var i = 0; i < count; i++) {
						this.removeNodeFromMap((BaseNode)node.getChildAt(i));
					}
					// Remove the node from the array.
					nodeList.Remove(node);
					if (nodeList.Length == 0) {
						this.nodeMap_.Remove(labelText);
					}
				}
			}
		}

		/// <summary>
		/// Select the first matching node for the given typeahead.
		/// </summary>
		/// <param name="typeAhead">Typeahead characters to match.</param>
		/// <returns>True iff a node is found.</returns>
		private bool jumpToLabel_(string typeAhead)
		{
			var handled = false;
			var labels = this.nodeMap_.Keys.Where((i) => i == typeAhead).ToArray();

			// Make sure we have at least one matching label.
			if (labels != null && labels.Length > 0) {
				this.matchingNodeIndex_ = 0;
				this.matchingLabelIndex_ = 0;

				this.nodeMap_.TryGetValue(labels[0], out var nodes);
				if ((handled = this.selectMatchingNode_(nodes))) {
					this.matchingLabels_ = labels;
				}
			}

			// TODO(user): beep when no node is found
			return handled;
		}

		/// <summary>
		/// Select the next or previous node based on the offset.
		/// </summary>
		/// <param name="offset">DOWN or UP.</param>
		/// <returns>Whether a node is found.</returns>
		private bool jumpTo_(Offset offset)
		{
			var handled = false;
			var labels = this.matchingLabels_;

			if (labels != null) {
				JsArray<BaseNode> nodes = null;
				var nodeIndexOutOfRange = false;

				// Navigate within the nodes array.
				if (this.matchingNodes_ != null) {
					var newNodeIndex = this.matchingNodeIndex_ + (int)offset;
					if (newNodeIndex >= 0 && newNodeIndex < this.matchingNodes_.Length) {
						this.matchingNodeIndex_ = (int)newNodeIndex;
						nodes = this.matchingNodes_;
					}
					else {
						nodeIndexOutOfRange = true;
					}
				}

				// Navigate to the next or previous label.
				if (nodes == null) {
					var newLabelIndex = this.matchingLabelIndex_ + (int)offset;
					if (newLabelIndex >= 0 && newLabelIndex < labels.Length) {
						this.matchingLabelIndex_ = newLabelIndex;
					}

					if (labels.Length > this.matchingLabelIndex_) {
						this.nodeMap_.TryGetValue(labels[this.matchingLabelIndex_], out nodes);
					}

					// Handle the case where we are moving beyond the available nodes,
					// while going UP select the last item of multiple nodes with same label
					// and while going DOWN select the first item of next set of nodes
					if (nodes != null && nodes.Length > 0 && nodeIndexOutOfRange) {
						this.matchingNodeIndex_ =
							(offset == goog.ui.tree.TypeAhead.Offset.UP) ? nodes.Length - 1 : 0;
					}
				}

				if ((handled = this.selectMatchingNode_(nodes))) {
					this.matchingLabels_ = labels;
				}
			}

			// TODO(user): beep when no node is found
			return handled;
		}

		private BaseNode[] ToArray()
		{
			return matchingNodes_;
		}

		/// <summary>
		/// Given a nodes array reveals and selects the node while using node index.
		/// </summary>
		/// <param name="nodes">Nodes array to select</param>
		/// <returns>Whether a matching node was found.</returns>
		private bool selectMatchingNode_(JsArray<BaseNode> nodes)
		{
			BaseNode node = null;

			if (nodes != null) {
				// Find the matching node.
				if (this.matchingNodeIndex_ < nodes.Length) {
					node = nodes[this.matchingNodeIndex_];
					this.matchingNodes_ = nodes;
				}

				if (node != null) {
					node.reveal();
					node.select();
				}
			}

			return node != null;
		}

		/// <summary>
		/// Clears the typeahead buffer.
		/// </summary>
		public void clear()
		{
			this.buffer_ = "";
		}
	}
}
