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
 * @fileoverview Utilities for adding, removing and setting ARIA roles and
 * states as defined by W3C ARIA standard: http://www.w3.org/TR/wai-aria/
 * All modern browsers have some form of ARIA support, so no browser checks are
 * performed when adding ARIA to components.
 *
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog
{
	public class a11y
	{
		public class aria
		{
			private static string ARIA_PREFIX_ = "aria-";
			private static readonly string ROLE_ATTRIBUTE_ = "role";

			public enum State
			{
				EXPANDED = 1,
				ATOMIC,
				AUTOCOMPLETE,
				DROPEFFECT,
				HASPOPUP,
				LIVE,
				MULTILINE,
				MULTISELECTABLE,
				ORIENTATION,
				READONLY,
				RELEVANT,
				REQUIRED,
				SORT,
				BUSY,
				DISABLED,
				HIDDEN,
				INVALID,
				SELECTED,
				CHECKED,
				ACTIVEDESCENDANT,
				LEVEL,
				LABELLEDBY,
				PRESSED,
				SETSIZE,
				POSINSET,
				LABEL,
				_END_
			}

			internal static bool containsValue(goog.a11y.aria.State ariaName)
			{
				return ((int)ariaName > 0) && ((int)ariaName < (int)goog.a11y.aria.State._END_);
			}

			public enum Role
			{
				TREEITEM = 1,
				BUTTON,
				CHECKBOX,
				MENU_ITEM,
				MENU_ITEM_CHECKBOX,
				MENU_ITEM_RADIO,
				RADIO,
				TAB,
				PRESENTATION,
				GROUP,
				TREE,
				GRID,
				ROWHEADER,
				COLUMNHEADER,
				GRIDCELL,
				ROW,
				_END_,
			}

			internal static void setState(HTMLElement element, goog.a11y.aria.State stateName, object value)
			{
				if (value is JsArray<string>) {
					value = ((JsArray<string>)value).Join(" ");
				}
				var attrStateName = goog.a11y.aria.getAriaAttributeName_(stateName);
				if ((value as string) == "" || value == Script.Undefined) {
					var defaultValueMap = goog.a11y.aria.datatables.getDefaultValuesMap();
					// Work around for browsers that don't properly support ARIA.
					// According to the ARIA W3C standard, user agents should allow
					// setting empty value which results in setting the default value
					// for the ARIA state if such exists. The exact text from the ARIA W3C
					// standard (http://www.w3.org/TR/wai-aria/states_and_properties):
					// "When a value is indicated as the default, the user agent
					// MUST follow the behavior prescribed by this value when the state or
					// property is empty or undefined."
					// The defaultValueMap contains the default values for the ARIA states
					// and has as a key the goog.a11y.aria.State constant for the state.
					if (defaultValueMap.ContainsKey(stateName)) {
						element.SetAttribute(attrStateName, (string)defaultValueMap[stateName]);
					}
					else {
						element.RemoveAttribute(attrStateName);
					}
				}
				else {
					element.SetAttribute(attrStateName, value.ToString());
				}
			}

			private static string getAriaAttributeName_(goog.a11y.aria.State ariaName)
			{
				if (goog.asserts.ENABLE_ASSERTS) {
					goog.asserts.assert((int)ariaName == 0, "ARIA attribute cannot be empty.");
					goog.asserts.assert(
						goog.a11y.aria.containsValue(ariaName),
						"No such ARIA attribute " + ariaName);
				}
				return goog.a11y.aria.ARIA_PREFIX_ + ariaName.ToString().ToLower();
			}

			public static void setRole(HTMLElement element, Role roleName)
			{
				if (roleName == 0) {
					// Setting the ARIA role to empty string is not allowed
					// by the ARIA standard.
					goog.a11y.aria.removeRole(element);
				}
				else {
					if (goog.asserts.ENABLE_ASSERTS) {
						goog.asserts.assert(
							(roleName > 0) && (roleName < goog.a11y.aria.Role._END_),
							"No such ARIA role " + roleName);
					}
					element.SetAttribute(goog.a11y.aria.ROLE_ATTRIBUTE_, roleName.ToString().ToLower());
				}
			}

			public static void removeRole(HTMLElement element)
			{
				element.RemoveAttribute(goog.a11y.aria.ROLE_ATTRIBUTE_);
			}

			public class datatables
			{
				private static Dictionary<goog.a11y.aria.State, object> DefaultStateValueMap_;

				internal static Dictionary<goog.a11y.aria.State, object> getDefaultValuesMap()
				{
					if (DefaultStateValueMap_ == null) {
						DefaultStateValueMap_ = new Dictionary<goog.a11y.aria.State, object>() {
							{ goog.a11y.aria.State.ATOMIC, false },
							{ goog.a11y.aria.State.AUTOCOMPLETE,"none" },
							{ goog.a11y.aria.State.DROPEFFECT, "none" },
							{ goog.a11y.aria.State.HASPOPUP, false },
							{ goog.a11y.aria.State.LIVE, "off" },
							{ goog.a11y.aria.State.MULTILINE, false },
							{ goog.a11y.aria.State.MULTISELECTABLE, false },
							{ goog.a11y.aria.State.ORIENTATION, "vertical" },
							{ goog.a11y.aria.State.READONLY, false },
							{ goog.a11y.aria.State.RELEVANT, "additions text" },
							{ goog.a11y.aria.State.REQUIRED, false },
							{ goog.a11y.aria.State.SORT, "none" },
							{ goog.a11y.aria.State.BUSY, false },
							{ goog.a11y.aria.State.DISABLED, false },
							{ goog.a11y.aria.State.HIDDEN, false },
							{ goog.a11y.aria.State.INVALID, "false" }
						};
					}

					return DefaultStateValueMap_;
				}
			}

			/// <summary>
			/// Sets the label of the given element.
			/// </summary>
			/// <param name="element">DOM node to set label to.</param>
			/// <param name="label">The label to set.</param>
			internal static void setLabel(HTMLElement element, string label)
			{
				goog.a11y.aria.setState(element, goog.a11y.aria.State.LABEL, label);
			}

			public static goog.a11y.aria.Role getRole(HTMLElement element)
			{
				var role = element.GetAttribute(goog.a11y.aria.ROLE_ATTRIBUTE_);
				switch (role) {
				case "treeitem": return goog.a11y.aria.Role.TREEITEM;
				case "button": return goog.a11y.aria.Role.BUTTON;
				case "checkbox": return goog.a11y.aria.Role.CHECKBOX;
				case "menu_item": return goog.a11y.aria.Role.MENU_ITEM;
				case "menu_item_checkbox": return goog.a11y.aria.Role.MENU_ITEM_CHECKBOX;
				case "menu_item_radio": return goog.a11y.aria.Role.MENU_ITEM_RADIO;
				case "radio": return goog.a11y.aria.Role.RADIO;
				case "tab": return goog.a11y.aria.Role.TAB;
				case "presentation": return goog.a11y.aria.Role.PRESENTATION;
				case "group": return goog.a11y.aria.Role.GROUP;
				case "tree": return goog.a11y.aria.Role.TREE;
				default: return 0;
				}
			}

			/// <summary>
			/// Remove the state or property for the element.
			/// </summary>
			/// <param name="element">DOM node where we set state.</param>
			/// <param name="stateName">State name.</param>
			public static void removeState(HTMLElement element, State stateName)
			{
				element.RemoveAttribute(goog.a11y.aria.getAriaAttributeName_(stateName));
			}

			/// <summary>
			/// Sets the activedescendant ARIA property value for an element.
			/// If the activeElement is not null, it should have an id set.
			/// </summary>
			/// <param name="element">DOM node to set activedescendant ARIA property to.</param>
			/// <param name="activeElement">DOM node being set as activedescendant.</param>
			public static void setActiveDescendant(HTMLElement element, HTMLElement activeElement)
			{
				var id = "";
				if (activeElement != null) {
					id = activeElement.Id;
					goog.asserts.assert(id != null, "The active element should have an id.");
				}

				goog.a11y.aria.setState(element, goog.a11y.aria.State.ACTIVEDESCENDANT, id);
			}

			public static string getState(HTMLElement element, State stateName)
			{
				// TODO(user): return properly typed value result --
				// boolean, number, string, null. We should be able to chain
				// getState(...) and setState(...) methods.

				var attr =
					/** @type {string|number|boolean} */ (
						element.GetAttribute(
							goog.a11y.aria.getAriaAttributeName_(stateName)));
				var isNullOrUndefined = attr == null/* || attr == undefined*/;
				return isNullOrUndefined ? "" : attr;
			}

			public static string getLabel(HTMLElement element)
			{
				return goog.a11y.aria.getState(element, goog.a11y.aria.State.LABEL);
			}
		}
	}
}
