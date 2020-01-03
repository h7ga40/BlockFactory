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
 * @fileoverview Popup Color Picker implementation.  This is intended to be
 * less general than goog.ui.ColorPicker and presents a default set of colors
 * that CCC apps currently use in their color pickers.
 *
 * @see ../demos/popupcolorpicker.html
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class PopupColorPicker : Component
	{
		/// <summary>
		/// Popup color picker widget.
		/// </summary>
		/// <param name="opt_domHelper">Optional DOM helper.</param>
		/// <param name="opt_colorPicker">Optional color picker to use
		/// for this popup.</param>
		public PopupColorPicker(dom.DomHelper opt_domHelper = null, ColorPicker opt_colorPicker = null)
			: base(opt_domHelper)
		{
			if (opt_colorPicker != null) {
				this.colorPicker_ = opt_colorPicker;
			}
		}

		/// <summary>
		/// Whether the color picker is initialized.
		/// </summary>
		private bool initialized_ = false;

		/// <summary>
		/// Instance of a color picker control.
		/// </summary>
		private goog.ui.ColorPicker colorPicker_ = null;

		/// <summary>
		/// Instance of goog.ui.Popup used to manage the behavior of the color picker.
		/// </summary>
		private goog.ui.Popup popup_ = null;


		/// <summary>
		/// Corner of the popup which is pinned to the attaching element.
		/// </summary>
		private goog.positioning.Corner pinnedCorner_ =
			goog.positioning.Corner.TOP_START;


		/// <summary>
		/// Corner of the attaching element where the popup shows.
		/// </summary>
		private goog.positioning.Corner popupCorner_ =
			goog.positioning.Corner.BOTTOM_START;


		/// <summary>
		/// Reference to the element that triggered the last popup.
		/// </summary>
		private HTMLElement lastTarget_ = null;


		public bool rememberSelection_;


		/// <summary>
		/// Whether the color picker can move the focus to its key event target when it
		/// is shown.  The default is true.  Setting to false can break keyboard
		/// navigation, but this is needed for certain scenarios, for example the
		/// toolbar menu in trogedit which can't have the selection changed.
		/// </summary>
		private bool allowAutoFocus_ = true;


		/// <summary>
		/// Whether the color picker can accept focus.
		/// </summary>
		private bool focusable_ = true;


		/// <summary>
		/// If true, then the colorpicker will toggle off if it is already visible.
		/// </summary>
		private bool toggleMode_ = true;


		/// <summary>
		/// If true, the colorpicker will appear on hover.
		/// </summary>
		private bool showOnHover_ = false;


		public override void createDom()
		{
			base.createDom();
			this.popup_ = new goog.ui.Popup(this.getElement());
			this.popup_.setPinnedCorner(this.pinnedCorner_);
			goog.asserts.assert(this.getElement() != null);
			goog.dom.classlist.set(this.getElement(), goog.le.getCssName("goog-popupcolorpicker"));
			/*TODO:unselectable???this.getElement().unselectable = "on";*/
		}


		public override void disposeInternal()
		{
			base.disposeInternal();
			this.colorPicker_ = null;
			this.lastTarget_ = null;
			this.initialized_ = false;
			if (this.popup_ != null) {
				this.popup_.dispose();
				this.popup_ = null;
			}
		}


		/// <summary>
		/// ColorPickers cannot be used to decorate pre-existing html, since the
		/// structure they build is fairly complicated.
		/// </summary>
		/// <param name="element"> Element to decorate.</param>
		/// <returns>Returns always false.</returns>
		public override bool canDecorate(Element element)
		{
			return false;
		}


		/// <summary>
		/// </summary>
		/// <returns>The color picker instance.</returns>
		public goog.ui.ColorPicker getColorPicker()
		{
			return this.colorPicker_;
		}


		/// <summary>
		/// Returns whether the Popup dismisses itself when the user clicks outside of
		/// it.
		/// </summary>
		/// <returns>Whether the Popup autohides on an external click.</returns>
		public bool getAutoHide()
		{
			return this.popup_ != null && this.popup_.getAutoHide();
		}


		/// <summary>
		/// Sets whether the Popup dismisses itself when the user clicks outside of it -
		/// must be called after the Popup has been created (in createDom()),
		/// otherwise it does nothing.
		/// </summary>
		/// <param name="autoHide"> Whether to autohide on an external click.</param>
		public void setAutoHide(bool autoHide)
		{
			if (this.popup_ != null) {
				this.popup_.setAutoHide(autoHide);
			}
		}


		/// <summary>
		/// Returns the region inside which the Popup dismisses itself when the user
		/// clicks, or null if it was not set. Null indicates the entire document is
		/// the autohide region.
		/// </summary>
		/// <returns>The DOM element for autohide, or null if it hasn't been
		///     set.</returns>
		public Element getAutoHideRegion()
		{
			return this.popup_ == null ? null : this.popup_.getAutoHideRegion();
		}


		/// <summary>
		/// Sets the region inside which the Popup dismisses itself when the user
		/// clicks - must be called after the Popup has been created (in createDom()),
		/// otherwise it does nothing.
		/// </summary>
		/// <param name="element"> The DOM element for autohide.</param>
		public void setAutoHideRegion(Element element)
		{
			if (this.popup_ != null) {
				this.popup_.setAutoHideRegion(element);
			}
		}


		/// <summary>
		/// Returns the {@link goog.ui.PopupBase} from this picker. Returns null if the
		/// popup has not yet been created.
		/// NOTE: This should *ONLY* be called from tests. If called before createDom(),
		/// this should return null.
		/// </summary>
		/// <returns>The popup or null if it hasn't been created.</returns>
		public goog.ui.PopupBase getPopup()
		{
			return this.popup_;
		}


		/// <summary>
		/// </summary>
		/// <returns>The last element that triggered the popup.</returns>
		public Element getLastTarget()
		{
			return this.lastTarget_;
		}


		/// <summary>
		/// Attaches the popup color picker to an element.
		/// </summary>
		/// <param name="element"> The element to attach to.</param>
		public void attach(Element element)
		{
			if (this.showOnHover_) {
				this.getHandler().listen<events.BrowserEvent>(
					element, goog.events.EventType.MOUSEOVER, this.show_);
			}
			else {
				this.getHandler().listen<events.BrowserEvent>(
					element, goog.events.EventType.MOUSEDOWN, this.show_);
			}
		}


		/// <summary>
		/// Detatches the popup color picker from an element.
		/// </summary>
		/// <param name="element"> The element to detach from.</param>
		public void detach(Element element)
		{
			if (this.showOnHover_) {
				this.getHandler().unlisten(
					element, goog.events.EventType.MOUSEOVER, new Action<events.BrowserEvent>(this.show_));
			}
			else {
				this.getHandler().unlisten(
					element, goog.events.EventType.MOUSEOVER, new Action<events.BrowserEvent>(this.show_));
			}
		}


		/// <summary>
		/// Gets the color that is currently selected in this color picker.
		/// </summary>
		/// <returns>The hex string of the color selected, or null if no
		///     color is selected.</returns>
		public string getSelectedColor()
		{
			return this.colorPicker_.getSelectedColor();
		}


		/// <summary>
		/// Sets whether the color picker can accept focus.
		/// </summary>
		/// <param name="focusable"> True iff the color picker can accept focus.</param>
		public void setFocusable(bool focusable)
		{
			this.focusable_ = focusable;
			if (this.colorPicker_ != null) {
				// TODO(user): In next revision sort the behavior of passing state to
				// children correctly
				this.colorPicker_.setFocusable(focusable);
			}
		}


		/// <summary>
		/// Sets whether the color picker can automatically move focus to its key event
		/// target when it is set to visible.
		/// </summary>
		/// <param name="allow"> Whether to allow auto focus.</param>
		public void setAllowAutoFocus(bool allow)
		{
			this.allowAutoFocus_ = allow;
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether the color picker can automatically move focus to
		///     its key event target when it is set to visible.</returns>
		public bool getAllowAutoFocus()
		{
			return this.allowAutoFocus_;
		}


		/// <summary>
		/// Sets whether the color picker should toggle off if it is already open.
		/// </summary>
		/// <param name="toggle"> The new toggle mode.</param>
		public void setToggleMode(bool toggle)
		{
			this.toggleMode_ = toggle;
		}


		/// <summary>
		/// Gets whether the colorpicker is in toggle mode
		/// </summary>
		/// <returns>toggle.</returns>
		public bool getToggleMode()
		{
			return this.toggleMode_;
		}


		/// <summary>
		/// Sets whether the picker remembers the last selected color between popups.
		/// </summary>
		/// <param name="remember"> Whether to remember the selection.</param>
		public void setRememberSelection(bool remember)
		{
			this.rememberSelection_ = remember;
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether the picker remembers the last selected color
		///     between popups.</returns>
		public bool getRememberSelection()
		{
			return this.rememberSelection_;
		}


		/// <summary>
		/// Add an array of colors to the colors displayed by the color picker.
		/// Does not add duplicated colors.
		/// </summary>
		/// <param name="colors"> The array of colors to be added.</param>
		public void addColors(JsArray<string> colors)
		{

		}


		/// <summary>
		/// Clear the colors displayed by the color picker.
		/// </summary>
		public void clearColors()
		{

		}


		/// <summary>
		/// Set the pinned corner of the popup.
		/// </summary>
		/// <param name="corner"> The corner of the popup which is
		/// pinned to the attaching element.</param>
		public void setPinnedCorner(goog.positioning.Corner corner)
		{
			this.pinnedCorner_ = corner;
			if (this.popup_ != null) {
				this.popup_.setPinnedCorner(this.pinnedCorner_);
			}
		}


		/// <summary>
		/// Sets which corner of the attaching element this popup shows up.
		/// </summary>
		/// <param name="corner"> The corner of the attaching element
		/// where to show the popup.</param>
		public void setPopupCorner(goog.positioning.Corner corner)
		{
			this.popupCorner_ = corner;
		}


		/// <summary>
		/// Sets whether the popup shows up on hover. By default, appears on click.
		/// </summary>
		/// <param name="showOnHover"> True if popup should appear on hover.</param>
		public void setShowOnHover(bool showOnHover)
		{
			this.showOnHover_ = showOnHover;
		}


		/// <summary>
		/// Handles click events on the targets and shows the color picker.
		/// </summary>
		/// <param name="e"> The browser event.</param>
		private void show_(goog.events.BrowserEvent e)
		{
			if (!this.initialized_) {
				this.colorPicker_ = this.colorPicker_ == null ? null :
					goog.ui.ColorPicker.createSimpleColorGrid(this.getDomHelper());
				this.colorPicker_.setFocusable(this.focusable_);
				this.addChild(this.colorPicker_, true);
				this.getHandler().listen<events.Event>(
					this.colorPicker_, goog.ui.ColorPicker.EventType.CHANGE,
					this.onColorPicked_);
				this.initialized_ = true;
			}

			if (this.popup_.isOrWasRecentlyVisible() && this.toggleMode_ &&
				this.lastTarget_ == e.currentTarget) {
				this.popup_.setVisible(false);
				return;
			}

			this.lastTarget_ = (HTMLElement)(e.currentTarget);
			this.popup_.setPosition(
				new goog.positioning.AnchoredPosition(
					this.lastTarget_, this.popupCorner_));
			if (!this.rememberSelection_) {
				this.colorPicker_.setSelectedIndex(-1);
			}
			this.popup_.setVisible(true);
			if (this.allowAutoFocus_) {
				this.colorPicker_.focus();
			}
		}


		/// <summary>
		/// Handles the color change event.
		/// </summary>
		/// <param name="e"> The event.</param>
		private void onColorPicked_(goog.events.Event e)
		{
			// When we show the color picker we reset the color, which triggers an event.
			// Here we block that event so that it doesn't dismiss the popup
			// TODO(user): Update the colorpicker to allow selection to be cleared
			if (this.colorPicker_.getSelectedIndex() == -1) {
				e.stopPropagation();
				return;
			}
			this.popup_.setVisible(false);
			if (this.allowAutoFocus_) {
				this.lastTarget_.Focus();
			}
		}
	}
}
