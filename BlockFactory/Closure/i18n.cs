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
using System.Collections.Generic;
using Bridge;

namespace goog
{
	public static class i18n
	{
		public static class bidi
		{
			public class Dir
			{
				public static Dir NEUTRAL = new Dir();
				public static Dir LTR = new Dir();
			}
		}

		public static DateTimeSymbolsType DateTimeSymbols;

		/// <summary>
		/// Date/time formatting symbols for locale ja.
		/// </summary>
		public static DateTimeSymbolsType DateTimeSymbols_ja = new DateTimeSymbolsType() {
			ERAS = new string[] { "紀元前", "西暦" },
			ERANAMES = new string[] { "紀元前", "西暦" },
			NARROWMONTHS = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" },
			STANDALONENARROWMONTHS = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" },
			MONTHS = new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" },
			STANDALONEMONTHS = new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" },
			SHORTMONTHS = new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" },
			STANDALONESHORTMONTHS = new string[] { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" },
			WEEKDAYS = new string[] { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" },
			STANDALONEWEEKDAYS = new string[] { "日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日" },
			SHORTWEEKDAYS = new string[] { "日", "月", "火", "水", "木", "金", "土" },
			STANDALONESHORTWEEKDAYS = new string[] { "日", "月", "火", "水", "木", "金", "土" },
			NARROWWEEKDAYS = new string[] { "日", "月", "火", "水", "木", "金", "土" },
			STANDALONENARROWWEEKDAYS = new string[] { "日", "月", "火", "水", "木", "金", "土" },
			SHORTQUARTERS = new string[] { "Q1", "Q2", "Q3", "Q4" },
			QUARTERS = new string[] { "第1四半期", "第2四半期", "第3四半期", "第4四半期" },
			AMPMS = new string[] { "午前", "午後" },
			DATEFORMATS = new string[] { "y年M月d日EEEE", "y年M月d日", "y/MM/dd", "y/MM/dd" },
			TIMEFORMATS = new string[] { "H時mm分ss秒 zzzz", "H:mm:ss z", "H:mm:ss", "H:mm" },
			DATETIMEFORMATS = new string[] { "{1} {0}", "{1} {0}", "{1} {0}", "{1} {0}" },
			FIRSTDAYOFWEEK = 6,
			WEEKENDRANGE = new int[] { 5, 6 },
			FIRSTWEEKCUTOFFDAY = 5
		};

		public static readonly DateTimePatternsType DateTimePatterns_ja = new DateTimePatternsType() {
			YEAR_FULL = "y年",
			YEAR_FULL_WITH_ERA = "Gy年",
			YEAR_MONTH_ABBR = "y年M月",
			YEAR_MONTH_FULL = "y年M月",
			MONTH_DAY_ABBR = "M月d日",
			MONTH_DAY_FULL = "M月dd日",
			MONTH_DAY_SHORT = "M/d",
			MONTH_DAY_MEDIUM = "M月d日",
			MONTH_DAY_YEAR_MEDIUM = "y年M月d日",
			WEEKDAY_MONTH_DAY_MEDIUM = "M月d日(EEE)",
			WEEKDAY_MONTH_DAY_YEAR_MEDIUM = "y年M月d日(EEE)",
			DAY_ABBR = "d日"
		};

		public static readonly DateTimePatternsType DateTimePatterns_ja_JP = DateTimePatterns_ja;
		public static readonly DateTimePatternsType DateTimePatterns = DateTimePatterns_ja_JP;

		public static IEnumerable<string> Keys {
			get {
				foreach (var field in typeof(i18n).GetFields()) {
					if (field.FieldType != typeof(DateTimeSymbolsType))
						continue;
					yield return field.Name;
				}
			}
		}

		public class DateTimeSymbolsType
		{
			public string[] ERAS;
			public string[] ERANAMES;
			public string[] NARROWMONTHS;
			public string[] STANDALONENARROWMONTHS;
			public string[] MONTHS;
			public string[] STANDALONEMONTHS;
			public string[] SHORTMONTHS;
			public string[] STANDALONESHORTMONTHS;
			public string[] WEEKDAYS;
			public string[] STANDALONEWEEKDAYS;
			public string[] SHORTWEEKDAYS;
			public string[] STANDALONESHORTWEEKDAYS;
			public string[] NARROWWEEKDAYS;
			public string[] STANDALONENARROWWEEKDAYS;
			public string[] SHORTQUARTERS;
			public string[] QUARTERS;
			public string[] AMPMS;
			public string[] DATEFORMATS;
			public string[] TIMEFORMATS;
			public string[] DATETIMEFORMATS;
			public int FIRSTDAYOFWEEK;
			public int[] WEEKENDRANGE;
			public int FIRSTWEEKCUTOFFDAY;
		}

		public class DateTimePatternsType
		{
			public string YEAR_FULL;
			public string YEAR_FULL_WITH_ERA;
			public string YEAR_MONTH_ABBR;
			public string YEAR_MONTH_FULL;
			public string MONTH_DAY_ABBR;
			public string MONTH_DAY_FULL;
			public string MONTH_DAY_SHORT;
			public string MONTH_DAY_MEDIUM;
			public string MONTH_DAY_YEAR_MEDIUM;
			public string WEEKDAY_MONTH_DAY_MEDIUM;
			public string WEEKDAY_MONTH_DAY_YEAR_MEDIUM;
			public string DAY_ABBR;
		}

		public static DateTimeSymbolsType GetValue(string name)
		{
			return (DateTimeSymbolsType)typeof(i18n).GetField(name).GetValue(null);
		}

		internal class DateTimeFormat
		{
			public enum Format
			{
				FULL_DATE = 0,
				LONG_DATE = 1,
				MEDIUM_DATE = 2,
				SHORT_DATE = 3,
				FULL_TIME = 4,
				LONG_TIME = 5,
				MEDIUM_TIME = 6,
				SHORT_TIME = 7,
				FULL_DATETIME = 8,
				LONG_DATETIME = 9,
				MEDIUM_DATETIME = 10,
				SHORT_DATETIME = 11
			}

			class PatternPart
			{
				public string text;
				public int type;
			}

			private JsArray<PatternPart> patternParts_;

			/// <summary>
			/// Data structure that with all the locale info needed for date formatting.
			/// (day/month names, most common patterns, rules for week-end, etc.)
			/// </summary>
			private DateTimeSymbolsType dateTimeSymbols_;

			public DateTimeFormat(string pattern, DateTimeSymbolsType opt_dateTimeSymbols = null)
			{
				this.patternParts_ = new JsArray<PatternPart>();
				this.dateTimeSymbols_ = opt_dateTimeSymbols ?? goog.i18n.DateTimeSymbols;
			}

			internal string format(date.Date date, TimeZone opt_timeZone = null)
			{
				throw new NotImplementedException();
			}
		}

		public class TimeZone
		{

		}
	}
}
