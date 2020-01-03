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
	public class Palette : Control
	{
		/// <summary>
		/// A palette is a grid of DOM nodes that the user can highlight or select via
		/// the keyboard or the mouse.  The selection state of the palette is controlled
		/// an ACTION event.  Event listeners may retrieve the selected item using the
		/// {@link #getSelectedItem} or {@link #getSelectedIndex} method.
		///
		/// Use this class as the base for components like color palettes or emoticon
		/// pickers.  Use {@link #setContent} to set/change the items in the palette
		/// after construction.  See palette.html demo for example usage.
		/// </summary>
		/// <param name="items">Array of DOM nodes to be displayed as items
		/// in the palette grid (limited to one per cell).</param>
		/// <param name="opt_renderer">Renderer used to render or
		/// decorate the palette; defaults to {@link goog.ui.PaletteRenderer}.</param>
		/// <param name="opt_domHelper">Optional DOM helper, used for
		/// document interaction.</param>
		public Palette(JsArray<Node> items, PaletteRenderer opt_renderer = null,
			dom.DomHelper opt_domHelper = null)
			: base(new ControlContent(items),
				opt_renderer ?? goog.ui.PaletteRenderer.getInstance(), opt_domHelper)
		{
			this.setAutoStates(
				goog.ui.Component.State.CHECKED | goog.ui.Component.State.SELECTED |
					goog.ui.Component.State.OPENED,
				false);

			this.currentCellControl_ = new goog.ui.Palette.CurrentCell_();
			this.currentCellControl_.setParentEventTarget(this);

			this.lastHighlightedIndex_ = -1;
		}

		/// <summary>
		/// A fake component for dispatching events on palette cell changes.
		/// </summary>
		private CurrentCell_ currentCellControl_;

		/// <summary>
		/// The last highlighted index, or -1 if it never had one.
		/// </summary>
		private int lastHighlightedIndex_;

		/// <summary>
		/// Events fired by the palette object
		/// </summary>
		public static new class EventType
		{
			public static readonly string AFTER_HIGHLIGHT = goog.events.getUniqueId("afterhighlight");
		}

		/// <summary>
		/// Palette dimensions (columns x rows).  If the number of rows is undefined,
		/// it is calculated on first use.
		/// </summary>
		private goog.math.Size size_ = null;


		/// <summary>
		/// Index of the currently highlighted item (-1 if none).
		/// </summary>
		private int highlightedIndex_ = -1;

		/// <summary>
		/// Selection model controlling the palette's selection state.
		/// </summary>
		private goog.ui.SelectionModel selectionModel_ = null;

		// goog.ui.Component / goog.ui.Control implementation.

		public override void disposeInternal()
		{
			base.disposeInternal();

			if (this.selectionModel_ != null) {
				this.selectionModel_.dispose();
				this.selectionModel_ = null;
			}

			this.size_ = null;

			this.currentCellControl_.dispose();
		}

		/// <summary>
		/// Overrides {@link goog.ui.Control#setContentInternal} by also updating the
		/// grid size and the selection model.  Considered protected.
		/// </summary>
		/// <param name="content">Array of DOM nodes to be displayed
		/// as items in the palette grid (one item per cell).</param>
		public override void setContentInternal(goog.ui.ControlContent content)
		{
			var items = content.As<JsArray<Node>>();
			base.setContentInternal(new ControlContent(items));

			// Adjust the palette size.
			this.adjustSize_();

			// Add the items to the selection model, replacing previous items (if any).
			if (this.selectionModel_ != null) {
				// We already have a selection model; just replace the items.
				this.selectionModel_.clear();
				this.selectionModel_.addItems(items);
			}
			else {
				// Create a selection model, initialize the items, and hook up handlers.
				this.selectionModel_ = new goog.ui.SelectionModel(items);
				this.selectionModel_.setSelectionHandler(new Action<Node, bool>(this.selectItem_));
				this.getHandler().listen(
					this.selectionModel_, goog.events.EventType.SELECT,
					new Action<events.Event>(this.handleSelectionChange));
			}

			// In all cases, clear the highlight.
			this.highlightedIndex_ = -1;
		}

		/// <summary>
		/// Overrides {@link goog.ui.Control#getCaption} to return the empty string,
		/// since palettes don't have text captions.
		/// </summary>
		/// <returns>The empty string.</returns>
		public override string getCaption()
		{
			return "";
		}


		/// <summary>
		/// Overrides {@link goog.ui.Control#setCaption} to be a no-op, since palettes
		/// don't have text captions.
		/// </summary>
		/// <param name="caption">Ignored.</param>
		public override void setCaption(string caption)
		{
			// Do nothing.
		}


		// Palette event handling.


		/// <summary>
		/// Handles mouseover events.  Overrides {@link goog.ui.Control#handleMouseOver}
		/// by determining which palette item (if any) was moused over, highlighting it,
		/// and un-highlighting any previously-highlighted item.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public override void handleMouseOver(goog.events.BrowserEvent e)
		{
			base.handleMouseOver(e);

			var item = ((PaletteRenderer)this.getRenderer()).getContainingItem(this, (Node)e.target);
			if (item != null && e.relatedTarget != null && goog.dom.contains(item, (Node)e.relatedTarget)) {
				// Ignore internal mouse moves.
				return;
			}

			if (item != this.getHighlightedItem()) {
				this.setHighlightedItem(item);
			}
		}

		/// <summary>
		/// Handles mousedown events.  Overrides {@link goog.ui.Control#handleMouseDown}
		/// by ensuring that the item on which the user moused down is highlighted.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public override void handleMouseDown(goog.events.BrowserEvent e)
		{
			base.handleMouseDown(e);

			if (this.isActive()) {
				// Make sure we move the highlight to the cell on which the user moused
				// down.
				var item = ((PaletteRenderer)this.getRenderer()).getContainingItem(this, (Node)e.target);
				if (item != this.getHighlightedItem()) {
					this.setHighlightedItem(item);
				}
			}
		}


		/// <summary>
		/// Selects the currently highlighted palette item (triggered by mouseup or by
		/// keyboard action).  Overrides {@link goog.ui.Control#performActionInternal}
		/// by selecting the highlighted item and dispatching an ACTION event.
		/// </summary>
		/// <param name="e">Mouse or key event that triggered the action.</param>
		/// <returns>True if the action was allowed to proceed, false otherwise.</returns>
		public override bool performActionInternal(goog.events.Event e)
		{
			var highlightedItem = this.getHighlightedItem();
			if (highlightedItem != null) {
				if (e != null && this.shouldSelectHighlightedItem_(e)) {
					this.setSelectedItem(highlightedItem);
				}
				return base.performActionInternal(e);
			}
			return false;
		}

		/// <summary>
		/// Determines whether to select the highlighted item while handling an internal
		/// action. The highlighted item should not be selected if the action is a mouse
		/// event occurring outside the palette or in an "empty" cell.
		/// </summary>
		/// <param name="e">Mouseup or key event being handled.</param>
		/// <returns>True if the highlighted item should be selected.</returns>
		private bool shouldSelectHighlightedItem_(goog.events.Event e)
		{
			if (this.getSelectedItem() == null) {
				// It's always ok to select when nothing is selected yet.
				return true;
			}
			else if (e.type != "mouseup") {
				// Keyboard can only act on valid cells.
				return true;
			}
			else {
				// Return whether or not the mouse action was in the palette.
				return ((PaletteRenderer)this.getRenderer()).getContainingItem(this, (Node)e.target) != null;
			}
		}


		/// <summary>
		/// Handles keyboard events dispatched while the palette has focus.  Moves the
		/// highlight on arrow keys, and selects the highlighted item on Enter or Space.
		/// Returns true if the event was handled, false otherwise.  In particular, if
		/// the user attempts to navigate out of the grid, the highlight isn't changed,
		/// and this method returns false; it is then up to the parent component to
		/// handle the event (e.g. by wrapping the highlight around).  Overrides {@link
		/// goog.ui.Control#handleKeyEvent}.
		/// </summary>
		/// <param name="e">Key event to handle.</param>
		/// <returns>True iff the key event was handled by the component.</returns>
		public override bool handleKeyEvent(goog.events.KeyEvent e)
		{
			var items = this.getContent().As<JsArray<Node>>();
			var numItems = items != null ? items.Length : 0;
			var numColumns = (int)this.size_.width;

			// If the component is disabled or the palette is empty, bail.
			if (numItems == 0 || !this.isEnabled()) {
				return false;
			}

			// User hit ENTER or SPACE; trigger action.
			if (e.keyCode == (int)goog.events.KeyCodes.ENTER ||
				e.keyCode == (int)goog.events.KeyCodes.SPACE) {
				return this.performActionInternal(e);
			}

			// User hit HOME or END; move highlight.
			if (e.keyCode == (int)goog.events.KeyCodes.HOME) {
				this.setHighlightedIndex(0);
				return true;
			}
			else if (e.keyCode == (int)goog.events.KeyCodes.END) {
				this.setHighlightedIndex(numItems - 1);
				return true;
			}

			// If nothing is highlighted, start from the selected index.  If nothing is
			// selected either, highlightedIndex is -1.
			var highlightedIndex = this.highlightedIndex_ < 0 ? this.getSelectedIndex() :
																this.highlightedIndex_;

			switch ((goog.events.KeyCodes)e.keyCode) {
			case goog.events.KeyCodes.LEFT:
				// If the highlighted index is uninitialized, or is at the beginning, move
				// it to the end.
				if (highlightedIndex == -1 || highlightedIndex == 0) {
					highlightedIndex = numItems;
				}
				this.setHighlightedIndex(highlightedIndex - 1);
				e.preventDefault();
				return true;

			case goog.events.KeyCodes.RIGHT:
				// If the highlighted index at the end, move it to the beginning.
				if (highlightedIndex == numItems - 1) {
					highlightedIndex = -1;
				}
				this.setHighlightedIndex(highlightedIndex + 1);
				e.preventDefault();
				return true;

			case goog.events.KeyCodes.UP:
				if (highlightedIndex == -1) {
					highlightedIndex = numItems + numColumns - 1;
				}
				if (highlightedIndex >= numColumns) {
					this.setHighlightedIndex(highlightedIndex - numColumns);
					e.preventDefault();
					return true;
				}
				break;

			case goog.events.KeyCodes.DOWN:
				if (highlightedIndex == -1) {
					highlightedIndex = -numColumns;
				}
				if (highlightedIndex < numItems - numColumns) {
					this.setHighlightedIndex(highlightedIndex + numColumns);
					e.preventDefault();
					return true;
				}
				break;
			}

			return false;
		}


		/// <summary>
		/// Handles selection change events dispatched by the selection model.
		/// </summary>
		/// <param name="e">Selection event to handle.</param>
		public void handleSelectionChange(goog.events.Event e)
		{
			// No-op in the base class.
		}


		// Palette management.


		/// <summary>
		/// Returns the size of the palette grid.
		/// </summary>
		/// <returns>Palette size (columns x rows).</returns>
		public goog.math.Size getSize()
		{
			return this.size_;
		}


		/// <summary>
		/// Sets the size of the palette grid to the given size.  Callers can either
		/// pass a single {@link goog.math.Size} or a pair of numbers (first the number
		/// of columns, then the number of rows) to this method.  In both cases, the
		/// number of rows is optional and will be calculated automatically if needed.
		/// It is an error to attempt to change the size of the palette after it has
		/// been rendered.
		/// </summary>
		/// <param name="size">Either a size object or the number of
		/// columns.</param>
		/// <param name="opt_rows">The number of rows (optional).</param>
		public void setSize(Union<goog.math.Size, int> size, int opt_rows = 0)
		{
			if (this.getElement() != null) {
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			this.size_ = size.Is<int>() ?
				new goog.math.Size(size.As<int>(), opt_rows) :
				size.As<math.Size>();

			// Adjust size, if needed.
			this.adjustSize_();
		}


		/// <summary>
		/// Returns the 0-based index of the currently highlighted palette item, or -1
		/// if no item is highlighted.
		/// </summary>
		/// <returns>Index of the highlighted item (-1 if none).</returns>
		public int getHighlightedIndex()
		{
			return this.highlightedIndex_;
		}


		/// <summary>
		/// Returns the currently highlighted palette item, or null if no item is
		/// highlighted.
		/// </summary>
		/// <returns>The highlighted item (undefined if none).</returns>
		public Node getHighlightedItem()
		{
			var items = this.getContent().As<JsArray<Node>>();
			return items != null ? items[this.highlightedIndex_] : null;
		}


		/// <summary>
		/// </summary>
		/// <returns>The highlighted cell.</returns>
		private HTMLElement getHighlightedCellElement_()
		{
			return ((PaletteRenderer)this.getRenderer()).getCellForItem(this.getHighlightedItem());
		}


		/// <summary>
		/// Highlights the item at the given 0-based index, or removes the highlight
		/// if the argument is -1 or out of range.  Any previously-highlighted item
		/// will be un-highlighted.
		/// </summary>
		/// <param name="index">0-based index of the item to highlight.</param>
		public void setHighlightedIndex(int index)
		{
			if (index != this.highlightedIndex_) {
				this.highlightIndex_(this.highlightedIndex_, false);
				this.lastHighlightedIndex_ = this.highlightedIndex_;
				this.highlightedIndex_ = index;
				this.highlightIndex_(index, true);
				this.dispatchEvent(goog.ui.Palette.EventType.AFTER_HIGHLIGHT);
			}
		}


		/// <summary>
		/// Highlights the given item, or removes the highlight if the argument is null
		/// or invalid.  Any previously-highlighted item will be un-highlighted.
		/// </summary>
		/// <param name="item">Item to highlight.</param>
		public void setHighlightedItem(Node item)
		{
			var items = this.getContent().As<JsArray<Node>>();
			this.setHighlightedIndex(
				(items != null && item != null) ? items.IndexOf(item) : -1);
		}

		/// <summary>
		/// Returns the 0-based index of the currently selected palette item, or -1
		/// if no item is selected.
		/// </summary>
		/// <returns>Index of the selected item (-1 if none).</returns>
		public int getSelectedIndex()
		{
			return this.selectionModel_ != null ? this.selectionModel_.getSelectedIndex() : -1;
		}

		/// <summary>
		/// Returns the currently selected palette item, or null if no item is selected.
		/// </summary>
		/// <returns>The selected item (null if none).</returns>
		public Node getSelectedItem()
		{
			return this.selectionModel_ != null ?
				this.selectionModel_.getSelectedItem() :
									null;
		}

		/// <summary>
		/// Selects the item at the given 0-based index, or clears the selection
		/// if the argument is -1 or out of range.  Any previously-selected item
		/// will be deselected.
		/// </summary>
		/// <param name="index">0-based index of the item to select.</param>
		public void setSelectedIndex(int index)
		{
			if (this.selectionModel_ != null) {
				this.selectionModel_.setSelectedIndex(index);
			}
		}

		/// <summary>
		/// Selects the given item, or clears the selection if the argument is null or
		/// invalid.  Any previously-selected item will be deselected.
		/// </summary>
		/// <param name="item">Item to select.</param>
		public void setSelectedItem(Node item)
		{
			if (this.selectionModel_ != null) {
				this.selectionModel_.setSelectedItem(item);
			}
		}


		/// <summary>
		/// Private helper; highlights or un-highlights the item at the given index
		/// based on the value of the Boolean argument.  This implementation simply
		/// applies highlight styling to the cell containing the item to be highighted.
		/// Does nothing if the palette hasn't been rendered yet.
		/// </summary>
		/// <param name="index">0-based index of item to highlight or un-highlight.</param>
		/// <param name="highlight">If true, the item is highlighted; otherwise it
		/// is un-highlighted.</param>
		private void highlightIndex_(int index, bool highlight)
		{
			if (this.getElement() != null) {
				var items = this.getContent().As<JsArray<Node>>();
				if (items != null && index >= 0 && index < items.Length) {
					var cellEl = this.getHighlightedCellElement_();
					if (this.currentCellControl_.getElement() != cellEl) {
						this.currentCellControl_.setElementInternal(cellEl);
					}
					if (this.currentCellControl_.tryHighlight(highlight)) {
						((PaletteRenderer)this.getRenderer()).highlightCell(this, items[index], highlight);
					}
				}
			}
		}


		public override void setHighlighted(bool highlight)
		{
			if (highlight && this.highlightedIndex_ == -1) {
				// If there was a last highlighted index, use that. Otherwise, highlight the
				// first cell.
				this.setHighlightedIndex(
					this.lastHighlightedIndex_ > -1 ? this.lastHighlightedIndex_ : 0);
			}
			else if (!highlight) {
				this.setHighlightedIndex(-1);
			}
			// The highlight event should be fired once the component has updated its own
			// state.
			base.setHighlighted(highlight);
		}


		/// <summary>
		/// Private helper; selects or deselects the given item based on the value of
		/// the Boolean argument.  This implementation simply applies selection styling
		/// to the cell containing the item to be selected.  Does nothing if the palette
		/// hasn't been rendered yet.
		/// </summary>
		/// <param name="item">Item to select or deselect.</param>
		/// <param name="select">If true, the item is selected; otherwise it is
		/// deselected.</param>
		private void selectItem_(Node item, bool select)
		{
			if (this.getElement() != null) {
				((PaletteRenderer)this.getRenderer()).selectCell(this, item, select);
			}
		}


		/// <summary>
		/// Calculates and updates the size of the palette based on any preset values
		/// and the number of palette items.  If there is no preset size, sets the
		/// palette size to the smallest square big enough to contain all items.  If
		/// there is a preset number of columns, increases the number of rows to hold
		/// all items if needed.  (If there are too many rows, does nothing.)
		/// </summary>
		private void adjustSize_()
		{
			var items = this.getContent().As<JsArray<Node>>();
			if (items != null) {
				if (this.size_ != null && this.size_.width != 0) {
					// There is already a size set; honor the number of columns (if >0), but
					// increase the number of rows if needed.
					var minRows = Math.Round(items.Length / this.size_.width, MidpointRounding.AwayFromZero);
					if ((this.size_.height >= Double.MinValue && this.size_.height <= Double.MaxValue)
						|| this.size_.height < minRows) {
						this.size_.height = minRows;
					}
				}
				else {
					// No size has been set; size the grid to the smallest square big enough
					// to hold all items (hey, why not?).
					var length = Math.Round(Math.Sqrt(items.Length), MidpointRounding.AwayFromZero);
					this.size_ = new goog.math.Size(length, length);
				}
			}
			else {
				// No items; set size to 0x0.
				this.size_ = new goog.math.Size(0, 0);
			}
		}

		internal class CurrentCell_ : Control
		{
			/// <summary>
			/// A component to represent the currently highlighted cell.
			/// </summary>
			internal CurrentCell_()
			{
				this.setDispatchTransitionEvents(goog.ui.Component.State.HOVER, true);
			}

			/// <summary>
			/// </summary>
			/// <param name="highlight">Whether to highlight or unhighlight the component.</param>
			/// <returns>Whether it was successful.</returns>
			public bool tryHighlight(bool highlight)
			{
				this.setHighlighted(highlight);
				return this.isHighlighted() == highlight;
			}
		}
	}
}
