// Copyright 2009 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview The SafeStyle type and its builders.
 *
 * TODO(xtof): Link to document stating type contract.
 */

using System;
using System.Collections.Generic;
using Bridge;
using System.Text.RegularExpressions;

namespace goog.html
{
	public class SafeStyle
	{
		/// <summary>
		/// The contained value of this SafeStyle.  The field has a purposely
		/// ugly name to make (non-compiled) code that attempts to directly access this
		/// field stand out.
		/// </summary>
		private string privateDoNotAccessOrElseSafeStyleWrappedValue_ = "";

		private object SAFE_STYLE_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ =
			TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_;

		private static readonly object TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ = new object();


		public static SafeStyle EMPTY =
			goog.html.SafeStyle.createSafeStyleSecurityPrivateDoNotAccessOrElse("");

		public const string INNOCUOUS_STRING = "zClosurez";

		internal static SafeStyle create(Dictionary<string, object> map)
		{
			var style = "";
			foreach (var name in map.Keys) {
				if (!new Regex(@"^[-_a-zA-Z0-9]+$").Test(name)) {
					throw new Exception("Name allows only [-_a-zA-Z0-9], got: " + name);
				}
				var value = map[name];
				if (value == null) {
					continue;
				}
				if (value is goog.@string.Const) {
					var str = goog.@string.Const.unwrap((goog.@string.Const)value);
					// These characters can be used to change context and we don"t want that
					// even with const values.
					goog.asserts.assert(!new Regex(@"[{;}]").Test(str), "Value does not allow [{;}].");
				}
				else if (!goog.html.SafeStyle.VALUE_RE_.Test((string)value)) {
					goog.asserts.fail(
						"String value allows only [-,.\"\"%_!# a-zA-Z0-9], rgb() and " +
						"rgba(), got: " + value);
					value = goog.html.SafeStyle.INNOCUOUS_STRING;
				}
				else if (!goog.html.SafeStyle.hasBalancedQuotes_((string)value)) {
					goog.asserts.fail("String value requires balanced quotes, got: " + value);
					value = goog.html.SafeStyle.INNOCUOUS_STRING;
				}
				style += name + ":" + value + ";";
			}
			if (String.IsNullOrEmpty(style)) {
				return goog.html.SafeStyle.EMPTY;
			}
			goog.html.SafeStyle.checkStyle_(style);
			return goog.html.SafeStyle.createSafeStyleSecurityPrivateDoNotAccessOrElse(
				style);
		}

		/// <summary>
		/// Checks that quotes (" and ') are properly balanced inside a string. Assumes
		/// that neither escape (\) nor any other character that could result in
		/// breaking out of a string parsing context are allowed;
		/// see http://www.w3.org/TR/css3-syntax/#string-token-diagram.
		/// </summary>
		/// <param name="value">Untrusted CSS property value.</param>
		/// <returns>True if property value is safe with respect to quote
		/// balancedness.</returns>
		private static bool hasBalancedQuotes_(string value)
		{
			var outsideSingle = true;
			var outsideDouble = true;
			for (var i = 0; i < value.Length; i++) {
				var c = value[i];
				if (c == '\'' && outsideDouble) {
					outsideSingle = !outsideSingle;
				}
				else if (c == '"' && outsideSingle) {
					outsideDouble = !outsideDouble;
				}
			}
			return outsideSingle && outsideDouble;
		}

		/// <summary>
		/// Checks if the style definition is valid.
		/// </summary>
		/// <param name="style"></param>
		private static void checkStyle_(string style)
		{
			goog.asserts.assert(
				!new Regex(@"[<>]").Test(style), "Forbidden characters in style string: " + style);
		}

		/// <summary>
		/// Performs a runtime check that the provided object is indeed a
		/// SafeStyle object, and returns its value.
		/// </summary>
		/// <param name="safeStyle">The object to extract from.</param>
		/// <returns>The safeStyle object's contained string, unless
		/// the run-time type check fails. In that case, {@code unwrap} returns an
		/// innocuous string, or, if assertions are enabled, throws
		/// {@code goog.asserts.AssertionError}.</returns>
		internal static string unwrap(object safeStyle_)
		{
			// Perform additional Run-time type-checking to ensure that
			// safeStyle is indeed an instance of the expected type.  This
			// provides some additional protection against security bugs due to
			// application code that disables type checks.
			// Specifically, the following checks are performed:
			// 1. The object is an instance of the expected type.
			// 2. The object is not an instance of a subclass.
			// 3. The object carries a type marker for the expected type. "Faking" an
			// object requires a reference to the type marker, which has names intended
			// to stand out in code reviews.
			var safeStyle = safeStyle_ as SafeStyle;
			if (safeStyle != null &&
				/*safeStyle.constructor === goog.html.SafeStyle &&*/
				safeStyle.SAFE_STYLE_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ ==
					goog.html.SafeStyle.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_) {
				return safeStyle.privateDoNotAccessOrElseSafeStyleWrappedValue_;
			}
			else {
				goog.asserts.fail("expected object of type SafeStyle, got \'" +
					safeStyle + "\' of type " + safeStyle.GetType().Name);
				return "type_error:SafeStyle";
			}
		}

		/// <summary>
		/// Package-internal utility method to create SafeStyle instances.
		/// </summary>
		/// <param name="style">The string to initialize the SafeStyle object with.</param>
		/// <returns>The initialized SafeStyle object.</returns>
		private static SafeStyle createSafeStyleSecurityPrivateDoNotAccessOrElse(string style)
		{
			return new goog.html.SafeStyle().initSecurityPrivateDoNotAccessOrElse_(style);
		}

		/// <summary>
		/// Called from createSafeStyleSecurityPrivateDoNotAccessOrElse(). This
		/// method exists only so that the compiler can dead code eliminate static
		/// fields (like EMPTY) when they're not accessed.
		/// </summary>
		/// <param name="style"></param>
		/// <returns></returns>
		private SafeStyle initSecurityPrivateDoNotAccessOrElse_(string style)
		{
			this.privateDoNotAccessOrElseSafeStyleWrappedValue_ = style;
			return this;
		}

		/// <summary>
		/// Regular expression for safe values.
		/// 
		/// Quotes (" and ') are allowed, but a check must be done elsewhere to ensure
		/// they're balanced.
		/// 
		/// ',' allows multiple values to be assigned to the same property
		/// (e.g. background-attachment or font-family) and hence could allow
		/// multiple values to get injected, but that should pose no risk of XSS.
		/// 
		/// The rgb() and rgba() expression checks only for XSS safety, not for CSS
		/// validity.
		/// </summary>
		private static readonly Regex VALUE_RE_ =
			new Regex(@"^([-,.""'%_!# a-zA-Z0-9]+|(?:rgb|hsl)a?\([0-9.%, ]+\))$");

	}
}
