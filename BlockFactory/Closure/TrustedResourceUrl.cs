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
 * @fileoverview The TrustedResourceUrl type and its builders.
 *
 * TODO(xtof): Link to document stating type contract.
 */

using System;

namespace goog.html
{
	public class TrustedResourceUrl : ISafeCode
	{
		/// <summary>
		/// The contained value of this TrustedResourceUrl.  The field has a purposely
		/// ugly name to make (non-compiled) code that attempts to directly access this
		/// field stand out.
		/// </summary>
		private string privateDoNotAccessOrElseTrustedResourceUrlWrappedValue_ = "";

		/// <summary>
		/// A type marker used to implement additional run-time type checking.
		/// @see goog.html.TrustedResourceUrl#unwrap
		/// </summary>
		private object TRUSTED_RESOURCE_URL_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ =
			goog.html.TrustedResourceUrl.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_;

		public bool implementsGoogStringTypedString => true;

		/// <summary>
		/// Returns this TrustedResourceUrl's value as a string.
		/// 
		/// IMPORTANT: In code where it is security relevant that an object's type is
		/// indeed {@code TrustedResourceUrl}, use
		/// {@code goog.html.TrustedResourceUrl.unwrap} instead of this method. If in
		/// doubt, assume that it's security relevant. In particular, note that
		/// goog.html functions which return a goog.html type do not guarantee that
		/// the returned instance is of the right type. For example:
		/// 
		/// <pre>
		/// var fakeSafeHtml = new String('fake');
		/// fakeSafeHtml.__proto__ = goog.html.SafeHtml.prototype;
		/// var newSafeHtml = goog.html.SafeHtml.htmlEscape(fakeSafeHtml);
		/// // newSafeHtml is just an alias for fakeSafeHtml, it's passed through by
		/// // goog.html.SafeHtml.htmlEscape() as fakeSafeHtml instanceof
		/// // goog.html.SafeHtml.
		/// </pre>
		/// 
		/// @see goog.html.TrustedResourceUrl#unwrap
		/// </summary>
		/// <returns></returns>
		public string getTypedStringValue()
		{
			return this.privateDoNotAccessOrElseTrustedResourceUrlWrappedValue_;
		}

		public bool implementsGoogI18nBidiDirectionalString => true;

		public goog.i18n.bidi.Dir getDirection()
		{
			return goog.i18n.bidi.Dir.LTR;
		}

		public static string unwrap(object trustedResourceUrl_)
		{
			// Perform additional Run-time type-checking to ensure that
			// trustedResourceUrl is indeed an instance of the expected type.  This
			// provides some additional protection against security bugs due to
			// application code that disables type checks.
			// Specifically, the following checks are performed:
			// 1. The object is an instance of the expected type.
			// 2. The object is not an instance of a subclass.
			// 3. The object carries a type marker for the expected type. "Faking" an
			// object requires a reference to the type marker, which has names intended
			// to stand out in code reviews.
			var trustedResourceUrl = trustedResourceUrl_ as TrustedResourceUrl;
			if (trustedResourceUrl != null &&
				/*trustedResourceUrl.constructor === goog.html.TrustedResourceUrl &&*/
				trustedResourceUrl.TRUSTED_RESOURCE_URL_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ ==
					goog.html.TrustedResourceUrl.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_) {
				return trustedResourceUrl
					.privateDoNotAccessOrElseTrustedResourceUrlWrappedValue_;
			} else {
				goog.asserts.fail("expected object of type TrustedResourceUrl, got \'" +
					trustedResourceUrl + "\' of type " + trustedResourceUrl.GetType().Name);
				return "type_error:TrustedResourceUrl";
			}
		}

		/// <summary>
		/// Type marker for the TrustedResourceUrl type, used to implement additional
		/// run-time type checking.
		/// </summary>
		private static readonly object TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ = new object();
	}
}
