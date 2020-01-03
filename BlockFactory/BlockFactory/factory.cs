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
 * @fileoverview JavaScript for Blockly's Block Factory application through
 * which users can build blocks using a visual interface and dynamically
 * generate a preview block and starter code for the block (block definition and
 * generator stub. Uses the Block Factory namespace. Depends on the FactoryUtils
 * for its code generation functions.
 *
 * @author fraser@google.com (Neil Fraser), quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using Blockly;
using Bridge;
using Bridge.Html5;

/*
 * Namespace for Block Factory.
 */
namespace BlockFactoryApp
{
	public static partial class BlockFactory
	{
		/// <summary>
		/// Workspace for user to build block.
		/// </summary>
		public static Blockly.WorkspaceSvg mainWorkspace = null;

		/// <summary>
		/// Workspace for preview of block.
		/// </summary>
		public static Blockly.WorkspaceSvg previewWorkspace = null;

		/// <summary>
		/// Name of block if not named.
		/// </summary>
		public static string UNNAMED = "unnamed";

		/// <summary>
		/// Existing direction ("ltr" vs "rtl") of preview.
		/// </summary>
		public static string oldDir = null;

		/*
		 * The starting XML for the Block Factory main workspace. Contains the
		 * unmovable, undeletable factory_base block.
		 */
		public static string STARTER_BLOCK_XML_TEXT = "<xml><block type=\"factory_base\" " +
			"deletable=\"false\" movable=\"false\"></block></xml>";

		/// <summary>
		/// Change the language code format.
		/// </summary>
		public static void formatChange()
		{
			var mask = (HTMLDivElement)Document.GetElementById("blocklyMask");
			var languagePre = (HTMLPreElement)Document.GetElementById("languagePre");
			var languageTA = (HTMLTextAreaElement)Document.GetElementById("languageTA");
			if (((HTMLSelectElement)Document.GetElementById("format")).Value == "Manual") {
				Blockly.Core.hideChaff();
				mask.Style.Display = Display.Block;
				languagePre.Style.Display = Display.None;
				languageTA.Style.Display = Display.Block;
				var code = languagePre.TextContent.Trim();
				languageTA.Value = code;
				languageTA.Focus();
				BlockFactory.updatePreview();
			}
			else {
				mask.Style.Display = Display.None;
				languageTA.Style.Display = Display.None;
				languagePre.Style.Display = Display.Block;
				BlockFactory.updateLanguage(null);
			}
			BlockFactory.disableEnableLink();
		}

		/// <summary>
		/// Update the language code based on constructs made in Blockly.
		/// </summary>
		public static void updateLanguage(Events.Abstract e)
		{
			var rootBlock = FactoryUtils.getRootBlock(BlockFactory.mainWorkspace);
			if (rootBlock == null) {
				return;
			}
			var blockType = rootBlock.getFieldValue("NAME").Trim().ToLowerCase();
			if (blockType == null) {
				blockType = BlockFactory.UNNAMED;
			}
			var format = ((HTMLSelectElement)Document.GetElementById("format")).Value;
			var code = FactoryUtils.getBlockDefinition(blockType, rootBlock, format,
				BlockFactory.mainWorkspace);
			FactoryUtils.injectCode(code, "languagePre");
			BlockFactory.updatePreview();
		}

		/// <summary>
		/// Update the generator code.
		/// </summary>
		/// <param name="block"> Rendered block in preview workspace.</param>
		public static void updateGenerator(Blockly.Block block)
		{
			var language = ((HTMLSelectElement)Document.GetElementById("language")).Value;
			var generatorStub = FactoryUtils.getGeneratorStub(block, language);
			FactoryUtils.injectCode(generatorStub, "generatorPre");
		}

		/// <summary>
		/// Update the preview display.
		/// </summary>
		public static void updatePreview()
		{
			// Toggle between LTR/RTL if needed (also used in first display).
			var newDir = ((HTMLSelectElement)Document.GetElementById("direction")).Value;
			if (BlockFactory.oldDir != newDir) {
				if (BlockFactory.previewWorkspace != null) {
					BlockFactory.previewWorkspace.dispose();
				}
				var rtl = newDir == "rtl";
				BlockFactory.previewWorkspace = Blockly.Core.inject("preview", new Dictionary<string, object> {
					{ "rtl", rtl },
					{ "media", "../../media/" },
					{ "scrollbars", true }
				});
				BlockFactory.oldDir = newDir;
			}
			BlockFactory.previewWorkspace.clear();

			// Fetch the code and determine its format (JSON or JavaScript).
			var format = ((HTMLSelectElement)Document.GetElementById("format")).Value;
			string code;
			if (format == "Manual") {
				code = ((HTMLTextAreaElement)Document.GetElementById("languageTA")).Value;
				// If the code is JSON, it will parse, otherwise treat as JS.
				try {
					JSON.Parse(code);
					format = "JSON";
				}
				catch (Exception) {
					format = "JavaScript";
				}
			}
			else {
				code = Document.GetElementById("languagePre").TextContent;
			}
			if (String.IsNullOrEmpty(code.Trim())) {
				// Nothing to render.  Happens while cloud storage is loading.
				return;
			}

			// Backup Blockly.Core.Blocks object so that main workspace and preview don't
			// collide if user creates a "factory_base" block, for instance.
			var backupBlocks = Blockly.Core.Blocks;
			try {
				// Make a shallow copy.
				Blockly.Core.Blocks = new Blockly.Blocks();
				foreach (var prop in backupBlocks.Keys) {
					Blockly.Core.Blocks[prop] = backupBlocks[prop];
				}

				if (format == "JSON") {
					var json = (Dictionary<string, object>)JSON.Parse(code);
					Blockly.Core.Blocks.Add(json.ContainsKey("type") ? (string)json["type"] : BlockFactory.UNNAMED, code);
				}
				else if (format == "JavaScript") {
					Script.Eval(code);
				}
				else {
					throw new Exception("Unknown format: " + format);
				}

				// Look for a block on Blockly.Core.Blocks that does not match the backup.
				string blockType = null;
				foreach (var type in Blockly.Core.Blocks.Keys) {
					if (!backupBlocks.ContainsKey(type) || Blockly.Core.Blocks[type] != backupBlocks[type]) {
						blockType = type;
						break;
					}
				}
				if (String.IsNullOrEmpty(blockType)) {
					return;
				}

				// Create the preview block.
				var previewBlock = (Blockly.BlockSvg)BlockFactory.previewWorkspace.newBlock(blockType);
				previewBlock.initSvg();
				previewBlock.render();
				previewBlock.setMovable(false);
				previewBlock.setDeletable(false);
				previewBlock.moveBy(15, 10);
				BlockFactory.previewWorkspace.clearUndo();
				BlockFactory.updateGenerator(previewBlock);

				// Warn user only if their block type is already exists in Blockly's
				// standard library.
				var rootBlock = FactoryUtils.getRootBlock(BlockFactory.mainWorkspace);
				if (Array.IndexOf(StandardCategories.coreBlockTypes, blockType) != -1) {
					rootBlock.setWarningText("A core Blockly block already exists " +
						"under this name.");

				}
				else if (blockType == "block_type") {
					// Warn user to let them know they can't save a block under the default
					// name "block_type"
					rootBlock.setWarningText("You cannot save a block with the default " +
						"name, \"block_type\"");

				}
				else {
					rootBlock.setWarningText(null);
				}

			}
			finally {
				Blockly.Core.Blocks = backupBlocks;
			}
		}

		/// <summary>
		/// Disable link and save buttons if the format is "Manual", enable otherwise.
		/// </summary>
		public static void disableEnableLink()
		{
			var linkButton = (HTMLButtonElement)Document.GetElementById("linkButton");
			var saveBlockButton = (HTMLButtonElement)Document.GetElementById("localSaveButton");
			var saveToLibButton = (HTMLButtonElement)Document.GetElementById("saveToBlockLibraryButton");
			var disabled = ((HTMLSelectElement)Document.GetElementById("format")).Value == "Manual";
			linkButton.Disabled = disabled;
			saveBlockButton.Disabled = disabled;
			saveToLibButton.Disabled = disabled;
		}

		/// <summary>
		/// Render starter block (factory_base).
		/// </summary>
		public static void showStarterBlock()
		{
			BlockFactory.mainWorkspace.clear();
			var xml = Blockly.Xml.textToDom(BlockFactory.STARTER_BLOCK_XML_TEXT);
			Blockly.Xml.domToWorkspace(xml, BlockFactory.mainWorkspace);
		}

		/// <summary>
		/// Returns whether or not the current block open is the starter block.
		/// </summary>
		public static bool isStarterBlock()
		{
			var rootBlock = FactoryUtils.getRootBlock(BlockFactory.mainWorkspace);
			// The starter block does not have blocks nested into the factory_base block.
			return !(rootBlock.getChildren().Length > 0 ||
				// The starter block's name is the default, "block_type".
				rootBlock.getFieldValue("NAME").Trim().ToLowerCase() != "block_type" ||
				// The starter block has no connections.
				rootBlock.getFieldValue("CONNECTIONS") != "NONE" ||
				// The starter block has automatic inputs.
				rootBlock.getFieldValue("INLINE") != "AUTO");
		}
	}
}
