/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2011 Google Inc.
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
 * @fileoverview Functions for injecting Blockly into a web page.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public partial class Core
	{
		/// <summary>
		/// Inject a Blockly editor into the specified container element (usually a div).
		/// </summary>
		/// <param name="container">Containing element, or its ID, or a CSS selector.</param>
		/// <param name="opt_options">opt_options Optional dictionary of options.</param>
		/// <returns>Newly created main workspace.</returns>
		public static WorkspaceSvg inject(Union<string, Element> container_, Dictionary<string, object> opt_options = null)
		{
			Element container;
			if (container_.Is<string>()) {
				container = Document.GetElementById(container_.As<string>()) ??
					Document.QuerySelector(container_.As<string>());
			}
			else {
				container = container_.As<Element>();
			}
			// Verify that the container is in document.
			if (!goog.dom.contains(Document.Instance, container)) {
				throw new Exception("Error: container is not in current document.");
			}
			var options = new Options(opt_options ?? new Dictionary<string, object>());
			var subContainer = goog.dom.createDom("div", "injectionDiv");
			container.AppendChild(subContainer);
			var svg = Core.createDom_(subContainer, options);
			var workspace = Core.createMainWorkspace_(svg, options);
			Core.init_(workspace);
			workspace.markFocused(null);
			Core.bindEventWithChecks_(svg, "focus", workspace, new Action<Event>(workspace.markFocused));
			Core.svgResize(workspace);
			return workspace;
		}

		/// <summary>
		/// Create the SVG image.
		/// </summary>
		/// <param name="container">Containing element.</param>
		/// <param name="options">Dictionary of options.</param>
		/// <returns>Newly created SVG image.</returns>
		private static SVGElement createDom_(Element container, Options options)
		{
			// Sadly browsers (Chrome vs Firefox) are currently inconsistent in laying
			// out content in RTL mode.  Therefore Blockly forces the use of LTR,
			// then manually positions content in RTL as needed.
			container.SetAttribute("dir", "LTR");
			// Closure can be trusted to create HTML widgets with the proper direction.
			goog.ui.Component.setDefaultRightToLeft(options.RTL);

			// Load CSS.
			Css.inject(options.hasCss, options.pathToMedia);
#if false
			// Build the SVG DOM.
			/*
			<svg
			  xmlns="http://www.w3.org/2000/svg"
			  xmlns:html="http://www.w3.org/1999/xhtml"
			  xmlns:xlink="http://www.w3.org/1999/xlink"
			  version="1.1"
			  class="blocklySvg">
			  ...
			</svg>
			*/
			var svg = Core.createSvgElement("svg", new Dictionary<string, object>(){
				{ "xmlns", "http://www.w3.org/2000/svg" },
				{ "xmlns:html", "http://www.w3.org/1999/xhtml" },
				{ "xmlns:xlink", "http://www.w3.org/1999/xlink" },
				{ "version", "1.1" },
				{ "class", "blocklySvg" }
				}, container);
			/*
			<defs>
			  ... filters go here ...
			</defs>
			*/
			var defs = Core.createSvgElement("defs", new Dictionary<string, object>(), svg);
			var rnd = Script.Random().ToString().Substring(2);
			/*
			  <filter id="blocklyEmbossFilter837493">
				<feGaussianBlur in="SourceAlpha" stdDeviation="1" result="blur"/>
				<feSpecularLighting in="blur" surfaceScale="1" specularConstant="0.5"
									specularExponent="10" lighting-color="white"
									result="specOut">
				  <fePointLight x="-5000" y="-10000" z="20000"/>
				</feSpecularLighting>
				<feComposite in="specOut" in2="SourceAlpha" operator="in"
							 result="specOut"/>
				<feComposite in="SourceGraphic" in2="specOut" operator="arithmetic"
							 k1="0" k2="1" k3="1" k4="0"/>
			  </filter>
			*/
			var embossFilter = Core.createSvgElement("filter", new Dictionary<string, object>() {
				{ "id", "blocklyEmbossFilter" + rnd} }, defs);
			Core.createSvgElement("feGaussianBlur", new Dictionary<string, object>() {
				{ "in", "SourceAlpha" }, {"stdDeviation", 1 }, {"result", "blur" } }, embossFilter);
			var feSpecularLighting = Core.createSvgElement("feSpecularLighting", new Dictionary<string, object>() {
				{ "in", "blur" }, {"surfaceScale", 1 }, {"specularConstant", 0.5 },
				{ "specularExponent", 10 }, {"lighting-color", "white" }, {"result", "specOut" } },
				embossFilter);
			Core.createSvgElement("fePointLight", new Dictionary<string, object>() {
				{ "x", -5000 }, {"y", -10000 }, {"z", 20000 } }, feSpecularLighting);
			Core.createSvgElement("feComposite", new Dictionary<string, object>() {
				{ "in", "specOut" }, {"in2", "SourceAlpha" }, {"operator", "in" },
				{ "result", "specOut" } }, embossFilter);
			Core.createSvgElement("feComposite", new Dictionary<string, object>() {
				{ "in", "SourceGraphic" }, {"in2", "specOut" }, {"operator", "arithmetic" },
				{ "k1", 0 }, {"k2", 1 }, {"k3", 1 }, {"k4", 0 } }, embossFilter);
			options.embossFilterId = embossFilter.Id;
			/*
			  <pattern id="blocklyDisabledPattern837493" patternUnits="userSpaceOnUse"
					   width="10" height="10">
				<rect width="10" height="10" fill="#aaa" />
				<path d="M 0 0 L 10 10 M 10 0 L 0 10" stroke="#cc0" />
			  </pattern>
			*/
			var disabledPattern = Core.createSvgElement("pattern", new Dictionary<string, object>() {
				{ "id", "blocklyDisabledPattern" + rnd },
				{ "patternUnits", "userSpaceOnUse" },
				{ "width", 10 }, {"height", 10 } }, defs);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
				{ "width", 10 }, {"height", 10 }, {"fill", "#aaa" } }, disabledPattern);
			Core.createSvgElement("path", new Dictionary<string, object>() {
				{ "d", "M 0 0 L 10 10 M 10 0 L 0 10" }, {"stroke", "#cc0" } }, disabledPattern);
			options.disabledPatternId = disabledPattern.Id;
			/*
			  <pattern id="blocklyGridPattern837493" patternUnits="userSpaceOnUse">
				<rect stroke="#888" />
				<rect stroke="#888" />
			  </pattern>
			*/
			var gridPattern = Core.createSvgElement("pattern", new Dictionary<string, object>() {
				{ "id", "blocklyGridPattern" + rnd },
				{ "patternUnits", "userSpaceOnUse"} }, defs);
			if (options.gridOptions.length > 0 && options.gridOptions.spacing > 0) {
				Core.createSvgElement("line", new Dictionary<string, object>() {
					{ "stroke", options.gridOptions.colour} },
					gridPattern);
				if (options.gridOptions.length > 1) {
					Core.createSvgElement("line", new Dictionary<string, object>() {
						{ "stroke", options.gridOptions.colour} },
						gridPattern);
				}
				// x1, y1, x1, x2 properties will be set later in updateGridPattern_.
			}
			options.gridPattern = gridPattern;
#else
			var opt = Script.NewObject();
			var gridOptions = Script.NewObject();
			Script.Set(gridOptions, "spacing", options.gridOptions.spacing);
			Script.Set(gridOptions, "colour", options.gridOptions.colour);
			Script.Set(gridOptions, "length", options.gridOptions.length);
			Script.Set(gridOptions, "snap", options.gridOptions.snap);
			Script.Set(opt, "gridOptions", gridOptions);
			var svg = SVGElement.Create(Script.CreateSvgDom(container.Instance, opt));
			options.embossFilterId = (string)Script.Get(opt, "embossFilterId");
			options.disabledPatternId = (string)Script.Get(opt, "disabledPatternId");
			options.gridPattern = SVGElement.Create(Script.Get(opt, "gridPattern"));
#endif
			return svg;
		}

		/// <summary>
		/// Create a main workspace and add it to the SVG.
		/// </summary>
		/// <param name="svg">SVG element with pattern defined.</param>
		/// <param name="options">Dictionary of options.</param>
		/// <returns>Newly created main workspace.</returns>
		private static WorkspaceSvg createMainWorkspace_(SVGElement svg, Options options)
		{
			options.parentWorkspace = null;
			var mainWorkspace = new WorkspaceSvg(options);
			mainWorkspace.scale = options.zoomOptions.startScale;
			svg.AppendChild(mainWorkspace.createDom("blocklyMainBackground"));
			// A null translation will also apply the correct initial scale.
			mainWorkspace.translate(0, 0);
			mainWorkspace.markFocused(null);

			if (!options.readOnly && !options.hasScrollbars) {
				var workspaceChanged = new Action<Events.Abstract>((e) => {
					if (Core.dragMode_ == Core.DRAG_NONE) {
						var metrics = mainWorkspace.getMetrics();
						var edgeLeft = metrics.viewLeft + metrics.absoluteLeft;
						var edgeTop = metrics.viewTop + metrics.absoluteTop;
						if (metrics.contentTop < edgeTop ||
							metrics.contentTop + metrics.contentHeight >
							metrics.viewHeight + edgeTop ||
							metrics.contentLeft <
								(options.RTL ? metrics.viewLeft : edgeLeft) ||
							metrics.contentLeft + metrics.contentWidth > (options.RTL ?
								metrics.viewWidth : metrics.viewWidth + edgeLeft)) {
							// One or more blocks may be out of bounds.  Bump them back in.
							var MARGIN = 25;
							var blocks = mainWorkspace.getTopBlocks(false);
							foreach (var block in blocks) {
								var blockXY = block.getRelativeToSurfaceXY();
								var blockHW = ((BlockSvg)block).getHeightWidth();
								// Bump any block that's above the top back inside.
								var overflowTop = edgeTop + MARGIN - blockHW.height - blockXY.y;
								if (overflowTop > 0) {
									block.moveBy(0, overflowTop);
								}
								// Bump any block that's below the bottom back inside.
								var overflowBottom =
									edgeTop + metrics.viewHeight - MARGIN - blockXY.y;
								if (overflowBottom < 0) {
									block.moveBy(0, overflowBottom);
								}
								// Bump any block that's off the left back inside.
								var overflowLeft = MARGIN + edgeLeft -
									blockXY.x - (options.RTL ? 0 : blockHW.width);
								if (overflowLeft > 0) {
									block.moveBy(overflowLeft, 0);
								}
								// Bump any block that's off the right back inside.
								var overflowRight = edgeLeft + metrics.viewWidth - MARGIN -
									blockXY.x + (options.RTL ? blockHW.width : 0);
								if (overflowRight < 0) {
									block.moveBy(overflowRight, 0);
								}
							}
						}
					}
				});
				mainWorkspace.addChangeListener(workspaceChanged);
			}
			// The SVG is now fully assembled.
			Core.svgResize(mainWorkspace);
			WidgetDiv.createDom();
			Tooltip.createDom();
			return mainWorkspace;
		}

		/// <summary>
		/// Initialize Blockly with various handlers.
		/// </summary>
		/// <param name="mainWorkspace">Newly created main workspace.</param>
		private static void init_(WorkspaceSvg mainWorkspace)
		{
			var options = mainWorkspace.options;
			var svg = mainWorkspace.getParentSvg();

			// Supress the browser's context menu.
			Core.bindEventWithChecks_(svg, "contextmenu", null,
				new Action<Event>((e) => {
					if (!Core.isTargetInput_(e)) {
						e.PreventDefault();
					}
				}));

			var workspaceResizeHandler = Core.bindEventWithChecks_(new Node(Window.Instance.instance), "resize",
				null,
				new Action<Event>((e) => {
					Core.hideChaff(true);
					Core.svgResize(mainWorkspace);
				}));
			mainWorkspace.setResizeHandlerWrapper(workspaceResizeHandler);

			Core.inject_bindDocumentEvents_();

			if (options.languageTree != null) {
				if (mainWorkspace.toolbox_ != null) {
					mainWorkspace.toolbox_.init(/*mainWorkspace*/);
				}
				else if (mainWorkspace.flyout_ != null) {
					// Build a fixed flyout with the root blocks.
					mainWorkspace.flyout_.init(mainWorkspace);
					mainWorkspace.flyout_.show(options.languageTree.ChildNodes);
					mainWorkspace.flyout_.scrollToStart();
					// Translate the workspace sideways to avoid the fixed flyout.
					mainWorkspace.scrollX = mainWorkspace.flyout_.width_;
					if (options.toolboxPosition == Core.TOOLBOX_AT_RIGHT) {
						mainWorkspace.scrollX *= -1;
					}
					mainWorkspace.translate(mainWorkspace.scrollX, 0);
				}
			}

			if (options.hasScrollbars) {
				mainWorkspace.scrollbar = new ScrollbarPair(mainWorkspace);
				mainWorkspace.scrollbar.resize();
			}

			// Load the sounds.
			if (options.hasSounds) {
				Core.inject_loadSounds_(options.pathToMedia, mainWorkspace);
			}
		}

		private static bool documentEventsBound_;

		/// <summary>
		/// Bind document events, but only once.  Destroying and reinjecting Blockly
		/// should not bind again.
		/// Bind events for scrolling the workspace.
		/// Most of these events should be bound to the SVG's surface.
		/// However, "mouseup" has to be on the whole document so that a block dragged
		/// out of bounds and released will know that it has been released.
		/// Also, "keydown" has to be on the whole document since the browser doesn"t
		/// understand a concept of focus on the SVG image.
		/// </summary>
		private static void inject_bindDocumentEvents_()
		{
			if (!Core.documentEventsBound_) {
				Core.bindEventWithChecks_(Document.Instance, "keydown", null, new Action<KeyboardEvent>(Core.onKeyDown_));
				Core.bindEventWithChecks_(Document.Instance, "touchend", null, new Action<Event>(Core.longStop_));
				Core.bindEventWithChecks_(Document.Instance, "touchcancel", null,
					new Action<Event>(Core.longStop_));
				// Don't use bindEvent_ for document's mouseup since that would create a
				// corresponding touch handler that would squeltch the ability to interact
				// with non-Blockly elements.
				Document.AddEventListener("mouseup", new Action<Event>(Core.onMouseUp_), false);
				// Some iPad versions don't fire resize after portrait to landscape change.
				if (goog.userAgent.IPAD) {
					Core.bindEventWithChecks_(new Node(Window.Instance), "orientationchange", Document.Instance,
						new Action<Event>((e) => {
							// TODO(#397): Fix for multiple blockly workspaces.
							Core.svgResize(Core.getMainWorkspace());
						}));
				}
			}
			Core.documentEventsBound_ = true;
		}

		/// <summary>
		/// Load sounds for the given workspace.
		/// </summary>
		/// <param name="pathToMedia">The path to the media directory.</param>
		/// <param name="workspace">The workspace to load sounds for.</param>
		private static void inject_loadSounds_(string pathToMedia, WorkspaceSvg workspace)
		{
			workspace.loadAudio_(new string[]
				{ pathToMedia + "click.mp3",
				  pathToMedia + "click.wav",
				  pathToMedia + "click.ogg"}, "click");
			workspace.loadAudio_(new string[]
				{ pathToMedia + "disconnect.wav",
				  pathToMedia + "disconnect.mp3",
				  pathToMedia + "disconnect.ogg"}, "disconnect");
			workspace.loadAudio_(new string[]
				{ pathToMedia + "delete.mp3",
				  pathToMedia + "delete.ogg",
				  pathToMedia + "delete.wav"}, "delete");

			// Bind temporary hooks that preload the sounds.
			var soundBinds = new JsArray<JsArray<EventWrapInfo>>();
			var unbindSounds = new Action<Event>((e) => {
				while (soundBinds.Length != 0) {
					Core.unbindEvent_((JsArray<EventWrapInfo>)soundBinds.Pop());
				}
				workspace.preloadAudio_();
			});

			// These are bound on mouse/touch events with Blockly.bindEventWithChecks_, so
			// they restrict the touch identifier that will be recognized.  But this is
			// really something that happens on a click, not a drag, so that's not
			// necessary.

			// Android ignores any sound not loaded as a result of a user action.
			soundBinds.Push(
				Core.bindEventWithChecks_(Document.Instance, "mousemove", null, unbindSounds,
					true));
			soundBinds.Push(
				Core.bindEventWithChecks_(Document.Instance, "touchstart", null, unbindSounds,
					true));
		}

		/// <summary>
		/// Modify the block tree on the existing toolbox.
		/// </summary>
		/// <param name="tree">DOM tree of blocks, or text representation of same.</param>
		public static void updateToolbox(Union<string, Element> tree)
		{
			Console.WriteLine("Deprecated call to Blockly.updateToolbox, " +
						"use workspace.updateToolbox instead.");
			Core.getMainWorkspace().updateToolbox(tree);
		}
	}
}
