// Copyright 2009 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview Base class for containers that host {@link goog.ui.Control}s,
 * such as menus and toolbars.  Provides default keyboard and mouse event
 * handling and child management, based on a generalized version of
 * {@link goog.ui.Menu}.
 *
 * @author attila@google.com (Attila Bodis)
 * @see ../demos/container.html
 */
// TODO(attila):  Fix code/logic duplication between this and goog.ui.Control.
// TODO(attila):  Maybe pull common stuff all the way up into Component...?
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class Container : Component
	{
		/// <summary>
		/// Base class for containers.  Extends {@link goog.ui.Component} by adding
		/// the following:
		///  <ul>
		///    <li>a {@link goog.events.KeyHandler}, to simplify keyboard handling,
		///    <li>a pluggable <em>renderer</em> framework, to simplify the creation of
		///        containers without the need to subclass this class,
		///    <li>methods to manage child controls hosted in the container,
		///    <li>default mouse and keyboard event handling methods.
		///  </ul>
		/// </summary>
		/// <param name="opt_orientation">Container
		///     orientation; defaults to {@code VERTICAL}.</param>
		/// <param name="opt_renderer">Renderer used to render or
		///     decorate the container; defaults to {@link goog.ui.ContainerRenderer}.</param>
		/// <param name="opt_domHelper">DOM helper, used for document
		///     interaction.</param>
		public Container(goog.ui.Container.Orientation opt_orientation = (Orientation)0,
			goog.ui.ContainerRenderer opt_renderer = null, goog.dom.DomHelper opt_domHelper = null)
			: base(opt_domHelper)
		{
			this.renderer_ = opt_renderer ?? goog.ui.ContainerRenderer.getInstance();
			this.orientation_ = opt_orientation != (Orientation)0 ? opt_orientation : this.renderer_.getDefaultOrientation();
		}

		/// <summary>
		/// Container-specific events.
		/// </summary>
		public new class EventType
		{
			/// <summary>
			/// Dispatched after a goog.ui.Container becomes visible. Non-cancellable.
			/// NOTE(user): This event really shouldn't exist, because the
			/// goog.ui.Component.EventType.SHOW event should behave like this one. But the
			/// SHOW event for containers has been behaving as other components"
			/// BEFORE_SHOW event for a long time, and too much code relies on that old
			/// behavior to fix it now.
			/// </summary>
			public const string AFTER_SHOW = "aftershow";
			/// <summary>
			/// Dispatched after a goog.ui.Container becomes invisible. Non-cancellable.
			/// </summary>
			public const string AFTER_HIDE = "afterhide";
		}

		/// <summary>
		/// Container orientation constants.
		/// </summary>
		public enum Orientation
		{
			HORIZONTAL = 1,
			VERTICAL
		}

		/// <summary>
		/// Allows an alternative element to be set to receive key events, otherwise
		/// defers to the renderer's element choice.
		/// </summary>
		protected HTMLElement keyEventTarget_;

		/// <summary>
		/// Keyboard event handler.
		/// </summary>
		private events.KeyHandler keyHandler_;

		/// <summary>
		/// Renderer for the container.  Defaults to {@link goog.ui.ContainerRenderer}.
		/// </summary>
		private ContainerRenderer renderer_;

		/// <summary>
		/// Container orientation; determines layout and default keyboard navigation.
		/// </summary>
		private Orientation orientation_;

		/// <summary>
		/// Whether the container is set to be visible.  Defaults to true.
		/// </summary>
		private bool visible_ = true;

		/// <summary>
		/// Whether the container is enabled and reacting to keyboard and mouse events.
		/// Defaults to true.
		/// </summary>
		private bool enabled_ = true;

		/// <summary>
		/// Whether the container supports keyboard focus.  Defaults to true.  Focusable
		/// containers have a {@code tabIndex} and can be navigated to via the keyboard.
		/// </summary>
		private bool focusable_ = true;

		/// <summary>
		/// The 0-based index of the currently highlighted control in the container
		/// (-1 if none).
		/// </summary>
		private int highlightedIndex_ = -1;

		/// <summary>
		/// The currently open (expanded) control in the container (null if none).
		/// </summary>
		protected Control openItem_;

		/// <summary>
		/// Whether the mouse button is held down.  Defaults to false.  This flag is set
		/// when the user mouses down over the container, and remains set until they
		/// release the mouse button.
		/// </summary>
		private bool mouseButtonPressed_;

		/// <summary>
		/// Whether focus of child components should be allowed.  Only effective if
		/// focusable_ is set to false.
		/// </summary>
		private bool allowFocusableChildren_;

		/// <summary>
		/// Whether highlighting a child component should also open it.
		/// </summary>
		private bool openFollowsHighlight_;

		/// <summary>
		/// Map of DOM IDs to child controls.  Each key is the DOM ID of a child
		/// control's root element; each value is a reference to the child control
		/// itself.  Used for looking up the child control corresponding to a DOM
		/// node in O(1) time.
		/// </summary>
		protected Dictionary<string, Control> childElementIdMap_;

		// Event handler and renderer management.

		/// <summary>
		/// Returns the DOM element on which the container is listening for keyboard
		/// events (null if none).
		/// </summary>
		/// <returns>Element on which the container is listening for key
		/// events.<returns>
		public HTMLElement getKeyEventTarget()
		{
			// Delegate to renderer, unless we've set an explicit target.
			return this.keyEventTarget_ ?? this.renderer_.getKeyEventTarget(this);
		}

		/// <summary>
		/// Attaches an element on which to listen for key events.
		/// </summary>
		/// <param name="element">The element to attach, or null/undefined</param>
		///     to attach to the default element.
		public void setKeyEventTarget(HTMLElement element)
		{
			if (this.focusable_) {
				var oldTarget = this.getKeyEventTarget();
				var inDocument = this.isInDocument();

				this.keyEventTarget_ = element;
				var newTarget = this.getKeyEventTarget();

				if (inDocument) {
					// Unlisten for events on the old key target.  Requires us to reset
					// key target state temporarily.
					this.keyEventTarget_ = oldTarget;
					this.enableFocusHandling_(false);
					this.keyEventTarget_ = element;

					// Listen for events on the new key target.
					this.getKeyHandler().attach(newTarget);
					this.enableFocusHandling_(true);
				}
			}
			else {
				throw new Exception(
					"Can\'t set key event target for container " +
					"that doesn\'t support keyboard focus!");
			}
		}

		/// <summary>
		/// Returns the keyboard event handler for this container, lazily created the
		/// first time this method is called.  The keyboard event handler listens for
		/// keyboard events on the container's key event target, as determined by its
		/// renderer.
		/// </summary>
		/// <returns>Keyboard event handler for this container.</returns>
		public goog.events.KeyHandler getKeyHandler()
		{
			return this.keyHandler_ ??
				(this.keyHandler_ = new goog.events.KeyHandler(this.getKeyEventTarget()));
		}

		/// <summary>
		/// Returns the renderer used by this container to render itself or to decorate
		/// an existing element.
		/// </summary>
		/// <returns>Renderer used by the container.</returns>
		public goog.ui.ContainerRenderer getRenderer()
		{
			return this.renderer_;
		}
		/// <summary>
		/// Registers the given renderer with the container.  Changing renderers after
		/// the container has already been rendered or decorated is an error.
		/// </summary>
		/// <param name="renderer">Renderer used by the container.</param>
		public void setRenderer(goog.ui.ContainerRenderer renderer)
		{
			if (this.getElement() != null) {
				// Too late.
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			this.renderer_ = renderer;
		}

		// Standard goog.ui.Component implementation.

		/// <summary>
		/// Creates the container's DOM.
		/// </summary>
		public override void createDom()
		{
			// Delegate to renderer.
			this.setElementInternal(this.renderer_.createDom(this));
		}

		/// <summary>
		/// Returns the DOM element into which child components are to be rendered,
		/// or null if the container itself hasn't been rendered yet.  Overrides
		/// {@link goog.ui.Component#getContentElement} by delegating to the renderer.
		/// </summary>
		/// <returns>Element to contain child elements (null if none).</returns>
		public override HTMLElement getContentElement()
		{
			// Delegate to renderer.
			return this.renderer_.getContentElement(this.getElement());
		}

		/// <summary>
		/// Returns true if the given element can be decorated by this container.
		/// Overrides {@link goog.ui.Component#canDecorate}.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>True iff the element can be decorated.</returns>
		public override bool canDecorate(Element element)
		{
			// Delegate to renderer.
			return this.renderer_.canDecorate(element);
		}

		/// <summary>
		/// Decorates the given element with this container. Overrides {@link
		/// goog.ui.Component#decorateInternal}.  Considered protected.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		protected override void decorateInternal(HTMLElement element)
		{
			// Delegate to renderer.
			this.setElementInternal(this.renderer_.decorate(this, element));
			// Check whether the decorated element is explicitly styled to be invisible.
			if (element.Style.Display == Display.None) {
				this.visible_ = false;
			}
		}

		/// <summary>
		/// Configures the container after its DOM has been rendered, and sets up event
		/// handling.  Overrides {@link goog.ui.Component#enterDocument}.
		/// </summary>
		public override void enterDocument()
		{
			base.enterDocument();

			this.forEachChild((child) => {
				if (child.isInDocument()) {
					this.registerChildId_(child);
				}
			});

			var elem = this.getElement();

			// Call the renderer's initializeDom method to initialize the container's DOM.
			this.renderer_.initializeDom(this);

			// Initialize visibility (opt_force = true, so we don't dispatch events).
			this.setVisible(this.visible_, true);

			// Handle events dispatched by child controls.
			this.getHandler()
				.listen(this, goog.ui.Component.EventType.ENTER, new Func<events.Event, bool>(this.handleEnterItem))
				.listen(
					this, goog.ui.Component.EventType.HIGHLIGHT, new Action<events.Event>(this.handleHighlightItem))
				.listen(
					this, goog.ui.Component.EventType.UNHIGHLIGHT,
					new Action<events.Event>(this.handleUnHighlightItem))
				.listen(this, goog.ui.Component.EventType.OPEN, new Action<events.Event>(this.handleOpenItem))
				.listen(this, goog.ui.Component.EventType.CLOSE, new Action<events.Event>(this.handleCloseItem))
				.

				// Handle mouse events.
				listen(elem, goog.events.EventType.MOUSEDOWN, new Action<goog.events.BrowserEvent>(this.handleMouseDown))
				.listen(
					goog.dom.getOwnerDocument(elem), goog.events.EventType.MOUSEUP,
					new Action<goog.events.BrowserEvent>(this.handleDocumentMouseUp))
				.

				// Handle mouse events on behalf of controls in the container.
				listen(
					elem, new JsArray<string> {
						goog.events.EventType.MOUSEDOWN, goog.events.EventType.MOUSEUP,
						goog.events.EventType.MOUSEOVER, goog.events.EventType.MOUSEOUT,
						goog.events.EventType.CONTEXTMENU
					},
					new Action<events.BrowserEvent>(this.handleChildMouseEvents));

			// If the container is focusable, set up keyboard event handling.
			if (this.isFocusable()) {
				this.enableFocusHandling_(true);
			}
		}

		/// <summary>
		/// Sets up listening for events applicable to focusable containers.
		/// </summary>
		/// <param name="enable">Whether to enable or disable focus handling.</param>
		private void enableFocusHandling_(bool enable)
		{
			var handler = this.getHandler();
			var keyTarget = this.getKeyEventTarget();
			if (enable) {
				handler.listen(keyTarget, goog.events.EventType.FOCUS, new Action<goog.events.BrowserEvent>(this.handleFocus))
					.listen(keyTarget, goog.events.EventType.BLUR, new Action<goog.events.BrowserEvent>(this.handleBlur))
					.listen(
						this.getKeyHandler(), goog.events.KeyHandler.EventType.KEY,
						new Func<goog.events.KeyEvent, bool>(this.handleKeyEvent));
			}
			else {
				handler.unlisten(keyTarget, goog.events.EventType.FOCUS, new Action<goog.events.BrowserEvent>(this.handleFocus))
					.unlisten(keyTarget, goog.events.EventType.BLUR, new Action<goog.events.BrowserEvent>(this.handleBlur))
					.unlisten(
						this.getKeyHandler(), goog.events.KeyHandler.EventType.KEY,
						new Func<goog.events.KeyEvent, bool>(this.handleKeyEvent));
			}
		}

		/// <summary>
		/// Cleans up the container before its DOM is removed from the document, and
		/// removes event handlers.  Overrides {@link goog.ui.Component#exitDocument}.
		/// </summary>
		public override void exitDocument()
		{
			// {@link #setHighlightedIndex} has to be called before
			// {@link goog.ui.Component#exitDocument}, otherwise it has no effect.
			this.setHighlightedIndex(-1);

			if (this.openItem_ != null) {
				this.openItem_.setOpen(false);
			}

			this.mouseButtonPressed_ = false;

			base.exitDocument();
		}

		public override void disposeInternal()
		{
			base.disposeInternal();

			if (this.keyHandler_ != null) {
				this.keyHandler_.dispose();
				this.keyHandler_ = null;
			}

			this.keyEventTarget_ = null;
			this.childElementIdMap_ = null;
			this.openItem_ = null;
			this.renderer_ = null;
		}

		// Default event handlers.

		/// <summary>
		/// Handles ENTER events raised by child controls when they are navigated to.
		/// </summary>
		/// <param name="e">ENTER event to handle.</param>
		/// <returns>Whether to prevent handleMouseOver from handling
		/// the event.<returns>
		public virtual bool handleEnterItem(goog.events.Event e)
		{
			// Allow the Control to highlight itself.
			return true;
		}

		/// <summary>
		/// Handles HIGHLIGHT events dispatched by items in the container when
		/// they are highlighted.
		/// </summary>
		/// <param name="e">Highlight event to handle.</param>
		public void handleHighlightItem(goog.events.Event e)
		{
			Control control = (goog.ui.Control)e.target;
			var index = this.indexOfChild(control);
			if (index > -1 && index != this.highlightedIndex_) {
				var item = this.getHighlighted();
				if (item != null) {
					// Un-highlight previously highlighted item.
					item.setHighlighted(false);
				}

				this.highlightedIndex_ = index;
				item = this.getHighlighted();

				if (this.isMouseButtonPressed()) {
					// Activate item when mouse button is pressed, to allow MacOS-style
					// dragging to choose menu items.  Although this should only truly
					// happen if the highlight is due to mouse movements, there is little
					// harm in doing it for keyboard or programmatic highlights.
					item.setActive(true);
				}

				// Update open item if open item needs follow highlight.
				if (this.openFollowsHighlight_ && this.openItem_ != null &&
					item != this.openItem_) {
					if (item.isSupportedState(goog.ui.Component.State.OPENED)) {
						item.setOpen(true);
					}
					else {
						this.openItem_.setOpen(false);
					}
				}
			}

			var element = this.getElement();
			goog.asserts.assert(
				element != null, "The DOM element for the container cannot be null.");
			if (control.getElement() != null) {
				goog.a11y.aria.setState(
					element, goog.a11y.aria.State.ACTIVEDESCENDANT,
					control.getElement().Id);
			}
		}

		/// <summary>
		/// Handles UNHIGHLIGHT events dispatched by items in the container when
		/// they are unhighlighted.
		/// </summary>
		/// <param name="e">Unhighlight event to handle.</param>
		public void handleUnHighlightItem(goog.events.Event e)
		{
			if (e.target == this.getHighlighted()) {
				this.highlightedIndex_ = -1;
			}
			var element = this.getElement();
			goog.asserts.assert(
				element != null, "The DOM element for the container cannot be null.");
			// Setting certain ARIA attributes to empty strings is problematic.
			// Just remove the attribute instead.
			goog.a11y.aria.removeState(element, goog.a11y.aria.State.ACTIVEDESCENDANT);
		}

		/// <summary>
		/// Handles OPEN events dispatched by items in the container when they are
		/// opened.
		/// </summary>
		/// <param name="e">Open event to handle.</param>
		public void handleOpenItem(goog.events.Event e)
		{
			var item = (goog.ui.Control)(e.target);
			if (item != null && item != this.openItem_ && item.getParent() == this) {
				if (this.openItem_ != null) {
					this.openItem_.setOpen(false);
				}
				this.openItem_ = item;
			}
		}

		/// <summary>
		/// Handles CLOSE events dispatched by items in the container when they are
		/// closed.
		/// </summary>
		/// <param name="e">Close event to handle.</param>
		public void handleCloseItem(goog.events.Event e)
		{
			if (e.target == this.openItem_) {
				this.openItem_ = null;
			}

			var control = (Control)e.target;
			var element = this.getElement();
			var targetEl = control.getElement();
			// Set the active descendant to the menu item when its submenu is closed and
			// it is still highlighted. This can sometimes be called when the menuitem is
			// unhighlighted because the focus moved elsewhere, do nothing at that point.
			if (element != null && control.isHighlighted() && targetEl != null) {
				goog.a11y.aria.setActiveDescendant(element, targetEl);
			}
		}

		/// <summary>
		/// Handles mousedown events over the container.  The default implementation
		/// sets the "mouse button pressed" flag and, if the container is focusable,
		/// grabs keyboard focus.
		/// </summary>
		/// <param name="e">Mousedown event to handle.</param>
		public void handleMouseDown(goog.events.BrowserEvent e)
		{
			if (this.enabled_) {
				this.setMouseButtonPressed(true);
			}

			var keyTarget = this.getKeyEventTarget();
			if (keyTarget != null && goog.dom.isFocusableTabIndex(keyTarget)) {
				// The container is configured to receive keyboard focus.
				keyTarget.Focus();
			}
			else {
				// The control isn't configured to receive keyboard focus; prevent it
				// from stealing focus or destroying the selection.
				e.preventDefault();
			}
		}

		/// <summary>
		/// Handles mouseup events over the document.  The default implementation
		/// clears the "mouse button pressed" flag.
		/// </summary>
		/// <param name="e">Mouseup event to handle.</param>
		public void handleDocumentMouseUp(goog.events.BrowserEvent e)
		{
			this.setMouseButtonPressed(false);
		}

		/// <summary>
		/// Handles mouse events originating from nodes belonging to the controls hosted
		/// in the container.  Locates the child control based on the DOM node that
		/// dispatched the event, and forwards the event to the control for handling.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public void handleChildMouseEvents(goog.events.BrowserEvent e)
		{
			var control = this.getOwnerControl((Node)e.target);
			if (control != null) {
				// Child control identified; forward the event.
				switch (e.type) {
				case goog.events.EventType.MOUSEDOWN:
					control.handleMouseDown(e);
					break;
				case goog.events.EventType.MOUSEUP:
					control.handleMouseUp(e);
					break;
				case goog.events.EventType.MOUSEOVER:
					control.handleMouseOver(e);
					break;
				case goog.events.EventType.MOUSEOUT:
					control.handleMouseOut(e);
					break;
				case goog.events.EventType.CONTEXTMENU:
					control.handleContextMenu(e);
					break;
				}
			}
		}

		/// <summary>
		/// Returns the child control that owns the given DOM node, or null if no such
		/// control is found.
		/// </summary>
		/// <param name="node">DOM node whose owner is to be returned.</param>
		/// <returns>Control hosted in the container to which the node
		/// belongs (if found).<returns>
		public Control getOwnerControl(Node node)
		{
			// Ensure that this container actually has child controls before
			// looking up the owner.
			if (this.childElementIdMap_ != null) {
				var elem = this.getElement();
				// See http://b/2964418 . IE9 appears to evaluate '!=' incorrectly, so
				// using '!==' instead.
				// TODO(user): Possibly revert this change if/when IE9 fixes the issue.
				while (node != null && node != elem) {
					var id = ((Element)node).Id;
					if (id != null && this.childElementIdMap_.ContainsKey(id)) {
						return this.childElementIdMap_[id];
					}
					node = node.ParentNode;
				}
			}
			return null;
		}

		/// <summary>
		/// Handles focus events raised when the container's key event target receives
		/// keyboard focus.
		/// </summary>
		/// <param name="e">Focus event to handle.</param>
		public virtual void handleFocus(goog.events.BrowserEvent arg)
		{
			// No-op in the base class.
		}

		/// <summary>
		/// Handles blur events raised when the container's key event target loses
		/// keyboard focus.  The default implementation clears the highlight index.
		/// </summary>
		/// <param name="e">Blur event to handle.</param>
		public void handleBlur(goog.events.BrowserEvent arg)
		{
			this.setHighlightedIndex(-1);
			this.setMouseButtonPressed(false);
			// If the container loses focus, and one of its children is open, close it.
			if (this.openItem_ != null) {
				this.openItem_.setOpen(false);
			}
		}

		/// <summary>
		/// Attempts to handle a keyboard event, if the control is enabled, by calling
		/// {@link handleKeyEventInternal}.  Considered protected; should only be used
		/// within this package and by subclasses.
		/// </summary>
		/// <param name="arg">Key event to handle.</param>
		public bool handleKeyEvent(goog.events.KeyEvent e)
		{
			if (this.isEnabled() && this.isVisible() &&
				(this.getChildCount() != 0 || this.keyEventTarget_ != null) &&
				this.handleKeyEventInternal(e)) {
				e.preventDefault();
				e.stopPropagation();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Attempts to handle a keyboard event, if the control is enabled, by calling
		/// {@link handleKeyEventInternal}.  Considered protected; should only be used
		/// within this package and by subclasses.
		/// </summary>
		/// <param name="e">Key event to handle.</param>
		/// <returns>Whether the key event was handled.</returns>
		public virtual bool handleKeyEventInternal(goog.events.KeyEvent e)
		{
			// Give the highlighted control the chance to handle the key event.
			var highlighted = this.getHighlighted();
			if (highlighted != null && /*typeof highlighted.handleKeyEvent == "function" &&*/
				highlighted.handleKeyEvent(e)) {
				return true;
			}

			// Give the open control the chance to handle the key event.
			if (this.openItem_ != null && this.openItem_ != highlighted &&
				/*typeof this.openItem_.handleKeyEvent == "function" &&*/
				this.openItem_.handleKeyEvent(e)) {
				return true;
			}

			// Do not handle the key event if any modifier key is pressed.
			if (e.shiftKey || e.ctrlKey || e.metaKey || e.altKey) {
				return false;
			}

			// Either nothing is highlighted, or the highlighted control didn't handle
			// the key event, so attempt to handle it here.
			switch ((goog.events.KeyCodes)e.keyCode) {
			case goog.events.KeyCodes.ESC:
				if (this.isFocusable()) {
					this.getKeyEventTarget().Blur();
				}
				else {
					return false;
				}
				break;

			case goog.events.KeyCodes.HOME:
				this.highlightFirst();
				break;

			case goog.events.KeyCodes.END:
				this.highlightLast();
				break;

			case goog.events.KeyCodes.UP:
				if (this.orientation_ == goog.ui.Container.Orientation.VERTICAL) {
					this.highlightPrevious();
				}
				else {
					return false;
				}
				break;

			case goog.events.KeyCodes.LEFT:
				if (this.orientation_ == goog.ui.Container.Orientation.HORIZONTAL) {
					if (this.isRightToLeft()) {
						this.highlightNext();
					}
					else {
						this.highlightPrevious();
					}
				}
				else {
					return false;
				}
				break;

			case goog.events.KeyCodes.DOWN:
				if (this.orientation_ == goog.ui.Container.Orientation.VERTICAL) {
					this.highlightNext();
				}
				else {
					return false;
				}
				break;

			case goog.events.KeyCodes.RIGHT:
				if (this.orientation_ == goog.ui.Container.Orientation.HORIZONTAL) {
					if (this.isRightToLeft()) {
						this.highlightPrevious();
					}
					else {
						this.highlightNext();
					}
				}
				else {
					return false;
				}
				break;

			default:
				return false;
			}

			return true;
		}

		// Child component management.

		/// <summary>
		/// Creates a DOM ID for the child control and registers it to an internal
		/// hash table to be able to find it fast by id.
		/// </summary>
		/// <param name="child">The child control. Its root element has</param>
		///     to be created yet.
		private void registerChildId_(goog.ui.Component child)
		{
			// Map the DOM ID of the control's root element to the control itself.
			var childElem = child.getElement();

			// If the control's root element doesn't have a DOM ID assign one.
			var id = childElem.Id ?? (childElem.Id = child.getId());

			// Lazily create the child element ID map on first use.
			if (this.childElementIdMap_ == null) {
				this.childElementIdMap_ = new Dictionary<string, Control>();
			}
			this.childElementIdMap_[id] = (Control)child;
		}

		/// <summary>
		/// Adds the specified control as the last child of this container.  See
		/// {@link goog.ui.Container#addChildAt} for detailed semantics.
		/// </summary>
		/// <param name="child">The new child control.</param>
		/// <param name="opt_render">Whether the new child should be rendered</param>
		///     immediately after being added (defaults to false).
		public override void addChild(Component child, bool opt_render = false)
		{
			goog.asserts.assertInstanceof(
				child, typeof(goog.ui.Control), "The child of a container must be a control");
			base.addChild(child, opt_render);
		}

		/// <summary>
		/// Adds the control as a child of this container at the given 0-based index.
		/// Overrides {@link goog.ui.Component#addChildAt} by also updating the
		/// container's highlight index.  Since {@link goog.ui.Component#addChild} uses
		/// {@link #addChildAt} internally, we only need to override this method.
		/// </summary>
		/// <param name="control">New child.</param>
		/// <param name="index">Index at which the new child is to be added.</param>
		/// <param name="opt_render">Whether the new child should be rendered</param>
		///     immediately after being added (defaults to false).
		public override void addChildAt(goog.ui.Component component, int index, bool opt_render = false)
		{
			var control = (goog.ui.Control)component;

			// Make sure the child control dispatches HIGHLIGHT, UNHIGHLIGHT, OPEN, and
			// CLOSE events, and that it doesn't steal keyboard focus.
			control.setDispatchTransitionEvents(goog.ui.Component.State.HOVER, true);
			control.setDispatchTransitionEvents(goog.ui.Component.State.OPENED, true);
			if (this.isFocusable() || !this.isFocusableChildrenAllowed()) {
				control.setSupportedState(goog.ui.Component.State.FOCUSED, false);
			}

			// Disable mouse event handling by child controls.
			control.setHandleMouseEvents(false);

			var srcIndex =
				(control.getParent() == this) ? this.indexOfChild(control) : -1;

			// Let the superclass implementation do the work.
			base.addChildAt(control, index, opt_render);

			if (control.isInDocument() && this.isInDocument()) {
				this.registerChildId_(control);
			}

			this.updateHighlightedIndex_(srcIndex, index);
		}

		/// <summary>
		/// Updates the highlighted index when children are added or moved.
		/// </summary>
		/// <param name="fromIndex">Index of the child before it was moved, or -1 if</param>
		///     the child was added.
		/// <param name="toIndex">Index of the child after it was moved or added.</param>
		private void updateHighlightedIndex_(
			int fromIndex, int toIndex)
		{
			if (fromIndex == -1) {
				fromIndex = this.getChildCount();
			}
			if (fromIndex == this.highlightedIndex_) {
				// The highlighted element itself was moved.
				this.highlightedIndex_ = Math.Min(this.getChildCount() - 1, toIndex);
			}
			else if (
			  fromIndex > this.highlightedIndex_ && toIndex <= this.highlightedIndex_) {
				// The control was added or moved behind the highlighted index.
				this.highlightedIndex_++;
			}
			else if (
			  fromIndex < this.highlightedIndex_ && toIndex > this.highlightedIndex_) {
				// The control was moved from before to behind the highlighted index.
				this.highlightedIndex_--;
			}
		}

		/// <summary>
		/// Removes a child control.  Overrides {@link goog.ui.Component#removeChild} by
		/// updating the highlight index.  Since {@link goog.ui.Component#removeChildAt}
		/// uses {@link #removeChild} internally, we only need to override this method.
		/// </summary>
		/// <param name="control">The ID of the child to remove, or</param>
		///     the control itself.
		/// <param name="opt_unrender">Whether to call {@code exitDocument} on the</param>
		///     removed control, and detach its DOM from the document (defaults to
		///     false).
		/// <returns>The removed control, if any.</returns>
		public override goog.ui.Component removeChild(Union<string, goog.ui.Component> component, bool opt_unrender = false)
		{
			var control = (goog.ui.Control)(component.Is<string>() ? this.getChild(component.As<string>()) : component.As<Component>());

			if (control != null) {
				var index = this.indexOfChild(control);
				if (index != -1) {
					if (index == this.highlightedIndex_) {
						control.setHighlighted(false);
						this.highlightedIndex_ = -1;
					}
					else if (index < this.highlightedIndex_) {
						this.highlightedIndex_--;
					}
				}

				// Remove the mapping from the child element ID map.
				var childElem = control.getElement();
				if (childElem != null && childElem.Id != null && this.childElementIdMap_ != null) {
					this.childElementIdMap_.Remove(childElem.Id);
				}
			}

			control = (goog.ui.Control)(
				base.removeChild(control, opt_unrender));

			// Re-enable mouse event handling (in case the control is reused elsewhere).
			control.setHandleMouseEvents(true);

			return control;
		}

		// Container state management.

		/// <summary>
		/// Returns the container's orientation.
		/// </summary>
		/// <returns>Container orientation.</returns>
		public goog.ui.Container.Orientation getOrientation()
		{
			return this.orientation_;
		}

		/// <summary>
		/// Sets the container's orientation.
		/// </summary>
		/// <param name="orientation">Container orientation.</param>
		// TODO(attila): Do we need to support containers with dynamic orientation?
		public void setOrientation(goog.ui.Container.Orientation orientation)
		{
			if (this.getElement() != null) {
				// Too late.
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			this.orientation_ = orientation;
		}

		/// <summary>
		/// Returns true if the container's visibility is set to visible, false if
		/// it is set to hidden.  A container that is set to hidden is guaranteed
		/// to be hidden from the user, but the reverse isn't necessarily true.
		/// A container may be set to visible but can otherwise be obscured by another
		/// element, rendered off-screen, or hidden using direct CSS manipulation.
		/// </summary>
		/// <returns>Whether the container is set to be visible.</returns>
		public bool isVisible()
		{
			return this.visible_;
		}

		/// <summary>
		/// Shows or hides the container.  Does nothing if the container already has
		/// the requested visibility.  Otherwise, dispatches a SHOW or HIDE event as
		/// appropriate, giving listeners a chance to prevent the visibility change.
		/// </summary>
		/// <param name="visible">Whether to show or hide the container.</param>
		/// <param name="opt_force">If true, doesn't check whether the container</param>
		///     already has the requested visibility, and doesn't dispatch any events.
		/// <returns>Whether the visibility was changed.</returns>
		public virtual bool setVisible(bool visible, bool opt_force = false)
		{
			if (opt_force || (this.visible_ != visible &&
							  this.dispatchEvent(
								  visible ? goog.ui.Component.EventType.SHOW :
											goog.ui.Component.EventType.HIDE))) {
				this.visible_ = visible;

				var elem = this.getElement();
				if (elem != null) {
					goog.style.setElementShown(elem, visible);
					if (this.isFocusable()) {
						// Enable keyboard access only for enabled & visible containers.
						this.renderer_.enableTabIndex(
							this.getKeyEventTarget(), this.enabled_ && this.visible_);
					}
					if (!opt_force) {
						this.dispatchEvent(
							this.visible_ ? goog.ui.Container.EventType.AFTER_SHOW :
											goog.ui.Container.EventType.AFTER_HIDE);
					}
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the container is enabled, false otherwise.
		/// </summary>
		/// <returns>Whether the container is enabled.</returns>
		internal bool isEnabled()
		{
			return this.enabled_;
		}

		/// <summary>
		/// Enables/disables the container based on the {@code enable} argument.
		/// Dispatches an {@code ENABLED} or {@code DISABLED} event prior to changing
		/// the container's state, which may be caught and canceled to prevent the
		/// container from changing state.  Also enables/disables child controls.
		/// </summary>
		/// <param name="enable">Whether to enable or disable the container.</param>
		public void setEnabled(bool enable)
		{
			if (this.enabled_ != enable &&
				this.dispatchEvent(
					enable ? goog.ui.Component.EventType.ENABLE :
							 goog.ui.Component.EventType.DISABLE)) {
				if (enable) {
					// Flag the container as enabled first, then update children.  This is
					// because controls can't be enabled if their parent is disabled.
					this.enabled_ = true;
					this.forEachChild((child) => {
						// Enable child control unless it is flagged.
						if (((Control)child).wasDisabled) {
							((Control)child).wasDisabled = false;
						}
						else {
							((Control)child).setEnabled(true);
						}
					});
				}
				else {
					// Disable children first, then flag the container as disabled.  This is
					// because controls can't be disabled if their parent is already disabled.
					this.forEachChild((child) => {
						// Disable child control, or flag it if it's already disabled.
						if (((Control)child).isEnabled()) {
							((Control)child).setEnabled(false);
						}
						else {
							((Control)child).wasDisabled = true;
						}
					});
					this.enabled_ = false;
					this.setMouseButtonPressed(false);
				}

				if (this.isFocusable()) {
					// Enable keyboard access only for enabled & visible components.
					this.renderer_.enableTabIndex(
						this.getKeyEventTarget(), enable && this.visible_);
				}
			}
		}

		/// <summary>
		/// Returns true if the container is focusable, false otherwise.  The default
		/// is true.  Focusable containers always have a tab index and allocate a key
		/// handler to handle keyboard events while focused.
		/// </summary>
		/// <returns>Whether the component is focusable.</returns>
		public bool isFocusable()
		{
			return this.focusable_;
		}

		/// <summary>
		/// Sets whether the container is focusable.  The default is true.  Focusable
		/// containers always have a tab index and allocate a key handler to handle
		/// keyboard events while focused.
		/// </summary>
		/// <param name="focusable">Whether the component is to be focusable.</param>
		public void setFocusable(bool focusable)
		{
			if (focusable != this.focusable_ && this.isInDocument()) {
				this.enableFocusHandling_(focusable);
			}
			this.focusable_ = focusable;
			if (this.enabled_ && this.visible_) {
				this.renderer_.enableTabIndex(this.getKeyEventTarget(), focusable);
			}
		}

		/// <summary>
		/// Returns true if the container allows children to be focusable, false
		/// otherwise.  Only effective if the container is not focusable.
		/// </summary>
		/// <returns>Whether children should be focusable.</returns>
		public bool isFocusableChildrenAllowed()
		{
			return this.allowFocusableChildren_;
		}

		/// <summary>
		/// Sets whether the container allows children to be focusable, false
		/// otherwise.  Only effective if the container is not focusable.
		/// </summary>
		/// <param name="focusable">Whether the children should be focusable.</param>
		public void setFocusableChildrenAllowed(bool focusable)
		{
			this.allowFocusableChildren_ = focusable;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether highlighting a child component should also open it.</returns>
		public bool isOpenFollowsHighlight()
		{
			return this.openFollowsHighlight_;
		}

		// Highlight management.

		/// <summary>
		/// Returns the index of the currently highlighted item (-1 if none).
		/// </summary>
		/// <returns>Index of the currently highlighted item.</returns>
		public int getHighlightedIndex()
		{
			return this.highlightedIndex_;
		}

		/// <summary>
		/// Highlights the item at the given 0-based index (if any).  If another item
		/// was previously highlighted, it is un-highlighted.
		/// </summary>
		/// <param name="index">Index of item to highlight (-1 removes the current</param>
		///     highlight).
		public virtual void setHighlightedIndex(int index)
		{
			var child = this.getChildAt(index);
			if (child != null) {
				((Control)child).setHighlighted(true);
			}
			else if (this.highlightedIndex_ > -1) {
				this.getHighlighted().setHighlighted(false);
			}
		}

		/// <summary>
		/// Highlights the given item if it exists and is a child of the container;
		/// otherwise un-highlights the currently highlighted item.
		/// </summary>
		/// <param name="item">Item to highlight.</param>
		public void setHighlighted(goog.ui.Control item)
		{
			this.setHighlightedIndex(this.indexOfChild(item));
		}

		/// <summary>
		/// Returns the currently highlighted item (if any).
		/// </summary>
		/// <returns>Highlighted item (null if none).</returns>
		public goog.ui.Control getHighlighted()
		{
			return (Control)this.getChildAt(this.highlightedIndex_);
		}

		/// <summary>
		/// Highlights the first highlightable item in the container
		/// </summary>
		public void highlightFirst()
		{
			this.highlightHelper((index, max) => {
				return (index + 1) % max;
			}, this.getChildCount() - 1);
		}

		/// <summary>
		/// Highlights the last highlightable item in the container.
		/// </summary>
		public void highlightLast()
		{
			this.highlightHelper((index, max) => {
				index--;
				return index < 0 ? max - 1 : index;
			}, 0);
		}

		/// <summary>
		/// Highlights the next highlightable item (or the first if nothing is currently
		/// highlighted).
		/// </summary>
		public void highlightNext()
		{
			this.highlightHelper((index, max) => {
				return (index + 1) % max;
			}, this.highlightedIndex_);
		}

		/// <summary>
		/// Highlights the previous highlightable item (or the last if nothing is
		/// currently highlighted).
		/// </summary>
		public void highlightPrevious()
		{
			this.highlightHelper((index, max) => {
				index--;
				return index < 0 ? max - 1 : index;
			}, this.highlightedIndex_);
		}

		/// <summary>
		/// Helper function that manages the details of moving the highlight among
		/// child controls in response to keyboard events.
		/// </summary>
		/// <param name="fn"></param>
		///     Function that accepts the current and maximum indices, and returns the
		///     next index to check.
		/// <param name="startIndex">Start index.</param>
		/// <returns>Whether the highlight has changed.</returns>
		protected bool highlightHelper(Func<int, int, int> fn, int startIndex)
		{
			// If the start index is -1 (meaning there's nothing currently highlighted),
			// try starting from the currently open item, if any.
			var curIndex =
				startIndex < 0 ? this.indexOfChild(this.openItem_) : startIndex;
			var numItems = this.getChildCount();

			curIndex = fn(curIndex, numItems);
			var visited = 0;
			while (visited <= numItems) {
				var control = (Control)this.getChildAt(curIndex);
				if (control != null && this.canHighlightItem(control)) {
					this.setHighlightedIndexFromKeyEvent(curIndex);
					return true;
				}
				visited++;
				curIndex = fn(curIndex, numItems);
			}
			return false;
		}

		/// <summary>
		/// Returns whether the given item can be highlighted.
		/// </summary>
		/// <param name="item">The item to check.</param>
		/// <returns>Whether the item can be highlighted.</returns>
		protected virtual bool canHighlightItem(goog.ui.Control item)
		{
			return item.isVisible() && item.isEnabled() &&
				item.isSupportedState(goog.ui.Component.State.HOVER);
		}

		/// <summary>
		/// Helper method that sets the highlighted index to the given index in response
		/// to a keyboard event.  The base class implementation simply calls the
		/// {@link #setHighlightedIndex} method, but subclasses can override this
		/// behavior as needed.
		/// </summary>
		/// <param name="index">Index of item to highlight.</param>
		protected void setHighlightedIndexFromKeyEvent(int index)
		{
			this.setHighlightedIndex(index);
		}

		/// <summary>
		/// Returns the currently open (expanded) control in the container (null if
		/// none).
		/// </summary>
		/// <returns>The currently open control.</returns>
		public goog.ui.Control getOpenItem()
		{
			return this.openItem_;
		}

		/// <summary>
		/// Returns true if the mouse button is pressed, false otherwise.
		/// </summary>
		/// <returns>Whether the mouse button is pressed.</returns>
		public bool isMouseButtonPressed()
		{
			return this.mouseButtonPressed_;
		}

		/// <summary>
		/// Sets or clears the "mouse button pressed" flag.
		/// </summary>
		/// <param name="pressed">Whether the mouse button is presed.</param>
		private void setMouseButtonPressed(bool pressed)
		{
			this.mouseButtonPressed_ = pressed;
		}
	}
}
