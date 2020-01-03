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
 * @fileoverview Image field.  Used for titles, labels, etc.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldImage : Field
	{
		private double width_;
		private double height_;

		/// <summary>
		/// Class for an image.
		/// </summary>
		/// <param name="src">The URL of the image.</param>
		/// <param name="width">Width of the image.</param>
		/// <param name="height">Height of the image.</param>
		/// <param name="opt_alt">Optional alt text for when block is collapsed.</param>
		public FieldImage(string src, double width, double height, string opt_alt = null)
			: base(src)
		{
			this.sourceBlock_ = null;
			// Ensure height and width are numbers.  Strings are bad at math.
			this.height_ = height;
			this.width_ = width;
			this.size_ = new goog.math.Size(this.width_,
				this.height_ + 2 * BlockSvg.INLINE_PADDING_Y);
			this.text_ = opt_alt ?? "";
			this.setValue(src);
		}

		/// <summary>
		/// Rectangular mask used by Firefox.
		/// </summary>
		private SVGElement rectElement_;

		private SVGElement imageElement_;

		public new bool EDITABLE = false;

		private string src_;

		/// <summary>
		/// Install this image on a block.
		/// </summary>
		public override void init()
		{
			if (this.fieldGroup_ != null) {
				// Image has already been initialized once.
				return;
			}
			// Build the DOM.
			/** @type {SVGElement} */
			this.fieldGroup_ = Core.createSvgElement("g", new Dictionary<string, object>(), null);
			if (!this.visible_) {
				this.fieldGroup_.style.Display = Display.None;
			}
			/** @type {SVGElement} */
			this.imageElement_ = Core.createSvgElement("image", new Dictionary<string, object>() {
				{"height", this.height_ + "px" },
				{ "width", this.width_ + "px"}}, this.fieldGroup_);
			this.setValue(this.src_);
			if (goog.userAgent.GECKO) {
				/**
				 * Due to a Firefox bug which eats mouse events on image elements,
				 * a transparent rectangle needs to be placed on top of the image.
				 * @type {SVGElement}
				 */
				this.rectElement_ = Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"height", this.height_ + "px" },
					{ "width", this.width_ + "px" },
					{ "fill-opacity", 0}}, this.fieldGroup_);
			}
			this.sourceBlock_.getSvgRoot().AppendChild(this.fieldGroup_);

			// Configure the field to be transparent with respect to tooltips.
			var topElement = this.rectElement_ ?? this.imageElement_;
			topElement.tooltip = this.sourceBlock_;
			Tooltip.bindMouseEvents(topElement);
		}

		/// <summary>
		/// Dispose of all DOM objects belonging to this text.
		/// </summary>
		public override void dispose()
		{
			goog.dom.removeNode(this.fieldGroup_);
			this.fieldGroup_ = null;
			this.imageElement_ = null;
			this.rectElement_ = null;
		}

		/// <summary>
		/// Change the tooltip text for this field.
		/// </summary>
		/// <param name="newTip">Text for tooltip or a parent element to
		/// link to for its tooltip.</param>
		public override void setTooltip(BlockSvg newTip)
		{
			var topElement = this.rectElement_ ?? this.imageElement_;
			topElement.tooltip = newTip;
		}

		/// <summary>
		/// Get the source URL of this image.
		/// </summary>
		/// <returns>Current text.</returns>
		public override string getValue()
		{
			return this.src_;
		}

		/// <summary>
		/// Set the source URL of this image.
		/// </summary>
		/// <param name="newText">New source.</param>
		public override void setValue(string src)
		{
			if (src == null) {
				// No change if null.
				return;
			}
			this.src_ = src;
			if (this.imageElement_ != null) {
				this.imageElement_.SetAttributeNS("http://www.w3.org/1999/xlink",
					"xlink:href", src is string ? src : "");
			}
		}

		/// <summary>
		/// Set the alt text of this image.
		/// </summary>
		/// <param name="alt">New alt text.</param>
		public override void setText(string alt)
		{
			if (alt == null) {
				// No change if null.
				return;
			}
			this.text_ = alt;
		}

		/// <summary>
		/// Images are fixed width, no need to render.
		/// </summary>
		protected override void render_()
		{
			// NOP
		}

		public override void showEditor_(bool opt_quietInput)
		{
		}
	}
}
