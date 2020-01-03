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
 * @fileoverview Object representing a code comment.
 * @author fraser@google.com (Neil Fraser)
 */
using Bridge;
using Bridge.Html5;
using System;
using System.Collections.Generic;

namespace Blockly
{
	public class Comment : Icon
	{
		/// <summary>
		/// Class for a comment.
		/// </summary>
		/// <param name="block">The block associated with this comment.</param>
		public Comment(BlockSvg block)
			: base(block)
		{
			this.createIcon();
		}

		/// <summary>
		/// Comment text (if bubble is not visible).
		/// </summary>
		private string text_ = "";

		/// <summary>
		/// Width of bubble.
		/// </summary>
		private double width_ = 160;

		/// <summary>
		/// Height of bubble.
		/// </summary>
		private double height_ = 80;

		/// <summary>
		/// Draw the comment icon.
		/// </summary>
		/// <param name="group">The icon group.</param>
		protected override void drawIcon_(SVGElement group)
		{
			// Circle.
			Core.createSvgElement("circle", new Dictionary<string, object>() {
					{"class", "blocklyIconShape" }, {"r", "8" }, {"cx", "8" }, {"cy", "8" } },
				group);
			// Can't use a real "?" text character since different browsers and operating
			// systems render it differently.
			// Body of question mark.
			Core.createSvgElement("path", new Dictionary<string, object>() {
					{ "class", "blocklyIconSymbol" },
					{ "d", "m6.8,10h2c0.003,-0.617 0.271,-0.962 0.633,-1.266 2.875,-2.405 0.607,-5.534 -3.765,-3.874v1.7c3.12,-1.657 3.698,0.118 2.336,1.25 -1.201,0.998 -1.201,1.528 -1.204,2.19z"} },
				group);
			// Dot of question point.
			Core.createSvgElement("rect", new Dictionary<string, object>() {
					{"class", "blocklyIconSymbol" },
					{ "x", "6.8" }, {"y", "10.78" }, {"height", "2" }, {"width", "2"} },
				group);
		}

		private SVGElement foreignObject_;
		private HTMLTextAreaElement textarea_;

		private SVGElement createEditor_()
		{
			/* Create the editor.  Here's the markup that will be generated:
			  <foreignObject x="8" y="8" width="164" height="164">
				<body xmlns="http://www.w3.org/1999/xhtml" class="blocklyMinimalBody">
				  <textarea xmlns="http://www.w3.org/1999/xhtml"
					  class="blocklyCommentTextarea"
					  style="height: 164px; width: 164px;"></textarea>
				</body>
			  </foreignObject>
			*/
			this.foreignObject_ = Core.createSvgElement("foreignObject", new Dictionary<string, object>() {
				{ "x", Bubble.BORDER_WIDTH }, {"y", Bubble.BORDER_WIDTH} },
				null);
			var body = Document.CreateElementNS<HTMLBodyElement>(Core.HTML_NS, "body");
			body.SetAttribute("xmlns", Core.HTML_NS);
			body.ClassName = "blocklyMinimalBody";
			var textarea = Document.CreateElementNS<HTMLTextAreaElement>(Core.HTML_NS, "textarea");
			textarea.ClassName = "blocklyCommentTextarea";
			textarea.SetAttribute("dir", this.block_.RTL ? "RTL" : "LTR");
			body.AppendChild(textarea);
			this.textarea_ = textarea;
			this.foreignObject_.AppendChild(body);
			Core.bindEventWithChecks_(textarea, "mouseup", this, new Action<Event>(this.textareaFocus_));
			// Don't zoom with mousewheel.
			Core.bindEventWithChecks_(textarea, "wheel", null, new Action<Event>((e) => {
				e.StopPropagation();
			}));
			Core.bindEventWithChecks_(textarea, "change", null, new Action<Event>((e) => {
				if (this.text_ != textarea.Value) {
					Events.fire(new Events.Change(
					  this.block_, "comment", null, this.text_, textarea.Value));
					this.text_ = textarea.Value;
				}
			}));
			Window.SetTimeout(() => {
				textarea.Focus();
			}, 0);
			return this.foreignObject_;
		}

		/// <summary>
		/// Add or remove editability of the comment.
		/// </summary>
		public override void updateEditable()
		{
			if (this.isVisible()) {
				// Toggling visibility will force a rerendering.
				this.setVisible(false);
				this.setVisible(true);
			}
			// Allow the icon to update.
			base.updateEditable();
		}

		/// <summary>
		/// Callback function triggered when the bubble has resized.
		/// Resize the text area accordingly.
		/// </summary>
		private void resizeBubble_()
		{
			if (this.isVisible()) {
				var size = this.bubble_.getBubbleSize();
				var doubleBorderWidth = 2 * Bubble.BORDER_WIDTH;
				this.foreignObject_.SetAttribute("width", (size.width - doubleBorderWidth).ToString());
				this.foreignObject_.SetAttribute("height", (size.height - doubleBorderWidth).ToString());
				this.textarea_.Style.Width = (size.width - doubleBorderWidth - 4) + "px";
				this.textarea_.Style.Height = (size.height - doubleBorderWidth - 4) + "px";
			}
		}

		/// <summary>
		/// Show or hide the comment bubble.
		/// </summary>
		/// <param name="visible">True if the bubble should be visible.</param>
		internal override void setVisible(bool visible)
		{
			if (visible == this.isVisible()) {
				// No change.
				return;
			}
			Events.fire(
				new Events.Ui(this.block_, "commentOpen", (!visible).ToString(), visible.ToString()));
			if ((!this.block_.isEditable() && this.textarea_ == null) || goog.userAgent.IE) {
				// Steal the code from warnings to make an uneditable text bubble.
				// MSIE does not support foreignobject; textareas are impossible.
				// http://msdn.microsoft.com/en-us/library/hh834675%28v=vs.85%29.aspx
				// Always treat comments in IE as uneditable.
				// TODO:Warning.prototype.setVisible.call(this, visible);
				return;
			}
			// Save the bubble stats before the visibility switch.
			var text = this.getText();
			var size = this.getBubbleSize();
			if (visible) {
				// Create the bubble.
				this.bubble_ = new Bubble((WorkspaceSvg)this.block_.workspace,
					this.createEditor_(), ((BlockSvg)this.block_).svgPath_,
					this.iconXY_, this.width_, this.height_);
				this.bubble_.registerResizeEvent(new Action(this.resizeBubble_));
				this.updateColour();
			}
			else {
				// Dispose of the bubble.
				this.bubble_.dispose();
				this.bubble_ = null;
				this.textarea_ = null;
				this.foreignObject_ = null;
			}
			// Restore the bubble stats after the visibility switch.
			this.setText(text);
			this.setBubbleSize(size.width, size.height);
		}

		/// <summary>
		/// Bring the comment to the top of the stack when clicked on.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		private void textareaFocus_(Event e)
		{
			// Ideally this would be hooked to the focus event for the comment.
			// However doing so in Firefox swallows the cursor for unknown reasons.
			// So this is hooked to mouseup instead.  No big deal.
			this.bubble_.promote_();
			// Since the act of moving this node within the DOM causes a loss of focus,
			// we need to reapply the focus.
			this.textarea_.Focus();
		}

		/// <summary>
		/// Get the dimensions of this comment's bubble.
		/// </summary>
		/// <returns>Object with width and height properties.</returns>
		internal goog.math.Size getBubbleSize()
		{
			if (this.isVisible()) {
				return this.bubble_.getBubbleSize();
			}
			else {
				return new goog.math.Size { width = this.width_, height = this.height_ };
			}
		}

		/// <summary>
		/// Size this comment's bubble.
		/// </summary>
		/// <param name="width">Width of the bubble.</param>
		/// <param name="height">Height of the bubble.</param>
		internal void setBubbleSize(double width, double height)
		{
			if (this.textarea_ != null) {
				this.bubble_.setBubbleSize(width, height);
			}
			else {
				this.width_ = width;
				this.height_ = height;
			}
		}

		/// <summary>
		/// Returns this comment's text.
		/// </summary>
		/// <returns>Comment text.</returns>
		public override string getText()
		{
			return this.textarea_ != null ? this.textarea_.Value : this.text_;
		}

		/// <summary>
		/// Set this comment's text.
		/// </summary>
		/// <param name="text">Comment text.</param>
		/// <param name="opt_pid"></param>
		public override void setText(string text, string opt_pid = null)
		{
			if (this.text_ != text) {
				Events.fire(new Events.Change(
				  this.block_, "comment", null, this.text_, text));
				this.text_ = text;
			}
			if (this.textarea_ != null) {
				this.textarea_.Value = text;
			}
		}

		/// <summary>
		/// Dispose of this comment.
		/// </summary>
		public override void dispose()
		{
			if (Events.isEnabled()) {
				this.setText("");  // Fire event to delete comment.
			}
			this.block_.comment = null;
			base.dispose();
		}
	}
}
