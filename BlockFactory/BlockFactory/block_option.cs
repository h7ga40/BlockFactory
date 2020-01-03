/*
 * @license
 * Blockly Demos: Block Factory
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

/*
 * @fileoverview Javascript for the BlockOption class, used to represent each of
 * the various blocks that you may select. Each block option has a checkbox,
 * a label, and a preview workspace through which to view the block.
 *
 * @author quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockOption
	{
		public Element blockSelector;
		public string blockType;
		public HTMLInputElement checkbox;
		public Element dom;
		public Element previewBlockXml;
		public Blockly.WorkspaceSvg previewWorkspace;
		private bool selected;

		/// <summary>
		/// BlockOption Class
		/// A block option includes checkbox, label, and div element that shows a preview
		/// of the block.
		/// </summary>
		/// <param name="blockSelector"> Scrollable div that will contain the
		/// block options for the selector.</param>
		/// <param name="blockType"> Type of block for which to create an option.</param>
		/// <param name="=" previewBlockXml XML element containing the preview block.
		public BlockOption(Element blockSelector, string blockType, Element previewBlockXml)
		{
			// The div to contain the block option.
			this.blockSelector = blockSelector;
			// The type of block represented by the option.
			this.blockType = blockType;
			// The checkbox for the option. Set in createDom.
			this.checkbox = null;
			// The dom for the option. Set in createDom.
			this.dom = null;
			// Xml element containing the preview block.
			this.previewBlockXml = previewBlockXml;
			// Workspace containing preview of block. Set upon injection of workspace in
			// showPreviewBlock.
			this.previewWorkspace = null;
			// Whether or not block the option is selected.
			this.selected = false;
			// Using this.selected rather than this.checkbox.Checked allows for proper
			// handling of click events on the block option; Without this, clicking
			// directly on the checkbox does not toggle selection.
		}

		/// <summary>
		/// Creates the dom for a single block option. Includes checkbox, label, and div
		/// in which to inject the preview block.
		/// </summary>
		/// <returns>Root node of the selector dom which consists of a
		/// checkbox, a label, and a fixed size preview workspace per block.</returns>
		public Element createDom()
		{
			// Create the div for the block option.
			var blockOptContainer = goog.dom.createDom("div", new Dictionary<string, string> {
				{ "id", this.blockType},
				{ "class", "blockOption"}
			}, ""); // Empty quotes for empty div.

			// Create and append div in which to inject the workspace for viewing the
			// block option.
			var blockOptionPreview = goog.dom.createDom("div", new Dictionary<string, string> {
				{ "id" , this.blockType + "_workspace"},
				{"class", "blockOption_preview"}
			}, "");
			blockOptContainer.AppendChild(blockOptionPreview);

			// Create and append container to hold checkbox and label.
			var checkLabelContainer = goog.dom.createDom("div", new Dictionary<string, string> {
				{ "class", "blockOption_checkLabel" }
			}, "");
			blockOptContainer.AppendChild(checkLabelContainer);

			// Create and append container for checkbox.
			var checkContainer = goog.dom.createDom("div", new Dictionary<string, string> {
				{ "class", "blockOption_check"}
			}, "");
			checkLabelContainer.AppendChild(checkContainer);

			// Create and append checkbox.
			this.checkbox = (HTMLInputElement)goog.dom.createDom("input", new Dictionary<string, string> {
				{"type", "checkbox"},
				{"id", this.blockType + "_check"}
			}, "");
			checkContainer.AppendChild(this.checkbox);

			// Create and append container for block label.
			var labelContainer = goog.dom.createDom("div", new Dictionary<string, string> {
				{ "class", "blockOption_label" }
			}, "");
			checkLabelContainer.AppendChild(labelContainer);

			// Create and append text node for the label.
			var labelText = goog.dom.createDom("p", new Dictionary<string, string> {
				{ "id", this.blockType + "_text" }
			}, this.blockType);
			labelContainer.AppendChild(labelText);

			this.dom = blockOptContainer;
			return this.dom;
		}

		/// <summary>
		/// Injects a workspace containing the block into the block option's preview div.
		/// </summary>
		public void showPreviewBlock()
		{
			// Get ID of preview workspace.
			var blockOptPreviewID = this.dom.Id + "_workspace";

			// Inject preview block.
			var workspace = Blockly.Core.inject(blockOptPreviewID, new Dictionary<string, object> { { "readOnly", true } });
			Blockly.Xml.domToWorkspace(this.previewBlockXml, workspace);
			this.previewWorkspace = workspace;

			// Center the preview block in the workspace.
			this.centerBlock();
		}

		/// <summary>
		/// Centers the preview block in the workspace.
		/// </summary>
		public void centerBlock()
		{
			// Get metrics.
			var block = (Blockly.BlockSvg)this.previewWorkspace.getTopBlocks(false)[0];
			var blockMetrics = block.getHeightWidth();
			var blockCoordinates = block.getRelativeToSurfaceXY();
			var workspaceMetrics = this.previewWorkspace.getMetrics();

			// Calculate new coordinates.
			var x = workspaceMetrics.viewWidth / 2 - blockMetrics.width / 2 -
				blockCoordinates.x;
			var y = workspaceMetrics.viewHeight / 2 - blockMetrics.height / 2 -
				blockCoordinates.y;

			// Move block.
			block.moveBy(x, y);
		}

		/// <summary>
		/// Selects or deselects the block option.
		/// </summary>
		/// <param name="selected">True if selecting option, false if deselecting
		/// option.</param> 
		public void setSelected(bool selected)
		{
			this.selected = selected;
			if (this.checkbox != null) {
				this.checkbox.Checked = selected;
			}
		}

		/// <summary>
		/// Returns boolean telling whether or not block is selected.
		/// </summary>
		/// <returns>True if selecting option, false if deselecting
		///    option.</returns>
		public bool isSelected()
		{
			return this.selected;
		}
	}
}
