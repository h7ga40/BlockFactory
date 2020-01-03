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
 * @fileoverview Object that controls settings for the workspace.
 * @author fenichel@google.com (Rachel Fenichel)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Options
	{
		public Element languageTree;
		public bool oneBasedIndex;
		public bool comments;
		public bool readOnly;
		public bool RTL;
		public bool horizontalLayout;
		public int toolboxPosition;
		public int maxBlocks;
		public SVGElement gridPattern;
		public GridOptions gridOptions;
		public ZoomOptions zoomOptions;
		public bool hasTrashcan;
		public bool hasSounds;
		public bool hasCss;
		public bool hasCategories;
		public bool hasScrollbars;
		public bool collapse;
		public string disabledPatternId;
		public bool disable;
		public string pathToMedia;
		public string embossFilterId;

		public Options(Dictionary<string, object> options = null)
		{
			if (options == null)
				options = new Dictionary<string, object>();

			var readOnly = options.ContainsKey("readOnly") ? (bool)options["readOnly"] : false;
			Element languageTree = null;
			var hasCategories = false;
			var hasTrashcan = false;
			var hasCollapse = false;
			var hasComments = false;
			var hasDisable = false;
			var hasSounds = false;
			if (!readOnly) {
				languageTree = options.ContainsKey("toolbox") ? Options.parseToolboxTree(new Union<string, Element>(options["toolbox"])) : null;
				hasCategories = languageTree != null &&
					languageTree.GetElementsByTagName("category").Length != 0;
				if (options.ContainsKey("trashcan")) {
					hasTrashcan = (bool)options["trashcan"];
				}
				else {
					hasTrashcan = hasCategories;
				}
				if (options.ContainsKey("collapse")) {
					hasCollapse = (bool)options["collapse"];
				}
				else {
					hasCollapse = hasCategories;
				}
				if (options.ContainsKey("comments")) {
					hasComments = (bool)options["comments"];
				}
				else {
					hasComments = hasCategories;
				}
				if (options.ContainsKey("disable")) {
					hasDisable = (bool)options["disable"];
				}
				else {
					hasDisable = hasCategories;
				}
				if (options.ContainsKey("sounds")) {
					hasSounds = (bool)options["sounds"];
				}
				else {
					hasSounds = true;
				}
			}
			var rtl = options.ContainsKey("rtl") ? (bool)options["rtl"] : false;
			bool horizontalLayout;
			if (options.ContainsKey("horizontalLayout")) {
				horizontalLayout = (bool)options["horizontalLayout"];
			}
			else {
				horizontalLayout = false;
			}
			bool toolboxAtStart = true;
			if (options.ContainsKey("toolboxPosition")) {
				if (options["toolboxPosition"].ToString() == "end") {
					toolboxAtStart = false;
				}
			}

			int toolboxPosition;
			if (horizontalLayout) {
				toolboxPosition = toolboxAtStart ?
					Core.TOOLBOX_AT_TOP : Core.TOOLBOX_AT_BOTTOM;
			}
			else {
				toolboxPosition = (toolboxAtStart == rtl) ?
					Core.TOOLBOX_AT_RIGHT : Core.TOOLBOX_AT_LEFT;
			}

			bool hasScrollbars;
			if (options.ContainsKey("scrollbars")) {
				hasScrollbars = (bool)options["scrollbars"];
			}
			else {
				hasScrollbars = hasCategories;
			}
			bool hasCss;
			if (options.ContainsKey("css")) {
				hasCss = (bool)options["css"];
			}
			else {
				hasCss = true;
			}
			var pathToMedia = "https://blockly-demo.appspot.com/static/media/";
			if (options.ContainsKey("media")) {
				pathToMedia = options["media"].ToString();
			}
			else if (options.ContainsKey("path")) {
				// "path" is a deprecated option which has been replaced by "media".
				pathToMedia = options["path"] + "media/";
			}
			var oneBasedIndex = true;
			if (options.ContainsKey("oneBasedIndex")) {
				oneBasedIndex = (bool)options["oneBasedIndex"];
			}

			this.RTL = rtl;
			this.oneBasedIndex = oneBasedIndex;
			this.collapse = hasCollapse;
			this.comments = hasComments;
			this.disable = hasDisable;
			this.readOnly = readOnly;
			this.maxBlocks = options.ContainsKey("maxBlocks") ? (int)options["maxBlocks"] : int.MaxValue;
			this.pathToMedia = pathToMedia;
			this.hasCategories = hasCategories;
			this.hasScrollbars = hasScrollbars;
			this.hasTrashcan = hasTrashcan;
			this.hasSounds = hasSounds;
			this.hasCss = hasCss;
			this.horizontalLayout = horizontalLayout;
			this.languageTree = languageTree;
			this.gridOptions = Options.parseGridOptions_(options);
			this.zoomOptions = Options.parseZoomOptions_(options);
			this.toolboxPosition = toolboxPosition;
		}

		/// <summary>
		/// The parent of the current workspace, or null if there is no parent workspace.
		/// </summary>
		public Workspace parentWorkspace;

		/// <summary>
		/// If set, sets the translation of the workspace to match the scrollbars.
		/// </summary>
		public Action<Metrics> setMetrics;

		/// <summary>
		/// Return an object with the metrics required to size the workspace.
		/// </summary>
		/// <returns>Contains size and position metrics, or null.</returns>
		public Func<Metrics> getMetrics;

		/// <summary>
		/// Parse the user-specified zoom options, using reasonable defaults where
		/// behaviour is unspecified.  See zoom documentation:
		/// https://developers.google.com/blockly/guides/configure/web/zoom
		/// </summary>
		/// <param name="options">Dictionary of options.</param>
		/// <returns>A dictionary of normalized options.</returns>
		private static ZoomOptions parseZoomOptions_(Dictionary<string, object> options)
		{
			var zoom = options.ContainsKey("zoom") ? (Dictionary<string, object>)options["zoom"] : new Dictionary<string, object>();
			var zoomOptions = new ZoomOptions();
			if (!zoom.ContainsKey("controls")) {
				zoomOptions.controls = false;
			}
			else {
				zoomOptions.controls = (bool)zoom["controls"];
			}
			if (!zoom.ContainsKey("wheel")) {
				zoomOptions.wheel = false;
			}
			else {
				zoomOptions.wheel = (bool)zoom["wheel"];
			}
			if (!zoom.ContainsKey("startScale")) {
				zoomOptions.startScale = 1;
			}
			else {
				zoomOptions.startScale = Script.ParseFloat(zoom["startScale"].ToString());
			}
			if (!zoom.ContainsKey("maxScale")) {
				zoomOptions.maxScale = 3;
			}
			else {
				zoomOptions.maxScale = Script.ParseFloat(zoom["maxScale"].ToString());
			}
			if (!zoom.ContainsKey("minScale")) {
				zoomOptions.minScale = 0.3;
			}
			else {
				zoomOptions.minScale = Script.ParseFloat(zoom["minScale"].ToString());
			}
			if (!zoom.ContainsKey("scaleSpeed")) {
				zoomOptions.scaleSpeed = 1.2;
			}
			else {
				zoomOptions.scaleSpeed = Script.ParseFloat(zoom["scaleSpeed"].ToString());
			}
			return zoomOptions;
		}

		/// <summary>
		/// Parse the user-specified grid options, using reasonable defaults where
		/// behaviour is unspecified. See grid documentation:
		/// https://developers.google.com/blockly/guides/configure/web/grid
		/// </summary>
		/// <param name="options">Dictionary of options.</param>
		/// <returns>A dictionary of normalized options.</returns>
		private static GridOptions parseGridOptions_(Dictionary<string, object> options)
		{
			var grid = options.ContainsKey("grid") ? (Dictionary<string, object>)options["grid"] : new Dictionary<string, object>();
			var gridOptions = new GridOptions();
			gridOptions.spacing = grid.ContainsKey("spacing") ? Script.ParseFloat(grid["spacing"].ToString()) : 0.0;
			gridOptions.colour = grid.ContainsKey("colour") ? grid["colour"].ToString() : "#888";
			gridOptions.length = grid.ContainsKey("length") ? Script.ParseFloat(grid["length"].ToString()) : 1.0;
			gridOptions.snap = gridOptions.spacing > 0 ? (grid.ContainsKey("snap") ? (bool)grid["snap"] : false) : false;
			return gridOptions;
		}

		/// <summary>
		/// Parse the provided toolbox tree into a consistent DOM format.
		/// </summary>
		/// <param name="tree_">DOM tree of blocks, or text representation of same.</param>
		/// <returns>tree of blocks, or null.</returns>
		public static Element parseToolboxTree(Union<string, Element> tree_)
		{
			Element tree = tree_.As<Element>();
			if (tree_ != null) {
				if (!tree_.Is<string>()) {
					if (false/*typeof XSLTProcessor == 'undefined' && tree.outerHTML*/) {
						// In this case the tree will not have been properly built by the
						// browser. The HTML will be contained in the element, but it will
						// not have the proper DOM structure since the browser doesn't support
						// XSLTProcessor (XML -> HTML). This is the case in IE 9+.
						tree_ = tree_.As<Element>().OuterHTML;
					}
					else if (!(tree_.Is<Element>())) {
						tree = null;
					}
				}
				if (tree_.Is<string>()) {
					tree = Xml.textToDom(tree_.As<string>());
				}
			}
			else {
				tree = null;
			}
			return tree;
		}
	}

	public class ZoomOptions
	{
		public double minScale;
		public double maxScale;
		internal bool controls;
		internal bool wheel;
		internal double scaleSpeed;
		internal double startScale;
	}

	public class GridOptions
	{
		public double spacing = 0.0;
		public string colour = "#888";
		public double length = 1.0;
		public bool snap = false;
	}
}
