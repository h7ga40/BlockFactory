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
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace goog.html
{
	public class SafeHtml : ISafeCode
	{
		/// <summary>
		/// The contained value of this SafeHtml.  The field has a purposely ugly
		/// name to make (non-compiled) code that attempts to directly access this
		/// field stand out.
		/// </summary>
		private string privateDoNotAccessOrElseSafeHtmlWrappedValue_;

		/// <summary>
		/// Type marker for the SafeHtml type, used to implement additional run-time
		/// type checking.
		/// </summary>
		private static readonly object TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ = new { };

		/// <summary>
		/// A type marker used to implement additional run-time type checking.
		/// @see goog.html.SafeHtml#unwrap
		/// </summary>
		private readonly object SAFE_HTML_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_;
		private goog.i18n.bidi.Dir dir_;

		private static readonly Dictionary<string, object> URL_ATTRIBUTES_ = new Dictionary<string, object>() {
			{ "action", true }, { "cite", true }, { "data", true }, { "formaction", true }, { "href", true }, { "manifest", true }, { "poster", true },
			{ "src", true }
		};

		private static readonly Regex VALID_NAMES_IN_TAG_ = new Regex(@"^[a-zA-Z0-9-]+$");

		/// <summary>
		/// Tags which are unsupported via create(). They might be supported via a
		/// tag-specific create method. These are tags which might require a
		/// TrustedResourceUrl in one of their attributes or a restricted type for
		/// their content.
		/// </summary>
		private static readonly Dictionary<string, object> NOT_ALLOWED_TAG_NAMES_ = new Dictionary<string, object>() {
			{ goog.dom.TagName.APPLET,true }, { goog.dom.TagName.BASE, true },
			{ goog.dom.TagName.EMBED, true }, { goog.dom.TagName.IFRAME, true },
			{ goog.dom.TagName.LINK, true }, { goog.dom.TagName.MATH, true },
			{ goog.dom.TagName.META, true }, { goog.dom.TagName.OBJECT, true },
			{ goog.dom.TagName.SCRIPT, true }, { goog.dom.TagName.STYLE, true },
			{ goog.dom.TagName.SVG, true }, { goog.dom.TagName.TEMPLATE, true }
		};

		public SafeHtml()
		{
			privateDoNotAccessOrElseSafeHtmlWrappedValue_ = "";
			SAFE_HTML_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_ = TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_;
			dir_ = null;
		}

		public bool implementsGoogI18nBidiDirectionalString { get; } = true;

		public virtual goog.i18n.bidi.Dir getDirection()
		{
			return this.dir_;
		}

		public bool implementsGoogStringTypedString { get; } = true;

		public string getTypedStringValue()
		{
			return this.privateDoNotAccessOrElseSafeHtmlWrappedValue_;
		}

		/// <summary>
		/// Performs a runtime check that the provided object is indeed a SafeHtml
		/// object, and returns its value.
		/// </summary>
		/// <param name="safeHtml">The object to extract from.</param>
		/// <returns>The SafeHtml object's contained string, unless the run-time
		/// type check fails. In that case, {@code unwrap} returns an innocuous
		/// string, or, if assertions are enabled, throws
		/// {@code goog.asserts.AssertionError}.
		/// </returns>
		public static string unwrap(object safeHtml_)
		{
			// Perform additional run-time type-checking to ensure that safeHtml is indeed
			// an instance of the expected type.  This provides some additional protection
			// against security bugs due to application code that disables type checks.
			// Specifically, the following checks are performed:
			// 1. The object is an instance of the expected type.
			// 2. The object is not an instance of a subclass.
			// 3. The object carries a type marker for the expected type. "Faking" an
			// object requires a reference to the type marker, which has names intended
			// to stand out in code reviews.
			var safeHtml = safeHtml_ as SafeHtml;
			if (safeHtml != null &&
				/*safeHtml.constructor === goog.html.SafeHtml &&*/
				safeHtml.SAFE_HTML_TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_
					== goog.html.SafeHtml.TYPE_MARKER_GOOG_HTML_SECURITY_PRIVATE_) {
				return safeHtml.privateDoNotAccessOrElseSafeHtmlWrappedValue_;
			}
			else {
				goog.asserts.fail("expected object of type SafeHtml, got \"" +
					safeHtml + "\" of type " + safeHtml.GetType().Name);
				return "type_error:SafeHtml";
			}
		}

		/// <summary>
		/// Returns HTML-escaped text as a SafeHtml object.
		/// 
		/// If text is of a type that implements
		/// {@code goog.i18n.bidi.DirectionalString}, the directionality of the new
		/// {@code SafeHtml} object is set to {@code text}'s directionality, if known.
		/// Otherwise, the directionality of the resulting SafeHtml is unknown (i.e.,
		/// {@code null}).
		/// </summary>
		/// <param name="opt_html">The text to escape. If
		/// the parameter is of type SafeHtml it is returned directly (no escaping
		/// is done).</param>
		/// <returns>The escaped text, wrapped as a SafeHtml.</returns>
		public static SafeHtml htmlEscape(object textOrHtml_)
		{
			if (textOrHtml_ is goog.html.SafeHtml) {
				return textOrHtml_ as goog.html.SafeHtml;
			}
			var textOrHtml = textOrHtml_ as goog.html.ISafeCode;
			goog.i18n.bidi.Dir dir = null;
			if (textOrHtml != null && textOrHtml.implementsGoogI18nBidiDirectionalString) {
				dir = textOrHtml.getDirection();
			}
			string textAsString;
			if (textOrHtml != null && textOrHtml.implementsGoogStringTypedString) {
				textAsString = textOrHtml.getTypedStringValue();
			}
			else {
				textAsString = textOrHtml_.ToString();
			}
			return goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				goog.@string.htmlEscape(textAsString), dir);
		}

		/// <summary>
		/// Returns HTML-escaped text as a SafeHtml object, with newlines changed to
		/// &lt;br&gt;.
		/// </summary>
		/// <param name="textOrHtml">The text to escape. If
		/// the parameter is of type SafeHtml it is returned directly (no escaping
		/// is done).</param>
		/// <returns>The escaped text, wrapped as a SafeHtml.</returns>
		public static SafeHtml htmlEscapePreservingNewlines(Union<string, SafeHtml> textOrHtml)
		{
			if (textOrHtml.Is<SafeHtml>()) {
				return textOrHtml.As<SafeHtml>();
			}
			var html = goog.html.SafeHtml.htmlEscape(textOrHtml);
			return goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				goog.@string.newLineToBr(goog.html.SafeHtml.unwrap(html)),
				html.getDirection());
		}

		public static SafeHtml create(string tagName, Dictionary<string, object> opt_attributes = null,
			params SafeHtml[] opt_content)
		{
			verifyTagName(tagName);
			return createSafeHtmlTagSecurityPrivateDoNotAccessOrElse(tagName, opt_attributes, new JsArray<SafeHtml>(opt_content));
		}

		/// <summary>
		/// Verifies if the tag name is valid and if it doesn't change the context.
		/// E.g. STRONG is fine but SCRIPT throws because it changes context. See
		/// goog.html.SafeHtml.create for an explanation of allowed tags.
		/// </summary>
		/// <param name="tagName"></param>
		public static void verifyTagName(string tagName)
		{
			if (!goog.html.SafeHtml.VALID_NAMES_IN_TAG_.Test(tagName)) {
				throw new Exception("Invalid tag name <' + tagName + '>.");
			}
			if (goog.html.SafeHtml.NOT_ALLOWED_TAG_NAMES_.ContainsKey(tagName.ToUpperCase())) {
				throw new Exception("Tag name <' + tagName + '> is not allowed for SafeHtml.");
			}
		}

		private static string getAttrNameAndValue_(string tagName, string name, object value)
		{
			// If it"s goog.@string.Const, allow any valid attribute name.
			if (value is goog.@string.Const) {
				value = goog.@string.Const.unwrap(value as goog.@string.Const);
			}
			else if (name.ToLowerCase() == "style") {
				value = goog.html.SafeHtml.getStyleValue_(value);
			}
			else if (new Regex(@"^on", RegexOptions.IgnoreCase).Test(name)) {
				// TODO(jakubvrana): Disallow more attributes with a special meaning.
				throw new Exception(
					"Attribute \"" + name + "\" requires goog.@string.Const value, \"" + value +
					"\" given.");
				// URL attributes handled differently accroding to tag.
			}
			else if (goog.html.SafeHtml.URL_ATTRIBUTES_.ContainsKey(name.ToLowerCase())) {
				if (value is goog.html.TrustedResourceUrl) {
					value = goog.html.TrustedResourceUrl.unwrap(value);
				}
				else if (value is goog.html.SafeUrl) {
					value = goog.html.SafeUrl.unwrap(value);
				}
				else if (value is string) {
					value = goog.html.SafeUrl.sanitize(value).getTypedStringValue();
				}
				else {
					throw new Exception(
						"Attribute \"" + name + "\" on tag \"" + tagName +
						"\" requires goog.html.SafeUrl, goog.@string.Const, or string," +
						" value \"" + value + "\" given.");
				}
			}

			// Accept SafeUrl, TrustedResourceUrl, etc. for attributes which only require
			// HTML-escaping.
			var code = value as ISafeCode;
			if (code != null && code.implementsGoogStringTypedString) {
				// Ok to call getTypedStringValue() since there"s no reliance on the type
				// contract for security here.
				value = code.getTypedStringValue();
			}

			goog.asserts.assert(
				value is string || value is int || value is double,
				"String or number value expected, got " + value.GetType().Name +
					" with value: " + value);
			return name + "=\"" + goog.@string.htmlEscape(value.ToString()) + "\"";
		}

		private static string getStyleValue_(object value)
		{
			if (value is string || value is int || value is double) {
				throw new Exception(
					"The \"style\" attribute requires goog.html.SafeStyle or map " +
					"of style properties, " + (value.GetType().Name) + " given: " + value);
			}
			if (!(value is goog.html.SafeStyle)) {
				// Process the property bag into a style object.
				value = goog.html.SafeStyle.create((Dictionary<string, object>)value);
			}
			return goog.html.SafeStyle.unwrap(value);
		}

		public static SafeHtml concat(params SafeHtml[] var_args)
		{
			var dir = goog.i18n.bidi.Dir.NEUTRAL;
			var content = "";

			Action<object> addArgument = null;
			/**
			 * @param {!goog.html.SafeHtml.TextOrHtml_|
			 *     !Array<!goog.html.SafeHtml.TextOrHtml_>} argument
			 */
			addArgument = new Action<object>((argument) => {
				if (argument is IEnumerable) {
					foreach (var i in argument as IEnumerable) addArgument(i);
				}
				else {
					var html = goog.html.SafeHtml.htmlEscape(argument);
					content += goog.html.SafeHtml.unwrap(html);
					var htmlDir = html.getDirection();
					if (dir == goog.i18n.bidi.Dir.NEUTRAL) {
						dir = htmlDir;
					}
					else if (htmlDir != goog.i18n.bidi.Dir.NEUTRAL && dir != htmlDir) {
						dir = null;
					}
				}
			});

			foreach (var i in var_args) addArgument(i);
			return goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				content, dir);
		}

		/// <summary>
		/// Like create() but does not restrict which tags can be constructed.
		/// </summary>
		/// <param name="tagName"></param>
		/// <param name="opt_attributes"></param>
		/// <param name="opt_content"></param>
		/// <returns></returns>
		public static SafeHtml createSafeHtmlTagSecurityPrivateDoNotAccessOrElse(string tagName, Dictionary<string, object> opt_attributes = null, JsArray<SafeHtml> opt_content = null)
		{
			goog.i18n.bidi.Dir dir = null;
			var result = "<" + tagName;
			result += goog.html.SafeHtml.stringifyAttributes(tagName, opt_attributes);

			var content = opt_content;
			if (content == null) {
				content = new JsArray<SafeHtml>();
			}
			else if (!(content is IEnumerable<SafeHtml>)) {
				content = new JsArray<SafeHtml>((IEnumerable<SafeHtml>)content);
			}

			if (goog.dom.tags.isVoidTag(tagName.ToLowerCase())) {
				goog.asserts.assert(
					content.Length == 0, "Void tag <" + tagName + "> does not allow content.");
				result += ">";
			}
			else {
				var html = goog.html.SafeHtml.concat(content);
				result += ">" + goog.html.SafeHtml.unwrap(html) + "</" + tagName + ">";
				dir = html.getDirection();
			}

			var dirAttribute = opt_attributes != null && opt_attributes.ContainsKey("dir") ? (string)opt_attributes["dir"] : null;
			if (dirAttribute != null) {
				if (new Regex(@"^(ltr|rtl|auto)$", RegexOptions.IgnoreCase).Test(dirAttribute)) {
					// If the tag has the "dir" attribute specified then its direction is
					// neutral because it can be safely used in any context.
					dir = goog.i18n.bidi.Dir.NEUTRAL;
				}
				else {
					dir = null;
				}
			}

			return goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				result, dir);
		}

		/// <summary>
		/// Package-internal utility method to create SafeHtml instances.
		/// </summary>
		/// <param name="html">The string to initialize the SafeHtml object with.</param>
		/// <param name="dir">The directionality of the SafeHtml to be
		/// constructed, or null if unknown.</param>
		/// <returns>The initialized SafeHtml object.</returns>
		public static SafeHtml createSafeHtmlSecurityPrivateDoNotAccessOrElse(string html, goog.i18n.bidi.Dir dir)
		{
			return (new goog.html.SafeHtml()).initSecurityPrivateDoNotAccessOrElse_(
				html, dir);
		}

		/// <summary>
		/// Called from createSafeHtmlSecurityPrivateDoNotAccessOrElse(). This
		/// method exists only so that the compiler can dead code eliminate static
		/// fields (like EMPTY) when they're not accessed.
		/// </summary>
		/// <param name="html"></param>
		/// <param name="dir"></param>
		/// <returns></returns>
		private SafeHtml initSecurityPrivateDoNotAccessOrElse_(string html, goog.i18n.bidi.Dir dir)
		{
			this.privateDoNotAccessOrElseSafeHtmlWrappedValue_ = html;
			this.dir_ = dir;
			return this;
		}

		/// <summary>
		/// Creates a string with attributes to insert after tagName.
		/// </summary>
		/// <param name="tagName"></param>
		/// <param name="opt_attributes"></param>
		/// <returns>Returns an empty string if there are no attributes, returns
		/// a string starting with a space otherwise.</returns>
		public static string stringifyAttributes(string tagName, Dictionary<string, object> opt_attributes)
		{
			var result = "";
			if (opt_attributes != null) {
				foreach (var name in opt_attributes.Keys) {
					if (!goog.html.SafeHtml.VALID_NAMES_IN_TAG_.Test(name)) {
						throw new Exception("Invalid attribute name \"" + name + "\".");
					}
					if (opt_attributes.TryGetValue(name, out var value) && value == null) {
						continue;
					}
					result +=
						" " + goog.html.SafeHtml.getAttrNameAndValue_(tagName, name, value);
				}
			}
			return result;
		}

		/// <summary>
		/// A SafeHtml instance corresponding to the empty string.
		/// </summary>
		public static readonly SafeHtml EMPTY =
			goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				"", goog.i18n.bidi.Dir.NEUTRAL);

		/// <summary>
		/// A SafeHtml instance corresponding to the <br> tag.
		/// </summary>
		public static readonly SafeHtml BR =
			goog.html.SafeHtml.createSafeHtmlSecurityPrivateDoNotAccessOrElse(
				"<br>", goog.i18n.bidi.Dir.NEUTRAL);
	}

	public interface ISafeCode
	{
		bool implementsGoogI18nBidiDirectionalString { get; }
		goog.i18n.bidi.Dir getDirection();
		bool implementsGoogStringTypedString { get; }
		string getTypedStringValue();
	}
}
