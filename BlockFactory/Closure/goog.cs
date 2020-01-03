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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;
using goog.html;

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog
{
	public class @string
	{
		public class Const
		{
			/// <summary>
			/// A type marker used to implement additional run-time type checking.
			/// </summary>
			private object STRING_CONST_TYPE_MARKER__GOOG_STRING_SECURITY_PRIVATE_ =
				goog.@string.Const.TYPE_MARKER_;
			/// <summary>
			/// The wrapped value of this Const object.  The field has a purposely ugly
			/// name to make (non-compiled) code that attempts to directly access this
			/// field stand out.
			/// </summary>
			private string stringConstValueWithSecurityContract__googStringSecurityPrivate_;
			/// <summary>
			/// Type marker for the Const type, used to implement additional run-time
			/// type checking.
			/// </summary>
			private static readonly object TYPE_MARKER_ = new object();

			public Const()
			{
			}

			public static string unwrap(Const stringConst)
			{
				// Perform additional run-time type-checking to ensure that stringConst is
				// indeed an instance of the expected type.  This provides some additional
				// protection against security bugs due to application code that disables type
				// checks.
				if (stringConst is goog.@string.Const &&
					/*stringConst.constructor == goog.string.Const &&*/
					stringConst.STRING_CONST_TYPE_MARKER__GOOG_STRING_SECURITY_PRIVATE_ ==
						goog.@string.Const.TYPE_MARKER_) {
					return stringConst
						.stringConstValueWithSecurityContract__googStringSecurityPrivate_;
				}
				else {
					goog.asserts.fail(
						"expected object of type Const, got \"" + stringConst + "\"");
					return "type_error:Const";
				}
			}
		}

		public static bool DETECT_DOUBLE_ESCAPING;

		private static readonly Regex AMP_RE_ = new Regex(@"&", RegexOptions.Multiline);
		private static readonly Regex LT_RE_ = new Regex(@"<", RegexOptions.Multiline);
		private static readonly Regex GT_RE_ = new Regex(@">", RegexOptions.Multiline);
		private static readonly Regex QUOT_RE_ = new Regex(@"""", RegexOptions.Multiline);
		private static readonly Regex SINGLE_QUOTE_RE_ = new Regex(@"'", RegexOptions.Multiline);
		private static readonly Regex NULL_RE_ = new Regex(@"\x00", RegexOptions.Multiline);
		private static readonly Regex E_RE_ = new Regex(@"e", RegexOptions.Multiline);
		private static readonly Regex ALL_RE_ = DETECT_DOUBLE_ESCAPING
			? new Regex(@"[\x00&<>""'e]")
			: new Regex(@"[\x00&<>""']");
		private const bool FORCE_NON_DOM_HTML_UNESCAPING = false;

		public static string htmlEscape(string str, bool opt_isLikelyToContainHtmlChars = false)
		{
			if (opt_isLikelyToContainHtmlChars) {
				str = str.Replace(goog.@string.AMP_RE_, "&amp;")
						 .Replace(goog.@string.LT_RE_, "&lt;")
						 .Replace(goog.@string.GT_RE_, "&gt;")
						 .Replace(goog.@string.QUOT_RE_, "&quot;")
						 .Replace(goog.@string.SINGLE_QUOTE_RE_, "&#39;")
						 .Replace(goog.@string.NULL_RE_, "&#0;");
				if (goog.@string.DETECT_DOUBLE_ESCAPING) {
					str = str.Replace(goog.@string.E_RE_, "&#101;");
				}
				return str;

			}
			else {
				// quick test helps in the case when there are no chars to replace, in
				// worst case this makes barely a difference to the time taken
				if (!goog.@string.ALL_RE_.Test(str)) return str;

				// str.indexOf is faster than regex.test in this case
				if (str.IndexOf("&") != -1) {
					str = str.Replace(goog.@string.AMP_RE_, "&amp;");
				}
				if (str.IndexOf("<") != -1) {
					str = str.Replace(goog.@string.LT_RE_, "&lt;");
				}
				if (str.IndexOf(">") != -1) {
					str = str.Replace(goog.@string.GT_RE_, "&gt;");
				}
				if (str.IndexOf("\"") != -1) {
					str = str.Replace(goog.@string.QUOT_RE_, "&quot;");
				}
				if (str.IndexOf("'") != -1) {
					str = str.Replace(goog.@string.SINGLE_QUOTE_RE_, "&#39;");
				}
				if (str.IndexOf("\x00") != -1) {
					str = str.Replace(goog.@string.NULL_RE_, "&#0;");
				}
				if (goog.@string.DETECT_DOUBLE_ESCAPING && str.IndexOf("e") != -1) {
					str = str.Replace(goog.@string.E_RE_, "&#101;");
				}
				return str;
			}
		}

		public static string newLineToBr(string str, bool opt_xml = false)
		{
			return str.Replace(new Regex(@"(\r\n|\r|\n)", RegexOptions.Multiline), opt_xml ? "<br />" : "<br>");
		}

		public static string unescapeEntities(string str)
		{
			if (str.Contains("&")) {
				// We are careful not to use a DOM if we do not have one or we explicitly
				// requested non-DOM html unescaping.
				if (!goog.@string.FORCE_NON_DOM_HTML_UNESCAPING
					/*&& "document" in goog.global*/) {
					return goog.@string.unescapeEntitiesUsingDom_(str);
				}
				else {
					// Fall back on pure XML entities
					return goog.@string.unescapePureXmlEntities_(str);
				}
			}
			return str;
		}

		private static string unescapePureXmlEntities_(string str)
		{
			return str.Replace(new Regex(@"&([^;]+);", RegexOptions.Multiline), (s, entity) => {
				switch (entity) {
				case "amp":
					return "&";
				case "lt":
					return "<";
				case "gt":
					return ">";
				case "quot":
					return "\"";
				default:
					if (entity[0] == '#') {
						// Prefix with 0 so that hex entities (e.g. &#x10) parse as hex.
						var n = Script.ParseFloat("0" + entity.Substr(1));
						if (!Double.IsNaN(n)) {
							return Char.ConvertFromUtf32((int)n);
						}
					}
					// For invalid entities we just return the entity
					return s;
				}
			});
		}

		private static string unescapeEntitiesUsingDom_(string str, DocumentInstance opt_document = null)
		{
			var seen = new Dictionary<string, string> { { "&amp;", "&" }, { "&lt;", "<" }, { "&gt;", ">" }, { "&quot;", "\"" } };
			HTMLDivElement div;
			if (opt_document != null) {
				div = opt_document.CreateElement<HTMLDivElement>("div");
			}
			else {
				div = Document.CreateElement<HTMLDivElement>("div");
			}
			// Match as many valid entity characters as possible. If the actual entity
			// happens to be shorter, it will still work as innerHTML will return the
			// trailing characters unchanged. Since the entity characters do not include
			// open angle bracket, there is no chance of XSS from the innerHTML use.
			// Since no whitespace is passed to innerHTML, whitespace is preserved.
			return str.Replace(goog.@string.HTML_ENTITY_PATTERN_, (s, entity) => {
				// Check for cached entity.
				var value = seen[s];
				if (value != null) {
					return value;
				}
				// Check for numeric entity.
				if (entity[0] == '#') {
					// Prefix with 0 so that hex entities (e.g. &#x10) parse as hex numbers.
					var n = Script.ParseFloat("0" + entity.Substr(1));
					if (!Double.IsNaN(n)) {
						value = Char.ConvertFromUtf32((int)n);
					}
				}
				// Fall back to innerHTML otherwise.
				if (value == null) {
					// Append a non-entity character to avoid a bug in Webkit that parses
					// an invalid entity at the end of innerHTML text as the empty string.
					div.InnerHTML = s + " ";
					// Then remove the trailing character from the result.
					value = div.FirstChild.NodeValue.ToString().Slice(0, -1);
				}
				// Cache and return.
				return seen[s] = value;
			});
		}

		private static Regex HTML_ENTITY_PATTERN_ = new Regex(@"&([^;\s<&]+);?", RegexOptions.Multiline);

		/// <summary>
		/// Escapes characters in the string that are not safe to use in a RegExp.
		/// </summary>
		/// <param name="s">The string to escape. If not a string, it will be casted</param>
		/// <returns>to one.</returns>
		public static string regExpEscape(string s)
		{
			return s
				.Replace(new Regex(@"([-()\[\]{}+?*.$\^|,:#<!\\])", RegexOptions.Multiline), "\\$1")
				.Replace(new Regex(@"\x08", RegexOptions.Multiline), "\\x08");
		}

		/// <summary>
		/// Removes the breaking spaces from the left and right of the string and
		/// collapses the sequences of breaking spaces in the middle into single spaces.
		/// The original and the result strings render the same way in HTML.
		/// </summary>
		/// <param name="str">A string in which to collapse spaces.</param>
		/// <returns>Copy of the string with normalized breaking spaces.</returns>
		public static string collapseBreakingSpaces(string str)
		{
			return str.Replace(new Regex(@"[\t\r\n ]+", RegexOptions.Multiline), " ")
				.Replace(new Regex(@"^[\t\r\n ]+|[\t\r\n ]+$", RegexOptions.Multiline), "");
		}

		/// <summary>
		/// Replaces Windows and Mac new lines with unix style: \r or \r\n with \n.
		/// </summary>
		/// <param name="str">The string to in which to canonicalize newlines.</param>
		/// <returns>A copy of {@code} with canonicalized newlines.</returns>
		public static string canonicalizeNewlines(string str)
		{
			return str.Replace(new Regex(@"(\r\n|\r|\n)", RegexOptions.Multiline), "\n");
		}

		/// <summary>
		/// Converts a string from selector-case to camelCase (e.g. from
		/// "multi-part-string" to "multiPartString"), useful for converting
		/// CSS selectors and HTML dataset keys to their equivalent JS properties.
		/// </summary>
		/// <param name="str">The string in selector-case form.</param>
		/// <returns>The string in camelCase form.</returns>
		public static string toCamelCase(string str)
		{
			return str.Replace(new Regex(@"\-([a-z])", RegexOptions.Multiline),
				(all, match) => { return match.ToUpperCase(); });
		}

		public static string toTitleCase(string str, string opt_delimiters = null)
		{
			var delimiters = opt_delimiters != null ?
				goog.@string.regExpEscape(opt_delimiters) :
				"\\s";

			// For IE8, we need to prevent using an empty character set. Otherwise,
			// incorrect matching will occur.
			delimiters = !String.IsNullOrEmpty(delimiters) ? "|[" + delimiters + "]+" : "";

			var regexp = new Regex(@"(^" + delimiters + ")([a-z])", RegexOptions.Multiline);
			return str.Replace(
				regexp, (all, p1, p2) => { return p1 + p2.ToUpperCase(); });
		}
	}

	public static class math
	{
		public class Size
		{
			public double width;
			public double height;

			public Size(double width = 0.0, double hieght = 0.0)
			{
				this.width = width;
				this.height = hieght;
			}
		}

		public class Box
		{
			public double left;
			public double top;
			public double right;
			public double bottom;

			public Box(double left, double top, double right, double bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}
		}

		public class Rect
		{
			public double left;
			public double top;
			public double width;
			public double height;
			public double right;
			public double bottom;

			public Rect(double x = 0.0, double y = 0.0, double w = 0.0, double h = 0.0)
			{
				this.left = x;
				this.top = y;
				this.width = w;
				this.height = h;
			}

			public Rect(ClientRect cr)
			{
				this.left = cr.Left;
				this.top = cr.Top;
				this.width = cr.Width;
				this.height = cr.Height;
			}

			public bool contains(Coordinate another)
			{
				return another.x >= this.left && another.x <= this.left + this.width &&
					another.y >= this.top && another.y <= this.top + this.height;
			}
		}

		public class Coordinate
		{
			public double x;
			public double y;
			private Dictionary<string, object> keyValues = new Dictionary<string, object>();

			public Coordinate(double x = 0.0, double y = 0.0)
			{
				this.x = x;
				this.y = y;
			}

			public object this[string name] {
				get { return keyValues.TryGetValue(name, out var value) ? value : null; }
				set { keyValues[name] = value; }
			}

			public static Coordinate difference(Coordinate a, Coordinate b)
			{
				return new Coordinate(a.x - b.x, a.y - b.y);
			}

			public static Coordinate sum(Coordinate a, Coordinate b)
			{
				return new Coordinate(a.x + b.x, a.y + b.y);
			}

			public static bool equals(Coordinate a, Coordinate b)
			{
				if (a == b) {
					return true;
				}
				if (a == null || b == null) {
					return false;
				}
				return a.x == b.x && a.y == b.y;
			}

			public void translate(double tx, double ty)
			{
				this.x += tx;
				this.y += ty;
			}

			public static double distance(Coordinate a, Coordinate b)
			{
				var dx = a.x - b.x;
				var dy = a.y - b.y;
				return System.Math.Sqrt(dx * dx + dy * dy);
			}
		}

		public static double clamp(double value, double min, double max)
		{
			return System.Math.Min(System.Math.Max(value, min), max);
		}

		public static double toDegrees(double angleRadians)
		{
			return angleRadians * 180 / System.Math.PI;
		}

		public static double toRadians(double angleDegrees)
		{
			return angleDegrees * Math.PI / 180;
		}

		public static double lerp(double a, double b, double x)
		{
			return a + x * (b - a);
		}
	}

	public static class dom
	{
		public static class TagName
		{
			public const string INPUT = "input";
			public const string DIV = "div";
			public const string APPLET = "applet";
			public const string BASE = "base";
			public const string EMBED = "embed";
			public const string IFRAME = "iframe";
			public const string LINK = "link";
			public const string MATH = "math";
			public const string META = "meta";
			public const string OBJECT = "object";
			public const string SCRIPT = "script";
			public const string STYLE = "style";
			public const string SVG = "svg";
			public const string TEMPLATE = "template";
			public const string TABLE = "table";
			public const string THEAD = "thead";
			public const string TBODY = "tbody";
			public const string TFOOT = "tfoot";
			public const string TD = "td";
			public const string TR = "tr";
			public const string BUTTON = "button";
			public const string UL = "ul";
			public const string LI = "li";
			public const string BODY = "body";
		}

		public class BrowserFeature
		{
			public static bool INNER_HTML_NEEDS_SCOPED_ELEMENT = goog.userAgent.IE;
			public static bool CAN_ADD_NAME_OR_TYPE_ATTRIBUTES =
				!goog.userAgent.IE || goog.userAgent.isDocumentModeOrHigher(9);
			public static bool CAN_USE_INNER_TEXT =
				goog.userAgent.IE && !goog.userAgent.isVersionOrHigher("9");
		}

		public static Element createDom(string tagName, Union<string, string[], Dictionary<string, string>> opt_properties = null, Union<string, Element> var_args = null)
		{
			var element = Document.CreateElement<Element>(tagName);

			if (opt_properties != null) {
				if (opt_properties.Is<string>()) {
					element.ClassName = opt_properties.As<string>();
				}
				else if (opt_properties.Is<string[]>()) {
					element.ClassName = opt_properties.As<string[]>().Join(" ");
				}
				else {
					foreach (var kvp in opt_properties.As<Dictionary<string, string>>()) {
						element.SetAttribute(kvp.Key, kvp.Value);
					}
				}
			}

			if (var_args != null) {
				element.AppendChild(
					var_args.Is<string>() ? (Node)Document.CreateTextNode(var_args.As<string>()) : var_args.As<Element>());
			}

			return element;
		}

		public static goog.math.Size getViewportSize(WindowInstance opt_window = null)
		{
			// TODO(arv): This should not take an argument
			return goog.dom.getViewportSize_(opt_window ?? Window.Instance);
		}

		/// <summary>
		/// Cross-browser function for getting the document element of a frame or iframe.
		/// </summary>
		/// <param name="frame">Frame element.</param>
		/// <returns>The frame content document.</returns>
		internal static DocumentInstance getFrameContentDocument(HTMLElement frame_)
		{
			if (frame_ is HTMLIFrameElement frame)
				return frame.ContentDocument ??
					frame.ContentWindow.Document;
			return null;
		}

		private static goog.math.Size getViewportSize_(WindowInstance win)
		{
			var doc = win.Document;
			var el = goog.dom.isCss1CompatMode_(doc) ? doc.DocumentElement : doc.Body;
			return new goog.math.Size(el.ClientWidth, el.ClientHeight);
		}

		public static void removeChildren(Node node)
		{
			// Note: Iterations over live collections can be slow, this is the fastest
			// we could find. The double parenthesis are used to prevent JsCompiler and
			// strict warnings.
			Node child;
			while ((child = node.FirstChild) != null) {
				node.RemoveChild(child);
			}
		}

		public static Node removeNode(Node node)
		{
			return node != null && node.ParentNode != null ? node.ParentNode.RemoveChild(node) : null;
		}

		public static bool contains(Node parent, Node descendant)
		{
			if (parent == null || descendant == null) {
				return false;
			}
			// We use browser specific methods for this if available since it is faster
			// that way.

			// IE DOM
			if (parent.IsDefined("contains") && descendant.NodeType == NodeType.Element) {
				return parent == descendant || parent.Contains(descendant);
			}

			// W3C DOM Level 3
			if (parent.IsDefined("compareDocumentPosition")) {
				return parent == descendant ||
					((int)parent.CompareDocumentPosition(descendant) & 16) != 0;
			}

			// W3C DOM Level 1
			while (descendant != null && parent != descendant) {
				descendant = descendant.ParentNode;
			}
			return descendant == parent;
		}

		public static goog.math.Coordinate getDocumentScroll()
		{
			return goog.dom.getDocumentScroll_(Document.Instance);
		}

		public static void insertSiblingAfter(SVGElement newNode, SVGElement refNode)
		{
			if (refNode.ParentNode != null) {
				refNode.ParentNode.InsertBefore(newNode, refNode.NextSibling);
			}
		}

		public class DomHelper
		{
			private DocumentInstance document_;

			public DomHelper(DocumentInstance opt_document = null)
			{
				this.document_ = opt_document ?? Document.Instance;
			}

			public goog.math.Coordinate getDocumentScroll()
			{
				return getDocumentScroll_(this.document_);
			}

			public bool isCss1CompatMode()
			{
				return isCss1CompatMode_(this.document_);
			}

			public DocumentInstance getDocument()
			{
				return document_;
			}

			public HTMLElement createElement(string name)
			{
				return goog.dom.createElement_(this.document_, name);
			}

			public HTMLElement createDom(string tagName,
				Union<string, JsArray<string>, Dictionary<string, string>> opt_attributes = null,
				Union<string, Node, NodeList, JsArray<Node>> var_args = null)
			{
				return goog.dom.createDom_(this.document_, tagName, opt_attributes, var_args);
			}

			public Node safeHtmlToNode(SafeHtml html)
			{
				return goog.dom.safeHtmlToNode_(this.document_, html);
			}

			public Element getElement(Union<string, Element> element)
			{
				return goog.dom.getElementHelper_(this.document_, element);
			}

			public JsArray<Element> getElementsByClass(string className, Union<Element, DocumentInstance> opt_el = null)
			{
				var doc = opt_el != null ? opt_el : this.document_;
				return goog.dom.getElementsByClass(className, doc);
			}

			public JsArray<Element> getElementsByTagNameAndClass(
				string opt_tag = null, string opt_class = null, Element opt_el = null)
			{
				return goog.dom.getElementsByTagNameAndClass_(
					Document.Instance, opt_tag, opt_class, opt_el);
			}

			public Element getElementByClass(string className, Union<Element, DocumentInstance> opt_el = null)
			{
				var doc = opt_el != null ? opt_el : this.document_;
				return goog.dom.getElementByClass(className, doc);
			}

			public Node createTextNode(string content)
			{
				return this.document_.CreateTextNode(content);
			}
		}

		public static Element getElementByClass(string className, Union<Element, DocumentInstance> opt_el = null)
		{
			Node parent = opt_el != null ? (Node)opt_el.Value : Document.Instance;
			Element retVal = null;
			if (parent.IsDefined("getElementsByClassName")) {
				retVal = (Element)(new NodeList(((DocumentInstance)parent).GetElementsByClassName(className)))[0];
			}
			else if (goog.dom.canUseQuerySelector_(parent)) {
				retVal = ((DocumentInstance)parent).QuerySelector("." + className);
			}
			else {
				retVal = goog.dom.getElementsByTagNameAndClass_(
					Document.Instance, "*", className, opt_el)[0];
			}
			return retVal;
		}

		public static JsArray<Element> getElementsByClass(string className, Union<Element, DocumentInstance> opt_el = null)
		{
			Node parent = opt_el != null ? (Node)opt_el.Value : Document.Instance;
			if (goog.dom.canUseQuerySelector_(parent)) {
				var retVal = new JsArray<Element>();
				foreach (var node in ((DocumentInstance)parent).QuerySelectorAll("." + className))
					retVal.Push((Element)node);
				return retVal;
			}
			return goog.dom.getElementsByTagNameAndClass_(
				Document.Instance, "*", className, opt_el);
		}

		private static JsArray<Element> getElementsByTagNameAndClass_(DocumentInstance doc, string opt_tag = null, string opt_class = null, Union<Element, DocumentInstance> opt_el = null)
		{
			Node parent = opt_el != null ? (Node)opt_el.Value : Document.Instance;
			var tagName =
				(opt_tag != null && opt_tag != "*") ? opt_tag.ToString().ToUpperCase() : "";
			var retVal = new JsArray<Element>();

			if (goog.dom.canUseQuerySelector_(parent) && (tagName ?? opt_class) != null) {
				var query = tagName + (opt_class != null ? "." + opt_class : "");
				foreach (var el in ((DocumentInstance)parent).QuerySelectorAll(query))
					retVal.Push((Element)el);
				return retVal;
			}

			// Use the native getElementsByClassName if available, under the assumption
			// that even when the tag name is specified, there will be fewer elements to
			// filter through when going by class than by tag name
			if (opt_class != null && parent.IsDefined("getElementsByClassName")) {
				var els = ((DocumentInstance)parent).GetElementsByClassName(opt_class);

				if (tagName != null) {
					// Filter for specific tags if requested.
					foreach (var el in els) {
						if (tagName == el.NodeName) {
							retVal.Push((Element)el);
						}
					}

					return retVal;
				}
				else {
					foreach (var el in els)
						retVal.Push((Element)el);
					return retVal;
				}
			}
			{
				var els = ((DocumentInstance)parent).GetElementsByTagName(tagName ?? "*");

				if (opt_class != null) {
					foreach (var el in els) {
						string className = ((Element)el).ClassName;
						// Check if className has a split function since SVG className does not.
						if (/*typeof className.Split == "function" &&*/
							className.Split(new Regex(@"\s +")).Contains(opt_class)) {
							retVal.Push((Element)el);
						}
					}
					return retVal;
				}
				else {
					foreach (var el in els)
						retVal.Push((Element)el);
					return retVal;
				}
			}
		}

		public static string getTextContent(Node node)
		{
			string textContent;
			// Note(arv): IE9, Opera, and Safari 3 support innerText but they include
			// text nodes in script tags. So we revert to use a user agent test here.
			if (goog.dom.BrowserFeature.CAN_USE_INNER_TEXT && node != null &&
				(node.IsDefined("innerText"))) {
				textContent = goog.@string.canonicalizeNewlines((string)Script.Get(node.Instance, "innerText"));
				// Unfortunately .innerText() returns text with &shy; symbols
				// We need to filter it out and then remove duplicate whitespaces
			}
			else {
				var buf = new JsArray<string>();
				goog.dom.getTextContent_(node, buf, true);
				textContent = buf.Join("");
			}

			// Strip &shy; entities. goog.format.insertWordBreaks inserts them in Opera.
			textContent = textContent.Replace(@"/ \xAD /g", " ").Replace(@"/\xAD/g", "");
			// Strip &#8203; entities. goog.format.insertWordBreaks inserts them in IE8.
			textContent = textContent.Replace(@"/\u200B/g", "");

			// Skip this replacement on old browsers with working innerText, which
			// automatically turns &nbsp; into " " and / +/ into " " when reading
			// innerText.
			if (!goog.dom.BrowserFeature.CAN_USE_INNER_TEXT) {
				textContent = textContent.Replace(@"/ +/g", " ");
			}
			if (textContent != " ") {
				textContent = textContent.Replace(@"/^\s*/", "");
			}

			return textContent;
		}

		private static bool canUseQuerySelector_(Node parent)
		{
			return parent.IsDefined("querySelectorAll") && parent.IsDefined("querySelector");
		}

		public static Element getElementHelper_(DocumentInstance doc, Union<string, Element> element)
		{
			return element.Is<string>() ? (Element)doc.GetElementById(element.As<string>()) : element.As<HTMLElement>();
		}

		private static Node safeHtmlToNode_(DocumentInstance doc, SafeHtml html)
		{
			var tempDiv = goog.dom.createElement_(doc, goog.dom.TagName.DIV);
			if (goog.dom.BrowserFeature.INNER_HTML_NEEDS_SCOPED_ELEMENT) {
				goog.dom.safe.setInnerHtml(
					tempDiv, goog.html.SafeHtml.concat(goog.html.SafeHtml.BR, html));
				tempDiv.RemoveChild(tempDiv.FirstChild);
			}
			else {
				goog.dom.safe.setInnerHtml(tempDiv, html);
			}
			return goog.dom.childrenToNode_(doc, tempDiv);
		}

		private static Node childrenToNode_(DocumentInstance doc, HTMLElement tempDiv)
		{
			if (tempDiv.ChildNodes.Length == 1) {
				return tempDiv.RemoveChild(tempDiv.FirstChild);
			}
			else {
				var fragment = doc.CreateDocumentFragment();
				while (tempDiv.FirstChild != null) {
					fragment.AppendChild(tempDiv.FirstChild);
				}
				return fragment;
			}
		}

		private static HTMLElement createDom_(DocumentInstance document_, string tagName,
			Union<string, JsArray<string>, Dictionary<string, string>> attributes,
			Union<string, Node, NodeList, JsArray<Node>> args)
		{
			// Internet Explorer is dumb:
			// name: https://msdn.microsoft.com/en-us/library/ms534184(v=vs.85).aspx
			// type: https://msdn.microsoft.com/en-us/library/ms534700(v=vs.85).aspx
			// Also does not allow setting of 'type' attribute on 'input' or 'button'.
			if (!goog.dom.BrowserFeature.CAN_ADD_NAME_OR_TYPE_ATTRIBUTES && attributes != null &&
				(Script.IsDefined(attributes, "name") || Script.IsDefined(attributes, "type"))) {
				var tagNameArr = new JsArray<string> { "<", tagName };
				if (Script.IsDefined(attributes, "name")) {
					tagNameArr.Push(" name=\"", goog.@string.htmlEscape(Script.Get(attributes, "name").ToString()), "\"");
				}
				if (Script.IsDefined(attributes, "type")) {
					tagNameArr.Push(" type=\"", goog.@string.htmlEscape(Script.Get(attributes, "type").ToString()), "\"");

					// Clone attributes map to remove "type" without mutating the input.
					var clone = new Dictionary<string, string>();
					foreach (var kvp in attributes.As<Dictionary<string, string>>()) {
						clone.Add(kvp.Key, kvp.Value);
					}

					// JSCompiler can"t see how goog.object.extend added this property,
					// because it was essentially added by reflection.
					// So it needs to be quoted.
					clone.Remove("type");

					attributes = clone;
				}
				tagNameArr.Push(">");
				tagName = tagNameArr.Join("");
			}

			var element = Document.CreateElement<HTMLElement>(tagName);

			if (attributes != null) {
				if (attributes.Is<string>()) {
					element.ClassName = attributes.As<string>();
				}
				else if (attributes.Is<JsArray<string>>()) {
					element.ClassName = attributes.As<JsArray<string>>().Join(" ");
				}
				else {
					goog.dom.setProperties(element, attributes.As<Dictionary<string, string>>());
				}
			}

			if (args != null) {
				goog.dom.append_(Document.Instance, element, args);
			}

			return element;
		}

		private static void append_(DocumentInstance doc, HTMLElement parent, Union<string, Node, NodeList, JsArray<Node>> args)
		{
			var childHandler = new Action<Union<string, Node>>((child) => {
				// TODO(user): More coercion, ala MochiKit?
				if (child != null) {
					parent.AppendChild(
						child.Is<string>() ? doc.CreateTextNode(child.As<string>()) : child.As<Node>());
				}
			});

			if (args.Is<string>() || args.Is<Node>()) {
				childHandler(new Union<string, Node>(args.Value));
			}
			else if (args.Is<JsArray<Node>>()) {
				foreach (var arg in args.As<JsArray<Node>>()) {
					childHandler(arg);
				}
			}
			else {
				foreach (var arg in args.As<NodeList>()) {
					childHandler(arg);
				}
			}
		}

		private static void setProperties(HTMLElement element, Dictionary<string, string> properties)
		{
			foreach (var kvp in properties) {
				var key = kvp.Key;
				var val = kvp.Value;
				if (key == "style") {
					element.Style.CssText = val;
				}
				else if (key == "class") {
					element.ClassName = val;
				}
				else if (key == "for") {
					((HTMLLabelElement)element).HtmlFor = val;
				}
				else if (goog.dom.DIRECT_ATTRIBUTE_MAP_.ContainsKey(key)) {
					element.SetAttribute(goog.dom.DIRECT_ATTRIBUTE_MAP_[key], val);
				}
				else if (
				  key.StartsWith("aria-") ||
				  key.StartsWith("data-")) {
					element.SetAttribute(key, val);
				}
				else {
					element[key] = val;
				}
			}
		}

		private static readonly Dictionary<string, string> DIRECT_ATTRIBUTE_MAP_ = new Dictionary<string, string>{
			{ "cellpadding", "cellPadding" },
			{ "cellspacing", "cellSpacing" },
			{ "colspan", "colSpan" },
			{ "frameborder", "frameBorder" },
			{ "height", "height" },
			{ "maxlength", "maxLength" },
			{ "nonce", "nonce" },
			{ "role", "role" },
			{ "rowspan", "rowSpan" },
			{ "type", "type" },
			{ "usemap", "useMap" },
			{ "valign", "vAlign" },
			{ "width", "width}" }
		};

		private static HTMLElement createElement_(DocumentInstance doc, string name)
		{
			return doc.CreateElement<HTMLElement>(name);
		}

		private static bool isCss1CompatMode_(DocumentInstance doc)
		{
			//if (goog.dom.COMPAT_MODE_KNOWN_) {
			//	return goog.dom.ASSUME_STANDARDS_MODE;
			//}

			return doc.CompatMode == CompatMode.CSS1Compat;
		}

		private static goog.math.Coordinate getDocumentScroll_(DocumentInstance doc)
		{
			var el = goog.dom.getDocumentScrollElement_(doc);
			var win = goog.dom.getWindow_(doc);
			if (goog.userAgent.IE && goog.userAgent.isVersionOrHigher("10") &&
				win.PageYOffset != el.ScrollTop) {
				// The keyboard on IE10 touch devices shifts the page using the pageYOffset
				// without modifying scrollTop. For this case, we want the body scroll
				// offsets.
				return new goog.math.Coordinate(el.ScrollLeft, el.ScrollTop);
			}
			return new goog.math.Coordinate(
				win.PageXOffset != 0 ? win.PageXOffset : el.ScrollLeft, win.PageYOffset != 0 ? win.PageYOffset : el.ScrollTop);
		}

		private static WindowInstance getWindow_(DocumentInstance doc)
		{
			return doc.IsDefined("parentWindow") ? doc.ParentWindow : doc.DefaultView;
		}

		private static HTMLElement getDocumentScrollElement_(DocumentInstance doc)
		{
			// Old WebKit needs body.scrollLeft in both quirks mode and strict mode. We
			// also default to the documentElement if the document does not have a body
			// (e.g. a SVG document).
			// Uses http://dev.w3.org/csswg/cssom-view/#dom-document-scrollingelement to
			// avoid trying to guess about browser behavior from the UA string.
			if (doc.IsDefined("scrollingElement")) {
				return doc.ScrollingElement;
			}
			if (!goog.userAgent.WEBKIT && goog.dom.isCss1CompatMode_(doc)) {
				return doc.DocumentElement;
			}
			return doc.Body ?? doc.DocumentElement;
		}

		public static DocumentInstance getOwnerDocument(Node node)
		{
			// TODO(nnaze): Update param signature to be non-nullable.
			goog.asserts.assert(node != null, "Node cannot be null or undefined.");
			return (node.NodeType == NodeType.Document ? (DocumentInstance)node : node.OwnerDocument) ??
				new DocumentInstance(node["document"]);
		}

		static DomHelper defaultDomHelper_;

		public static DomHelper getDomHelper(Node opt_element = null)
		{
			return opt_element != null ?
				new goog.dom.DomHelper(goog.dom.getOwnerDocument(opt_element)) :
				(goog.dom.defaultDomHelper_ ??
				 (goog.dom.defaultDomHelper_ = new goog.dom.DomHelper()));
		}

		public static DocumentInstance getDocument()
		{
			return Document.Instance;
		}

		public class safe
		{
			private static Dictionary<string, bool> SET_INNER_HTML_DISALLOWED_TAGS_ =
				new Dictionary<string, bool>() {
					{ "MATH", true },
					{ "SCRIPT", true },
					{ "STYLE", true },
					{ "SVG", true },
					{ "TEMPLATE", true }
				};

			public static void setInnerHtml(HTMLElement elem, html.SafeHtml html)
			{
				if (goog.asserts.ENABLE_ASSERTS) {
					var tagName = elem.TagName.ToUpperCase();
					if (goog.dom.safe.SET_INNER_HTML_DISALLOWED_TAGS_.TryGetValue(tagName, out var value) && value) {
						throw new Exception(
							"goog.dom.safe.setInnerHtml cannot be used to set content of " +
							elem.TagName + ".");
					}
				}
				elem.InnerHTML = goog.html.SafeHtml.unwrap(html);
			}
		}

		public class classlist
		{
			public const bool ALWAYS_USE_DOM_TOKEN_LIST = false;

			/// <summary>
			/// Gets an array-like object of class names on an element.
			/// </summary>
			/// <param name="element">DOM node to get the classes of.</param>
			/// <returns>Class names on {@code element}.</returns>
			public static JsArray<string> get(HTMLElement element)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					return new JsArray<string>(element.ClassList);
				}

				var className = element.ClassName;
				// Some types of elements don't have a className in IE (e.g. iframes).
				// Furthermore, in Firefox, className is not a string when the element is
				// an SVG element.
				string[] m = null;
				if (className != null)
					m = className.Match(new Regex(@"\S+", RegexOptions.Multiline));
				return m != null ? new JsArray<string>(m) : new JsArray<string>();
			}

			/// <summary>
			/// Sets the entire class name of an element.
			/// </summary>
			/// <param name="element">DOM node to set class of.</param>
			/// <param name="className">Class name(s) to apply to element.</param>
			public static void set(HTMLElement element, string className)
			{
				element.ClassName = className;
			}

			/// <summary>
			/// Returns true if an element has a class.  This method may throw a DOM
			/// exception for an invalid or empty class name if DOMTokenList is used.
			/// </summary>
			/// <param name="element">DOM node to test.</param>
			/// <param name="className">Class name to test for.</param>
			/// <returns>Whether element has the class.</returns>
			public static bool contains(HTMLElement element, string className)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					return element.ClassList.Contains(className);
				}
				return goog.dom.classlist.get(element).Contains(className);
			}

			/// <summary>
			/// Adds a class to an element.  Does not add multiples of class names.  This
			/// method may throw a DOM exception for an invalid or empty class name if
			/// DOMTokenList is used.
			/// </summary>
			/// <param name="element">DOM node to add class to.</param>
			/// <param name="className">Class name to add.</param>
			public static void add(HTMLElement element, string className)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					element.ClassList.Add(className);
					return;
				}

				if (!goog.dom.classlist.contains(element, className)) {
					// Ensure we add a space if this is not the first class name added.
					var temp = element.ClassName;
					element.ClassName =
						!String.IsNullOrEmpty(temp) ? (temp + " " + className) : className;
				}
			}

			/// <summary>
			/// Convenience method to add a number of class names at once.
			/// </summary>
			/// <param name="element">The element to which to add classes.</param>
			/// <param name="classesToAdd">An array-like object
			/// containing a collection of class names to add to the element.
			/// This method may throw a DOM exception if classesToAdd contains invalid
			/// or empty class names.</param>
			public static void addAll(HTMLElement element, JsArray<string> classesToAdd)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					classesToAdd.ForEach((className) => {
						goog.dom.classlist.add(element, className);
					});
					return;
				}

				var classMap = new Dictionary<string, object>();

				// Get all current class names into a map.
				goog.dom.classlist.get(element).ForEach((className) => {
					classMap[className] = true;
				});

				// Add new class names to the map.

				classesToAdd.ForEach((className) => { classMap[className] = true; });

				// Flatten the keys of the map into the className.
				var classNames = "";
				foreach (var className in classMap.Keys) {
					classNames +=
						classNames.Length > 0 ? (" " + className) : className;
				}
				element.ClassName = classNames;
			}

			/// <summary>
			/// Removes a class from an element.  This method may throw a DOM exception
			/// for an invalid or empty class name if DOMTokenList is used.
			/// </summary>
			/// <param name="element">DOM node to remove class from.</param>
			/// <param name="className">Class name to remove.</param>
			public static void remove(HTMLElement element, string className)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					element.ClassList.Remove(className);
					return;
				}

				if (goog.dom.classlist.contains(element, className)) {
					// Filter out the class name.
					element.ClassName = goog.dom.classlist.get(element).Where(
						(c) => { return c != className; }).Join(" ");
				}
			}

			/// <summary>
			/// Removes a set of classes from an element.  Prefer this call to
			/// repeatedly calling {@code goog.dom.classlist.remove} if you want to remove
			/// a large set of class names at once.
			/// </summary>
			/// <param name="element">The element from which to remove classes.</param>
			/// <param name="classesToRemove">An array-like object
			/// containing a collection of class names to remove from the element.
			/// This method may throw a DOM exception if classesToRemove contains invalid
			/// or empty class names.</param>
			public static void removeAll(HTMLElement element, JsArray<string> classesToRemove)
			{
				if (goog.dom.classlist.ALWAYS_USE_DOM_TOKEN_LIST || element.ClassList != null) {
					foreach (var className in classesToRemove) {
						element.ClassList.Remove(className);
					}
					return;
				}
				// Filter out those classes in classesToRemove.
				element.ClassName = goog.dom.classlist.get(element).Where(
					(className) => { return classesToRemove.Contains(className); }).Join(" ");
			}

			/// <summary>
			/// Adds or removes a class depending on the enabled argument.  This method
			/// may throw a DOM exception for an invalid or empty class name if DOMTokenList
			/// is used.
			/// </summary>
			/// <param name="element">DOM node to add or remove the class on.</param>
			/// <param name="className">Class name to add or remove.</param>
			/// <param name="enable">Whether to add or remove the class (true adds,
			/// false removes).</param>
			public static void enable(HTMLElement element, string className, bool enabled)
			{
				if (enabled) {
					goog.dom.classlist.add(element, className);
				}
				else {
					goog.dom.classlist.remove(element, className);
				}
			}

			/// <summary>
			/// Adds or removes a set of classes depending on the enabled argument.  This
			/// method may throw a DOM exception for an invalid or empty class name if
			/// DOMTokenList is used.
			/// </summary>
			/// <param name="element">DOM node to add or remove the class on.</param>
			/// <param name="classesToEnable">An array-like object
			/// containing a collection of class names to add or remove from the element.</param>
			/// <param name="enabled">Whether to add or remove the classes (true adds,
			/// false removes).</param>
			public static void enableAll(HTMLElement element, JsArray<string> classesToEnable, bool enabled)
			{
				if (enabled) {
					goog.dom.classlist.addAll(element, classesToEnable);
				}
				else {
					goog.dom.classlist.removeAll(element, classesToEnable);
				}
			}

			/// <summary>
			/// Adds and removes a class of an element.  Unlike
			/// {@link goog.dom.classlist.swap}, this method adds the classToAdd regardless
			/// of whether the classToRemove was present and had been removed.  This method
			/// may throw a DOM exception if the class names are empty or invalid.
			/// </summary>
			/// <param name="element">DOM node to swap classes on.</param>
			/// <param name="classToRemove">Class to remove.</param>
			/// <param name="classToAdd">Class to add.</param>
			internal static void addRemove(HTMLElement element, string classToRemove, string classToAdd)
			{
				goog.dom.classlist.remove(element, classToRemove);
				goog.dom.classlist.add(element, classToAdd);
			}
		}

		public static void setTextContent(Node node, string text)
		{
			goog.asserts.assert(
				node != null,
				"goog.dom.setTextContent expects a non-null value for node");

			if (Script.IsDefined(node, "textContent")) {
				node.TextContent = text;
			}
			else if (node.NodeType == NodeType.Text) {
				((Text)node).Data = text;
			}
			else if (node.FirstChild != null && node.FirstChild.NodeType == NodeType.Text) {
				// If the first child is a text node we just change its data and remove the
				// rest of the children.
				while (node.LastChild != node.FirstChild) {
					node.RemoveChild(node.LastChild);
				}
				((Text)node.FirstChild).Data = text;
			}
			else {
				goog.dom.removeChildren(node);
				var doc = goog.dom.getOwnerDocument(node);
				node.AppendChild(doc.CreateTextNode(text));
			}
		}

		public class tags
		{
			private static Dictionary<string, bool> VOID_TAGS_ = new Dictionary<string, bool> {
				{ "area", true },
				{ "base", true },
				{ "br", true },
				{ "col", true },
				{ "command", true },
				{ "embed", true },
				{ "hr", true },
				{ "img", true },
				{ "input", true },
				{ "keygen", true },
				{ "link", true },
				{ "meta", true },
				{ "param", true },
				{ "source", true },
				{ "track", true },
				{ "wbr", true },
			};

			public static bool isVoidTag(string tagName)
			{
				return goog.dom.tags.VOID_TAGS_.TryGetValue(tagName, out var tag) && tag;
			}
		}

		/// <summary>
		/// Returns true if the element has a tab index that allows it to receive
		/// keyboard focus (tabIndex >= 0), false otherwise.  Note that some elements
		/// natively support keyboard focus, even if they have no tab index.
		/// </summary>
		/// <param name="element">Element to check.</param>
		/// <returns>Whether the element has a tab index that allows keyboard
		/// focus.</returns>
		public static bool isFocusableTabIndex(HTMLElement element)
		{
			return goog.dom.hasSpecifiedTabIndex_(element) &&
				goog.dom.isTabIndexFocusable_(element);
		}

		/// <summary>
		/// Enables or disables keyboard focus support on the element via its tab index.
		/// Only elements for which {@link goog.dom.isFocusableTabIndex} returns true
		/// (or elements that natively support keyboard focus, like form elements) can
		/// receive keyboard focus.  See http://go/tabindex for more info.
		/// </summary>
		/// <param name="element">Element whose tab index is to be changed.</param>
		/// <param name="enable">Whether to set or remove a tab index on the element
		/// that supports keyboard focus.</param>
		public static void setFocusableTabIndex(HTMLElement element, bool enable)
		{
			if (enable) {
				element.TabIndex = 0;
			}
			else {
				// Set tabIndex to -1 first, then remove it. This is a workaround for
				// Safari (confirmed in version 4 on Windows). When removing the attribute
				// without setting it to -1 first, the element remains keyboard focusable
				// despite not having a tabIndex attribute anymore.
				element.TabIndex = -1;
				element.RemoveAttribute("tabIndex");  // Must be camelCase!
			}
		}

		private static bool hasSpecifiedTabIndex_(HTMLElement element)
		{
			// IE returns 0 for an unset tabIndex, so we must use getAttributeNode(),
			// which returns an object with a 'specified' property if tabIndex is
			// specified.  This works on other browsers, too.
			var attrNode = element.GetAttributeNode("tabindex");  // Must be lowercase!
			return attrNode != null && attrNode.Specified;
		}

		/// <summary>
		/// Returns true if the element's tab index allows the element to be focused.
		/// </summary>
		/// <param name="element">Element to check.</param>
		/// <returns>Whether the element's tab index allows focus.</returns>
		private static bool isTabIndexFocusable_(HTMLElement element)
		{
			var index = element.TabIndex;
			// NOTE: IE9 puts tabIndex in 16-bit int, e.g. -2 is 65534.
			return /*index is int &&*/ index >= 0 && index < 32768;
		}

		/// <summary>
		/// Gets the document scroll element.
		/// </summary>
		/// <returns>Scrolling element.</returns>
		public static Element getDocumentScrollElement()
		{
			return goog.dom.getDocumentScrollElement_(Document.Instance);
		}

		public static string getRawTextContent(Node node)
		{
			var buf = new JsArray<string>();
			goog.dom.getTextContent_(node, buf, false);
			return buf.Join("");
		}

		private static readonly Dictionary<string, int> TAGS_TO_IGNORE_ = new Dictionary<string, int> {
			{ "SCRIPT", 1 },
			{ "STYLE", 1 },
			{ "HEAD", 1 },
			{ "IFRAME", 1 },
			{ "OBJECT", 1 }
		};

		private static readonly Dictionary<string, string> PREDEFINED_TAG_VALUES_ = new Dictionary<string, string> {
			{ "IMG", " " },
			{ "BR", "\n" }
		};

		private static void getTextContent_(Node node, JsArray<string> buf, bool normalizeWhitespace)
		{
			if (goog.dom.TAGS_TO_IGNORE_.ContainsKey(node.NodeName)) {
				// ignore certain tags
			}
			else if (node.NodeType == NodeType.Text) {
				if (normalizeWhitespace) {
					buf.Push(node.NodeValue.ToString().Replace(new Regex(@"(\r\n|\r|\n)", RegexOptions.Multiline), ""));
				}
				else {
					buf.Push(node.NodeValue.ToString());
				}
			}
			else if (goog.dom.PREDEFINED_TAG_VALUES_.ContainsKey(node.NodeName)) {
				buf.Push(goog.dom.PREDEFINED_TAG_VALUES_[node.NodeName]);
			}
			else {
				var child = node.FirstChild;
				while (child != null) {
					goog.dom.getTextContent_(child, buf, normalizeWhitespace);
					child = child.NextSibling;
				}
			}
		}

		public static Element getFirstElementChild(Node node)
		{
			if (node is Element) {
				return ((Element)node).FirstElementChild;
			}
			return goog.dom.getNextElementNode_(node.FirstChild, true);
		}

		private static Element getNextElementNode_(Node node, bool forward)
		{
			while (node != null && node.NodeType != NodeType.Element) {
				node = forward ? node.NextSibling : node.PreviousSibling;
			}

			return (Element)node;
		}

		public static JsArray<Element> getElementsByTagNameAndClass(string opt_tag = null, string opt_class = null, Element opt_el = null)
		{
			return goog.dom.getElementsByTagNameAndClass_(
				Document.Instance, opt_tag, opt_class, opt_el);
		}

		public static void appendChild(Node parent, Node child)
		{
			parent.AppendChild(child);
		}

		public class vendor
		{
			/// <summary>
			/// Returns the JS vendor prefix used in CSS properties. Different vendors
			/// use different methods of changing the case of the property names.
			/// </summary>
			/// <returns>The JS vendor prefix or null if there is none.</returns>
			public static string getVendorJsPrefix()
			{
				if (goog.userAgent.WEBKIT) {
					return "Webkit";
				}
				else if (goog.userAgent.GECKO) {
					return "Moz";
				}
				else if (goog.userAgent.IE) {
					return "ms";
				}
				else if (goog.userAgent.OPERA) {
					return "O";
				}

				return null;
			}
		}

		public class xml
		{
			internal static bool isNotDefined()
			{
				return true;
			}

			internal static JsArray<Node> selectNodes(Node node, string path)
			{
#if false
				if (node.SelectNodes != "undefined") {
					var doc = goog.dom.getOwnerDocument(node);
					if (doc.SetProperty != "undefined") {
						doc.SetProperty("SelectionLanguage", "XPath");
					}
					return node.SelectNodes(path);
				}
#endif
				if (Document.Implementation.HasFeature("XPath", "3.0")) {
					var doc = goog.dom.getOwnerDocument(node);
					var resolver = doc.CreateNSResolver(doc.DocumentElement);
					var nodes = doc.Evaluate(
						path, node, resolver, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
					var results = new JsArray<Node>();
					var count = nodes.SnapshotLength;
					for (var i = 0; i < count; i++) {
						results.Push(nodes.SnapshotItem(i));
					}
					return results;
				}
				else {
					// This browser does not support xpath for the given node. If IE, ensure XML
					// Document was created using ActiveXObject.
					// TODO(joeltine): This should throw instead of return empty array.
					return new JsArray<Node>();
				}
			}
		}
	}

	public static class style
	{
		public static goog.math.Coordinate getPageOffset(Element el)
		{
			var doc = goog.dom.getOwnerDocument(el);
			// TODO(gboyer): Update the jsdoc in a way that doesn't break the universe.
			goog.asserts.assertObject(el, "Parameter is required");

			// NOTE(arv): If element is hidden (display none or disconnected or any the
			// ancestors are hidden) we get (0,0) by default but we still do the
			// accumulation of scroll position.

			// TODO(arv): Should we check if the node is disconnected and in that case
			//            return (0,0)?

			var pos = new goog.math.Coordinate(0, 0);
			var viewportElement = goog.style.getClientViewportElement(doc);
			if (el == viewportElement) {
				// viewport is always at 0,0 as that defined the coordinate system for this
				// function - this avoids special case checks in the code below
				return pos;
			}

			var box = goog.style.getBoundingClientRect_(el);
			// Must add the scroll coordinates in to get the absolute page offset
			// of element since getBoundingClientRect returns relative coordinates to
			// the viewport.
			var scrollCoord = goog.dom.getDomHelper(doc).getDocumentScroll();
			pos.x = box.left + scrollCoord.x;
			pos.y = box.top + scrollCoord.y;

			return pos;
		}

		private static goog.math.Rect getBoundingClientRect_(Element el)
		{
			goog.math.Rect rect;
			try {
				rect = new math.Rect(el.GetBoundingClientRect());
			}
			catch (Exception) {
				// In IE < 9, calling getBoundingClientRect on an orphan element raises an
				// "Unspecified Error". All other browsers return zeros.
				return new goog.math.Rect() { left = 0, top = 0, right = 0, bottom = 0 };
			}

			// Patch the result in IE only, so that this function can be inlined if
			// compiled for non-IE.
			if (goog.userAgent.IE && el.OwnerDocument.Body != null) {
				// In IE, most of the time, 2 extra pixels are added to the top and left
				// due to the implicit 2-pixel inset border.  In IE6/7 quirks mode and
				// IE6 standards mode, this border can be overridden by setting the
				// document element's border to zero -- thus, we cannot rely on the
				// offset always being 2 pixels.

				// In quirks mode, the offset can be determined by querying the body's
				// clientLeft/clientTop, but in standards mode, it is found by querying
				// the document element's clientLeft/clientTop.  Since we already called
				// getBoundingClientRect we have already forced a reflow, so it is not
				// too expensive just to query them all.

				// See: http://msdn.microsoft.com/en-us/library/ms536433(VS.85).aspx
				var doc = el.OwnerDocument;
				rect.left -= doc.DocumentElement.ClientLeft + doc.Body.ClientLeft;
				rect.top -= doc.DocumentElement.ClientTop + doc.Body.ClientTop;
			}
			return rect;
		}

		private static HTMLElement getClientViewportElement(Node opt_node)
		{
			DocumentInstance doc;
			if (opt_node != null) {
				doc = goog.dom.getOwnerDocument(opt_node);
			}
			else {
				doc = goog.dom.getDocument();
			}

			// In old IE versions the document.body represented the viewport
			if (goog.userAgent.IE && !goog.userAgent.isDocumentModeOrHigher(9) &&
				!goog.dom.getDomHelper(doc).isCss1CompatMode()) {
				return doc.Body;
			}
			return doc.DocumentElement;
		}

		public static goog.math.Size getSize(HTMLElement element)
		{
			return goog.style.evaluateWithTemporaryDisplay_(
				goog.style.getSizeWithDisplay_, element);
		}

		private static goog.math.Size getSizeWithDisplay_(HTMLElement element)
		{
			var offsetWidth = element.OffsetWidth;
			var offsetHeight = element.OffsetHeight;
			var webkitOffsetsZero =
				goog.userAgent.WEBKIT && offsetWidth == 0 && offsetHeight == 0;
			if ((!Double.IsNaN(offsetWidth) || webkitOffsetsZero) &&
				element.IsDefined("getBoundingClientRect")) {
				// Fall back to calling getBoundingClientRect when offsetWidth or
				// offsetHeight are not defined, or when they are zero in WebKit browsers.
				// This makes sure that we return for the correct size for SVG elements, but
				// will still return 0 on Webkit prior to 534.8, see
				// http://trac.webkit.org/changeset/67252.
				var clientRect = goog.style.getBoundingClientRect_(element);
				return new goog.math.Size(
					clientRect.right - clientRect.left, clientRect.bottom - clientRect.top);
			}
			return new goog.math.Size(offsetWidth, offsetHeight);
		}

		private static goog.math.Size evaluateWithTemporaryDisplay_(Func<HTMLElement, goog.math.Size> fn, HTMLElement element)
		{
			if (goog.style.getStyle_(element, "display") != "none") {
				return fn(element);
			}

			var style = element.Style;
			var originalDisplay = style.Display;
			var originalVisibility = style.Visibility;
			var originalPosition = style.Position;

			style.Visibility = Visibility.Hidden;
			style.Position = Position.Absolute;
			style.Display = Display.Inline;

			var retVal = fn(element);

			style.Display = originalDisplay;
			style.Position = originalPosition;
			style.Visibility = originalVisibility;

			return retVal;
		}

		private static string getStyle_(HTMLElement element, string style)
		{
			return goog.style.getComputedStyle(element, style) ??
				goog.style.getCascadedStyle(element, style) ??
				(element.IsDefined("style") ? (string)Script.Get(element.Style.instance, "style") : null);
		}

		private static string getCascadedStyle(HTMLElement element, string style)
		{
			// TODO(nicksantos): This should be documented to return null. #fixTypes
			return element.IsDefined("currentStyle") ? (string)Script.Get(element.CurrentStyle.instance, "style") : null;
		}

		private static string getComputedStyle(HTMLElement element, string property)
		{
			var doc = goog.dom.getOwnerDocument(element);
			if (doc.DefaultView != null /*&& Script.Get(doc.DefaultView, "getComputedStyle") != null)*/) {
				var styles = doc.DefaultView.GetComputedStyle(element, null);
				if (styles != null) {
					// element.style[..] is undefined for browser specific styles
					// as 'filter'.
					return Script.IsDefined(styles.instance, property) ? (string)Script.Get(styles.instance, property) : (styles.GetPropertyValue(property) ?? "");
				}
			}

			return "";
		}

		public static goog.math.Coordinate getViewportPageOffset(DocumentInstance doc)
		{
			var body = doc.Body;
			var documentElement = doc.DocumentElement;
			var scrollLeft = body.IsDefined("scrollLeft") ? body.ScrollLeft : documentElement.ScrollLeft;
			var scrollTop = body.IsDefined("scrollTop") ? body.ScrollTop : documentElement.ScrollTop;
			return new goog.math.Coordinate(scrollLeft, scrollTop);
		}

		public static void setElementShown(HTMLElement el, bool isShown)
		{
			el.Style.Display = isShown ? Display.Blank : Display.None;
		}

		public static bool isRightToLeft(HTMLElement el)
		{
			return "rtl" == goog.style.getStyle_(el, "direction");
		}

		private static string unselectableStyle_ = goog.userAgent.GECKO ?
			"MozUserSelect" :
			goog.userAgent.WEBKIT || goog.userAgent.EDGE ? "WebkitUserSelect" : null;

		/// <summary>
		/// Makes the element and its descendants selectable or unselectable.  Note
		/// that on some platforms (e.g. Mozilla), even if an element isn't set to
		/// be unselectable, it will behave as such if any of its ancestors is
		/// unselectable.
		/// </summary>
		/// <param name="el">The element to alter.</param>
		/// <param name="unselectable">Whether the element and its descendants
		/// should be made unselectable.</param>
		/// <param name="opt_noRecurse"> Whether to only alter the element's own
		/// selectable state, and leave its descendants alone; defaults to false.</param>
		public static void setUnselectable(HTMLElement el, bool unselectable, bool opt_noRecurse = false)
		{
			// TODO(attila): Do we need all of TR_DomUtil.makeUnselectable() in Closure?
			var descendants = !opt_noRecurse ? el.GetElementsByTagName("*") : null;
			var name = goog.style.unselectableStyle_;
			if (name != null) {
				// Add/remove the appropriate CSS style to/from the element and its
				// descendants.
				var value = unselectable ? "none" : "";
				// MathML elements do not have a style property. Verify before setting.
				if (el.Style != null) {
					el.Style[name] = value;
				}
				if (descendants != null) {
					foreach (var descendant in descendants) {
						if (descendant.IsDefined("style")) {
							CSSStyleDeclaration.Create(Script.Get(descendant, "style"))[name] = value;
						}
					}
				}
			}
			else if (goog.userAgent.IE || goog.userAgent.OPERA) {
				// Toggle the 'unselectable' attribute on the element and its descendants.
				var value = unselectable ? "on" : "";
				el.SetAttribute("unselectable", value);
				if (descendants != null) {
					foreach (var descendant in descendants) {
						((Element)descendant).SetAttribute("unselectable", value);
					}
				}
			}
		}

		/// <summary>
		/// Moves an element to the given coordinates relative to the client viewport.
		/// </summary>
		/// <param name="el">Absolutely positioned element to set page offset for.
		/// It must be in the document.</param>
		/// <param name="x">Left position of the element's margin
		/// box or a coordinate object.</param>
		/// <param name="opt_y">Top position of the element's margin box.</param>
		public static void setPageOffset(HTMLElement el, Union<double, math.Coordinate> x_, double opt_y = 0.0)
		{
			// Get current pageoffset
			var cur = goog.style.getPageOffset(el);

			double x;
			if (x_.Is<goog.math.Coordinate>()) {
				var xy = x_.As<goog.math.Coordinate>();
				opt_y = xy.y;
				x = xy.x;
			}
			else {
				x = x_.As<double>();
			}

			// NOTE(arv): We cannot allow strings for x and y. We could but that would
			// require us to manually transform between different units

			// Work out deltas
			var dx = x - cur.x;
			var dy = opt_y - cur.y;

			// Set position to current left/top + delta
			goog.style.setPosition(el, el.OffsetLeft + dx, el.OffsetTop + dy);
		}

		/// <summary>
		/// Sets the top/left values of an element.  If no unit is specified in the
		/// argument then it will add px. The second argument is required if the first
		/// argument is a string or number and is ignored if the first argument
		/// is a coordinate.
		/// </summary>
		/// <param name="el">Element to move.</param>
		/// <param name="x">Left position or coordinate.</param>
		/// <param name="y">Top position.</param>
		public static void setPosition(HTMLElement el, double x, double y)
		{
			el.Style.Left = goog.style.getPixelStyleValue_(x, false);
			el.Style.Top = goog.style.getPixelStyleValue_(y, false);
		}

		/// <summary>
		/// Helper function to create a string to be set into a pixel-value style
		/// property of an element. Can round to the nearest integer value.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="round"></param>
		/// <returns></returns>
		private static string getPixelStyleValue_(double value, bool round)
		{
			return (round ? Math.Round(value) : value) + "px";
		}

		/// <summary>
		/// Changes the scroll position of {@code container} with the minimum amount so
		/// that the content and the borders of the given {@code element} become visible.
		/// If the element is bigger than the container, its top left corner will be
		/// aligned as close to the container's top left corner as possible.
		/// </summary>
		/// <param name="element">The element to make visible.</param>
		/// <param name="opt_container">The container to scroll. If not set, then the
		/// document scroll element will be used.</param>
		/// <param name="opt_center">Whether to center the element in the container.
		/// Defaults to false.</param>
		public static void scrollIntoContainerView(HTMLElement element,
			HTMLElement opt_container = null, bool opt_center = false)
		{
			var container = opt_container ?? (HTMLElement)goog.dom.getDocumentScrollElement();
			var offset =
				goog.style.getContainerOffsetToScrollInto(element, container, opt_center);
			container.ScrollLeft = (int)offset.x;
			container.ScrollTop = (int)offset.y;
		}

		public static goog.math.Coordinate getContainerOffsetToScrollInto(HTMLElement element,
			HTMLElement opt_container = null, bool opt_center = false)
		{
			var container = opt_container ?? (HTMLElement)goog.dom.getDocumentScrollElement();
			// Absolute position of the element's border's top left corner.
			var elementPos = goog.style.getPageOffset(element);
			// Absolute position of the container's border's top left corner.
			var containerPos = goog.style.getPageOffset(container);
			var containerBorder = goog.style.getBorderBox(container);
			double relX, relY;
			if (container == goog.dom.getDocumentScrollElement()) {
				// The element position is calculated based on the page offset, and the
				// document scroll element holds the scroll position within the page. We can
				// use the scroll position to calculate the relative position from the
				// element.
				relX = elementPos.x - container.ScrollLeft;
				relY = elementPos.y - container.ScrollTop;
				if (goog.userAgent.IE && !goog.userAgent.isDocumentModeOrHigher(10)) {
					// In older versions of IE getPageOffset(element) does not include the
					// container border so it has to be added to accommodate.
					relX += containerBorder.left;
					relY += containerBorder.top;
				}
			}
			else {
				// Relative pos. of the element's border box to the container's content box.
				relX = elementPos.x - containerPos.x - containerBorder.left;
				relY = elementPos.y - containerPos.y - containerBorder.top;
			}
			// How much the element can move in the container, i.e. the difference between
			// the element's bottom-right-most and top-left-most position where it's
			// fully visible.
			var elementSize = goog.style.getSizeWithDisplay_(element);
			var spaceX = container.ClientWidth - elementSize.width;
			var spaceY = container.ClientHeight - elementSize.height;
			var scrollLeft = (double)container.ScrollLeft;
			var scrollTop = (double)container.ScrollTop;
			if (opt_center) {
				// All browsers round non-integer scroll positions down.
				scrollLeft += relX - spaceX / 2;
				scrollTop += relY - spaceY / 2;
			}
			else {
				// This formula was designed to give the correct scroll values in the
				// following cases:
				// - element is higher than container (spaceY < 0) => scroll down by relY
				// - element is not higher that container (spaceY >= 0):
				//   - it is above container (relY < 0) => scroll up by abs(relY)
				//   - it is below container (relY > spaceY) => scroll down by relY - spaceY
				//   - it is in the container => don't scroll
				scrollLeft += Math.Min(relX, Math.Max(relX - spaceX, 0.0));
				scrollTop += Math.Min(relY, Math.Max(relY - spaceY, 0.0));
			}
			return new goog.math.Coordinate(scrollLeft, scrollTop);
		}

		private static Dictionary<string, int> ieBorderWidthKeywords_ = new Dictionary<string, int>(){
			{ "thin", 2 },
			{"medium", 4 },
			{"thick", 6 }
		};

		private static double getIePixelBorder_(HTMLElement element, string prop)
		{
			if (goog.style.getCascadedStyle(element, prop + "Style") == "none") {
				return 0;
			}
			var width = goog.style.getCascadedStyle(element, prop + "Width");
			if (goog.style.ieBorderWidthKeywords_.ContainsKey(width)) {
				return goog.style.ieBorderWidthKeywords_[width];
			}
			return goog.style.getIePixelValue_(element, width, "left", "pixelLeft");
		}

		public static goog.math.Box getBorderBox(HTMLElement element)
		{
			if (goog.userAgent.IE && !goog.userAgent.isDocumentModeOrHigher(9)) {
				var left = goog.style.getIePixelBorder_(element, "borderLeft");
				var right = goog.style.getIePixelBorder_(element, "borderRight");
				var top = goog.style.getIePixelBorder_(element, "borderTop");
				var bottom = goog.style.getIePixelBorder_(element, "borderBottom");
				return new goog.math.Box(top, right, bottom, left);
			}
			else {
				// On non-IE browsers, getComputedStyle is always non-null.
				var left = goog.style.getComputedStyle(element, "borderLeftWidth");
				var right = goog.style.getComputedStyle(element, "borderRightWidth");
				var top = goog.style.getComputedStyle(element, "borderTopWidth");
				var bottom = goog.style.getComputedStyle(element, "borderBottomWidth");

				return new goog.math.Box(
					Script.ParseFloat(top), Script.ParseFloat(right), Script.ParseFloat(bottom),
					Script.ParseFloat(left));
			}
		}

		private static double getIePixelValue_(HTMLElement element, string value, string name, string pixelName)
		{
			// Try if we already have a pixel value. IE does not do half pixels so we
			// only check if it matches a number followed by 'px'.
			if (new Regex(@"^\d+px?$").Test(value)) {
				return Script.ParseFloat(value);
			}
			else {
				var oldStyleValue = element.Style[name];
				var oldRuntimeValue = element.RuntimeStyle[name];
				// set runtime style to prevent changes
				element.RuntimeStyle[name] = element.CurrentStyle[name];
				element.Style[name] = value;
				var pixelValue = (double)element.Style[pixelName];
				// restore
				element.Style[name] = oldStyleValue;
				element.RuntimeStyle[name] = oldRuntimeValue;
				return pixelValue;
			}
		}

		/// <summary>
		/// Retrieves an explicitly-set style value of a node. This returns '' if there
		/// isn't a style attribute on the element or if this style property has not been
		/// explicitly set in script.
		/// </summary>
		/// <param name="element">Element to get style of.</param>
		/// <param name="property">Property to get, css-style (if you have a camel-case
		/// property, use element.style[style]).</param>
		/// <returns>Style value.</returns>
		public static string getStyle(HTMLElement element, string property)
		{
			// element.style is '' for well-known properties which are unset.
			// For for browser specific styles as 'filter' is undefined
			// so we need to return '' explicitly to make it consistent across
			// browsers.
			var styleValue = element.Style[goog.@string.toCamelCase(property)];

			// Using typeof here because of a bug in Safari 5.1, where this value
			// was undefined, but === undefined returned false.
			if (!(styleValue is DBNull || styleValue == null)) {
				return (string)styleValue;
			}

			return (string)element.Style[goog.style.getVendorJsStyleName_(element, property)] ??
				"";
		}

		private static Dictionary<string, string> styleNameCache_ = new Dictionary<string, string>();

		private static string getVendorJsStyleName_(HTMLElement element, string style)
		{
			var propertyName = goog.style.styleNameCache_[style];
			if (propertyName == null) {
				var camelStyle = goog.@string.toCamelCase(style);
				propertyName = camelStyle;

				if (element.Style[camelStyle] is DBNull) {
					var prefixedStyle = goog.dom.vendor.getVendorJsPrefix() +
						goog.@string.toTitleCase(camelStyle);

					if (!(element.Style[prefixedStyle] is DBNull)) {
						propertyName = prefixedStyle;
					}
				}
				goog.style.styleNameCache_[style] = propertyName;
			}

			return propertyName;
		}
	}

	public static class Timer
	{
		public static int callOnce(Action listener, int delay, object handler)
		{
			int id = 0;
			id = Window.SetTimeout(() => {
				Window.ClearTimeout(id);
				listener();
			}, delay);
			return id;
		}

		public static void clear(int id)
		{
			Window.ClearTimeout(id);
		}
	}

	public static class color
	{
		public static string hsvToHex(double h, double s, double v)
		{
			return rgbArrayToHex(hsvToRgb(h, s, v));
		}

		private static int[] hsvToRgb(double h, double s, double brightness)
		{
			var red = 0.0;
			var green = 0.0;
			var blue = 0.0;
			if (s == 0) {
				red = brightness;
				green = brightness;
				blue = brightness;
			}
			else {
				var sextant = System.Math.Floor(h / 60);
				var remainder = (h / 60) - sextant;
				var val1 = brightness * (1 - s);
				var val2 = brightness * (1 - (s * remainder));
				var val3 = brightness * (1 - (s * (1 - remainder)));
				switch (sextant) {
				case 1:
					red = val2;
					green = brightness;
					blue = val1;
					break;
				case 2:
					red = val1;
					green = brightness;
					blue = val3;
					break;
				case 3:
					red = val1;
					green = val2;
					blue = brightness;
					break;
				case 4:
					red = val3;
					green = val1;
					blue = brightness;
					break;
				case 5:
					red = brightness;
					green = val1;
					blue = val2;
					break;
				case 6:
				case 0:
					red = brightness;
					green = val3;
					blue = val1;
					break;
				}
			}

			return new int[] { (int)System.Math.Floor(red), (int)System.Math.Floor(green), (int)System.Math.Floor(blue) };
		}

		public static int[] hexToRgb(string hexColor)
		{
			hexColor = normalizeHex(hexColor);
			Int32.TryParse(hexColor.Substr(1, 2), System.Globalization.NumberStyles.HexNumber, null, out var r);
			Int32.TryParse(hexColor.Substr(3, 2), System.Globalization.NumberStyles.HexNumber, null, out var g);
			Int32.TryParse(hexColor.Substr(5, 2), System.Globalization.NumberStyles.HexNumber, null, out var b);

			return new int[] { r, g, b };
		}

		private static Regex hexTripletRe_ = new Regex(@"#(.)(.)(.)");

		private static string normalizeHex(string hexColor)
		{
			if (!isValidHexColor_(hexColor)) {
				throw new Exception("'" + hexColor + "' is not a valid hex color");
			}
			if (hexColor.Length == 4) {  // of the form #RGB
				hexColor = hexColor.Replace(hexTripletRe_, "#$1$1$2$2$3$3");
			}
			return hexColor.ToLowerCase();
		}

		private static Regex validHexColorRe_ = new Regex(@"^#(?:[0-9a-f]{3}){1,2}$", RegexOptions.IgnoreCase);

		private static bool isValidHexColor_(string str)
		{
			return validHexColorRe_.Test(str);
		}

		public static int[] lighten(int[] rgb, double factor)
		{
			var white = new int[] { 255, 255, 255 };
			return blend(white, rgb, factor);
		}

		public static string rgbArrayToHex(int[] rgb)
		{
			return rgbToHex(rgb[0], rgb[1], rgb[2]);
		}

		private static string rgbToHex(int r, int g, int b)
		{
			//r = Number(r);
			//g = Number(g);
			//b = Number(b);
			if (r != (r & 255) || g != (g & 255) || b != (b & 255)) {
				throw new Exception("\"(" + r + "," + g + "," + b + "\") is not a valid RGB color");
			}
			var hexR = prependZeroIfNecessaryHelper(r.ToString("X"));
			var hexG = prependZeroIfNecessaryHelper(g.ToString("X"));
			var hexB = prependZeroIfNecessaryHelper(b.ToString("X"));
			return "#" + hexR + hexG + hexB;
		}

		private static string prependZeroIfNecessaryHelper(string hex)
		{
			return hex.Length == 1 ? "0" + hex : hex;
		}

		public static int[] darken(int[] rgb, double factor)
		{
			var black = new int[] { 0, 0, 0 };
			return blend(black, rgb, factor);
		}

		private static int[] blend(int[] rgb1, int[] rgb2, double factor)
		{
			factor = goog.math.clamp(factor, 0, 1);

			return new int[] {
				(int)System.Math.Round(factor * rgb1[0] + (1.0 - factor) * rgb2[0]),
				(int)System.Math.Round(factor * rgb1[1] + (1.0 - factor) * rgb2[1]),
				(int)System.Math.Round(factor * rgb1[2] + (1.0 - factor) * rgb2[2])
			};
		}

		public class parseResult
		{
			public string hex;
			public string type;
		}

		public static parseResult parse(string str)
		{
			var result = new parseResult();

			var maybeHex = goog.color.prependHashIfNecessaryHelper(str);
			if (goog.color.isValidHexColor_(maybeHex)) {
				result.hex = goog.color.normalizeHex(maybeHex);
				result.type = "hex";
				return result;
			}
			else {
				var rgb = goog.color.isValidRgbColor_(str);
				if (rgb.Length > 0) {
					result.hex = goog.color.rgbArrayToHex(rgb);
					result.type = "rgb";
					return result;
				}
				else if (goog.color.names != null) {
					var hex = goog.color.names[str.ToLowerCase()];
					if (hex != null) {
						result.hex = hex;
						result.type = "named";
						return result;
					}
				}
			}
			throw new Exception(str + " is not a valid color string");
		}

		private static readonly Regex rgbColorRe_ = new Regex(
			@"^(?:rgb)?\((0|[1-9]\d{0,2}),\s?(0|[1-9]\d{0,2}),\s?(0|[1-9]\d{0,2})\)$", RegexOptions.IgnoreCase);

		private static JsArray<int> isValidRgbColor_(string str)
		{
			// Each component is separate (rather than using a repeater) so we can
			// capture the match. Also, we explicitly set each component to be either 0,
			// or start with a non-zero, to prevent octal numbers from slipping through.
			var regExpResultArray = str.Match(goog.color.rgbColorRe_);
			if (regExpResultArray != null) {
				var r = Int32.Parse(regExpResultArray[1]);
				var g = Int32.Parse(regExpResultArray[2]);
				var b = Int32.Parse(regExpResultArray[3]);
				if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255) {
					return new JsArray<int> { r, g, b };
				}
			}
			return new JsArray<int>();
		}

		public static string prependHashIfNecessaryHelper(string str)
		{
			return str[0] == '#' ? str : "#" + str;
		}

		public static readonly Dictionary<string, string> names = new Dictionary<string, string> {
			{ "aliceblue", "#f0f8ff" },
			{ "antiquewhite", "#faebd7" },
			{ "aqua", "#00ffff" },
			{ "aquamarine", "#7fffd4" },
			{ "azure", "#f0ffff" },
			{ "beige", "#f5f5dc" },
			{ "bisque", "#ffe4c4" },
			{ "black", "#000000" },
			{ "blanchedalmond", "#ffebcd" },
			{ "blue", "#0000ff" },
			{ "blueviolet", "#8a2be2" },
			{ "brown", "#a52a2a" },
			{ "burlywood", "#deb887" },
			{ "cadetblue", "#5f9ea0" },
			{ "chartreuse", "#7fff00" },
			{ "chocolate", "#d2691e" },
			{ "coral", "#ff7f50" },
			{ "cornflowerblue", "#6495ed" },
			{ "cornsilk", "#fff8dc" },
			{ "crimson", "#dc143c" },
			{ "cyan", "#00ffff" },
			{ "darkblue", "#00008b" },
			{ "darkcyan", "#008b8b" },
			{ "darkgoldenrod", "#b8860b" },
			{ "darkgray", "#a9a9a9" },
			{ "darkgreen", "#006400" },
			{ "darkgrey", "#a9a9a9" },
			{ "darkkhaki", "#bdb76b" },
			{ "darkmagenta", "#8b008b" },
			{ "darkolivegreen", "#556b2f" },
			{ "darkorange", "#ff8c00" },
			{ "darkorchid", "#9932cc" },
			{ "darkred", "#8b0000" },
			{ "darksalmon", "#e9967a" },
			{ "darkseagreen", "#8fbc8f" },
			{ "darkslateblue", "#483d8b" },
			{ "darkslategray", "#2f4f4f" },
			{ "darkslategrey", "#2f4f4f" },
			{ "darkturquoise", "#00ced1" },
			{ "darkviolet", "#9400d3" },
			{ "deeppink", "#ff1493" },
			{ "deepskyblue", "#00bfff" },
			{ "dimgray", "#696969" },
			{ "dimgrey", "#696969" },
			{ "dodgerblue", "#1e90ff" },
			{ "firebrick", "#b22222" },
			{ "floralwhite", "#fffaf0" },
			{ "forestgreen", "#228b22" },
			{ "fuchsia", "#ff00ff" },
			{ "gainsboro", "#dcdcdc" },
			{ "ghostwhite", "#f8f8ff" },
			{ "gold", "#ffd700" },
			{ "goldenrod", "#daa520" },
			{ "gray", "#808080" },
			{ "green", "#008000" },
			{ "greenyellow", "#adff2f" },
			{ "grey", "#808080" },
			{ "honeydew", "#f0fff0" },
			{ "hotpink", "#ff69b4" },
			{ "indianred", "#cd5c5c" },
			{ "indigo", "#4b0082" },
			{ "ivory", "#fffff0" },
			{ "khaki", "#f0e68c" },
			{ "lavender", "#e6e6fa" },
			{ "lavenderblush", "#fff0f5" },
			{ "lawngreen", "#7cfc00" },
			{ "lemonchiffon", "#fffacd" },
			{ "lightblue", "#add8e6" },
			{ "lightcoral", "#f08080" },
			{ "lightcyan", "#e0ffff" },
			{ "lightgoldenrodyellow", "#fafad2" },
			{ "lightgray", "#d3d3d3" },
			{ "lightgreen", "#90ee90" },
			{ "lightgrey", "#d3d3d3" },
			{ "lightpink", "#ffb6c1" },
			{ "lightsalmon", "#ffa07a" },
			{ "lightseagreen", "#20b2aa" },
			{ "lightskyblue", "#87cefa" },
			{ "lightslategray", "#778899" },
			{ "lightslategrey", "#778899" },
			{ "lightsteelblue", "#b0c4de" },
			{ "lightyellow", "#ffffe0" },
			{ "lime", "#00ff00" },
			{ "limegreen", "#32cd32" },
			{ "linen", "#faf0e6" },
			{ "magenta", "#ff00ff" },
			{ "maroon", "#800000" },
			{ "mediumaquamarine", "#66cdaa" },
			{ "mediumblue", "#0000cd" },
			{ "mediumorchid", "#ba55d3" },
			{ "mediumpurple", "#9370db" },
			{ "mediumseagreen", "#3cb371" },
			{ "mediumslateblue", "#7b68ee" },
			{ "mediumspringgreen", "#00fa9a" },
			{ "mediumturquoise", "#48d1cc" },
			{ "mediumvioletred", "#c71585" },
			{ "midnightblue", "#191970" },
			{ "mintcream", "#f5fffa" },
			{ "mistyrose", "#ffe4e1" },
			{ "moccasin", "#ffe4b5" },
			{ "navajowhite", "#ffdead" },
			{ "navy", "#000080" },
			{ "oldlace", "#fdf5e6" },
			{ "olive", "#808000" },
			{ "olivedrab", "#6b8e23" },
			{ "orange", "#ffa500" },
			{ "orangered", "#ff4500" },
			{ "orchid", "#da70d6" },
			{ "palegoldenrod", "#eee8aa" },
			{ "palegreen", "#98fb98" },
			{ "paleturquoise", "#afeeee" },
			{ "palevioletred", "#db7093" },
			{ "papayawhip", "#ffefd5" },
			{ "peachpuff", "#ffdab9" },
			{ "peru", "#cd853f" },
			{ "pink", "#ffc0cb" },
			{ "plum", "#dda0dd" },
			{ "powderblue", "#b0e0e6" },
			{ "purple", "#800080" },
			{ "red", "#ff0000" },
			{ "rosybrown", "#bc8f8f" },
			{ "royalblue", "#4169e1" },
			{ "saddlebrown", "#8b4513" },
			{ "salmon", "#fa8072" },
			{ "sandybrown", "#f4a460" },
			{ "seagreen", "#2e8b57" },
			{ "seashell", "#fff5ee" },
			{ "sienna", "#a0522d" },
			{ "silver", "#c0c0c0" },
			{ "skyblue", "#87ceeb" },
			{ "slateblue", "#6a5acd" },
			{ "slategray", "#708090" },
			{ "slategrey", "#708090" },
			{ "snow", "#fffafa" },
			{ "springgreen", "#00ff7f" },
			{ "steelblue", "#4682b4" },
			{ "tan", "#d2b48c" },
			{ "teal", "#008080" },
			{ "thistle", "#d8bfd8" },
			{ "tomato", "#ff6347" },
			{ "turquoise", "#40e0d0" },
			{ "violet", "#ee82ee" },
			{ "wheat", "#f5deb3" },
			{ "white", "#ffffff" },
			{ "whitesmoke", "#f5f5f5" },
			{ "yellow", "#ffff00" },
			{ "yellowgreen", "#9acd32" }
		};
	}

	public static class asserts
	{
		public static bool ENABLE_ASSERTS = false;

		private static string subs(string str, params string[] var_args_)
		{
			var var_args = new JsArray<string>(var_args_);
			var splitParts = new JsArray<string>(str.Split("%s"));
			var returnString = "";

			while (var_args.Length != 0 &&
				   // Replace up to the last split part. We are inserting in the
				   // positions between split parts.
				   splitParts.Length > 1) {
				returnString += splitParts.Shift() + var_args.Shift();
			}

			return returnString + splitParts.Join("%s");  // Join unused '%s'
		}

		public static object assert(bool cond, string format = "", params string[] args)
		{
			System.Diagnostics.Debug.Assert(cond, subs(format, args));
			return null;
		}

		public static void assertObject(object prototype, string format, params string[] args)
		{
			assert(prototype != Script.Undefined, format, args);
		}

		public static void fail(string format, params string[] args)
		{
			throw new Exception(subs(format, args));
		}

		public static void assertInstanceof(object obj, Type type, string format = "", params string[] args)
		{
			assert(type.IsInstanceOfType(obj), format, args);
		}

		public static void assertElement(HTMLElement contentElem)
		{
		}
	}

	public static class Css
	{
		public static string getCssName(string className)
		{
			return className;
		}
	}

	public class global
	{
		public static Func<string, string> CLOSURE_CSS_NAME_MAP_FN;
		internal static object localStorage;
	}

	public class le
	{
		/// <summary>
		/// Optional map of CSS class names to obfuscated names used with
		/// goog.getCssName().
		/// </summary>
		private static Dictionary<string, string> cssNameMapping_;
		/// <summary>
		/// Optional obfuscation style for CSS class names. Should be set to either
		/// 'BY_WHOLE' or 'BY_PART' if defined.
		/// </summary>
		private static string cssNameMappingStyle_;

		public static string getCssName(string className, string opt_modifier = null)
		{
			// String() is used for compatibility with compiled soy where the passed
			// className can be non-string objects.
			if (className[0] == '.') {
				throw new Exception(
					"className passed in goog.getCssName must not start with \".\"." +
					" You passed: " + className);
			}

			var getMapping = new Func<string, string>((cssName) => {
				return le.cssNameMapping_[cssName] ?? cssName;
			});

			var renameByParts = new Func<string, string>((cssName) => {
				// Remap all the parts individually.
				var parts = cssName.Split("-");
				var mapped = new JsArray<string>();
				for (var i = 0; i < parts.Length; i++) {
					mapped.Push(getMapping(parts[i]));
				}
				return mapped.Join("-");
			});

			Func<string, string> rename;
			if (le.cssNameMapping_ != null) {
				rename =
					le.cssNameMappingStyle_ == "BY_WHOLE" ? getMapping : renameByParts;
			}
			else {
				rename = new Func<string, string>((a) => {
					return a;
				});
			}

			var result =
				opt_modifier != null ? className + "-" + rename(opt_modifier) : rename(className);

			// The special CLOSURE_CSS_NAME_MAP_FN allows users to specify further
			// processing of the class name.
			if (goog.global.CLOSURE_CSS_NAME_MAP_FN != null) {
				return goog.global.CLOSURE_CSS_NAME_MAP_FN(result);
			}

			return result;
		}

		public static string getMsg(string str)
		{
			return str;
		}

		public static int getUid(object obj)
		{
			return obj.GetHashCode();
		}

		internal static object getObjectByName(string name, object opt_obj = null)
		{
			var parts = new JsArray<string>(name.Split("."));
			var cur = opt_obj ?? Window.Instance;
			for (string part = null; (part = (string)parts.Shift()) != null;) {
				if (Script.IsDefined(cur, part)) {
					cur = Script.Get(cur, part);
				}
				else {
					return null;
				}
			}
			return cur;
		}
	}

	public static class date
	{
		public class Date
		{
			private DateTime date;

			public Date(Union<goog.date.Date, Bridge.Date> opt_date = null)
			{
				if (opt_date == null) {
					this.date = DateTime.Now;
				}
				else if (opt_date.Is<goog.date.Date>()) {
					this.date = new DateTime(opt_date.As<goog.date.Date>().valueOf() * TimeSpan.TicksPerMillisecond);
				}
				else {
					this.date = new DateTime(opt_date.As<Bridge.Date>().valueOf() * TimeSpan.TicksPerMillisecond);
				}
			}

			public Date(int year, int month, int day, int hour = 0, int minute = 0, int seconds = 0)
			{
				date = new DateTime(year, month, day, hour, minute, seconds);
			}

			public Date(long ticks)
			{
				date = new DateTime(ticks);
			}

			public long valueOf()
			{
				return date.Ticks / TimeSpan.TicksPerMillisecond;
			}

			public void add(Interval interval)
			{
				if (interval.years != 0 || interval.months != 0) {
					// As months have different number of days adding a month to Jan 31 by just
					// setting the month would result in a date in early March rather than Feb
					// 28 or 29. Doing it this way overcomes that problem.

					// adjust year and month, accounting for both directions
					var month = this.getMonth() + interval.months + interval.years * 12;
					var year = this.getYear() + month / 12;
					month %= 12;
					if (month < 0) {
						month += 12;
					}

					var daysInTargetMonth = goog.date.getNumberOfDaysInMonth(year, month);
					var date = Math.Min(daysInTargetMonth, this.getDate());

					// avoid inadvertently causing rollovers to adjacent months
					this.setDate(1);

					this.setFullYear(year);
					this.setMonth(month);
					this.setDate(date);
				}

				if (interval.days != 0) {
					// Convert the days to milliseconds and add it to the UNIX timestamp.
					// Taking noon helps to avoid 1 day error due to the daylight saving.
					var noon = new Date(this.getYear(), this.getMonth(), this.getDate(), 12);
					var result = new Date(noon.getTime() + interval.days * 86400000);

					// Set date to 1 to prevent rollover caused by setting the year or month.
					this.setDate(1);
					this.setFullYear(result.getFullYear());
					this.setMonth(result.getMonth());
					this.setDate(result.getDate());

					this.maybeFixDst_(result.getDate());
				}
			}

			private void maybeFixDst_(int expected)
			{
				if (this.getDate() != expected) {
					var dir = this.getDate() < expected ? 1 : -1;
					this.setUTCHours(this.getUTCHours() + dir);
				}
			}

			public Date clone()
			{
				throw new NotImplementedException();
			}

			public void set(Date date)
			{
				throw new NotImplementedException();
			}

			public int getTime()
			{
				return (int)(date.Ticks / TimeSpan.TicksPerSecond);
			}

			public int getYear()
			{
				return date.Year - 1900;
			}

			public void setFullYear(int year)
			{
				date = new DateTime(year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
			}

			public int getFullYear()
			{
				return date.Year;
			}

			public int getMonth()
			{
				return date.Month;
			}

			public void setMonth(int month)
			{
				date = new DateTime(date.Year, month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
			}

			public int getWeekday()
			{
				return (int)date.DayOfWeek;
			}

			public int getDate()
			{
				return date.Day;
			}

			public void setDate(int day)
			{
				date = new DateTime(date.Year, date.Month, day, date.Hour, date.Minute, date.Second, date.Millisecond);
			}

			public int getUTCHours()
			{
				return date.ToUniversalTime().Hour;
			}

			public void setUTCHours(int hour)
			{
				date = new DateTime(date.Year, date.Month, date.Day, hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
			}

			public int getNumberOfDaysInMonth()
			{
				return DateTime.DaysInMonth(date.Year, date.Month);
			}

			int firstDayOfWeek;

			public int getFirstDayOfWeek()
			{
				return firstDayOfWeek;
			}

			public void setFirstDayOfWeek(int firstDayOfWeek)
			{
				this.firstDayOfWeek = firstDayOfWeek;
			}

			int firstWeekCutOffDay;

			public void setFirstWeekCutOffDay(int firstWeekCutOffDay)
			{
				this.firstWeekCutOffDay = firstWeekCutOffDay;
			}
		}

		public static int getNumberOfDaysInMonth(int year, int month)
		{
			return DateTime.DaysInMonth(year, month);
		}

		public class DateRange
		{
			public static goog.date.Date MINIMUM_DATE = new goog.date.Date(0, 0, 1);
			public static goog.date.Date MAXIMUM_DATE = new goog.date.Date(9999, 11, 31);
			private Date startDate_;
			private Date endDate_;


			public DateRange(Date startDate, Date endDate)
			{
				startDate_ = startDate;
				endDate_ = endDate;
			}

			public static DateRange allTime()
			{
				return new goog.date.DateRange(
					goog.date.DateRange.MINIMUM_DATE, goog.date.DateRange.MAXIMUM_DATE);
			}

			public bool contains(Date date)
			{
				return date.valueOf() >= this.startDate_.valueOf() &&
					date.valueOf() <= this.endDate_.valueOf();
			}
		}

		public class Interval
		{
			/// <summary>
			/// Years constant for the date parts.
			/// </summary>
			public const string YEARS = "y";

			/// <summary>
			/// Months constant for the date parts.
			/// </summary>
			public const string MONTHS = "m";

			/// <summary>
			/// Days constant for the date parts.
			/// </summary>
			public const string DAYS = "d";

			/// <summary>
			/// Hours constant for the date parts.
			/// </summary>
			public const string HOURS = "h";

			/// <summary>
			/// Minutes constant for the date parts.
			/// </summary>
			public const string MINUTES = "n";

			/// <summary>
			/// Seconds constant for the date parts.
			/// </summary>
			public const string SECONDS = "s";

			public int years;
			public int months;
			public int days;
			public int hours;
			public int minutes;
			public int seconds;

			public Interval(string type, int interval)
			{
				this.years = type == goog.date.Interval.YEARS ? interval : 0;
				this.months = type == goog.date.Interval.MONTHS ? interval : 0;
				this.days = type == goog.date.Interval.DAYS ? interval : 0;
				this.hours = type == goog.date.Interval.HOURS ? interval : 0;
				this.minutes = type == goog.date.Interval.MINUTES ? interval : 0;
				this.seconds = type == goog.date.Interval.SECONDS ? interval : 0;
			}

			public Interval(int opt_years = 0, int opt_months = 0, int opt_days = 0,
				int opt_hours = 0, int opt_minutes = 0, int opt_seconds = 0)
			{
				this.years = opt_years;
				this.months = opt_months;
				this.days = opt_days;
				this.hours = opt_hours;
				this.minutes = opt_minutes;
				this.seconds = opt_seconds;
			}
		}
	}

	public class reflect
	{
		internal static bool canAccessProperty(Node relatedTarget, string v)
		{
			throw new NotImplementedException();
		}
	}
}

namespace goog.positioning
{
	/// <summary>
	/// Enum for bits in the {@see goog.positioning.Corner) bitmap.
	/// </summary>
	public enum CornerBit
	{
		BOTTOM = 1,
		CENTER = 2,
		RIGHT = 4,
		FLIP_RTL = 8
	}

	/// <summary>
	/// The START constants map to LEFT if element directionality is left
	/// to right and RIGHT if the directionality is right to left.
	/// Likewise END maps to RIGHT or LEFT depending on the directionality.
	/// </summary>
	public enum Corner
	{
		TOP_LEFT = 0,
		TOP_RIGHT = goog.positioning.CornerBit.RIGHT,
		BOTTOM_LEFT = goog.positioning.CornerBit.BOTTOM,
		BOTTOM_RIGHT =
			goog.positioning.CornerBit.BOTTOM | goog.positioning.CornerBit.RIGHT,
		TOP_START = goog.positioning.CornerBit.FLIP_RTL,
		TOP_END =
			goog.positioning.CornerBit.FLIP_RTL | goog.positioning.CornerBit.RIGHT,
		BOTTOM_START =
			goog.positioning.CornerBit.BOTTOM | goog.positioning.CornerBit.FLIP_RTL,
		BOTTOM_END = goog.positioning.CornerBit.BOTTOM |
			goog.positioning.CornerBit.RIGHT | goog.positioning.CornerBit.FLIP_RTL,
		TOP_CENTER = goog.positioning.CornerBit.CENTER,
		BOTTOM_CENTER =
			goog.positioning.CornerBit.BOTTOM | goog.positioning.CornerBit.CENTER
	}

	/// <summary>
	/// Abstract position object. Encapsulates position and overflow handling.
	/// </summary>
	public class AbstractPosition
	{
		internal void reposition(HTMLElement el, Corner popupCorner_, math.Box margin_)
		{
			throw new NotImplementedException();
		}
	}

	public class AnchoredPosition : AbstractPosition
	{
		private Element lastTarget_;
		private Corner popupCorner_;

		public AnchoredPosition(Element lastTarget_, Corner popupCorner_)
		{
			this.lastTarget_ = lastTarget_;
			this.popupCorner_ = popupCorner_;
		}
	}
}

namespace goog.fx
{
	public class Transition : goog.events.EventTarget
	{
		/// <summary>
		/// Transition event types.
		/// </summary>
		public class EventType
		{
			/// <summary>
			/// Dispatched when played for the first time OR when it is resumed.
			/// </summary>
			public const string PLAY = "play";
			/// <summary>
			/// Dispatched only when the animation starts from the beginning.
			/// </summary>
			public const string BEGIN = "begin";
			/// <summary>
			/// Dispatched only when animation is restarted after a pause.
			/// </summary>
			public const string RESUME = "resume";
			/// <summary>
			/// Dispatched when animation comes to the end of its duration OR stop
			/// is called.
			/// </summary>
			public const string END = "end";
			/// <summary>
			/// Dispatched only when stop is called.
			/// </summary>
			public const string STOP = "stop";
			/// <summary>
			/// Dispatched only when animation comes to its end naturally.
			/// </summary>
			public const string FINISH = "finish";
			/// <summary>
			/// Dispatched when an animation is paused.
			/// </summary>
			public const string PAUSE = "pause";
		}

		/// <summary>
		/// Plays the transition.
		/// </summary>
		public virtual void play()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Stops the transition.
		/// </summary>
		public virtual void stop()
		{
			throw new NotImplementedException();
		}
	}
}
