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
 * @fileoverview Javascript for the BlockExporter Tools class, which generates
 * block definitions and generator stubs for given block types.  Also generates
 * toolbox XML for the exporter's workspace.  Depends on the FactoryUtils for
 * its code generation functions.
 *
 * @author quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockExporterTools
	{
		public HTMLDivElement container;
		public Blockly.Workspace hiddenWorkspace;

		/// <summary>
		/// Block Exporter Tools Class
		/// </summary>
		public BlockExporterTools()
		{
			// Create container for hidden workspace.
			this.container = (HTMLDivElement)goog.dom.createDom("div", new Dictionary<string, string> {
				{ "id", "blockExporterTools_hiddenWorkspace" }
			}, ""); // Empty quotes for empty div.
					// Hide hidden workspace.
			this.container.Style.Display = Display.None;
			Document.Body.AppendChild(this.container);
			/// <summary>
			/// Hidden workspace for the Block Exporter that holds pieces that make
			/// up the block
			/// </summary>
			this.hiddenWorkspace = Blockly.Core.inject(this.container.Id, new Dictionary<string, object> {
				{ "collapse", false },
				{ "media", "../../media/" }
			});
		}

		/// <summary>
		/// Get Blockly Block object from XML that encodes the blocks used to design
		/// the block.
		/// </summary>
		/// <param name="xml"> XML element that encodes the blocks used to design
		/// the block. For example, the block XMLs saved in block library.</param>
		/// <returns>Root block (factory_base block) which contains
		///    all information needed to generate block definition or null.</returns>
		private Blockly.Block getRootBlockFromXml_(Element xml)
		{
			// Render XML in hidden workspace.
			this.hiddenWorkspace.clear();
			Blockly.Xml.domToWorkspace(xml, this.hiddenWorkspace);
			// Get root block.
			var rootBlocks = this.hiddenWorkspace.getTopBlocks(false);
			return rootBlocks.Length > 0 ? rootBlocks[0] : null;
		}

		/// <summary>
		/// Return the given language code of each block type in an array.
		/// </summary>
		/// <param name="blockXmlMap"> Map of block type to XML.</param>
		/// <param name="definitionFormat"> "JSON" or "JavaScript"</param>
		/// <returns>The concatenation of each block's language code in the
		///    desired format.</returns>
		public string getBlockDefinitions(Dictionary<string, Element> blockXmlMap, string definitionFormat)
		{
			var blockCode = new JsArray<string>();
			foreach (var blockType in blockXmlMap.Keys) {
				string code;
				var xml = blockXmlMap[blockType];
				if (xml != null) {
					// Render and get block from hidden workspace.
					var rootBlock = this.getRootBlockFromXml_(xml);
					if (rootBlock != null) {
						// Generate the block's definition.
						code = FactoryUtils.getBlockDefinition(blockType, rootBlock,
							definitionFormat, this.hiddenWorkspace);
						// Add block's definition to the definitions to return.
					}
					else {
						// Append warning comment and write to console.
						code = "// No block definition generated for " + blockType +
						  ". Could not find root block in XML stored for this block.";
						Console.WriteLine("No block definition generated for " + blockType +
						  ". Could not find root block in XML stored for this block.");
					}
				}
				else {
					// Append warning comment and write to console.
					code = "// No block definition generated for " + blockType +
					  ". Block was not found in Block Library Storage.";
					Console.WriteLine("No block definition generated for " + blockType +
					  ". Block was not found in Block Library Storage.");
				}
				blockCode.Push(code);
			}

			// Surround json with [] and comma separate items.
			if (definitionFormat == "JSON") {
				return "[" + blockCode.Join(",\n") + "]";
			}
			return blockCode.Join("\n\n");
		}

		/// <summary>
		/// Return the generator code of each block type in an array in a given language.
		/// </summary>
		/// <param name="blockXmlMap"></param> Map of block type to XML.
		/// <param name="generatorLanguage"> E.g. "JavaScript", "Python", "PHP", "Lua",
		/// "Dart"</param>
		/// <returns>The concatenation of each block's generator code in the
		/// desired format.</returns>
		public string getGeneratorCode(Dictionary<string, Element> blockXmlMap, string generatorLanguage)
		{
			var multiblockCode = new JsArray<string>();
			// Define the custom blocks in order to be able to create instances of
			// them in the exporter workspace.
			this.addBlockDefinitions(blockXmlMap);

			foreach (var blockType in blockXmlMap.Keys) {
				string blockGenCode;
				var xml = blockXmlMap[blockType];
				if (xml != null) {
					// Render the preview block in the hidden workspace.
					var tempBlock =
						FactoryUtils.getDefinedBlock(blockType, this.hiddenWorkspace);
					// Get generator stub for the given block and add to  generator code.
					blockGenCode =
						FactoryUtils.getGeneratorStub(tempBlock, generatorLanguage);
				}
				else {
					// Append warning comment and write to console.
					blockGenCode = "// No generator stub generated for " + blockType +
					  ". Block was not found in Block Library Storage.";
					Console.WriteLine("No block generator stub generated for " + blockType +
					  ". Block was not found in Block Library Storage.");
				}
				multiblockCode.Push(blockGenCode);
			}
			return multiblockCode.Join("\n\n");
		}

		/// <summary>
		/// Evaluates block definition code of each block in given object mapping
		/// block type to XML. Called in order to be able to create instances of the
		/// blocks in the exporter workspace.
		/// </summary>
		/// <param name="blockXmlMap"> Map of block type to XML.</param>
		public void addBlockDefinitions(Dictionary<string, Element> blockXmlMap)
		{
			//var blockDefs = this.getBlockDefinitions(blockXmlMap, "JavaScript");
			//Script.Eval(blockDefs);
			var definitionFormat = "JSON";
			foreach (var blockXml in blockXmlMap) {
				var xml = blockXml.Value;
				if (xml == null)
					continue;

				var rootBlock = this.getRootBlockFromXml_(xml);
				if (rootBlock != null) {
					// Generate the block's definition.
					var code = FactoryUtils.getBlockDefinition(blockXml.Key, rootBlock,
						definitionFormat, this.hiddenWorkspace);
					// Add block's definition to the definitions to return.
					Blockly.Core.Blocks[blockXml.Key] = code;
				}
			}
		}

		/// <summary>
		/// Pulls information about all blocks in the block library to generate XML
		/// for the selector workpace's toolbox.
		/// </summary>
		/// <param name="blockLibStorage"> Block Library Storage object.</param>
		/// <returns>XML representation of the toolbox.</returns>
		Element generateToolboxFromLibrary(BlockLibraryStorage blockLibStorage)
		{
			// Create DOM for XML.
			var xmlDom = goog.dom.createDom("xml", new Dictionary<string, string>{
				{ "id" , "blockExporterTools_toolbox"},
				{"style" , "display:none"}
			});

			var allBlockTypes = blockLibStorage.getBlockTypes();
			// Object mapping block type to XML.
			var blockXmlMap = blockLibStorage.getBlockXmlMap(allBlockTypes);

			// Define the custom blocks in order to be able to create instances of
			// them in the exporter workspace.
			this.addBlockDefinitions(blockXmlMap);

			foreach (var blockType in blockXmlMap.Keys) {
				// Get block.
				var block = FactoryUtils.getDefinedBlock(blockType, this.hiddenWorkspace);
				var category = FactoryUtils.generateCategoryXml(new JsArray<Blockly.Block> { block }, blockType);
				xmlDom.AppendChild(category);
			}

			// If there are no blocks in library and the map is empty, append dummy
			// category.
			if (blockXmlMap.Count == 0) {
				var category = goog.dom.createDom("category");
				category.SetAttribute("name", "Next Saved Block");
				xmlDom.AppendChild(category);
			}
			return xmlDom;
		}

		/// <summary>
		/// Generate XML for the workspace factory's category from imported block
		/// definitions.
		/// </summary>
		/// <param name="blockLibStorage">Block Library Storage object.</param>
		/// <returns>XML representation of a category.</returns>
		public Element generateCategoryFromBlockLib(BlockLibraryStorage blockLibStorage)
		{
			var allBlockTypes = blockLibStorage.getBlockTypes();
			// Object mapping block type to XML.
			var blockXmlMap = blockLibStorage.getBlockXmlMap(allBlockTypes);

			// Define the custom blocks in order to be able to create instances of
			// them in the exporter workspace.
			this.addBlockDefinitions(blockXmlMap);

			// Get array of defined blocks.
			var blocks = new JsArray<Blockly.Block>();
			foreach (var blockType in blockXmlMap.Keys) {
				var block = FactoryUtils.getDefinedBlock(blockType, this.hiddenWorkspace);
				blocks.Push(block);
			}

			return FactoryUtils.generateCategoryXml(blocks, "Block Library");
		}

		/// <summary>
		/// Generate selector dom from block library storage. For each block in the
		/// library, it has a block option, which consists of a checkbox, a label,
		/// and a fixed size preview workspace.
		/// </summary>
		/// <param name="blockLibStorage"> Block Library Storage object.</param>
		/// <param name="blockSelectorId"> ID of the div element that will contain
		/// the block options.</param>
		/// <returns>Map of block type to Block Option object.</returns>
		public Dictionary<string, BlockOption> createBlockSelectorFromLib(BlockLibraryStorage blockLibStorage, string blockSelectorId)
		{
			// Object mapping each stored block type to XML.
			var allBlockTypes = blockLibStorage.getBlockTypes();
			var blockXmlMap = blockLibStorage.getBlockXmlMap(allBlockTypes);

			// Define the custom blocks in order to be able to create instances of
			// them in the exporter workspace.
			this.addBlockDefinitions(blockXmlMap);

			var blockSelector = Document.GetElementById(blockSelectorId);
			// Clear the block selector.
			Node child;
			while ((child = blockSelector.FirstChild) != null) {
				blockSelector.RemoveChild(child);
			}

			// Append each block option's dom to the selector.
			var blockOptions = new Dictionary<string, BlockOption>();
			foreach (var blockType in blockXmlMap.Keys) {
				// Get preview block's XML.
				var block = FactoryUtils.getDefinedBlock(blockType, this.hiddenWorkspace);
				var previewBlockXml = Blockly.Xml.workspaceToDom(this.hiddenWorkspace);

				// Create block option, inject block into preview workspace, and append
				// option to block selector.
				var blockOpt = new BlockOption(blockSelector, blockType, previewBlockXml);
				blockOpt.createDom();
				blockSelector.AppendChild(blockOpt.dom);
				blockOpt.showPreviewBlock();
				blockOptions[blockType] = blockOpt;
			}
			return blockOptions;
		}
	}
}