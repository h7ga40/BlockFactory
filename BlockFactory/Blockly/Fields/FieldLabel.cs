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
 * @fileoverview Non-editable text field.  Used for titles, labels, etc.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class FieldLabel : Field
	{
		private string class_;

		/// <summary>
		/// Class for a non-editable field.
		/// </summary>
		/// <param name="text">The initial content of the field.</param>
		/// <param name="opt_class">Optional CSS class for the field's text.</param>
		public FieldLabel(string text, string opt_class = null)
			: base(text)
		{
			this.size_ = new goog.math.Size(0, 17.5);
			this.class_ = opt_class;
			this.setValue(text);
		}

		/// <summary>
		/// Install this text on a block.
		/// </summary>
		public override void init()
		{
			if (this.textElement_ != null) {
				// Text has already been initialized once.
				return;
			}
			// Build the DOM.
			this.textElement_ = Core.createSvgElement("text",
				new Dictionary<string, object>() {
					{ "class", "blocklyText" },
					{ "y", this.size_.height - 5}
				},
				null);
			if (this.class_ != null) {
				Core.addClass_(this.textElement_, this.class_);
			}
			if (!this.visible_) {
				this.textElement_.style.Display = Display.None;
			}
			this.sourceBlock_.getSvgRoot().AppendChild(this.textElement_);

			// Configure the field to be transparent with respect to tooltips.
			this.textElement_.tooltip = this.sourceBlock_;
			Tooltip.bindMouseEvents(this.textElement_);
			// Force a render.
			this.updateTextNode_();
		}

		/// <summary>
		/// Dispose of all DOM objects belonging to this text.
		/// </summary>
		public override void dispose()
		{
			goog.dom.removeNode(this.textElement_);
			this.textElement_ = null;
		}

		/// <summary>
		/// Gets the group element for this field.
		/// Used for measuring the size and for positioning.
		/// </summary>
		/// <returns></returns>
		public override SVGElement getSvgRoot()
		{
			return this.textElement_;
		}

		/// <summary>
		/// Change the tooltip text for this field.
		/// </summary>
		/// <param name="newTip">Text for tooltip or a parent element to
		/// link to for its tooltip.</param>
		public override void setTooltip(BlockSvg newTip)
		{
			this.textElement_.tooltip = newTip;
		}

		public override void showEditor_(bool opt_quietInput)
		{
			throw new NotImplementedException();
		}
	}
}
