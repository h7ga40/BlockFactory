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

using System;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	internal class DefaultDatePickerRenderer : DatePickerRenderer
	{
		/// <summary>Name of base CSS class of datepicker</summary>
		private string baseCssClass_;
		private dom.DomHelper dom_;

		public DefaultDatePickerRenderer(string baseCssClass, dom.DomHelper opt_domHelper)
		{
			this.baseCssClass_ = baseCssClass;
			this.dom_ = opt_domHelper ?? goog.dom.getDomHelper();
		}

		/// <summary>
		/// Returns the dom helper that is being used on this component.
		/// </summary>
		/// <returns>The dom helper used on this component.</returns>
		public dom.DomHelper getDomHelper()
		{
			return this.dom_;
		}

		/// <summary>
		/// Returns base CSS class. This getter is used to get base CSS class part.
		/// All CSS class names in component are created as:
		/// _.getCssName(this.getBaseCssClass(), 'CLASS_NAME')
		/// </summary>
		/// <returns>Base CSS class.</returns>
		public string getBaseCssClass()
		{
			return this.baseCssClass_;
		}

		/// <summary>
		/// Render the navigation row (navigating months and maybe years).
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
		public override void renderNavigationRow(HTMLElement row, bool simpleNavigation,
			bool showWeekNum, string fullDateFormat)
		{
			// Populate the navigation row according to the configured navigation mode.
			HTMLTableDataCellElement cell, monthCell, yearCell;

			if (simpleNavigation) {
				cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
				cell.ColSpan = showWeekNum ? 1 : 2;
				this.createButton_(
					cell, "\u00AB",
					le.getCssName(this.getBaseCssClass(), "previousMonth"));  // <<
				row.AppendChild(cell);

				cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
				cell.ColSpan = showWeekNum ? 6 : 5;
				cell.ClassName = le.getCssName(this.getBaseCssClass(), "monthyear");
				row.AppendChild(cell);

				cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
				this.createButton_(
					cell, "\u00BB",
					le.getCssName(this.getBaseCssClass(), "nextMonth"));  // >>
				row.AppendChild(cell);

			}
			else {
				monthCell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
				monthCell.ColSpan = 5;
				this.createButton_(
					monthCell, "\u00AB",
					le.getCssName(this.getBaseCssClass(), "previousMonth"));  // <<
				this.createButton_(
					monthCell, "", le.getCssName(this.getBaseCssClass(), "month"));
				this.createButton_(
					monthCell, "\u00BB",
					le.getCssName(this.getBaseCssClass(), "nextMonth"));  // >>

				yearCell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
				yearCell.ColSpan = 3;
				this.createButton_(
					yearCell, "\u00AB",
					le.getCssName(this.getBaseCssClass(), "previousYear"));  // <<
				this.createButton_(
					yearCell, "", le.getCssName(this.getBaseCssClass(), "year"));
				this.createButton_(
					yearCell, "\u00BB",
					le.getCssName(this.getBaseCssClass(), "nextYear"));  // <<

				// If the date format has year ("y") appearing first before month ("m"),
				// show the year on the left hand side of the datepicker popup.  Otherwise,
				// show the month on the left side.  This check assumes the data to be
				// valid, and that all date formats contain month and year.
				if (fullDateFormat.IndexOf("y") < fullDateFormat.IndexOf("m")) {
					row.AppendChild(yearCell);
					row.AppendChild(monthCell);
				}
				else {
					row.AppendChild(monthCell);
					row.AppendChild(yearCell);
				}
			}
		}

		/// <summary>
		/// Render the footer row (with select buttons).
		/// </summary>
		/// <param name="row">The parent element to render the component into.</param>
		/// <param name="showWeekNum">Whether week numbers should be shown.</param>
		public override void renderFooterRow(Element row, bool showWeekNum)
		{
			// Populate the footer row with buttons for Today and None.
			var cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
			cell.ColSpan = showWeekNum ? 2 : 3;
			cell.ClassName = le.getCssName(this.getBaseCssClass(), "today-cont");

			/** @desc Label for button that selects the current date. */
			var MSG_DATEPICKER_TODAY_BUTTON_LABEL = le.getMsg("Today");
			this.createButton_(
				cell, MSG_DATEPICKER_TODAY_BUTTON_LABEL,
				le.getCssName(this.getBaseCssClass(), "today-btn"));
			row.AppendChild(cell);

			cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
			cell.ColSpan = showWeekNum ? 4 : 3;
			row.AppendChild(cell);

			cell = (HTMLTableDataCellElement)this.getDomHelper().createElement(goog.dom.TagName.TD);
			cell.ColSpan = 2;
			cell.ClassName = le.getCssName(this.getBaseCssClass(), "none-cont");

			/** @desc Label for button that clears the selection. */
			var MSG_DATEPICKER_NONE = le.getMsg("None");
			this.createButton_(
				cell, MSG_DATEPICKER_NONE,
				le.getCssName(this.getBaseCssClass(), "none-btn"));
			row.AppendChild(cell);
		}

		/// <summary>
		/// Support function for button creation.
		/// </summary>
		/// <param name="parentNode">Container the button should be added to.</param>
		/// <param name="label">Button label.</param>
		/// <param name="opt_className">Class name for button, which will be used
		/// in addition to "goog-date-picker-btn".</param>
		private HTMLElement createButton_(HTMLTableDataCellElement parentNode, string label,
			string opt_className = null)
		{
			var classes = new JsArray<string>() { le.getCssName(this.getBaseCssClass(), "btn") };
			if (opt_className != null) {
				classes.Push(opt_className);
			}
			var el = this.getDomHelper().createElement(goog.dom.TagName.BUTTON);
			el.ClassName = classes.Join(" ");
			el.AppendChild(this.getDomHelper().createTextNode(label));
			parentNode.AppendChild(el);
			return el;
		}
	}
}
