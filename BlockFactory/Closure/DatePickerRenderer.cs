// Copyright 2013 The Closure Library Authors. All Rights Reserved.
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
* @fileoverview The renderer interface for {@link goog.ui.DatePicker}.
*
* @see ../demos/datepicker.html
*/
using Bridge.Html5;

namespace goog.ui
{
	public abstract class DatePickerRenderer
	{
		/// <summary>
		/// The renderer for {@link goog.ui.DatePicker}. Renders the date picker's
		/// navigation header and footer.
		/// </summary>
		public DatePickerRenderer()
		{

		}

		/// <summary>
		/// Render the navigation row.
		/// </summary>
		/// <param name="row">The parent element to render the component into.</param>
		/// <param name="simpleNavigation">Whether the picker should render a simple
		/// navigation menu that only contains controls for navigating to the next
		/// and previous month. The default navigation menu contains controls for
		/// navigating to the next/previous month, next/previous year, and menus for
		/// jumping to specific months and years.</param>
		/// <param name="showWeekNum">Whether week numbers should be shown.</param>
		/// <param name="fullDateFormat">The full date format.
		/// {@see goog.i18n.DateTimeSymbols}.</param>
		public abstract void renderNavigationRow(HTMLElement row, bool simpleNavigation,
			bool showWeekNum, string fullDateFormat);

		/// <summary>
		/// Render the footer row.
		/// </summary>
		/// <param name="row">The parent element to render the component into.</param>
		/// <param name="simpleNavigation">Whether week numbers should be shown.</param>
		public abstract void renderFooterRow(Element row, bool simpleNavigation);
	}
}
