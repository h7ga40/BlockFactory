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
 * @fileoverview Javascript for the Block Exporter Controller class. Allows
 * users to export block definitions and generator stubs of their saved blocks
 * easily using a visual interface. Depends on Block Exporter View and Block
 * Exporter Tools classes. Interacts with Export Settings in the index.html.
 *
 * @author quachtina96 (Tina Quach)
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockExporterController
	{
		public BlockLibraryStorage blockLibStorage;
		public BlockExporterTools tools;
		public string selectorID;
		public Dictionary<string, BlockOption> blockOptions;
		public BlockExporterView view;
		private JsArray<string> usedBlockTypes;

		/*
         * BlockExporter Controller Class
         * @param {!} blockLibStorage Block Library Storage.
         * @constructor
         */
		public BlockExporterController(BlockLibraryStorage blockLibStorage)
		{
			// BlockLibrary.Storage object containing user's saved blocks.
			this.blockLibStorage = blockLibStorage;
			// Utils for generating code to export.
			this.tools = new BlockExporterTools();
			// The ID of the block selector, a div element that will be populated with the
			// block options.
			this.selectorID = "blockSelector";
			// Map of block types stored in block library to their corresponding Block
			// Option objects.
			this.blockOptions = this.tools.createBlockSelectorFromLib(
				this.blockLibStorage, this.selectorID);
			// View provides the block selector and export settings UI.
			this.view = new BlockExporterView(this.blockOptions);
		}

		/// <summary>
		/// Set the block library storage object from which exporter exports.
		/// </summary>
		/// <param name="blockLibStorage"> Block Library Storage object
		/// that stores the blocks.</param>
		public void setBlockLibraryStorage(BlockLibraryStorage blockLibStorage)
		{
			this.blockLibStorage = blockLibStorage;
		}

		/// <summary>
		/// Get the block library storage object from which exporter exports.
		/// </summary>
		/// <returns>blockLibStorage Block Library Storage object
		///    that stores the blocks.</returns>
		public BlockLibraryStorage getBlockLibraryStorage(BlockLibraryStorage blockLibStorage)
		{
			return this.blockLibStorage;
		}

		/// <summary>
		/// Get selected blocks from block selector, pulls info from the Export
		/// Settings form in Block Exporter, and downloads code accordingly.
		/// </summary>
		public void export()
		{
			// Get selected blocks" information.
			var blockTypes = this.view.getSelectedBlockTypes();
			var blockXmlMap = this.blockLibStorage.getBlockXmlMap(blockTypes);

			// Pull block definition(s) settings from the Export Settings form.
			var wantBlockDef = ((HTMLInputElement)Document.GetElementById("blockDefCheck")).Checked;
			var definitionFormat = ((HTMLSelectElement)Document.GetElementById("exportFormat")).Value;
			var blockDef_filename = ((HTMLInputElement)Document.GetElementById("blockDef_filename")).Value;

			// Pull block generator stub(s) settings from the Export Settings form.
			var wantGenStub = ((HTMLInputElement)Document.GetElementById("genStubCheck")).Checked;
			var language = ((HTMLSelectElement)Document.GetElementById("exportLanguage")).Value;
			var generatorStub_filename = ((HTMLInputElement)Document.GetElementById(
				"generatorStub_filename")).Value;

			if (wantBlockDef) {
				// User wants to export selected blocks" definitions.
				if (blockDef_filename == null) {
					// User needs to enter filename.
					Window.Alert("Please enter a filename for your block definition(s) download.");
				}
				else {
					// Get block definition code in the selected format for the blocks.
					var blockDefs = this.tools.getBlockDefinitions(blockXmlMap,
						definitionFormat);
					// Download the file, using .js file ending for JSON or Javascript.
					FactoryUtils.createAndDownloadFile(
						blockDefs, blockDef_filename, "javascript");
				}
			}

			if (wantGenStub) {
				// User wants to export selected blocks" generator stubs.
				if (generatorStub_filename == null) {
					// User needs to enter filename.
					Window.Alert("Please enter a filename for your generator stub(s) download.");
				}
				else {
					// Get generator stub code in the selected language for the blocks.
					var genStubs = this.tools.getGeneratorCode(blockXmlMap,
						language);
					// Get the correct file extension.
					var fileType = (language == "JavaScript") ? "javascript" : "plain";
					// Download the file.
					FactoryUtils.createAndDownloadFile(
						genStubs, generatorStub_filename, fileType);
				}
			}
		}

		/// <summary>
		/// Update the Exporter's block selector with block options generated from blocks
		/// stored in block library.
		/// </summary>
		public void updateSelector()
		{
			// Get previously selected block types.
			var oldSelectedTypes = this.view.getSelectedBlockTypes();

			// Generate options from block library and assign to view.
			this.blockOptions = this.tools.createBlockSelectorFromLib(
				this.blockLibStorage, this.selectorID);
			this.addBlockOptionSelectHandlers();
			this.view.setBlockOptions(this.blockOptions);

			// Select all previously selected blocks.
			for (var i = 0; i < oldSelectedTypes.Length; i++) {
				var blockType = oldSelectedTypes[i];
				if (this.blockOptions.ContainsKey(blockType)) {
					this.view.select(blockType);
				}
			}

			this.view.listSelectedBlocks();
		}

		/// <summary>
		/// Tied to the "Clear Selected Blocks" button in the Block Exporter.
		/// Deselects all blocks in the selector and updates text accordingly.
		/// </summary>
		public void clearSelectedBlocks()
		{
			this.view.deselectAllBlocks();
			this.view.listSelectedBlocks();
		}

		/// <summary>
		/// Tied to the "All Stored" button in the Block Exporter "Select" dropdown.
		/// Selects all blocks stored in block library for export.
		/// </summary>
		public void selectAllBlocks()
		{
			var allBlockTypes = this.blockLibStorage.getBlockTypes();
			for (var i = 0; i < allBlockTypes.Length; i++) {
				var blockType = allBlockTypes[i];
				this.view.select(blockType);
			}
			this.view.listSelectedBlocks();
		}

		/// <summary>
		/// Returns the category XML containing all blocks in the block library.
		/// </summary>
		/// <returns>XML for a category to be used in toolbox.</returns>
		public Element getBlockLibraryCategory()
		{
			return this.tools.generateCategoryFromBlockLib(this.blockLibStorage);
		}

		/// <summary>
		/// Add select handlers to each block option to update the view and the selected
		/// blocks accordingly.
		/// </summary>
		public void addBlockOptionSelectHandlers()
		{
			var self = this;

			// Click handler for a block option. Toggles whether or not it's selected and
			// updates helper text accordingly.
			var updateSelectedBlockTypes_ = new Action<BlockOption>((blockOption) => {
				// Toggle selected.
				blockOption.setSelected(!blockOption.isSelected());

				// Show currently selected blocks in helper text.
				self.view.listSelectedBlocks();
			});

			// Returns a block option select handler.
			var makeBlockOptionSelectHandler_ = new Func<BlockOption, Action>((blockOption) => {
				return () => {
					updateSelectedBlockTypes_(blockOption);
					self.updatePreview();
				};
			});

			// Assign a click handler to each block option.
			foreach (var blockType in this.blockOptions.Keys) {
				var blockOption = this.blockOptions[blockType];
				// Use an additional closure to correctly assign the tab callback.
				blockOption.dom.AddEventListener(
					"click", makeBlockOptionSelectHandler_(blockOption), false);
			}
		}

		/// <summary>
		/// Tied to the "All Used" button in the Block Exporter's "Select" button.
		/// Selects all blocks stored in block library and used in workspace factory.
		/// </summary>
		public void selectUsedBlocks()
		{
			// Deselect all blocks.
			this.view.deselectAllBlocks();

			// Get list of block types that are in block library and used in workspace
			// factory.
			var storedBlockTypes = this.blockLibStorage.getBlockTypes();
			var sharedBlockTypes = new JsArray<string>();
			// Keep list of custom block types used but not in library.
			var unstoredCustomBlockTypes = new JsArray<string>();

			for (var i = 0; i < this.usedBlockTypes.Length; i++) {
				var blockType = this.usedBlockTypes[i];
				if (storedBlockTypes.IndexOf(blockType) != -1) {
					sharedBlockTypes.Push(blockType);
				}
				else if (Array.IndexOf(StandardCategories.coreBlockTypes, blockType) == -1) {
					unstoredCustomBlockTypes.Push(blockType);
				}
			}

			// Select each shared block type.
			for (var i = 0; i < sharedBlockTypes.Length; i++) {
				var blockType = sharedBlockTypes[i];
				this.view.select(blockType);
			}
			this.view.listSelectedBlocks();

			if (unstoredCustomBlockTypes.Length > 0) {
				// Warn user to import block defifnitions and generator code for blocks
				// not in their Block Library nor Blockly's standard library.
				var blockTypesText = unstoredCustomBlockTypes.Join(", ");
				var customWarning = "Custom blocks used in workspace factory but not " +
					"stored in block library:\n " + blockTypesText +
					"\n\nDon\'t forget to include block definitions and generator code " +
					"for these blocks.";
				Window.Alert(customWarning);
			}
		}

		/// <summary>
		/// Set the array that holds the block types used in workspace factory.
		/// </summary>
		/// <param name="usedBlockTypes"> Block types used in</param>
		public void setUsedBlockTypes(JsArray<string> usedBlockTypes)
		{
			this.usedBlockTypes = usedBlockTypes;
		}

		/// <summary>
		/// Updates preview code (block definitions and generator stubs) in the exporter
		/// preview to reflect selected blocks.
		/// </summary>
		public void updatePreview()
		{
			// Generate preview code for selected blocks.
			var blockDefs = this.getBlockDefinitionsOfSelected();
			var genStubs = this.getGeneratorStubsOfSelected();

			// Update the text areas containing the code.
			FactoryUtils.injectCode(blockDefs, "blockDefs_textArea");
			FactoryUtils.injectCode(genStubs, "genStubs_textArea");
		}

		/// <summary>
		/// Returns a map of each selected block's type to its corresponding XML.
		/// </summary>
		/// <returns>A map of each selected block's type (a string) to its
		/// corresponding XML element.</returns>
		public Dictionary<string, Element> getSelectedBlockXmlMap()
		{
			var blockTypes = this.view.getSelectedBlockTypes();
			return this.blockLibStorage.getBlockXmlMap(blockTypes);
		}

		/// <summary>
		/// Get block definition code in the selected format for selected blocks.
		/// </summary>
		/// <returns>The concatenation of each selected block's language code
		/// in the format specified in export settings.</returns>
		public string getBlockDefinitionsOfSelected()
		{
			// Get selected blocks" information.
			var blockXmlMap = this.getSelectedBlockXmlMap();

			// Get block definition code in the selected format for the blocks.
			var definitionFormat = ((HTMLSelectElement)Document.GetElementById("exportFormat")).Value;
			return this.tools.getBlockDefinitions(blockXmlMap, definitionFormat);
		}

		/// <summary>
		/// Get generator stubs in the selected language for selected blocks.
		/// </summary>
		/// <returns>The concatenation of each selected block's generator stub
		/// in the language specified in export settings.</returns>
		public string getGeneratorStubsOfSelected()
		{
			// Get selected blocks" information.
			var blockXmlMap = this.getSelectedBlockXmlMap();

			// Get generator stub code in the selected language for the blocks.
			var language = ((HTMLSelectElement)Document.GetElementById("exportLanguage")).Value;
			return this.tools.getGeneratorCode(blockXmlMap, language);
		}
	}
}
