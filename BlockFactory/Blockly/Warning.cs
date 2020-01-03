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
 * @fileoverview Object representing a warning.
 * @author fraser@google.com (Neil Fraser)
 */
using Bridge;
using Bridge.Html5;
using System;
using System.Collections.Generic;

namespace Blockly
{
	public class Warning : Icon
	{
		public Dictionary<string, string> text_;

		/// <summary>
		/// Class for a warning.
		/// </summary>
		/// <param name="block">The block associated with this warning.</param>
		public Warning(BlockSvg block)
			: base(block)
		{
			this.createIcon();
			// The text_ object can contain multiple warnings.
			this.text_ = new Dictionary<string, string>();
		}

		/// <summary>
		/// Does this icon get hidden when the block is collapsed.
		/// </summary>
		//public bool collapseHidden = false;

		/// <summary>
		/// Draw the warning icon.
		/// </summary>
		/// <param name="group">The icon group.</param>
		protected override void drawIcon_(SVGElement group)
		{
			// Triangle with rounded corners.
			Core.createSvgElement("path", new Dictionary<string, object>() {
					{"class", "blocklyIconShape" },
					{ "d", "M2,15Q-1,15 0.5,12L6.5,1.7Q8,-1 9.5,1.7L15.5,12Q17,15 14,15z"} },
				 group);
			// Can't use a real "!" text character since different browsers and operating
			// systems render it differently.
			// Body of exclamation point.
			Core.createSvgElement("path", new Dictionary<string, object>() {
					{"class", "blocklyIconSymbol" },
					{ "d", "m7,4.8v3.16l0.27,2.27h1.46l0.27,-2.27v-3.16z"} },
				 group);
			// Dot of exclamation point.
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"class", "blocklyIconSymbol" },
					{ "x", "7" }, {"y", "11" }, {"height", "2" }, {"width", "2"} },
				group);
		}

		/// <summary>
		/// Create the text for the warning's bubble.
		/// </summary>
		/// <param name="text">The text to display.</param>
		/// <returns>The text to display.</returns>
		private static SVGTextElement textToDom_(string text)
		{
			var paragraph = (SVGTextElement)
				Core.createSvgElement("text", new Dictionary<string, object>() {
						{"class", "blocklyText blocklyBubbleText"},
						{ "y", Bubble.BORDER_WIDTH}},
					null);
			var lines = text.Split("\n");
			for (var i = 0; i < lines.Length; i++) {
				var tspanElement = Core.createSvgElement("tspan", new Dictionary<string, object>() {
					{"dy", "1em" }, {"x", Bubble.BORDER_WIDTH}}, paragraph);
				var textNode = Document.CreateTextNode(lines[i]);
				tspanElement.AppendChild(textNode);
			}
			return paragraph;
		}

		private Element body_;

		/// <summary>
		/// Show or hide the warning bubble.
		/// </summary>
		/// <param name="visible">True if the bubble should be visible.</param>
		internal override void setVisible(bool visible)
		{
			if (visible == this.isVisible()) {
				// No change.
				return;
			}
			Events.fire(
				new Events.Ui(this.block_, "warningOpen", (!visible).ToString(), visible.ToString()));
			if (visible) {
				// Create the bubble to display all warnings.
				var paragraph = Warning.textToDom_(this.getText());
				this.bubble_ = new Bubble((WorkspaceSvg)this.block_.workspace,
					paragraph, this.block_.svgPath_, this.iconXY_, 0.0, 0.0);
				if (this.block_.RTL) {
					// Right-align the paragraph.
					// This cannot be done until the bubble is rendered on screen.
					var maxWidth = paragraph.getBBox().width;
					foreach (Element textElement in paragraph.ChildNodes) {
						textElement.SetAttribute("text-anchor", "end");
						textElement.SetAttribute("x", (maxWidth + Bubble.BORDER_WIDTH).ToString());
					}
				}
				this.updateColour();
				// Bump the warning into the right location.
				var size = this.bubble_.getBubbleSize();
				this.bubble_.setBubbleSize(size.width, size.height);
			}
			else {
				// Dispose of the bubble.
				this.bubble_.dispose();
				this.bubble_ = null;
				this.body_ = null;
			}
		}

		/// <summary>
		/// Bring the warning to the top of the stack when clicked on.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void bodyFocus_(Event e)
		{
			this.bubble_.promote_();
		}

		/// <summary>
		/// Set this warning's text.
		/// </summary>
		/// <param name="text">Warning text (or '' to delete).</param>
		/// <param name="opt_pid">An ID for this text entry to be able to maintain</param>
		public override void setText(string text, string id)
		{
			if (this.text_.TryGetValue(id, out var ret) && ret == text) {
				return;
			}
			if (text != null) {
				this.text_[id] = text;
			}
			else {
				this.text_.Remove(id);
				//Script.Delete(this.text_[id]);
			}
			if (this.isVisible()) {
				this.setVisible(false);
				this.setVisible(true);
			}
		}

		/// <summary>
		/// Get this warning's texts.
		/// </summary>
		/// <returns>All texts concatenated into one string.</returns>
		public override string getText()
		{
			var allWarnings = new JsArray<string>();
			foreach (var id in this.text_.Keys) {
				allWarnings.Push(this.text_[id]);
			}
			return allWarnings.Join("\n");
		}

		public override void dispose()
		{
			this.block_.warning = null;
			base.dispose();
		}
	}
}
