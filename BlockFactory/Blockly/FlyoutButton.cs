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
 * @fileoverview Class for a button in the flyout.
 * @author fenichel@google.com (Rachel Fenichel)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FlyoutButton
	{
		private WorkspaceSvg workspace_;
		private WorkspaceSvg targetWorkspace_;
		private string text_;
		private goog.math.Coordinate position_;
		private Action<FlyoutButton> callback_;
		private bool isLabel_;

		/// <summary>
		/// Class for a button in the flyout.
		/// </summary>
		/// <param name="workspace">The workspace in which to place this
		/// button.</param>
		/// <param name="targetWorkspace">The flyout's target workspace.</param>
		/// <param name="text">The text to display on the button.</param>
		public FlyoutButton(WorkspaceSvg workspace, WorkspaceSvg targetWorkspace, string text,
			string d = null, bool label = false)
		{
			this.workspace_ = workspace;
			this.targetWorkspace_ = targetWorkspace;
			this.text_ = text;
			position_ = new goog.math.Coordinate(0.0, 0.0);
			this.callback_ = Core.flyoutButtonCallbacks_[d];
			this.isLabel_ = label;
		}

		/// <summary>
		/// The margin around the text in the button.
		/// </summary>
		public static double MARGIN = 5.0;

		/// <summary>
		/// The width of the button's rect.
		/// </summary>
		public double width;

		/// <summary>
		/// The height of the button's rect.
		/// </summary>
		public double height;

		private SVGElement svgGroup_;

		/// <summary>
		/// Create the button elements.
		/// </summary>
		/// <returns>The button's SVG group.</returns>
		public SVGElement createDom()
		{
			this.svgGroup_ = Core.createSvgElement("g", new Dictionary<string, object>() {
				{"class", this.isLabel_ ? "blocklyFlyoutLabel" : "blocklyFlyoutButton"}}, this.workspace_.getCanvas());

			SVGElement shadow = null;
			if (!this.isLabel_) {
				// Shadow rectangle (light source does not mirror in RTL).
				shadow = Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"class", "blocklyFlyoutButtonShadow" },
					{ "rx", 4 }, {"ry", 4 }, {"x", 1 }, {"y", 1}},
				 this.svgGroup_);
			}
			// Background rectangle.
			var rect = Core.createSvgElement("rect", new Dictionary<string, object>() {
				{"class", this.isLabel_ ? "blocklyFlyoutLabelBackground" : "blocklyFlyoutButtonBackground"},
				{ "rx", 4 }, {"ry", 4}},
				this.svgGroup_);
			var svgText = Core.createSvgElement("text", new Dictionary<string, object>() {
				{"class", this.isLabel_ ? "blocklyFlyoutLabelText" : "blocklyText" },
				{ "x", 0 }, {"y", 0 },
				{ "text-anchor", "middle"}}, this.svgGroup_);
			svgText.TextContent = this.text_;

			this.width = svgText.getComputedTextLength() + 2 * FlyoutButton.MARGIN;
			this.height = 20;  // Can't compute it :(

			if (!this.isLabel_) {
				shadow.SetAttribute("width", this.width.ToString());
				shadow.SetAttribute("height", this.height.ToString());
			}

			rect.SetAttribute("width", this.width.ToString());
			rect.SetAttribute("height", this.height.ToString());

			svgText.SetAttribute("x", (this.width / 2).ToString());
			svgText.SetAttribute("y", (this.height - FlyoutButton.MARGIN).ToString());

			this.updateTransform_();
			return this.svgGroup_;
		}

		/// <summary>
		/// Correctly position the flyout button and make it visible.
		/// </summary>
		public void show()
		{
			this.updateTransform_();
			this.svgGroup_.SetAttribute("display", "block");
		}

		/// <summary>
		/// Update svg attributes to match internal state.
		/// </summary>
		private void updateTransform_()
		{
			this.svgGroup_.SetAttribute("transform",
				"translate(" + this.position_.x + "," + this.position_.y + ")");
		}

		/// <summary>
		/// Move the button to the given x, y coordinates.
		/// </summary>
		/// <param name="x">The new x coordinate.</param>
		/// <param name="y">The new y coordinate.</param>
		public void moveTo(double x, double y)
		{
			this.position_.x = x;
			this.position_.y = y;
			this.updateTransform_();
		}

		/// <summary>
		/// Dispose of this button.
		/// </summary>
		public void dispose()
		{
			if (this.svgGroup_ != null) {
				goog.dom.removeNode(this.svgGroup_);
				this.svgGroup_ = null;
			}
			this.workspace_ = null;
			this.targetWorkspace_ = null;
		}

		/// <summary>
		/// Do something when the button is clicked.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		public void onMouseUp(Event e)
		{
			// Don't scroll the page.
			e.PreventDefault();
			// Don't propagate mousewheel event (zooming).
			e.StopPropagation();
			// Stop binding to mouseup and mousemove events--flyout mouseup would normally
			// do this, but we're skipping that.
			Flyout.terminateDrag_();
			this.callback_?.Invoke(this);
		}

		public Workspace getTargetWorkspace()
		{
			return this.targetWorkspace_;
		}
	}
}
