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
 * @fileoverview Date picker implementation.
 *
 * @author eae@google.com (Emil A Eklund)
 * @see ../demos/datepicker.html
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class DatePicker : Component
	{
		/// <summary>
		/// Date and time symbols to use.
		/// </summary>
		goog.i18n.DateTimeSymbolsType symbols_;
		private string[] wdayNames_;
		private i18n.DateTimeFormat i18nDateFormatterDay_;
		private i18n.DateTimeFormat i18nDateFormatterDay2_;
		private i18n.DateTimeFormat i18nDateFormatterWeek_;
		private i18n.DateTimeFormat i18nDateFormatterDayAriaLabel_;
		private i18n.DateTimeFormat i18nDateFormatterYear_;
		private i18n.DateTimeFormat i18nDateFormatterMonthYear_;
		private DatePickerRenderer renderer_;
		/// <summary>Selected date.</summary>
		private date.Date date_;
		/// <summary>Active month.</summary>
		private date.Date activeMonth_;
		/// <summary>Class names to apply to the weekday columns.</summary>
		private string[] wdayStyles_;
		/// <summary>Object that is being used to cache key handlers.</summary>
		private Dictionary<int, events.KeyHandler> keyHandlers_;
		/// <summary>Collection of dates that make up the date picker.</summary>
		private JsArray<JsArray<date.Date>> grid_;
		private JsArray<JsArray<HTMLElement>> elTable_;
		/// <summary>
		/// TODO(tbreisacher): Remove external references to this field,
		/// and make it private.
		/// </summary>
		private HTMLElement tableBody_;
		private HTMLElement tableFoot_;
		private HTMLElement elYear_;
		private HTMLElement elMonth_;
		private HTMLElement elToday_;
		private HTMLElement elNone_;
		private HTMLElement menu_;
		private HTMLElement menuSelected_;
		private Action<HTMLElement> menuCallback_;

		public DatePicker(Union<goog.date.Date, Date> opt_date = null, Object opt_dateTimeSymbols = null,
			goog.dom.DomHelper opt_domHelper = null, goog.ui.DatePickerRenderer opt_renderer = null)
			: base(opt_domHelper)
		{
			this.symbols_ = (goog.i18n.DateTimeSymbolsType)(
				opt_dateTimeSymbols ?? goog.i18n.DateTimeSymbols);

			this.wdayNames_ = this.symbols_.STANDALONESHORTWEEKDAYS;

			// Formatters for the various areas of the picker
			this.i18nDateFormatterDay_ = new goog.i18n.DateTimeFormat("d", this.symbols_);
			this.i18nDateFormatterDay2_ =
				new goog.i18n.DateTimeFormat("dd", this.symbols_);
			this.i18nDateFormatterWeek_ =
				new goog.i18n.DateTimeFormat("w", this.symbols_);
			// Formatter for day grid aria label.
			this.i18nDateFormatterDayAriaLabel_ =
				new goog.i18n.DateTimeFormat("M d", this.symbols_);

			// Previous implementation did not use goog.i18n.DateTimePatterns,
			// so it is likely most developers did not set it.
			// This is why the fallback to a hard-coded string (just in case).
			var patYear = goog.i18n.DateTimePatterns.YEAR_FULL ?? "y";
			this.i18nDateFormatterYear_ =
				new goog.i18n.DateTimeFormat(patYear, this.symbols_);
			var patMMMMy = goog.i18n.DateTimePatterns.YEAR_MONTH_FULL ?? "MMMM y";
			this.i18nDateFormatterMonthYear_ =
				new goog.i18n.DateTimeFormat(patMMMMy, this.symbols_);

			this.renderer_ = opt_renderer ??
				new goog.ui.DefaultDatePickerRenderer(
					this.getBaseCssClass(), this.getDomHelper());

			this.date_ = new goog.date.Date(opt_date);
			this.date_.setFirstWeekCutOffDay(this.symbols_.FIRSTWEEKCUTOFFDAY);
			this.date_.setFirstDayOfWeek(this.symbols_.FIRSTDAYOFWEEK);

			this.activeMonth_ = this.date_.clone();
			this.activeMonth_.setDate(1);

			this.wdayStyles_ = new string[] { "", "", "", "", "", "", "" };
			this.wdayStyles_[this.symbols_.WEEKENDRANGE[0]] =
				le.getCssName(this.getBaseCssClass(), "wkend-start");
			this.wdayStyles_[this.symbols_.WEEKENDRANGE[1]] =
				le.getCssName(this.getBaseCssClass(), "wkend-end");

			this.keyHandlers_ = new Dictionary<int, events.KeyHandler>();

			this.grid_ = new JsArray<JsArray<goog.date.Date>>();

			this.elTable_ = new JsArray<JsArray<HTMLElement>>();
		}

		/// <summary>
		/// Flag indicating if the number of weeks shown should be fixed.
		/// </summary>
		private bool showFixedNumWeeks_ = true;

		/// <summary>
		/// Flag indicating if days from other months should be shown.
		/// </summary>
		private bool showOtherMonths_ = true;

		/// <summary>
		/// Range of dates which are selectable by the user.
		/// </summary>
		private goog.date.DateRange userSelectableDateRange_ =
			goog.date.DateRange.allTime();

		/// <summary>
		/// Flag indicating if extra week(s) always should be added at the end. If not
		/// set the extra week is added at the beginning if the number of days shown
		/// from the previous month is less then the number from the next month.
		/// </summary>
		private bool extraWeekAtEnd_ = true;

		/// <summary>
		/// Flag indicating if week numbers should be shown.
		/// </summary>
		private bool showWeekNum_ = true;

		/// <summary>
		/// Flag indicating if weekday names should be shown.
		/// </summary>
		private bool showWeekdays_ = true;

		/// <summary>
		/// Flag indicating if none is a valid selection. Also controls if the none
		/// button should be shown or not.
		/// </summary>
		private bool allowNone_ = true;

		/// <summary>
		/// Flag indicating if the today button should be shown.
		/// </summary>
		private bool showToday_ = true;

		/// <summary>
		/// Flag indicating if the picker should use a simple navigation menu that only
		/// contains controls for navigating to the next and previous month. The default
		/// navigation menu contains controls for navigating to the next/previous month,
		/// next/previous year, and menus for jumping to specific months and years.
		/// </summary>
		private bool simpleNavigation_ = false;

		/// <summary>
		/// Custom decorator function. Takes a goog.date.Date object, returns a String
		/// representing a CSS class or null if no special styling applies
		/// </summary>
		private Func<date.Date, string> decoratorFunction_ = null;

		/// <summary>
		/// Flag indicating if the dates should be printed as a two charater date.
		/// </summary>
		private bool longDateFormat_ = false;

		/// <summary>
		/// Element for navigation row on a datepicker.
		/// </summary>
		private HTMLElement elNavRow_ = null;

		/// <summary>
		/// Element for the month/year in the navigation row.
		/// </summary>
		private HTMLElement elMonthYear_ = null;

		/// <summary>
		/// Element for footer row on a datepicker.
		/// </summary>
		private HTMLElement elFootRow_ = null;

		/// <summary>
		/// Generator for unique table cell IDs.
		/// </summary>
		private static int cellIdGenerator_;

		/// <summary>
		/// Name of base CSS class of datepicker.
		/// </summary>
		private static string BASE_CSS_CLASS_ = le.getCssName("goog-date-picker");

		/// <summary>
		/// The numbers of years to show before and after the current one in the
		/// year pull-down menu. A total of YEAR_MENU_RANGE * 2 + 1 will be shown.
		/// Example: for range = 2 and year 2013 => [2011, 2012, 2013, 2014, 2015]
		/// </summary>
		private const int YEAR_MENU_RANGE_ = 5;

		/// <summary>
		/// Constants for event names
		/// </summary>
		public static class Events
		{
			public const string CHANGE = "change";
			public const string CHANGE_ACTIVE_MONTH = "changeActiveMonth";
			public const string SELECT = "select";
		}

		public bool isCreated()
		{
			return isInDocument();
		}

		/// <summary>
		/// </summary>
		/// <returns>The first day of week, 0 = Monday, 6 = Sunday.</returns>
		public int getFirstWeekday()
		{
			return this.activeMonth_.getFirstDayOfWeek();
		}

		/// <summary>
		/// Returns the class name associated with specified weekday.
		/// </summary>
		/// <param name="wday">The week day number to get the class name for.</param>
		/// <returns>The class name associated with specified weekday.</returns>
		public string getWeekdayClass(int wday)
		{
			return this.wdayStyles_[wday];
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether a fixed number of weeks should be showed. If not
		/// only weeks for the current month will be shown.<returns>
		public bool getShowFixedNumWeeks()
		{
			return this.showFixedNumWeeks_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether a days from the previous and/or next month should
		/// be shown.<returns>
		public bool getShowOtherMonths()
		{
			return this.showOtherMonths_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether a the extra week(s) added always should be at the
		/// end. Only applicable if a fixed number of weeks are shown.<returns>
		public bool getExtraWeekAtEnd()
		{
			return this.extraWeekAtEnd_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether week numbers should be shown.</returns>
		public bool getShowWeekNum()
		{
			return this.showWeekNum_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether weekday names should be shown.</returns>
		public bool getShowWeekdayNames()
		{
			return this.showWeekdays_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether none is a valid selection.</returns>
		public bool getAllowNone()
		{
			return this.allowNone_;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether the today button should be shown.</returns>
		public bool getShowToday()
		{
			return this.showToday_;
		}

		/// <summary>
		/// Returns base CSS class. This getter is used to get base CSS class part.
		/// All CSS class names in component are created as:
		///   _.getCssName(this.getBaseCssClass(), "CLASS_NAME")
		/// </summary>
		/// <returns>Base CSS class.</returns>
		public string getBaseCssClass()
		{
			return goog.ui.DatePicker.BASE_CSS_CLASS_;
		}

		/// <summary>
		/// Sets the first day of week
		/// </summary>
		/// <param name="wday">Week day, 0 = Monday, 6 = Sunday.</param>
		public void setFirstWeekday(int wday)
		{
			this.activeMonth_.setFirstDayOfWeek(wday);
			this.updateCalendarGrid_();
			this.redrawWeekdays_();
		}

		/// <summary>
		/// Sets class name associated with specified weekday.
		/// </summary>
		/// <param name="wday">Week day, 0 = Monday, 6 = Sunday.</param>
		/// <param name="className">Class name.</param>
		public void setWeekdayClass(int wday, string className)
		{
			this.wdayStyles_[wday] = className;
			this.redrawCalendarGrid_();
		}

		/// <summary>
		/// Sets whether a fixed number of weeks should be showed. If not only weeks
		/// for the current month will be showed.
		/// </summary>
		/// <param name="b">Whether a fixed number of weeks should be showed.</param>
		public void setShowFixedNumWeeks(bool b)
		{
			this.showFixedNumWeeks_ = b;
			this.updateCalendarGrid_();
		}

		/// <summary>
		/// Sets whether a days from the previous and/or next month should be shown.
		/// </summary>
		/// <param name="b">Whether a days from the previous and/or next month should</param>
		///     be shown.
		public void setShowOtherMonths(bool b)
		{
			this.showOtherMonths_ = b;
			this.redrawCalendarGrid_();
		}

		/// <summary>
		/// Sets the range of dates which may be selected by the user.
		/// </summary>
		/// <param name="dateRange">The range of selectable dates.</param>
		public void setUserSelectableDateRange(goog.date.DateRange dateRange)
		{
			this.userSelectableDateRange_ = dateRange;
		}

		/// <summary>
		/// Gets the range of dates which may be selected by the user.
		/// </summary>
		/// <returns>The range of selectable dates.</returns>
		public goog.date.DateRange getUserSelectableDateRange()
		{
			return this.userSelectableDateRange_;
		}

		/// <summary>
		/// Determine if a date may be selected by the user.
		/// </summary>
		/// <param name="date">The date to be tested.</param>
		/// <returns>Whether the user may select this date.</returns>
		private bool isUserSelectableDate_(goog.date.Date date)
		{
			return this.userSelectableDateRange_.contains(date);
		}

		/// <summary>
		/// Sets whether the picker should use a simple navigation menu that only
		/// contains controls for navigating to the next and previous month. The default
		/// navigation menu contains controls for navigating to the next/previous month,
		/// next/previous year, and menus for jumping to specific months and years.
		/// </summary>
		/// <param name="b">Whether to use a simple navigation menu.</param>
		public void setUseSimpleNavigationMenu(bool b)
		{
			this.simpleNavigation_ = b;
			this.updateNavigationRow_();
			this.updateCalendarGrid_();
		}

		/// <summary>
		/// Sets whether a the extra week(s) added always should be at the end. Only
		/// applicable if a fixed number of weeks are shown.
		/// </summary>
		/// <param name="b">Whether a the extra week(s) added always should be at the
		///     end.</param>
		public void setExtraWeekAtEnd(bool b)
		{
			this.extraWeekAtEnd_ = b;
			this.updateCalendarGrid_();
		}

		/// <summary>
		/// Sets whether week numbers should be shown.
		/// </summary>
		/// <param name="b">Whether week numbers should be shown.</param>
		public void setShowWeekNum(bool b)
		{
			this.showWeekNum_ = b;
			// The navigation and footer rows may rely on the number of visible columns,
			// so we update them when adding/removing the weeknum column.
			this.updateNavigationRow_();
			this.updateFooterRow_();
			this.updateCalendarGrid_();
		}

		/// <summary>
		/// Sets whether weekday names should be shown.
		/// </summary>
		/// <param name="b">Whether weekday names should be shown.</param>
		public void setShowWeekdayNames(bool b)
		{
			this.showWeekdays_ = b;
			this.redrawWeekdays_();
			this.redrawCalendarGrid_();
		}

		/// <summary>
		/// Sets whether the picker uses narrow weekday names ("M", "T", "W", ...).
		///
		/// The default behavior is to use short names ("Mon", "Tue", "Wed", ...).
		/// </summary>
		/// <param name="b">Whether to use narrow weekday names.</param>
		public void setUseNarrowWeekdayNames(bool b)
		{
			this.wdayNames_ = b ? this.symbols_.STANDALONENARROWWEEKDAYS :
								  this.symbols_.STANDALONESHORTWEEKDAYS;
			this.redrawWeekdays_();
		}

		/// <summary>
		/// Sets whether none is a valid selection.
		/// </summary>
		/// <param name="b">Whether none is a valid selection.</param>
		public void setAllowNone(bool b)
		{
			this.allowNone_ = b;
			if (this.elNone_ != null) {
				this.updateTodayAndNone_();
			}
		}

		/// <summary>
		/// Sets whether the today button should be shown.
		/// </summary>
		/// <param name="b">Whether the today button should be shown.</param>
		public void setShowToday(bool b)
		{
			this.showToday_ = b;
			if (this.elToday_ != null) {
				this.updateTodayAndNone_();
			}
		}

		/// <summary>
		/// Updates the display style of the None and Today buttons as well as hides the
		/// table foot if both are hidden.
		/// </summary>
		private void updateTodayAndNone_()
		{
			goog.style.setElementShown(this.elToday_, this.showToday_);
			goog.style.setElementShown(this.elNone_, this.allowNone_);
			goog.style.setElementShown(
				this.tableFoot_, this.showToday_ || this.allowNone_);
		}

		/// <summary>
		/// Sets the decorator function. The function should have the interface of
		///   {string} f({goog.date.Date});
		/// and return a String representing a CSS class to decorate the cell
		/// corresponding to the date specified.
		/// </summary>
		/// <param name="f">The decorator function.</param>
		public void setDecorator(Func<object, string> f)
		{
			this.decoratorFunction_ = f;
		}

		/// <summary>
		/// Sets whether the date will be printed in long format. In long format, dates
		/// such as "1" will be printed as "01".
		/// </summary>
		/// <param name="b">Whethere dates should be printed in long format.</param>
		public void setLongDateFormat(bool b)
		{
			this.longDateFormat_ = b;
			this.redrawCalendarGrid_();
		}

		/// <summary>
		/// Changes the active month to the previous one.
		/// </summary>
		public void previousMonth()
		{
			this.activeMonth_.add(new goog.date.Interval(goog.date.Interval.MONTHS, -1));
			this.updateCalendarGrid_();
			this.fireChangeActiveMonthEvent_();
		}

		/// <summary>
		/// Changes the active month to the next one.
		/// </summary>
		public void nextMonth()
		{
			this.activeMonth_.add(new goog.date.Interval(goog.date.Interval.MONTHS, 1));
			this.updateCalendarGrid_();
			this.fireChangeActiveMonthEvent_();
		}

		/// <summary>
		/// Changes the active year to the previous one.
		/// </summary>
		public void previousYear()
		{
			this.activeMonth_.add(new goog.date.Interval(goog.date.Interval.YEARS, -1));
			this.updateCalendarGrid_();
			this.fireChangeActiveMonthEvent_();
		}

		/// <summary>
		/// Changes the active year to the next one.
		/// </summary>
		public void nextYear()
		{
			this.activeMonth_.add(new goog.date.Interval(goog.date.Interval.YEARS, 1));
			this.updateCalendarGrid_();
			this.fireChangeActiveMonthEvent_();
		}

		/// <summary>
		/// Selects the current date.
		/// </summary>
		public void selectToday()
		{
			this.setDate(new goog.date.Date());
		}

		/// <summary>
		/// Clears the selection.
		/// </summary>
		public void selectNone()
		{
			if (this.allowNone_) {
				this.setDate(null);
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>The active month displayed.</returns>
		public goog.date.Date getActiveMonth()
		{
			return this.activeMonth_.clone();
		}

		/// <summary>
		/// </summary>
		/// <returns>The selected date or null if nothing is selected.</returns>
		public goog.date.Date getDate()
		{
			return this.date_ != null ? this.date_.clone() : null;
		}

		/// <summary>
		/// </summary>
		/// <param name="row">The row in the grid.</param>
		/// <param name="col">The column in the grid.</param>
		/// <returns>The date in the grid or null if there is none.</returns>
		public goog.date.Date getDateAt(int row, int col)
		{
			return this.grid_[row] != null ?
				this.grid_[row][col] != null ? this.grid_[row][col].clone() : null :
				null;
		}

		/// <summary>
		/// Returns a date element given a row and column. In elTable_, the elements that
		/// represent dates are 1 indexed because of other elements such as headers.
		/// This corrects for the offset and makes the API 0 indexed.
		/// </summary>
		/// <param name="row">The row in the element table.</param>
		/// <param name="col">The column in the element table.</param>
		/// <returns>The element in the grid or null if there is none.</returns>
		protected Element getDateElementAt(int row, int col)
		{
			if (row < 0 || col < 0) {
				return null;
			}
			var adjustedRow = row + 1;
			return this.elTable_[adjustedRow] != null ?
				this.elTable_[adjustedRow][col + 1] ?? null :
				null;
		}

		/// <summary>
		/// Sets the selected date. Will always fire the SELECT event.
		/// </summary>
		/// <param name="date">Date to select or null to select nothing.</param>
		public void setDate(Union<goog.date.Date, Date> date)
		{
			if (date.Is<Date>())
				date = new Union<date.Date, Date>(date.As<Date>());
			this.setDate_(date.As<goog.date.Date>(), true);
		}

		/// <summary>
		/// Sets the selected date, and optionally fires the SELECT event based on param.
		/// </summary>
		/// <param name="date">Date to select or null to select nothing.</param>
		/// <param name="fireSelection">Whether to fire the selection event.</param>
		private void setDate_(goog.date.Date date, bool fireSelection)
		{
			// Check if the month has been changed.
			var sameMonth = date == this.date_ ||
				date != null && this.date_ != null && date.getFullYear() == this.date_.getFullYear() &&
					date.getMonth() == this.date_.getMonth();

			// Check if the date has been changed.
			var sameDate =
				date == this.date_ || sameMonth && date.getDate() == this.date_.getDate();

			// Set current date to clone of supplied goog.date.Date or Date.
			this.date_ = date != null ? new goog.date.Date(date) : null;

			// Set current month
			if (date != null) {
				this.activeMonth_.set(this.date_);
				// Set years with two digits to their full year, not 19XX.
				this.activeMonth_.setFullYear(this.date_.getFullYear());
				this.activeMonth_.setDate(1);
			}

			// Update calendar grid even if the date has not changed as even if today is
			// selected another month can be displayed.
			this.updateCalendarGrid_();

			if (fireSelection) {
				// TODO(eae): Standardize selection and change events with other components.
				// Fire select event.
				var selectEvent = new goog.ui.DatePickerEvent(
					goog.ui.DatePicker.Events.SELECT, this, this.date_);
				this.dispatchEvent(selectEvent);
			}

			// Fire change event.
			if (!sameDate) {
				var changeEvent = new goog.ui.DatePickerEvent(
					goog.ui.DatePicker.Events.CHANGE, this, this.date_);
				this.dispatchEvent(changeEvent);
			}

			// Fire change active month event.
			if (!sameMonth) {
				this.fireChangeActiveMonthEvent_();
			}
		}

		/// <summary>
		/// Updates the navigation row (navigating months and maybe years) in the navRow_
		/// element of a created picker.
		/// </summary>
		private void updateNavigationRow_()
		{
			if (this.elNavRow_ == null) {
				return;
			}
			var row = this.elNavRow_;

			// Clear the navigation row.
			while (row.FirstChild != null) {
				row.RemoveChild(row.FirstChild);
			}

			var fullDateFormat =
				this.symbols_.DATEFORMATS[(int)goog.i18n.DateTimeFormat.Format.FULL_DATE]
					.ToLowerCase();
			this.renderer_.renderNavigationRow(
				row, this.simpleNavigation_, this.showWeekNum_, fullDateFormat);

			if (this.simpleNavigation_) {
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "previousMonth"),
					new Action(this.previousMonth));
				var previousMonthElement = (HTMLElement)goog.dom.getElementByClass(
					le.getCssName(this.getBaseCssClass(), "previousMonth"), row);
				if (previousMonthElement != null) {
					// Note: we"re hiding the next and previous month buttons from screen
					// readers because keyboard navigation doesn't currently work correctly
					// with them. If that is fixed, we can show the buttons again.
					goog.a11y.aria.setState(
						previousMonthElement, goog.a11y.aria.State.HIDDEN, true);
					previousMonthElement.TabIndex = -1;
				}

				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "nextMonth"),
					new Action(this.nextMonth));
				var nextMonthElement = (HTMLElement)goog.dom.getElementByClass(
					le.getCssName(this.getBaseCssClass(), "nextMonth"), row);
				if (nextMonthElement != null) {
					goog.a11y.aria.setState(
						nextMonthElement, goog.a11y.aria.State.HIDDEN, true);
					nextMonthElement.TabIndex = -1;
				}

				this.elMonthYear_ = (HTMLElement)goog.dom.getElementByClass(
					le.getCssName(this.getBaseCssClass(), "monthyear"), row);
			}
			else {
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "previousMonth"),
					new Action(this.previousMonth));
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "nextMonth"),
					new Action(this.nextMonth));
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "month"),
					new Action<events.Event>(this.showMonthMenu_));

				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "previousYear"),
					new Action(this.previousYear));
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "nextYear"),
					new Action(this.nextYear));
				this.addPreventDefaultClickHandler_(
					row, le.getCssName(this.getBaseCssClass(), "year"),
					new Action<events.Event>(this.showYearMenu_));

				this.elMonth_ = (HTMLElement)goog.dom.getElementByClass(
					le.getCssName(this.getBaseCssClass(), "month"), row);
				this.elYear_ = (HTMLElement)goog.dom.getDomHelper().getElementByClass(
					le.getCssName(this.getBaseCssClass(), "year"), row);
			}
		}

		/// <summary>
		/// Setup click handler with prevent default.
		/// </summary>
		/// <param name="parentElement">The parent element of the element. This is</param>
		///     needed because the element in question might not be in the dom yet.
		/// <param name="cssName">The CSS class name of the element to attach a click</param>
		///     handler.
		/// <param name="handlerFunction">The click handler function.</param>
		private void addPreventDefaultClickHandler_(
			Element parentElement, string cssName, Delegate handlerFunction)
		{
			var element = goog.dom.getElementByClass(cssName, parentElement);
			this.getHandler().listen(element, goog.events.EventType.CLICK, new Action<DatePickerEvent>((e) => {
				e.preventDefault();
				handlerFunction.DynamicInvoke(e);
			}));
		}

		/// <summary>
		/// Updates the footer row (with select buttons) in the footRow_ element of a
		/// created picker.
		/// </summary>
		private void updateFooterRow_()
		{
			if (this.elFootRow_ == null) {
				return;
			}

			var row = this.elFootRow_;

			// Clear the footer row.
			goog.dom.removeChildren(row);

			this.renderer_.renderFooterRow(row, this.showWeekNum_);

			this.addPreventDefaultClickHandler_(
				row, le.getCssName(this.getBaseCssClass(), "today-btn"),
				new Action(this.selectToday));
			this.addPreventDefaultClickHandler_(
				row, le.getCssName(this.getBaseCssClass(), "none-btn"),
				new Action(this.selectNone));

			this.elToday_ = (HTMLElement)goog.dom.getElementByClass(
				le.getCssName(this.getBaseCssClass(), "today-btn"), row);
			this.elNone_ = (HTMLElement)goog.dom.getElementByClass(
				le.getCssName(this.getBaseCssClass(), "none-btn"), row);

			this.updateTodayAndNone_();
		}

		protected override void decorateInternal(HTMLElement el)
		{
			base.decorateInternal(el);
			goog.asserts.assert(el != null);
			goog.dom.classlist.add(el, this.getBaseCssClass());

			var table = (HTMLTableElement)this.dom_.createElement(goog.dom.TagName.TABLE);
			var thead = this.dom_.createElement(goog.dom.TagName.THEAD);
			var tbody = this.dom_.createElement(goog.dom.TagName.TBODY);
			var tfoot = this.dom_.createElement(goog.dom.TagName.TFOOT);

			goog.a11y.aria.setRole(tbody, a11y.aria.Role.GRID);
			tbody.TabIndex = 0;

			// As per comment in colorpicker: table.tBodies and table.tFoot should not be
			// used because of a bug in Safari, hence using an instance variable
			this.tableBody_ = tbody;
			this.tableFoot_ = tfoot;

			var row = this.dom_.createElement(goog.dom.TagName.TR);
			row.ClassName = le.getCssName(this.getBaseCssClass(), "head");
			this.elNavRow_ = row;
			this.updateNavigationRow_();

			thead.AppendChild(row);

			HTMLElement cell;
			this.elTable_ = new JsArray<JsArray<HTMLElement>>();
			for (var i = 0; i < 7; i++) {
				row = this.dom_.createElement(goog.dom.TagName.TR);
				this.elTable_[i] = new JsArray<HTMLElement>();
				for (var j = 0; j < 8; j++) {
					cell = this.dom_.createElement(j == 0 || i == 0 ? "th" : "td");
					if ((j == 0 || i == 0) && j != i) {
						cell.ClassName = (j == 0) ?
							le.getCssName(this.getBaseCssClass(), "week") :
							le.getCssName(this.getBaseCssClass(), "wday");
						goog.a11y.aria.setRole(cell, j == 0 ? a11y.aria.Role.ROWHEADER : a11y.aria.Role.COLUMNHEADER);
					}
					row.AppendChild(cell);
					this.elTable_[i][j] = cell;
				}
				tbody.AppendChild(row);
			}

			row = this.dom_.createElement(goog.dom.TagName.TR);
			row.ClassName = le.getCssName(this.getBaseCssClass(), "foot");
			this.elFootRow_ = row;
			this.updateFooterRow_();
			tfoot.AppendChild(row);


			table.CellSpacing = "0";
			table.CellPadding = "0";
			table.AppendChild(thead);
			table.AppendChild(tbody);
			table.AppendChild(tfoot);
			el.AppendChild(table);

			this.redrawWeekdays_();
			this.updateCalendarGrid_();

			el.TabIndex = 0;
		}

		public override void createDom()
		{
			base.createDom();
			this.decorateInternal(this.getElement());
		}

		public override void enterDocument()
		{
			base.enterDocument();

			var eh = this.getHandler();
			eh.listen(
				this.tableBody_, goog.events.EventType.CLICK, new Action<events.Event>(this.handleGridClick_));
			eh.listen(
				this.getKeyHandlerForElement_(this.getElement()),
				goog.events.KeyHandler.EventType.KEY, new Action<events.BrowserEvent>(this.handleGridKeyPress_));
		}

		public override void exitDocument()
		{
			base.exitDocument();
			this.destroyMenu_(null);
			foreach (var uid in this.keyHandlers_.Keys) {
				this.keyHandlers_[uid].dispose();
			}
			this.keyHandlers_ = new Dictionary<int, events.KeyHandler>();
		}

		public void create(HTMLElement element)
		{
			decorate(element);
		}

		public override void disposeInternal()
		{
			base.disposeInternal();

			this.elTable_ = null;
			this.tableBody_ = null;
			this.tableFoot_ = null;
			this.elNavRow_ = null;
			this.elFootRow_ = null;
			this.elMonth_ = null;
			this.elMonthYear_ = null;
			this.elYear_ = null;
			this.elToday_ = null;
			this.elNone_ = null;
		}

		/// <summary>
		/// Click handler for date grid.
		/// </summary>
		/// <param name="event">Click event.</param>
		private void handleGridClick_(goog.events.Event ev)
		{
			if (((HTMLElement)ev.target).TagName == goog.dom.TagName.TD) {
				// colIndex/rowIndex is broken in Safari, find position by looping
				Node el;
				int x = -2, y = -2;  // first col/row is for weekday/weeknum
				for (el = ((Node)ev.target); el != null; el = el.PreviousSibling, x++) {
				}
				for (el = ((Node)ev.target).ParentNode; el != null; el = el.PreviousSibling, y++) {
				}
				var obj = this.grid_[y][x];
				if (this.isUserSelectableDate_(obj)) {
					this.setDate(obj.clone());
				}
			}
		}

		/// <summary>
		/// Keypress handler for date grid.
		/// </summary>
		/// <param name="event">Keypress event.</param>
		private void handleGridKeyPress_(goog.events.BrowserEvent ev)
		{
			int months = 0, days = 0;
			switch (ev.keyCode) {
			case 33:  // Page up
				ev.preventDefault();
				months = -1;
				break;
			case 34:  // Page down
				ev.preventDefault();
				months = 1;
				break;
			case 37:  // Left
				ev.preventDefault();
				days = -1;
				break;
			case 39:  // Right
				ev.preventDefault();
				days = 1;
				break;
			case 38:  // Down
				ev.preventDefault();
				days = -7;
				break;
			case 40:  // Up
				ev.preventDefault();
				days = 7;
				break;
			case 36:  // Home
				ev.preventDefault();
				this.selectToday();
				goto case 46;
			case 46:  // Delete
				ev.preventDefault();
				this.selectNone();
				break;
			case 13:  // Enter
			case 32:  // Space
				ev.preventDefault();
				this.setDate_(this.date_, true /* fireSelection */);
				goto default;
			default:
				return;
			}
			goog.date.Date date;
			if (this.date_ != null) {
				date = this.date_.clone();
				date.add(new goog.date.Interval(0, months, days));
			}
			else {
				date = this.activeMonth_.clone();
				date.setDate(1);
			}
			if (this.isUserSelectableDate_(date)) {
				this.setDate_(date, false /* fireSelection */);
			}
		}

		/// <summary>
		/// Click handler for month button. Opens month selection menu.
		/// </summary>
		/// <param name="event">Click event.</param>
		private void showMonthMenu_(goog.events.Event ev)
		{
			ev.stopPropagation();

			var list = new JsArray<string>();
			for (var i = 0; i < 12; i++) {
				list.Push(this.symbols_.STANDALONEMONTHS[i]);
			}
			this.createMenu_(
				this.elMonth_, list, this.handleMonthMenuClick_,
				this.symbols_.STANDALONEMONTHS[this.activeMonth_.getMonth()]);
		}

		/// <summary>
		/// Click handler for year button. Opens year selection menu.
		/// </summary>
		/// <param name="event">Click event.</param>
		private void showYearMenu_(goog.events.Event ev)
		{
			ev.stopPropagation();

			var list = new JsArray<string>();
			var year = this.activeMonth_.getFullYear();
			var loopDate = this.activeMonth_.clone();
			for (var i = -goog.ui.DatePicker.YEAR_MENU_RANGE_;
				 i <= goog.ui.DatePicker.YEAR_MENU_RANGE_; i++) {
				loopDate.setFullYear(year + i);
				list.Push(this.i18nDateFormatterYear_.format(loopDate));
			}
			this.createMenu_(
				this.elYear_, list, this.handleYearMenuClick_,
				this.i18nDateFormatterYear_.format(this.activeMonth_));
		}

		/// <summary>
		/// Call back function for month menu.
		/// </summary>
		/// <param name="target">Selected item.</param>
		private void handleMonthMenuClick_(Element target)
		{
			var itemIndex = Int32.Parse(target.GetAttribute("itemIndex"));
			this.activeMonth_.setMonth(itemIndex);
			this.updateCalendarGrid_();

			if (true/*this.elMonth_.Focus*/) {
				this.elMonth_.Focus();
			}
		}

		/// <summary>
		/// Call back function for year menu.
		/// </summary>
		/// <param name="target">Selected item.</param>
		private void handleYearMenuClick_(Element target)
		{
			if (target.FirstChild.NodeType == NodeType.Text) {
				// We use the same technique used for months to get the position of the
				// item in the menu, as the year is not necessarily numeric.
				var itemIndex = Int32.Parse(target.GetAttribute("itemIndex"));
				var year = this.activeMonth_.getFullYear();
				this.activeMonth_.setFullYear(
					year + itemIndex - goog.ui.DatePicker.YEAR_MENU_RANGE_);
				this.updateCalendarGrid_();
			}

			this.elYear_.Focus();
		}

		/// <summary>
		/// Support function for menu creation.
		/// </summary>
		/// <param name="srcEl">Button to create menu for.</param>
		/// <param name="items">List of items to populate menu with.</param>
		/// <param name="method">Call back method.</param>
		/// <param name="selected">Item to mark as selected in menu.</param>
		private void createMenu_(
			HTMLElement srcEl, JsArray<string> items, Action<HTMLElement> method, string selected)
		{
			this.destroyMenu_(null);

			var el = this.dom_.createElement(goog.dom.TagName.DIV);
			el.ClassName = le.getCssName(this.getBaseCssClass(), "menu");

			this.menuSelected_ = null;

			var ul = this.dom_.createElement(goog.dom.TagName.UL);
			for (var i = 0; i < items.Length; i++) {
				var li = this.dom_.createDom(goog.dom.TagName.LI, null, items[i]);
				li.SetAttribute("itemIndex", i.ToString());
				if (items[i] == selected) {
					this.menuSelected_ = li;
				}
				ul.AppendChild(li);
			}
			el.AppendChild(ul);
			//srcEl = (HTMLElement)(srcEl);
			el.Style.Left = srcEl.OffsetLeft + ((HTMLElement)srcEl.ParentNode).OffsetLeft + "px";
			el.Style.Top = srcEl.OffsetTop + "px";
			el.Style.Width = srcEl.ClientWidth + "px";
			this.elMonth_.ParentNode.AppendChild(el);

			this.menu_ = el;
			if (this.menuSelected_ == null) {
				this.menuSelected_ = (HTMLElement)(ul.FirstChild);
			}
			this.menuSelected_.ClassName =
				le.getCssName(this.getBaseCssClass(), "menu-selected");
			this.menuCallback_ = method;

			var eh = this.getHandler();
			eh.listen(this.menu_, goog.events.EventType.CLICK, new Action<events.Event>(this.handleMenuClick_));
			eh.listen(
				this.getKeyHandlerForElement_(this.menu_),
				goog.events.KeyHandler.EventType.KEY, new Action<events.BrowserEvent>(this.handleMenuKeyPress_));
			eh.listen(
				this.dom_.getDocument(), goog.events.EventType.CLICK, new Action<events.BrowserEvent>(this.destroyMenu_));
			el.TabIndex = 0;
			el.Focus();
		}

		/// <summary>
		/// Click handler for menu.
		/// </summary>
		/// <param name="event">Click event.</param>
		private void handleMenuClick_(goog.events.Event ev)
		{
			ev.stopPropagation();

			this.destroyMenu_(null);
			if (this.menuCallback_ != null) {
				this.menuCallback_((HTMLElement)(ev.target));
			}
		}

		/// <summary>
		/// Keypress handler for menu.
		/// </summary>
		/// <param name="event">Keypress event.</param>
		private void handleMenuKeyPress_(goog.events.BrowserEvent ev)
		{
			// Prevent the grid keypress handler from catching the keypress event.
			ev.stopPropagation();

			HTMLElement el = null;
			var menuSelected = this.menuSelected_;
			switch (ev.keyCode) {
			case 35:  // End
				ev.preventDefault();
				el = (HTMLElement)menuSelected.ParentNode.LastChild;
				break;
			case 36:  // Home
				ev.preventDefault();
				el = (HTMLElement)menuSelected.ParentNode.FirstChild;
				break;
			case 38:  // Up
				ev.preventDefault();
				el = (HTMLElement)menuSelected.PreviousSibling;
				break;
			case 40:  // Down
				ev.preventDefault();
				el = (HTMLElement)menuSelected.NextSibling;
				break;
			case 13:  // Enter
			case 9:   // Tab
			case 0:   // Space
				ev.preventDefault();
				this.destroyMenu_(null);
				this.menuCallback_(menuSelected);
				break;
			}
			if (el != null && el != menuSelected) {
				menuSelected.ClassName = "";
				el.ClassName = le.getCssName(this.getBaseCssClass(), "menu-selected");
				this.menuSelected_ = (HTMLElement)(el);
			}
		}

		/// <summary>
		/// Support function for menu destruction.
		/// </summary>
		private void destroyMenu_(events.BrowserEvent e)
		{
			if (this.menu_ != null) {
				var eh = this.getHandler();
				eh.unlisten(this.menu_, goog.events.EventType.CLICK, new Action<events.Event>(this.handleMenuClick_));
				eh.unlisten(
					this.getKeyHandlerForElement_(this.menu_),
					goog.events.KeyHandler.EventType.KEY, new Action<events.BrowserEvent>(this.handleMenuKeyPress_));
				eh.unlisten(
					this.dom_.getDocument(), goog.events.EventType.CLICK,
					new Action<events.BrowserEvent>(this.destroyMenu_));
				goog.dom.removeNode(this.menu_);
				Script.Delete(ref this.menu_);
			}
		}

		/// <summary>
		/// Determines the dates/weekdays for the current month and builds an in memory
		/// representation of the calendar.
		/// </summary>
		private void updateCalendarGrid_()
		{
			if (this.getElement() == null) {
				return;
			}

			var date = this.activeMonth_.clone();
			date.setDate(1);

			// Show year name of select month
			if (this.elMonthYear_ != null) {
				goog.dom.setTextContent(
					this.elMonthYear_, this.i18nDateFormatterMonthYear_.format(date));
			}
			if (this.elMonth_ != null) {
				goog.dom.setTextContent(
					this.elMonth_, this.symbols_.STANDALONEMONTHS[date.getMonth()]);
			}
			if (this.elYear_ != null) {
				goog.dom.setTextContent(
					this.elYear_, this.i18nDateFormatterYear_.format(date));
			}

			var wday = date.getWeekday();
			var days = date.getNumberOfDaysInMonth();

			// Determine how many days to show for previous month
			date.add(new goog.date.Interval(goog.date.Interval.MONTHS, -1));
			date.setDate(date.getNumberOfDaysInMonth() - (wday - 1));

			if (this.showFixedNumWeeks_ && !this.extraWeekAtEnd_ && days + wday < 33) {
				date.add(new goog.date.Interval(goog.date.Interval.DAYS, -7));
			}

			// Create weekday/day grid
			var dayInterval = new goog.date.Interval(goog.date.Interval.DAYS, 1);
			this.grid_ = new JsArray<JsArray<date.Date>>();
			for (var y = 0; y < 6; y++) {  // Weeks
				this.grid_[y] = new JsArray<date.Date>();
				for (var x = 0; x < 7; x++) {  // Weekdays
					this.grid_[y][x] = date.clone();
					// Date.add breaks dates before year 100 by adding 1900 to the year
					// value. As a workaround we store the year before the add and reapply it
					// after (with special handling for January 1st).
					var year = date.getFullYear();
					date.add(dayInterval);
					if (date.getMonth() == 0 && date.getDate() == 1) {
						// Increase year on January 1st.
						year++;
					}
					date.setFullYear(year);
				}
			}

			this.redrawCalendarGrid_();
		}

		/// <summary>
		/// Draws calendar view from in memory representation and applies class names
		/// depending on the selection, weekday and whatever the day belongs to the
		/// active month or not.
		/// </summary>
		private void redrawCalendarGrid_()
		{
			if (this.getElement() == null) {
				return;
			}

			var month = this.activeMonth_.getMonth();
			var today = new goog.date.Date();
			var todayYear = today.getFullYear();
			var todayMonth = today.getMonth();
			var todayDate = today.getDate();

			// Draw calendar week by week, a worst case month has six weeks.
			for (var y = 0; y < 6; y++) {
				// Draw week number, if enabled
				if (this.showWeekNum_) {
					goog.dom.setTextContent(
						this.elTable_[y + 1][0],
						this.i18nDateFormatterWeek_.format(this.grid_[y][0]));
					goog.dom.classlist.set(
						this.elTable_[y + 1][0],
						le.getCssName(this.getBaseCssClass(), "week"));
				}
				else {
					goog.dom.setTextContent(this.elTable_[y + 1][0], "");
					goog.dom.classlist.set(this.elTable_[y + 1][0], "");
				}

				for (var x = 0; x < 7; x++) {
					var o = this.grid_[y][x];
					var el = this.elTable_[y + 1][x + 1];

					// Assign a unique element id (required for setting the active descendant
					// ARIA role) unless already set.
					if (el.Id == null) {
						el.Id = (++cellIdGenerator_).ToString();
					}
					goog.asserts.assert(el != null, "The table DOM element cannot be null.");
					goog.a11y.aria.setRole(el, a11y.aria.Role.GRIDCELL);
					// Set the aria label of the grid cell to the month plus the day.
					goog.a11y.aria.setLabel(
						el, this.i18nDateFormatterDayAriaLabel_.format(o));

					var classes = new JsArray<string>() { le.getCssName(this.getBaseCssClass(), "date") };
					if (!this.isUserSelectableDate_(o)) {
						classes.Push(
							le.getCssName(this.getBaseCssClass(), "unavailable-date"));
					}
					if (this.showOtherMonths_ || o.getMonth() == month) {
						// Date belongs to previous or next month
						if (o.getMonth() != month) {
							classes.Push(le.getCssName(this.getBaseCssClass(), "other-month"));
						}

						// Apply styles set by setWeekdayClass
						var wday = (x + this.activeMonth_.getFirstDayOfWeek() + 7) % 7;
						if (this.wdayStyles_[wday] != null) {
							classes.Push(this.wdayStyles_[wday]);
						}

						// Current date
						if (o.getDate() == todayDate && o.getMonth() == todayMonth &&
							o.getFullYear() == todayYear) {
							classes.Push(le.getCssName(this.getBaseCssClass(), "today"));
						}

						// Selected date
						if (this.date_ != null && o.getDate() == this.date_.getDate() &&
							o.getMonth() == this.date_.getMonth() &&
							o.getFullYear() == this.date_.getFullYear()) {
							classes.Push(le.getCssName(this.getBaseCssClass(), "selected"));
							goog.asserts.assert(
								this.tableBody_ != null, "The table body DOM element cannot be null");
							goog.a11y.aria.setState(this.tableBody_, a11y.aria.State.ACTIVEDESCENDANT, el.Id);
						}

						// Custom decorator
						if (this.decoratorFunction_ != null) {
							var customClass = this.decoratorFunction_(o);
							if (customClass != null) {
								classes.Push(customClass);
							}
						}

						// Set cell text to the date and apply classes.
						var formatedDate = this.longDateFormat_ ?
							this.i18nDateFormatterDay2_.format(o) :
							this.i18nDateFormatterDay_.format(o);
						goog.dom.setTextContent(el, formatedDate);
						// Date belongs to previous or next month and showOtherMonths is false,
						// clear text and classes.
					}
					else {
						goog.dom.setTextContent(el, "");
					}
					goog.dom.classlist.set(el, classes.Join(" "));
				}

				// Hide the either the last one or last two weeks if they contain no days
				// from the active month and the showFixedNumWeeks is false. The first four
				// weeks are always shown as no month has less than 28 days).
				if (y >= 4) {
					var parentEl = (HTMLElement)(
						this.elTable_[y + 1][0].ParentElement ??
						this.elTable_[y + 1][0].ParentNode);
					goog.style.setElementShown(
						parentEl,
						this.grid_[y][0].getMonth() == month || this.showFixedNumWeeks_);
				}
			}
		}

		/// <summary>
		/// Fires the CHANGE_ACTIVE_MONTH event.
		/// </summary>
		private void fireChangeActiveMonthEvent_()
		{
			var changeMonthEvent = new goog.ui.DatePickerEvent(
				goog.ui.DatePicker.Events.CHANGE_ACTIVE_MONTH, this,
				this.getActiveMonth());
			this.dispatchEvent(changeMonthEvent);
		}

		/// <summary>
		/// Draw weekday names, if enabled. Start with whatever day has been set as the
		/// first day of week.
		/// </summary>
		private void redrawWeekdays_()
		{
			if (this.getElement() == null) {
				return;
			}
			if (this.showWeekdays_) {
				for (var x = 0; x < 7; x++) {
					var el = this.elTable_[0][x + 1];
					var wday = (x + this.activeMonth_.getFirstDayOfWeek() + 7) % 7;
					goog.dom.setTextContent(el, this.wdayNames_[(wday + 1) % 7]);
				}
			}
			var parentEl = (HTMLElement)(
				this.elTable_[0][0].ParentElement ?? this.elTable_[0][0].ParentNode);
			goog.style.setElementShown(parentEl, this.showWeekdays_);
		}

		/// <summary>
		/// Returns the key handler for an element and caches it so that it can be
		/// retrieved at a later point.
		/// </summary>
		/// <param name="el">The element to get the key handler for.</param>
		/// <returns>The key handler for the element.</returns>
		private goog.events.KeyHandler getKeyHandlerForElement_(HTMLElement el)
		{
			var uid = le.getUid(el);
			if (!this.keyHandlers_.ContainsKey(uid)) {
				this.keyHandlers_[uid] = new goog.events.KeyHandler(el);
			}
			return this.keyHandlers_[uid];
		}
	}

	public class DatePickerEvent : goog.events.Event
	{
		/// <summary>
		/// The selected date
		/// </summary>
		private date.Date date;

		public DatePickerEvent(string type, DatePicker target, date.Date date)
			: base(type, target)
		{
			this.date = date;
		}
	}
}
