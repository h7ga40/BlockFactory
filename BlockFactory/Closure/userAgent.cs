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
using System.Collections.Generic;

namespace goog
{
	public static class userAgent
	{
		/// <summary>
		/// Whether we know at compile-time that the browser is IE.
		/// </summary>
		public static bool ASSUME_IE = false;
		/// <summary>
		/// Whether we know at compile-time that the browser is EDGE.
		/// </summary>
		public static bool ASSUME_EDGE = false;
		/// <summary>
		/// Whether we know at compile-time that the browser is GECKO.
		/// </summary>
		public static bool ASSUME_GECKO = false;
		/// <summary>
		/// Whether we know at compile-time that the browser is WEBKIT.
		/// </summary>
		public static bool ASSUME_WEBKIT = false;
		/// <summary>
		/// Whether we know at compile-time that the browser is a
		/// mobile device running WebKit e.g. iPhone or Android.
		/// </summary>
		public static bool ASSUME_MOBILE_WEBKIT = false;
		/// <summary>
		/// Whether we know at compile-time that the browser is OPERA.
		/// </summary>
		public static bool ASSUME_OPERA = false;
		/// <summary>
		/// Whether the
		/// {@code isVersionOrHigher}
		/// function will return true for any version.
		/// </summary>
		public static bool ASSUME_ANY_VERSION = false;
		/// <summary>
		/// Whether we know the browser engine at compile-time.
		/// </summary>
		private static bool BROWSER_KNOWN_ = ASSUME_IE ||
			ASSUME_EDGE || ASSUME_GECKO ||
			ASSUME_MOBILE_WEBKIT || ASSUME_WEBKIT ||
			ASSUME_OPERA;

		/// <summary>
		/// Returns the userAgent string for the current browser.
		/// </summary>
		/// <returns></returns>
		public static string getUserAgentString()
		{
			var navigator = getNavigator();
			if (navigator != null) {
				var userAgent = navigator.UserAgent;
				if (userAgent != null) {
					return userAgent;
				}
			}
			return "";
		}

		/// <summary>
		/// TODO(nnaze): Change type to "Navigator" and update compilation targets.
		/// </summary>
		/// <returns>The native navigator object.</returns>
		public static NavigatorInstance getNavigator()
		{
			return Navigator.Instance;
		}

		public static bool OPERA = BROWSER_KNOWN_ ?
			ASSUME_OPERA : isOpera();

		public static bool isOpera()
		{
			return Navigator.UserAgent.Contains("Opera");
		}

		public static bool IE = BROWSER_KNOWN_ ?
			ASSUME_IE : isIE();

		public static bool isIE()
		{
			var userAgent = Window.Navigator.UserAgent;
			return userAgent.Contains("Trident") || userAgent.Contains("MSIE");
		}

		public static bool EDGE = BROWSER_KNOWN_ ?
			ASSUME_EDGE : isEdge();

		private static bool isEdge()
		{
			return Window.Navigator.UserAgent.Contains("Edge");
		}

		public static bool EDGE_OR_IE = IE || EDGE;

		public static bool GECKO = BROWSER_KNOWN_ ?
			ASSUME_GECKO : isGecko();

		private static bool isGecko()
		{
			return Window.Navigator.UserAgent.Contains("Gecko") &&
				!isWebKit() &&
				!isTrident() &&
				!isEdge();
		}

		public static bool WEBKIT = BROWSER_KNOWN_ ?
				ASSUME_WEBKIT || ASSUME_MOBILE_WEBKIT : isWebKit();

		public static bool isWebKit()
		{
			return Window.Navigator.UserAgent.ToLower().Contains("WebKit".ToLower()) &&
				!isEdge();
		}

		public static bool isTrident()
		{
			// IE only started including the Trident token in IE8.
			var userAgent = Window.Navigator.UserAgent;
			return userAgent.Contains("Trident") || userAgent.Contains("MSIE");
		}

		private static bool isMobile_()
		{
			return goog.userAgent.WEBKIT &&
				Window.Navigator.UserAgent.Contains("Mobile");
		}

		public static bool MOBILE =
			ASSUME_MOBILE_WEBKIT || isMobile_();

		public static bool SAFARI = WEBKIT;

		/// <summary>
		/// the platform (operating system) the user agent is running
		/// on. Default to empty string because navigator.platform may not be defined
		/// (on Rhino, for example).
		/// </summary>
		private static string determinePlatform_()
		{
			var navigator = goog.userAgent.getNavigator();
			return navigator == null ? null : navigator.Platform ?? "";
		}

		/// <summary>
		/// The platform (operating system) the user agent is running on. Default to
		/// empty string because navigator.platform may not be defined (on Rhino, for
		/// example).
		/// </summary>
		public static string PLATFORM = determinePlatform_();

		/// <summary>
		/// Whether the user agent is running on a Macintosh operating
		/// </summary>
		public static bool ASSUME_MAC = false;

		/// <summary>
		/// Whether the user agent is running on a Windows operating
		/// </summary>
		public static bool ASSUME_WINDOWS = false;

		/// <summary>
		/// Whether the user agent is running on a Linux operating
		/// </summary>
		public static bool ASSUME_LINUX = false;

		/// <summary>
		/// Whether the user agent is running on a X11 windowing
		/// </summary>
		public static bool ASSUME_X11 = false;

		/// <summary>
		/// Whether the user agent is running on Android.
		/// </summary>
		public static bool ASSUME_ANDROID = false;

		/// <summary>
		/// Whether the user agent is running on an iPhone.
		/// </summary>
		public static bool ASSUME_IPHONE = false;

		/// <summary>
		/// Whether the user agent is running on an iPad.
		/// </summary>
		public static bool ASSUME_IPAD = false;

		/// <summary>
		/// Whether the user agent is running on an iPod.
		/// </summary>
		public static bool ASSUME_IPOD = false;

		public static bool PLATFORM_KNOWN_ = ASSUME_MAC ||
			ASSUME_WINDOWS || ASSUME_LINUX ||
			ASSUME_X11 || ASSUME_ANDROID ||
			ASSUME_IPHONE || ASSUME_IPAD ||
			ASSUME_IPOD;

		/// <summary>
		/// Whether the user agent is running on a Macintosh operating system.
		/// </summary>
		public static bool MAC = PLATFORM_KNOWN_ ?
			ASSUME_MAC : isMacintosh();

		/// <summary>
		/// Whether the platform is Mac.
		/// </summary>
		/// <returns></returns>
		public static bool isMacintosh()
		{
			return Window.Navigator.UserAgent.Contains("Macintosh");
		}

		/// <summary>
		/// Whether the user agent is running on a Windows operating system.
		/// </summary>
		public static bool WINDOWS = PLATFORM_KNOWN_ ?
			ASSUME_WINDOWS : isWindows();

		public static bool isWindows()
		{
			return Window.Navigator.UserAgent.Contains("Windows");
		}

		/// <summary>
		/// Whether the user agent is Linux per the legacy behavior of
		/// LINUX, which considered ChromeOS to also be
		/// Linux.
		/// </summary>
		/// <returns></returns>
		private static bool isLegacyLinux_()
		{
			return isLinux() || isChromeOS();
		}

		/// <summary>
		/// Whether the user agent is running on a Linux operating system.
		///
		/// Note that LINUX considers ChromeOS to be Linux,
		/// while goog.labs.userAgent.platform considers ChromeOS and
		/// Linux to be different OSes.
		/// </summary>
		public static bool LINUX = PLATFORM_KNOWN_ ?
			ASSUME_LINUX : isLegacyLinux_();

		/// <summary>
		/// Whether the user agent is an X11 windowing system.
		/// </summary>
		private static bool isX11_()
		{
			var navigator = getNavigator();
			return (navigator != null) && (navigator.AppVersion ?? "").Contains("X11");
		}

		/// <summary>
		/// Whether the user agent is running on a X11 windowing system.
		/// @type {boolean}
		/// </summary>
		public static bool X11 = PLATFORM_KNOWN_ ?
			ASSUME_X11 : isX11_();

		public static bool isLinux()
		{
			return Window.Navigator.UserAgent.Contains("Linux");
		}

		public static bool isChromeOS()
		{
			return Window.Navigator.UserAgent.Contains("CrOS");
		}

		/// <summary>
		/// Whether the user agent is running on Android.
		/// </summary>
		public static bool ANDROID = PLATFORM_KNOWN_ ?
			ASSUME_ANDROID : isAndroid();

		public static bool isAndroid()
		{
			return Window.Navigator.UserAgent.Contains("Android");
		}

		/// <summary>
		/// Whether the user agent is running on an iPhone.
		/// @type {boolean}
		/// </summary>
		public static bool IPHONE = PLATFORM_KNOWN_ ?
			ASSUME_IPHONE : isIphone();

		/// <summary>
		/// Whether the user agent is running on an iPad.
		/// </summary>
		public static bool IPAD = PLATFORM_KNOWN_ ?
			ASSUME_IPAD : isIpad();


		/// <summary>
		/// Whether the user agent is running on an iPod.
		/// </summary>
		public static bool IPOD = PLATFORM_KNOWN_ ?
			ASSUME_IPOD : isIpod();

		public static bool isIphone()
		{
			var userAgent = Window.Navigator.UserAgent;
			return userAgent.Contains("iPhone") &&
				userAgent.Contains("iPod") && userAgent.Contains("iPad");
		}

		public static bool isIpad()
		{
			return Window.Navigator.UserAgent.Contains("iPad");
		}

		public static bool isIpod()
		{
			return Window.Navigator.UserAgent.Contains("iPod");
		}

		public static bool isIos()
		{
			return isIphone() || isIpad() || isIpod();
		}

		/// <summary>
		/// Returns the document mode (for testing).
		/// </summary>
		private static int? getDocumentMode_()
		{
			// NOTE(user): goog.userAgent may be used in context where there is no DOM.
			var doc = Document.Instance;
			if (doc != null)
				return doc.DocumentMode;
			else
				return null;
		}

		/// <summary>
		/// The version of the user agent. This is a string because it might contain
		/// 'b' (as in beta) as well as multiple dots.
		/// </summary>
		public static string VERSION = determineVersion_();
		/// <summary>
		/// Cache for {@link isVersionOrHigher}.
		/// Calls to compareVersions are surprisingly expensive and, as a browser's
		/// version number is unlikely to change during a session, we cache the results.
		/// </summary>
		private static bool? isVersionOrHigherCache_;

		/// <summary>
		/// Whether the user agent version is higher or the same as the given version.
		/// NOTE: When checking the version numbers for Firefox and Safari, be sure to
		/// use the engine's version, not the browser's version number.  For example,
		/// Firefox 3.0 corresponds to Gecko 1.9 and Safari 3.0 to Webkit 522.11.
		/// Opera and Internet Explorer versions match the product release number.<br>
		/// @see <a href="http://en.wikipedia.org/wiki/Safari_version_history">
		///     Webkit</a>
		/// @see <a href="http://en.wikipedia.org/wiki/Gecko_engine">Gecko</a>
		///
		/// @param {string|number} version The version to check.
		/// @return {boolean} Whether the user agent version is higher or the same as
		///     the given version.
		/// </summary>
		public static bool isVersionOrHigher(string version)
		{
			if (ASSUME_ANY_VERSION)
				return true;
			if (isVersionOrHigherCache_ == null) {
				isVersionOrHigherCache_ = compareVersions(VERSION, version) >= 0;
			}
			return isVersionOrHigherCache_.Value;
		}

		/// <summary>
		/// Compares two version numbers.
		/// </summary>
		/// <param name="version1">Version of first item.</param>
		/// <param name="version2">Version of second item.</param>
		/// <returns>1 if {@code version1} is higher.
		/// 0 if arguments are equal.
		/// -1 if {@code version2} is higher.</returns>
		private static int compareVersions(string version1, string version2)
		{
			var order = 0;
			// Trim leading and trailing whitespace and split the versions into
			// subversions.
			var v1Subs = version1.Trim().Split(".");
			var v2Subs = version2.Trim().Split(".");
			var subCount = Math.Max(v1Subs.Length, v2Subs.Length);

			// Iterate over the subversions, as long as they appear to be equivalent.
			for (var subIdx = 0; order == 0 && subIdx < subCount; subIdx++) {
				var v1Sub = v1Subs.Length > subIdx ? v1Subs[subIdx] : "";
				var v2Sub = v2Subs.Length > subIdx ? v2Subs[subIdx] : "";

				do {
					// Split the subversions into pairs of numbers and qualifiers (like "b").
					// Two different RegExp objects are use to make it clear the code
					// is side-effect free
					var v1Comp = new Regex(@"(\d*)(\D*)(.*)").Exec(v1Sub) ?? new string[] { "", "", "", "" };
					var v2Comp = new Regex(@"(\d*)(\D*)(.*)").Exec(v2Sub) ?? new string[] { "", "", "", "" };
					// Break if there are no more matches.
					if (v1Comp[0].Length == 0 && v2Comp[0].Length == 0) {
						break;
					}

					// Parse the numeric part of the subversion. A missing number is
					// equivalent to 0.
					var v1CompNum = v1Comp[1].Length == 0 ? 0 : Int32.Parse(v1Comp[1]);
					var v2CompNum = v2Comp[1].Length == 0 ? 0 : Int32.Parse(v2Comp[1]);

					// Compare the subversion components. The number has the highest
					// precedence. Next, if the numbers are equal, a subversion without any
					// qualifier is always higher than a subversion with any qualifier. Next,
					// the qualifiers are compared as strings.
					order = compareElements_(v1CompNum, v2CompNum);
					if (order != 0)
						order = compareElements_(v1Comp[2].Length == 0, v2Comp[2].Length == 0);
					if (order != 0)
						order = compareElements_(v1Comp[2], v2Comp[2]);
					// Stop as soon as an inequality is discovered.

					v1Sub = v1Comp[3];
					v2Sub = v2Comp[3];
				} while (order == 0);
			}

			return order;
		}

		/// <summary>
		/// Compares elements of a version number.
		/// </summary>
		/// <param name="left">An element from a version number.</param>
		/// <param name="right">An element from a version number.</param>
		/// <returns>1 if {@code left} is higher.
		/// 0 if arguments are equal.
		/// -1 if {@code right} is higher.</returns>
		private static int compareElements_(int left, int right)
		{
			if (left < right) {
				return -1;
			}
			else if (left > right) {
				return 1;
			}
			return 0;
		}

		private static int compareElements_(bool left, bool right)
		{
			if (!left && right) {
				return -1;
			}
			else if (left && !right) {
				return 1;
			}
			return 0;
		}

		private static int compareElements_(string left, string right)
		{
			if (String.Compare(left, right) < 0) {
				return -1;
			}
			else if (String.Compare(left, right) > 0) {
				return 1;
			}
			return 0;
		}

		/// <summary>
		/// Whether the IE effective document mode is higher or the same as the given
		/// document mode version.
		/// NOTE: Only for IE, return false for another browser.
		///
		/// @param {number} documentMode The document mode version to check.
		/// @return {boolean} Whether the IE effective document mode is higher or the
		///     same as the given version.
		/// </summary>
		public static bool isDocumentModeOrHigher(int documentMode)
		{
			return DOCUMENT_MODE.HasValue ? DOCUMENT_MODE.Value >= documentMode : false;
		}

		/// <summary>
		/// The string that describes the version number of the user
		/// agent.
		/// </summary>
		private static string determineVersion_()
		{
			// All browsers have different ways to detect the version and they all have
			// different naming schemes.
			// version is a string rather than a number because it may contain 'b', 'a',
			// and so on.
			var version = "";
			var arr = getVersionRegexResult_();
			if (arr != null) {
				version = arr.Length > 1 ? arr[1] : "";
			}

			if (IE) {
				// IE9 can be in document mode 9 but be reporting an inconsistent user agent
				// version.  If it is identifying as a version lower than 9 we take the
				// documentMode as the version instead.  IE8 has similar behavior.
				// It is recommended to set the X-UA-Compatible header to ensure that IE9
				// uses documentMode 9.
				var docMode = getDocumentMode_();
				if (docMode != null && docMode > Script.ParseFloat(version)) {
					return docMode?.ToString();
				}
			}

			return version;
		}

		/// <summary>
		/// The version regex matches from parsing the user
		/// agent string. These regex statements must be executed inline so they can
		/// be compiled out by the closure compiler with the rest of the useragent
		/// detection logic when ASSUME_* is specified.
		/// </summary>
		private static string[] getVersionRegexResult_()
		{
			var userAgent = getUserAgentString();
			if (GECKO) {
				return new Regex(@"rv\:([^\);]+)(\)|;)").Exec(userAgent);
			}
			if (EDGE) {
				return new Regex(@"Edge\/([\d\.]+)").Exec(userAgent);
			}
			if (IE) {
				return new Regex(@"\b(?:MSIE|rv)[: ]([^\);]+)(\)|;)").Exec(userAgent);
			}
			if (WEBKIT) {
				// WebKit/125.4
				return new Regex(@"WebKit\/(\S+)").Exec(userAgent);
			}
			if (OPERA) {
				// If none of the above browsers were detected but the browser is Opera, the
				// only string that is of interest is 'Version/<number>'.
				return new Regex(@"(?:Version)[ \/]?(\S+)").Exec(userAgent);
			}
			return null;
		}

		public static string getVersion()
		{
			var userAgentString = Window.Navigator.UserAgent;
			var version = "";
			Regex re;
			if (isWindows()) {
				re = new Regex(@"Windows (?:NT|Phone) ([0-9.]+)");
				var match = re.Exec(userAgentString);
				if (match != null) {
					version = match[1];
				}
				else {
					version = "0.0";
				}
			}
			else if (isIos()) {
				re = new Regex(@"(?:iPhone|iPod|iPad|CPU)\s+OS\s+(\S+)");
				var match = re.Exec(userAgentString);
				// Report the version as x.y.z and not x_y_z
				version = match != null ? match[1].Replace(new Regex(@"_", RegexOptions.Multiline), ".") : "";
			}
			else if (isMacintosh()) {
				re = new Regex(@"Mac OS X ([0-9_.]+)");
				var match = re.Exec(userAgentString);
				// Note: some old versions of Camino do not report an OSX version.
				// Default to 10.
				version = match != null ? match[1].Replace(new Regex(@"_", RegexOptions.Multiline), ".") : "10";
			}
			else if (isAndroid()) {
				re = new Regex(@"Android\s+([^\);]+)(\)|;)");
				var match = re.Exec(userAgentString);
				version = match == null ? null : match[1];
			}
			else if (isChromeOS()) {
				re = new Regex(@"(?:CrOS\s+(?:i686|x86_64)\s+([0-9.]+))");
				var match = re.Exec(userAgentString);
				version = match == null ? null : match[1];
			}
			return version ?? "";
		}

		/// <summary>
		/// For IE version < 7, documentMode is undefined, so attempt to use the
		/// CSS1Compat property to see if we are in standards mode. If we are in
		/// standards mode, treat the browser version as the document mode. Otherwise,
		/// IE is emulating version 5.
		/// @type {number|undefined}
		/// @const
		/// </summary>
		public static readonly int? DOCUMENT_MODE;

		static userAgent()
		{
			var doc = Document.Instance;
			var mode = getDocumentMode_();
			if (doc == null || !IE) {
				DOCUMENT_MODE = null;
			}
			DOCUMENT_MODE = mode.HasValue ? mode.Value :
				(doc.CompatMode == CompatMode.CSS1Compat ? (int)Script.ParseFloat(VERSION) : 5);
		}
	}
}
