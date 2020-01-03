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
 * @fileoverview Base class for container renderers.
 *
 * @author attila@google.com (Attila Bodis)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog.ui
{
	public class ContainerRenderer
	{
		private static ContainerRenderer instance_;
		private string cssClassName_ = goog.ui.MenuRenderer.CSS_CLASS;

		internal static ContainerRenderer getInstance()
		{
			if (instance_ == null)
				instance_ = new ContainerRenderer();
			return instance_;
		}

		/// <summary>
		/// Constructs a new renderer and sets the CSS class that the renderer will use
		/// as the base CSS class to apply to all elements rendered by that renderer.
		/// An example to use this function using a menu is:
		///
		/// <pre>
		/// var myCustomRenderer = goog.ui.ContainerRenderer.getCustomRenderer(
		///     goog.ui.MenuRenderer, "my-special-menu");
		/// var newMenu = new goog.ui.Menu(opt_domHelper, myCustomRenderer);
		/// </pre>
		///
		/// Your styles for the menu can now be:
		/// <pre>
		/// .my-special-menu { }
		/// </pre>
		///
		/// <em>instead</em> of
		/// <pre>
		/// .CSS_MY_SPECIAL_MENU .goog-menu { }
		/// </pre>
		///
		/// You would want to use this functionality when you want an instance of a
		/// component to have specific styles different than the other components of the
		/// same type in your application.  This avoids using descendant selectors to
		/// apply the specific styles to this component.
		/// </summary>
		/// <param name="ctor">The constructor of the renderer you want to create.</param>
		/// <param name="cssClassName">The name of the CSS class for this renderer.</param>
		/// <returns>An instance of the desired renderer with
		///     its getCssClass() method overridden to return the supplied custom CSS
		///     class name.</returns>
		public static ContainerRenderer getCustomRenderer(Func<ContainerRenderer> ctor, string cssClassName)
		{
			var renderer = ctor();

			/**
			 * Returns the CSS class to be applied to the root element of components
			 * rendered using this renderer.
			 * @return {string} Renderer-specific CSS class.
			 */
			//renderer.getCssClass = function() { return cssClassName; };
			renderer.cssClassName_ = cssClassName;

			return renderer;
		}

		/// <summary>
		/// Default CSS class to be applied to the root element of containers rendered
		/// by this renderer.
		/// </summary>
		public static readonly string CSS_CLASS = le.getCssName("goog-container");

		private goog.a11y.aria.Role ariaRole_;

		/// <summary>
		/// Returns the ARIA role to be applied to the container.
		/// See http://wiki/Main/ARIA for more info.
		/// </summary>
		/// <returns>ARIA role.</returns>
		public goog.a11y.aria.Role getAriaRole()
		{
			return this.ariaRole_;
		}

		/// <summary>
		/// Enables or disables the tab index of the element.  Only elements with a
		/// valid tab index can receive focus.
		/// </summary>
		/// <param name="element">Element whose tab index is to be changed.</param>
		/// <param name="enable">Whether to add or remove the element's tab index.</param>
		public void enableTabIndex(HTMLElement element, bool enable)
		{
			if (element != null) {
				element.TabIndex = enable ? (short)0 : (short)-1;
			}
		}

		/// <summary>
		/// Creates and returns the container's root element.  The default
		/// simply creates a DIV and applies the renderer's own CSS class name to it.
		/// To be overridden in subclasses.
		/// </summary>
		/// <param name="container">Container to render.</param>
		/// <returns>Root element for the container.</returns>
		public HTMLElement createDom(goog.ui.Container container)
		{
			return container.getDomHelper().createDom(
				goog.dom.TagName.DIV, this.getClassNames(container).Join(" "));
		}

		/// <summary>
		/// Returns the DOM element into which child components are to be rendered,
		/// or null if the container hasn't been rendered yet.
		/// </summary>
		/// <param name="element">Root element of the container whose content element</param>
		///     is to be returned.
		/// <returns>Element to contain child elements (null if none).</returns>
		public HTMLElement getContentElement(HTMLElement element)
		{
			return element;
		}

		/// <summary>
		/// Default implementation of {@code canDecorate}; returns true if the element
		/// is a DIV, false otherwise.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Whether the renderer can decorate the element.</returns>
		public bool canDecorate(Element element)
		{
			return element.TagName == "DIV";
		}

		/// <summary>
		/// Default implementation of {@code decorate} for {@link goog.ui.Container}s.
		/// Decorates the element with the container, and attempts to decorate its child
		/// elements.  Returns the decorated element.
		/// </summary>
		/// <param name="container">Container to decorate the element.</param>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Decorated element.</returns>
		public virtual HTMLElement decorate(goog.ui.Container container, HTMLElement element)
		{
			// Set the container's ID to the decorated element's DOM ID, if any.
			if (element.Id != null) {
				container.setId(element.Id);
			}

			// Configure the container's state based on the CSS class names it has.
			var baseClass = this.getCssClass();
			var hasBaseClass = false;
			var classNames = goog.dom.classlist.get(element);
			if (classNames != null) {
				classNames.ForEach((className) => {
					if (className == baseClass) {
						hasBaseClass = true;
					}
					else if (className != null) {
						this.setStateFromClassName(container, className, baseClass);
					}
				});
			}

			if (!hasBaseClass) {
				// Make sure the container's root element has the renderer's own CSS class.
				goog.dom.classlist.add(element, baseClass);
			}

			// Decorate the element's children, if applicable.  This should happen after
			// the container's own state has been initialized, since how children are
			// decorated may depend on the state of the container.
			this.decorateChildren(container, this.getContentElement(element));

			return element;
		}

		/// <summary>
		/// Sets the container's state based on the given CSS class name, encountered
		/// during decoration.  CSS class names that don't represent container states
		/// are ignored.  Considered protected; subclasses should override this method
		/// to support more states and CSS class names.
		/// </summary>
		/// <param name="container">Container to update.</param>
		/// <param name="className">CSS class name.</param>
		/// <param name="baseClass">Base class name used as the root of state-specific
		///     class names (typically the renderer's own class name).</param>
		protected void setStateFromClassName(
			goog.ui.Container container, string className, string baseClass)
		{
			if (className == le.getCssName(baseClass, "disabled")) {
				container.setEnabled(false);
			}
			else if (className == le.getCssName(baseClass, "horizontal")) {
				container.setOrientation(goog.ui.Container.Orientation.HORIZONTAL);
			}
			else if (className == le.getCssName(baseClass, "vertical")) {
				container.setOrientation(goog.ui.Container.Orientation.VERTICAL);
			}
		}

		/// <summary>
		/// Takes a container and an element that may contain child elements, decorates
		/// the child elements, and adds the corresponding components to the container
		/// as child components.  Any non-element child nodes (e.g. empty text nodes
		/// introduced by line breaks in the HTML source) are removed from the element.
		/// </summary>
		/// <param name="container">Container whose children are to be</param>
		///     discovered.
		/// <param name="element">Element whose children are to be decorated.</param>
		/// <param name="opt_firstChild">the first child to be decorated.</param>
		public void decorateChildren(
			goog.ui.Container container, HTMLElement element, HTMLElement opt_firstChild = null)
		{
			if (element != null) {
				var node = opt_firstChild ?? element.FirstChild;
				Node next;
				// Tag soup HTML may result in a DOM where siblings have different parents.
				while (node != null && node.ParentNode == element) {
					// Get the next sibling here, since the node may be replaced or removed.
					next = node.NextSibling;
					if (node.NodeType == NodeType.Element) {
						// Decorate element node.
						var child = this.getDecoratorForChild((HTMLElement)node);
						if (child != null) {
							// addChild() may need to look at the element.
							child.setElementInternal((HTMLElement)node);
							// If the container is disabled, mark the child disabled too.  See
							// bug 1263729.  Note that this must precede the call to addChild().
							if (!container.isEnabled()) {
								child.setEnabled(false);
							}
							container.addChild(child);
							child.decorate((HTMLElement)node);
						}
					}
					else if (node.NodeValue == null || node.NodeValue.ToString().Trim() == "") {
						// Remove empty text node, otherwise madness ensues (e.g. controls that
						// use goog-inline-block will flicker and shift on hover on Gecko).
						element.RemoveChild(node);
					}
					node = next;
				}
			}
		}

		/// <summary>
		/// Inspects the element, and creates an instance of {@link goog.ui.Control} or
		/// an appropriate subclass best suited to decorate it.  Returns the control (or
		/// null if no suitable class was found).  This default implementation uses the
		/// element's CSS class to find the appropriate control class to instantiate.
		/// May be overridden in subclasses.
		/// </summary>
		/// <param name="element">Element to decorate.</param>
		/// <returns>A new control suitable to decorate the element
		///      (null if none).</returns>
		public goog.ui.Control getDecoratorForChild(HTMLElement element)
		{
			return (goog.ui.Control)(
				goog.ui.registry.getDecorator(element));
		}

		/// <summary>
		/// Initializes the container's DOM when the container enters the document.
		/// Called from {@link goog.ui.Container#enterDocument}.
		/// </summary>
		/// <param name="container">Container whose DOM is to be initialized</param>
		///     as it enters the document.
		public void initializeDom(goog.ui.Container container)
		{
			var elem = container.getElement();
			goog.asserts.assert(elem != null, "The container DOM element cannot be null.");
			// Make sure the container's element isn't selectable.  On Gecko, recursively
			// marking each child element unselectable is expensive and unnecessary, so
			// only mark the root element unselectable.
			goog.style.setUnselectable(elem, true, goog.userAgent.GECKO);

			// IE doesn't support outline:none, so we have to use the hideFocus property.
			if (goog.userAgent.IE) {
				elem.HideFocus = true;
			}

			// Set the ARIA role.
			var ariaRole = this.getAriaRole();
			if (ariaRole != 0) {
				goog.a11y.aria.setRole(elem, ariaRole);
			}
		}

		/// <summary>
		/// </summary>
		/// Returns the element within the container's DOM that should receive keyboard
		/// focus (null if none).  The default implementation returns the container"s
		/// root element.
		/// <param name="container">Container whose key event target is</param>
		///     to be returned.
		/// <returns>Key event target (null if none).</returns>
		public HTMLElement getKeyEventTarget(goog.ui.Container container)
		{
			return container.getElement();
		}

		/// <summary>
		/// Returns the CSS class to be applied to the root element of containers
		/// rendered using this renderer.
		/// </summary>
		/// <returns></returns>
		public virtual string getCssClass()
		{
			return cssClassName_;
		}

		/// <summary>
		/// Returns all CSS class names applicable to the given container, based on its
		/// state.  The array of class names returned includes the renderer's own CSS
		/// class, followed by a CSS class indicating the container's orientation,
		/// followed by any state-specific CSS classes.
		/// </summary>
		/// <param name="container">Container whose CSS classes are to be</param>
		///     returned.
		/// <returns>Array of CSS class names applicable to the
		///      container.</returns>
		public JsArray<string> getClassNames(goog.ui.Container container)
		{
			var baseClass = this.getCssClass();
			var isHorizontal =
				container.getOrientation() == goog.ui.Container.Orientation.HORIZONTAL;
			var classNames = new JsArray<string> {
			  baseClass, (isHorizontal ? le.getCssName(baseClass, "horizontal") :
										 le.getCssName(baseClass, "vertical"))
			};
			if (!container.isEnabled()) {
				classNames.Push(le.getCssName(baseClass, "disabled"));
			}
			return classNames;
		}

		/// <summary>
		/// Returns the default orientation of containers rendered or decorated by this
		/// renderer.  The base class implementation returns {@code VERTICAL}.
		/// </summary>
		/// <returns>Default orientation for containers
		/// created or decorated by this renderer.</returns>
		public Container.Orientation getDefaultOrientation()
		{
			return goog.ui.Container.Orientation.VERTICAL;
		}
	}

	public class registry
	{
		public static Component getDecoratorByClassName(string className)
		{
			if (goog.ui.registry.decoratorFunctions_.ContainsKey(className)) {
				var type = goog.ui.registry.decoratorFunctions_[className];
				var ctor = type.GetConstructor(new Type[0]);
				return (Component)ctor.Invoke(new object[0]);
			}
			else
				return null;
		}

		public static Component getDecorator(HTMLElement element)
		{
			Component decorator;
			goog.asserts.assert(element != null);
			var classNames = goog.dom.classlist.get(element);
			foreach (var i in classNames) {
				if ((decorator = goog.ui.registry.getDecoratorByClassName(i)) != null) {
					return decorator;
				}
			}
			return null;
		}

		public static void setDecoratorByClassName(string className, Type decoratorFn)
		{
			// In this case, explicit validation has negligible overhead (since each
			// decorator  is only registered once), and helps catch subtle bugs.
			if (className == null) {
				throw new Exception("Invalid class name " + className);
			}
			if (decoratorFn == null) {
				throw new Exception("Invalid decorator function " + decoratorFn);
			}

			goog.ui.registry.decoratorFunctions_[className] = decoratorFn;
		}

		private static Dictionary<string, Type> decoratorFunctions_ = new Dictionary<string, Type>();
	}
}
