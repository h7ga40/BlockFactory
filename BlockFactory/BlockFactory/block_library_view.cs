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
 * @fileoverview Javascript for BlockLibraryView class. It manages the display
 * of the Block Library dropdown, save, and delete buttons.
 *
 * @author quachtina96 (Tina Quach)
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockLibraryView
	{
		public HTMLDivElement dropdown;
		public Dictionary<string, object> optionMap;
		public HTMLButtonElement saveButton;
		public HTMLButtonElement deleteButton;

		/// <summary>
		/// BlockLibraryView Class
		/// </summary>
		public BlockLibraryView()
		{
			// Div element to contain the block types to choose from.
			this.dropdown = (HTMLDivElement)Document.GetElementById("dropdownDiv_blockLib");
			// Map of block type to corresponding "a" element that is the option in the
			// dropdown. Used to quickly and easily get a specific option.
			this.optionMap = new Dictionary<string, object>();
			// Save and delete buttons.
			this.saveButton = (HTMLButtonElement)Document.GetElementById("saveToBlockLibraryButton");
			this.deleteButton = (HTMLButtonElement)Document.GetElementById("removeBlockFromLibraryButton");
			// Initially, user should not be able to delete a block. They must save a
			// block or select a stored block first.
			this.deleteButton.Disabled = true;
		}

		/// <summary>
		/// Creates a node of a given element type and appends to the node with given ID.
		/// </summary>
		/// <param name="blockType"> Type of block.</param>
		/// <param name="selected">Whether or not the option should be selected on
		/// the dropdown.</param>
		public void addOption(string blockType, bool selected)
		{
			// Create option.
			var option = goog.dom.createDom("a", new Dictionary<string, string>{
				{ "id", "dropdown_" + blockType},
				{"class", "blockLibOpt"}
			}, blockType);

			// Add option to dropdown.
			this.dropdown.AppendChild(option);
			this.optionMap[blockType] = option;

			// Select the block.
			if (selected) {
				this.setSelectedBlockType(blockType);
			}
		}

		/// <summary>
		/// Sets a given block type to selected and all other blocks to deselected.
		/// If null, deselects all blocks.
		/// </summary>
		/// <param name="blockTypeToSelect"> Type of block to select or null.</param>
		public void setSelectedBlockType(string blockTypeToSelect)
		{
			// Select given block type and deselect all others. Will deselect all blocks
			// if null or invalid block type selected.
			foreach (var blockType in this.optionMap.Keys) {
				var option = (HTMLElement)this.optionMap[blockType];
				if (blockType == blockTypeToSelect) {
					this.selectOption_(option);
				}
				else {
					this.deselectOption_(option);
				}
			}
		}

		/// <summary>
		/// Selects a given option.
		/// </summary>
		/// <param name="option">HTML "a" element in the dropdown that represents
		/// a particular block type.</param>
		private void selectOption_(HTMLElement option)
		{
			goog.dom.classlist.add(option, "dropdown-content-selected");
		}

		/// <summary>
		/// Deselects a given option.
		/// </summary>
		/// <param name="option">HTML "a" element in the dropdown that represents
		/// a particular block type.</param>
		private void deselectOption_(HTMLElement option)
		{
			goog.dom.classlist.remove(option, "dropdown-content-selected");
		}

		/// <summary>
		/// Updates the save and delete buttons to represent how the current block will
		/// be saved by including the block type in the button text as well as indicating
		/// whether the block is being saved or updated.
		/// </summary>
		/// <param name="blockType"> The type of block being edited.</param>
		/// <param name="isInLibrary"> Whether the block type is in the library.</param>
		/// <param name="savedChanges">Whether changes to block have been saved.</param>
		public void updateButtons(string blockType, bool isInLibrary, bool savedChanges)
		{
			if (!String.IsNullOrEmpty(blockType)) {
				// User is editing a block.

				if (!isInLibrary) {
					// Block type has not been saved to library yet. Disable the delete button
					// and allow user to save.
					this.saveButton.TextContent = "Save \"" + blockType + "\"";
					this.saveButton.Disabled = false;
					this.deleteButton.Disabled = true;
				}
				else {
					// Block type has already been saved. Disable the save button unless the
					// there are unsaved changes (checked below).
					this.saveButton.TextContent = "Update \"" + blockType + "\"";
					this.saveButton.Disabled = true;
					this.deleteButton.Disabled = false;
				}
				this.deleteButton.TextContent = "Delete \"" + blockType + "\"";

				// If changes to block have been made and are not saved, make button
				// green to encourage user to save the block.
				if (!savedChanges) {
					var buttonFormatClass = "button_warn";

					// If block type is the default, "block_type", make button red to alert
					// user.
					if (blockType == "block_type") {
						buttonFormatClass = "button_alert";
					}
					goog.dom.classlist.add(this.saveButton, buttonFormatClass);
					this.saveButton.Disabled = false;

				}
				else {
					// No changes to save.
					var classesToRemove = new JsArray<string> { "button_alert", "button_warn" };
					goog.dom.classlist.removeAll(this.saveButton, classesToRemove);
					this.saveButton.Disabled = true;
				}
			}
		}

		/// <summary>
		/// Removes option currently selected in dropdown from dropdown menu.
		/// </summary>
		public void removeSelectedOption()
		{
			var selectedOption = this.getSelectedOption();
			//this.dropdown.RemoveNode(selectedOption);
			this.dropdown.RemoveChild(selectedOption);
		}

		/// <summary>
		/// Returns block type of selected block.
		/// </summary>
		/// <returns>Type of block selected.</returns>
		public string getSelectedBlockType()
		{
			var selectedOption = this.getSelectedOption();
			var blockType = selectedOption.TextContent;
			return blockType;
		}

		/// <summary>
		/// Returns selected option.
		/// </summary>
		/// <returns>HTML "a" element that is the option for a block type.</returns>
		public Element getSelectedOption()
		{
			return (Element)this.dropdown.GetElementsByClassName("dropdown-content-selected")[0];
		}

		/// <summary>
		/// Removes all options from dropdown.
		/// </summary>
		public void clearOptions()
		{
			var blockOpts = this.dropdown.GetElementsByClassName("blockLibOpt");
			Node option;
			while ((option = blockOpts[0]) != null) {
				option.ParentNode.RemoveChild(option);
			}
		}
	}
}