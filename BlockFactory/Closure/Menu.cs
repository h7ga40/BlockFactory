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
 * @fileoverview A base menu class that supports key and mouse events. The menu
 * can be bound to an existing HTML structure or can generate its own DOM.
 *
 * To decorate, the menu should be bound to an element containing children
 * with the classname 'goog-menuitem'.  HRs will be classed as separators.
 *
 * Decorate Example:
 * <div id="menu" class="goog-menu" tabIndex="0">
 *   <div class="goog-menuitem">Google</div>
 *   <div class="goog-menuitem">Yahoo</div>
 *   <div class="goog-menuitem">MSN</div>
 *   <hr>
 *   <div class="goog-menuitem">New...</div>
 * </div>
 * <script>
 *
 * var menu = new goog.ui.Menu();
 * menu.decorate(goog.dom.getElement('menu'));
 *
 * TESTED=FireFox 2.0, IE6, Opera 9, Chrome.
 * TODO(user): Key handling is flaky in Opera and Chrome
 * TODO(user): Rename all references of "item" to child since menu is
 * essentially very generic and could, in theory, host a date or color picker.
 *
 * @see ../demos/menu.html
 * @see ../demos/menus.html
 */
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;


namespace goog.ui
{
	// The dependencies MenuHeader, MenuItem, and MenuSeparator are implicit.
	// There are no references in the code, but we need to load these
	// classes before goog.ui.Menu.


	public class Menu : Container
	{
		// TODO(robbyw): Reverse constructor argument order for consistency.
		/// <summary>
		/// A basic menu class.
		/// </summary>
		/// <param name="opt_domHelper">Optional DOM helper.</param>
		/// <param name="opt_renderer">Renderer used to render or
		///    decorate the container; defaults to {@link goog.ui.MenuRenderer}.</param>
		public Menu(goog.dom.DomHelper opt_domHelper = null, goog.ui.MenuRenderer opt_renderer = null)
			: base(Orientation.VERTICAL, opt_renderer ?? goog.ui.MenuRenderer.getInstance(), opt_domHelper)
		{
			// Unlike Containers, Menus aren't keyboard-accessible by default.  This line
			// preserves backwards compatibility with code that depends on menus not
			// receiving focus - e.g. {@code goog.ui.MenuButton}.
			this.setFocusable(false);
		}

		public new class EventType
		{
			/// <summary>Dispatched before the menu becomes visible</summary>
			public const string BEFORE_SHOW = Component.EventType.BEFORE_SHOW;
			/// <summary>Dispatched when the menu is shown</summary>
			public const string SHOW = Component.EventType.SHOW;
			/// <summary>Dispatched before the menu becomes hidden</summary>
			public const string BEFORE_HIDE = Component.EventType.HIDE;
			/// <summary>Dispatched when the menu is hidden</summary>
			public const string HIDE = Component.EventType.HIDE;
		}

		// TODO(robbyw): Remove this and all references to it.
		/// <summary>
		/// CSS class for menus.
		/// </summary>
		public static string CSS_CLASS;

		/// <summary>
		/// Coordinates of the mousedown event that caused this menu to be made visible.
		/// Used to prevent the consequent mouseup event due to a simple click from
		/// activating a menu item immediately. Considered protected; should only be used
		/// within this package or by subclasses.
		/// </summary>
		public goog.math.Coordinate openingCoords;

		/// <summary>
		/// Whether the menu can move the focus to its key event target when it is
		/// shown.  Default = true
		/// </summary>
		private bool allowAutoFocus_ = true;

		/// <summary>
		/// Whether the menu should use windows syle behavior and allow disabled menu
		/// items to be highlighted (though not selectable).  Defaults to false
		/// </summary>
		private bool allowHighlightDisabled_ = false;

		/// <summary>
		/// Returns the CSS class applied to menu elements, also used as the prefix for
		/// derived styles, if any.  Subclasses should override this method as needed.
		/// Considered protected.
		/// </summary>
		/// <returns>The CSS class applied to menu elements.</returns>
		protected string getCssClass()
		{
			return this.getRenderer().getCssClass();
		}

		/// <summary>
		/// Returns whether the provided element is to be considered inside the menu for
		/// purposes such as dismissing the menu on an event.  This is so submenus can
		/// make use of elements outside their own DOM.
		/// </summary>
		/// <param name="element">The element to test for.</param>
		/// <returns>Whether the provided element is to be considered inside
		///     the menu.</returns>
		public bool containsElement(HTMLElement element)
		{
			if (((MenuRenderer)this.getRenderer()).containsElement(this, element)) {
				return true;
			}

			for (int i = 0, count = this.getChildCount(); i < count; i++) {
				var child = this.getChildAt(i);
				if (/*typeof child.containsElement == "function"*/
					child is Menu menu
					/**/ &&
					/*child*/menu/**/.containsElement(element)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Adds a new menu item at the end of the menu.
		/// </summary>
		/// <param name="item"> Menu</param>
		///    item to add to the menu.
		public void addItem(Union<MenuHeader, MenuItem, MenuSeparator> item)
		{
			this.addChild((Component)item.Value, true);
		}

		/// <summary>
		/// Adds a new menu item at a specific index in the menu.
		/// </summary>
		/// <param name="item">Menu
		///    item to add to the menu.</param>
		/// <param name="n"> Index at which to insert the menu item.</param>
		public void addItemAt(Union<MenuHeader, MenuItem, MenuSeparator> item, int n)
		{
			this.addChildAt((Component)item.Value, n, true);
		}

		/// <summary>
		/// Removes an item from the menu and disposes of it.
		/// </summary>
		/// <param name="item"> The
		///    menu item to remove.</param>
		public void removeItem(Union<MenuHeader, MenuItem, MenuSeparator> item)
		{
			var removedChild = this.removeChild((Component)item.Value, true);
			if (removedChild != null) {
				removedChild.dispose();
			}
		}

		/// <summary>
		/// Removes a menu item at a given index in the menu and disposes of it.
		/// </summary>
		/// <param name="n">Index of item.</param>
		public void removeItemAt(int n)
		{
			var removedChild = this.removeChildAt(n, true);
			if (removedChild != null) {
				removedChild.dispose();
			}
		}

		/// <summary>
		/// Returns a reference to the menu item at a given index.
		/// </summary>
		/// <param name="n"> Index of menu item.</param>
		/// <returns>Reference to the menu item.</returns>
		public Union<MenuHeader, MenuItem, MenuSeparator> getItemAt(int n)
		{
			return new Union<MenuHeader, MenuItem, MenuSeparator>(this.getChildAt(n));
		}

		/// <summary>
		/// Returns the number of items in the menu (including separators).
		/// </summary>
		/// <returns>The number of items in the menu.</returns>
		public int getItemCount()
		{
			return this.getChildCount();
		}

		/// <summary>
		/// Returns an array containing the menu items contained in the menu.
		/// </summary>
		/// <returns>An array of menu items.</returns>
		public JsArray<MenuItem> getItems()
		{
			// TODO(user): Remove reference to getItems and instead use getChildAt,
			// forEachChild, and getChildCount
			var children = new JsArray<MenuItem>();
			this.forEachChild((child) => { children.Push((MenuItem)child); });
			return children;
		}

		/// <summary>
		/// Sets the position of the menu relative to the view port.
		/// </summary>
		/// <param name="x">Left position or coordinate obj.</param>
		/// <param name="opt_y"> Top position.</param>
		public void setPosition(Union<double, goog.math.Coordinate> x, double opt_y = 0.0)
		{
			// NOTE(user): It is necessary to temporarily set the display from none, so
			// that the position gets set correctly.
			var visible = this.isVisible();
			if (!visible) {
				goog.style.setElementShown(this.getElement(), true);
			}
			goog.style.setPageOffset(this.getElement(), x, opt_y);
			if (!visible) {
				goog.style.setElementShown(this.getElement(), false);
			}
		}

		/// <summary>
		/// Gets the page offset of the menu, or null if the menu isn't visible
		/// </summary>
		/// <returns>Object holding the x-y coordinates of the
		///     menu or null if the menu is not visible.</returns>
		public goog.math.Coordinate getPosition()
		{
			return this.isVisible() ? goog.style.getPageOffset(this.getElement()) : null;
		}

		/// <summary>
		/// Sets whether the menu can automatically move focus to its key event target
		/// when it is set to visible.
		/// </summary>
		/// <param name="allow">Whether the menu can automatically move focus to its</param>
		public void setAllowAutoFocus(bool allow)
		{
			this.allowAutoFocus_ = allow;
			if (allow) {
				this.setFocusable(true);
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the menu can automatically move focus to its key
		///     event target when it is set to visible.</returns>
		public bool getAllowAutoFocus()
		{
			return this.allowAutoFocus_;
		}

		/// <summary>
		/// Sets whether the menu will highlight disabled menu items or skip to the next
		/// active item.
		/// </summary>
		/// <param name="allow">Whether the menu will highlight disabled menu items or
		/// skip to the next active item.</param>
		public void setAllowHighlightDisabled(bool allow)
		{
			this.allowHighlightDisabled_ = allow;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the menu will highlight disabled menu items or skip
		///     to the next active item.</returns>
		public bool getAllowHighlightDisabled()
		{
			return this.allowHighlightDisabled_;
		}

		public override bool setVisible(bool visible, bool opt_force = false)
		{
			return setVisible(visible, opt_force);
		}

		/// <summary>
		/// </summary>
		/// <param name="show">Whether to show or hide the menu.</param>
		/// <param name="opt_force">If true, doesn't check whether the menu
		/// already has the requested visibility, and doesn't dispatch any events.</param>
		/// <param name="opt_e">Mousedown event that caused this menu to
		/// be made visible (ignored if show is false).</param>
		/// <returns></returns>
		public bool setVisible(bool show, bool opt_force = false, goog.events.BrowserEvent opt_e = null)
		{
			var visibilityChanged =
				base.setVisible(show, opt_force);
			if (visibilityChanged && show && this.isInDocument() &&
				this.allowAutoFocus_) {
				this.getKeyEventTarget().Focus();
			}
			if (show && opt_e != null /*&& goog.isNumber(opt_e.clientX)*/) {
				this.openingCoords = new goog.math.Coordinate(opt_e.clientX, opt_e.clientY);
			}
			else {
				this.openingCoords = null;
			}
			return visibilityChanged;
		}

		public override bool handleEnterItem(events.Event e)
		{
			if (this.allowAutoFocus_) {
				this.getKeyEventTarget().Focus();
			}

			return base.handleEnterItem(e);
		}
		/// <summary>
		/// Highlights the next item that begins with the specified string.  If no
		/// (other) item begins with the given string, the selection is unchanged.
		/// </summary>
		/// <param name="charStr"> The prefix to match.</param>
		/// <returns>Whether a matching prefix was found.</returns>
		public bool highlightNextPrefix(string charStr)
		{
			var re = new Regex(@"^" + goog.@string.regExpEscape(charStr), RegexOptions.IgnoreCase);
			return this.highlightHelper((index, max) => {
				// Index is >= -1 because it is set to -1 when nothing is selected.
				var start = index < 0 ? 0 : index;
				var wrapped = false;

				// We always start looking from one after the current, because we
				// keep the current selection only as a last resort. This makes the
				// loop a little awkward in the case where there is no current
				// selection, as we need to stop somewhere but can't just stop
				// when index == start, which is why we need the "wrapped" flag.
				do {
					++index;
					if (index == max) {
						index = 0;
						wrapped = true;
					}
					var name = ((Control)this.getChildAt(index)).getCaption();
					if (name != null && name.Match(re) != null) {
						return index;
					}
				} while (!wrapped || index != start);
				return this.getHighlightedIndex();
			}, this.getHighlightedIndex());
		}

		protected override bool canHighlightItem(Control item)
		{
			return (this.allowHighlightDisabled_ || item.isEnabled()) &&
				item.isVisible() && item.isSupportedState(goog.ui.Component.State.HOVER);
		}

		protected override void decorateInternal(HTMLElement element)
		{
			this.decorateContent(element);
			base.decorateInternal(element);
		}


		public override bool handleKeyEventInternal(events.KeyEvent e)
		{
			var handled = base.handleKeyEventInternal(e);
			if (!handled) {
				// Loop through all child components, and for each menu item call its
				// key event handler so that keyboard mnemonics can be handled.
				this.forEachChild((menuItem_) => {
					MenuItem menuItem = (MenuItem)menuItem_;
					if (!handled && /*menuItem.getMnemonic &&*/
						(int)menuItem.getMnemonic() == e.keyCode) {
						if (this.isEnabled()) {
							this.setHighlighted(menuItem);
						}
						// We still delegate to handleKeyEvent, so that it can handle
						// enabled/disabled state.
						handled = menuItem.handleKeyEvent(e);
					}
				});
			}
			return handled;
		}

		public override void setHighlightedIndex(int index)
		{
			base.setHighlightedIndex(index);

			// Bring the highlighted item into view. This has no effect if the menu is not
			// scrollable.
			var child = this.getChildAt(index);
			if (child != null) {
				goog.style.scrollIntoContainerView(child.getElement(), this.getElement());
			}
		}

		/// <summary>
		/// Decorate menu items located in any descendent node which as been explicitly
		/// marked as a "content" node.
		/// </summary>
		/// <param name="element"> Element to decorate.</param>
		protected void decorateContent(Element element)
		{
			var renderer = this.getRenderer();
			var contentElements = this.getDomHelper().getElementsByTagNameAndClass(
				goog.dom.TagName.DIV, le.getCssName(renderer.getCssClass(), "content"),
				element);

			// Some versions of IE do not like it when you access this nodeList
			// with invalid indices. See
			// http://code.google.com/p/closure-library/issues/detail?id=373
			var length = contentElements.Length;
			for (var i = 0; i < length; i++) {
				renderer.decorateChildren(this, (HTMLElement)contentElements[i]);
			}
		}
	}

	public class MenuHeader
	{

	}

	public class MenuSeparator
	{

	}
}
