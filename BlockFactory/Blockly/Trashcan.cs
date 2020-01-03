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
 * @fileoverview Object representing a trash can icon.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Trashcan
	{
		private WorkspaceSvg workspace_;

		/// <summary>
		/// Class for a trash can.
		/// </summary>
		/// <param name="workspace">The workspace to sit in.</param>
		public Trashcan(WorkspaceSvg workspace)
		{
			this.workspace_ = workspace;
		}

		/// <summary>
		/// Width of both the trash can and lid images.
		/// </summary>
		private int WIDTH_ = 47;

		/// <summary>
		/// Height of the trashcan image (minus lid).
		/// </summary>
		private int BODY_HEIGHT_ = 44;

		/// <summary>
		/// Height of the lid image.
		/// </summary>
		private int LID_HEIGHT_ = 16;

		/// <summary>
		/// Distance between trashcan and bottom edge of workspace.
		/// </summary>
		private int MARGIN_BOTTOM_ = 20;

		/// <summary>
		/// Distance between trashcan and right edge of workspace.
		/// </summary>
		private int MARGIN_SIDE_ = 20;

		/// <summary>
		/// Extent of hotspot on all sides beyond the size of the image.
		/// </summary>
		private int MARGIN_HOTSPOT_ = 10;

		/// <summary>
		/// Location of trashcan in sprite image.
		/// </summary>
		private int SPRITE_LEFT_ = 0;

		/// <summary>
		/// Location of trashcan in sprite image.
		/// </summary>
		private int SPRITE_TOP_ = 32;

		/// <summary>
		/// Current open/close state of the lid.
		/// </summary>
		public bool isOpen;

		/// <summary>
		/// The SVG group containing the trash can.
		/// </summary>
		private SVGElement svgGroup_;

		/// <summary>
		/// The SVG image element of the trash can lid.
		/// </summary>
		private SVGElement svgLid_;

		/// <summary>
		/// Task ID of opening/closing animation.
		/// </summary>
		private int lidTask_;

		/// <summary>
		/// Current state of lid opening (0.0 = closed, 1.0 = open).
		/// </summary>
		private double lidOpen_;

		/// <summary>
		/// Left coordinate of the trash can.
		/// </summary>
		private double left_;

		/// <summary>
		/// Top coordinate of the trash can.
		/// </summary>
		private double top_;

		/// <summary>
		/// Create the trash can elements.
		/// </summary>
		/// <returns>The trash can's SVG group.</returns>
		public SVGElement createDom()
		{
			/* Here's the markup that will be generated:
			<g class="blocklyTrash">
			  <clippath id="blocklyTrashBodyClipPath837493">
				<rect width="47" height="45" y="15"></rect>
			  </clippath>
			  <image width="64" height="92" y="-32" xlink:href="media/sprites.png"
				  clip-path="url(#blocklyTrashBodyClipPath837493)"></image>
			  <clippath id="blocklyTrashLidClipPath837493">
				<rect width="47" height="15"></rect>
			  </clippath>
			  <image width="84" height="92" y="-32" xlink:href="media/sprites.png"
				  clip-path="url(#blocklyTrashLidClipPath837493)"></image>
			</g>
			*/
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", "blocklyTrash"}}, null);
			var rnd = Script.Random().ToString().Substring(2);
			var clip = Core.createSvgElement("clipPath", new Dictionary<string, object>() {
				{"id", "blocklyTrashBodyClipPath" + rnd} },
				this.svgGroup_);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"width", this.WIDTH_ }, {"height", this.BODY_HEIGHT_ },
				{ "y", this.LID_HEIGHT_} },
				clip);
			var body = Core.createSvgElement("image", new Dictionary<string, object>() {
				{"width", Core.SPRITE.width }, {"x", -this.SPRITE_LEFT_ },
				{ "height", Core.SPRITE.height }, {"y", -this.SPRITE_TOP_ },
				{ "clip-path", "url(#blocklyTrashBodyClipPath" + rnd + ")"} },
				this.svgGroup_);
			body.SetAttributeNS("http://www.w3.org/1999/xlink", "xlink:href",
				this.workspace_.options.pathToMedia + Core.SPRITE.url);

			clip = Core.createSvgElement("clipPath", new Dictionary<string, object>() {
				{"id", "blocklyTrashLidClipPath" + rnd} },
				this.svgGroup_);
			Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"width", this.WIDTH_ }, {"height", this.LID_HEIGHT_} }, clip);
			this.svgLid_ = Core.createSvgElement("image", new Dictionary<string, object>() {
				{"width", Core.SPRITE.width }, {"x", -this.SPRITE_LEFT_ },
				{ "height", Core.SPRITE.height }, {"y", -this.SPRITE_TOP_ },
				{ "clip-path", "url(#blocklyTrashLidClipPath" + rnd + ")"} },
				this.svgGroup_);
			this.svgLid_.SetAttributeNS("http://www.w3.org/1999/xlink", "xlink:href",
				this.workspace_.options.pathToMedia + Core.SPRITE.url);

			Core.bindEventWithChecks_(this.svgGroup_, "mouseup", this, new Action<Event>(this.click));
			this.animateLid_();
			return this.svgGroup_;
		}

		private double bottom_;

		/// <summary>
		/// Initialize the trash can.
		/// </summary>
		/// <param name="bottom">Distance from workspace bottom to bottom of trashcan.</param>
		/// <returns>Distance from workspace bottom to the top of trashcan.</returns>
		public double init(double bottom)
		{
			this.bottom_ = this.MARGIN_BOTTOM_ + bottom;
			this.setOpen_(false);
			return this.bottom_ + this.BODY_HEIGHT_ + this.LID_HEIGHT_;
		}

		/// <summary>
		/// Dispose of this trash can.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public void dispose()
		{
			if (this.svgGroup_ != null) {
				goog.dom.removeNode(this.svgGroup_);
				this.svgGroup_ = null;
			}
			this.svgLid_ = null;
			this.workspace_ = null;
			goog.Timer.clear(this.lidTask_);
		}

		/// <summary>
		/// Move the trash can to the bottom-right corner.
		/// </summary>
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
				(this.BODY_HEIGHT_ + this.LID_HEIGHT_) - this.bottom_;

			if (metrics.toolboxPosition == Core.TOOLBOX_AT_BOTTOM) {
				this.top_ -= metrics.flyoutHeight;
			}
			this.svgGroup_.SetAttribute("transform",
				"translate(" + this.left_ + "," + this.top_ + ")");
		}

		/// <summary>
		/// Return the deletion rectangle for this trash can.
		/// </summary>
		/// <returns>Rectangle in which to delete.</returns>
		public goog.math.Rect getClientRect()
		{
			if (this.svgGroup_ == null) {
				return null;
			}

			var trashRect = this.svgGroup_.GetBoundingClientRect();
			var left = trashRect.Left + this.SPRITE_LEFT_ - this.MARGIN_HOTSPOT_;
			var top = trashRect.Top + this.SPRITE_TOP_ - this.MARGIN_HOTSPOT_;
			var width = this.WIDTH_ + 2 * this.MARGIN_HOTSPOT_;
			var height = this.LID_HEIGHT_ + this.BODY_HEIGHT_ + 2 * this.MARGIN_HOTSPOT_;
			return new goog.math.Rect(left, top, width, height);
		}


		/// <summary>
		/// Flip the lid open or shut.
		/// </summary>
		/// <param name="state">True if open.</param>
		internal void setOpen_(bool state)
		{
			if (this.isOpen == state) {
				return;
			}
			goog.Timer.clear(this.lidTask_);
			this.isOpen = state;
			this.animateLid_();
		}

		/// <summary>
		/// Rotate the lid open or closed by one step.  Then wait and recurse.
		/// </summary>
		private void animateLid_()
		{
			this.lidOpen_ += this.isOpen ? 0.2 : -0.2;
			this.lidOpen_ = goog.math.clamp(this.lidOpen_, 0, 1);
			var lidAngle = this.lidOpen_ * 45;
			this.svgLid_.SetAttribute("transform", "rotate(" +
				(this.workspace_.RTL ? -lidAngle : lidAngle) + "," +
				(this.workspace_.RTL ? 4 : this.WIDTH_ - 4) + "," +
				(this.LID_HEIGHT_ - 2) + ")");
			var opacity = goog.math.lerp(0.4, 0.8, this.lidOpen_);
			this.svgGroup_.style.Opacity = opacity.ToString();
			if (this.lidOpen_ > 0 && this.lidOpen_ < 1) {
				this.lidTask_ = goog.Timer.callOnce(this.animateLid_, 20, this);
			}
		}

		/// <summary>
		/// Flip the lid shut.
		/// Called externally after a drag.
		/// </summary>
		public void close()
		{
			this.setOpen_(false);
		}

		/// <summary>
		/// Inspect the contents of the trash.
		/// </summary>
		public void click(Event e)
		{
			var dx = this.workspace_.startScrollX - this.workspace_.scrollX;
			var dy = this.workspace_.startScrollY - this.workspace_.scrollY;
			if (System.Math.Sqrt(dx * dx + dy * dy) > Core.DRAG_RADIUS) {
				return;
			}
			Console.WriteLine("TODO: Inspect trash.");
		}
	}
}
