// Copyright 2008 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview Base class for control renderers.
 * TODO(attila):  If the renderer framework works well, pull it into Component.
 *
 * @author attila@google.com (Attila Bodis)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog.ui
{
	public class ControlRenderer
	{
		private static ControlRenderer instance_;
		public static ControlRenderer getInstance()
		{
			if (instance_ == null)
				instance_ = new ControlRenderer();
			return instance_;
		}

		private string cssClassName_ = goog.ui.ControlRenderer.CSS_CLASS;

		/// <summary>
		/// </summary>
		/// Constructs a new renderer and sets the CSS class that the renderer will use
		/// as the base CSS class to apply to all elements rendered by that renderer.
		/// An example to use this function using a color palette:
		///
		/// <pre>
		/// var myCustomRenderer = goog.ui.ControlRenderer.getCustomRenderer(
		///     goog.ui.PaletteRenderer, "my-special-palette");
		/// var newColorPalette = new goog.ui.ColorPalette(
		///     colors, myCustomRenderer, opt_domHelper);
		/// </pre>
		///
		/// Your CSS can look like this now:
		/// <pre>
		/// .my-special-palette { }
		/// .my-special-palette-table { }
		/// .my-special-palette-cell { }
		/// etc.
		/// </pre>
		///
		/// <em>instead</em> of
		/// <pre>
		/// .CSS_MY_SPECIAL_PALETTE .goog-palette { }
		/// .CSS_MY_SPECIAL_PALETTE .goog-palette-table { }
		/// .CSS_MY_SPECIAL_PALETTE .goog-palette-cell { }
		/// etc.
		/// </pre>
		///
		/// You would want to use this functionality when you want an instance of a
		/// component to have specific styles different than the other components of the
		/// same type in your application.  This avoids using descendant selectors to
		/// apply the specific styles to this component.
		///
		/// <param name="ctor">The constructor of the renderer you are trying to</param>
		///     create.
		/// <param name="cssClassName">The name of the CSS class for this renderer.</param>
		/// @return {} An instance of the desired renderer with
		///     its getCssClass() method overridden to return the supplied custom CSS
		///     class name.
		public static goog.ui.ControlRenderer getCustomRenderer(Func<ControlRenderer> ctor, string cssClassName)
		{
			var renderer = ctor();

			/**
			 * Returns the CSS class to be applied to the root element of components
			 * rendered using this renderer.
			 * @return {string} Renderer-specific CSS class.
			 */
			//renderer.getCssClass = () => { return cssClassName; };
			renderer.cssClassName_ = cssClassName;

			return renderer;
		}

		/// <summary>
		/// Default CSS class to be applied to the root element of components rendered
		/// by this renderer.
		/// </summary>
		public static readonly string CSS_CLASS = le.getCssName("goog-control");

		/// <summary>
		/// Array of arrays of CSS classes that we want composite classes added and
		/// removed for in IE6 and lower as a workaround for lack of multi-class CSS
		/// selector support.
		///
		/// Subclasses that have accompanying CSS requiring this workaround should define
		/// their own static IE6_CLASS_COMBINATIONS constant and override
		/// getIe6ClassCombinations to return it.
		///
		/// For example, if your stylesheet uses the selector .button.collapse-left
		/// (and is compiled to .button_collapse-left for the IE6 version of the
		/// stylesheet,) you should include ["button", "collapse-left"] in this array
		/// and the class button_collapse-left will be applied to the root element
		/// whenever both button and collapse-left are applied individually.
		///
		/// Members of each class name combination will be joined with underscores in the
		/// order that they"re defined in the array. You should alphabetize them (for
		/// compatibility with the CSS compiler) unless you are doing something special.
		/// </summary>
		public static JsArray<JsArray<string>> IE6_CLASS_COMBINATIONS = new JsArray<JsArray<string>>();

		/// <summary>
		/// Map of component states to corresponding ARIA attributes.  Since the mapping
		/// of component states to ARIA attributes is neither component- nor
		/// renderer-specific, this is a static property of the renderer class, and is
		/// initialized on first use.
		/// </summary>
		private static Dictionary<goog.ui.Component.State, goog.a11y.aria.State> ariaAttributeMap_;

		/// <summary>
		/// Map of certain ARIA states to ARIA roles that support them. Used for checked
		/// and selected Component states because they are used on Components with ARIA
		/// roles that do not support the corresponding ARIA state.
		/// </summary>
		private static Dictionary<goog.a11y.aria.Role, goog.a11y.aria.State> TOGGLE_ARIA_STATE_MAP_ =
			new Dictionary<a11y.aria.Role, a11y.aria.State> {
				{ goog.a11y.aria.Role.BUTTON, goog.a11y.aria.State.PRESSED },
				{ goog.a11y.aria.Role.CHECKBOX, goog.a11y.aria.State.CHECKED },
				{ goog.a11y.aria.Role.MENU_ITEM, goog.a11y.aria.State.SELECTED },
				{ goog.a11y.aria.Role.MENU_ITEM_CHECKBOX, goog.a11y.aria.State.CHECKED },
				{ goog.a11y.aria.Role.MENU_ITEM_RADIO, goog.a11y.aria.State.CHECKED },
				{ goog.a11y.aria.Role.RADIO, goog.a11y.aria.State.CHECKED },
				{ goog.a11y.aria.Role.TAB, goog.a11y.aria.State.SELECTED },
				{ goog.a11y.aria.Role.TREEITEM, goog.a11y.aria.State.SELECTED }
			};

		/// <summary>
		/// Returns the ARIA role to be applied to the container.
		/// See http://wiki/Main/ARIA for more info.
		/// </summary>
		/// <returns>ARIA role.</returns>
		public virtual goog.a11y.aria.Role getAriaRole()
		{
			return this.ariaRole_;
		}

		/// <summary>
		/// Returns the control's contents wrapped in a DIV, with the renderer's own
		/// CSS class and additional state-specific classes applied to it.
		/// </summary>
		/// <param name="control">Control to render.</param>
		/// <returns>Root element for the control.</returns>
		public virtual HTMLElement createDom(Control control)
		{
			// Create and return DIV wrapping contents.
			var element = control.getDomHelper().createDom(
				goog.dom.TagName.DIV, this.getClassNames(control).Join(" "),
				control.getContent());

			return element;
		}

		/// <summary>
		/// Returns the DOM element into which child components are to be rendered,
		/// or null if the container hasn't been rendered yet.
		/// </summary>
		/// <param name="element">Root element of the container whose content element
		/// is to be returned.</param>
		/// <returns>Element to contain child elements (null if none).</returns>
		public virtual HTMLElement getContentElement(HTMLElement element)
		{
			return element;
		}

		/// <summary>
		/// Updates the control's DOM by adding or removing the specified class name
		/// to/from its root element. May add additional combined classes as needed in
		/// IE6 and lower. Because of this, subclasses should use this method when
		/// modifying class names on the control's root element.
		/// </summary>
		/// <param name="control">Control instance (or root element)
		/// to be updated.</param>
		/// <param name="className">CSS class name to add or remove.</param>
		/// <param name="enable">Whether to add or remove the class name.</param>
		public void enableClassName(Union<Control, HTMLElement> control, string className, bool enable)
		{
			var element = control.Is<Control>() ? control.As<Control>().getElement() : control.As<HTMLElement>();
			if (element != null) {
				var classNames = new JsArray<string>() { className };

				// For IE6, we need to enable any combined classes involving this class
				// as well.
				// TODO(user): Remove this as IE6 is no longer in use.
				if (goog.userAgent.IE && !goog.userAgent.isVersionOrHigher("7")) {
					classNames = this.getAppliedCombinedClassNames_(
						goog.dom.classlist.get(element), className);
					classNames.Push(className);
				}

				goog.dom.classlist.enableAll(element, classNames, enable);
			}
		}

		/// <summary>
		/// </summary>
		/// Updates the control's DOM by adding or removing the specified extra class
		/// name to/from its element.
		/// <param name="control">Control to be updated.</param>
		/// <param name="className">CSS class name to add or remove.</param>
		/// <param name="enable">Whether to add or remove the class name.</param>
		public void enableExtraClassName(
			goog.ui.Control control, string className, bool enable)
		{
			// The base class implementation is trivial; subclasses should override as
			// needed.
			this.enableClassName(control, className, enable);
		}

		/// <summary>
		/// Returns true if this renderer can decorate the element, false otherwise.
		/// The default implementation always returns true.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Whether the renderer can decorate the element.</returns>
		public virtual bool canDecorate(Element element)
		{
			return true;
		}

		/// <summary>
		/// Default implementation of {@code decorate} for {@link goog.ui.Control}s.
		/// Initializes the control's ID, content, and state based on the ID of the
		/// element, its child nodes, and its CSS classes, respectively.  Returns the
		/// element.
		/// </summary>
		/// <param name="control">Control instance to decorate the element.</param>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Decorated element.</returns>
		public virtual HTMLElement decorate(goog.ui.Control control, HTMLElement element)
		{
			// Set the control's ID to the decorated element's DOM ID, if any.
			if (element.Id != null) {
				control.setId(element.Id);
			}

			// Set the control's content to the decorated element's content.
			var contentElem = this.getContentElement(element);
			if (contentElem != null && contentElem.FirstChild != null) {
				control.setContentInternal(
					contentElem.FirstChild.NextSibling != null ?
						new ControlContent(contentElem.ChildNodes) :
						new ControlContent(contentElem.FirstChild));
			}
			else {
				control.setContentInternal(null);
			}

			// Initialize the control's state based on the decorated element's CSS class.
			// This implementation is optimized to minimize object allocations, string
			// comparisons, and DOM access.
			var state = (Component.State)0x00;
			var rendererClassName = this.getCssClass();
			var structuralClassName = this.getStructuralCssClass();
			var hasRendererClassName = false;
			var hasStructuralClassName = false;
			var hasCombinedClassName = false;
			var classNames = goog.dom.classlist.get(element);
			classNames.ForEach((className) => {
				if (!hasRendererClassName && className == rendererClassName) {
					hasRendererClassName = true;
					if (structuralClassName == rendererClassName) {
						hasStructuralClassName = true;
					}
				}
				else if (!hasStructuralClassName && className == structuralClassName) {
					hasStructuralClassName = true;
				}
				else {
					state |= this.getStateFromClass(className);
				}
				if (this.getStateFromClass(className) == goog.ui.Component.State.DISABLED) {
					goog.asserts.assertElement(contentElem);
					if (goog.dom.isFocusableTabIndex(contentElem)) {
						goog.dom.setFocusableTabIndex(contentElem, false);
					}
				}
			});
			control.setStateInternal(state);

			// Make sure the element has the renderer's CSS classes applied, as well as
			// any extra class names set on the control.
			if (!hasRendererClassName) {
				classNames.Push(rendererClassName);
				if (structuralClassName == rendererClassName) {
					hasStructuralClassName = true;
				}
			}
			if (!hasStructuralClassName) {
				classNames.Push(structuralClassName);
			}
			var extraClassNames = control.getExtraClassNames();
			if (extraClassNames != null) {
				classNames.PushRange(extraClassNames);
			}

			// For IE6, rewrite all classes on the decorated element if any combined
			// classes apply.
			if (goog.userAgent.IE && !goog.userAgent.isVersionOrHigher("7")) {
				var combinedClasses = this.getAppliedCombinedClassNames_(classNames);
				if (combinedClasses.Length > 0) {
					classNames.PushRange(combinedClasses);
					hasCombinedClassName = true;
				}
			}

			// Only write to the DOM if new class names had to be added to the element.
			if (!hasRendererClassName || !hasStructuralClassName || extraClassNames != null ||
				hasCombinedClassName) {
				goog.dom.classlist.set(element, classNames.Join(" "));
			}

			return element;
		}

		/// <summary>
		/// Initializes the control's DOM by configuring properties that can only be set
		/// after the DOM has entered the document.  This implementation sets up BiDi
		/// and keyboard focus.  Called from {@link goog.ui.Control#enterDocument}.
		/// </summary>
		/// <param name="control">Control whose DOM is to be initialized</param>
		///     as it enters the document.
		public void initializeDom(goog.ui.Control control)
		{
			// Initialize render direction (BiDi).  We optimize the left-to-right render
			// direction by assuming that elements are left-to-right by default, and only
			// updating their styling if they are explicitly set to right-to-left.
			if (control.isRightToLeft()) {
				this.setRightToLeft(control.getElement(), true);
			}

			// Initialize keyboard focusability (tab index).  We assume that components
			// aren't focusable by default (i.e have no tab index), and only touch the
			// DOM if the component is focusable, enabled, and visible, and therefore
			// needs a tab index.
			if (control.isEnabled()) {
				this.setFocusable(control, control.isVisible());
			}
		}

		/// <summary>
		/// Sets the element's ARIA role.
		/// </summary>
		/// <param name="element">Element to update.</param>
		/// <param name="opt_preferredRole">The preferred ARIA role.</param>
		public void setAriaRole(HTMLElement element, goog.a11y.aria.Role opt_preferredRole = 0)
		{
			var ariaRole = opt_preferredRole == 0 ? 0 : this.getAriaRole();
			if (ariaRole != 0) {
				goog.asserts.assert(
					element != null, "The element passed as a first parameter cannot be null.");
				var currentRole = goog.a11y.aria.getRole(element);
				if (ariaRole == currentRole) {
					return;
				}
				goog.a11y.aria.setRole(element, ariaRole);
			}
		}

		/// <summary>
		/// Sets the element's ARIA attributes, including distinguishing between
		/// universally supported ARIA properties and ARIA states that are only
		/// supported by certain ARIA roles. Only attributes which are initialized to be
		/// true will be set.
		/// </summary>
		/// <param name="control">Control whose ARIA state will be updated.</param>
		/// <param name="element">Element whose ARIA state is to be updated.</param>
		public void setAriaStates(Control control, HTMLElement element)
		{
			goog.asserts.assert(control != null);
			goog.asserts.assert(element != null);

			var ariaLabel = control.getAriaLabel();
			if (ariaLabel != null) {
				this.setAriaLabel(element, ariaLabel);
			}

			if (!control.isVisible()) {
				goog.a11y.aria.setState(
					element, goog.a11y.aria.State.HIDDEN, !control.isVisible());
			}
			if (!control.isEnabled()) {
				this.updateAriaState(
					element, goog.ui.Component.State.DISABLED, !control.isEnabled());
			}
			if (control.isSupportedState(goog.ui.Component.State.SELECTED)) {
				this.updateAriaState(
					element, goog.ui.Component.State.SELECTED, control.isSelected());
			}
			if (control.isSupportedState(goog.ui.Component.State.CHECKED)) {
				this.updateAriaState(
					element, goog.ui.Component.State.CHECKED, control.isChecked());
			}
			if (control.isSupportedState(goog.ui.Component.State.OPENED)) {
				this.updateAriaState(
					element, goog.ui.Component.State.OPENED, control.isOpen());
			}
		}

		/// <summary>
		/// Sets the element's ARIA label. This should be overriden by subclasses that
		/// don't apply the role directly on control.element_.
		/// </summary>
		/// <param name="element">Element whose ARIA label is to be updated.</param>
		/// <param name="ariaLabel">Label to add to the element.</param>
		public void setAriaLabel(HTMLElement element, string ariaLabel)
		{
			goog.a11y.aria.setLabel(element, ariaLabel);
		}

		/// <summary>
		/// Allows or disallows text selection within the control's DOM.
		/// </summary>
		/// <param name="element">The control's root element.</param>
		/// <param name="allow">Whether the element should allow text selection.</param>
		public void setAllowTextSelection(HTMLElement element, bool allow)
		{
			// On all browsers other than IE and Opera, it isn't necessary to recursively
			// apply unselectable styling to the element's children.
			goog.style.setUnselectable(
				element, !allow, !goog.userAgent.IE && !goog.userAgent.OPERA);
		}

		/// <summary>
		/// Applies special styling to/from the control's element if it is rendered
		/// right-to-left, and removes it if it is rendered left-to-right.
		/// </summary>
		/// <param name="element">The control's root element.</param>
		/// <param name="rightToLeft">Whether the component is rendered
		///     right-to-left.</param>
		public void setRightToLeft(HTMLElement element, bool rightToLeft)
		{
			this.enableClassName(
				element, le.getCssName(this.getStructuralCssClass(), "rtl"),
				rightToLeft);
		}

		/// <summary>
		/// Returns true if the control's key event target supports keyboard focus
		/// (based on its {@code tabIndex} attribute), false otherwise.
		/// </summary>
		/// <param name="control">Control whose key event target is to be
		/// checked.</param>
		/// <returns>Whether the control's key event target is focusable.</returns>
		public bool isFocusable(Control control)
		{
			HTMLElement keyTarget;
			if (control.isSupportedState(goog.ui.Component.State.FOCUSED) &&
				(keyTarget = control.getKeyEventTarget()) != null) {
				return goog.dom.isFocusableTabIndex(keyTarget);
			}
			return false;
		}

		/// <summary>
		/// Updates the control's key event target to make it focusable or non-focusable
		/// via its {@code tabIndex} attribute.  Does nothing if the control doesn't
		/// support the {@code FOCUSED} state, or if it has no key event target.
		/// </summary>
		/// <param name="control">Control whose key event target is to be
		/// updated.</param>
		/// <param name="focusable">Whether to enable keyboard focus support on the
		/// control's key event target.</param>
		public void setFocusable(Control control, bool focusable)
		{
			HTMLElement keyTarget;
			if (control.isSupportedState(goog.ui.Component.State.FOCUSED) &&
				(keyTarget = control.getKeyEventTarget()) != null) {
				if (!focusable && control.isFocused()) {
					// Blur before hiding.  Note that IE calls onblur handlers asynchronously.
					try {
						keyTarget.Blur();
					}
					catch (Exception) {
						// TODO(user|user):  Find out why this fails on IE.
					}
					// The blur event dispatched by the key event target element when blur()
					// was called on it should have been handled by the control's handleBlur()
					// method, so at this point the control should no longer be focused.
					// However, blur events are unreliable on IE and FF3, so if at this point
					// the control is still focused, we trigger its handleBlur() method
					// programmatically.
					if (control.isFocused()) {
						control.handleBlur(null);
					}
				}
				// Don't overwrite existing tab index values unless needed.
				if (goog.dom.isFocusableTabIndex(keyTarget) != focusable) {
					goog.dom.setFocusableTabIndex(keyTarget, focusable);
				}
			}
		}

		/// <summary>
		/// Shows or hides the element.
		/// </summary>
		/// <param name="element">Element to update.</param>
		/// <param name="visible">Whether to show the element.</param>
		public void setVisible(HTMLElement element, bool visible)
		{
			// The base class implementation is trivial; subclasses should override as
			// needed.  It should be possible to do animated reveals, for example.
			goog.style.setElementShown(element, visible);
			if (element != null) {
				goog.a11y.aria.setState(element, goog.a11y.aria.State.HIDDEN, !visible);
			}
		}

		/// <summary>
		/// Updates the appearance of the control in response to a state change.
		/// </summary>
		/// <param name="control">Control instance to update.</param>
		/// <param name="state">State to enable or disable.</param>
		/// <param name="enable">Whether the control is entering or exiting the state.</param>
		public void setState(Control control, Component.State state, bool enable)
		{
			var element = control.getElement();
			if (element != null) {
				var className = this.getClassForState(state);
				if (className != null) {
					this.enableClassName(control, className, enable);
				}
				this.updateAriaState(element, state, enable);
			}
		}

		/// <summary>
		/// Updates the element's ARIA (accessibility) attributes , including
		/// distinguishing between universally supported ARIA properties and ARIA states
		/// that are only supported by certain ARIA roles.
		/// </summary>
		/// <param name="element">Element whose ARIA state is to be updated.</param>
		/// <param name="state">Component state being enabled or disabled.</param>
		/// <param name="enable">Whether the state is being enabled or disabled.</param>
		protected void updateAriaState(HTMLElement element, Component.State state, bool enable)
		{
			// Ensure the ARIA attribute map exists.
			if (goog.ui.ControlRenderer.ariaAttributeMap_ == null) {
				goog.ui.ControlRenderer.ariaAttributeMap_ = new Dictionary<Component.State, a11y.aria.State> {
					{ goog.ui.Component.State.DISABLED, goog.a11y.aria.State.DISABLED },
					{ goog.ui.Component.State.SELECTED, goog.a11y.aria.State.SELECTED },
					{ goog.ui.Component.State.CHECKED, goog.a11y.aria.State.CHECKED },
					{ goog.ui.Component.State.OPENED, goog.a11y.aria.State.EXPANDED} };
			}
			goog.asserts.assert(
				element != null, "The element passed as a first parameter cannot be null.");
			var ariaAttr = goog.ui.ControlRenderer.getAriaStateForAriaRole_(
				element, goog.ui.ControlRenderer.ariaAttributeMap_.TryGetValue(state, out var ret) ? ret : 0);
			if (ariaAttr != 0) {
				goog.a11y.aria.setState(element, ariaAttr, enable);
			}
		}

		/// <summary>
		/// Returns the appropriate ARIA attribute based on ARIA role if the ARIA
		/// attribute is an ARIA state.
		/// </summary>
		/// <param name="element">The element from which to get the ARIA role for
		/// matching ARIA state.</param>
		/// <param name="attr">The ARIA attribute to check to see if it
		/// can be applied to the given ARIA role.</param>
		/// <returns>An ARIA attribute that can be applied to the
		/// given ARIA role.</returns>
		private static goog.a11y.aria.State getAriaStateForAriaRole_(HTMLElement element, goog.a11y.aria.State attr)
		{
			var role = goog.a11y.aria.getRole(element);
			if (role == 0) {
				return attr;
			}
			//role = /** @type {goog.a11y.aria.Role} */ (role);
			var matchAttr = goog.ui.ControlRenderer.TOGGLE_ARIA_STATE_MAP_[role];
			matchAttr = matchAttr != 0 ? matchAttr : attr;
			return goog.ui.ControlRenderer.isAriaState_(attr) ? matchAttr : attr;
		}

		/// <summary>
		/// Determines if the given ARIA attribute is an ARIA property or ARIA state.
		/// </summary>
		/// <param name="attr">The ARIA attribute to classify.</param>
		/// <returns>If the ARIA attribute is an ARIA state.</returns>
		private static bool isAriaState_(goog.a11y.aria.State attr)
		{
			return attr == goog.a11y.aria.State.CHECKED ||
				attr == goog.a11y.aria.State.SELECTED;
		}

		/// <summary>
		/// </summary>
		/// Takes a control's root element, and sets its content to the given text
		/// caption or DOM structure.  The default implementation replaces the children
		/// of the given element.  Renderers that create more complex DOM structures
		/// must override this method accordingly.
		/// <param name="element">The control's root element.</param>
		/// <param name="content">Text caption or DOM structure to be</param>
		///     set as the control's content. The DOM nodes will not be cloned, they
		///     will only moved under the content element of the control.
		public virtual void setContent(HTMLElement element, goog.ui.ControlContent content)
		{
			var contentElem = this.getContentElement(element);
			if (contentElem != null) {
				goog.dom.removeChildren(contentElem);
				if (content != null) {
					if (content.IsString()) {
						goog.dom.setTextContent(contentElem, content.AsString());
					}
					else {
						var childHandler = new Action<Union<string, Node>>((child) => {
							if (child != null) {
								var doc = goog.dom.getOwnerDocument(contentElem);
								contentElem.AppendChild(
									child.Is<string>() ? doc.CreateTextNode(child.As<string>()) : child.As<Node>());
							}
						});
						if (content.IsArray()) {
							// Array of nodes.
							foreach (var node in content.AsArray()) {
								childHandler(node);
							}
						}
						else if (content.Is<NodeList>()) {
							// NodeList. The second condition filters out TextNode which also has
							// length attribute but is not array like. The nodes have to be cloned
							// because childHandler removes them from the list during iteration.
							foreach (var node in content.As<NodeList>()) {
								childHandler(node);
							}
						}
						else {
							// Node or string.
							childHandler(content.AsNodeOrString());
						}
					}
				}
			}
		}

		/// <summary>
		/// By default, the ARIA role is unspecified.
		/// </summary>
		private goog.a11y.aria.Role ariaRole_;

		/// <summary>
		/// Returns the element within the component's DOM that should receive keyboard
		/// focus (null if none).  The default implementation returns the control's root
		/// element.
		/// </summary>
		/// <param name="control">Control whose key event target is to be
		/// returned.</param>
		/// <returns>The key event target.</returns>
		public HTMLElement getKeyEventTarget(Control control)
		{
			return control.getElement();
		}

		/// <summary>
		/// Returns the CSS class name to be applied to the root element of all
		/// components rendered or decorated using this renderer.  The class name
		/// is expected to uniquely identify the renderer class, i.e. no two
		/// renderer classes are expected to share the same CSS class name.
		/// </summary>
		/// <returns>Renderer-specific CSS class name.</returns>
		public virtual string getCssClass()
		{
			return cssClassName_;
		}

		/// <summary>
		/// Returns an array of combinations of classes to apply combined class names for
		/// in IE6 and below. See {@link IE6_CLASS_COMBINATIONS} for more detail. This
		/// method doesn't reference {@link IE6_CLASS_COMBINATIONS} so that it can be
		/// compiled out, but subclasses should return their IE6_CLASS_COMBINATIONS
		/// static constant instead.
		/// </summary>
		/// <returns>Array of class name combinations.</returns>
		public JsArray<JsArray<string>> getIe6ClassCombinations()
		{
			return new JsArray<JsArray<string>>();
		}

		/// <summary>
		/// Returns the name of a DOM structure-specific CSS class to be applied to the
		/// root element of all components rendered or decorated using this renderer.
		/// Unlike the class name returned by {@link #getCssClass}, the structural class
		/// name may be shared among different renderers that generate similar DOM
		/// structures.  The structural class name also serves as the basis of derived
		/// class names used to identify and style structural elements of the control's
		/// DOM, as well as the basis for state-specific class names.  The default
		/// implementation returns the same class name as {@link #getCssClass}, but
		/// subclasses are expected to override this method as needed.
		/// </summary>
		/// <returns>DOM structure-specific CSS class name (same as the renderer-
		/// specific CSS class name by default).</returns>
		public string getStructuralCssClass()
		{
			return this.getCssClass();
		}

		/// <summary>
		/// Returns all CSS class names applicable to the given control, based on its
		/// state.  The return value is an array of strings containing
		/// <ol>
		///   <li>the renderer-specific CSS class returned by {@link #getCssClass},
		///       followed by
		///   <li>the structural CSS class returned by {@link getStructuralCssClass} (if
		///       different from the renderer-specific CSS class), followed by
		///   <li>any state-specific classes returned by {@link #getClassNamesForState},
		///       followed by
		///   <li>any extra classes returned by the control's {@code getExtraClassNames}
		///       method and
		///   <li>for IE6 and lower, additional combined classes from
		///       {@link getAppliedCombinedClassNames_}.
		/// </ol>
		/// Since all controls have at least one renderer-specific CSS class name, this
		/// method is guaranteed to return an array of at least one element.
		/// </summary>
		/// <param name="control">Control whose CSS classes are to be
		/// returned.</param>
		/// <returns>Array of CSS class names applicable to the control.</returns>
		protected JsArray<string> getClassNames(Control control)
		{
			var cssClass = this.getCssClass();

			// Start with the renderer-specific class name.
			var classNames = new JsArray<string> { cssClass };

			// Add structural class name, if different.
			var structuralCssClass = this.getStructuralCssClass();
			if (structuralCssClass != cssClass) {
				classNames.Push(structuralCssClass);
			}

			// Add state-specific class names, if any.
			var classNamesForState = this.getClassNamesForState(control.getState());
			classNames.PushRange(classNamesForState);

			// Add extra class names, if any.
			var extraClassNames = control.getExtraClassNames();
			if (extraClassNames != null) {
				classNames.PushRange(extraClassNames);
			}

			// Add composite classes for IE6 support
			if (goog.userAgent.IE && !goog.userAgent.isVersionOrHigher("7")) {
				classNames.PushRange(
					this.getAppliedCombinedClassNames_(classNames));
			}

			return classNames;
		}

		/// <summary>
		/// Returns an array of all the combined class names that should be applied based
		/// on the given list of classes. Checks the result of
		/// {@link getIe6ClassCombinations} for any combinations that have all
		/// members contained in classes. If a combination matches, the members are
		/// joined with an underscore (in order), and added to the return array.
		/// 
		/// If opt_includedClass is provided, return only the combined classes that have
		/// all members contained in classes AND include opt_includedClass as well.
		/// opt_includedClass is added to classes as well.
		/// </summary>
		/// <param name="classes">Array-like thing of classes to
		/// return matching combined classes for.</param>
		/// <param name="opt_includedClass">If provided, get only the combined
		/// classes that include this one.</param>
		/// <returns>Array of combined class names that should be
		/// applied.</returns>
		private JsArray<string> getAppliedCombinedClassNames_(JsArray<string> classes, string opt_includedClass = null)
		{
			var toAdd = new JsArray<string>();
			if (opt_includedClass != null) {
				classes.Push(opt_includedClass);
			}
			foreach (var combo in this.getIe6ClassCombinations()) {
				if (combo.All((a) => classes.Contains(a)) &&
					(opt_includedClass == null || combo.Contains(opt_includedClass))) {
					toAdd.Push(combo.Join("_"));
				}
			}
			return toAdd;
		}

		/// <summary>
		/// Takes a bit mask of {@link goog.ui.Component.State}s, and returns an array
		/// of the appropriate class names representing the given state, suitable to be
		/// applied to the root element of a component rendered using this renderer, or
		/// null if no state-specific classes need to be applied.  This default
		/// implementation uses the renderer's {@link getClassForState} method to
		/// generate each state-specific class.
		/// </summary>
		/// <param name="state">Bit mask of component states.</param>
		/// <returns>Array of CSS class names representing the given
		/// state.</returns>
		protected JsArray<string> getClassNamesForState(Component.State state)
		{
			var classNames = new JsArray<string>();
			while (state != 0) {
				// For each enabled state, push the corresponding CSS class name onto
				// the classNames array.
				var mask = (int)state & -(int)state;  // Least significant bit
				classNames.Push(
					this.getClassForState((Component.State)mask));
				state = (Component.State)((int)state & ~(int)mask);
			}
			return classNames;
		}

		/// <summary>
		/// Map of component states to state-specific structural class names,
		/// used when changing the DOM in response to a state change.  Precomputed
		/// and cached on first use to minimize object allocations and string
		/// concatenation.
		/// </summary>
		private Dictionary<Component.State, string> classByState_;

		/// <summary>
		/// Takes a single {@link goog.ui.Component.State}, and returns the
		/// corresponding CSS class name (null if none).
		/// </summary>
		/// <param name="state">Component state.</param>
		/// <returns>CSS class representing the given state (undefined
		/// if none).</returns>
		public virtual string getClassForState(Component.State state)
		{
			if (this.classByState_ == null) {
				this.createClassByStateMap_();
			}
			return this.classByState_[state];
		}

		/// <summary>
		/// Takes a single CSS class name which may represent a component state, and
		/// returns the corresponding component state (0x00 if none).
		/// </summary>
		/// <param name="className">CSS class name, possibly representing a component</param>
		///     state.
		/// <returns>state Component state corresponding
		///      to the given CSS class (0x00 if none).</returns>
		protected virtual goog.ui.Component.State getStateFromClass(string className)
		{
			if (this.stateByClass_ == null) {
				this.createStateByClassMap_();
			}
			var state = Script.ParseFloat(this.stateByClass_[className].ToString());
			return (goog.ui.Component.State)(Double.IsNaN(state) ? 0x00 : state);
		}

		/// <summary>
		/// Takes a single {@link goog.ui.Component.State}, and returns the
		/// corresponding CSS class name (null if none).
		/// </summary>
		/// <param name="state">Component state.</param>
		/// <returns>CSS class representing the given state (undefined
		///      if none).</returns>
		private void createClassByStateMap_()
		{
			var baseClass = this.getStructuralCssClass();

			// This ensures space-separated css classnames are not allowed, which some
			// ControlRenderers had been doing.  See http://b/13694665.
			var isValidClassName =
				!baseClass.Replace(new Regex(@"\xa0|\s", RegexOptions.Multiline), " ").Contains(" ");
			goog.asserts.assert(
				isValidClassName,
				"ControlRenderer has an invalid css class: \'" + baseClass + "\'");

			this.classByState_ = new Dictionary<Component.State, string>{
				{ goog.ui.Component.State.DISABLED, le.getCssName(baseClass, "disabled") },
				{ goog.ui.Component.State.HOVER, le.getCssName(baseClass, "hover") },
				{ goog.ui.Component.State.ACTIVE, le.getCssName(baseClass, "active") },
				{ goog.ui.Component.State.SELECTED, le.getCssName(baseClass, "selected") },
				{ goog.ui.Component.State.CHECKED, le.getCssName(baseClass, "checked") },
				{ goog.ui.Component.State.FOCUSED, le.getCssName(baseClass, "focused") },
				{ goog.ui.Component.State.OPENED, le.getCssName(baseClass, "open")} };
		}

		/// <summary>
		/// Creates the lookup table of classes to states, used during decoration.
		/// </summary>
		private void createStateByClassMap_()
		{
			// We need the classByState_ map so we can transpose it.
			if (this.classByState_ == null) {
				this.createClassByStateMap_();
			}

			foreach (var kvp in classByState_)
				stateByClass_[kvp.Value] = kvp.Key;
		}

		/// <summary>
		/// Map of state-specific structural class names to component states,
		/// used during element decoration.  Precomputed and cached on first use
		/// to minimize object allocations and string concatenation.
		/// </summary>
		private Dictionary<string, Component.State> stateByClass_;
	}
}
