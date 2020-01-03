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
 * @fileoverview Contains the code for Block Library Controller, which
 * depends on Block Library Storage and Block Library UI. Provides the
 * interfaces for the user to
 *  - save their blocks to the browser
 *  - re-open and edit saved blocks
 *  - delete blocks
 *  - clear their block library
 * Depends on BlockFactory functions defined in factory.js.
 *
 * @author quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockLibraryController
	{
		public string name;
		public BlockLibraryStorage storage;
		public BlockLibraryView view;

		/// <summary>
		/// Block Library Controller Class
		/// </summary>
		/// <param name="blockLibraryName"> Desired name of Block Library, also used
		///   to create the key for where it's stored in local storage.</param>
		/// <param name="opt_blockLibraryStorage"> Optional storage
		/// object that allows user to import a block library.</param>
		public BlockLibraryController(string blockLibraryName, BlockLibraryStorage opt_blockLibraryStorage = null)
		{
			this.name = blockLibraryName;
			// Create a new, empty Block Library Storage object, or load existing one.
			this.storage = opt_blockLibraryStorage ?? new BlockLibraryStorage(this.name);
			// The BlockLibraryView object handles the proper updating and formatting of
			// the block library dropdown.
			this.view = new BlockLibraryView();
		}

		/// <summary>
		/// Returns the block type of the block the user is building.
		/// </summary>
		/// <returns>The current block's type.</returns>
		public string getCurrentBlockType()
		{
			var rootBlock = FactoryUtils.getRootBlock(BlockFactory.mainWorkspace);
			var blockType = rootBlock.getFieldValue("NAME").Trim().ToLowerCase();
			// Replace white space with underscores
			return blockType.Replace(new Regex(@"\W", RegexOptions.Multiline), "_").Replace(new Regex(@"^(\d)"), "_\\1");
		}

		/// <summary>
		/// Removes current block from Block Library and updates the save and delete
		/// buttons so that user may save block to library and but not delete.
		/// </summary>
		/// <param name="blockType">Type of block.</param> 
		public void removeFromBlockLibrary()
		{
			var blockType = this.getCurrentBlockType();
			this.storage.removeBlock(blockType);
			this.storage.saveToLocalStorage();
			this.populateBlockLibrary();
			this.view.updateButtons(blockType, false, false);
		}

		/// <summary>
		/// Updates the workspace to show the block user selected from library
		/// </summary>
		/// <param name="blockType"> Block to edit on block factory.</param>
		public void openBlock(string blockType)
		{
			if (String.IsNullOrEmpty(blockType)) {
				var xml = this.storage.getBlockXml(blockType);
				BlockFactory.mainWorkspace.clear();
				Blockly.Xml.domToWorkspace(xml, BlockFactory.mainWorkspace);
				BlockFactory.mainWorkspace.clearUndo();
			}
			else {
				BlockFactory.showStarterBlock();
				this.view.setSelectedBlockType(null);
			}
		}

		/// <summary>
		/// Returns type of block selected from library.
		/// </summary>
		/// <returns>Type of block selected.</returns>
		public string getSelectedBlockType()
		{
			return this.view.getSelectedBlockType();
		}

		/// <summary>
		/// Confirms with user before clearing the block library in local storage and
		/// updating the dropdown and displaying the starter block (factory_base).
		/// </summary>
		public void clearBlockLibrary()
		{
			var check = Window.Confirm("Delete all blocks from library?");
			if (check) {
				// Clear Block Library Storage.
				this.storage.clear();
				this.storage.saveToLocalStorage();
				// Update dropdown.
				this.view.clearOptions();
				// Show default block.
				BlockFactory.showStarterBlock();
				// User may not save the starter block, but will get explicit instructions
				// upon clicking the red save button.
				this.view.updateButtons(null, false, false);
			}
		}

		/// <summary>
		/// Saves current block to local storage and updates dropdown.
		/// </summary>
		public void saveToBlockLibrary()
		{
			var blockType = this.getCurrentBlockType();
			// If user has not changed the name of the starter block.
			if (blockType == "block_type") {
				// Do not save block if it has the default type, "block_type".
				Window.Alert("You cannot save a block under the name \"block_type\". Try changing " +
					"the name before saving. Then, click on the \"Block Library\" button " +
					"to view your saved blocks.");
				return;
			}

			// Create block XML.
			var xmlElement = goog.dom.createDom("xml");
			var block = FactoryUtils.getRootBlock(BlockFactory.mainWorkspace);
			xmlElement.AppendChild(Blockly.Xml.blockToDomWithXY(block));

			// Do not add option again if block type is already in library.
			if (!this.has(blockType)) {
				this.view.addOption(blockType, true);
			}

			// Save block.
			this.storage.addBlock(blockType, xmlElement);
			this.storage.saveToLocalStorage();

			// Show saved block without other stray blocks sitting in Block Factory's
			// main workspace.
			this.openBlock(blockType);

			// Add select handler to the new option.
			this.addOptionSelectHandler(blockType);
		}

		/// <summary>
		/// Checks to see if the given blockType is already in Block Library
		/// </summary>
		/// <param name="blockType"> Type of block.</param>
		/// <returns>Boolean indicating whether or not block is in the library.</returns>
		public bool has(string blockType)
		{
			var blockLibrary = this.storage.blocks;
			return (blockLibrary.ContainsKey(blockType) && blockLibrary[blockType] != null);
		}

		/// <summary>
		/// Populates the dropdown menu.
		/// </summary>
		public void populateBlockLibrary()
		{
			this.view.clearOptions();
			// Add an unselected option for each saved block.
			var blockLibrary = this.storage.blocks;
			foreach (var blockType in blockLibrary.Keys) {
				this.view.addOption(blockType, false);
			}
			this.addOptionSelectHandlers();
		}

		/// <summary>
		/// Return block library mapping block type to XML.
		/// </summary>
		/// <returns>Object mapping block type to XML text.</returns>
		public Dictionary<string, object> getBlockLibrary()
		{
			return this.storage.getBlockXmlTextMap();
		}

		/// <summary>
		/// Return stored XML of a given block type.
		/// </summary>
		/// <param name="blockType"> The type of block.</param>
		/// <returns>XML element of a given block type or null.</returns>
		public Element getBlockXml(string blockType)
		{
			return this.storage.getBlockXml(blockType);
		}

		/// <summary>
		/// Set the block library storage object from which exporter exports.
		/// </summary>
		/// <param name="blockLibStorage"></param> Block Library Storage object.
		public void setBlockLibraryStorage(BlockLibraryStorage blockLibStorage)
		{
			this.storage = blockLibStorage;
		}

		/// <summary>
		/// Get the block library storage object from which exporter exports.
		/// </summary>
		/// <returns>blockLibStorage Block Library Storage object
		///    that stores the blocks.</returns>
		public BlockLibraryStorage getBlockLibraryStorage()
		{
			return this.storage;
		}

		/// <summary>
		/// Get the block library storage object from which exporter exports.
		/// </summary>
		/// <returns>True if the Block Library is empty, false otherwise.</returns>
		public bool hasEmptyBlockLibrary()
		{
			return this.storage.isEmpty();
		}

		/// <summary>
		/// Get all block types stored in block library.
		/// </summary>
		/// <returns>Array of block types.</returns>
		public JsArray<string> getStoredBlockTypes()
		{
			return this.storage.getBlockTypes();
		}

		/// <summary>
		/// Sets the currently selected block option to none.
		/// </summary>
		public void setNoneSelected()
		{
			this.view.setSelectedBlockType(null);
		}

		/// <summary>
		/// If there are unsaved changes to the block in open in Block Factory
		/// and the block is not the starter block, check if user wants to proceed,
		/// knowing that it will cause them to lose their changes.
		/// </summary>
		/// <returns>Whether or not to proceed.</returns>
		public bool warnIfUnsavedChanges()
		{
			if (!FactoryUtils.savedBlockChanges(this)) {
				return Window.Confirm("You have unsaved changes. By proceeding without saving " +
					" your block first, you will lose these changes.");
			}
			return true;
		}

		/// <summary>
		/// Add select handler for an option of a given block type. The handler will to
		/// update the view and the selected block accordingly.
		/// </summary>
		/// <param name="blockType"> The type of block represented by the option is for.</param>
		public void addOptionSelectHandler(string blockType)
		{
			var self = this;

			// Click handler for a block option. Sets the block option as the selected
			// option and opens the block for edit in Block Factory.
			var setSelectedAndOpen_ = new Action<Node>((blockOption2) => {
				var blockType2 = blockOption2.TextContent;
				self.view.setSelectedBlockType(blockType2);
				self.openBlock(blockType2);
				// The block is saved in the block library and all changes have been saved
				// when the user opens a block from the block library dropdown.
				// Thus, the buttons show up as a disabled update button and an enabled
				// delete.
				self.view.updateButtons(blockType2, true, true);
				BlockFactory.blocklyFactory.closeModal();
			});

			// Returns a block option select handler.
			var makeOptionSelectHandler_ = new Func<Node, Action>((blockOption2) => {
				return () => {
					// If there are unsaved changes warn user, check if they'd like to
					// proceed with unsaved changes, and act accordingly.
					var proceedWithUnsavedChanges = self.warnIfUnsavedChanges();
					if (!proceedWithUnsavedChanges) {
						return;
					}
					setSelectedAndOpen_(blockOption2);
				};
			});

			// Assign a click handler to the block option.
			var blockOption = (Node)this.view.optionMap[blockType];
			// Use an additional closure to correctly assign the tab callback.
			blockOption.AddEventListener(
				"click", makeOptionSelectHandler_(blockOption), false);
		}

		/// <summary>
		/// Add select handlers to each option to update the view and the selected
		/// blocks accordingly.
		/// </summary>
		public void addOptionSelectHandlers()
		{
			// Assign a click handler to each block option.
			foreach (var blockType in this.view.optionMap.Keys) {
				this.addOptionSelectHandler(blockType);
			}
		}

		/// <summary>
		/// Update the save and delete buttons based on the current block type of the
		/// block the user is currently editing.
		/// </summary>
		/// <param name="savedChanges">Whether changes to the block have been saved.</param> 
		public void updateButtons(bool savedChanges)
		{
			var blockType = this.getCurrentBlockType();
			var isInLibrary = this.has(blockType);
			this.view.updateButtons(blockType, isInLibrary, savedChanges);
		}
	}
}
