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
 * @fileoverview Stores and updates information about state and categories
 * in workspace factory. Each list element is either a separator or a category,
 * and each category stores its name, XML to load that category, color,
 * custom tags, and a unique ID making it possible to change category names and
 * move categories easily. Keeps track of the currently selected list
 * element. Also keeps track of all the user-created shadow blocks and
 * manipulates them as necessary.
 *
 * @author Emma Dauterman (evd2014)
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class WorkspaceFactoryModel
	{
		public JsArray<ListElement> toolboxList;
		public ListElement flyout;
		public JsArray<string> shadowBlocks;
		public ListElement selected;
		public bool hasVariableCategory;
		public bool hasProcedureCategory;
		public Element preloadXml;
		public Dictionary<string, object> options;
		public JsArray<string> libBlockTypes;
		public JsArray<string> importedBlockTypes;

		/// <summary>
		/// Class for a WorkspaceFactoryModel
		/// </summary>
		public WorkspaceFactoryModel()
		{
			// Ordered list of ListElement objects. Empty if there is a single flyout.
			this.toolboxList = new JsArray<ListElement>();
			// ListElement for blocks in a single flyout. Null if a toolbox exists.
			this.flyout = new ListElement(ListElement.TYPE_FLYOUT);
			// Array of block IDs for all user created shadow blocks.
			this.shadowBlocks = new JsArray<string>();
			// Reference to currently selected ListElement. Stored in this.toolboxList if
			// there are categories, or in this.flyout if blocks are displayed in a single
			// flyout.
			this.selected = this.flyout;
			// Boolean for if a Variable category has been added.
			this.hasVariableCategory = false;
			// Boolean for if a Procedure category has been added.
			this.hasProcedureCategory = false;
			// XML to be pre-loaded to workspace. Empty on default;
			this.preloadXml = Blockly.Xml.textToDom("<xml></xml>");
			// Options object to be configured for Blockly inject call.
			this.options = new Dictionary<string, object>();
			// Block Library block types.
			this.libBlockTypes = new JsArray<string>();
			// Imported block types.
			this.importedBlockTypes = new JsArray<string>();
			//
		}

		/// <summary>
		/// Given a name, determines if it is the name of a category already present.
		/// Used when getting a valid category name from the user.
		/// </summary>
		/// <param name="name"> String name to be compared against.</param>
		/// <returns>True if string is a used category name, false otherwise.</returns>
		public bool hasCategoryByName(string name)
		{
			for (var i = 0; i < this.toolboxList.Length; i++) {
				if (this.toolboxList[i].type == ListElement.TYPE_CATEGORY &&
					this.toolboxList[i].name == name) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines if a category with the "VARIABLE" tag exists.
		/// </summary>
		/// <returns>True if there exists a category with the Variables tag,
		/// false otherwise.</returns>
		public bool hasVariables()
		{
			return this.hasVariableCategory;
		}

		/// <summary>
		/// Determines if a category with the "PROCEDURE" tag exists.
		/// </summary>
		/// <returns>True if there exists a category with the Procedures tag,
		/// false otherwise.</returns>
		public bool hasProcedures()
		{
			return this.hasProcedureCategory;
		}

		/// <summary>
		/// Determines if the user has any elements in the toolbox. Uses the length of
		/// toolboxList.
		/// </summary>
		/// <returns>True if elements exist, false otherwise.</returns>
		public bool hasElements()
		{
			return this.toolboxList.Length > 0;
		}

		/// <summary>
		/// Given a ListElement, adds it to the toolbox list.
		/// </summary>
		/// <param name="element"> The element to be added to the list.</param>
		public void addElementToList(ListElement element)
		{
			// Update state if the copied category has a custom tag.
			this.hasVariableCategory = element.custom == "VARIABLE" ? true :
				this.hasVariableCategory;
			this.hasProcedureCategory = element.custom == "PROCEDURE" ? true :
				this.hasProcedureCategory;
			// Add element to toolboxList.
			this.toolboxList.Push(element);
			// Empty single flyout.
			this.flyout = null;
		}

		/// <summary>
		/// Given an index, deletes a list element and all associated data.
		/// </summary>
		/// <param name="index"> The index of the list element to delete.</param>
		public void deleteElementFromList(int index)
		{
			// Check if index is out of bounds.
			if (index < 0 || index >= this.toolboxList.Length) {
				return; // No entry to delete.
			}
			// Check if need to update flags.
			this.hasVariableCategory = this.toolboxList[index].custom == "VARIABLE" ?
				false : this.hasVariableCategory;
			this.hasProcedureCategory = this.toolboxList[index].custom == "PROCEDURE" ?
				false : this.hasProcedureCategory;
			// Remove element.
			this.toolboxList.Splice(index, 1);
		}

		/// <summary>
		/// Sets selected to be an empty category not in toolbox list if toolbox list
		/// is empty. Should be called when removing the last element from toolbox list.
		/// If the toolbox list is empty, selected stores the XML for the single flyout
		/// of blocks displayed.
		/// </summary>
		public void createDefaultSelectedIfEmpty()
		{
			if (this.toolboxList.Length == 0) {
				this.flyout = new ListElement(ListElement.TYPE_FLYOUT);
				this.selected = this.flyout;
			}
		}

		/// <summary>
		/// Moves a list element to a certain position in toolboxList by removing it
		/// and then inserting it at the correct index. Checks that indices are in
		/// bounds (throws error if not), but assumes that oldIndex is the correct index
		/// for list element.
		/// </summary>
		/// <param name="element"> The element to move in toolboxList.</param>
		/// <param name="newIndex">The index to insert the element at.</param> 
		/// <param  name="oldIndex">The index the element is currently at.</param> 
		public void moveElementToIndex(ListElement element, int newIndex,
			int oldIndex)
		{
			// Check that indexes are in bounds.
			if (newIndex < 0 || newIndex >= this.toolboxList.Length || oldIndex < 0 ||
				oldIndex >= this.toolboxList.Length) {
				throw new Exception("Index out of bounds when moving element in the model.");
			}
			this.deleteElementFromList(oldIndex);
			this.toolboxList.Splice(newIndex, 0, element);
		}

		/// <summary>
		/// Returns the ID of the currently selected element. Returns null if there are
		/// no categories (if selected == null).
		/// </summary>
		/// <returns>The ID of the element currently selected.</returns>
		public string getSelectedId()
		{
			return this.selected != null ? this.selected.id : null;
		}

		/// <summary>
		/// Returns the name of the currently selected category. Returns null if there
		/// are no categories (if selected == null) or the selected element is not
		/// a category (in which case its name is null).
		/// </summary>
		/// <returns>The name of the category currently selected.</returns>
		public string getSelectedName()
		{
			return this.selected != null ? this.selected.name : null;
		}

		/// <summary>
		/// Returns the currently selected list element object.
		/// </summary>
		/// <returns>The currently selected ListElement</returns>
		public ListElement getSelected()
		{
			return this.selected;
		}

		/// <summary>
		/// Sets list element currently selected by id.
		/// </summary>
		/// <param name="id"> ID of list element that should now be selected.</param>
		public void setSelectedById(string id)
		{
			this.selected = this.getElementById(id);
		}

		/// <summary>
		/// Given an ID of a list element, returns the index of that list element in
		/// toolboxList. Returns -1 if ID is not present.
		/// </summary>
		/// <param  name="id">The ID of list element to search for.</param>
		/// <returns>The index of the list element in toolboxList, or -1 if it
		/// doesn't exist.</returns>
		public int getIndexByElementId(string id)
		{
			for (var i = 0; i < this.toolboxList.Length; i++) {
				if (this.toolboxList[i].id == id) {
					return i;
				}
			}
			return -1;  // ID not present in toolboxList.
		}

		/// <summary>
		/// Given the ID of a list element, returns that ListElement object.
		/// </summary>
		/// <param name="id">The ID of element to search for.</param>
		/// <returns>Corresponding ListElement object in toolboxList, or
		///     null if that element does not exist.</returns>
		public ListElement getElementById(string id)
		{
			for (var i = 0; i < this.toolboxList.Length; i++) {
				if (this.toolboxList[i].id == id) {
					return this.toolboxList[i];
				}
			}
			return null;  // ID not present in toolboxList.
		}

		/// <summary>
		/// Given the index of a list element in toolboxList, returns that ListElement
		/// object.
		/// </summary>
		/// <param name="index"> The index of the element to return.</param>
		/// <returns>The corresponding ListElement object in toolboxList.</returns>
		public ListElement getElementByIndex(int index)
		{
			if (index < 0 || index >= this.toolboxList.Length) {
				return null;
			}
			return this.toolboxList[index];
		}

		/// <summary>
		/// Returns the XML to load the selected element.
		/// </summary>
		/// <returns>The XML of the selected element, or null if there is
		/// no selected element.</returns>
		public Element getSelectedXml()
		{
			return this.selected != null ? this.selected.xml : null;
		}

		/// <summary>
		/// Return ordered list of ListElement objects.
		/// </summary>
		/// <returns>ordered list of ListElement objects</returns>
		public JsArray<ListElement> getToolboxList()
		{
			return this.toolboxList;
		}

		/// <summary>
		/// Gets the ID of a category given its name.
		/// </summary>
		/// <param name="name"> Name of category.</param>
		/// <returns>ID of category</returns>
		public string getCategoryIdByName(string name)
		{
			for (var i = 0; i < this.toolboxList.Length; i++) {
				if (this.toolboxList[i].name == name) {
					return this.toolboxList[i].id;
				}
			}
			return null;  // Name not present in toolboxList.
		}

		/// <summary>
		/// Clears the toolbox list, deleting all ListElements.
		/// </summary>
		public void clearToolboxList()
		{
			this.toolboxList = new JsArray<ListElement>();
			this.hasVariableCategory = false;
			this.hasProcedureCategory = false;
			this.shadowBlocks = new JsArray<string>();
			this.selected.xml = Blockly.Xml.textToDom("<xml></xml>");
		}

		/// <summary>
		/// Class for a ListElement
		/// Adds a shadow block to the list of shadow blocks.
		/// </summary>
		/// <param name="blockId"> The unique ID of block to be added.</param>
		public void addShadowBlock(string blockId)
		{
			this.shadowBlocks.Push(blockId);
		}

		/// <summary>
		/// Removes a shadow block ID from the list of shadow block IDs if that ID is
		/// in the list.
		/// </summary>
		/// <param name="blockId"> The unique ID of block to be removed.</param>
		public void removeShadowBlock(string blockId)
		{
			for (var i = 0; i < this.shadowBlocks.Length; i++) {
				if (this.shadowBlocks[i] == blockId) {
					this.shadowBlocks.Splice(i, 1);
					return;
				}
			}
		}

		/// <summary>
		/// Determines if a block is a shadow block given a unique block ID.
		/// </summary>
		/// <param  name="blockId"> The unique ID of the block to examine.</param>
		/// <returns>True if the block is a user-generated shadow block, false
		///    otherwise.</returns>
		public bool isShadowBlock(string blockId)
		{
			for (var i = 0; i < this.shadowBlocks.Length; i++) {
				if (this.shadowBlocks[i] == blockId) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Given a set of blocks currently loaded, returns all blocks in the workspace
		/// that are user generated shadow blocks.
		/// </summary>
		/// <param  name="blocks"> Array of blocks currently loaded.</param>
		/// <returns>Array of user-generated shadow blocks currently
		///   loaded.</returns>
		public JsArray<Blockly.Block> getShadowBlocksInWorkspace(JsArray<Blockly.Block> workspaceBlocks)
		{
			var shadowsInWorkspace = new JsArray<Blockly.Block>();
			for (var i = 0; i < workspaceBlocks.Length; i++) {
				if (this.isShadowBlock(workspaceBlocks[i].id)) {
					shadowsInWorkspace.Push(workspaceBlocks[i]);
				}
			}
			return shadowsInWorkspace;
		}

		/// <summary>
		/// Adds a custom tag to a category, updating state variables accordingly.
		/// Only accepts "VARIABLE" and "PROCEDURE" tags.
		/// </summary>
		/// <param  name="category">The category to add the tag to.</param>
		/// <param name="tag"> The custom tag to add to the category.</param>
		public void addCustomTag(ListElement category, string tag)
		{
			// Only update list elements that are categories.
			if (category.type != ListElement.TYPE_CATEGORY) {
				return;
			}
			// Only update the tag to be "VARIABLE" or "PROCEDURE".
			if (tag == "VARIABLE") {
				this.hasVariableCategory = true;
				category.custom = "VARIABLE";
			}
			else if (tag == "PROCEDURE") {
				this.hasProcedureCategory = true;
				category.custom = "PROCEDURE";
			}
		}

		/// <summary>
		/// Have basic pre-loaded workspace working
		/// Saves XML as XML to be pre-loaded into the workspace.
		/// </summary>
		/// <param name="xml"> The XML to be saved.</param>
		public void savePreloadXml(Element xml)
		{
			this.preloadXml = xml;
		}

		/// <summary>
		/// Gets the XML to be pre-loaded into the workspace.
		/// </summary>
		/// <returns>The XML for the workspace.</returns>
		public Element getPreloadXml()
		{
			return this.preloadXml;
		}

		/// <summary>
		/// Sets a new options object for injecting a Blockly workspace.
		/// </summary>
		/// <param name="options"> Options object for injecting a Blockly workspace.</param>
		public void setOptions(Dictionary<string, object> options)
		{
			this.options = options;
		}

		/// <summary>
		/// Returns an array of all the block types currently being used in the toolbox
		/// and the pre-loaded blocks. No duplicates.
		/// TODO(evd2014): Move pushBlockTypesToList to FactoryUtils.
		/// </summary>
		/// <returns>Array of block types currently being used.</returns>
		public JsArray<string> getAllUsedBlockTypes()
		{
			var blockTypeList = new JsArray<string>();

			// Given XML for the workspace, adds all block types included in the XML
			// to the list, not including duplicates.
			var pushBlockTypesToList = new Action<Element, JsArray<string>>((xml, list) => {
				// Get all block XML nodes.
				var blocks = xml.GetElementsByTagName("block");

				// Add block types if not already in list.
				for (var i = 0; i < blocks.Length; i++) {
					var type = ((Element)blocks[i]).GetAttribute("type");
					if (list.IndexOf(type) == -1) {
						list.Push(type);
					}
				}
			});

			if (this.flyout != null) {
				// If has a single flyout, add block types for the single flyout.
				pushBlockTypesToList(this.getSelectedXml(), blockTypeList);
			}
			else {
				// If has categories, add block types for each category.

				for (var i = 0; i < this.toolboxList.Length; i++) {
					var category = this.toolboxList[i];
					if (category.type == ListElement.TYPE_CATEGORY) {
						pushBlockTypesToList(category.xml, blockTypeList);
					}
				}
			}

			// Add the block types from any pre-loaded blocks.
			pushBlockTypesToList(this.getPreloadXml(), blockTypeList);

			return blockTypeList;
		}

		/// <summary>
		/// Adds new imported block types to the list of current imported block types.
		/// </summary>
		/// <param name="blockTypes"> Array of block types imported.</param>
		public void addImportedBlockTypes(JsArray<string> blockTypes)
		{
			this.importedBlockTypes = this.importedBlockTypes.Concat(blockTypes);
		}

		/// <summary>
		/// Updates block types in block library.
		/// </summary>
		/// <param name="blockTypes"> Array of block types in block library.</param>
		public void updateLibBlockTypes(JsArray<string> blockTypes)
		{
			this.libBlockTypes = blockTypes;
		}

		/// <summary>
		/// Determines if a block type is defined as a standard block, in the block
		/// library, or as an imported block.
		/// </summary>
		/// <param name="blockType"> Block type to check.</param>
		/// <returns>True if blockType is defined, false otherwise.</returns>
		public bool isDefinedBlockType(string blockType)
		{
			var isStandardBlock = Array.IndexOf(StandardCategories.coreBlockTypes, blockType)
				!= -1;
			var isLibBlock = this.libBlockTypes.IndexOf(blockType) != -1;
			var isImportedBlock = this.importedBlockTypes.IndexOf(blockType) != -1;
			return (isStandardBlock || isLibBlock || isImportedBlock);
		}

		/// <summary>
		/// Checks if any of the block types are already defined.
		/// </summary>
		/// <param  name="blockTypes">Array of block types.</param>
		/// <returns>True if a block type in the array is already defined,
		///    false if none of the blocks are already defined.</returns>
		public bool hasDefinedBlockTypes(JsArray<string> blockTypes)
		{
			for (var i = 0; i < blockTypes.Length; i++) {
				var blockType = blockTypes[i];
				if (this.isDefinedBlockType(blockType)) {
					return true;
				}
			}
			return false;
		}
	}

	public class ListElement
	{
		public string type;
		public Element xml;
		public string name;
		public string id;
		public string color;
		public string custom;

		/// <summary>
		/// Class for a ListElement.
		/// </summary>
		public ListElement(string type, string opt_name = null)
		{
			this.type = type;
			// XML DOM element to load the element.
			this.xml = Blockly.Xml.textToDom("<xml></xml>");
			// Name of category. Can be changed by user. Null if separator.
			this.name = opt_name != null ? opt_name : null;
			// Unique ID of element. Does not change.
			this.id = Blockly.Core.genUid();
			// Color of category. Default is no color. Null if separator.
			this.color = null;
			// Stores a custom tag, if necessary. Null if no custom tag or separator.
			this.custom = null;
		}

		// List element types.
		public static string TYPE_CATEGORY = "category";
		public static string TYPE_SEPARATOR = "separator";
		public static string TYPE_FLYOUT = "flyout";

		/// <summary>
		/// Saves a category by updating its XML (does not save XML for
		/// elements that are not categories).
		/// </summary>
		/// <param  name="workspace">The workspace to save category entry
		/// from.</param>
		public void saveFromWorkspace(Blockly.Workspace workspace)
		{
			// Only save XML for categories and flyouts.
			if (this.type == ListElement.TYPE_FLYOUT ||
				this.type == ListElement.TYPE_CATEGORY) {
				this.xml = Blockly.Xml.workspaceToDom(workspace);
			}
		}

		/// <summary>
		/// Changes the name of a category object given a new name. Returns if
		/// not a category.
		/// </summary>
		/// <param name="name"> New name of category.</param>
		public void changeName(string name)
		{
			// Only update list elements that are categories.
			if (this.type != ListElement.TYPE_CATEGORY) {
				return;
			}
			this.name = name;
		}

		/// <summary>
		/// Sets the color of a category. If tries to set the color of something other
		/// than a category, returns.
		/// </summary>
		/// <param name="color"> The color that should be used for that category.</param>
		public void changeColor(string color)
		{
			if (this.type != ListElement.TYPE_CATEGORY) {
				return;
			}
			this.color = color;
		}

		/// <summary>
		/// Makes a copy of the original element and returns it. Everything about the
		/// copy is identical except for its ID.
		/// </summary>
		/// <returns>The copy of the ListElement.</returns>
		public ListElement copy()
		{
			var copy = new ListElement(this.type);
			// Generate a unique ID for the element.
			copy.id = Blockly.Core.genUid();
			// Copy all attributes except ID.
			copy.name = this.name;
			copy.xml = this.xml;
			copy.color = this.color;
			copy.custom = this.custom;
			// Return copy.
			return copy;
		}
	}
}
