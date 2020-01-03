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
 * @fileoverview Renderer for {@link goog.ui.MenuItem}s.
 *
 * @author attila@google.com (Attila Bodis)
 */
using System;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class MenuItemRenderer : ControlRenderer
	{
		private static MenuItemRenderer instance_;

		internal static new MenuItemRenderer getInstance()
		{
			if (instance_ == null)
				instance_ = new MenuItemRenderer();
			return instance_;
		}

		/// <summary>
		/// Default renderer for {@link goog.ui.MenuItem}s.  Each item has the following
		/// structure:
		///
		///    <div class="goog-menuitem">
		///      <div class="goog-menuitem-content">
		///        ...(menu item contents)...
		///      </div>
		///    </div>
		///
		/// </summary>
		public MenuItemRenderer()
		{

		}
		/// <summary>
		/// Commonly used CSS class names, cached here for convenience (and to avoid
		/// unnecessary string concatenation).
		/// </summary>
		private string[] classNameCache_ = new string[3];

		/// <summary>
		/// CSS class name the renderer applies to menu item elements.
		/// </summary>
		public static readonly new string CSS_CLASS = le.getCssName("goog-menuitem");

		/// <summary>
		/// Constants for referencing composite CSS classes.
		/// </summary>
		private enum CompositeCssClassIndex_
		{
			HOVER,
			CHECKBOX,
			CONTENT
		}

		/// <summary>
		/// Returns the composite CSS class by using the cached value or by constructing
		/// the value from the base CSS class and the passed index.
		/// </summary>
		/// <param name="index">Index for the CSS class - could be highlight,
		/// checkbox or content in usual cases.</param>
		/// <returns>The composite CSS class.</returns>
		private string getCompositeCssClass_(CompositeCssClassIndex_ index)
		{
			var result = this.classNameCache_[(int)index];
			if (result == null) {
				switch (index) {
				case goog.ui.MenuItemRenderer.CompositeCssClassIndex_.HOVER:
					result = le.getCssName(this.getStructuralCssClass(), "highlight");
					break;
				case goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CHECKBOX:
					result = le.getCssName(this.getStructuralCssClass(), "checkbox");
					break;
				case goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CONTENT:
					result = le.getCssName(this.getStructuralCssClass(), "content");
					break;
				}
				this.classNameCache_[(int)index] = result;
			}

			return result;
		}

		/// <summary>
		/// </summary>
		public override goog.a11y.aria.Role getAriaRole()
		{
			return goog.a11y.aria.Role.MENU_ITEM;
		}

		/// <summary>
		/// </summary>
		/// Overrides {@link goog.ui.ControlRenderer#createDom} by adding extra markup
		/// and stying to the menu item's element if it is selectable or checkable.
		/// <param name="item">Menu item to render.</param>
		/// <returns>Root element for the item.</returns>
		public override HTMLElement createDom(goog.ui.Control item)
		{
			var element = item.getDomHelper().createDom(
				goog.dom.TagName.DIV, this.getClassNames(item).Join(" "),
				this.createContent(item.getContent(), item.getDomHelper()));
			this.setEnableCheckBoxStructure(
				(MenuItem)item, element, item.isSupportedState(goog.ui.Component.State.SELECTED) ||
					item.isSupportedState(goog.ui.Component.State.CHECKED));
			return element;
		}

		/// <summary>
		/// </summary>
		public override HTMLElement getContentElement(HTMLElement element)
		{
			return element != null ? (HTMLElement)element.FirstChild : null;
		}

		/// <summary>
		/// </summary>
		/// Overrides {@link goog.ui.ControlRenderer#decorate} by initializing the
		/// menu item to checkable based on whether the element to be decorated has
		/// extra stying indicating that it should be.
		/// <param name="item">Menu item instance to decorate the element.</param>
		/// <param name="element">Element to decorate.</param>
		/// <returns>Decorated element.</returns>
		public override HTMLElement decorate(goog.ui.Control item, HTMLElement element)
		{
			goog.asserts.assert(element != null);
			if (!this.hasContentStructure(element)) {
				element.AppendChild(
					this.createContent(new ControlContent(element.ChildNodes), item.getDomHelper()));
			}
			if (goog.dom.classlist.contains(element, le.getCssName("goog-option"))) {
				((goog.ui.MenuItem)item).setCheckable(true);
				this.setCheckable((goog.ui.MenuItem)item, element, true);
			}
			return base.decorate(item, element);
		}

		/// <summary>
		/// </summary>
		/// Takes a menu item's root element, and sets its content to the given text
		/// caption or DOM structure.  Overrides the superclass immplementation by
		/// making sure that the checkbox structure (for selectable/checkable menu
		/// items) is preserved.
		/// <param name="element">The item's root element.</param>
		/// <param name="content">Text caption or DOM structure to be</param>
		///     set as the item's content.
		public override void setContent(HTMLElement element, ControlContent content)
		{
			// Save the checkbox element, if present.
			var contentElement = this.getContentElement(element);
			var checkBoxElement =
				this.hasCheckBoxStructure(element) ? contentElement.FirstChild : null;
			base.setContent(element, content);
			if (checkBoxElement != null && !this.hasCheckBoxStructure(element)) {
				// The call to setContent() blew away the checkbox element; reattach it.
				contentElement.InsertBefore(
					checkBoxElement, contentElement.FirstChild ?? null);
			}
		}

		/// <summary>
		/// </summary>
		/// Returns true if the element appears to have a proper menu item structure by
		/// checking whether its first child has the appropriate structural class name.
		/// <param name="element">Element to check.</param>
		/// <returns>Whether the element appears to have a proper menu item DOM.</returns>
		protected bool hasContentStructure(HTMLElement element)
		{
			var child = goog.dom.getFirstElementChild(element);
			var contentClassName = this.getCompositeCssClass_(
				goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CONTENT);
			return child != null && goog.dom.classlist.contains((HTMLElement)child, contentClassName);
		}

		/// <summary>
		/// Wraps the given text caption or existing DOM node(s) in a structural element
		/// containing the menu item's contents.
		/// </summary>
		/// <param name="content">Menu item contents.</param>
		/// <param name="dom">DOM helper for document interaction.</param>
		/// <returns>Menu item content element.</returns>
		private HTMLElement createContent(ControlContent content, dom.DomHelper dom)
		{
			var contentClassName = this.getCompositeCssClass_(
				goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CONTENT);
			return dom.createDom(goog.dom.TagName.DIV, contentClassName, content);
		}

		/// <summary>
		/// Enables/disables radio button semantics on the menu item.
		/// </summary>
		/// <param name="item">Menu item to update.</param>
		/// <param name="element">Menu item element to update (may be null if the
		/// item hasn't been rendered yet).</param>
		/// <param name="selectable">Whether the item should be selectable.</param>
		public void setSelectable(MenuItem item, HTMLElement element, bool selectable)
		{
			if (item != null && element != null) {
				this.setEnableCheckBoxStructure(item, element, selectable);
			}
		}

		/// <summary>
		/// Enables/disables checkbox semantics on the menu item.
		/// </summary>
		/// <param name="item">Menu item to update.</param>
		/// <param name="element">Menu item element to update (may be null if the
		/// item hasn't been rendered yet).</param>
		/// <param name="checkable">Whether the item should be checkable.</param>
		public void setCheckable(MenuItem item, HTMLElement element, bool checkable)
		{
			if (item != null && element != null) {
				this.setEnableCheckBoxStructure(item, element, checkable);
			}
		}

		/// <summary>
		/// Determines whether the item contains a checkbox element.
		/// </summary>
		/// <param name="element">Menu item root element.</param>
		/// <returns>Whether the element contains a checkbox element.</returns>
		public bool hasCheckBoxStructure(HTMLElement element)
		{
			var contentElement = this.getContentElement(element);
			if (contentElement != null) {
				var child = contentElement.FirstChild;
				var checkboxClassName = this.getCompositeCssClass_(
					goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CHECKBOX);
				return child != null && /*goog.dom.isElement(child)*/ child.NodeType == NodeType.Element &&
					goog.dom.classlist.contains((HTMLElement)child, checkboxClassName);
			}
			return false;
		}

		/// <summary>
		/// Adds or removes extra markup and CSS styling to the menu item to make it
		/// selectable or non-selectable, depending on the value of the
		/// {@code selectable} argument.
		/// </summary>
		/// <param name="item">Menu item to update.</param>
		/// <param name="element">Menu item element to update.</param>
		/// <param name="enable">Whether to add or remove the checkbox structure.</param>
		protected void setEnableCheckBoxStructure(MenuItem item, HTMLElement element, bool enable)
		{
			this.setAriaRole(element, item.getPreferredAriaRole());
			this.setAriaStates(item, element);
			if (enable != this.hasCheckBoxStructure(element)) {
				goog.dom.classlist.enable(element, le.getCssName("goog-option"), enable);
				var contentElement = this.getContentElement(element);
				if (enable) {
					// Insert checkbox structure.
					var checkboxClassName = this.getCompositeCssClass_(
						goog.ui.MenuItemRenderer.CompositeCssClassIndex_.CHECKBOX);
					contentElement.InsertBefore(
						item.getDomHelper().createDom(
							goog.dom.TagName.DIV, checkboxClassName),
						contentElement.FirstChild ?? null);
				}
				else {
					// Remove checkbox structure.
					contentElement.RemoveChild(contentElement.FirstChild);
				}
			}
		}


		/// <summary>
		/// </summary>
		/// Takes a single {@link goog.ui.Component.State}, and returns the
		/// corresponding CSS class name (null if none).  Overrides the superclass
		/// implementation by using "highlight" as opposed to "hover" as the CSS
		/// class name suffix for the HOVER state, for backwards compatibility.
		/// <param name="state">Component state.</param>
		/// <returns>CSS class representing the given state
		///      (undefined if none).</returns>
		public override string getClassForState(goog.ui.Component.State state)
		{
			switch (state) {
			case goog.ui.Component.State.HOVER:
				// We use "highlight" as the suffix, for backwards compatibility.
				return this.getCompositeCssClass_(
					goog.ui.MenuItemRenderer.CompositeCssClassIndex_.HOVER);
			case goog.ui.Component.State.CHECKED:
			case goog.ui.Component.State.SELECTED:
				// We use "goog-option-selected" as the class, for backwards
				// compatibility.
				return le.getCssName("goog-option-selected");
			default:
				return base.getClassForState(state);
			}
		}

		/// <summary>
		/// </summary>
		/// Takes a single CSS class name which may represent a component state, and
		/// returns the corresponding component state (0x00 if none).  Overrides the
		/// superclass implementation by treating "goog-option-selected" as special,
		/// for backwards compatibility.
		/// <param name="className">CSS class name, possibly representing a component</param>
		///     state.
		/// <returns>state Component state corresponding
		///      to the given CSS class (0x00 if none).</returns>
		protected override goog.ui.Component.State getStateFromClass(string className)
		{
			var hoverClassName = this.getCompositeCssClass_(
				goog.ui.MenuItemRenderer.CompositeCssClassIndex_.HOVER);
			if (className == le.getCssName("goog-option-selected"))
				return goog.ui.Component.State.CHECKED;
			else if (className == hoverClassName)
				return goog.ui.Component.State.HOVER;
			else
				return base.getStateFromClass(className);
		}

		/// <summary>
		/// </summary>
		public override string getCssClass()
		{
			return goog.ui.MenuItemRenderer.CSS_CLASS;
		}
	}
}
