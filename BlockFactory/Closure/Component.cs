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
 * @fileoverview Abstract class for all UI components. This defines the standard
 * design pattern that all UI components should follow.
 *
 * @author attila@google.com (Attila Bodis)
 * @see ../demos/samplecomponent.html
 * @see http://code.google.com/p/closure-library/wiki/IntroToComponents
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class Component : goog.events.EventTarget
	{
		protected dom.DomHelper dom_;
		protected HTMLElement element_;
		private goog.events.EventHandler googUiComponentHandler_;
		bool inDocument_;
		goog.ui.Component parent_;
		protected JsArray<goog.ui.Component> children_;
		Dictionary<string, goog.ui.Component> childIndex_;
		private static bool defaultRightToLeft_;
		private string id_;
		private static int nextId_;
		private bool? rightToLeft_;
		private object model_;
		private bool wasDecorated_;

		/// <summary>
		/// Whether to support calling decorate with an element that is
		/// not yet in the document. If true, we check if the element is in the
		/// document, and avoid calling enterDocument if it isn't. If false, we
		/// maintain legacy behavior (always call enterDocument from decorate).
		/// </summary>
		public const bool ALLOW_DETACHED_DECORATION = false;

		/// <summary>
		/// </summary>
		/// Default implementation of UI component.
		/// <param name="opt_domHelper">Optional DOM helper.</param>
		public Component(dom.DomHelper opt_domHelper = null)
		{
			this.dom_ = opt_domHelper ?? goog.dom.getDomHelper();
		}

		/// <summary>
		/// Common events fired by components so that event propagation is useful.  Not
		/// all components are expected to dispatch or listen for all event types.
		/// Events dispatched before a state transition should be cancelable to prevent
		/// the corresponding state change.
		/// </summary>
		public class EventType
		{
			/// <summary>Dispatched before the component becomes visible.</summary>
			public const string BEFORE_SHOW = "beforeshow";

			/// <summary>
			/// Dispatched after the component becomes visible.
			/// NOTE(user): For goog.ui.Container, this actually fires before containers
			/// are shown.  Use goog.ui.Container.EventType.AFTER_SHOW if you want an event
			/// that fires after a goog.ui.Container is shown.
			/// </summary>
			public const string SHOW = "show";

			/// <summary>Dispatched before the component becomes hidden.</summary>
			public const string HIDE = "hide";

			/// <summary>Dispatched before the component becomes disabled.</summary>
			public const string DISABLE = "disable";

			/// <summary>Dispatched before the component becomes enabled.</summary>
			public const string ENABLE = "enable";

			/// <summary>Dispatched before the component becomes highlighted.</summary>
			public const string HIGHLIGHT = "highlight";

			/// <summary>Dispatched before the component becomes un-highlighted.</summary>
			public const string UNHIGHLIGHT = "unhighlight";

			/// <summary>Dispatched before the component becomes activated.</summary>
			public const string ACTIVATE = "activate";

			/// <summary>Dispatched before the component becomes deactivated.</summary>
			public const string DEACTIVATE = "deactivate";

			/// <summary>Dispatched before the component becomes selected.</summary>
			public const string SELECT = "select";

			/// <summary>Dispatched before the component becomes un-selected.</summary>
			public const string UNSELECT = "unselect";

			/// <summary>Dispatched before a component becomes checked.</summary>
			public const string CHECK = "check";

			/// <summary>Dispatched before a component becomes un-checked.</summary>
			public const string UNCHECK = "uncheck";

			/// <summary>Dispatched before a component becomes focused.</summary>
			public const string FOCUS = "focus";

			/// <summary>Dispatched before a component becomes blurred.</summary>
			public const string BLUR = "blur";

			/// <summary>Dispatched before a component is opened (expanded).</summary>
			public const string OPEN = "open";

			/// <summary>Dispatched before a component is closed (collapsed).</summary>
			public const string CLOSE = "close";

			/// <summary>Dispatched after a component is moused over.</summary>
			public const string ENTER = "enter";

			/// <summary>Dispatched after a component is moused out of.</summary>
			public const string LEAVE = "leave";

			/// <summary>Dispatched after the user activates the component.</summary>
			public const string ACTION = "action";

			/// <summary>Dispatched after the external-facing state of a component is changed.</summary>
			public const string CHANGE = "change";
		}

		/// <summary>
		/// Errors thrown by the component.
		/// </summary>
		public class Error
		{
			public const string NOT_SUPPORTED = "Method not supported";
			public const string DECORATE_INVALID = "Invalid element to decorate";
			public const string ALREADY_RENDERED = "Component already rendered";
			public const string STATE_INVALID = "Invalid component state";
			public const string CHILD_INDEX_OUT_OF_BOUNDS = "Child component index out of bounds";
			public const string PARENT_UNABLE_TO_BE_SET = "Unable to set parent component";
			public const string NOT_OUR_CHILD = "Child is not in parent component";
			public const string NOT_IN_DOCUMENT = "Operation not supported while component is not in document";
		}


		/// <summary>
		/// Common component states.  Components may have distinct appearance depending
		/// on what state(s) apply to them.  Not all components are expected to support
		/// all states.
		/// </summary>
		[Flags]
		public enum State
		{
			ALL = 0xFF,
			DISABLED = 0x01,
			HOVER = 0x02,
			ACTIVE = 0x04,
			SELECTED = 0x08,
			CHECKED = 0x10,
			FOCUSED = 0x20,
			OPENED = 0x40
		}

		/// <summary>
		/// Static helper method; returns the type of event components are expected to
		/// dispatch when transitioning to or from the given state.
		/// </summary>
		/// <param name="state">State to/from which the component</param>
		///     is transitioning.
		/// <param name="isEntering">Whether the component is entering or leaving the</param>
		///     state.
		/// <returns>Event type to dispatch.</returns>
		public static string getStateTransitionEvent(State state, bool isEntering)
		{
			switch (state) {
			case goog.ui.Component.State.DISABLED:
				return isEntering ? goog.ui.Component.EventType.DISABLE :
									goog.ui.Component.EventType.ENABLE;
			case goog.ui.Component.State.HOVER:
				return isEntering ? goog.ui.Component.EventType.HIGHLIGHT :
									goog.ui.Component.EventType.UNHIGHLIGHT;
			case goog.ui.Component.State.ACTIVE:
				return isEntering ? goog.ui.Component.EventType.ACTIVATE :
									goog.ui.Component.EventType.DEACTIVATE;
			case goog.ui.Component.State.SELECTED:
				return isEntering ? goog.ui.Component.EventType.SELECT :
									goog.ui.Component.EventType.UNSELECT;
			case goog.ui.Component.State.CHECKED:
				return isEntering ? goog.ui.Component.EventType.CHECK :
									goog.ui.Component.EventType.UNCHECK;
			case goog.ui.Component.State.FOCUSED:
				return isEntering ? goog.ui.Component.EventType.FOCUS :
									goog.ui.Component.EventType.BLUR;
			case goog.ui.Component.State.OPENED:
				return isEntering ? goog.ui.Component.EventType.OPEN :
									goog.ui.Component.EventType.CLOSE;
			default:
				// Fall through.
				break;
			}

			// Invalid state.
			throw new Exception(goog.ui.Component.Error.STATE_INVALID);
		}

		/// <summary>
		/// Set the default right-to-left value. This causes all component's created from
		/// this point forward to have the given value. This is useful for cases where
		/// a given page is always in one directionality, avoiding unnecessary
		/// right to left determinations.
		/// </summary>
		/// <param name="rightToLeft">Whether the components should be rendered</param>
		///     right-to-left. Null iff components should determine their directionality.
		internal static void setDefaultRightToLeft(bool rightToLeft)
		{
			goog.ui.Component.defaultRightToLeft_ = rightToLeft;
		}

		/// <summary>
		/// Gets the unique ID for the instance of this component.  If the instance
		/// doesn't already have an ID, generates one on the fly.
		/// </summary>
		/// <returns>Unique component ID.</returns>
		public string getId()
		{
			//return this.id_ ?? (this.id_ = this.idGenerator_.getNextUniqueId());
			return this.id_ ?? (this.id_ = ":" + (nextId_++).ToString());
		}

		/// <summary>
		/// Assigns an ID to this component instance.  It is the caller's responsibility
		/// to guarantee that the ID is unique.  If the component is a child of a parent
		/// component, then the parent component's child index is updated to reflect the
		/// new ID; this may throw an error if the parent already has a child with an ID
		/// that conflicts with the new ID.
		/// </summary>
		/// <param name="id">Unique component ID.</param>
		public void setId(string id)
		{
			if (this.parent_ != null && this.parent_.childIndex_ != null) {
				// Update the parent's child index.
				this.parent_.childIndex_.Remove(this.id_);
				this.parent_.childIndex_.Add(id, this);
			}

			// Update the component ID.
			this.id_ = id;
		}

		/// <summary>
		/// Gets the component's element.
		/// </summary>
		/// <returns>The element for the component.</returns>
		public virtual HTMLElement getElement()
		{
			return this.element_;
		}

		/// <summary>
		/// Gets the component's element. This differs from getElement in that
		/// it assumes that the element exists (i.e. the component has been
		/// rendered/decorated) and will cause an assertion error otherwise (if
		/// assertion is enabled).
		/// </summary>
		/// <returns>The element for the component.</returns>
		public HTMLElement getElementStrict()
		{
			var el = this.element_;
			goog.asserts.assert(
				el != null, "Can not call getElementStrict before rendering/decorating.");
			return el;
		}

		/// <summary>
		/// Sets the component's root element to the given element.  Considered
		/// protected and final.
		///
		/// This should generally only be called during createDom. Setting the element
		/// does not actually change which element is rendered, only the element that is
		/// associated with this UI component.
		///
		/// This should only be used by subclasses and its associated renderers.
		/// </summary>
		/// <param name="element">Root element for the component.</param>
		public void setElementInternal(HTMLElement element)
		{
			this.element_ = element;
		}


		/// <summary>
		/// Returns an array of all the elements in this component's DOM with the
		/// provided className.
		/// </summary>
		/// <param name="className">The name of the class to look for.</param>
		/// <returns>The items found with the class name provided.</returns>
		public JsArray<Element> getElementsByClass(string className)
		{
			return this.element_ != null ?
				this.dom_.getElementsByClass(className, this.element_) : new JsArray<Element>();
		}

		/// <summary>
		/// Returns the first element in this component's DOM with the provided
		/// className.
		/// </summary>
		/// <param name="className">The name of the class to look for.</param>
		/// <returns>The first item with the class name provided.</returns>
		public Element getElementByClass(string className)
		{
			return this.element_ != null ? this.dom_.getElementByClass(className, this.element_) : null;
		}

		/// <summary>
		/// Similar to {@code getElementByClass} except that it expects the
		/// element to be present in the dom thus returning a required value. Otherwise,
		/// will assert.
		/// </summary>
		/// <param name="className">The name of the class to look for.</param>
		/// <returns>The first item with the class name provided.</returns>
		public Element getRequiredElementByClass(string className)
		{
			var el = this.getElementByClass(className);
			goog.asserts.assert(
				el != null, "Expected element in component with class: %s", className);
			return el;
		}


		/// <summary>
		/// Returns the event handler for this component, lazily created the first time
		/// this method is called.
		/// </summary>
		/// <returns>Event handler for this component.</returns>
		public goog.events.EventHandler getHandler()
		{
			// TODO(user): templated "this" values currently result in "this" being
			// "unknown" in the body of the function.
			var self = (goog.ui.Component)(this);
			if (self.googUiComponentHandler_ == null) {
				self.googUiComponentHandler_ = new goog.events.EventHandler(self);
			}
			return self.googUiComponentHandler_;
		}

		/// <summary>
		/// Sets the parent of this component to use for event bubbling.  Throws an error
		/// if the component already has a parent or if an attempt is made to add a
		/// component to itself as a child.  Callers must use {@code removeChild}
		/// or {@code removeChildAt} to remove components from their containers before
		/// calling this method.
		/// @see goog.ui.Component#removeChild
		/// @see goog.ui.Component#removeChildAt
		/// </summary>
		/// <param name="parent">The parent component.</param>
		public void setParent(Component parent)
		{
			if (this == parent) {
				// Attempting to add a child to itself is an error.
				throw new Exception(goog.ui.Component.Error.PARENT_UNABLE_TO_BE_SET);
			}

			if (parent != null && this.parent_ != null && this.id_ != null && this.parent_.getChild(this.id_) != null &&
				this.parent_ != parent) {
				// This component is already the child of some parent, so it should be
				// removed using removeChild/removeChildAt first.
				throw new Exception(goog.ui.Component.Error.PARENT_UNABLE_TO_BE_SET);
			}

			this.parent_ = parent;
			base.setParentEventTarget(parent);
		}

		/// <summary>
		/// Returns the component's parent, if any.
		/// </summary>
		/// <returns>The parent component.</returns>
		public virtual Component getParent()
		{
			return this.parent_;
		}


		/// <summary>
		/// Overrides {@link goog.events.EventTarget#setParentEventTarget} to throw an
		/// error if the parent component is set, and the argument is not the parent.
		/// </summary>
		public override void setParentEventTarget(goog.events.EventTarget parent)
		{
			if (this.parent_ != null && this.parent_ != parent) {
				throw new Exception(goog.ui.Component.Error.NOT_SUPPORTED);
			}
			base.setParentEventTarget(parent);
		}


		/// <summary>
		/// Returns the dom helper that is being used on this component.
		/// </summary>
		/// <returns>The dom helper used on this component.</returns>
		public goog.dom.DomHelper getDomHelper()
		{
			return this.dom_;
		}

		/// <summary>
		/// Determines whether the component has been added to the document.
		/// </summary>
		/// <returns>TRUE if rendered. Otherwise, FALSE.</returns>
		public bool isInDocument()
		{
			return this.inDocument_;
		}

		/// <summary>
		/// Creates the initial DOM representation for the component.  The default
		/// implementation is to set this.element_ = div.
		/// </summary>
		public virtual void createDom()
		{
			this.element_ = this.dom_.createElement(goog.dom.TagName.DIV);
		}

		/// <summary>
		/// Renders the component.  If a parent element is supplied, the component"s
		/// element will be appended to it.  If there is no optional parent element and
		/// the element doesn't have a parentNode then it will be appended to the
		/// document body.
		///
		/// If this component has a parent component, and the parent component is
		/// not in the document already, then this will not call {@code enterDocument}
		/// on this component.
		///
		/// Throws an Error if the component is already rendered.
		/// </summary>
		/// <param name="opt_parentElement">Optional parent element to render the</param>
		///    component into.
		public void render(HTMLElement opt_parentElement = null)
		{
			this.render_(opt_parentElement);
		}


		/// <summary>
		/// Renders the component before another element. The other element should be in
		/// the document already.
		///
		/// Throws an Error if the component is already rendered.
		/// </summary>
		/// <param name="sibling">Node to render the component before.</param>
		public void renderBefore(Node sibling)
		{
			this.render_((HTMLElement)(sibling.ParentNode), (HTMLElement)sibling);
		}


		/// <summary>
		/// Renders the component.  If a parent element is supplied, the component"s
		/// element will be appended to it.  If there is no optional parent element and
		/// the element doesn't have a parentNode then it will be appended to the
		/// document body.
		///
		/// If this component has a parent component, and the parent component is
		/// not in the document already, then this will not call {@code enterDocument}
		/// on this component.
		///
		/// Throws an Error if the component is already rendered.
		/// </summary>
		/// <param name="opt_parentElement">Optional parent element to render the</param>
		///    component into.
		/// <param name="opt_beforeNode">Node before which the component is to</param>
		///    be rendered.  If left out the node is appended to the parent element.
		private void render_(HTMLElement opt_parentElement = null, HTMLElement opt_beforeNode = null)
		{
			if (this.inDocument_) {
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			if (this.element_ == null) {
				this.createDom();
			}

			if (opt_parentElement != null) {
				opt_parentElement.InsertBefore(this.element_, opt_beforeNode);
			}
			else {
				this.dom_.getDocument().Body.AppendChild(this.element_);
			}

			// If this component has a parent component that isn't in the document yet,
			// we don't call enterDocument() here.  Instead, when the parent component
			// enters the document, the enterDocument() call will propagate to its
			// children, including this one.  If the component doesn't have a parent
			// or if the parent is already in the document, we call enterDocument().
			if (this.parent_ == null || this.parent_.isInDocument()) {
				this.enterDocument();
			}
		}

		/// <summary>
		/// Decorates the element for the UI component. If the element is in the
		/// document, the enterDocument method will be called.
		///
		/// If goog.ui.Component.ALLOW_DETACHED_DECORATION is false, the caller must
		/// pass an element that is in the document.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		public void decorate(HTMLElement element)
		{
			if (this.inDocument_) {
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}
			else if (element != null && this.canDecorate(element)) {
				this.wasDecorated_ = true;

				// Set the DOM helper of the component to match the decorated element.
				var doc = goog.dom.getOwnerDocument(element);
				if (this.dom_ == null || this.dom_.getDocument() != doc) {
					this.dom_ = goog.dom.getDomHelper(element);
				}

				// Call specific component decorate logic.
				this.decorateInternal(element);

				// If supporting detached decoration, check that element is in doc.
				if (!goog.ui.Component.ALLOW_DETACHED_DECORATION ||
					goog.dom.contains(doc, element)) {
					this.enterDocument();
				}
			}
			else {
				throw new Exception(goog.ui.Component.Error.DECORATE_INVALID);
			}
		}


		/// <summary>
		/// Determines if a given element can be decorated by this type of component.
		/// This method should be overridden by inheriting objects.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>True if the element can be decorated, false otherwise.</returns>
		public virtual bool canDecorate(Element element)
		{
			return true;
		}


		/// <summary>
		/// </summary>
		/// <returns>Whether the component was decorated.</returns>
		public bool wasDecorated()
		{
			return this.wasDecorated_;
		}


		/// <summary>
		/// Actually decorates the element. Should be overridden by inheriting objects.
		/// This method can assume there are checks to ensure the component has not
		/// already been rendered have occurred and that enter document will be called
		/// afterwards. This method is considered protected.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		protected virtual void decorateInternal(HTMLElement element)
		{
			this.element_ = element;
		}


		/// <summary>
		/// Called when the component's element is known to be in the document. Anything
		/// using document.getElementById etc. should be done at this stage.
		///
		/// If the component contains child components, this call is propagated to its
		/// children.
		/// </summary>
		public virtual void enterDocument()
		{
			this.inDocument_ = true;

			// Propagate enterDocument to child components that have a DOM, if any.
			// If a child was decorated before entering the document (permitted when
			// goog.ui.Component.ALLOW_DETACHED_DECORATION is true), its enterDocument
			// will be called here.
			this.forEachChild((child) => {
				if (!child.isInDocument() && child.getElement() != null) {
					child.enterDocument();
				}
			});
		}

		/// <summary>
		/// Called by dispose to clean up the elements and listeners created by a
		/// component, or by a parent component/application who has removed the
		/// component from the document but wants to reuse it later.
		///
		/// If the component contains child components, this call is propagated to its
		/// children.
		///
		/// It should be possible for the component to be rendered again once this method
		/// has been called.
		/// </summary>
		public virtual void exitDocument()
		{
			// Propagate exitDocument to child components that have been rendered, if any.
			this.forEachChild((child) => {
				if (child.isInDocument()) {
					child.exitDocument();
				}
			});

			if (this.googUiComponentHandler_ != null) {
				this.googUiComponentHandler_.removeAll();
			}

			this.inDocument_ = false;
		}

		/// <summary>
		/// Disposes of the component.  Calls {@code exitDocument}, which is expected to
		/// remove event handlers and clean up the component.  Propagates the call to
		/// the component's children, if any. Removes the component's DOM from the
		/// document unless it was decorated.
		/// </summary>
		public override void disposeInternal()
		{
			if (this.inDocument_) {
				this.exitDocument();
			}

			if (this.googUiComponentHandler_ != null) {
				this.googUiComponentHandler_.dispose();
				Script.Delete(ref this.googUiComponentHandler_);
			}

			// Disposes of the component's children, if any.
			this.forEachChild((child) => { child.dispose(); });

			// Detach the component's element from the DOM, unless it was decorated.
			if (!this.wasDecorated_ && this.element_ != null) {
				goog.dom.removeNode(this.element_);
			}

			this.children_ = null;
			this.childIndex_ = null;
			this.element_ = null;
			this.model_ = null;
			this.parent_ = null;

			base.disposeInternal();
		}


		/// <summary>
		/// Helper function for subclasses that gets a unique id for a given fragment,
		/// this can be used by components to generate unique string ids for DOM
		/// elements.
		/// </summary>
		/// <param name="idFragment">A partial id.</param>
		/// <returns>Unique element id.</returns>
		public string makeId(string idFragment)
		{
			return this.getId() + "." + idFragment;
		}


		/// <summary>
		/// Makes a collection of ids.  This is a convenience method for makeId.  The
		/// object's values are the id fragments and the new values are the generated
		/// ids.  The key will remain the same.
		/// </summary>
		/// <param name="object">The object that will be used to create the ids.</param>
		/// <returns>An object of id keys to generated ids.</returns>
		public Dictionary<string, string> makeIds(Dictionary<string, string> obj)
		{
			var ids = new Dictionary<string, string>();
			foreach (var key in obj.Keys) {
				ids[key] = this.makeId(obj[key]);
			}
			return ids;
		}


		/// <summary>
		/// Returns the model associated with the UI component.
		/// </summary>
		/// <returns>The model.</returns>
		public object getModel()
		{
			return this.model_;
		}

		/// <summary>
		/// Sets the model associated with the UI component.
		/// </summary>
		/// <param name="obj">The model.</param>
		public void setModel(object obj)
		{
			this.model_ = obj;
		}


		/// <summary>
		/// Helper function for returning the fragment portion of an id generated using
		/// makeId().
		/// </summary>
		/// <param name="id">Id generated with makeId().</param>
		/// <returns>Fragment.</returns>
		public string getFragmentFromId(string id)
		{
			return id.Substring(this.getId().Length + 1);
		}


		/// <summary>
		/// Helper function for returning an element in the document with a unique id
		/// generated using makeId().
		/// </summary>
		/// <param name="idFragment">The partial id.</param>
		/// <returns>The element with the unique id, or null if it cannot be
		/// found.<returns>
		public Element getElementByFragment(string idFragment)
		{
			if (!this.inDocument_) {
				throw new Exception(goog.ui.Component.Error.NOT_IN_DOCUMENT);
			}
			return this.dom_.getElement(this.makeId(idFragment));
		}


		/// <summary>
		/// Adds the specified component as the last child of this component.  See
		/// {@link goog.ui.Component#addChildAt} for detailed semantics.
		///
		/// @see goog.ui.Component#addChildAt
		/// </summary>
		/// <param name="child">The new child component.</param>
		/// <param name="opt_render">If true, the child component will be rendered</param>
		///    into the parent.
		public virtual void addChild(Component child, bool opt_render = false)
		{
			// TODO(gboyer): addChildAt(child, this.getChildCount(), false) will
			// reposition any already-rendered child to the end.  Instead, perhaps
			// addChild(child, false) should never reposition the child; instead, clients
			// that need the repositioning will use addChildAt explicitly.  Right now,
			// clients can get around this by calling addChild before calling decorate.
			this.addChildAt(child, this.getChildCount(), opt_render);
		}

		/// <summary>
		/// Adds the specified component as a child of this component at the given
		/// 0-based index.
		///
		/// Both {@code addChild} and {@code addChildAt} assume the following contract
		/// between parent and child components:
		///  <ul>
		///    <li>the child component's element must be a descendant of the parent
		///        component's element, and
		///    <li>the DOM state of the child component must be consistent with the DOM
		///        state of the parent component (see {@code isInDocument}) in the
		///        steady state -- the exception is to addChildAt(child, i, false) and
		///        then immediately decorate/render the child.
		///  </ul>
		///
		/// In particular, {@code parent.addChild(child)} will throw an error if the
		/// child component is already in the document, but the parent isn't.
		///
		/// Clients of this API may call {@code addChild} and {@code addChildAt} with
		/// {@code opt_render} set to true.  If {@code opt_render} is true, calling these
		/// methods will automatically render the child component's element into the
		/// parent component's element. If the parent does not yet have an element, then
		/// {@code createDom} will automatically be invoked on the parent before
		/// rendering the child.
		///
		/// Invoking {@code parent.addChild(child, true)} will throw an error if the
		/// child component is already in the document, regardless of the parent's DOM
		/// state.
		///
		/// If {@code opt_render} is true and the parent component is not already
		/// in the document, {@code enterDocument} will not be called on this component
		/// at this point.
		///
		/// Finally, this method also throws an error if the new child already has a
		/// different parent, or the given index is out of bounds.
		///
		/// @see goog.ui.Component#addChild
		/// </summary>
		/// <param name="child">The new child component.</param>
		/// <param name="index">0-based index at which the new child component is to be</param>
		///    added; must be between 0 and the current child count (inclusive).
		/// <param name="opt_render">If true, the child component will be rendered</param>
		///    into the parent.
		/// <returns>Nada.</returns>
		public virtual void addChildAt(Component child, int index, bool opt_render = false)
		{
			goog.asserts.assert(child != null, "Provided element must not be null.");

			if (child.inDocument_ && (opt_render || !this.inDocument_)) {
				// Adding a child that's already in the document is an error, except if the
				// parent is also in the document and opt_render is false (e.g. decorate()).
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}

			if (index < 0 || index > this.getChildCount()) {
				// Allowing sparse child arrays would lead to strange behavior, so we don't.
				throw new Exception(goog.ui.Component.Error.CHILD_INDEX_OUT_OF_BOUNDS);
			}

			// Create the index and the child array on first use.
			if (this.childIndex_ == null || this.children_ == null) {
				this.childIndex_ = new Dictionary<string, Component>();
				this.children_ = new JsArray<Component>();
			}

			// Moving child within component, remove old reference.
			if (child.getParent() == this) {
				this.childIndex_[child.getId()] = child;
				this.children_.Remove(child);

				// Add the child to this component.  goog.object.add() throws an error if
				// a child with the same ID already exists.
			}
			else {
				this.childIndex_[child.getId()] = child;
			}

			// Set the parent of the child to this component.  This throws an error if
			// the child is already contained by another component.
			child.setParent(this);
			this.children_.Splice(index, 0, child);

			if (child.inDocument_ && this.inDocument_ && child.getParent() == this) {
				// Changing the position of an existing child, move the DOM node (if
				// necessary).
				var contentElement = this.getContentElement();
				var insertBeforeElement = contentElement.ChildNodes[index];
				if (insertBeforeElement != child.getElement()) {
					contentElement.InsertBefore(child.getElement(), insertBeforeElement);
				}
			}
			else if (opt_render) {
				// If this (parent) component doesn't have a DOM yet, call createDom now
				// to make sure we render the child component's element into the correct
				// parent element (otherwise render_ with a null first argument would
				// render the child into the document body, which is almost certainly not
				// what we want).
				if (this.element_ == null) {
					this.createDom();
				}
				// Render the child into the parent at the appropriate location.  Note that
				// getChildAt(index + 1) returns undefined if inserting at the end.
				// TODO(attila): We should have a renderer with a renderChildAt API.
				var sibling = this.getChildAt(index + 1);
				// render_() calls enterDocument() if the parent is already in the document.
				child.render_(this.getContentElement(), sibling != null ? sibling.element_ : null);
			}
			else if (
			  this.inDocument_ && !child.inDocument_ && child.element_ != null &&
			  child.element_.ParentNode != null &&
			  // Under some circumstances, IE8 implicitly creates a Document Fragment
			  // for detached nodes, so ensure the parent is an Element as it should be.
			  child.element_.ParentNode.NodeType == NodeType.Element) {
				// We don't touch the DOM, but if the parent is in the document, and the
				// child element is in the document but not marked as such, then we call
				// enterDocument on the child.
				// TODO(gboyer): It would be nice to move this condition entirely, but
				// there's a large risk of breaking existing applications that manually
				// append the child to the DOM and then call addChild.
				child.enterDocument();
			}
		}

		/// <summary>
		/// Returns the DOM element into which child components are to be rendered,
		/// or null if the component itself hasn't been rendered yet.  This default
		/// implementation returns the component's root element.  Subclasses with
		/// complex DOM structures must override this method.
		/// </summary>
		/// <returns>Element to contain child elements (null if none).</returns>
		public virtual HTMLElement getContentElement()
		{
			return this.element_;
		}


		/// <summary>
		/// Returns true if the component is rendered right-to-left, false otherwise.
		/// The first time this function is invoked, the right-to-left rendering property
		/// is set if it has not been already.
		/// </summary>
		/// <returns>Whether the control is rendered right-to-left.</returns>
		public bool isRightToLeft()
		{
			if (this.rightToLeft_ == null) {
				this.rightToLeft_ = goog.style.isRightToLeft(
					this.inDocument_ ? this.element_ : this.dom_.getDocument().Body);
			}
			return this.rightToLeft_.Value;
		}

		/// <summary>
		/// Set is right-to-left. This function should be used if the component needs
		/// to know the rendering direction during dom creation (i.e. before
		/// {@link #enterDocument} is called and is right-to-left is set).
		/// </summary>
		/// <param name="rightToLeft">Whether the component is rendered</param>
		///     right-to-left.
		public virtual void setRightToLeft(bool rightToLeft)
		{
			if (this.inDocument_) {
				throw new Exception(goog.ui.Component.Error.ALREADY_RENDERED);
			}
			this.rightToLeft_ = rightToLeft;
		}

		/// <summary>
		/// Returns true if the component has children.
		/// </summary>
		/// <returns>True if the component has children.</returns>
		public bool hasChildren()
		{
			return this.children_ != null && this.children_.Length != 0;
		}

		/// <summary>
		/// Returns the number of children of this component.
		/// </summary>
		/// <returns>The number of children.</returns>
		public int getChildCount()
		{
			return this.children_ != null ? this.children_.Length : 0;
		}

		/// <summary>
		/// Returns an array containing the IDs of the children of this component, or an
		/// empty array if the component has no children.
		/// </summary>
		/// <returns>Child component IDs.</returns>
		public JsArray<string> getChildIds()
		{
			var ids = new JsArray<string>();

			// We don't use goog.@object.getKeys(this.childIndex_) because we want to
			// return the IDs in the correct order as determined by this.children_.
			this.forEachChild((child) => {
				// addChild()/addChildAt() guarantee that the child array isn't sparse.
				ids.Push(child.getId());
			});

			return ids;
		}

		/// <summary>
		/// Returns the child with the given ID, or null if no such child exists.
		/// </summary>
		/// <param name="id">Child component ID.</param>
		/// <returns>The child with the given ID; null if none.</returns>
		public Component getChild(string id)
		{
			// Use childIndex_ for O(1) access by ID.
			return (this.childIndex_ != null && id != null) ?
				this.childIndex_[id] : null;
		}

		/// <summary>
		/// Returns the child at the given index, or null if the index is out of bounds.
		/// </summary>
		/// <param name="index">0-based index.</param>
		/// <returns>The child at the given index; null if none.</returns>
		public Component getChildAt(int index)
		{
			// Use children_ for access by index.
			return this.children_ != null && index >= 0 && index < this.children_.Length ? this.children_[index] : null;
		}

		/// <summary>
		/// Calls the given function on each of this component's children in order.  If
		/// {@code opt_obj} is provided, it will be used as the "this" object in the
		/// function when called.  The function should take two arguments:  the child
		/// component and its 0-based index.  The return value is ignored.
		/// </summary>
		/// <param name="f">The function to call for every</param>
		/// child component; should take 2 arguments (the child and its index).
		/// <param name="opt_obj">Used as the "this" object in f when called.</param>
		/// @template T
		public void forEachChild(Action<Component> p)
		{
			if (this.children_ != null) {
				foreach (var child in children_) {
					p(child);
				}
			}
		}

		/// <summary>
		/// Returns the 0-based index of the given child component, or -1 if no such
		/// child is found.
		/// </summary>
		/// <param name="child">The child component.</param>
		/// <returns>0-based index of the child component; -1 if not found.</returns>
		public int indexOfChild(Component child)
		{
			return (this.children_ != null && child != null) ? Array.IndexOf(this.children_, child) :
				-1;
		}

		/// <summary>
		/// Removes the given child from this component, and returns it.  Throws an error
		/// if the argument is invalid or if the specified child isn't found in the
		/// parent component.  The argument can either be a string (interpreted as the
		/// ID of the child component to remove) or the child component itself.
		///
		/// If {@code opt_unrender} is true, calls {@link goog.ui.component#exitDocument}
		/// on the removed child, and subsequently detaches the child's DOM from the
		/// document.  Otherwise it is the caller's responsibility to clean up the child
		/// component's DOM.
		///
		/// @see goog.ui.Component#removeChildAt
		/// </summary>
		/// <param name="child">The ID of the child to remove,</param>
		///    or the child component itself.
		/// <param name="opt_unrender">If true, calls {@code exitDocument} on the</param>
		///    removed child component, and detaches its DOM from the document.
		/// <returns>The removed component, if any.</returns>
		public virtual Component removeChild(Union<string, Component> child_, bool opt_unrender = false)
		{
			Component child = null;

			if (child_ != null) {
				// Normalize child to be the object and id to be the ID string.  This also
				// ensures that the child is really ours.
				var id = child_.Is<string>() ? child_.As<string>() : child_.As<Component>().getId();
				child = this.getChild(id);

				if (id != null && child != null) {
					this.childIndex_.Remove(id);
					this.children_.Remove(child);

					if (opt_unrender) {
						// Remove the child component's DOM from the document.  We have to call
						// exitDocument first (see documentation).
						child.exitDocument();
						if (child.element_ != null) {
							goog.dom.removeNode(child.element_);
						}
					}

					// Child's parent must be set to null after exitDocument is called
					// so that the child can unlisten to its parent if required.
					child.setParent(null);
				}
			}

			if (child == null) {
				throw new Exception(goog.ui.Component.Error.NOT_OUR_CHILD);
			}

			return /** @type {!goog.ui.Component} */ (child);
		}

		/// <summary>
		/// Removes the child at the given index from this component, and returns it.
		/// Throws an error if the argument is out of bounds, or if the specified child
		/// isn't found in the parent.  See {@link goog.ui.Component#removeChild} for
		/// detailed semantics.
		/// 
		/// @see goog.ui.Component#removeChild
		/// </summary>
		/// <param name="index">0-based index of the child to remove.</param>
		/// <param name="opt_unrender">If true, calls {@code exitDocument} on the
		///    removed child component, and detaches its DOM from the document.</param>
		/// <returns>The removed component, if any.</returns>
		public Component removeChildAt(int index, bool opt_unrender = false)
		{
			// removeChild(null) will throw error.
			return this.removeChild(this.getChildAt(index), opt_unrender);
		}

		/// <summary>
		/// Removes every child component attached to this one and returns them.
		/// 
		/// @see goog.ui.Component#removeChild
		/// </summary>
		/// <param name="opt_unrender">If true, calls {@link #exitDocument} on the
		/// removed child components, and detaches their DOM from the document.</param>
		/// <returns>The removed components if any.</returns>
		public Component[] removeChildren(bool opt_unrender = false)
		{
			var removedChildren = new JsArray<Component>();
			while (this.hasChildren()) {
				removedChildren.Push(this.removeChildAt(0, opt_unrender));
			}
			return removedChildren;
		}
	}
}
