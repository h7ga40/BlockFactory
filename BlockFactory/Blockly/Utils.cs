/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2012 Google Inc.
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
 * @fileoverview Utility methods.
 * These methods are not specific to Blockly, and could be factored out into
 * a JavaScript framework such as Closure.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public partial class Core
	{
		/// <summary>
		/// Add a CSS class to a element.
		/// Similar to Closure's goog.dom.classes.add, except it handles SVG elements.
		/// </summary>
		/// <param name="element">DOM element to add class to.</param>
		/// <param name="className">Name of class to add.</param>
		internal static void addClass_(Element element, string className)
		{
			var classes = element.GetAttribute("class") ?? "";
			if ((" " + classes + " ").IndexOf(" " + className + " ") == -1) {
				if (classes.Length > 0) {
					classes += " ";
				}
				element.SetAttribute("class", classes + className);
			}
		}

		/// <summary>
		/// Remove a CSS class from a element.
		/// Similar to Closure's goog.dom.classes.remove, except it handles SVG elements.
		/// </summary>
		/// <param name="element">DOM element to remove class from.</param>
		/// <param name="className">Name of class to remove.</param>
		internal static void removeClass_(Element element, string className)
		{
			var classes = element.GetAttribute("class");
			if ((" " + classes + " ").IndexOf(" " + className + " ") != -1) {
				var classList = classes.Split(new Regex(@"\s+"));
				for (var i = 0; i < classList.Length; i++) {
					if (classList[i] == null || classList[i] == className) {
						classList.Splice(i, 1);
						i--;
					}
				}
				if (classList.Length >= 0) {
					element.SetAttribute("class", classList.Join(" "));
				}
				else {
					element.RemoveAttribute("class");
				}
			}
		}

		/// <summary>
		/// Checks if an element has the specified CSS class.
		/// Similar to Closure's goog.dom.classes.has, except it handles SVG elements.
		/// </summary>
		/// <param name="element">DOM element to check.</param>
		/// <param name="className">Name of class to check.</param>
		/// <returns>True if class exists, false otherwise.</returns>
		internal static bool hasClass_(Element element, string className)
		{
			var classes = element.GetAttribute("class");
			return (" " + classes + " ").IndexOf(" " + className + " ") != -1;
		}

		/// <summary>
		/// Bind an ev to a function call.  When calling the function, verifies that
		/// it belongs to the touch stream that is currently being processsed, and splits
		/// multitouch events into multiple events as needed.
		/// </summary>
		/// <param name="node">Node upon which to listen.</param>
		/// <param name="name">Event name to listen to (e.g. 'mousedown').</param>
		/// <param name="thisObject">The value of 'this' in the function.</param>
		/// <param name="func">Function to call when ev is triggered.</param>
		/// <param name="opt_noCaptureIdentifier">True if triggering on this ev
		/// should not block execution of other ev handlers on this touch or other
		/// simultaneous touches.</param>
		/// <returns>Opaque data that can be passed to unbindEvent_.</returns>
		internal static JsArray<EventWrapInfo> bindEventWithChecks_<T>(Node node, string name, object thisObject,
			Action<T> func, bool opt_noCaptureIdentifier = false) where T : Event
		{
			if ((thisObject != null) && (thisObject != func.Target)) {
				throw new ArgumentException();
			}
			var handled = false;
			var wrapFunc = new Action<T>((e) => {
				var captureIdentifier = !opt_noCaptureIdentifier;
				// Handle each touch point separately.  If the ev was a mouse ev, this
				// will hand back an array with one element, which we're fine handling.
				var events = Touch.splitEventByTouches(e);
				foreach (var ev in events) {
					if (captureIdentifier && !Touch.shouldHandleEvent(ev)) {
						continue;
					}
					Touch.setClientFromTouch(ev);
					if ((thisObject != null) && (thisObject != func.Target)) {
						func.Method.Invoke(thisObject, new object[] { ev });
					}
					else {
						func.Invoke(ev);
					}
					handled = true;
				}
			});

			node.AddEventListener(name, wrapFunc, false);
			var bindData = new JsArray<EventWrapInfo> {
				new EventWrapInfo(node, name, wrapFunc)
			};

			// Add equivalent touch ev.
			if (Touch.TOUCH_MAP.ContainsKey(name)) {
				var touchWrapFunc = new Action<T>((e) => {
					wrapFunc(e);
					// Stop the browser from scrolling/zooming the page.
					if (handled) {
						e.PreventDefault();
					}
				});
				string eventName;

				for (var i = 0; (eventName = (string)Touch.TOUCH_MAP[name][i]) != null; i++) {
					node.AddEventListener(eventName, touchWrapFunc, false);
					bindData.Push(new EventWrapInfo(node, eventName, touchWrapFunc));
				}
			}
			return bindData;
		}

		/// <summary>
		/// Bind an ev to a function call.  Handles multitouch events by using the
		/// coordinates of the first changed touch, and doesn't do any safety checks for
		/// simultaneous ev processing.
		/// </summary>
		/// <remarks>deprecated in favor of bindEventWithChecks_, but preserved for external
		/// users.</remarks>
		/// <param name="node">Node upon which to listen.</param>
		/// <param name="name">Event name to listen to (e.g. 'mousedown').</param>
		/// <param name="thisObject">The value of 'this' in the function.</param>
		/// <param name="func">Function to call when ev is triggered.</param>
		/// <returns>Opaque data that can be passed to unbindEvent_.</returns>
		internal static JsArray<EventWrapInfo> bindEvent_<T>(Node node, string name,
			object thisObject, Action<T> func) where T : Event
		{
			if ((thisObject != null) && (thisObject != func.Target) && (func.Target != null)) {
				throw new ArgumentException();
			}
			var wrapFunc = new Action<T>((e) => {
				if (thisObject != null) {
					var mi = func.Method;
					var len = mi.GetParameters().Length;
					if (len == 0) {
						mi.Invoke(thisObject, new object[0]);
					}
					else {
						mi.Invoke(thisObject, new object[] { e });
					}
				}
				else {
					func.Invoke(e);
				}
			});

			node.AddEventListener(name, wrapFunc, false);
			var bindData = new JsArray<EventWrapInfo> {
				 new EventWrapInfo(node, name, wrapFunc)
			};

			// Add equivalent touch event.
			if (Touch.TOUCH_MAP.ContainsKey(name)) {
				var touchWrapFunc = new Action<T>((e) => {
					// Punt on multitouch events.
					if (e.ChangedTouches.Length == 1) {
						// Map the touch event's properties to the event.
						var touchPoint = e.ChangedTouches[0];
						e.ClientX = touchPoint.ClientX;
						e.ClientY = touchPoint.ClientY;
					}
					wrapFunc(e);

					// Stop the browser from scrolling/zooming the page.
					e.PreventDefault();
				});
				string eventName;
				for (var i = 0;
					 (eventName = (string)Touch.TOUCH_MAP[name][i]) != null; i++) {
					node.AddEventListener(eventName, touchWrapFunc, false);
					bindData.Push(new EventWrapInfo(node, eventName, touchWrapFunc));
				}
			}
			return bindData;
		}

		/// <summary>
		/// Unbind one or more events ev from a function call.
		/// </summary>
		/// <param name="bindData">Opaque data from bindEvent_.  This list is
		/// emptied during the course of calling this function.</param>
		/// <returns>The function call.</returns>
		internal static Delegate unbindEvent_(JsArray<EventWrapInfo> bindData)
		{
			Node node;
			string name;
			Delegate func = null;
			while (bindData.Length > 0) {
				var bindDatum = bindData.Pop();
				node = bindDatum.node;
				name = bindDatum.name;
				func = bindDatum.func;
				node.RemoveEventListener(name, func, false);
			}
			return func;
		}

		/// <summary>
		/// Don't do anything for this ev, just halt propagation.
		/// </summary>
		/// <param name="e">An ev.</param>
		public static void noEvent(Event e)
		{
			// This ev has been handled.  No need to bubble up to the document.
			e.PreventDefault();
			e.StopPropagation();
		}

		/// <summary>
		/// Is this ev targeting a text input widget?
		/// </summary>
		/// <param name="e">An ev.</param>
		/// <returns>True if text input.</returns>
		internal static bool isTargetInput_(Event e)
		{
			return e.Target.Type == "textarea" || e.Target.Type == "text" ||
				   e.Target.Type == "number" || e.Target.Type == "email" ||
				   e.Target.Type == "password" || e.Target.Type == "search" ||
				   e.Target.Type == "tel" || e.Target.Type == "url" ||
				   (e.Target is HTMLElement) ? ((HTMLElement)e.Target).IsContentEditable : false;
		}

		/// <summary>
		/// Return the coordinates of the top-left corner of this element relative to
		/// its parent.  Only for SVG elements and children (e.g. rect, g, path).
		/// </summary>
		/// <param name="element">SVG element to find the coordinates of.</param>
		/// <returns>Object with .x and .y properties.</returns>
		internal static goog.math.Coordinate getRelativeXY_(Element element)
		{
			var xy = new goog.math.Coordinate(0, 0);
			// First, check for x and y attributes.
			var x = element.GetAttribute("x");
			if (!String.IsNullOrEmpty(x)) {
				xy.x = Script.ParseFloat(x);
			}
			var y = element.GetAttribute("y");
			if (!String.IsNullOrEmpty(y)) {
				xy.y = Script.ParseFloat(y);
			}
			// Second, check for transform="translate(...)" attribute.
			var transform = element.GetAttribute("transform");
			var r = transform?.Match(Core.getRelativeXY__XY_REGEXP_);
			if (r?.Length > 0) {
				xy.x += Script.ParseFloat(r[1]);
				if (r[3] != null) {
					xy.y += Script.ParseFloat(r[3]);
				}
			}
			return xy;
		}

		/// <summary>
		/// Static regex to pull the x,y values out of an SVG translate() directive.
		/// Note that Firefox and IE (9,10) return 'translate(12)' instead of
		/// 'translate(12, 0)'.
		/// Note that IE (9,10) returns 'translate(16 8)' instead of 'translate(16, 8)'.
		/// Note that IE has been reported to return scientific notation (0.123456e-42).
		/// </summary>
		private static Regex getRelativeXY__XY_REGEXP_ =
			new Regex(@"translate\(\s*([-+\d.e]+)([ ,]\s*([-+\d.e]+)\s*\))?");

		/// <summary>
		/// Return the absolute coordinates of the top-left corner of this element,
		/// scales that after canvas SVG element, if it's a descendant.
		/// The origin (0,0) is the top-left corner of the Blockly SVG.
		/// </summary>
		/// <param name="element">Element to find the coordinates of.</param>
		/// <param name="workspace">Element must be in this workspace.</param>
		/// <returns>Object with .x and .y properties.</returns>
		internal static goog.math.Coordinate getSvgXY_(SVGElement element,
			WorkspaceSvg workspace)
		{
			var x = 0.0;
			var y = 0.0;
			var scale = 1.0;
			if (goog.dom.contains(workspace.getCanvas(), element) ||
				goog.dom.contains(workspace.getBubbleCanvas(), element)) {
				// Before the SVG canvas, scale the coordinates.
				scale = workspace.scale;
			}
			do {
				// Loop through this block and every parent.
				var xy = Core.getRelativeXY_(element);
				if (element == workspace.getCanvas() ||
					element == workspace.getBubbleCanvas()) {
					// After the SVG canvas, don't scale the coordinates.
					scale = 1;
				}
				x += xy.x * scale;
				y += xy.y * scale;
				element = element.ParentNode as SVGElement;
			} while (element != null && element != workspace.getParentSvg());
			return new goog.math.Coordinate(x, y);
		}

		/// <summary>
		/// Helper method for creating SVG elements.
		/// </summary>
		/// <param name="name">Element's tag name.</param>
		/// <param name="attrs">Dictionary of attribute names and values.</param>
		/// <param name="parent">Optional parent on which to append the element.</param>
		/// <param name="opt_workspace">Optional workspace for access to context (scale...).</param>
		/// <returns>Newly created SVG element.</returns>
		public static SVGElement createSvgElement(string name, Dictionary<string, object> attrs,
			Element parent, Workspace opt_workspace = null)
		{
			var e = Document.CreateElementNS<SVGElement>(Core.SVG_NS, name);
			foreach (var key in attrs.Keys) {
				e.SetAttribute(key, attrs[key].ToString());
			}
			// IE defines a unique attribute "runtimeStyle", it is NOT applied to
			// elements created with createElementNS. However, Closure checks for IE
			// and assumes the presence of the attribute and crashes.
			if (Document.Body.runtimeStyle != null) {  // Indicates presence of IE-only attr.
				e.runtimeStyle = e.currentStyle = e.style;
			}
			if (parent != null) {
				parent.AppendChild(e);
			}
			return e;
		}

		/// <summary>
		/// Is this ev a right-click?
		/// </summary>
		/// <param name="e">Mouse ev.</param>
		/// <returns>True if right-click.</returns>
		public static bool isRightButton(Event e)
		{
			if (e.CtrlKey && goog.userAgent.MAC) {
				// Control-clicking on Mac OS X is treated as a right-click.
				// WebKit on Mac OS X fails to change button to 2 (but Gecko does).
				return true;
			}
			return e.Button == 2;
		}

		/// <summary>
		/// Return the converted coordinates of the given mouse ev.
		/// The origin (0,0) is the top-left corner of the Blockly svg.
		/// </summary>
		/// <param name="e">Mouse ev.</param>
		/// <param name="svg">SVG element.</param>
		/// <param name="matrix">Inverted screen CTM to use.</param>
		/// <returns>Object with .x and .y properties.</returns>
		public static SVGPoint mouseToSvg(Event e, SVGElement svg, SVGMatrix matrix)
		{
			var svgPoint = svg.createSVGPoint();
			svgPoint.x = e.ClientX;
			svgPoint.y = e.ClientY;

			if (matrix == null) {
				matrix = svg.getScreenCTM().Inverse();
			}
			return svgPoint.matrixTransform(matrix);
		}

		/// <summary>
		/// Given an array of strings, return the length of the shortest one.
		/// </summary>
		/// <param name="array">Array of strings.</param>
		/// <returns>Length of shortest string.</returns>
		public static int shortestStringLength(string[] array)
		{
			if (array.Length == 0) {
				return 0;
			}
			var len = array[0].Length;
			for (var i = 1; i < array.Length; i++) {
				len = System.Math.Min(len, array[i].Length);
			}
			return len;
		}

		/// <summary>
		/// Given an array of strings, return the length of the common prefix.
		/// Words may not be split.  Any space after a word is included in the length.
		/// </summary>
		/// <param name="array">Array of strings.</param>
		/// <param name="opt_shortest">Length of shortest string.</param>
		/// <returns>Length of common prefix.</returns>
		public static int commonWordPrefix(string[] array, int opt_shortest = 0)
		{
			if (array.Length == 0) {
				return 0;
			}
			else if (array.Length == 1) {
				return array[0].Length;
			}
			var wordPrefix = 0;
			var max = opt_shortest != 0 ? opt_shortest : Core.shortestStringLength(array);
			int len;
			for (len = 0; len < max; len++) {
				var letter = array[0][len];
				for (var i = 1; i < array.Length; i++) {
					if (letter != array[i][len]) {
						return wordPrefix;
					}
				}
				if (letter == ' ') {
					wordPrefix = len + 1;
				}
			}
			for (var i = 1; i < array.Length; i++) {
				var letter = len < array[i].Length ? array[i][len] : '\0';
				if (letter != '\0' && letter != ' ') {
					return wordPrefix;
				}
			}
			return max;
		}

		/// <summary>
		/// Given an array of strings, return the length of the common suffix.
		/// Words may not be split.  Any space after a word is included in the length.
		/// </summary>
		/// <param name="array">Array of strings.</param>
		/// <param name="opt_shortest">Length of shortest string.</param>
		/// <returns>Length of common suffix.</returns>
		public static int commonWordSuffix(string[] array, int opt_shortest = 0)
		{
			if (array.Length == 0) {
				return 0;
			}
			else if (array.Length == 1) {
				return array[0].Length;
			}
			var wordPrefix = 0;
			var max = opt_shortest != 0 ? opt_shortest : Core.shortestStringLength(array);
			int len;
			for (len = 0; len < max; len++) {
				var letter = array[0].Substring(array[0].Length - len - 1, 1);
				for (var i = 1; i < array.Length; i++) {
					if (letter != array[i].Substring(array[i].Length - len - 1, 1)) {
						return wordPrefix;
					}
				}
				if (letter == " ") {
					wordPrefix = len + 1;
				}
			}
			for (var i = 1; i < array.Length; i++) {
				var letter = array[i].CharAt(array[i].Length - len - 1);
				if (letter != null && letter != " ") {
					return wordPrefix;
				}
			}
			return max;
		}

		/// <summary>
		/// Is the given string a number (includes negative and decimals).
		/// </summary>
		/// <param name="str">Input string.</param>
		/// <returns>True if number, false otherwise.</returns>
		public static bool isNumber(string str)
		{
			return str.Match(new Regex(@"^\s*-?\d+(\.\d+)?\s*$")).Length > 0;
		}

		/// <summary>
		/// Generate a unique ID.  This should be globally unique.
		/// 87 characters ^ 20 length > 128 bits (better than a UUID).
		/// </summary>
		/// <returns>A globally unique ID string.</returns>
		public static string genUid()
		{
			var length = 20;
			var soupLength = Core.genUid_soup_.Length;
			var id = new JsArray<string>();
			for (var i = 0; i < length; i++) {
				int r = (int)(Script.Random() * soupLength);
				id.Push(Core.genUid_soup_.CharAt(r));
			}
			return id.Join("");
		}

		/// <summary>
		/// Legal characters for the unique ID.  Should be all on a US keyboard.
		/// No characters that conflict with XML or JSON.  Requests to remove additional
		/// 'problematic' characters from this soup will be denied.  That's your failure
		/// to properly escape in your own environment.  Issues #251, #625, #682.
		/// </summary>
		private static string genUid_soup_ = "!#$%()*+,-./:;=?@[]^_`{|}~" +
			"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

		public static class utils
		{
			/// <summary>
			/// Parse a string with any number of interpolation tokens (%1, %2, ...).
			/// '%' characters may be self-escaped (%%).
			/// </summary>
			/// <param name="message">Text containing interpolation tokens.</param>
			/// <returns>Array of strings and numbers.</returns>
			public static JsArray<Union<string, int>> tokenizeInterpolation(string message)
			{
				var tokens = new JsArray<Union<string, int>>();
				var chars = new JsArray<char>(message.ToCharArray());
				// End marker.
				chars.Push('\0');
				// Parse the message with a finite state machine.
				// 0 - Base case.
				// 1 - % found.
				// 2 - Digit found.
				var state = 0;
				var buffer = new JsArray<char>();
				var number = "";
				for (var i = 0; i < chars.Length; i++) {
					var c = chars[i];
					if (state == 0) {
						if (c == '%') {
							state = 1;  // Start escape.
						}
						else {
							buffer.Push(c);  // Regular char.
						}
					}
					else if (state == 1) {
						if (c == '%') {
							buffer.Push(c);  // Escaped %: %%
							state = 0;
						}
						else if ('0' <= c && c <= '9') {
							state = 2;
							number = c.ToString();
							var text2 = new String(buffer.ToArray());
							if (!String.IsNullOrEmpty(text2)) {
								tokens.Push(text2);
							}
							buffer = new JsArray<char>();
						}
						else {
							buffer.Push('%', c);  // Not an escape: %a
							state = 0;
						}
					}
					else if (state == 2) {
						if ('0' <= c && c <= '9') {
							number += c;  // Multi-digit number.
						}
						else {
							tokens.Push(Int32.Parse(number));
							i--;  // Parse this char again.
							state = 0;
						}
					}
				}
				var text = new String(buffer.ToArray());
				if (!String.IsNullOrEmpty(text)) {
					tokens.Push(text);
				}
				return tokens;
			}

			/// <summary>
			/// Wrap text to the specified width.
			/// </summary>
			/// <param name="text">Text to wrap.</param>
			/// <param name="limit">Width to wrap each line.</param>
			/// <returns></returns>
			public static string wrap(string text, int limit)
			{
				var lines = text.Split("\n");
				for (var i = 0; i < lines.Length; i++) {
					lines[i] = Core.utils.wrap_line_(lines[i], limit);
				}
				return lines.Join("\n");
			}

			/// <summary>
			/// Wrap single line of text to the specified width.
			/// </summary>
			/// <param name="text">Text to wrap.</param>
			/// <param name="limit">Width to wrap each line.</param>
			/// <returns>Wrapped text.</returns>
			private static string wrap_line_(string text, int limit)
			{
				if (text.Length <= limit) {
					// Short text, no need to wrap.
					return text;
				}
				// Split the text into words.
				var words = text.Trim().Split(new Regex(@"\s+"));
				// Set limit to be the length of the largest word.
				for (var i = 0; i < words.Length; i++) {
					if (words[i].Length > limit) {
						limit = words[i].Length;
					}
				}

				double lastScore;
				var score = Double.MinValue;
				string lastText;
				var lineCount = 1;
				do {
					lastScore = score;
					lastText = text;
					// Create a list of booleans representing if a space (false) or
					// a break (true) appears after each word.
					var wordBreaks = new JsArray<bool?>(words.Length);
					// Seed the list with evenly spaced linebreaks.
					var steps = words.Length / lineCount;
					var insertedBreaks = 1;
					for (var i = 0; i < words.Length - 1; i++) {
						if (insertedBreaks < (i + 1.5) / steps) {
							insertedBreaks++;
							wordBreaks[i] = true;
						}
						else {
							wordBreaks[i] = false;
						}
					}
					wordBreaks[words.Length - 1] = false; // 一つ追加してwordsと数を合わせた
					wordBreaks = Core.utils.wrapMutate_(words, wordBreaks, limit);
					score = Core.utils.wrapScore_(words, wordBreaks, limit);
					text = Core.utils.wrapToText_(words, wordBreaks);
					lineCount++;
				} while (score > lastScore);
				return lastText;
			}

			/// <summary>
			/// Compute a score for how good the wrapping is.
			/// </summary>
			/// <param name="words">Array of each word.</param>
			/// <param name="wordBreaks">Array of line breaks.</param>
			/// <param name="limit">Width to wrap each line.</param>
			/// <returns>Larger the better.</returns>
			private static double wrapScore_(string[] words, bool?[] wordBreaks, int limit)
			{
				// If this function becomes a performance liability, add caching.
				// Compute the length of each line.
				var lineLengths = new JsArray<int> { 0 };
				var linePunctuation = new JsArray<string>();
				for (var i = 0; i < words.Length; i++) {
					lineLengths[lineLengths.Length - 1] += words[i].Length;
					if (wordBreaks[i].HasValue && wordBreaks[i].Value == true) {
						lineLengths.Push(0);
						linePunctuation.Push(words[i].CharAt(words[i].Length - 1));
					}
					else if (wordBreaks[i].HasValue && wordBreaks[i].Value == false) {
						lineLengths[lineLengths.Length - 1]++;
					}
				}
				var maxLength = lineLengths.Max();

				var score = 0.0;
				for (var i = 0; i < lineLengths.Length; i++) {
					// Optimize for width.
					// -2 points per char over limit (scaled to the power of 1.5).
					score -= System.Math.Pow(System.Math.Abs(limit - lineLengths[i]), 1.5) * 2;
					// Optimize for even lines.
					// -1 point per char smaller than max (scaled to the power of 1.5).
					score -= System.Math.Pow(maxLength - lineLengths[i], 1.5);
					// Optimize for structure.
					// Add score to line endings after punctuation.
					if (i < linePunctuation.Length) {
						if (".?!".IndexOf(linePunctuation[i]) != -1) {
							score += limit / 3;
						}
						else if (",;)]}".IndexOf(linePunctuation[i]) != -1) {
							score += limit / 4;
						}
					}
				}
				// All else being equal, the last line should not be longer than the
				// previous line.  For example, this looks wrong:
				// aaa bbb
				// ccc ddd eee
				if (lineLengths.Length > 1 && lineLengths[lineLengths.Length - 1] <=
					lineLengths[lineLengths.Length - 2]) {
					score += 0.5;
				}
				return score;
			}

			/// <summary>
			/// Mutate the array of line break locations until an optimal solution is found.
			/// No line breaks are added or deleted, they are simply moved around.
			/// </summary>
			/// <param name="words">Array of each word.</param>
			/// <param name="wordBreaks">Array of line breaks.</param>
			/// <param name="limit">Width to wrap each line.</param>
			/// <returns>New array of optimal line breaks.</returns>
			private static JsArray<bool?> wrapMutate_(string[] words, JsArray<bool?> wordBreaks, int limit)
			{
				var bestScore = Core.utils.wrapScore_(words, wordBreaks, limit);
				JsArray<bool?> bestBreaks = null;
				// Try shifting every line break forward or backward.
				for (var i = 0; i < wordBreaks.Length - 1; i++) {
					if (wordBreaks[i] == wordBreaks[i + 1]) {
						continue;
					}
					var mutatedWordBreaks = new JsArray<bool?>();
					wordBreaks.ForEach((a) => mutatedWordBreaks.Push(a));
					mutatedWordBreaks[i] = !mutatedWordBreaks[i];
					mutatedWordBreaks[i + 1] = !mutatedWordBreaks[i + 1];
					var mutatedScore =
						Core.utils.wrapScore_(words, mutatedWordBreaks, limit);
					if (mutatedScore > bestScore) {
						bestScore = mutatedScore;
						bestBreaks = mutatedWordBreaks;
					}
				}
				if (bestBreaks != null) {
					// Found an improvement.  See if it may be improved further.
					return Core.utils.wrapMutate_(words, bestBreaks, limit);
				}
				// No improvements found.  Done.
				return wordBreaks;
			}

			/// <summary>
			/// Reassemble the array of words into text, with the specified line breaks.
			/// </summary>
			/// <param name="words">Array of each word.</param>
			/// <param name="wordBreaks">Array of line breaks.</param>
			/// <returns>Plain text.</returns>
			private static string wrapToText_(string[] words, bool?[] wordBreaks)
			{
				var text = new JsArray<string>();
				for (var i = 0; i < words.Length; i++) {
					text.Push(words[i]);
					if (wordBreaks[i].HasValue) {
						text.Push(wordBreaks[i].Value ? "\n" : " ");
					}
				}
				return text.Join("");
			}
		}
	}
}
