﻿// Copyright 2013 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview The SafeUrl type and its builders.
 *
 * TODO(xtof): Link to document stating type contract.
 */

using Bridge;
using System.Text.RegularExpressions;
using System;

namespace goog.html
{
	public class SafeUrl : ISafeCode
	{
		/// <summary>
		/// The contained value of this SafeUrl.  The field has a purposely ugly
		/// name to make (non-compiled) code that attempts to directly access this
		/// field stand out.
		/// </summary>
		private string privateDoNotAccessOrElseSafeHtmlWrappedValue_ = "";

		private object SAFE_URL_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ =
			goog.html.SafeUrl.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_;

		/// <summary>
		/// The innocuous string generated by goog.html.SafeUrl.sanitize when passed
		/// an unsafe URL.
		/// 
		/// about:invalid is registered in
		/// http://www.w3.org/TR/css3-values/#about-invalid.
		/// http://tools.ietf.org/html/rfc6694#section-2.2.1 permits about URLs to
		/// contain a fragment, which is not to be considered when determining if an
		/// about URL is well-known.
		/// 
		/// Using about:invalid seems preferable to using a fixed data URL, since
		/// browsers might choose to not report CSP violations on it, as legitimate
		/// CSS function calls to attr() can result in this URL being produced. It is
		/// also a standard URL which matches exactly the semantics we need:
		/// "The about:invalid URI references a non-existent document with a generic
		/// error condition. It can be used when a URI is necessary, but the default
		/// value shouldn't be resolveable as any type of document".
		/// </summary>
		public const string INNOCUOUS_STRING = "about:invalid#zClosurez";

		public bool implementsGoogStringTypedString => true;

		public bool implementsGoogI18nBidiDirectionalString => true;

		/// <summary>
		/// Returns this SafeUrl's value a string.
		/// 
		/// IMPORTANT: In code where it is security relevant that an object's type is
		/// indeed {@code SafeUrl}, use {@code goog.html.SafeUrl.unwrap} instead of this
		/// method. If in doubt, assume that it's security relevant. In particular, note
		/// that goog.html functions which return a goog.html type do not guarantee that
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
		/// IMPORTANT: The guarantees of the SafeUrl type contract only extend to the
		/// behavior of browsers when interpreting URLs. Values of SafeUrl objects MUST
		/// be appropriately escaped before embedding in a HTML document. Note that the
		/// required escaping is context-sensitive (e.g. a different escaping is
		/// required for embedding a URL in a style property within a style
		/// attribute, as opposed to embedding in a href attribute).
		/// </summary>
		/// <returns></returns>
		public string getTypedStringValue()
		{
			return this.privateDoNotAccessOrElseSafeHtmlWrappedValue_;
		}

		/// <summary>
		/// Returns this URLs directionality, which is always {@code LTR}.
		/// </summary>
		/// <returns></returns>
		public goog.i18n.bidi.Dir getDirection()
		{
			return goog.i18n.bidi.Dir.LTR;
		}

		/// <summary>
		/// Performs a runtime check that the provided object is indeed a SafeUrl
		/// object, and returns its value.
		/// 
		/// IMPORTANT: The guarantees of the SafeUrl type contract only extend to the
		/// behavior of  browsers when interpreting URLs. Values of SafeUrl objects MUST
		/// be appropriately escaped before embedding in a HTML document. Note that the
		/// required escaping is context-sensitive (e.g. a different escaping is
		/// required for embedding a URL in a style property within a style
		/// attribute, as opposed to embedding in a href attribute).
		/// </summary>
		/// <param name="safeUrl">The object to extract from.</param>
		/// <returns>The SafeUrl object's contained string, unless the run-time
		/// type check fails. In that case, {@code unwrap} returns an innocuous
		/// string, or, if assertions are enabled, throws
		/// {@code goog.asserts.AssertionError}.</returns>
		public static string unwrap(object safeUrl_)
		{
			// Perform additional Run-time type-checking to ensure that safeUrl is indeed
			// an instance of the expected type.  This provides some additional protection
			// against security bugs due to application code that disables type checks.
			// Specifically, the following checks are performed:
			// 1. The object is an instance of the expected type.
			// 2. The object is not an instance of a subclass.
			// 3. The object carries a type marker for the expected type. "Faking" an
			// object requires a reference to the type marker, which has names intended
			// to stand out in code reviews.
			var safeUrl = safeUrl_ as goog.html.SafeUrl;
			if (safeUrl_ != null &&
				/*safeUrl.constructor === goog.html.SafeUrl &&*/
				safeUrl.SAFE_URL_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ ==
					goog.html.SafeUrl.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_) {
				return safeUrl.privateDoNotAccessOrElseSafeHtmlWrappedValue_;
			}
			else {
				goog.asserts.fail("expected object of type SafeUrl, got \'" +
					safeUrl + "\' of type " + safeUrl.GetType().Name);
				return "type_error:SafeUrl";
			}
		}

		private static readonly Regex SAFE_URL_PATTERN_ =
			new Regex(@"^(?:(?:https?|mailto|ftp):|[^&:/?#]*(?:[/?#]|$))", RegexOptions.IgnoreCase);

		/// <summary>
		/// Creates a SafeUrl object from {@code url}. If {@code url} is a
		/// goog.html.SafeUrl then it is simply returned. Otherwise the input string is
		/// validated to match a pattern of commonly used safe URLs.
		/// 
		/// {@code url} may be a URL with the http, https, mailto or ftp scheme,
		/// or a relative URL (i.e., a URL without a scheme; specifically, a
		/// scheme-relative, absolute-path-relative, or path-relative URL).
		/// 
		/// @see http://url.spec.whatwg.org/#concept-relative-url
		/// </summary>
		/// <param name="url">The URL to validate.</param>
		/// <returns></returns>
		internal static SafeUrl sanitize(object url_)
		{
			string str;
			var url = url_ as ISafeCode;
			if (url is goog.html.SafeUrl) {
				return (SafeUrl)url;
			}
			else if (url.implementsGoogStringTypedString) {
				str = url.getTypedStringValue();
			}
			else {
				str = url.ToString();
			}
			if (!goog.html.SafeUrl.SAFE_URL_PATTERN_.Test(str)) {
				str = goog.html.SafeUrl.INNOCUOUS_STRING;
			}
			return goog.html.SafeUrl.createSafeUrlSecurityPrivateDoNotAccessOrElse(str);
		}

		/// <summary>
		/// Type marker for the SafeUrl type, used to implement additional run-time
		/// type checking.
		/// </summary>
		private static readonly object TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ = new object();

		/// <summary>
		/// Package-internal utility method to create SafeUrl instances.
		/// </summary>
		/// <param name="url">The string to initialize the SafeUrl object with.</param>
		/// <returns>The initialized SafeUrl object.</returns>
		private static SafeUrl createSafeUrlSecurityPrivateDoNotAccessOrElse(string url)
		{
			var safeUrl = new goog.html.SafeUrl();
			safeUrl.privateDoNotAccessOrElseSafeHtmlWrappedValue_ = url;
			return safeUrl;
		}
	}
}
