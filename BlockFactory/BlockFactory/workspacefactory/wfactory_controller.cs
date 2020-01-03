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
 * @fileoverview Contains the controller code for workspace factory. Depends
 * on the model and view objects (created as internal variables) and interacts
 * with previewWorkspace and toolboxWorkspace (internal references stored to
 * both). Also depends on standard_categories.js for standard Blockly
 * categories. Provides the functionality for the actions the user can initiate:
 * - adding and removing categories
 * - switching between categories
 * - printing and downloading configuration xml
 * - updating the preview workspace
 * - changing a category name
 * - moving the position of a category.
 *
 * @author Emma Dauterman (evd2014)
 */
using System;
using System.Collections.Generic;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class WorkspaceFactoryController
	{
		public Element toolbox;
		public WorkspaceSvg toolboxWorkspace;
		public WorkspaceSvg previewWorkspace;
		public WorkspaceFactoryModel model;
		public WorkspaceFactoryView view;
		public WorkspaceFactoryGenerator generator;
		public string selectedMode;
		public bool keyEventsEnabled;
		public bool hasUnsavedToolboxChanges;
		public bool hasUnsavedPreloadChanges;

		/// <summary>
		/// Class for a WorkspaceFactoryController
		/// </summary>
		/// <param name="toolboxName">Name of workspace toolbox XML.</param>
		/// <param name="toolboxDiv">Name of div to inject toolbox workspace in.</param>
		/// <param name="param">Name of div to inject preview workspace in.
		public WorkspaceFactoryController(string toolboxName, string toolboxDiv, string previewDiv)
		{
			// Toolbox XML element for the editing workspace.
			this.toolbox = Document.GetElementById(toolboxName);

			// Workspace for user to drag blocks in for a certain category.
			this.toolboxWorkspace = Blockly.Core.inject(toolboxDiv,
			new Dictionary<string, object> {
				{"grid", new Dictionary<string, object> {
					{ "spacing", 25 },
					{ "length", 3 },
					{ "colour", "#ccc" },
					{ "snap", true } }
				},
				{ "media", "../../media/" },
				{ "toolbox", this.toolbox }
			});

			// Workspace for user to preview their changes.
			this.previewWorkspace = Blockly.Core.inject(previewDiv, new Dictionary<string, object> {
				{"grid", new Dictionary<string, object> {
					{ "spacing", 25 },
					{ "length", 3 },
					{ "colour", "#ccc" },
					{ "snap", true } }
				},
				{ "media", "../../media/"},
				{ "toolbox", "<xml></xml>"},
				{ "zoom", new Dictionary<string, object> {
					{ "controls", true },
					{ "wheel", true } }
				}
			});

			// Model to keep track of categories and blocks.
			this.model = new WorkspaceFactoryModel();
			// Updates the category tabs.
			this.view = new WorkspaceFactoryView();
			// Generates XML for categories.
			this.generator = new WorkspaceFactoryGenerator(this.model);
			// Tracks which editing mode the user is in. Toolbox mode on start.
			this.selectedMode = WorkspaceFactoryController.MODE_TOOLBOX;
			// True if key events are enabled, false otherwise.
			this.keyEventsEnabled = true;
			// True if there are unsaved changes in the toolbox, false otherwise.
			this.hasUnsavedToolboxChanges = false;
			// True if there are unsaved changes in the preloaded blocks, false otherwise.
			this.hasUnsavedPreloadChanges = false;
		}

		// Toolbox editing mode. Changes the user makes to the workspace updates the
		// toolbox.
		public static string MODE_TOOLBOX = "toolbox";
		// Pre-loaded workspace editing mode. Changes the user makes to the workspace
		// udpates the pre-loaded blocks.
		public static string MODE_PRELOAD = "preload";

		/// <summary>
		/// Currently prompts the user for a name, checking that it's valid (not used
		/// before), and then creates a tab and switches to it.
		/// </summary>
		public void addCategory()
		{
			// Transfers the user's blocks to a flyout if it's the first category created.
			this.transferFlyoutBlocksToCategory();

			// After possibly creating a category, check again if it's the first category.
			var isFirstCategory = !this.model.hasElements();
			// Get name from user.
			var name = this.promptForNewCategoryName("Enter the name of your new category:");
			if (String.IsNullOrEmpty(name)) {  // Exit if cancelled.
				return;
			}
			// Create category.
			this.createCategory(name);
			// Switch to category.
			this.switchElement(this.model.getCategoryIdByName(name));

			// Sets the default options for injecting the workspace
			// when there are categories if adding the first category.
			if (isFirstCategory) {
				this.view.setCategoryOptions(this.model.hasElements());
				this.generateNewOptions();
			}
			// Update preview.
			this.updatePreview();
		}

		/// <summary>
		/// Helper method for addCategory. Adds a category to the view given a name, ID,
		/// and a boolean for if it's the first category created. Assumes the category
		/// has already been created in the model. Does not switch to category.
		/// </summary>
		/// <param name="name">Name of category being added.</param>
		/// <param neme="id">The ID of the category being added.</param>
		public void createCategory(string name)
		{
			// Create empty category
			var category = new ListElement(ListElement.TYPE_CATEGORY, name);
			this.model.addElementToList(category);
			// Create new category.
			var tab = this.view.addCategoryRow(name, category.id);
			this.addClickToSwitch(tab, category.id);
		}

		/// <summary>
		/// Given a tab and a ID to be associated to that tab, adds a listener to
		/// that tab so that when the user clicks on the tab, it switches to the
		/// element associated with that ID.
		/// </summary>
		/// <param name="tab">The DOM element to add the listener to.</param>
		/// <param name="id">The ID of the element to switch to when tab is clicked.</param>
		public void addClickToSwitch(Element tab, string id)
		{
			var self = this;
			var clickFunction = new Func<string, Action>((id2) => {  // Keep this in scope for switchElement.
				return () => {
					self.switchElement(id2);
				};
			});
			this.view.bindClick(tab, clickFunction(id));
		}

		/// <summary>
		/// Transfers the blocks in the user's flyout to a new category if
		/// the user is creating their first category and their workspace is not
		/// empty. Should be called whenever it is possible to switch from single flyout
		/// to categories (not including importing).
		/// </summary>
		public void transferFlyoutBlocksToCategory()
		{
			// Saves the user's blocks from the flyout in a category if there is no
			// toolbox and the user has dragged in blocks.
			if (!this.model.hasElements() &&
				  this.toolboxWorkspace.getAllBlocks().Length > 0) {
				// Create the new category.
				this.createCategory("Category 1");
				// Set the new category as selected.
				var id = this.model.getCategoryIdByName("Category 1");
				this.model.setSelectedById(id);
				this.view.setCategoryTabSelection(id, true);
				// Allow user to use the default options for injecting with categories.
				this.view.setCategoryOptions(this.model.hasElements());
				this.generateNewOptions();
				// Update preview here in case exit early.
				this.updatePreview();
			}
		}

		/// <summary>
		/// Attached to "-" button. Checks if the user wants to delete
		/// the current element.  Removes the element and switches to another element.
		/// When the last element is removed, it switches to a single flyout mode.
		/// </summary>
		public void removeElement()
		{
			// Check that there is a currently selected category to remove.
			if (this.model.getSelected() == null) {
				return;
			}

			// Check if user wants to remove current category.
			var check = Window.Confirm("Are you sure you want to delete the currently selected "
				  + this.model.getSelected().type + "?");
			if (!check) { // If cancelled, exit.
				return;
			}

			var selectedId = this.model.getSelectedId();
			var selectedIndex = this.model.getIndexByElementId(selectedId);
			// Delete element visually.
			this.view.deleteElementRow(selectedId, selectedIndex);
			// Delete element in model.
			this.model.deleteElementFromList(selectedIndex);

			// Find next logical element to switch to.
			var next = this.model.getElementByIndex(selectedIndex);
			if (next == null && this.model.hasElements()) {
				next = this.model.getElementByIndex(selectedIndex - 1);
			}
			var nextId = next != null ? next.id : null;

			// Open next element.
			this.clearAndLoadElement(nextId);

			// If no element to switch to, display message, clear the workspace, and
			// set a default selected element not in toolbox list in the model.
			if (String.IsNullOrEmpty(nextId)) {
				Window.Alert("You currently have no categories or separators. All your blocks" +
					" will be displayed in a single flyout.");
				this.toolboxWorkspace.clear();
				this.toolboxWorkspace.clearUndo();
				this.model.createDefaultSelectedIfEmpty();
			}
			// Update preview.
			this.updatePreview();
		}

		/// <summary>
		/// Gets a valid name for a new category from the user.
		/// </summary>
		/// <param name="promptString">Prompt for the user to enter a name.</param>
		/// <param name="opt_oldName">The current name.</param>
		/// <returns>Valid name for a new category, or null if cancelled.</returns>
		public string promptForNewCategoryName(string promptString, string opt_oldName = null)
		{
			string name;
			var defaultName = opt_oldName;
			do {
				name = Window.Prompt(promptString, defaultName);
				if (String.IsNullOrEmpty(name)) {  // If cancelled.
					return null;
				}
				defaultName = name;
			} while (this.model.hasCategoryByName(name));
			return name;
		}

		/// <summary>
		/// Switches to a new tab for the element given by ID. Stores XML and blocks
		/// to reload later, updates selected accordingly, and clears the workspace
		/// and clears undo, then loads the new element.
		/// </summary>
		/// <param name="id"> ID of tab to be opened, must be valid element ID.</param>
		public void switchElement(string id)
		{
			// Disables events while switching so that Blockly delete and create events
			// don't update the preview repeatedly.
			Blockly.Events.disable();
			// Caches information to reload or generate XML if switching to/from element.
			// Only saves if a category is selected.
			if (this.model.getSelectedId() != null && id != null) {
				this.model.getSelected().saveFromWorkspace(this.toolboxWorkspace);
			}
			// Load element.
			this.clearAndLoadElement(id);
			// Enable Blockly events again.
			Blockly.Events.enable();
		}

		/// <summary>
		/// Switches to a new tab for the element by ID. Helper for switchElement.
		/// Updates selected, clears the workspace and clears undo, loads a new element.
		/// </summary>
		/// <param name="id"> ID of category to load.</param>
		public void clearAndLoadElement(string id)
		{
			// Unselect current tab if switching to and from an element.
			if (this.model.getSelectedId() != null && id != null) {
				this.view.setCategoryTabSelection(this.model.getSelectedId(), false);
			}

			// If switching to another category, set category selection in the model and
			// view.
			if (id != null) {
				// Set next category.
				this.model.setSelectedById(id);

				// Clears workspace and loads next category.
				this.clearAndLoadXml_(this.model.getSelectedXml());

				// Selects the next tab.
				this.view.setCategoryTabSelection(id, true);

				// Order blocks as shown in flyout.
				this.toolboxWorkspace.cleanUp();

				// Update category editing buttons.
				this.view.updateState(this.model.getIndexByElementId
					(this.model.getSelectedId()), this.model.getSelected());
			}
			else {
				// Update category editing buttons for no categories.
				this.view.updateState(-1, null);
			}
		}

		/// <summary>
		/// Tied to "Export" button. Gets a file name from the user and downloads
		/// the corresponding configuration XML to that file.
		/// </summary>
		/// <param name="exportMode"> The type of file to export
		/// (WorkspaceFactoryController.MODE_TOOLBOX for the toolbox configuration,
		/// and WorkspaceFactoryController.MODE_PRELOAD for the pre-loaded workspace
		/// configuration)</param>
		public void exportXmlFile(string exportMode)
		{
			// Get file name.
			string fileName;
			if (exportMode == WorkspaceFactoryController.MODE_TOOLBOX) {
				fileName = Window.Prompt("File Name for toolbox XML:", "toolbox.xml");
			}
			else {
				fileName = Window.Prompt("File Name for pre-loaded workspace XML:",
									  "workspace.xml");
			}
			if (String.IsNullOrEmpty(fileName)) {  // If cancelled.
				return;
			}

			// Generate XML.
			string configXml;
			if (exportMode == WorkspaceFactoryController.MODE_TOOLBOX) {
				// Export the toolbox XML.
				configXml = Blockly.Xml.domToPrettyText
					(this.generator.generateToolboxXml());
				this.hasUnsavedToolboxChanges = false;
			}
			else if (exportMode == WorkspaceFactoryController.MODE_PRELOAD) {
				// Export the pre-loaded block XML.
				configXml = Blockly.Xml.domToPrettyText
					(this.generator.generateWorkspaceXml());
				this.hasUnsavedPreloadChanges = false;
			}
			else {
				// Unknown mode. Throw error.
				throw new Exception("Unknown export mode: " + exportMode);
			}

			// Download file.
			var data = new Blob(new string[] { configXml }, new Dictionary<string, object> { { "type", "text/xml" } });
			this.view.createAndDownloadFile(fileName, data);
		}

		/// <summary>
		/// Export the options object to be used for the Blockly inject call. Gets a
		/// file name from the user and downloads the options object to that file.
		/// </summary>
		public void exportInjectFile()
		{
			var fileName = Window.Prompt("File Name for starter Blockly workspace code:",
								  "workspace.js");
			if (String.IsNullOrEmpty(fileName)) {  // If cancelled.
				return;
			}
			// Generate new options to remove toolbox XML from options object (if
			// necessary).
			this.generateNewOptions();
			var printableOptions = this.generator.generateInjectString();

			var data = new Blob(new string[] { printableOptions }, new Dictionary<string, object> { { "type", "text/javascript" } });
			this.view.createAndDownloadFile(fileName, data);
		}

		/// <summary>
		/// Tied to "Print" button. Mainly used for debugging purposes. Prints
		/// the configuration XML to the console.
		/// </summary>
		public void printConfig()
		{
			// Capture any changes made by user before generating XML.
			this.saveStateFromWorkspace();
			// Print XML.
			Console.WriteLine(Blockly.Xml.domToPrettyText(this.generator.generateToolboxXml()));
		}

		/// <summary>
		/// Updates the preview workspace based on the toolbox workspace. If switching
		/// from no categories to categories or categories to no categories, reinjects
		/// Blockly with reinjectPreview, otherwise just updates without reinjecting.
		/// Called whenever a list element is created, removed, or modified and when
		/// Blockly move and delete events are fired. Do not call on create events
		/// or disabling will cause the user to "drop" their current blocks. Make sure
		/// that no changes have been made to the workspace since updating the model
		/// (if this might be the case, call saveStateFromWorkspace).
		/// </summary>
		public void updatePreview()
		{
			// Disable events to stop updatePreview from recursively calling itself
			// through event handlers.
			Blockly.Events.disable();

			// Only update the toolbox if not in read only mode.
			if (!this.model.options.ContainsKey("readOnly")) {
				// Get toolbox XML.
				var tree = Blockly.Options.parseToolboxTree(
					this.generator.generateToolboxXml());

				// No categories, creates a simple flyout.
				if (tree.GetElementsByTagName("category").Length == 0) {
					// No categories, creates a simple flyout.
					if (this.previewWorkspace.toolbox_ != null) {
						this.reinjectPreview(tree); // Switch to simple flyout, expensive.
					}
					else {
						this.previewWorkspace.updateToolbox(tree);
					}
				}
				else {
					// Uses categories, creates a toolbox.
					if (this.previewWorkspace.toolbox_ == null) {
						this.reinjectPreview(tree); // Create a toolbox, expensive.
					}
					else {
						// Close the toolbox before updating it so that the user has to reopen
						// the flyout and see their updated toolbox (open flyout doesn't update)
						this.previewWorkspace.toolbox_.clearSelection();
						this.previewWorkspace.updateToolbox(tree);
					}
				}
			}

			// Update pre-loaded blocks in the preview workspace.
			this.previewWorkspace.clear();
			Blockly.Xml.domToWorkspace(this.generator.generateWorkspaceXml(),
				this.previewWorkspace);

			// Reenable events.
			Blockly.Events.enable();
		}

		/// <summary>
		/// Saves the state from the workspace depending on the current mode. Should
		/// be called after making changes to the workspace.
		/// </summary>
		public void saveStateFromWorkspace()
		{
			if (this.selectedMode == WorkspaceFactoryController.MODE_TOOLBOX) {
				// If currently editing the toolbox.
				// Update flags if toolbox has been changed.
				if (this.model.getSelectedXml() !=
					Blockly.Xml.workspaceToDom(this.toolboxWorkspace)) {
					this.hasUnsavedToolboxChanges = true;
				}

				this.model.getSelected().saveFromWorkspace(this.toolboxWorkspace);

			}
			else if (this.selectedMode == WorkspaceFactoryController.MODE_PRELOAD) {
				// If currently editing the pre-loaded workspace.
				// Update flags if preloaded blocks have been changed.
				if (this.model.getPreloadXml() !=
					Blockly.Xml.workspaceToDom(this.toolboxWorkspace)) {
					this.hasUnsavedPreloadChanges = true;
				}

				this.model.savePreloadXml(
					Blockly.Xml.workspaceToDom(this.toolboxWorkspace));
			}
		}

		/// <summary>
		/// Used to completely reinject the preview workspace. This should be used only
		/// when switching from simple flyout to categories, or categories to simple
		/// flyout. More expensive than simply updating the flyout or toolbox.
		/// </summary>
		/// <param name="tree">Tree of XML elements</param>
		public void reinjectPreview(Element tree)
		{
			this.previewWorkspace.dispose();
			var injectOptions = this.readOptions_();
			injectOptions["toolbox"] = Blockly.Xml.domToPrettyText(tree);
			this.previewWorkspace = Blockly.Core.inject("preview_blocks", injectOptions);
			Blockly.Xml.domToWorkspace(this.generator.generateWorkspaceXml(),
				this.previewWorkspace);
		}

		/// <summary>
		/// Tied to "change name" button. Changes the name of the selected category.
		/// Continues prompting the user until they input a category name that is not
		/// currently in use, exits if user presses cancel.
		/// </summary>
		public void changeCategoryName()
		{
			var selected = this.model.getSelected();
			// Return if a category is not selected.
			if (selected.type != ListElement.TYPE_CATEGORY) {
				return;
			}
			// Get new name from user.
			//Window.Foo = selected;
			var newName = this.promptForNewCategoryName("What do you want to change this"
			  + " category\'s name to?", selected.name);
			if (String.IsNullOrEmpty(newName)) {  // If cancelled.
				return;
			}
			// Change category name.
			selected.changeName(newName);
			this.view.updateCategoryName(newName, this.model.getSelectedId());
			// Update preview.
			this.updatePreview();
		}

		/// <summary>
		/// Tied to arrow up and arrow down buttons. Swaps with the element above or
		/// below the currently selected element (offset categories away from the
		/// current element). Updates state to enable the correct element editing
		/// buttons.
		/// </summary>
		/// <param name="offset"> The index offset from the currently selected element
		/// to swap with. Positive if the element to be swapped with is below, negative
		/// if the element to be swapped with is above.</param>
		public void moveElement(int offset)
		{
			var curr = this.model.getSelected();
			if (curr == null) {  // Return if no selected element.
				return;
			}
			var currIndex = this.model.getIndexByElementId(curr.id);
			var swapIndex = this.model.getIndexByElementId(curr.id) + offset;
			var swap = this.model.getElementByIndex(swapIndex);
			if (swap == null) {  // Return if cannot swap in that direction.
				return;
			}
			// Move currently selected element to index of other element.
			// Indexes must be valid because confirmed that curr and swap exist.
			this.moveElementToIndex(curr, swapIndex, currIndex);
			// Update element editing buttons.
			this.view.updateState(swapIndex, this.model.getSelected());
			// Update preview.
			this.updatePreview();
		}

		/// <summary>
		/// Moves a element to a specified index and updates the model and view
		/// accordingly. Helper functions throw an error if indexes are out of bounds.
		/// </summary>
		/// <param name="element"> The element to move.</param>
		/// <param name="newIndex">The index to insert the element at.</param>
		/// <param  name="oldIndex">The index the element is currently at.</param>
		public void moveElementToIndex(ListElement element, int newIndex, int oldIndex)
		{
			this.model.moveElementToIndex(element, newIndex, oldIndex);
			this.view.moveTabToIndex(element.id, newIndex, oldIndex);
		}

		/// <summary>
		/// Changes the color of the selected category. Return if selected element is
		/// a separator.
		/// </summary>
		/// <param name="color"> The color to change the selected category. Must be
		/// a valid CSS string.</param>
		public void changeSelectedCategoryColor(string color)
		{
			// Return if category is not selected.
			if (this.model.getSelected().type != ListElement.TYPE_CATEGORY) {
				return;
			}
			// Change color of selected category.
			this.model.getSelected().changeColor(color);
			this.view.setBorderColor(this.model.getSelectedId(), color);
			this.updatePreview();
		}

		/// <summary>
		/// Tied to the "Standard Category" dropdown option, this function prompts
		/// the user for a name of a standard Blockly category (case insensitive) and
		/// loads it as a new category and switches to it. Leverages StandardCategories.
		/// </summary>
		public void loadCategory()
		{
			string name;
			// Prompt user for the name of the standard category to load.
			do {
				name = Window.Prompt("Enter the name of the category you would like to import "
					+ "(Logic, Loops, Math, Text, Lists, Colour, Variables, or Functions)", "");
				if (String.IsNullOrEmpty(name)) {
					return;  // Exit if cancelled.
				}
			} while (!this.isStandardCategoryName(name));

			// Load category.
			this.loadCategoryByName(name);
		}

		/// <summary>
		/// Loads a Standard Category by name and switches to it. Leverages
		/// StandardCategories. Returns if cannot load standard category.
		/// </summary>
		/// <param name="name"> Name of the standard category to load.</param>
		public void loadCategoryByName(string name)
		{
			// Check if the user can load that standard category.
			if (!this.isStandardCategoryName(name)) {
				return;
			}
			if (this.model.hasVariables() && name.ToLowerCase() == "variables") {
				Window.Alert("A Variables category already exists. You cannot create multiple" +
					" variables categories.");
				return;
			}
			if (this.model.hasProcedures() && name.ToLowerCase() == "functions") {
				Window.Alert("A Functions category already exists. You cannot create multiple" +
					" functions categories.");
				return;
			}
			// Check if the user can create a category with that name.
			var standardCategory = StandardCategories.categoryMap[name.ToLowerCase()];
			if (this.model.hasCategoryByName(standardCategory.name)) {
				Window.Alert("You already have a category with the name " + standardCategory.name
		+ ". Rename your category and try again.");
				return;
			}
			// Transfers current flyout blocks to a category if it's the first category
			// created.
			this.transferFlyoutBlocksToCategory();

			var isFirstCategory = !this.model.hasElements();
			// Copy the standard category in the model.
			var copy = standardCategory.copy();

			// Add it to the model.
			this.model.addElementToList(copy);

			// Update the copy in the view.
			var tab = this.view.addCategoryRow(copy.name, copy.id);
			this.addClickToSwitch(tab, copy.id);
			// Color the category tab in the view.
			if (!String.IsNullOrEmpty(copy.color)) {
				this.view.setBorderColor(copy.id, copy.color);
			}
			// Switch to loaded category.
			this.switchElement(copy.id);
			// Convert actual shadow blocks to user-generated shadow blocks.
			this.convertShadowBlocks();
			// Save state from workspace before updating preview.
			this.saveStateFromWorkspace();
			if (isFirstCategory) {
				// Allow the user to use the default options for injecting the workspace
				// when there are categories.
				this.view.setCategoryOptions(this.model.hasElements());
				this.generateNewOptions();
			}
			// Update preview.
			this.updatePreview();
		}

		/// <summary>
		/// Loads the standard Blockly toolbox into the editing space. Should only
		/// be called when the mode is set to toolbox.
		/// </summary>
		public void loadStandardToolbox()
		{
			this.loadCategoryByName("Logic");
			this.loadCategoryByName("Loops");
			this.loadCategoryByName("Math");
			this.loadCategoryByName("Text");
			this.loadCategoryByName("Lists");
			this.loadCategoryByName("Colour");
			this.addSeparator();
			this.loadCategoryByName("Variables");
			this.loadCategoryByName("Functions");
		}

		/// <summary>
		/// Given the name of a category, determines if it's the name of a standard
		/// category (case insensitive).
		/// </summary>
		/// <param name="name">The name of the category that should be checked if it's
		/// in StandardCategories categoryMap</param>
		/// <returns>True if name is a standard category name, false otherwise.</returns>
		public bool isStandardCategoryName(string name)
		{
			foreach (var category in StandardCategories.categoryMap.Keys) {
				if (name.ToLowerCase() == category) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Connected to the "add separator" dropdown option. If categories already
		/// exist, adds a separator to the model and view. Does not switch to select
		/// the separator, and updates the preview.
		/// </summary>
		public void addSeparator()
		{
			// If adding the first element in the toolbox, transfers the user's blocks
			// in a flyout to a category.
			this.transferFlyoutBlocksToCategory();
			// Create the separator in the model.
			var separator = new ListElement(ListElement.TYPE_SEPARATOR);
			this.model.addElementToList(separator);
			// Create the separator in the view.
			var tab = this.view.addSeparatorTab(separator.id);
			this.addClickToSwitch(tab, separator.id);
			// Switch to the separator and update the preview.
			this.switchElement(separator.id);
			this.updatePreview();
		}

		/// <summary>
		/// Connected to the import button. Given the file path inputted by the user
		/// from file input, if the import mode is for the toolbox, this function loads
		/// that toolbox XML to the workspace, creating category and separator tabs as
		/// necessary. If the import mode is for pre-loaded blocks in the workspace,
		/// this function loads that XML to the workspace to be edited further. This
		/// function switches mode to whatever the import mode is. Catches errors from
		/// file reading and prints an error message alerting the user.
		/// </summary>
		/// <param name="file"> The path for the file to be imported into the workspace.
		/// Should contain valid toolbox XML.</param>
		/// <param name="importMode"> The mode corresponding to the type of file the
		/// user is importing (WorkspaceFactoryController.MODE_TOOLBOX or
		/// WorkspaceFactoryController.MODE_PRELOAD).</param>
		public void importFile(Blob file, string importMode)
		{
			// Exit if cancelled.
			if (file == null) {
				return;
			}

			Blockly.Events.disable();
			var controller = this;
			var reader = new FileReader();

			// To be executed when the reader has read the file.
			reader.OnLoad = new Action<Event>((ev) => {
				// Try to parse XML from file and load it into toolbox editing area.
				// Print error message if fail.
				try {
					var tree = Blockly.Xml.textToDom(reader.Result);
					if (importMode == WorkspaceFactoryController.MODE_TOOLBOX) {
						// Switch mode.
						controller.setMode(WorkspaceFactoryController.MODE_TOOLBOX);

						// Confirm that the user wants to override their current toolbox.
						var hasToolboxElements = controller.model.hasElements() ||
							controller.toolboxWorkspace.getAllBlocks().Length > 0;
						if (hasToolboxElements &&
							!Window.Confirm("Are you sure you want to import? You will lose your " +
							"current toolbox.")) {
							return;
						}
						// Import toolbox XML.
						controller.importToolboxFromTree_(tree);

					}
					else if (importMode == WorkspaceFactoryController.MODE_PRELOAD) {
						// Switch mode.
						controller.setMode(WorkspaceFactoryController.MODE_PRELOAD);

						// Confirm that the user wants to override their current blocks.
						if (controller.toolboxWorkspace.getAllBlocks().Length > 0 &&
							!Window.Confirm("Are you sure you want to import? You will lose your " +
							"current workspace blocks.")) {
							return;
						}

						// Import pre-loaded workspace XML.
						controller.importPreloadFromTree_(tree);
					}
					else {
						// Throw error if invalid mode.
						throw new Exception("Unknown import mode: " + importMode);
					}
				}
				catch (Exception e) {
					Window.Alert("Cannot load XML from file.");
					Console.WriteLine(e);
				}
				finally {
					Blockly.Events.enable();
				}
			});

			// Read the file asynchronously.
			reader.ReadAsText(file);
		}

		/// <summary>
		/// Given a XML DOM tree, loads it into the toolbox editing area so that the
		/// user can continue editing their work. Assumes that tree is in valid toolbox
		/// XML format. Assumes that the mode is MODE_TOOLBOX.
		/// </summary>
		/// <param name="tree"> XML tree to be loaded to toolbox editing area.</param>
		private void importToolboxFromTree_(Element tree)
		{
			// Clear current editing area.
			this.model.clearToolboxList();
			this.view.clearToolboxTabs();

			if (tree.GetElementsByTagName("category").Length == 0) {
				// No categories present.
				// Load all the blocks into a single category evenly spaced.
				Blockly.Xml.domToWorkspace(tree, this.toolboxWorkspace);
				this.toolboxWorkspace.cleanUp();

				// Convert actual shadow blocks to user-generated shadow blocks.
				this.convertShadowBlocks();

				// Add message to denote empty category.
				this.view.addEmptyCategoryMessage();

			}
			else {
				// Categories/separators present.
				for (var i = 0; i < tree.Children.Length; i++) {
					var item = tree.Children[i];

					if (item.TagName == "category") {
						// If the element is a category, create a new category and switch to it.
						this.createCategory(item.GetAttribute("name"));
						var category = this.model.getElementByIndex(i);
						this.switchElement(category.id);

						// Load all blocks in that category to the workspace to be evenly
						// spaced and saved to that category.
						for (var j = 0; j < item.Children.Length; j++) {
							var blockXml = item.Children[j];
							Blockly.Xml.domToBlock(blockXml, this.toolboxWorkspace);
						}

						// Evenly space the blocks.
						this.toolboxWorkspace.cleanUp();

						// Convert actual shadow blocks to user-generated shadow blocks.
						this.convertShadowBlocks();

						// Set category color.
						if (item.GetAttribute("colour") != null) {
							category.changeColor(item.GetAttribute("colour"));
							this.view.setBorderColor(category.id, category.color);
						}
						// Set any custom tags.
						if (item.GetAttribute("custom") != null) {
							this.model.addCustomTag(category, item.GetAttribute("custom"));
						}
					}
					else {
						// If the element is a separator, add the separator and switch to it.
						this.addSeparator();
						this.switchElement(this.model.getElementByIndex(i).id);
					}
				}
			}
			this.view.updateState(this.model.getIndexByElementId
				(this.model.getSelectedId()), this.model.getSelected());

			this.saveStateFromWorkspace();

			// Set default configuration options for a single flyout or multiple
			// categories.
			this.view.setCategoryOptions(this.model.hasElements());
			this.generateNewOptions();

			this.updatePreview();
		}

		/// <summary>
		/// Given a XML DOM tree, loads it into the pre-loaded workspace editing area.
		/// Assumes that tree is in valid XML format and that the selected mode is
		/// MODE_PRELOAD.
		/// </summary>
		/// <param name="tree">XML tree to be loaded to pre-loaded block editing
		/// area.</param>
		public void importPreloadFromTree_(Element tree)
		{
			this.clearAndLoadXml_(tree);
			this.model.savePreloadXml(tree);
			this.saveStateFromWorkspace();
			this.updatePreview();
		}

		/// <summary>
		/// Clears the editing area completely, deleting all categories and all
		/// blocks in the model and view and all pre-loaded blocks. Tied to the
		/// "Clear" button.
		/// </summary>
		public void clearAll()
		{
			if (!Window.Confirm("Are you sure you want to clear all of your work in Workspace" +
				" Factory?")) {
				return;
			}
			var hasCategories = this.model.hasElements();
			this.model.clearToolboxList();
			this.view.clearToolboxTabs();
			this.model.savePreloadXml(Blockly.Xml.textToDom("<xml></xml>"));
			this.view.addEmptyCategoryMessage();
			this.view.updateState(-1, null);
			this.toolboxWorkspace.clear();
			this.toolboxWorkspace.clearUndo();
			this.saveStateFromWorkspace();
			this.hasUnsavedToolboxChanges = false;
			this.hasUnsavedPreloadChanges = false;
			this.view.setCategoryOptions(this.model.hasElements());
			this.generateNewOptions();
			this.updatePreview();
		}

		/*
		 * Makes the currently selected block a user-generated shadow block. These
		 * blocks are not made into real shadow blocks, but recorded in the model
		 * and visually marked as shadow blocks, allowing the user to move and edit
		 * them (which would be impossible with actual shadow blocks). Updates the
		 * preview when done.
		 */
		public void addShadow()
		{
			// No block selected to make a shadow block.
			if (Blockly.Core.selected == null) {
				return;
			}
			// Clear any previous warnings on the block (would only have warnings on
			// a non-shadow block if it was nested inside another shadow block).
			Blockly.Core.selected.setWarningText(null);
			// Set selected block and all children as shadow blocks.
			this.addShadowForBlockAndChildren_(Blockly.Core.selected);

			// Save and update the preview.
			this.saveStateFromWorkspace();
			this.updatePreview();
		}

		/// <summary>
		/// Sets a block and all of its children to be user-generated shadow blocks,
		/// both in the model and view.
		/// </summary>
		/// <param name="block"> The block to be converted to a user-generated
		/// shadow block.</param>
		private void addShadowForBlockAndChildren_(Blockly.BlockSvg block)
		{
			// Convert to shadow block.
			this.view.markShadowBlock(block);
			this.model.addShadowBlock(block.id);

			if (FactoryUtils.hasVariableField(block)) {
				block.setWarningText("Cannot make variable blocks shadow blocks.");
			}

			// Convert all children to shadow blocks recursively.
			var children = block.getChildren();
			for (var i = 0; i < children.Length; i++) {
				this.addShadowForBlockAndChildren_((BlockSvg)children[i]);
			}
		}

		/// <summary>
		/// If the currently selected block is a user-generated shadow block, this
		/// function makes it a normal block again, removing it from the list of
		/// shadow blocks and loading the workspace again. Updates the preview again.
		/// </summary>
		public void removeShadow()
		{
			// No block selected to modify.
			if (Blockly.Core.selected == null) {
				return;
			}
			this.model.removeShadowBlock(Blockly.Core.selected.id);
			this.view.unmarkShadowBlock(Blockly.Core.selected);

			// If turning invalid shadow block back to normal block, remove warning.
			Blockly.Core.selected.setWarningText(null);

			this.saveStateFromWorkspace();
			this.updatePreview();
		}

		/// <summary>
		/// Given a unique block ID, uses the model to determine if a block is a
		/// user-generated shadow block.
		/// </summary>
		/// <param name="blockId">The unique ID of the block to examine.</param>
		/// <returns>True if the block is a user-generated shadow block, false
		///    otherwise.</returns>
		public bool isUserGenShadowBlock(string blockId)
		{
			return this.model.isShadowBlock(blockId);
		}

		/// <summary>
		/// Call when importing XML containing real shadow blocks. This function turns
		/// all real shadow blocks loaded in the workspace into user-generated shadow
		/// blocks, meaning they are marked as shadow blocks by the model and appear as
		/// shadow blocks in the view but are still editable and movable.
		/// </summary>
		public void convertShadowBlocks()
		{
			var blocks = this.toolboxWorkspace.getAllBlocks();
			for (var i = 0; i < blocks.Length; i++) {
				var block = (BlockSvg)blocks[i];
				if (block.isShadow()) {
					block.setShadow(false);
					// Delete the shadow DOM attached to the block so that the shadow block
					// does not respawn. Dependent on implementation details.
					var parentConnection = block.outputConnection != null ?
						block.outputConnection.targetConnection :
						block.previousConnection.targetConnection;
					if (parentConnection != null) {
						parentConnection.setShadowDom(null);
					}
					this.model.addShadowBlock(block.id);
					this.view.markShadowBlock(block);
				}
			}
		}

		/// <summary>
		/// Sets the currently selected mode that determines what the toolbox workspace
		/// is being used to edit. Updates the view and then saves and loads XML
		/// to and from the toolbox and updates the help text.
		/// </summary>
		/// <param name="tab">The type of tab being switched to
		/// (WorkspaceFactoryController.MODE_TOOLBOX or
		/// WorkspaceFactoryController.MODE_PRELOAD).</param>
		public void setMode(string mode)
		{
			// No work to change mode that's currently set.
			if (this.selectedMode == mode) {
				return;
			}

			// No work to change mode that's currently set.
			if (this.selectedMode == mode) {
				return;
			}

			// Set tab selection and display appropriate tab.
			this.view.setModeSelection(mode);

			// Update selected tab.
			this.selectedMode = mode;

			// Update help text above workspace.
			this.view.updateHelpText(mode);

			if (mode == WorkspaceFactoryController.MODE_TOOLBOX) {
				// Open the toolbox editing space.
				this.model.savePreloadXml
					(Blockly.Xml.workspaceToDom(this.toolboxWorkspace));
				this.clearAndLoadXml_(this.model.getSelectedXml());
				this.view.disableWorkspace(this.view.shouldDisableWorkspace
					(this.model.getSelected()));
			}
			else {
				// Open the pre-loaded workspace editing space.
				if (this.model.getSelected() != null) {
					this.model.getSelected().saveFromWorkspace(this.toolboxWorkspace);
				}
				this.clearAndLoadXml_(this.model.getPreloadXml());
				this.view.disableWorkspace(false);
			}
		}

		/// <summary>
		/// Clears the toolbox workspace and loads XML to it, marking shadow blocks
		/// as necessary.
		/// </summary>
		/// <param name="xml"> The XML to be loaded to the workspace.</param>
		private void clearAndLoadXml_(Element xml)
		{
			this.toolboxWorkspace.clear();
			this.toolboxWorkspace.clearUndo();
			Blockly.Xml.domToWorkspace(xml, this.toolboxWorkspace);
			this.view.markShadowBlocks(this.model.getShadowBlocksInWorkspace(this.toolboxWorkspace.getAllBlocks()));
			this.warnForUndefinedBlocks_();
		}

		/// <summary>
		/// Sets the standard default options for the options object and updates
		/// the preview workspace. The default values depends on if categories are
		/// present.
		/// </summary>
		public void setStandardOptionsAndUpdate()
		{
			this.view.setBaseOptions();
			this.view.setCategoryOptions(this.model.hasElements());
			this.generateNewOptions();
		}

		/// <summary>
		/// Generates a new options object for injecting a Blockly workspace based
		/// on user input. Should be called every time a change has been made to
		/// an input field. Updates the model and reinjects the preview workspace.
		/// </summary>
		public void generateNewOptions()
		{
			this.model.setOptions(this.readOptions_());

			this.reinjectPreview(Blockly.Options.parseToolboxTree
				(this.generator.generateToolboxXml()));
		}

		/// <summary>
		/// Generates a new options object for injecting a Blockly workspace based on
		/// user input.
		/// </summary>
		/// <returns>Blockly injection options object.</returns>
		private Dictionary<string, object> readOptions_()
		{
			var optionsObj = new Dictionary<string, object>();

			// Add all standard options to the options object.
			// Use parse int to get numbers from value inputs.
			optionsObj["collapse"] =
				((HTMLInputElement)Document.GetElementById("option_collapse_checkbox")).Checked;
			optionsObj["comments"] =
				((HTMLInputElement)Document.GetElementById("option_comments_checkbox")).Checked;
			optionsObj["css"] = 
				((HTMLInputElement)Document.GetElementById("option_css_checkbox")).Checked;
			optionsObj["disable"] =
				((HTMLInputElement)Document.GetElementById("option_disable_checkbox")).Checked;
			if (((HTMLInputElement)Document.GetElementById("option_infiniteBlocks_checkbox")).Checked) {
				optionsObj["maxBlocks"] = Int32.MaxValue;
			}
			else {
				var maxBlocksValue =
					((HTMLInputElement)Document.GetElementById("option_maxBlocks_number")).Value;
				//optionsObj["maxBlocks"] = maxBlocksValue is string ?
				//	Convert.ToInt32(maxBlocksValue) : maxBlocksValue;
				optionsObj["maxBlocks"] = Convert.ToInt32(maxBlocksValue);
			}
			optionsObj["media"] =
				((HTMLInputElement)Document.GetElementById("option_media_text")).Value;
			optionsObj["readOnly"] =
				((HTMLInputElement)Document.GetElementById("option_readOnly_checkbox")).Checked;
			optionsObj["rtl"] =
				((HTMLInputElement)Document.GetElementById("option_rtl_checkbox")).Checked;
			optionsObj["scrollbars"] =
				((HTMLInputElement)Document.GetElementById("option_scrollbars_checkbox")).Checked;
			optionsObj["sounds"] =
				((HTMLInputElement)Document.GetElementById("option_sounds_checkbox")).Checked;
			if (!optionsObj.ContainsKey("readOnly")) {
				optionsObj["trashcan"] =
					((HTMLInputElement)Document.GetElementById("option_trashcan_checkbox")).Checked;
			}

			// If using a grid, add all grid options.
			if (((HTMLInputElement)Document.GetElementById("option_grid_checkbox")).Checked) {
				var grid = new Dictionary<string, object>();
				var spacingValue =
					((HTMLInputElement)Document.GetElementById("gridOption_spacing_number")).Value;
				//grid["spacing"] = spacingValue is string ?
				//	Convert.ToInt32(spacingValue) : spacingValue;
				grid["spacing"] = Convert.ToInt32(spacingValue);
				var lengthValue = 
					((HTMLInputElement)Document.GetElementById("gridOption_length_number")).Value;
				//grid["length"] = lengthValue is string ?
				//	Convert.ToInt32(lengthValue) : lengthValue;
				grid["length"] = Convert.ToInt32(lengthValue);
				grid["colour"] = ((HTMLInputElement)Document.GetElementById("gridOption_colour_text")).Value;
				grid["snap"] = ((HTMLInputElement)Document.GetElementById("gridOption_snap_checkbox")).Checked;
				optionsObj["grid"] = grid;
			}

			// If using zoom, add all zoom options.
			if (((HTMLInputElement)Document.GetElementById("option_zoom_checkbox")).Checked) {
				var zoom = new Dictionary<string, object>();
				zoom["controls"] =
					((HTMLInputElement)Document.GetElementById("zoomOption_controls_checkbox")).Checked;
				zoom["wheel"] =
					((HTMLInputElement)Document.GetElementById("zoomOption_wheel_checkbox")).Checked;
				var startScaleValue =
					((HTMLInputElement)Document.GetElementById("zoomOption_startScale_number")).Value;
				//zoom["startScale"] = startScaleValue is string ?
				//	Convert.ToDouble(startScaleValue) : startScaleValue;
				zoom["startScale"] = Convert.ToDouble(startScaleValue);
				var maxScaleValue =
					((HTMLInputElement)Document.GetElementById("zoomOption_maxScale_number")).Value;
				//zoom["maxcale"] = maxScaleValue is string ?
				//	Convert.ToDouble(maxScaleValue) : maxScaleValue;
				zoom["maxcale"] = Convert.ToDouble(maxScaleValue);
				var minScaleValue =
					((HTMLInputElement)Document.GetElementById("zoomOption_minScale_number")).Value;
				//zoom["minScale"] = minScaleValue is string ?
				//	Convert.ToDouble(minScaleValue) : minScaleValue;
				zoom["minScale"] = Convert.ToDouble(minScaleValue);
				var scaleSpeedValue =
					((HTMLInputElement)Document.GetElementById("zoomOption_scaleSpeed_number")).Value;
				//zoom["startScale"] = startScaleValue is string ?
				//	Convert.ToDouble(scaleSpeedValue) : scaleSpeedValue;
				zoom["startScale"] = Convert.ToDouble(scaleSpeedValue);
				optionsObj["zoom"] = zoom;
			}

			return optionsObj;
		}

		/// <summary>
		/// Imports blocks from a file, generating a category in the toolbox workspace
		/// to allow the user to use imported blocks in the toolbox and in pre-loaded
		/// blocks.
		/// </summary>
		/// <param name="file"> File object for the blocks to import.</param>
		/// <param name="format">The format of the file to import, either "JSON" or
		/// "JavaScript".</param>
		public void importBlocks(File file, string format)
		{
			// Generate category name from file name.
			var categoryName = file.Name;

			var controller = this;
			var reader = new FileReader();

			// To be executed when the reader has read the file.
			reader.OnLoad = new Action<Event>((e) => {
				try {
					// Define blocks using block types from file.
					var blockTypes = FactoryUtils.defineAndGetBlockTypes(reader.Result,
						format);

					// If an imported block type is already defined, check if the user wants
					// to override the current block definition.
					if (controller.model.hasDefinedBlockTypes(blockTypes) &&
						!Window.Confirm("An imported block uses the same name as a block "
						+ "already in your toolbox. Are you sure you want to override the "
						+ "currently defined block?")) {
						return;
					}

					var blocks = controller.generator.getDefinedBlocks(blockTypes);
					// Generate category XML and append to toolbox.
					var categoryXml = FactoryUtils.generateCategoryXml(blocks, categoryName);
					// Get random color for category between 0 and 360. Gives each imported
					// category a different color.
					var randomColor = System.Math.Floor(Script.Random() * 360);
					categoryXml.SetAttribute("colour", randomColor);
					controller.toolbox.AppendChild(categoryXml);
					controller.toolboxWorkspace.updateToolbox(controller.toolbox);
					// Update imported block types.
					controller.model.addImportedBlockTypes(blockTypes);
					// Reload current category to possibly reflect any newly defined blocks.
					controller.clearAndLoadXml_
						(Blockly.Xml.workspaceToDom(controller.toolboxWorkspace));
				}
				catch (Exception) {
					Window.Alert("Cannot read blocks from file.");
					Console.WriteLine(e);
				}
			});

			// Read the file asynchronously.
			reader.ReadAsText(file);
		}

		/// <summary>
		/// Updates the block library category in the toolbox workspace toolbox.
		/// <param name="categoryXml"> XML for the block library category.</param>
		/// <param name="libBlockTypes"> Array of block types from the block</param>
		///   library.
		/// </summary>
		public void setBlockLibCategory(Element categoryXml, JsArray<string> libBlockTypes)
		{
			var blockLibCategory = Document.GetElementById("blockLibCategory");

			// Set category ID so that it can be easily replaced, and set a standard,
			// arbitrary block library color.
			categoryXml.SetAttribute("id", "blockLibCategory");
			categoryXml.SetAttribute("colour", 260);

			// Update the toolbox and toolboxWorkspace.
			this.toolbox.ReplaceChild(categoryXml, blockLibCategory);
			this.toolboxWorkspace.toolbox_.clearSelection();
			this.toolboxWorkspace.updateToolbox(this.toolbox);

			// Update the block library types.
			this.model.updateLibBlockTypes(libBlockTypes);

			// Reload XML on page to account for blocks now defined or undefined in block
			// library.
			this.clearAndLoadXml_(Blockly.Xml.workspaceToDom(this.toolboxWorkspace));
		}

		/// <summary>
		/// Return the block types used in the custom toolbox and pre-loaded workspace.
		/// </summary>
		/// <returns>Block types used in the custom toolbox and
		///    pre-loaded workspace.</returns>
		public JsArray<string> getAllUsedBlockTypes()
		{
			return this.model.getAllUsedBlockTypes();
		}

		/// <summary>
		/// Determines if a block loaded in the workspace has a definition (if it
		/// is a standard block, is defined in the block library, or has a definition
		/// imported).
		/// </summary>
		/// <param name="block"> The block to examine.</param>
		public bool isDefinedBlock(Blockly.Block block)
		{
			return this.model.isDefinedBlockType(block.type);
		}

		/// <summary>
		/// Sets a warning on blocks loaded to the workspace that are not defined.
		/// </summary>
		private void warnForUndefinedBlocks_()
		{
			var blocks = this.toolboxWorkspace.getAllBlocks();
			for (var i = 0; i < blocks.Length; i++) {
				var block = blocks[i];
				if (!this.isDefinedBlock(block)) {
					block.setWarningText(block.type + " is not defined (it is not a standard "
						+ "block, \nin your block library, or an imported block)");
				}
			}
		}

		/// <summary>
		/// Determines if a standard variable category is in the custom toolbox.
		/// </summary>
		/// <returns>True if a variables category is in use, false otherwise.</returns>
		public bool hasVariablesCategory()
		{
			return this.model.hasVariables();
		}

		/// <summary>
		/// Determines if a standard procedures category is in the custom toolbox.
		/// </summary>
		/// <returns>True if a procedures category is in use, false otherwise.</returns>
		public bool hasProceduresCategory()
		{
			return this.model.hasProcedures();
		}

		/// <summary>
		/// Determines if there are any unsaved changes in workspace factory.
		/// </summary>
		/// <returns>True if there are unsaved changes, false otherwise.</returns>
		public bool hasUnsavedChanges()
		{
			return this.hasUnsavedToolboxChanges || this.hasUnsavedPreloadChanges;
		}
	}
}
