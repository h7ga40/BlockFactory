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
 * @fileoverview Javascript for the Block Exporter View class. Reads from and
 * manages a block selector through which users select blocks to export.
 *
 * @author quachtina96 (Tina Quach)
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockExporterView
	{
		public Dictionary<string, BlockOption> blockOptions;

		/// <summary>
		/// BlockExporter View Class
		/// </summary>
		/// <param name="blockOptions">Map of block types to BlockOption objects.</param>
		public BlockExporterView(Dictionary<string, BlockOption> blockOptions)
		{
			//  Map of block types to BlockOption objects to select from.
			this.blockOptions = blockOptions;
		}

		/// <summary>
		/// Set the block options in the selector of this instance of
		/// BlockExporterView.
		/// </summary>
		/// <param name="blockOptions"> Map of block types to BlockOption objects.</param>
		public void setBlockOptions(Dictionary<string, BlockOption> blockOptions)
		{
			this.blockOptions = blockOptions;
		}

		/// <summary>
		/// Updates the helper text to show list of currently selected blocks.
		/// </summary>
		public void listSelectedBlocks()
		{
			var selectedBlocksText = this.getSelectedBlockTypes().Join(",\n ");
			Document.GetElementById("selectedBlocksText").TextContent = selectedBlocksText;
		}

		/// <summary>
		/// Selects a given block type in the selector.
		/// </summary>
		/// <param name="blockType"> Type of block to selector.</param>
		public void select(string blockType)
		{
			this.blockOptions[blockType].setSelected(true);
		}

		/// <summary>
		/// Deselects a block in the selector.
		/// </summary>
		/// <param name="block"> Type of block to add to selector workspce.</param>
		public void deselect(string blockType)
		{
			this.blockOptions[blockType].setSelected(false);
		}

		/// <summary>
		/// Deselects all blocks.
		/// </summary>
		public void deselectAllBlocks()
		{
			foreach (var blockType in this.blockOptions.Keys) {
				this.deselect(blockType);
			}
		}

		/// <summary>
		/// Given an array of selected blocks, selects these blocks in the view, marking
		/// the checkboxes accordingly.
		/// </summary>
		/// <param name="blockTypes"> Array of block types to select.</param>
		public void setSelectedBlockTypes(JsArray<string> blockTypes)
		{
			for (var i = 0; i < blockTypes.Length; i++) {
				var blockType = blockTypes[i];
				this.select(blockType);
			}
		}

		/// <summary>
		/// Returns array of selected blocks.
		/// </summary>
		/// <returns>Array of all selected block types.</returns>
		public JsArray<string> getSelectedBlockTypes()
		{
			var selectedTypes = new JsArray<string>();
			foreach (var blockType in this.blockOptions.Keys) {
				var blockOption = this.blockOptions[blockType];
				if (blockOption.isSelected()) {
					selectedTypes.Push(blockType);
				}
			}
			return selectedTypes;
		}

		/// <summary>
		/// Centers the preview block of each block option in the exporter selector.
		/// </summary>
		public void centerPreviewBlocks()
		{
			foreach (var blockType in this.blockOptions.Keys) {
				this.blockOptions[blockType].centerBlock();
			}
		}
	}
}
