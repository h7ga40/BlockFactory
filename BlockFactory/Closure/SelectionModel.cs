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
 * @fileoverview A palette control.  A palette is a grid that the user can
 * highlight or select via the keyboard or the mouse.
 *
 * @author attila@google.com (Attila Bodis)
 * @see ../demos/palette.html
 */
using System;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class SelectionModel : goog.events.EventTarget
	{
		/// <summary>
		/// Array of items controlled by the selection model.  If the items support
		/// the {@code setSelected(Boolean)} interface, they will be (de)selected
		/// as needed.
		/// </summary>
		JsArray<Node> items_ = new JsArray<Node>();


		/// <summary>
		/// Single-selection model.  Dispatches a {@link goog.events.EventType.SELECT}
		/// event when a selection is made.
		/// </summary>
		/// <param name="opt_items">Array of items; defaults to empty.</param>
		public SelectionModel(JsArray<Node> opt_items = null)
		{
			this.addItems(opt_items);
		}

		/// <summary>
		/// The currently selected item (null if none).
		/// </summary>
		private Node selectedItem_;

		/// <summary>
		/// Selection handler function.  Called with two arguments (the item to be
		/// selected or deselected, and a Boolean indicating whether the item is to
		/// be selected or deselected).
		/// </summary>
		private Action<Node, bool> selectionHandler_;

		/// <summary>
		/// Returns the selection handler function used by the selection model to change
		/// the internal selection state of items under its control.
		/// </summary>
		/// <returns></returns>
		public Action<Node, bool> getSelectionHandler()
		{
			return selectionHandler_;
		}

		/// <summary>
		/// Sets the selection handler function to be used by the selection model to
		/// change the internal selection state of items under its control.  The
		/// function must take two arguments:  an item and a Boolean to indicate whether
		/// the item is to be selected or deselected.  Selection handler functions are
		/// only needed if the items in the selection model don't natively support the
		/// {@code setSelected(Boolean)} interface.
		/// </summary>
		/// <param name="handler">Selection handler function.</param>
		public void setSelectionHandler(Action<Node, bool> handler)
		{
			this.selectionHandler_ = handler;
		}

		/// <summary>
		/// Returns the number of items controlled by the selection model.
		/// </summary>
		/// <returns>Number of items.</returns>
		public int getItemCount()
		{
			return this.items_.Length;
		}

		/// <summary>
		/// Returns the 0-based index of the given item within the selection model, or
		/// -1 if no such item is found.
		/// </summary>
		/// <param name="item">Item to look for.</param>
		/// <returns>Index of the given item (-1 if none).</returns>
		public int indexOfItem(Node item)
		{
			return item != null ? this.items_.IndexOf(item) : -1;
		}

		/// <summary>
		/// </summary>
		/// <returns>The first item, or undefined if there are no items
		/// in the model.</returns>
		public object getFirst()
		{
			return this.items_[0];
		}

		/// <summary>
		/// </summary>
		/// <returns>The last item, or undefined if there are no items
		/// in the model.</returns>
		public object getLast()
		{
			return this.items_[this.items_.Length - 1];
		}

		/// <summary>
		/// Returns the item at the given 0-based index.
		/// </summary>
		/// <param name="index">Index of the item to return.</param>
		/// <returns>Item at the given index (null if none).</returns>
		public Node getItemAt(int index)
		{
			return this.items_[index];
		}

		/// <summary>
		/// Bulk-adds items to the selection model.  This is more efficient than calling
		/// {@link #addItem} for each new item.
		/// </summary>
		/// <param name="items">New items to add.</param>
		public void addItems(JsArray<Node> items)
		{
			if (items != null) {
				// New items shouldn't be selected.
				foreach (var item in items) {
					this.selectItem_(item, false);
				}
				this.items_.PushRange(items);
			}
		}

		/// <summary>
		/// Adds an item at the end of the list.
		/// </summary>
		/// <param name="item">Item to add.</param>
		public void addItem(Node item)
		{
			this.addItemAt(item, this.getItemCount());
		}

		/// <summary>
		/// Adds an item at the given index.
		/// </summary>
		/// <param name="item">Item to add.</param>
		/// <param name="index">Index at which to add the new item.</param>
		public void addItemAt(Node item, int index)
		{
			if (item != null) {
				// New items must not be selected.
				this.selectItem_(item, false);
				this.items_.Splice(index, 0, item);
			}
		}

		/// <summary>
		/// Removes the given item (if it exists).  Dispatches a {@code SELECT} event if
		/// the removed item was the currently selected item.
		/// </summary>
		/// <param name="item">Item to remove.</param>
		public void removeItem(Node item)
		{
			if (item != null && this.items_.Remove(item)) {
				if (item == this.selectedItem_) {
					this.selectedItem_ = null;
					this.dispatchEvent(goog.events.EventType.SELECT);
				}
			}
		}

		/// <summary>
		/// Removes the item at the given index.
		/// </summary>
		/// <param name="index">Index of the item to remove.</param>
		public void removeItemAt(int index)
		{
			this.removeItem(this.getItemAt(index));
		}

		/// <summary>
		/// </summary>
		/// <returns>The currently selected item, or null if none.</returns>
		public Node getSelectedItem()
		{
			return this.selectedItem_;
		}

		public JsArray<Node> getItems()
		{
			return this.items_.Clone();
		}

		/// <summary>
		/// Selects the given item, deselecting any previously selected item, and
		/// dispatches a {@code SELECT} event.
		/// </summary>
		/// <param name="item">Item to select (null to clear the selection).</param>
		public void setSelectedItem(Node item)
		{
			if (item != this.selectedItem_) {
				this.selectItem_(this.selectedItem_, false);
				this.selectedItem_ = item;
				this.selectItem_(item, true);
			}

			// Always dispatch a SELECT event; let listeners decide what to do if the
			// selected item hasn't changed.
			this.dispatchEvent(goog.events.EventType.SELECT);
		}

		/// <summary>
		/// </summary>
		/// <returns>The 0-based index of the currently selected item, or -1
		/// if none.</returns>
		public int getSelectedIndex()
		{
			return this.indexOfItem(this.selectedItem_);
		}

		/// <summary>
		/// Selects the item at the given index, deselecting any previously selected
		/// item, and dispatches a {@code SELECT} event.
		/// </summary>
		/// <param name="index">Index to select (-1 to clear the selection).</param>
		public void setSelectedIndex(int index)
		{
			this.setSelectedItem(this.getItemAt(index));
		}

		/// <summary>
		/// Clears the selection model by removing all items from the selection.
		/// </summary>
		public void clear()
		{
			while (this.items_.Length > 0) {
				this.items_.Pop();
			}
			this.selectedItem_ = null;
		}

		public override void disposeInternal()
		{
			base.disposeInternal();
			while (this.items_.Length > 0) {
				this.items_.Pop();
			}
			this.items_ = null;
			this.selectedItem_ = null;
		}

		/// <summary>
		/// Private helper; selects or deselects the given item based on the value of
		/// the {@code select} argument.  If a selection handler has been registered
		/// (via {@link #setSelectionHandler}, calls it to update the internal selection
		/// state of the item.  Otherwise, attempts to call {@code setSelected(Boolean)}
		/// on the item itself, provided the object supports that interface.
		/// </summary>
		/// <param name="item">Item to select or deselect.</param>
		/// <param name="select">If true, the object will be selected; if false, it
		/// will be deselected.</param>
		private void selectItem_(Node item, bool select)
		{
			if (item != null) {
				if (this.selectionHandler_ != null) {
					// Use the registered selection handler function.
					this.selectionHandler_(item, select);
				}
				else if (item["setSelected"] is Action<bool>) {
					// Call setSelected() on the item, if it supports it.
					((Action<bool>)item["setSelected"])(select);
				}
			}
		}
	}
}
