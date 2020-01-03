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
 * @fileoverview A class for representing items in menus.
 * @see goog.ui.Menu
 *
 * @author attila@google.com (Attila Bodis)
 * @see ../demos/menuitem.html
 */

using System;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class MenuItem : Control
	{
		/// <summary>
		/// Class representing an item in a menu.
		/// </summary>
		/// <param name="content">Text caption or DOM structure to
		///    display as the content of the item (use to add icons or styling to
		///    menus).</param>
		/// <param name="opt_model">Data/model associated with the menu item.</param>
		/// <param name="opt_domHelper">Optional DOM helper used for
		///    document interactions.</param>
		/// <param name="opt_renderer">Optional renderer.</param>
		public MenuItem(string content, object opt_model = null, goog.dom.DomHelper opt_domHelper = null, goog.ui.MenuItemRenderer opt_renderer = null)
			: base(new ControlContent(content), opt_renderer ?? goog.ui.MenuItemRenderer.getInstance(), opt_domHelper)
		{
			this.setValue(opt_model);
		}

		/// <summary>
		/// The access key for this menu item. This key allows the user to quickly
		/// trigger this item's action with they keyboard. For example, setting the
		/// mnenomic key to 70 (F), when the user opens the menu and hits "F," the
		/// menu item is triggered.
		/// </summary>
		private goog.events.KeyCodes mnemonicKey_;

		/// <summary>
		/// The class set on an element that contains a parenthetical mnemonic key hint.
		/// Parenthetical hints are added to items in which the mnemonic key is not found
		/// within the menu item's caption itself. For example, if you have a menu item
		/// with the caption "Record," but its mnemonic key is "I", the caption displayed
		/// in the menu will appear as "Record (I)".
		/// </summary>
		private static string MNEMONIC_WRAPPER_CLASS_ =
			le.getCssName("goog-menuitem-mnemonic-separator");

		/// <summary>
		/// The class set on an element that contains a keyboard accelerator hint.
		/// </summary>
		public static string ACCELERATOR_CLASS = le.getCssName("goog-menuitem-accel");


		// goog.ui.Component and goog.ui.Control implementation.

		/// <summary>
		/// Returns the value associated with the menu item.  The default implementation
		/// returns the model object associated with the item (if any), or its caption.
		/// </summary>
		/// <returns>Value associated with the menu item, if any, or its caption.</returns>
		public string getValue()
		{
			var model = this.getModel();
			return model != null ? model.ToString() : this.getCaption();
		}

		/// <summary>
		/// Sets the value associated with the menu item.  The default implementation
		/// stores the value as the model of the menu item.
		/// </summary>
		/// <param name="value"> Value to be associated with the menu item.</param>
		public void setValue(object value)
		{
			this.setModel(value);
		}

		public override void setSupportedState(State state, bool support)
		{
			base.setSupportedState(state, support);
			switch (state) {
			case goog.ui.Component.State.SELECTED:
				this.setSelectableInternal_(support);
				break;
			case goog.ui.Component.State.CHECKED:
				this.setCheckableInternal_(support);
				break;
			}
		}

		/// <summary>
		/// Sets the menu item to be selectable or not.  Set to true for menu items
		/// that represent selectable options.
		/// </summary>
		/// <param name="selectable"> Whether the menu item is selectable.</param>
		public void setSelectable(bool selectable)
		{
			this.setSupportedState(goog.ui.Component.State.SELECTED, selectable);
		}

		/// <summary>
		/// Sets the menu item to be selectable or not.
		/// </summary>
		/// <param name="selectable">  Whether the menu item is selectable.</param>
		private void setSelectableInternal_(bool selectable)
		{
			if (this.isChecked() && !selectable) {
				this.setChecked(false);
			}

			var element = this.getElement();
			if (element != null) {
				((MenuItemRenderer)this.getRenderer()).setSelectable(this, element, selectable);
			}
		}

		/// <summary>
		/// Sets the menu item to be checkable or not.  Set to true for menu items
		/// that represent checkable options.
		/// </summary>
		/// <param name="checkable"> Whether the menu item is checkable.</param>
		public void setCheckable(bool checkable)
		{
			this.setSupportedState(goog.ui.Component.State.CHECKED, checkable);
		}

		/// <summary>
		/// Sets the menu item to be checkable or not.
		/// </summary>
		/// <param name="checkable"> Whether the menu item is checkable.</param>
		private void setCheckableInternal_(bool checkable)
		{
			var element = this.getElement();
			if (element != null) {
				((MenuItemRenderer)this.getRenderer()).setCheckable(this, element, checkable);
			}
		}

		/// <summary>
		/// Returns the text caption of the component while ignoring accelerators.
		/// </summary>
		public override string getCaption()
		{
			var content = this.getContent();
			if (content.Is<NodeList>()) {
				var acceleratorClass = goog.ui.MenuItem.ACCELERATOR_CLASS;
				var mnemonicWrapClass = goog.ui.MenuItem.MNEMONIC_WRAPPER_CLASS_;
				var caption = new JsArray<string>(
					content.As<NodeList>().Select((node) => {
						if (node.NodeType == NodeType.Element &&
							(goog.dom.classlist.contains(
								(HTMLElement)node, acceleratorClass) ||
							 goog.dom.classlist.contains(
								(HTMLElement)node,
								 mnemonicWrapClass))) {
							return "";
						}
						else {
							return goog.dom.getRawTextContent(node);
						}
					})).Join("");
				return goog.@string.collapseBreakingSpaces(caption);
			}
			return base.getCaption();
		}

		/// <summary>
		/// </summary>
		/// <returns>The keyboard accelerator text, or null if the menu item
		///     doesn't have one.</returns>
		public string getAccelerator()
		{
			var dom = this.getDomHelper();
			var content = this.getContent();
			if (content.Is<NodeList>()) {
				var acceleratorEl = content.As<NodeList>().FirstOrDefault((e) => {
					return goog.dom.classlist.contains(
						(HTMLElement)e, goog.ui.MenuItem.ACCELERATOR_CLASS);
				});
				if (acceleratorEl != null) {
					return goog.dom.getTextContent(acceleratorEl);
				}
			}
			return null;
		}

		public override void handleMouseUp(events.BrowserEvent e)
		{
			var parentMenu = (Menu)this.getParent();

			if (parentMenu != null) {
				var oldCoords = parentMenu.openingCoords;
				// Clear out the saved opening coords immediately so they"re not used twice.
				parentMenu.openingCoords = null;

				if (oldCoords != null /*&& goog.isNumber(e.clientX)*/) {
					var newCoords = new goog.math.Coordinate(e.clientX, e.clientY);
					if (goog.math.Coordinate.equals(oldCoords, newCoords)) {
						// This menu was opened by a mousedown and we"re handling the consequent
						// mouseup. The coords haven't changed, meaning this was a simple click,
						// not a click and drag. Don't do the usual behavior because the menu
						// just popped up under the mouse and the user didn't mean to activate
						// this item.
						return;
					}
				}
			}

			base.handleMouseUp(e);
		}

		protected override bool handleKeyEventInternal(events.KeyEvent e)
		{
			if (e.keyCode == (int)this.getMnemonic() && this.performActionInternal(e)) {
				return true;
			}
			else {
				return base.handleKeyEventInternal(e);
			}
		}

		/// <summary>
		/// Sets the mnemonic key code. The mnemonic is the key associated with this
		/// action.
		/// </summary>
		/// <param name="key"> The key code.</param>
		public void setMnemonic(goog.events.KeyCodes key)
		{
			this.mnemonicKey_ = key;
		}

		/// <summary>
		/// Gets the mnemonic key code. The mnemonic is the key associated with this
		/// action.
		/// </summary>
		/// <returns>The key code of the mnemonic key.</returns>
		public goog.events.KeyCodes getMnemonic()
		{
			return this.mnemonicKey_;
		}
		/// <summary>
		/// </summary>
		public override a11y.aria.Role getPreferredAriaRole()
		{
			if (this.isSupportedState(goog.ui.Component.State.CHECKED)) {
				return goog.a11y.aria.Role.MENU_ITEM_CHECKBOX;
			}
			if (this.isSupportedState(goog.ui.Component.State.SELECTED)) {
				return goog.a11y.aria.Role.MENU_ITEM_RADIO;
			}
			return base.getPreferredAriaRole();
		}


		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override Component getParent()
		{
			return base.getParent();
		}


		/// <summary>
		/// </summary>
		/// <returns></returns>
		public override events.EventTarget getParentEventTarget()
		{
			return base.getParentEventTarget();
		}
	}
}