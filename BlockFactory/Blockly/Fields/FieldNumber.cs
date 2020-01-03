/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2016 Google Inc.
 * https://developers.google.com/blockly/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * @fileoverview Number input field
 * @author fenichel@google.com (Rachel Fenichel)
 */
using System;
using System.Text.RegularExpressions;
using Bridge;

namespace Blockly
{
	public class FieldNumber : FieldTextInput
	{
		/// <summary>
		/// Class for an editable number field.
		/// </summary>
		/// <param name="value">The initial content of the field.</param>
		/// <param name="opt_min">Minimum value.</param>
		/// <param name="opt_max">Maximum value.</param>
		/// <param name="opt_precision">Precision for value.</param>
		/// <param name="opt_validator">An optional function that is called
		/// to validate any constraints on what the user entered.  Takes the new
		/// text as an argument and returns either the accepted text, a replacement
		/// text, or null to abort the change.</param>
		public FieldNumber(string value, Union<string, double> opt_min = null,
			Union<string, double> opt_max = null, Union<string, double> opt_precision = null,
			Func<Field, string, object> opt_validator = null)
			: base(value, opt_validator)
		{
			value = value.ToString();

			if (opt_min == null) opt_min = Double.NaN;
			if (opt_max == null) opt_max = Double.NaN;
			if (opt_precision == null) opt_precision = Double.NaN;

			this.setConstraints(opt_min, opt_max, opt_precision);
		}

		private double precision_;
		private double min_;
		private double max_;

		/// <summary>
		/// Set the maximum, minimum and precision constraints on this field.
		/// Any of these properties may be undefiend or NaN to be disabled.
		/// Setting precision (usually a power of 10) enforces a minimum step between
		/// values. That is, the user's value will rounded to the closest multiple of
		/// precision. The least significant digit place is inferred from the precision.
		/// Integers values can be enforces by choosing an integer precision.
		/// </summary>
		/// <param name="min">Minimum value.</param>
		/// <param name="max">Maximum value.</param>
		/// <param name="precision">Precision for value.</param>
		public void setConstraints(Union<string, double> _min, Union<string, double> _max,
			Union<string, double> _precision)
		{
			var precision = _precision.Is<string>() ? Script.ParseFloat(_precision.As<string>()) : (double)_precision;
			this.precision_ = Double.IsNaN(precision) ? 0 : precision;
			double min, max;
			if (_min.Is<double>()) min = _min.As<double>(); else if (!Double.TryParse(_min.As<string>(), out min)) min = Double.NaN;
			this.min_ = Double.IsNaN(min) ? Double.NegativeInfinity : min;
			if (_max.Is<double>()) max = _max.As<double>(); else if (!Double.TryParse(_max.As<string>(), out max)) max = Double.NaN;
			this.max_ = Double.IsNaN(max) ? Double.PositiveInfinity : max;
			this.setValue(this.callValidator(this.getValue()));
		}

		/// <summary>
		/// Ensure that only a number in the correct range may be entered.
		/// </summary>
		/// <param name="text">The user's text</param>
		/// <returns>A string representing a valid number, or null if invalid.</returns>
		public override object classValidator(string text)
		{
			if (text == null) {
				return null;
			}
			text = text.ToString();
			// TODO: Handle cases like 'ten', '1.203,14', etc.
			// 'O' is sometimes mistaken for '0' by inexperienced users.
			text = text.Replace(new Regex(@"O", RegexOptions.Multiline | RegexOptions.IgnoreCase), "0");
			// Strip out thousands separators.
			text = text.Replace(new Regex(@",", RegexOptions.Multiline), "");
			var n = Script.ParseFloat(text ?? "0");
			if (Double.IsNaN(n)) {
				// Invalid number.
				return null;
			}
			// Round to nearest multiple of precision.
			if (this.precision_ != 0 && Script.IsFinite(n)) {
				n = System.Math.Round(n / this.precision_) * this.precision_;
			}
			// Get the value in range.
			n = goog.math.clamp(n, this.min_, this.max_);
			return n.ToString();
		}

		/// <summary>
		/// Sets a new change handler for number field.
		/// </summary>
		/// <param name="handler">New change handler, or null.</param>
		public override void setValidator(Func<Field, string, object> handler)
		{
			base.setValidator(handler);
		}

		/// <summary>
		/// Ensure that only a number in the correct range may be entered.
		/// </summary>
		/// <param name="text">The user's text.</param>
		/// <returns>A string representing a valid number, or null if invalid.</returns>
		public override string numberValidator(string text)
		{
			return base.numberValidator(text);
		}
	}
}
