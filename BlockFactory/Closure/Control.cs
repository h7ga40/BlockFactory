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
 * @fileoverview Base class for UI controls such as buttons, menus, menu items,
 * toolbar buttons, etc.  The implementation is based on a generalized version
 * of {@link goog.ui.MenuItem}.
 * TODO(attila):  If the renderer framework works well, pull it into Component.
 *
 * @author attila@google.com (Attila Bodis)
 * @see ../demos/control.html
 * @see http://code.google.com/p/closure-library/wiki/IntroToControls
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class Control : Component
	{
		/// <summary>
		/// The control's aria-label.
		/// </summary>
		private string ariaLabel_;
		private IeMouseEventSequenceSimulator_ ieMouseEventSequenceSimulator_;

		/// <summary>
		/// Base class for UI controls.  Extends {@link goog.ui.Component} by adding
		/// the following:
		///  <ul>
		///    <li>a {@link goog.events.KeyHandler}, to simplify keyboard handling,
		///    <li>a pluggable <em>renderer</em> framework, to simplify the creation of
		///        simple controls without the need to subclass this class,
		///    <li>the notion of component <em>content</em>, like a text caption or DOM
		///        structure displayed in the component (e.g. a button label),
		///    <li>getter and setter for component content, as well as a getter and
		///        setter specifically for caption text (for convenience),
		///    <li>support for hiding/showing the component,
		///    <li>fine-grained control over supported states and state transition
		///        events, and
		///    <li>default mouse and keyboard event handling.
		///  </ul>
		/// This class has sufficient built-in functionality for most simple UI controls.
		/// All controls dispatch SHOW, HIDE, ENTER, LEAVE, and ACTION events on show,
		/// hide, mouseover, mouseout, and user action, respectively.  Additional states
		/// are also supported.  See closure/demos/control.html
		/// for example usage.
		/// </summary>
		/// <param name="opt_content">Text caption or DOM structure
		/// to display as the content of the control (if any).</param>
		/// <param name="opt_renderer">Renderer used to render or
		/// decorate the component; defaults to {@link goog.ui.ControlRenderer}.</param>
		/// <param name="opt_domHelper">Optional DOM helper, used for
		/// document interaction.</param>
		public Control(ControlContent opt_content = null, ControlRenderer opt_renderer = null,
			dom.DomHelper opt_domHelper = null)
			: base(opt_domHelper)
		{
			renderer_ = opt_renderer ?? ControlRenderer.getInstance();
			this.setContentInternal(opt_content != null ? opt_content : null);
			handleContextMenu = nullFunction = new Action<events.Event>(nullFunction_);
		}

		// Renderer registry.
		// TODO(attila): Refactor existing usages inside Google in a follow-up CL.


		/// <summary>
		/// Maps a CSS class name to a function that returns a new instance of
		/// {@link goog.ui.Control} or a subclass thereof, suitable to decorate
		/// an element that has the specified CSS class.  UI components that extend
		/// {@link goog.ui.Control} and want {@link goog.ui.Container}s to be able
		/// to discover and decorate elements using them should register a factory
		/// function via this API.
		/// </summary>
		/// <param name="className">CSS class name.</param>
		/// <param name="decoratorFunction">Function that takes no arguments and
		///     returns a new instance of a control to decorate an element with the
		///     given class.</param>
		/// @deprecated Use {@link goog.ui.registry.setDecoratorByClassName} instead.
		public static void registerDecorator(string className, Type decoratorFunction)
		{
			goog.ui.registry.setDecoratorByClassName(className, decoratorFunction);
		}


		/// <summary>
		/// Takes an element and returns a new instance of {@link goog.ui.Control}
		/// or a subclass, suitable to decorate it (based on the element's CSS class).
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// @return {} New control instance to decorate the element
		///     (null if none).
		/// @deprecated Use {@link goog.ui.registry.getDecorator} instead.
		public static goog.ui.Control getDecorator(HTMLElement element)
		{
			return (Control)goog.ui.registry.getDecorator(element);
		}


		/// <summary>
		/// Renderer associated with the component.
		/// </summary>
		private goog.ui.ControlRenderer renderer_;


		/// <summary>
		/// Text caption or DOM structure displayed in the component.
		/// </summary>
		protected goog.ui.ControlContent content_ = null;


		/// <summary>
		/// Current component state; a bit mask of {@link goog.ui.Component.State}s.
		/// </summary>
		private State state_;


		/// <summary>
		/// A bit mask of {@link goog.ui.Component.State}s this component supports.
		/// </summary>
		private State supportedStates_ = State.DISABLED | State.HOVER | State.ACTIVE | State.FOCUSED;


		/// <summary>
		/// A bit mask of {@link goog.ui.Component.State}s for which this component
		/// provides default event handling.  For example, a component that handles
		/// the HOVER state automatically will highlight itself on mouseover, whereas
		/// a component that doesn't handle HOVER automatically will only dispatch
		/// ENTER and LEAVE events but not call {@link setHighlighted} on itself.
		/// By default, components provide default event handling for all states.
		/// Controls hosted in containers (e.g. menu items in a menu, or buttons in a
		/// toolbar) will typically want to have their container manage their highlight
		/// state.  Selectable controls managed by a selection model will also typically
		/// want their selection state to be managed by the model.
		/// </summary>
		private State autoStates_ = State.ALL;

		/// <summary>
		/// A bit mask of {@link goog.ui.Component.State}s for which this component
		/// dispatches state transition events.  Because events are expensive, the
		/// default behavior is to not dispatch any state transition events at all.
		/// Use the {@link #setDispatchTransitionEvents} API to request transition
		/// events  as needed.  Subclasses may enable transition events by default.
		/// Controls hosted in containers or managed by a selection model will typically
		/// want to dispatch transition events.
		/// </summary>
		protected State statesWithTransitionEvents_;


		/// <summary>
		/// Component visibility.
		/// </summary>
		private bool visible_ = true;


		/// <summary>
		/// Keyboard event handler.
		/// </summary>
		private events.KeyHandler keyHandler_;


		/// <summary>
		/// Additional class name(s) to apply to the control's root element, if any.
		/// </summary>
		private JsArray<string> extraClassNames_;


		/// <summary>
		/// Whether the control should listen for and handle mouse events; defaults to
		/// true.
		/// </summary>
		private bool handleMouseEvents_ = true;


		/// <summary>
		/// Whether the control allows text selection within its DOM.  Defaults to false.
		/// </summary>
		protected bool allowTextSelection_;


		/// <summary>
		/// The control's preferred ARIA role.
		/// </summary>
		private a11y.aria.Role preferredAriaRole_;

		// Event handler and renderer management.


		/// <summary>
		/// Returns true if the control is configured to handle its own mouse events,
		/// false otherwise.  Controls not hosted in {@link goog.ui.Container}s have
		/// to handle their own mouse events, but controls hosted in containers may
		/// allow their parent to handle mouse events on their behalf.  Considered
		/// protected; should only be used within this package and by subclasses.
		/// </summary>
		/// <returns>Whether the control handles its own mouse events.</returns>
		public bool isHandleMouseEvents()
		{
			return this.handleMouseEvents_;
		}

		/// <summary>
		/// Enables or disables mouse event handling for the control.  Containers may
		/// use this method to disable mouse event handling in their child controls.
		/// Considered protected; should only be used within this package and by
		/// subclasses.
		/// </summary>
		/// <param name="enable">Whether to enable or disable mouse event handling.</param>
		public void setHandleMouseEvents(bool enable)
		{
			if (this.isInDocument() && enable != this.handleMouseEvents_) {
				// Already in the document; need to update event handler.
				this.enableMouseEventHandling_(enable);
			}
			this.handleMouseEvents_ = enable;
		}

		/// <summary>
		/// Returns the DOM element on which the control is listening for keyboard
		/// events (null if none).
		/// </summary>
		/// <returns>Element on which the control is listening for key
		/// events.<returns>
		public HTMLElement getKeyEventTarget()
		{
			// Delegate to renderer.
			return this.renderer_.getKeyEventTarget(this);
		}

		/// <summary>
		/// Returns the keyboard event handler for this component, lazily created the
		/// first time this method is called.  Considered protected; should only be
		/// used within this package and by subclasses.
		/// </summary>
		/// <returns>Keyboard event handler for this component.</returns>
		protected goog.events.KeyHandler getKeyHandler()
		{
			return this.keyHandler_ ?? (this.keyHandler_ = new goog.events.KeyHandler());
		}
		/// <summary>
		/// Returns the renderer used by this component to render itself or to decorate
		/// an existing element.
		/// </summary>
		/// <returns>Renderer used by the component (undefined if none).</returns>
		public ControlRenderer getRenderer()
		{
			return this.renderer_;
		}

		/// <summary>
		/// Registers the given renderer with the component.  Changing renderers after
		/// the component has entered the document is an error.
		/// </summary>
		/// <param name="renderer">Renderer used by the component.</param>
		/// @throws {Error} If the control is already in the document.
		public void setRenderer(goog.ui.ControlRenderer renderer)
		{
			if (this.isInDocument()) {
				// Too late.
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			if (this.getElement() != null) {
				// The component has already been rendered, but isn't yet in the document.
				// Replace the renderer and delete the current DOM, so it can be re-rendered
				// using the new renderer the next time someone calls render().
				this.setElementInternal(null);
			}

			this.renderer_ = renderer;
		}


		// Support for additional styling.

		/// <summary>
		/// Returns any additional class name(s) to be applied to the component's
		/// root element, or null if no extra class names are needed.
		/// </summary>
		/// <returns>Additional class names to be applied to
		/// the component's root element (null if none).</returns>
		public JsArray<string> getExtraClassNames()
		{
			return this.extraClassNames_;
		}

		/// <summary>
		/// Adds the given class name to the list of classes to be applied to the
		/// component's root element.
		/// </summary>
		/// <param name="className">Additional class name to be applied to the</param>
		///     component's root element.
		public void addClassName(string className)
		{
			if (className != null) {
				if (this.extraClassNames_ != null) {
					if (!this.extraClassNames_.Contains(className)) {
						this.extraClassNames_.Push(className);
					}
				}
				else {
					this.extraClassNames_ = new JsArray<string>() { className };
				}
				this.renderer_.enableExtraClassName(this, className, true);
			}
		}


		/// <summary>
		/// Removes the given class name from the list of classes to be applied to
		/// the component's root element.
		/// </summary>
		/// <param name="className">Class name to be removed from the component's root</param>
		///     element.
		public void removeClassName(string className)
		{
			if (className != null && this.extraClassNames_ != null &&
				this.extraClassNames_.Remove(className)) {
				if (this.extraClassNames_.Length == 0) {
					this.extraClassNames_ = null;
				}
				this.renderer_.enableExtraClassName(this, className, false);
			}
		}


		/// <summary>
		/// Adds or removes the given class name to/from the list of classes to be
		/// applied to the component's root element.
		/// </summary>
		/// <param name="className">CSS class name to add or remove.</param>
		/// <param name="enable">Whether to add or remove the class name.</param>
		public void enableClassName(string className, bool enable)
		{
			if (enable) {
				this.addClassName(className);
			}
			else {
				this.removeClassName(className);
			}
		}
		// Standard goog.ui.Component implementation.

		/// <summary>
		/// Creates the control's DOM.  Overrides {@link goog.ui.Component#createDom} by
		/// delegating DOM manipulation to the control's renderer.
		/// </summary>
		public override void createDom()
		{
			var element = this.renderer_.createDom(this);
			this.setElementInternal(element);

			// Initialize ARIA role.
			this.renderer_.setAriaRole(element, this.getPreferredAriaRole());

			// Initialize text selection.
			if (!this.isAllowTextSelection()) {
				// The renderer is assumed to create selectable elements.  Since making
				// elements unselectable is expensive, only do it if needed (bug 1037090).
				this.renderer_.setAllowTextSelection(element, false);
			}

			// Initialize visibility.
			if (!this.isVisible()) {
				// The renderer is assumed to create visible elements. Since hiding
				// elements can be expensive, only do it if needed (bug 1037105).
				this.renderer_.setVisible(element, false);
			}
		}

		/// <summary>
		/// Returns the control's preferred ARIA role. This can be used by a control to
		/// override the role that would be assigned by the renderer.  This is useful in
		/// cases where a different ARIA role is appropriate for a control because of the
		/// context in which it's used.  E.g., a {@link goog.ui.MenuButton} added to a
		/// {@link goog.ui.Select} should have an ARIA role of LISTBOX and not MENUITEM.
		/// </summary>
		/// <returns>This control's preferred ARIA role or null if
		/// no preferred ARIA role is set.</returns>
		public virtual a11y.aria.Role getPreferredAriaRole()
		{
			return this.preferredAriaRole_;
		}

		/// <summary>
		/// Sets the control's preferred ARIA role. This can be used to override the role
		/// that would be assigned by the renderer.  This is useful in cases where a
		/// different ARIA role is appropriate for a control because of the
		/// context in which it's used.  E.g., a {@link goog.ui.MenuButton} added to a
		/// {@link goog.ui.Select} should have an ARIA role of LISTBOX and not MENUITEM.
		/// </summary>
		/// <param name="role">This control's preferred ARIA role.</param>
		public void setPreferredAriaRole(goog.a11y.aria.Role role)
		{
			this.preferredAriaRole_ = role;
		}

		/// <summary>
		/// Gets the control's aria label.
		/// </summary>
		/// <returns>This control's aria label.</returns>
		public string getAriaLabel()
		{
			return this.ariaLabel_;
		}

		/// <summary>
		/// Sets the control's aria label. This can be used to assign aria label to the
		/// element after it is rendered.
		/// </summary>
		/// <param name="label">The string to set as the aria label for this control.</param>
		///     No escaping is done on this value.
		public void setAriaLabel(string label)
		{
			this.ariaLabel_ = label;
			var element = this.getElement();
			if (element != null) {
				this.renderer_.setAriaLabel(element, label);
			}
		}
		/// <summary>
		/// Returns the DOM element into which child components are to be rendered,
		/// or null if the control itself hasn't been rendered yet.  Overrides
		/// {@link goog.ui.Component#getContentElement} by delegating to the renderer.
		/// </summary>
		/// <returns>Element to contain child elements (null if none).</returns>
		public override HTMLElement getContentElement()
		{
			// Delegate to renderer.
			return this.renderer_.getContentElement(this.getElement());
		}

		/// <summary>
		/// Returns true if the given element can be decorated by this component.
		/// Overrides {@link goog.ui.Component#canDecorate}.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Whether the element can be decorated by this component.</returns>
		public override bool canDecorate(Element element)
		{
			// Controls support pluggable renderers; delegate to the renderer.
			return this.renderer_.canDecorate(element);
		}

		/// <summary>
		/// Decorates the given element with this component. Overrides {@link
		/// goog.ui.Component#decorateInternal} by delegating DOM manipulation
		/// to the control's renderer.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		protected override void decorateInternal(HTMLElement element)
		{
			element = this.renderer_.decorate(this, element);
			this.setElementInternal(element);

			// Initialize ARIA role.
			this.renderer_.setAriaRole(element, this.getPreferredAriaRole());

			// Initialize text selection.
			if (!this.isAllowTextSelection()) {
				// Decorated elements are assumed to be selectable.  Since making elements
				// unselectable is expensive, only do it if needed (bug 1037090).
				this.renderer_.setAllowTextSelection(element, false);
			}

			// Initialize visibility based on the decorated element's styling.
			this.visible_ = element.Style.Display != Display.None;
		}

		/// <summary>
		/// Configures the component after its DOM has been rendered, and sets up event
		/// handling.  Overrides {@link goog.ui.Component#enterDocument}.
		/// </summary>
		public override void enterDocument()
		{
			base.enterDocument();

			// Call the renderer's setAriaStates method to set element's aria attributes.
			this.renderer_.setAriaStates(this, this.getElementStrict());

			// Call the renderer's initializeDom method to configure properties of the
			// control's DOM that can only be done once it's in the document.
			this.renderer_.initializeDom(this);

			// Initialize event handling if at least one state other than DISABLED is
			// supported.
			if (((int)this.supportedStates_ & ~(int)goog.ui.Component.State.DISABLED) != 0) {
				// Initialize mouse event handling if the control is configured to handle
				// its own mouse events.  (Controls hosted in containers don't need to
				// handle their own mouse events.)
				if (this.isHandleMouseEvents()) {
					this.enableMouseEventHandling_(true);
				}

				// Initialize keyboard event handling if the control is focusable and has
				// a key event target.  (Controls hosted in containers typically aren"t
				// focusable, allowing their container to handle keyboard events for them.)
				if (this.isSupportedState(goog.ui.Component.State.FOCUSED)) {
					var keyTarget = this.getKeyEventTarget();
					if (keyTarget != null) {
						var keyHandler = this.getKeyHandler();
						keyHandler.attach(keyTarget);
						this.getHandler()
							.listen(
								keyHandler, goog.events.KeyHandler.EventType.KEY,
								new Func<goog.events.KeyEvent, bool>(this.handleKeyEvent))
							.listen(keyTarget, goog.events.EventType.FOCUS, new Action<goog.events.Event>(this.handleFocus))
							.listen(keyTarget, goog.events.EventType.BLUR, new Action<goog.events.Event>(this.handleBlur));
					}
				}
			}
		}

		/// <summary>
		/// Enables or disables mouse event handling on the control.
		/// </summary>
		/// <param name="enable">Whether to enable mouse event handling.</param>
		private void enableMouseEventHandling_(bool enable)
		{
			var handler = this.getHandler();
			var element = this.getElement();
			if (enable) {
				handler
					.listen(element, goog.events.EventType.MOUSEOVER, new Action<events.BrowserEvent>(this.handleMouseOver))
					.listen(element, goog.events.EventType.MOUSEDOWN, new Action<events.BrowserEvent>(this.handleMouseDown))
					.listen(element, goog.events.EventType.MOUSEUP, new Action<events.BrowserEvent>(this.handleMouseUp))
					.listen(element, goog.events.EventType.MOUSEOUT, new Action<events.BrowserEvent>(this.handleMouseOut));
				if (this.handleContextMenu != nullFunction) {
					handler.listen(
						element, goog.events.EventType.CONTEXTMENU, this.handleContextMenu);
				}
				if (goog.userAgent.IE) {
					// Versions of IE before 9 send only one click event followed by a
					// dblclick, so we must explicitly listen for these. In later versions,
					// two click events are fired  and so a dblclick listener is unnecessary.
					if (!goog.userAgent.isVersionOrHigher("9")) {
						handler.listen(
							element, goog.events.EventType.DBLCLICK, new Action<events.Event>(this.handleDblClick));
					}
					if (this.ieMouseEventSequenceSimulator_ == null) {
						this.ieMouseEventSequenceSimulator_ =
							new goog.ui.Control.IeMouseEventSequenceSimulator_(this);
						this.registerDisposable(this.ieMouseEventSequenceSimulator_);
					}
				}
			}
			else {
				handler
					.unlisten(
						element, goog.events.EventType.MOUSEOVER, new Action<events.BrowserEvent>(this.handleMouseOver))
					.unlisten(
						element, goog.events.EventType.MOUSEDOWN, new Action<events.BrowserEvent>(this.handleMouseDown))
					.unlisten(element, goog.events.EventType.MOUSEUP, new Action<events.BrowserEvent>(this.handleMouseUp))
					.unlisten(element, goog.events.EventType.MOUSEOUT, new Action<events.BrowserEvent>(this.handleMouseOut));
				if (this.handleContextMenu != nullFunction) {
					handler.unlisten(
						element, goog.events.EventType.CONTEXTMENU, this.handleContextMenu);
				}
				if (goog.userAgent.IE) {
					if (!goog.userAgent.isVersionOrHigher("9")) {
						handler.unlisten(
							element, goog.events.EventType.DBLCLICK, new Action<events.Event>(this.handleDblClick));
					}
					Script.Delete(ref this.ieMouseEventSequenceSimulator_);
					this.ieMouseEventSequenceSimulator_ = null;
				}
			}
		}
		/// <summary>
		/// Cleans up the component before its DOM is removed from the document, and
		/// removes event handlers.  Overrides {@link goog.ui.Component#exitDocument}
		/// by making sure that components that are removed from the document aren"t
		/// focusable (i.e. have no tab index).
		/// </summary>
		public override void exitDocument()
		{
			base.exitDocument();
			if (this.keyHandler_ != null) {
				this.keyHandler_.detach();
			}
			if (this.isVisible() && this.isEnabled()) {
				this.renderer_.setFocusable(this, false);
			}
		}

		public override void disposeInternal()
		{
			base.disposeInternal();
			if (this.keyHandler_ != null) {
				this.keyHandler_.dispose();
				Script.Delete(ref this.keyHandler_);
			}
			Script.Delete(ref this.renderer_);
			this.content_ = null;
			this.extraClassNames_ = null;
			this.ieMouseEventSequenceSimulator_ = null;
		}

		// Component content management.

		/// <summary>
		/// Returns the text caption or DOM structure displayed in the component.
		/// </summary>
		/// <returns>Text caption or DOM structure
		/// comprising the component's contents.</returns>
		public ControlContent getContent()
		{
			return this.content_;
		}

		/// <summary>
		/// Sets the component's content to the given text caption, element, or array of
		/// nodes.  (If the argument is an array of nodes, it must be an actual array,
		/// not an array-like object.)
		/// </summary>
		/// <param name="content">Text caption or DOM</param>
		///     structure to set as the component's contents.
		public void setContent(goog.ui.ControlContent content)
		{
			// Controls support pluggable renderers; delegate to the renderer.
			this.renderer_.setContent(this.getElement(), content);

			// setContentInternal needs to be after the renderer, since the implementation
			// may depend on the content being in the DOM.
			this.setContentInternal(content);
		}

		/// <summary>
		/// Sets the component's content to the given text caption, element, or array
		/// of nodes.  Unlike {@link #setContent}, doesn't modify the component's DOM.
		/// Called by renderers during element decoration.
		/// 
		/// This should only be used by subclasses and its associated renderers.
		/// </summary>
		/// <param name="content">Text caption or DOM structure
		/// to set as the component's contents.</param>
		public virtual void setContentInternal(ControlContent content)
		{
			this.content_ = content;
		}

		/// <summary>
		/// Text caption of the control or empty string if none.
		/// </summary>
		/// <returns></returns>
		public virtual string getCaption()
		{
			var content = this.getContent();
			if (content == null) {
				return "";
			}
			var caption = content.Is<string>() ?
				content.As<string>() :
				content.Is<NodeList>() ?
				content.As<NodeList>().Select(goog.dom.getRawTextContent).Join("") :
				goog.dom.getTextContent(content.As<Node>());
			return goog.@string.collapseBreakingSpaces(caption);
		}

		/// <summary>
		/// Sets the text caption of the component.
		/// </summary>
		/// <param name="caption">Text caption of the component.</param>
		public virtual void setCaption(string caption)
		{
			this.setContent(new ControlContent(caption));
		}


		// Component state management.

		public override void setRightToLeft(bool rightToLeft)
		{
			// The superclass implementation ensures the control isn't in the document.
			base.setRightToLeft(rightToLeft);

			var element = this.getElement();
			if (element != null) {
				this.renderer_.setRightToLeft(element, rightToLeft);
			}
		}

		/// <summary>
		/// Returns true if the control allows text selection within its DOM, false
		/// otherwise.  Controls that disallow text selection have the appropriate
		/// unselectable styling applied to their elements.  Note that controls hosted
		/// in containers will report that they allow text selection even if their
		/// container disallows text selection.
		/// </summary>
		/// <returns>Whether the control allows text selection.</returns>
		public bool isAllowTextSelection()
		{
			return this.allowTextSelection_;
		}

		/// <summary>
		/// Allows or disallows text selection within the control's DOM.
		/// </summary>
		/// <param name="allow">Whether the control should allow text selection.</param>
		public void setAllowTextSelection(bool allow)
		{
			this.allowTextSelection_ = allow;

			var element = this.getElement();
			if (element != null) {
				this.renderer_.setAllowTextSelection(element, allow);
			}
		}

		/// <summary>
		/// Returns true if the component's visibility is set to visible, false if
		/// it is set to hidden.  A component that is set to hidden is guaranteed
		/// to be hidden from the user, but the reverse isn't necessarily true.
		/// A component may be set to visible but can otherwise be obscured by another
		/// element, rendered off-screen, or hidden using direct CSS manipulation.
		/// </summary>
		/// <returns>Whether the component is visible.</returns>
		public bool isVisible()
		{
			return this.visible_;
		}

		/// <summary>
		/// Shows or hides the component.  Does nothing if the component already has
		/// the requested visibility.  Otherwise, dispatches a SHOW or HIDE event as
		/// appropriate, giving listeners a chance to prevent the visibility change.
		/// When showing a component that is both enabled and focusable, ensures that
		/// its key target has a tab index.  When hiding a component that is enabled
		/// and focusable, blurs its key target and removes its tab index.
		/// </summary>
		/// <param name="visible">Whether to show or hide the component.</param>
		/// <param name="opt_force">If true, doesn't check whether the component</param>
		///     already has the requested visibility, and doesn't dispatch any events.
		/// <returns>Whether the visibility was changed.</returns>
		public virtual bool setVisible(bool visible, bool opt_force = false)
		{
			if (opt_force || (this.visible_ != visible &&
							  this.dispatchEvent(
								  visible ? goog.ui.Component.EventType.SHOW :
											goog.ui.Component.EventType.HIDE))) {
				var element = this.getElement();
				if (element != null) {
					this.renderer_.setVisible(element, visible);
				}
				if (this.isEnabled()) {
					this.renderer_.setFocusable(this, visible);
				}
				this.visible_ = visible;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns true if the component is enabled, false otherwise.
		/// </summary>
		/// <returns>Whether the component is enabled.</returns>
		public bool isEnabled()
		{
			return !this.hasState(goog.ui.Component.State.DISABLED);
		}

		/// <summary>
		/// Returns true if the control has a parent that is itself disabled, false
		/// otherwise.
		/// </summary>
		/// <returns>Whether the component is hosted in a disabled container.</returns>
		private bool isParentDisabled_()
		{
			var parent = this.getParent();
			return parent != null && parent is Control && !((Control)parent).isEnabled();
		}

		/// <summary>
		/// Enables or disables the component.  Does nothing if this state transition
		/// is disallowed.  If the component is both visible and focusable, updates its
		/// focused state and tab index as needed.  If the component is being disabled,
		/// ensures that it is also deactivated and un-highlighted first.  Note that the
		/// component's enabled/disabled state is "locked" as long as it is hosted in a
		/// {@link goog.ui.Container} that is itself disabled; this is to prevent clients
		/// from accidentally re-enabling a control that is in a disabled container.
		/// </summary>
		/// <param name="enable">Whether to enable or disable the component.</param>
		/// @see #isTransitionAllowed
		public void setEnabled(bool enable)
		{
			if (!this.isParentDisabled_() &&
				this.isTransitionAllowed(goog.ui.Component.State.DISABLED, !enable)) {
				if (!enable) {
					this.setActive(false);
					this.setHighlighted(false);
				}
				if (this.isVisible()) {
					this.renderer_.setFocusable(this, enable);
				}
				this.setState(goog.ui.Component.State.DISABLED, !enable, true);
			}
		}

		/// <summary>
		/// Returns true if the component is currently highlighted, false otherwise.
		/// </summary>
		/// <returns>Whether the component is highlighted.</returns>
		public bool isHighlighted()
		{
			return this.hasState(goog.ui.Component.State.HOVER);
		}

		/// <summary>
		/// Highlights or unhighlights the component.  Does nothing if this state
		/// transition is disallowed.
		/// </summary>
		/// <param name="highlight">Whether to highlight or unhighlight the component.
		/// @see #isTransitionAllowed</param>
		public virtual void setHighlighted(bool highlight)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.HOVER, highlight)) {
				this.setState(goog.ui.Component.State.HOVER, highlight);
			}
		}

		/// <summary>
		/// Returns true if the component is active (pressed), false otherwise.
		/// </summary>
		/// <returns>Whether the component is active.</returns>
		public bool isActive()
		{
			return this.hasState(goog.ui.Component.State.ACTIVE);
		}

		/// <summary>
		/// Activates or deactivates the component.  Does nothing if this state
		/// transition is disallowed.
		/// </summary>
		/// <param name="active">Whether to activate or deactivate the component.
		/// @see #isTransitionAllowed</param>
		public void setActive(bool active)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.ACTIVE, active)) {
				this.setState(goog.ui.Component.State.ACTIVE, active);
			}
		}

		/// <summary>
		/// Returns true if the component is selected, false otherwise.
		/// </summary>
		/// <returns>Whether the component is selected.</returns>
		public bool isSelected()
		{
			return this.hasState(goog.ui.Component.State.SELECTED);
		}

		/// <summary>
		/// Selects or unselects the component.  Does nothing if this state transition
		/// is disallowed.
		/// </summary>
		/// <param name="select">Whether to select or unselect the component.
		/// @see #isTransitionAllowed</param>
		public void setSelected(bool select)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.SELECTED, select)) {
				this.setState(goog.ui.Component.State.SELECTED, select);
			}
		}

		/// <summary>
		/// Returns true if the component is checked, false otherwise.
		/// </summary>
		/// <returns>Whether the component is checked.</returns>
		public bool isChecked()
		{
			return this.hasState(goog.ui.Component.State.CHECKED);
		}

		/// <summary>
		/// Checks or unchecks the component.  Does nothing if this state transition
		/// is disallowed.
		/// </summary>
		/// <param name="check">Whether to check or uncheck the component.
		/// @see #isTransitionAllowed</param>
		public void setChecked(bool check)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.CHECKED, check)) {
				this.setState(goog.ui.Component.State.CHECKED, check);
			}
		}

		/// <summary>
		/// Returns true if the component is styled to indicate that it has keyboard
		/// focus, false otherwise.  Note that {@code isFocused()} returning true
		/// doesn't guarantee that the component's key event target has keyborad focus,
		/// only that it is styled as such.
		/// </summary>
		/// <returns>Whether the component is styled to indicate as having
		/// keyboard focus.</returns>
		public bool isFocused()
		{
			return this.hasState(goog.ui.Component.State.FOCUSED);
		}

		/// <summary>
		/// Applies or removes styling indicating that the component has keyboard focus.
		/// Note that unlike the other "set" methods, this method is called as a result
		/// of the component's element having received or lost keyboard focus, not the
		/// other way around, so calling {@code setFocused(true)} doesn't guarantee that
		/// the component's key event target has keyboard focus, only that it is styled
		/// as such.
		/// </summary>
		/// <param name="focused">Whether to apply or remove styling to indicate that
		/// the component's element has keyboard focus.</param>
		public void setFocused(bool focused)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.FOCUSED, focused)) {
				this.setState(goog.ui.Component.State.FOCUSED, focused);
			}
		}

		/// <summary>
		/// Returns true if the component is open (expanded), false otherwise.
		/// </summary>
		/// <returns>Whether the component is open.</returns>
		public bool isOpen()
		{
			return this.hasState(goog.ui.Component.State.OPENED);
		}


		/// <summary>
		/// Opens (expands) or closes (collapses) the component.  Does nothing if this
		/// state transition is disallowed.
		/// </summary>
		/// <param name="open">Whether to open or close the component.
		/// @see #isTransitionAllowed</param>
		public void setOpen(bool open)
		{
			if (this.isTransitionAllowed(goog.ui.Component.State.OPENED, open)) {
				this.setState(goog.ui.Component.State.OPENED, open);
			}
		}

		/// <summary>
		/// Returns the component's state as a bit mask of {@link
		/// goog.ui.Component.State}s.
		/// </summary>
		/// <returns>Bit mask representing component state.</returns>
		public State getState()
		{
			return this.state_;
		}

		/// <summary>
		/// Returns true if the component is in the specified state, false otherwise.
		/// </summary>
		/// <param name="state">State to check.</param>
		/// <returns>Whether the component is in the given state.</returns>
		public bool hasState(State state)
		{
			return ((int)this.state_ & (int)state) != 0;
		}

		/// <summary>
		/// Sets or clears the given state on the component, and updates its styling
		/// accordingly.  Does nothing if the component is already in the correct state
		/// or if it doesn't support the specified state.  Doesn't dispatch any state
		/// transition events; use advisedly.
		/// </summary>
		/// <param name="state">State to set or clear.</param>
		/// <param name="enable">Whether to set or clear the state (if supported).</param>
		/// <param name="opt_calledFrom">Prevents looping with setEnabled.</param>
		public void setState(State state, bool enable, bool opt_calledFrom = false)
		{
			if (!opt_calledFrom && state == goog.ui.Component.State.DISABLED) {
				this.setEnabled(!enable);
				return;
			}
			if (this.isSupportedState(state) && enable != this.hasState(state)) {
				// Delegate actual styling to the renderer, since it is DOM-specific.
				this.renderer_.setState(this, state, enable);
				this.state_ = enable ? this.state_ | state : this.state_ & ~state;
			}
		}

		/// <summary>
		/// Sets the component's state to the state represented by a bit mask of
		/// {@link goog.ui.Component.State}s.  Unlike {@link #setState}, doesn"t
		/// update the component's styling, and doesn't reject unsupported states.
		/// Called by renderers during element decoration.  Considered protected;
		/// should only be used within this package and by subclasses.
		///
		/// This should only be used by subclasses and its associated renderers.
		/// </summary>
		/// <param name="state">Bit mask representing component state.</param>
		public void setStateInternal(State state)
		{
			this.state_ = state;
		}

		/// <summary>
		/// Returns true if the component supports the specified state, false otherwise.
		/// </summary>
		/// <param name="state">State to check.</param>
		/// <returns>Whether the component supports the given state.</returns>
		public bool isSupportedState(State state)
		{
			return (this.supportedStates_ & state) != 0;
		}

		/// <summary>
		/// Enables or disables support for the given state. Disabling support
		/// for a state while the component is in that state is an error.
		/// </summary>
		/// <param name="state">State to support or de-support.</param>
		/// <param name="support">support Whether the component should support the state.</param>
		public virtual void setSupportedState(State state, bool support)
		{
			if (this.isInDocument() && this.hasState(state) && !support) {
				// Since we hook up event handlers in enterDocument(), this is an error.
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			if (!support && this.hasState(state)) {
				// We are removing support for a state that the component is currently in.
				this.setState(state, false);
			}

			this.supportedStates_ =
				support ? this.supportedStates_ | state : this.supportedStates_ & ~state;
		}

		/// <summary>
		/// Returns true if the component provides default event handling for the state,
		/// false otherwise.
		/// </summary>
		/// <param name="state">State to check.</param>
		/// <returns>Whether the component provides default event handling for
		/// the state.<returns>
		public bool isAutoState(State state)
		{
			return ((int)this.autoStates_ & (int)state) != 0 && this.isSupportedState(state);
		}

		/// <summary>
		/// Enables or disables automatic event handling for the given state(s).
		/// </summary>
		/// <param name="states">Bit mask of {@link goog.ui.Component.State}s for which</param>
		///     default event handling is to be enabled or disabled.
		/// <param name="enable">Whether the component should provide default event</param>
		///     handling for the state(s).
		public void setAutoStates(State states, bool enable)
		{
			this.autoStates_ =
				enable ? this.autoStates_ | states : this.autoStates_ & ~states;
		}

		/// <summary>
		/// Enables or disables transition events for the given state(s).  Controls
		/// handle state transitions internally by default, and only dispatch state
		/// transition events if explicitly requested to do so by calling this method.
		/// </summary>
		/// <param name="states">Bit mask of {@link goog.ui.Component.State}s for</param>
		///     which transition events should be enabled or disabled.
		/// <param name="enable">Whether transition events should be enabled.</param>
		public void setDispatchTransitionEvents(
			State states, bool enable)
		{
			this.statesWithTransitionEvents_ = enable ?
				this.statesWithTransitionEvents_ | states :
				this.statesWithTransitionEvents_ & ~states;
		}

		/// <summary>
		/// Returns true if the transition into or out of the given state is allowed to
		/// proceed, false otherwise.  A state transition is allowed under the following
		/// conditions:
		/// <ul>
		///   <li>the component supports the state,
		///   <li>the component isn't already in the target state,
		///   <li>either the component is configured not to dispatch events for this
		///       state transition, or a transition event was dispatched and wasn"t
		///       canceled by any event listener, and
		///   <li>the component hasn't been disposed of
		/// </ul>
		/// Considered protected; should only be used within this package and by
		/// subclasses.
		/// </summary>
		/// <param name="state">State to/from which the control is</param>
		///     transitioning.
		/// <param name="enable">Whether the control is entering or leaving the state.</param>
		/// <returns>Whether the state transition is allowed to proceed.</returns>
		public bool isTransitionAllowed(State state, bool enable)
		{
			return this.isSupportedState(state) && this.hasState(state) != enable &&
				(((int)this.statesWithTransitionEvents_ & (int)state) == 0 ||
				 this.getElement().DispatchEvent(new Event(
					goog.ui.Component.getStateTransitionEvent(state, enable)))) &&
				!this.isDisposed();
		}

		// Default event handlers, to be overridden in subclasses.


		/// <summary>
		/// Handles mouseover events.  Dispatches an ENTER event; if the event isn"t
		/// canceled, the component is enabled, and it supports auto-highlighting,
		/// highlights the component.  Considered protected; should only be used
		/// within this package and by subclasses.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public virtual void handleMouseOver(events.BrowserEvent e)
		{
			// Ignore mouse moves between descendants.
			if (!goog.ui.Control.isMouseEventWithinElement_(e, this.getElement()) &&
				this.dispatchEvent(goog.ui.Component.EventType.ENTER) &&
				this.isEnabled() && this.isAutoState(goog.ui.Component.State.HOVER)) {
				this.setHighlighted(true);
			}
		}

		/// <summary>
		/// Handles mouseout events.  Dispatches a LEAVE event; if the event isn"t
		/// canceled, and the component supports auto-highlighting, deactivates and
		/// un-highlights the component.  Considered protected; should only be used
		/// within this package and by subclasses.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public void handleMouseOut(events.BrowserEvent e)
		{
			if (!goog.ui.Control.isMouseEventWithinElement_(e, this.getElement()) &&
				this.dispatchEvent(goog.ui.Component.EventType.LEAVE)) {
				if (this.isAutoState(goog.ui.Component.State.ACTIVE)) {
					// Deactivate on mouseout; otherwise we lose track of the mouse button.
					this.setActive(false);
				}
				if (this.isAutoState(goog.ui.Component.State.HOVER)) {
					this.setHighlighted(false);
				}
			}
		}

		private void nullFunction_(events.Event e)
		{
		}

		/// <summary>
		/// Handles contextmenu events.
		/// </summary>
		/// <param name="e">Event to handle.</param>
		public Action<events.Event> handleContextMenu;
		internal bool wasDisabled;
		private readonly Action<events.Event> nullFunction;

		/// <summary>
		/// Checks if a mouse event (mouseover or mouseout) occurred below an element.
		/// </summary>
		/// <param name="e">Mouse event (should be mouseover or</param>
		///     mouseout).
		/// <param name="elem">The ancestor element.</param>
		/// <returns>Whether the event has a relatedTarget (the element the
		/// mouse is coming from) and it's a descendent of elem.<returns>
		private static bool isMouseEventWithinElement_(events.BrowserEvent e, Element elem)
		{
			// If relatedTarget is null, it means there was no previous element (e.g.
			// the mouse moved out of the window).  Assume this means that the mouse
			// event was not within the element.
			return e.relatedTarget != null && goog.dom.contains(elem, (Node)e.relatedTarget);
		}

		/// <summary>
		/// Handles mousedown events.  If the component is enabled, highlights and
		/// activates it.  If the component isn't configured for keyboard access,
		/// prevents it from receiving keyboard focus.  Considered protected; should
		/// only be used within this package and by subclasses.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public virtual void handleMouseDown(events.BrowserEvent e)
		{
			if (this.isEnabled()) {
				// Highlight enabled control on mousedown, regardless of the mouse button.
				if (this.isAutoState(goog.ui.Component.State.HOVER)) {
					this.setHighlighted(true);
				}

				// For the left button only, activate the control, and focus its key event
				// target (if supported).
				if (e.isMouseActionButton()) {
					if (this.isAutoState(goog.ui.Component.State.ACTIVE)) {
						this.setActive(true);
					}
					if (this.renderer_ != null && this.renderer_.isFocusable(this)) {
						this.getKeyEventTarget().Focus();
					}
				}
			}

			// Cancel the default action unless the control allows text selection.
			if (!this.isAllowTextSelection() && e.isMouseActionButton()) {
				e.preventDefault();
			}
		}


		/// <summary>
		/// Handles mouseup events.  If the component is enabled, highlights it.  If
		/// the component has previously been activated, performs its associated action
		/// by calling {@link performActionInternal}, then deactivates it.  Considered
		/// protected; should only be used within this package and by subclasses.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public virtual void handleMouseUp(goog.events.BrowserEvent e)
		{
			if (this.isEnabled()) {
				if (this.isAutoState(goog.ui.Component.State.HOVER)) {
					this.setHighlighted(true);
				}
				if (this.isActive() && this.performActionInternal(e) &&
					this.isAutoState(goog.ui.Component.State.ACTIVE)) {
					this.setActive(false);
				}
			}
		}

		/// <summary>
		/// Handles dblclick events.  Should only be registered if the user agent is
		/// IE.  If the component is enabled, performs its associated action by calling
		/// {@link performActionInternal}.  This is used to allow more performant
		/// buttons in IE.  In IE, no mousedown event is fired when that mousedown will
		/// trigger a dblclick event.  Because of this, a user clicking quickly will
		/// only cause ACTION events to fire on every other click.  This is a workaround
		/// to generate ACTION events for every click.  Unfortunately, this workaround
		/// won't ever trigger the ACTIVE state.  This is roughly the same behaviour as
		/// if this were a "button" element with a listener on mouseup.  Considered
		/// protected; should only be used within this package and by subclasses.
		/// </summary>
		/// <param name="e">Mouse event to handle.</param>
		public void handleDblClick(goog.events.Event e)
		{
			if (this.isEnabled()) {
				this.performActionInternal(e);
			}
		}

		/// <summary>
		/// Performs the appropriate action when the control is activated by the user.
		/// The default implementation first updates the checked and selected state of
		/// controls that support them, then dispatches an ACTION event.  Considered
		/// protected; should only be used within this package and by subclasses.
		/// </summary>
		/// <param name="e">Event that triggered the action.</param>
		/// <returns>Whether the action is allowed to proceed.</returns>
		public virtual bool performActionInternal(goog.events.Event e)
		{
			if (this.isAutoState(goog.ui.Component.State.CHECKED)) {
				this.setChecked(!this.isChecked());
			}
			if (this.isAutoState(goog.ui.Component.State.SELECTED)) {
				this.setSelected(true);
			}
			if (this.isAutoState(goog.ui.Component.State.OPENED)) {
				this.setOpen(!this.isOpen());
			}

			var actionEvent =
				new goog.events.Event(goog.ui.Component.EventType.ACTION, this);
			if (e != null) {
				actionEvent.altKey = e.altKey;
				actionEvent.ctrlKey = e.ctrlKey;
				actionEvent.metaKey = e.metaKey;
				actionEvent.shiftKey = e.shiftKey;
				actionEvent.platformModifierKey = e.platformModifierKey;
			}
			return this.dispatchEvent(actionEvent);
		}

		/// <summary>
		/// Handles focus events on the component's key event target element.  If the
		/// component is focusable, updates its state and styling to indicate that it
		/// now has keyboard focus.  Considered protected; should only be used within
		/// this package and by subclasses.  <b>Warning:</b> IE dispatches focus and
		/// blur events asynchronously!
		/// </summary>
		/// <param name="e">Focus event to handle.</param>
		public void handleFocus(goog.events.Event e)
		{
			if (this.isAutoState(goog.ui.Component.State.FOCUSED)) {
				this.setFocused(true);
			}
		}

		/// <summary>
		/// Handles blur events on the component's key event target element.  Always
		/// deactivates the component.  In addition, if the component is focusable,
		/// updates its state and styling to indicate that it no longer has keyboard
		/// focus.  Considered protected; should only be used within this package and
		/// by subclasses.  <b>Warning:</b> IE dispatches focus and blur events
		/// asynchronously!
		/// </summary>
		/// <param name="e"></param>
		public void handleBlur(goog.events.Event e)
		{
			if (this.isAutoState(goog.ui.Component.State.ACTIVE)) {
				this.setActive(false);
			}
			if (this.isAutoState(goog.ui.Component.State.FOCUSED)) {
				this.setFocused(false);
			}
		}

		/// <summary>
		/// Attempts to handle a keyboard event, if the component is enabled and visible,
		/// by calling {@link handleKeyEventInternal}.  Considered protected; should only
		/// be used within this package and by subclasses.
		/// </summary>
		/// <param name="e">Key event to handle.</param>
		/// <returns>Whether the key event was handled.</returns>
		public virtual bool handleKeyEvent(goog.events.KeyEvent e)
		{
			if (this.isVisible() && this.isEnabled() && this.handleKeyEventInternal(e)) {
				e.preventDefault();
				e.stopPropagation();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Attempts to handle a keyboard event; returns true if the event was handled,
		/// false otherwise.  Considered protected; should only be used within this
		/// package and by subclasses.
		/// </summary>
		/// <param name="e">Key event to handle.</param>
		/// <returns>Whether the key event was handled.</returns>
		protected virtual bool handleKeyEventInternal(goog.events.KeyEvent e)
		{
			return e.keyCode == (int)goog.events.KeyCodes.ENTER &&
				this.performActionInternal(e);
		}

		private class IeMouseEventSequenceSimulator_ : events.EventTarget
		{
			private Control control_;
			private bool clickExpected_;
			private events.EventHandler handler_;

			/// <summary>
			/// A singleton that helps goog.ui.Control instances play well with screen
			/// readers.  It necessitated by shortcomings in IE, and need not be
			/// instantiated in any other browser.
			///
			/// In most cases, a click on a goog.ui.Control results in a sequence of events:
			/// MOUSEDOWN, MOUSEUP and CLICK.  UI controls rely on this sequence since most
			/// behavior is trigged by MOUSEDOWN and MOUSEUP.  But when IE is used with some
			/// traditional screen readers (JAWS, NVDA and perhaps others), IE only sends
			/// the CLICK event, resulting in the control being unresponsive.  This class
			/// monitors the sequence of these events, and if it detects a CLICK event not
			/// not preceded by a MOUSEUP event, directly calls the control's event handlers
			/// for MOUSEDOWN, then MOUSEUP.  While the resulting sequence is different from
			/// the norm (the CLICK comes first instead of last), testing thus far shows
			/// the resulting behavior to be correct.
			///
			/// See http://goo.gl/qvQR4C for more details.
			/// </summary>
			/// <param name="control"></param>
			public IeMouseEventSequenceSimulator_(Control control)
				: base()
			{
				this.control_ = control;

				this.clickExpected_ = false;

				/** @private @const {!goog.events.EventHandler<
				 *                       !goog.ui.Control.IeMouseEventSequenceSimulator_>}
				 */
				this.handler_ = new goog.events.EventHandler(this);
				this.registerDisposable(this.handler_);

				var element = this.control_.getElementStrict();
				this.handler_
					.listen(element, goog.events.EventType.MOUSEDOWN, new Action<events.BrowserEvent>(this.handleMouseDown_))
					.listen(element, goog.events.EventType.MOUSEUP, new Action<events.BrowserEvent>(this.handleMouseUp_))
					.listen(element, goog.events.EventType.CLICK, new Action<events.Event>(this.handleClick_));
			}

			/// <summary>
			/// Whether this browser supports synthetic MouseEvents.
			///
			/// See https://msdn.microsoft.com/library/dn905219(v=vs.85).aspx for details.
			/// </summary>
			private static readonly bool SYNTHETIC_EVENTS_ =
				!goog.userAgent.IE || goog.userAgent.isDocumentModeOrHigher(9);


			private void handleMouseDown_(events.BrowserEvent e)
			{
				this.clickExpected_ = false;
			}

			private void handleMouseUp_(events.BrowserEvent e)
			{
				this.clickExpected_ = true;
			}

			/// <summary>
			/// </summary>
			/// <param name="e"></param>
			/// <param name="typeArg"></param>
			private static MouseEvent makeLeftMouseEvent_(
				MouseEvent e, string typeArg)
			{
				//Script.Write("use strict");

				if (!goog.ui.Control.IeMouseEventSequenceSimulator_.SYNTHETIC_EVENTS_) {
					// IE < 9 does not support synthetic mouse events. Therefore, reuse the
					// existing MouseEvent by overwriting the read only button and type
					// properties. As IE < 9 does not support ES5 strict mode this will not
					// generate an exception even when the script specifies "use strict".
					e.Button = (int)goog.events.BrowserEvent.MouseButton.LEFT;
					e.Type = typeArg;
					return e;
				}

				var ev = new MouseEvent(Document.CreateEvent("MouseEvents"));
				ev.initMouseEvent(
					typeArg, e.Bubbles, e.Cancelable,
					e.View ?? null,  // IE9 errors if view is undefined
					e.Detail, (int)e.ScreenX, (int)e.ScreenY, (int)e.ClientX, (int)e.ClientY, e.CtrlKey, e.AltKey,
					e.ShiftKey, e.MetaKey, (ushort)goog.events.BrowserEvent.MouseButton.LEFT,
					e.RelatedTarget ?? null);  // IE9 errors if relatedTarget is undefined
				return ev;
			}


			/// <summary>
			/// </summary>
			/// <param name="e"></param>
			private void handleClick_(goog.events.Event e)
			{
				if (this.clickExpected_) {
					// This is the end of a normal click sequence: mouse-down, mouse-up, click.
					// Assume appropriate actions have already been performed.
					this.clickExpected_ = false;
					return;
				}

				// For click events not part of a normal sequence, similate the mouse-down and
				// mouse-up events by creating synthetic events for each and directly invoke
				// the corresponding event listeners in order.

				var browserEvent = (goog.events.BrowserEvent)(e);

				var ev = (MouseEvent)(browserEvent.getBrowserEvent());
				var origEventButton = ev.Button;
				var origEventType = ev.Type;

				var down = goog.ui.Control.IeMouseEventSequenceSimulator_.makeLeftMouseEvent_(
					ev, goog.events.EventType.MOUSEDOWN);
				this.control_.handleMouseDown(
					new goog.events.BrowserEvent(down, (EventTarget)browserEvent.currentTarget));

				var up = goog.ui.Control.IeMouseEventSequenceSimulator_.makeLeftMouseEvent_(
					ev, goog.events.EventType.MOUSEUP);
				this.control_.handleMouseUp(
					new goog.events.BrowserEvent(up, (EventTarget)browserEvent.currentTarget));

				if (goog.ui.Control.IeMouseEventSequenceSimulator_.SYNTHETIC_EVENTS_) {
					// This browser supports synthetic events. Avoid resetting the read only
					// properties (type, button) as they were not overwritten and writing them
					// results in an exception when running in ES5 strict mode.
					return;
				}

				// Restore original values for click handlers that have not yet been invoked.
				ev.Button = origEventButton;
				ev.Type = origEventType;
			}

			public override void disposeInternal()
			{
				this.control_ = null;
				base.disposeInternal();
			}
		}
	}

	public class ControlContent : Union<string, Node, NodeList, JsArray<Node>>
	{
		public ControlContent(object value)
			: base(value)
		{
		}

		public bool IsArray()
		{
			return Is<JsArray<Node>>();
		}

		public JsArray<Node> AsArray()
		{
			return As<JsArray<Node>>();
		}

		public bool IsString()
		{
			return Is<string>();
		}

		public string AsString()
		{
			return As<string>();
		}

		public Union<string, Node> AsNodeOrString()
		{
			return new Union<string, Node>(Value);
		}
	}
}
