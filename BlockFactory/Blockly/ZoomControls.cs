/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2015 Google Inc.
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
 * @fileoverview Object representing a zoom icons.
 * @author carloslfu@gmail.com (Carlos Galarza)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class ZoomControls
	{
		private WorkspaceSvg workspace_;

		/// <summary>
		/// Class for a zoom controls.
		/// </summary>
		/// <param name="workspace">The workspace to sit in.</param>
		public ZoomControls(WorkspaceSvg workspace)
		{
			this.workspace_ = workspace;
		}

		/// <summary>
		/// Width of the zoom controls.
		/// </summary>
		private int WIDTH_ = 32;

		/// <summary>
		/// Height of the zoom controls.
		/// </summary>
		private int HEIGHT_ = 110;

		/// <summary>
		/// Distance between zoom controls and bottom edge of workspace.
		/// </summary>
		private int MARGIN_BOTTOM_ = 20;

		/// <summary>
		/// Distance between zoom controls and right edge of workspace.
		/// </summary>
		private int MARGIN_SIDE_ = 20;

		/// <summary>
		/// The SVG group containing the zoom controls.
		/// </summary>
		private SVGElement svgGroup_;

		/// <summary>
		/// Left coordinate of the zoom controls.
		/// </summary>
		private double left_ = 0;

		/// <summary>
		/// Top coordinate of the zoom controls.
		/// </summary>
		private double top_ = 0;

		/// <summary>
		/// Create the zoom controls.
		/// </summary>
		/// <returns>The zoom controls SVG group.</returns>
		public SVGElement createDom()
		{
			var workspace = this.workspace_;
			/* Here's the markup that will be generated:
			<g class="blocklyZoom">
			  <clippath id="blocklyZoomoutClipPath837493">
				<rect width="32" height="32" y="77"></rect>
			  </clippath>
			  <image width="96" height="124" x="-64" y="-15" xlink:href="media/sprites.png"
				  clip-path="url(#blocklyZoomoutClipPath837493)"></image>
			  <clippath id="blocklyZoominClipPath837493">
				<rect width="32" height="32" y="43"></rect>
			  </clippath>
			  <image width="96" height="124" x="-32" y="-49" xlink:href="media/sprites.png"
				  clip-path="url(#blocklyZoominClipPath837493)"></image>
			  <clippath id="blocklyZoomresetClipPath837493">
				<rect width="32" height="32"></rect>
			  </clippath>
			  <image width="96" height="124" y="-92" xlink:href="media/sprites.png"
				  clip-path="url(#blocklyZoomresetClipPath837493)"></image>
			</g>
			*/
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyZoom"}}, null);
			var rnd = Script.Random().ToString().Substring(2);

			var clip = Core.createSvgElement("clipPath", new Dictionary<string, object>() {
					{"id", "blocklyZoomoutClipPath" + rnd} },
				this.svgGroup_);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"width", 32 }, {"height", 32 }, {"y", 77} },
				clip);
			var zoomoutSvg = Core.createSvgElement("image", new Dictionary<string, object>() {
					{"width", Core.SPRITE.width},
				{ "height", Core.SPRITE.height }, {"x", -64 },
				{ "y", -15 },
				{ "clip-path", "url(#blocklyZoomoutClipPath" + rnd + ")"}},
			this.svgGroup_);
			zoomoutSvg.SetAttributeNS("http://www.w3.org/1999/xlink", "xlink:href",
				workspace.options.pathToMedia + Core.SPRITE.url);

			clip = Core.createSvgElement("clipPath", new Dictionary<string, object>() {
					{"id", "blocklyZoominClipPath" + rnd}},
				this.svgGroup_);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"width", 32 }, {"height", 32 }, {"y", 43} },
				clip);
			var zoominSvg = Core.createSvgElement("image", new Dictionary<string, object>() {
					{"width", Core.SPRITE.width},
					{"height", Core.SPRITE.height },
					{"x", -32},
					{"y", -49},
					{"clip-path", "url(#blocklyZoominClipPath" + rnd + ")"}},
				this.svgGroup_);
			zoominSvg.SetAttributeNS("http://www.w3.org/1999/xlink", "xlink:href",
				workspace.options.pathToMedia + Core.SPRITE.url);

			clip = Core.createSvgElement("clipPath", new Dictionary<string, object>() {
					{"id", "blocklyZoomresetClipPath" + rnd}},
				this.svgGroup_);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{ "width", 32}, {"height", 32} },
				clip);
			var zoomresetSvg = Core.createSvgElement("image", new Dictionary<string, object>() {
					{"width", Core.SPRITE.width },
					{ "height", Core.SPRITE.height }, {"y", -92},
					{ "clip-path", "url(#blocklyZoomresetClipPath" + rnd + ")"}},
				this.svgGroup_);
			zoomresetSvg.SetAttributeNS("http://www.w3.org/1999/xlink", "xlink:href",
				workspace.options.pathToMedia + Core.SPRITE.url);

			// Attach event listeners.
			Core.bindEventWithChecks_(zoomresetSvg, "mousedown", null, new Action<Event>((e) => {
				workspace.setScale((double)workspace.options.zoomOptions.startScale);
				workspace.scrollCenter();
				Touch.clearTouchIdentifier(); // Don't block future drags.
				e.StopPropagation();  // Don't start a workspace scroll.
				e.PreventDefault();  // Stop double-clicking from selecting text.
			}));
			Core.bindEventWithChecks_(zoominSvg, "mousedown", null, new Action<Event>((e) => {
				workspace.zoomCenter(1);
				Touch.clearTouchIdentifier(); // Don't block future drags.
				e.StopPropagation();  // Don't start a workspace scroll.
				e.PreventDefault();  // Stop double-clicking from selecting text.
			}));
			Core.bindEventWithChecks_(zoomoutSvg, "mousedown", null, new Action<Event>((e) => {
				workspace.zoomCenter(-1);
				Touch.clearTouchIdentifier(); // Don't block future drags.
				e.StopPropagation();  // Don't start a workspace scroll.
				e.PreventDefault();  // Stop double-clicking from selecting text.
			}));

			return this.svgGroup_;
		}

		private double bottom_;

		/// <summary>
		/// Initialize the zoom controls.
		/// </summary>
		/// <param name="bottom">Distance from workspace bottom to bottom of controls.</param>
		/// <returns>Distance from workspace bottom to the top of controls.</returns>
		public double init(double bottom)
		{
			this.bottom_ = this.MARGIN_BOTTOM_ + bottom;
			return this.bottom_ + this.HEIGHT_;
		}

		public void dispose()
		{
			if (this.svgGroup_ != null) {
				goog.dom.removeNode(this.svgGroup_);
				this.svgGroup_ = null;
			}
			this.workspace_ = null;
		}

		public void position()
		{
			var metrics = this.workspace_.getMetrics();
			if (metrics == null) {
				// There are no metrics available (workspace is probably not visible).
				return;
			}
			if (this.workspace_.RTL) {
				this.left_ = this.MARGIN_SIDE_ + Scrollbar.scrollbarThickness;
				if (metrics.toolboxPosition == Core.TOOLBOX_AT_LEFT) {
					this.left_ += metrics.flyoutWidth;
					if (this.workspace_.toolbox_ != null) {
						this.left_ += metrics.absoluteLeft;
					}
				}
			}
			else {
				this.left_ = metrics.viewWidth + metrics.absoluteLeft -
					this.WIDTH_ - this.MARGIN_SIDE_ - Scrollbar.scrollbarThickness;

				if (metrics.toolboxPosition == Core.TOOLBOX_AT_RIGHT) {
					this.left_ -= metrics.flyoutWidth;
				}
			}
			this.top_ = metrics.viewHeight + metrics.absoluteTop -
				this.HEIGHT_ - this.bottom_;
			if (metrics.toolboxPosition == Core.TOOLBOX_AT_BOTTOM) {
				this.top_ -= metrics.flyoutHeight;
			}
			this.svgGroup_.SetAttribute("transform",
				"translate(" + this.left_ + "," + this.top_ + ")");
		}
	}
}
