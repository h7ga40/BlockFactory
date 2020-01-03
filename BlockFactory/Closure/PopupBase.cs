// Copyright 2006 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview Definition of the PopupBase class.
 *
 */

using System;
using Bridge;
using Bridge.Html5;
using goog.positioning;

namespace goog.ui
{
	public class PopupBase : goog.events.EventTarget
	{
		/// <summary>
		/// An event handler to manage the events easily
		/// </summary>
		private goog.events.EventHandler handler_;

		/// <summary>
		/// The PopupBase class provides functionality for showing and hiding a generic
		/// container element. It also provides the option for hiding the popup element
		/// if the user clicks outside the popup or the popup loses focus.
		/// </summary>
		/// <param name="opt_element"> A DOM element for the popup.</param>
		/// <param name="opt_type"> Type of popup.</param>
		public PopupBase(HTMLElement opt_element = null, goog.ui.PopupBase.Type opt_type = (Type)0)
		{
			this.handler_ = new goog.events.EventHandler(this);

			this.setElement(opt_element);
			if (opt_type != (Type)0) {
				this.setType(opt_type);
			}
		}

		/// <summary>
		/// Constants for type of Popup
		/// </summary>
		public enum /*string*/ Type
		{
			TOGGLE_DISPLAY,//= "toggle_display",
			MOVE_OFFSCREEN//= "move_offscreen"
		};


		/// <summary>
		/// The popup dom element that this Popup wraps.
		/// </summary>
		private HTMLElement element_ = null;


		/// <summary>
		/// Whether the Popup dismisses itself it the user clicks outside of it or the
		/// popup loses focus
		/// </summary>
		private bool autoHide_ = true;


		/// <summary>
		/// Mouse events without auto hide partner elements will not dismiss the popup.
		/// </summary>
		private JsArray<Element> autoHidePartners_ = null;


		/// <summary>
		/// Clicks outside the popup but inside this element will cause the popup to
		/// hide if autoHide_ is true. If this is null, then the entire document is used.
		/// For example, you can use a body-size div so that clicks on the browser
		/// scrollbar do not dismiss the popup.
		/// </summary>
		private Element autoHideRegion_ = null;


		/// <summary>
		/// Whether the popup is currently being shown.
		/// </summary>
		private bool isVisible_ = false;


		/// <summary>
		/// Whether the popup should hide itself asynchrously. This was added because
		/// there are cases where hiding the element in mouse down handler in IE can
		/// cause textinputs to get into a bad state if the element that had focus is
		/// hidden.
		/// </summary>
		private bool shouldHideAsync_ = false;


		/// <summary>
		/// The time when the popup was last shown.
		/// </summary>
		private long lastShowTime_ = -1;


		/// <summary>
		/// The time when the popup was last hidden.
		/// </summary>
		private long lastHideTime_ = -1;


		/// <summary>
		/// Whether to hide when the escape key is pressed.
		/// </summary>
		private bool hideOnEscape_ = false;


		/// <summary>
		/// Whether to enable cross-iframe dismissal.
		/// </summary>
		private bool enableCrossIframeDismissal_ = true;


		/// <summary>
		/// The type of popup
		/// </summary>
		private goog.ui.PopupBase.Type type_ = goog.ui.PopupBase.Type.TOGGLE_DISPLAY;


		/// <summary>
		/// Transition to play on showing the popup.
		/// </summary>
		private goog.fx.Transition showTransition_;


		/// <summary>
		/// Transition to play on hiding the popup.
		/// </summary>
		private goog.fx.Transition hideTransition_;


		/// <summary>
		/// Constants for event type fired by Popup
		/// </summary>
		public class EventType
		{
			public const string BEFORE_SHOW = "beforeshow";
			public const string SHOW = "show";
			public const string BEFORE_HIDE = "beforehide";
			public const string HIDE = "hide";
		};


		/// <summary>
		/// A time in ms used to debounce events that happen right after each other.
		/// A note about why this is necessary. There are two cases to consider.
		/// First case, a popup will usually see a focus event right after it's launched
		/// because it's typical for it to be launched in a mouse-down event which will
		/// then move focus to the launching button. We don't want to think this is a
		/// separate user action moving focus. Second case, a user clicks on the
		/// launcher button to close the menu. In that case, we"ll close the menu in the
		/// focus event and then show it again because of the mouse down event, even
		/// though the intention is to just close the menu. This workaround appears to
		/// be the least intrusive fix.
		/// </summary>
		public static int DEBOUNCE_DELAY_MS = 150;


		/// <summary>
		/// </summary>
		/// <returns>The type of popup this is.</returns>
		public goog.ui.PopupBase.Type getType()
		{
			return this.type_;
		}


		/// <summary>
		/// Specifies the type of popup to use.
		/// </summary>
		/// <param name="type"> Type of popup.</param>
		public void setType(goog.ui.PopupBase.Type type)
		{
			this.type_ = type;
		}


		/// <summary>
		/// Returns whether the popup should hide itself asynchronously using a timeout
		/// instead of synchronously.
		/// </summary>
		/// <returns>Whether to hide async.</returns>
		public bool shouldHideAsync()
		{
			return this.shouldHideAsync_;
		}


		/// <summary>
		/// Sets whether the popup should hide itself asynchronously using a timeout
		/// instead of synchronously.
		/// </summary>
		/// <param name="b"> Whether to hide async.</param>
		public void setShouldHideAsync(bool b)
		{
			this.shouldHideAsync_ = b;
		}


		/// <summary>
		/// Returns the dom element that should be used for the popup.
		/// </summary>
		/// <returns>The popup element.</returns>
		public HTMLElement getElement()
		{
			return this.element_;
		}


		/// <summary>
		/// Specifies the dom element that should be used for the popup.
		/// </summary>
		/// <param name="elt"> A DOM element for the popup.</param>
		public void setElement(HTMLElement elt)
		{
			this.ensureNotVisible_();
			this.element_ = elt;
		}


		/// <summary>
		/// Returns whether the Popup dismisses itself when the user clicks outside of
		/// it.
		/// </summary>
		/// <returns>Whether the Popup autohides on an external click.</returns>
		public bool getAutoHide()
		{
			return this.autoHide_;
		}


		/// <summary>
		/// Sets whether the Popup dismisses itself when the user clicks outside of it.
		/// </summary>
		/// <param name="autoHide"> Whether to autohide on an external click.</param>
		public void setAutoHide(bool autoHide)
		{
			this.ensureNotVisible_();
			this.autoHide_ = autoHide;
		}


		/// <summary>
		/// Mouse events that occur within an autoHide partner will not hide a popup
		/// set to autoHide.
		/// </summary>
		/// <param name="partner"> The auto hide partner element.</param>
		public void addAutoHidePartner(Element partner)
		{
			if (this.autoHidePartners_ == null) {
				this.autoHidePartners_ = new JsArray<Element>();
			}

			if (!this.autoHidePartners_.Contains(partner)) {
				this.autoHidePartners_.Push(partner);
			}
		}


		/// <summary>
		/// Removes a previously registered auto hide partner.
		/// </summary>
		/// <param name="partner"> The auto hide partner element.</param>
		public void removeAutoHidePartner(Element partner)
		{
			if (this.autoHidePartners_ != null) {
				this.autoHidePartners_.Remove(partner);
			}
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether the Popup autohides on the escape key.</returns>
		public bool getHideOnEscape()
		{
			return this.hideOnEscape_;
		}


		/// <summary>
		/// Sets whether the Popup dismisses itself on the escape key.
		/// </summary>
		/// <param name="hideOnEscape"> Whether to autohide on the escape key.</param>
		public void setHideOnEscape(bool hideOnEscape)
		{
			this.ensureNotVisible_();
			this.hideOnEscape_ = hideOnEscape;
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether cross iframe dismissal is enabled.</returns>
		public bool getEnableCrossIframeDismissal()
		{
			return this.enableCrossIframeDismissal_;
		}


		/// <summary>
		/// Sets whether clicks in other iframes should dismiss this popup.  In some
		/// cases it should be disabled, because it can cause spurious
		/// </summary>
		/// <param name="enable"> Whether to enable cross iframe dismissal.</param>
		public void setEnableCrossIframeDismissal(bool enable)
		{
			this.enableCrossIframeDismissal_ = enable;
		}


		/// <summary>
		/// Returns the region inside which the Popup dismisses itself when the user
		/// clicks, or null if it's the entire document.
		/// </summary>
		/// <returns>The DOM element for autohide, or null if it hasn't been
		///     set.</returns>
		public Element getAutoHideRegion()
		{
			return this.autoHideRegion_;
		}


		/// <summary>
		/// Sets the region inside which the Popup dismisses itself when the user
		/// clicks.
		/// </summary>
		/// <param name="element"> The DOM element for autohide.</param>
		public void setAutoHideRegion(Element element)
		{
			this.autoHideRegion_ = element;
		}


		/// <summary>
		/// Sets transition animation on showing and hiding the popup.
		/// </summary>
		/// <param name="opt_showTransition"> Transition to play on
		/// showing the popup.</param>
		/// <param name="opt_hideTransition"> Transition to play on
		/// hiding the popup.</param>
		public void setTransition(goog.fx.Transition opt_showTransition = null, goog.fx.Transition opt_hideTransition = null)
		{
			this.showTransition_ = opt_showTransition;
			this.hideTransition_ = opt_hideTransition;
		}

		/// <summary>
		/// Returns the time when the popup was last shown.
		/// </summary>
		/// <returns>time in ms since epoch when the popup was last shown, or
		/// -1 if the popup was never shown.</returns>
		public long getLastShowTime()
		{
			return this.lastShowTime_;
		}


		/// <summary>
		/// Returns the time when the popup was last hidden.
		/// </summary>
		/// <returns>time in ms since epoch when the popup was last hidden, or
		/// -1 if the popup was never hidden or is currently showing.</returns>
		public long getLastHideTime()
		{
			return this.lastHideTime_;
		}


		/// <summary>
		/// Returns the event handler for the popup. All event listeners belonging to
		/// this handler are removed when the tooltip is hidden. Therefore,
		/// the recommended usage of this handler is to listen on events in
		/// {@link #onShow}.
		/// </summary>
		/// <returns>Event handler for this popup.</returns>
		protected goog.events.EventHandler getHandler()
		{
			// As the template type is unbounded, narrow the "this" type
			var self = (goog.ui.PopupBase)(this);

			return self.handler_;
		}


		/// <summary>
		/// Helper to throw exception if the popup is showing.
		/// </summary>
		private void ensureNotVisible_()
		{
			if (this.isVisible_) {
				throw new Exception("Can not change this state of the popup while showing.");
			}
		}


		/// <summary>
		/// Returns whether the popup is currently visible.
		/// </summary>
		/// <returns>whether the popup is currently visible.</returns>
		public bool isVisible()
		{
			return this.isVisible_;
		}


		/// <summary>
		/// Returns whether the popup is currently visible or was visible within about
		/// 150 ms ago. This is used by clients to handle a very specific, but common,
		/// popup scenario. The button that launches the popup should close the popup
		/// on mouse down if the popup is alrady open. The problem is that the popup
		/// closes itself during the capture phase of the mouse down and thus the button
		/// thinks it's hidden and this should show it again. This method provides a
		/// good heuristic for clients. Typically in their event handler they will have
		/// code that is:
		/// <code>
		/// if (menu.isOrWasRecentlyVisible()) {
		///   menu.setVisible(false);
		/// } else {
		///   ... // code to position menu and initialize other state
		///   menu.setVisible(true);
		/// }
		/// </code>
		/// </summary>
		/// <returns>Whether the popup is currently visible or was visible
		///     within about 150 ms ago.</returns>
		public bool isOrWasRecentlyVisible()
		{
			return this.isVisible_ ||
				(DateTime.Now.Ticks - this.lastHideTime_ < goog.ui.PopupBase.DEBOUNCE_DELAY_MS);
		}

		/// <summary>
		/// Sets whether the popup should be visible. After this method
		/// returns, isVisible() will always return the new state, even if
		/// there is a transition.
		/// </summary>
		/// <param name="visible"> Desired visibility state.</param>
		public void setVisible(bool visible)
		{
			// Make sure that any currently running transition is stopped.
			if (this.showTransition_ != null) this.showTransition_.stop();
			if (this.hideTransition_ != null) this.hideTransition_.stop();

			if (visible) {
				this.show_();
			}
			else {
				this.hide_();
			}
		}

		/// <summary>
		/// Repositions the popup according to the current state.
		/// Should be overriden by subclases.
		/// </summary>
		public virtual void reposition() { }

		/// <summary>
		/// Does the work to show the popup.
		/// </summary>
		private void show_()
		{
			// Ignore call if we are already showing.
			if (this.isVisible_) {
				return;
			}

			// Give derived classes and handlers a chance to customize popup.
			if (!this.onBeforeShow()) {
				return;
			}

			// Allow callers to set the element in the BEFORE_SHOW event.
			if (this.element_ == null) {
				throw new Exception("Caller must call setElement before trying to show the popup");
			}

			// Call reposition after onBeforeShow, as it may change the style and/or
			// content of the popup and thereby affecting the size which is used for the
			// viewport calculation.
			this.reposition();

			var doc = goog.dom.getOwnerDocument(this.element_);

			if (this.hideOnEscape_) {
				// Handle the escape keys.  Listen in the capture phase so that we can
				// stop the escape key from propagating to other elements.  For example,
				// if there is a popup within a dialog box, we want the popup to be
				// dismissed first, rather than the dialog.
				this.handler_.listen<events.BrowserEvent>(
					doc, goog.events.EventType.KEYDOWN, this.onDocumentKeyDown_, true);
			}

			// Set up event handlers.
			if (this.autoHide_) {
				// Even if the popup is not in the focused document, we want to
				// close it on mousedowns in the document it's in.
				this.handler_.listen<events.BrowserEvent>(
					doc, goog.events.EventType.MOUSEDOWN, this.onDocumentMouseDown_, true);

				if (goog.userAgent.IE) {
					// We want to know about deactivates/mousedowns on the document with focus
					// The top-level document won't get a deactivate event if the focus is
					// in an iframe and the deactivate fires within that iframe.
					// The active element in the top-level document will remain the iframe
					// itself.
					HTMLElement activeElement = null;

					try {
						activeElement = doc.ActiveElement;
					}
					catch (Exception) {
						// There is an IE browser bug which can cause just the reading of
						// document.activeElement to throw an Unspecified Error.  This
						// may have to do with loading a popup within a hidden iframe.
					}
					while (activeElement != null &&
						   activeElement.NodeName == goog.dom.TagName.IFRAME) {
						DocumentInstance tempDoc;
						try {
							tempDoc = goog.dom.getFrameContentDocument(activeElement);
						}
						catch (Exception) {
							// The frame is on a different domain that its parent document
							// This way, we grab the lowest-level document object we can get
							// a handle on given cross-domain security.
							break;
						}
						doc = tempDoc;
						activeElement = doc.ActiveElement;
					}

					// Handle mousedowns in the focused document in case the user clicks
					// on the activeElement (in which case the popup should hide).
					this.handler_.listen<events.BrowserEvent>(
						doc, goog.events.EventType.MOUSEDOWN, this.onDocumentMouseDown_,
						true);

					// If the active element inside the focused document changes, then
					// we probably need to hide the popup.
					this.handler_.listen<events.BrowserEvent>(
						doc, goog.events.EventType.DEACTIVATE, this.onDocumentBlur_);

				}
				else {
					this.handler_.listen<events.BrowserEvent>(
						doc, goog.events.EventType.BLUR, this.onDocumentBlur_);
				}
			}

			// Make the popup visible.
			if (this.type_ == goog.ui.PopupBase.Type.TOGGLE_DISPLAY) {
				this.showPopupElement();
			}
			else if (this.type_ == goog.ui.PopupBase.Type.MOVE_OFFSCREEN) {
				this.reposition();
			}
			this.isVisible_ = true;

			this.lastShowTime_ = DateTime.Now.Ticks;
			this.lastHideTime_ = -1;

			// If there is transition to play, we play it and fire SHOW event after
			// the transition is over.
			if (this.showTransition_ != null) {
				goog.events.listenOnce(
					(goog.events.EventTarget)(this.showTransition_),
					goog.fx.Transition.EventType.END, new Action(this.onShow), false, this);
				this.showTransition_.play();
			}
			else {
				// Notify derived classes and handlers.
				this.onShow();
			}
		}


		/// <summary>
		/// Hides the popup. This call is idempotent.
		/// </summary>
		/// <param name="opt_target"> Target of the event causing the hide.</param>
		/// <returns>Whether the popup was hidden and not cancelled.</returns>
		private bool hide_(Node opt_target = null)
		{
			// Give derived classes and handlers a chance to cancel hiding.
			if (!this.isVisible_ || !this.onBeforeHide(opt_target)) {
				return false;
			}

			// Remove any listeners we attached when showing the popup.
			if (this.handler_ != null) {
				this.handler_.removeAll();
			}

			// Set visibility to hidden even if there is a transition.
			this.isVisible_ = false;
			this.lastHideTime_ = DateTime.Now.Ticks;

			// If there is transition to play, we play it and only hide the element
			// (and fire HIDE event) after the transition is over.
			if (this.hideTransition_ != null) {
				goog.events.listenOnce(
					(goog.events.EventTarget)(this.hideTransition_),
					goog.fx.Transition.EventType.END,
					new Action(() => { this.continueHidingPopup_(opt_target); }), false, this);
				this.hideTransition_.play();
			}
			else {
				this.continueHidingPopup_(opt_target);
			}

			return true;
		}


		/// <summary>
		/// Continues hiding the popup. This is a continuation from hide_. It is
		/// a separate method so that we can add a transition before hiding.
		/// </summary>
		/// <param name="opt_target"> Target of the event causing the hide.</param>
		private void continueHidingPopup_(Node opt_target = null)
		{
			// Hide the popup.
			if (this.type_ == goog.ui.PopupBase.Type.TOGGLE_DISPLAY) {
				if (this.shouldHideAsync_) {
					goog.Timer.callOnce(this.hidePopupElement, 0, this);
				}
				else {
					this.hidePopupElement();
				}
			}
			else if (this.type_ == goog.ui.PopupBase.Type.MOVE_OFFSCREEN) {
				this.moveOffscreen_();
			}

			// Notify derived classes and handlers.
			this.onHide(opt_target);
		}


		/// <summary>
		/// Shows the popup element.
		/// </summary>
		protected void showPopupElement()
		{
			this.element_.Style.Visibility = Visibility.Visible;
			goog.style.setElementShown(this.element_, true);
		}


		/// <summary>
		/// Hides the popup element.
		/// </summary>
		protected void hidePopupElement()
		{
			this.element_.Style.Visibility = Visibility.Hidden;
			goog.style.setElementShown(this.element_, false);
		}


		/// <summary>
		/// Hides the popup by moving it offscreen.
		/// </summary>
		private void moveOffscreen_()
		{
			this.element_.Style.Top = "-10000px";
		}


		/// <summary>
		/// Called before the popup is shown. Derived classes can override to hook this
		/// event but should make sure to call the parent class method.
		/// </summary>
		/// <returns>If anyone called preventDefault on the event object (or
		///     if any of the handlers returns false this will also return false.</returns>
		protected bool onBeforeShow()
		{
			return this.dispatchEvent(goog.ui.PopupBase.EventType.BEFORE_SHOW);
		}

		/// <summary>
		/// Called after the popup is shown. Derived classes can override to hook this
		/// event but should make sure to call the parent class method.
		/// </summary>
		protected void onShow()
		{
			this.dispatchEvent(goog.ui.PopupBase.EventType.SHOW);
		}


		/// <summary>
		/// Called before the popup is hidden. Derived classes can override to hook this
		/// event but should make sure to call the parent class method.
		/// </summary>
		/// <param name="opt_target"> Target of the event causing the hide.</param>
		/// <returns>If anyone called preventDefault on the event object (or
		///     if any of the handlers returns false this will also return false.</returns>
		protected bool onBeforeHide(Node opt_target = null)
		{
			return this.dispatchEvent(
				new events.Event(goog.ui.PopupBase.EventType.BEFORE_HIDE, opt_target));
		}


		/// <summary>
		/// Called after the popup is hidden. Derived classes can override to hook this
		/// event but should make sure to call the parent class method.
		/// </summary>
		/// <param name="opt_target"> Target of the event causing the hide.</param>
		protected void onHide(Node opt_target = null)
		{
			this.dispatchEvent(
				new events.Event(goog.ui.PopupBase.EventType.HIDE, opt_target));
		}


		/// <summary>
		/// Mouse down handler for the document on capture phase. Used to hide the
		/// popup for auto-hide mode.
		/// </summary>
		/// <param name="e"> The event object.</param>
		private void onDocumentMouseDown_(goog.events.BrowserEvent e)
		{
			var target = (Node)e.target;

			if (!goog.dom.contains(this.element_, target) &&
				!this.isOrWithinAutoHidePartner_(target) &&
				this.isWithinAutoHideRegion_(target) && !this.shouldDebounce_()) {
				// Mouse click was outside popup and partners, so hide.
				this.hide_(target);
			}
		}


		/// <summary>
		/// Handles key-downs on the document to handle the escape key.
		/// </summary>
		/// <param name="e"> The event object.</param>
		private void onDocumentKeyDown_(goog.events.BrowserEvent e)
		{
			if (e.keyCode == (int)goog.events.KeyCodes.ESC) {
				if (this.hide_((Node)e.target)) {
					// Eat the escape key, but only if this popup was actually closed.
					e.preventDefault();
					e.stopPropagation();
				}
			}
		}


		/// <summary>
		/// Deactivate handler(IE) and blur handler (other browsers) for document.
		/// Used to hide the popup for auto-hide mode.
		/// </summary>
		/// <param name="e"> The event object.</param>
		private void onDocumentBlur_(goog.events.BrowserEvent e)
		{
			if (!this.enableCrossIframeDismissal_) {
				return;
			}

			var doc = goog.dom.getOwnerDocument(this.element_);

			// Ignore blur events if the active element is still inside the popup or if
			// there is no longer an active element.  For example, a widget like a
			// goog.ui.Button might programatically blur itself before losing tabIndex.
			if (Document.ActiveElement != null) {
				var activeElement = doc.ActiveElement;
				if (activeElement == null || goog.dom.contains(this.element_, activeElement) ||
					activeElement.TagName == goog.dom.TagName.BODY) {
					return;
				}

				// Ignore blur events not for the document itself in non-IE browsers.
			}
			else if (e.target != doc) {
				return;
			}

			// Debounce the initial focus move.
			if (this.shouldDebounce_()) {
				return;
			}

			this.hide_();
		}


		/// <summary>
		/// </summary>
		/// <param name="element"> The element to inspect.</param>
		/// <returns>Returns true if the given element is one of the auto hide
		///     partners or is a child of an auto hide partner.</returns>
		private bool isOrWithinAutoHidePartner_(Node element)
		{
			if (this.autoHidePartners_ == null)
				return false;
			return this.autoHidePartners_.Find((partner) => {
				return element == partner || goog.dom.contains(partner, element);
			}) != null;
		}


		/// <summary>
		/// </summary>
		/// <param name="element"> The element to inspect.</param>
		/// <returns>Returns true if the element is contained within
		/// the autohide region. If unset, the autohide region is the entire
		/// entire document.</returns>
		private bool isWithinAutoHideRegion_(Node element)
		{
			return this.autoHideRegion_ != null ?
				goog.dom.contains(this.autoHideRegion_, element) :
				true;
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether the time since last show is less than the debounce
		///     delay.</returns>
		private bool shouldDebounce_()
		{
			return DateTime.Now.Ticks - this.lastShowTime_ < goog.ui.PopupBase.DEBOUNCE_DELAY_MS;
		}


		public override void disposeInternal()
		{
			base.disposeInternal();
			this.handler_.dispose();
			//goog.dispose(this.showTransition_);
			this.showTransition_ = null;
			//goog.dispose(this.hideTransition_);
			this.hideTransition_ = null;
			Script.Delete(ref this.element_);
			Script.Delete(ref this.handler_);
			Script.Delete(ref this.autoHidePartners_);
		}
	}
}
