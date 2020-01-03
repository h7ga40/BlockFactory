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
 * @fileoverview The AppController Class brings together the Block
 * Factory, Block Library, and Block Exporter functionality into a single web
 * app.
 *
 * @author quachtina96 (Tina Quach)
 */
using System;
using System.Collections.Generic;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class AppController
	{
		public string blockLibraryName;
		public BlockLibraryController blockLibraryController;
		public WorkspaceFactoryController workspaceFactoryController;
		public BlockExporterController exporter;
		public Dictionary<string, HTMLElement> tabMap;
		public string lastSelectedTab;
		public string selectedTab;

		/// <summary>
		/// Controller for the Blockly Factory
		/// </summary>
		public AppController()
		{
			// Initialize Block Library
			this.blockLibraryName = "blockLibrary";
			this.blockLibraryController =
				new BlockLibraryController(this.blockLibraryName);
			this.blockLibraryController.populateBlockLibrary();

			// Construct Workspace Factory Controller.
			this.workspaceFactoryController = new WorkspaceFactoryController
				("workspacefactory_toolbox", "toolbox_blocks", "preview_blocks");

			// Initialize Block Exporter
			this.exporter =
				new BlockExporterController(this.blockLibraryController.storage);

			// Map of tab type to the div element for the tab.
			this.tabMap = new Dictionary<string, HTMLElement>();
			this.tabMap[AppController.BLOCK_FACTORY] =
				(HTMLElement)Document.GetElementById("blockFactory_tab");
			this.tabMap[AppController.WORKSPACE_FACTORY] =
				(HTMLElement)Document.GetElementById("workspaceFactory_tab");
			this.tabMap[AppController.EXPORTER] =
				(HTMLElement)Document.GetElementById("blocklibraryExporter_tab");

			// Last selected tab.
			this.lastSelectedTab = null;
			// Selected tab.
			this.selectedTab = AppController.BLOCK_FACTORY;
		}

		// Constant values representing the three tabs in the controller.
		public static string BLOCK_FACTORY = "BLOCK_FACTORY";
		public static string WORKSPACE_FACTORY = "WORKSPACE_FACTORY";
		public static string EXPORTER = "EXPORTER";

		/// <summary>
		/// Tied to the "Import Block Library" button. Imports block library from file to
		/// Block Factory. Expects user to upload a single file of JSON mapping each
		/// block type to its XML text representation.
		/// </summary>
		public void importBlockLibraryFromFile()
		{
			var self = this;
			var files = (HTMLInputElement)Document.GetElementById("files");
			// If the file list is empty, the user likely canceled in the dialog.
			if (files.files.Length > 0) {
				// The input tag doesn't have the "multiple" attribute
				// so the user can only choose 1 file.
				var file = files.files[0];
				var fileReader = new FileReader();

				// Create a map of block type to XML text from the file when it has been
				// read.
				fileReader.AddEventListener("load", new Action<Event>((@event) => {
					var fileContents = ((FileReader)@event.Target).Result;
					// Create empty object to hold the read block library information.
					var blockXmlTextMap = new Dictionary<string, object>();
					try {
						// Parse the file to get map of block type to XML text.
						blockXmlTextMap = self.formatBlockLibraryForImport_(fileContents);
					}
					catch (Exception) {
						var message = "Could not load your block library file.\n";
						Window.Alert(message + "\nFile Name: " + file.Name);
						return;
					}

					// Create a new block library storage object with inputted block library.
					var blockLibStorage = new BlockLibraryStorage(
						self.blockLibraryName, blockXmlTextMap);

					// Update block library controller with the new block library
					// storage.
					self.blockLibraryController.setBlockLibraryStorage(blockLibStorage);
					// Update the block library dropdown.
					self.blockLibraryController.populateBlockLibrary();
					// Update the exporter's block library storage.
					self.exporter.setBlockLibraryStorage(blockLibStorage);
				}), false);
				// Read the file.
				fileReader.ReadAsText(file);
			}
		}

		/// <summary>
		/// Tied to the "Export Block Library" button. Exports block library to file that
		/// contains JSON mapping each block type to its XML text representation.
		/// </summary>
		public void exportBlockLibraryToFile()
		{
			// Get map of block type to XML.
			var blockLib = this.blockLibraryController.getBlockLibrary();
			// Concatenate the XMLs, each separated by a blank line.
			var blockLibText = this.formatBlockLibraryForExport_(blockLib);
			// Get file name.
			var filename = Window.Prompt("Enter the file name under which to save your block " +
				"library.", "library.xml");
			// Download file if all necessary parameters are provided.
			if (!String.IsNullOrEmpty(filename)) {
				FactoryUtils.createAndDownloadFile(blockLibText, filename, "xml");
			}
			else {
				Window.Alert("Could not export Block Library without file name under which to " +
				  "save library.");
			}
		}

		/// <summary>
		/// Converts an object mapping block type to XML to text file for output.
		/// </summary>
		/// <param name="blockXmlMap"> Object mapping block type to XML.</param>
		/// <returns>XML text containing the block XMLs.</returns>
		private string formatBlockLibraryForExport_(Dictionary<string, object> blockXmlMap)
		{
			// Create DOM for XML.
			var xmlDom = goog.dom.createDom("xml", new Dictionary<string, string> {
				{ "xmlns","http://www.w3.org/1999/xhtml" }
			});

			// Append each block node to XML DOM.
			foreach (var blockType in blockXmlMap.Keys) {
				var blockXmlDom = Blockly.Xml.textToDom((string)blockXmlMap[blockType]);
				var blockNode = blockXmlDom.FirstElementChild;
				xmlDom.AppendChild(blockNode);
			}

			// Return the XML text.
			return Blockly.Xml.domToText(xmlDom);
		}

		/// <summary>
		/// Converts imported block library to an object mapping block type to block XML.
		/// </summary>
		/// <param name="xmlText">String representation of an XML with each block as
		/// a child node.</param>
		/// <returns>Object mapping block type to XML text.</returns>
		private Dictionary<string, object> formatBlockLibraryForImport_(string xmlText)
		{
			var xmlDom = Blockly.Xml.textToDom(xmlText);

			// Get array of XMLs. Use an asterisk (*) instead of a tag name for the XPath
			// selector, to match all elements at that level and get all factory_base
			// blocks.
			var blockNodes = goog.dom.xml.selectNodes(xmlDom, "*");

			// Create empty map. The line below creates a  truly empy object. It doesn't
			// have built-in attributes/functions such as length or toString.
			var blockXmlTextMap = new Dictionary<string, object>();

			// Populate map.
			Element blockNode;
			for (var i = 0; i < blockNodes.Length; i++) {
				blockNode = (Element)blockNodes[i];
				// Add outer XML tag to the block for proper injection in to the
				// main workspace.
				// Create DOM for XML.
				xmlDom = goog.dom.createDom("xml", new Dictionary<string, string> {
					{ "xmlns","http://www.w3.org/1999/xhtml"}
				});
				xmlDom.AppendChild(blockNode);

				xmlText = Blockly.Xml.domToText(xmlDom);
				// All block types should be lowercase.
				var blockType = this.getBlockTypeFromXml_(xmlText).ToLowerCase();

				blockXmlTextMap[blockType] = xmlText;
			}

			return blockXmlTextMap;
		}

		/// <summary>
		/// Extracts out block type from XML text, the kind that is saved in block
		/// library storage.
		/// </summary>
		/// <param name="xmlText"> A block's XML text.</param>
		/// <returns>The block type that corresponds to the provided XML text.</returns>
		private string getBlockTypeFromXml_(string xmlText)
		{
			var xmlDom = Blockly.Xml.textToDom(xmlText);
			// Find factory base block.
			var factoryBaseBlockXml = (Element)xmlDom.GetElementsByTagName("block")[0];
			// Get field elements from factory base.
			var fields = factoryBaseBlockXml.GetElementsByTagName("field");
			for (var i = 0; i < fields.Length; i++) {
				// The field whose name is "NAME" holds the block type as its value.
				if (((Element)fields[i]).GetAttribute("name") == "NAME") {
					return fields[i].ChildNodes[0].NodeValue.ToString();
				}
			}
			return null;
		}

		/// <summary>
		/// Add click handlers to each tab to allow switching between the Block Factory,
		/// Workspace Factory, and Block Exporter tab.
		/// </summary>
		/// <param name="tabMap"> Map of tab name to div element that is the tab.</param>
		public void addTabHandlers(Dictionary<string, HTMLElement> tabMap)
		{
			var self = this;
			foreach (var tabName in tabMap.Keys) {
				var tab = tabMap[tabName];
				// Use an additional closure to correctly assign the tab callback.
				tab.AddEventListener("click", self.makeTabClickHandler_(tabName));
			}
		}

		/// <summary>
		/// Set the selected tab.
		/// </summary>
		/// <param name="tabName"> AppController.BLOCK_FACTORY,
		/// AppController.WORKSPACE_FACTORY, or AppController.EXPORTER</param>
		private void setSelected_(string tabName)
		{
			this.lastSelectedTab = this.selectedTab;
			this.selectedTab = tabName;
		}

		/// <summary>
		/// Creates the tab click handler specific to the tab specified.
		/// </summary>
		/// <param name="tabName">AppController.BLOCK_FACTORY,
		/// AppController.WORKSPACE_FACTORY, or AppController.EXPORTER</param>
		/// <returns>The tab click handler.</returns>
		private Delegate makeTabClickHandler_(string tabName)
		{
			var self = this;
			return new Action(() => {
				self.setSelected_(tabName);
				self.onTab();
			});
		}

		/// <summary>
		/// Called on each tab click. Hides and shows specific content based on which tab
		/// (Block Factory, Workspace Factory, or Exporter) is selected.
		/// </summary>
		public void onTab()
		{
			// Get tab div elements.
			var blockFactoryTab = this.tabMap[AppController.BLOCK_FACTORY];
			var exporterTab = this.tabMap[AppController.EXPORTER];
			var workspaceFactoryTab = this.tabMap[AppController.WORKSPACE_FACTORY];

			// Warn user if they have unsaved changes when leaving Block Factory.
			if (this.lastSelectedTab == AppController.BLOCK_FACTORY &&
				this.selectedTab != AppController.BLOCK_FACTORY) {

				var hasUnsavedChanges =
					!FactoryUtils.savedBlockChanges(this.blockLibraryController);
				if (hasUnsavedChanges &&
					!Window.Confirm("You have unsaved changes in Block Factory.")) {
					// If the user doesn't want to switch tabs with unsaved changes,
					// stay on Block Factory Tab.
					this.setSelected_(AppController.BLOCK_FACTORY);
					this.lastSelectedTab = AppController.BLOCK_FACTORY;
					return;
				}
			}

			// Only enable key events in workspace factory if workspace factory tab is
			// selected.
			this.workspaceFactoryController.keyEventsEnabled =
				this.selectedTab == AppController.WORKSPACE_FACTORY;

			// Turn selected tab on and other tabs off.
			this.styleTabs_();

			if (this.selectedTab == AppController.EXPORTER) {
				// Hide other tabs.
				FactoryUtils.hide("workspaceFactoryContent");
				FactoryUtils.hide("blockFactoryContent");
				// Show exporter tab.
				FactoryUtils.show("blockLibraryExporter");

				// Need accurate state in order to know which blocks are used in workspace
				// factory.
				this.workspaceFactoryController.saveStateFromWorkspace();

				// Update exporter's list of the types of blocks used in workspace factory.
				var usedBlockTypes = this.workspaceFactoryController.getAllUsedBlockTypes();
				this.exporter.setUsedBlockTypes(usedBlockTypes);

				// Update exporter's block selector to reflect current block library.
				this.exporter.updateSelector();

				// Update the exporter's preview to reflect any changes made to the blocks.
				this.exporter.updatePreview();

			}
			else if (this.selectedTab == AppController.BLOCK_FACTORY) {
				// Hide other tabs.
				FactoryUtils.hide("blockLibraryExporter");
				FactoryUtils.hide("workspaceFactoryContent");
				// Show Block Factory.
				FactoryUtils.show("blockFactoryContent");

			}
			else if (this.selectedTab == AppController.WORKSPACE_FACTORY) {
				// Hide other tabs.
				FactoryUtils.hide("blockLibraryExporter");
				FactoryUtils.hide("blockFactoryContent");
				// Show workspace factory container.
				FactoryUtils.show("workspaceFactoryContent");
				// Update block library category.
				var categoryXml = this.exporter.getBlockLibraryCategory();
				var blockTypes = this.blockLibraryController.getStoredBlockTypes();
				this.workspaceFactoryController.setBlockLibCategory(categoryXml,
					blockTypes);
			}

			// Resize to render workspaces" toolboxes correctly for all tabs.
			Window.DispatchEvent(new Event("resize"));
		}

		/// <summary>
		/// Called on each tab click. Styles the tabs to reflect which tab is selected.
		/// </summary>
		private void styleTabs_()
		{
			foreach (var tabName in this.tabMap.Keys) {
				if (this.selectedTab == tabName) {
					goog.dom.classlist.addRemove(this.tabMap[tabName], "taboff", "tabon");
				}
				else {
					goog.dom.classlist.addRemove(this.tabMap[tabName], "tabon", "taboff");
				}
			}
		}

		/// <summary>
		/// Assign button click handlers for the exporter.
		/// </summary>
		public void assignExporterClickHandlers()
		{
			var self = this;
			Document.GetElementById("button_setBlocks").AddEventListener("click",
				new Action(() => {
					self.openModal("dropdownDiv_setBlocks");
				}));

			Document.GetElementById("dropdown_addAllUsed").AddEventListener("click",
				new Action(() => {
					self.exporter.selectUsedBlocks();
					self.exporter.updatePreview();
					self.closeModal();
				}));

			Document.GetElementById("dropdown_addAllFromLib").AddEventListener("click",
				new Action(() => {
					self.exporter.selectAllBlocks();
					self.exporter.updatePreview();
					self.closeModal();
				}));

			Document.GetElementById("clearSelectedButton").AddEventListener("click",
				new Action(() => {
					self.exporter.clearSelectedBlocks();
					self.exporter.updatePreview();
				}));

			// Export blocks when the user submits the export settings.
			Document.GetElementById("exporterSubmitButton").AddEventListener("click",
				new Action(() => {
					self.exporter.export();
				}));
		}

		/// <summary>
		/// Assign change listeners for the exporter. These allow for the dynamic update
		/// of the exporter preview.
		/// </summary>
		public void assignExporterChangeListeners()
		{
			var self = this;

			var blockDefCheck = (HTMLInputElement)Document.GetElementById("blockDefCheck");
			var genStubCheck = (HTMLInputElement)Document.GetElementById("genStubCheck");

			// Select the block definitions and generator stubs on default.
			blockDefCheck.Checked = true;
			genStubCheck.Checked = true;

			// Checking the block definitions checkbox displays preview of code to export.
			Document.GetElementById("blockDefCheck").AddEventListener("change",
				new Action<Event>((e) => {
					self.ifCheckedEnable(blockDefCheck.Checked,
					  new JsArray<string> { "blockDefs", "blockDefSettings" });
				}));

			// Preview updates when user selects different block definition format.
			Document.GetElementById("exportFormat").AddEventListener("change",
				new Action<Event>((e) => {
					self.exporter.updatePreview();
				}));

			// Checking the generator stub checkbox displays preview of code to export.
			Document.GetElementById("genStubCheck").AddEventListener("change",
				new Action<Event>((e) => {
					self.ifCheckedEnable(genStubCheck.Checked,
					new JsArray<string> { "genStubs", "genStubSettings" });
				}));

			// Preview updates when user selects different generator stub language.
			Document.GetElementById("exportLanguage").AddEventListener("change",
				new Action<Event>((e) => {
					self.exporter.updatePreview();
				}));
		}

		/// <summary>
		/// If given checkbox is checked, enable the given elements.  Otherwise, disable.
		/// </summary>
		/// <param name="enabled"> True if enabled, false otherwise.</param>
		/// <param name="idArray">Array of element IDs to enable when
		/// checkbox is checked.</param> 
		public void ifCheckedEnable(bool enabled, JsArray<string> idArray)
		{
			for (var i = 0; i < idArray.Length; i++) {
				var id = idArray[i];
				var element = (HTMLElement)Document.GetElementById(id);
				if (enabled) {
					element.ClassList.Remove("disabled");
				}
				else {
					element.ClassList.Add("disabled");
				}
				var fields = element.QuerySelectorAll("input, textarea, select");
				for (var j = 0; j < fields.Length; j++) {
					var field = fields[j];
					var pi = field.GetType().GetProperty("Disabled", System.Reflection.BindingFlags.Public);
					pi.SetValue(field, !enabled);
				}
			}
		}

		/// <summary>
		/// Assign button click handlers for the block library.
		/// </summary>
		public void assignLibraryClickHandlers()
		{
			var self = this;

			// Button for saving block to library.
			Document.GetElementById("saveToBlockLibraryButton").AddEventListener("click",
				new Action(() => {
					self.blockLibraryController.saveToBlockLibrary();
				}));

			// Button for removing selected block from library.
			Document.GetElementById("removeBlockFromLibraryButton").AddEventListener(
			  "click",
				new Action(() => {
					self.blockLibraryController.removeFromBlockLibrary();
				}));

			// Button for clearing the block library.
			Document.GetElementById("clearBlockLibraryButton").AddEventListener("click",
				new Action(() => {
					self.blockLibraryController.clearBlockLibrary();
				}));

			// Hide and show the block library dropdown.
			Document.GetElementById("button_blockLib").AddEventListener("click",
				new Action(() => {
					self.openModal("dropdownDiv_blockLib");
				}));
		}

		/// <summary>
		/// Assign button click handlers for the block factory.
		/// </summary>
		public void assignBlockFactoryClickHandlers()
		{
			var self = this;
			// Assign button event handlers for Block Factory.
			Document.GetElementById("localSaveButton")
				.AddEventListener("click", new Action(() => {
					self.exportBlockLibraryToFile();
				}));

			Document.GetElementById("helpButton").AddEventListener("click",
				new Action(() => {
					Window.Open("https://developers.google.com/blockly/custom-blocks/block-factory",
						"BlockFactoryHelp");
				}));

			Document.GetElementById("files").AddEventListener("change",
				new Action<Event>((e) => {
					// Warn user.
					var replace = Window.Confirm("This imported block library will " +
						"replace your current block library.");
					if (replace) {
						self.importBlockLibraryFromFile();
						// Clear this so that the change event still fires even if the
						// same file is chosen again. If the user re-imports a file, we
						// want to reload the workspace with its contents.
						((HTMLInputElement)e.Target).Value = null;
					}
				}));

			Document.GetElementById("createNewBlockButton")
				.AddEventListener("click", new Action(() => {
					// If there are unsaved changes warn user, check if they'd like to
					// proceed with unsaved changes, and act accordingly.
					var proceedWithUnsavedChanges =
						self.blockLibraryController.warnIfUnsavedChanges();
					if (!proceedWithUnsavedChanges) {
						return;
					}

					BlockFactory.showStarterBlock();
					self.blockLibraryController.setNoneSelected();

					// Close the Block Library Dropdown.
					self.closeModal();
				}));
		}

		/// <summary>
		/// Add event listeners for the block factory.
		/// </summary>
		public void addBlockFactoryEventListeners()
		{
			// Update code on changes to block being edited.
			BlockFactory.mainWorkspace.addChangeListener(BlockFactory.updateLanguage);

			// Disable blocks not attached to the factory_base block.
			BlockFactory.mainWorkspace.addChangeListener(Blockly.Events.disableOrphans);

			// Update the buttons on the screen based on whether
			// changes have been saved.
			var self = this;
			BlockFactory.mainWorkspace.addChangeListener((e) => {
				self.blockLibraryController.updateButtons(FactoryUtils.savedBlockChanges(
					self.blockLibraryController));
			});

			Document.GetElementById("direction")
				.AddEventListener("change", new Action(BlockFactory.updatePreview));
			Document.GetElementById("languageTA")
				.AddEventListener("change", new Action(BlockFactory.updatePreview));
			Document.GetElementById("languageTA")
				.AddEventListener("keyup", new Action(BlockFactory.updatePreview));
			Document.GetElementById("format")
				.AddEventListener("change", new Action(BlockFactory.formatChange));
			Document.GetElementById("language")
				.AddEventListener("change", new Action(BlockFactory.updatePreview));
		}

		/// <summary>
		/// Handle Blockly Storage with App Engine.
		/// </summary>
		public void initializeBlocklyStorage()
		{
			BlocklyStorage.HTTPREQUEST_ERROR =
				"There was a problem with the request.\n";
			BlocklyStorage.LINK_ALERT =
				"Share your blocks with this link:\n\n%1";
			BlocklyStorage.HASH_ERROR =
				"Sorry, \"%1\" doesn\'t correspond with any saved Blockly file.";
			BlocklyStorage.XML_ERROR = "Could not load your saved file.\n" +
				"Perhaps it was created with a different version of Blockly?";
			var linkButton = (HTMLButtonElement)Document.GetElementById("linkButton");
			linkButton.Style.Display = Display.InlineBlock;
			linkButton.AddEventListener("click",
				new Action(() => {
					BlocklyStorage.link(BlockFactory.mainWorkspace);
				}));
			BlockFactory.disableEnableLink();
		}

		/// <summary>
		/// Handle resizing of elements.
		/// </summary>
		public void onresize(Event @event) {
			if (this.selectedTab == AppController.BLOCK_FACTORY) {
				// Handle resizing of Block Factory elements.
				var expandList = new Element[] {
				  Document.GetElementById("blocklyPreviewContainer"),
				  Document.GetElementById("blockly"),
				  Document.GetElementById("blocklyMask"),
				  Document.GetElementById("preview"),
				  Document.GetElementById("languagePre"),
				  Document.GetElementById("languageTA"),
				  Document.GetElementById("generatorPre"),
				};
				for (var i = 0; i < expandList.Length; i++) {
					var expand = (HTMLElement)expandList[i];
					expand.Style.Width = (((HTMLElement)expand.ParentNode).OffsetWidth - 2) + "px";
					expand.Style.Height = (((HTMLElement)expand.ParentNode).OffsetHeight - 2) + "px";
				}
			}
			else if (this.selectedTab == AppController.EXPORTER) {
				// Handle resize of Exporter block options.
				this.exporter.view.centerPreviewBlocks();
			}
		}

		/// <summary>
		/// Handler for the window's "onbeforeunload" event. When a user has unsaved
		/// changes and refreshes or leaves the page, confirm that they want to do so
		/// before actually refreshing.
		/// </summary>
		public string confirmLeavePage()
		{
			if ((!BlockFactory.isStarterBlock() &&
				!FactoryUtils.savedBlockChanges(this.blockLibraryController)) ||
				this.workspaceFactoryController.hasUnsavedChanges()) {
				// When a string is assigned to the returnValue Event property, a dialog box
				// appears, asking the users for confirmation to leave the page.
				return "You will lose any unsaved changes. Are you sure you want " +
					"to exit this page?";
			}
			return null;
		}

		/// <summary>
		/// Show a modal element, usually a dropdown list.
		/// </summary>
		/// <param name="id"> ID of element to show.</param>
		public void openModal(string id)
		{
			Blockly.Core.hideChaff();
			this.modalName_ = id;
			((HTMLElement)Document.GetElementById(id)).Style.Display = Display.Block;
			((HTMLElement)Document.GetElementById("modalShadow")).Style.Display = Display.Block;
		}

		/// <summary>
		/// Hide a previously shown modal element.
		/// </summary>
		public void closeModal()
		{
			var id = this.modalName_;
			if (String.IsNullOrEmpty(id)) {
				return;
			}
			((HTMLElement)Document.GetElementById(id)).Style.Display = Display.None;
			((HTMLElement)Document.GetElementById("modalShadow")).Style.Display = Display.None;
			this.modalName_ = null;
		}

		/// <summary>
		/// Name of currently open modal.
		/// </summary>
		private string modalName_ = null;

		/// <summary>
		/// Initialize Blockly and layout.  Called on page load.
		/// </summary>
		public void init()
		{
			// Block Factory has a dependency on bits of Closure that core Blockly
			// doesn't have. When you run this from file:// without a copy of Closure,
			// it breaks it non-obvious ways.  Warning about this for now until the
			// dependency is broken.
			// TODO: #668.
			if (!goog.dom.xml.isNotDefined()) {
				Window.Alert("Sorry: Closure dependency not found. We are working on removing " +
					"this dependency.  In the meantime, you can use our hosted demo\n " +
					"https://blockly-demo.appspot.com/static/demos/blockfactory/index.html" +
					"\nor use these instructions to continue running locally:\n" +
					"https://developers.google.com/blockly/guides/modify/web/closure");
				return;
			}

			var self = this;
			// Handle Blockly Storage with App Engine.
			if (BlocklyStorage.InWindow()) {
				this.initializeBlocklyStorage();
			}

			// Assign click handlers.
			this.assignExporterClickHandlers();
			this.assignLibraryClickHandlers();
			this.assignBlockFactoryClickHandlers();
			// Hide and show the block library dropdown.
			Document.GetElementById("modalShadow").AddEventListener("click",
				new Action(() => {
					self.closeModal();
				}));

			this.onresize(null);
			Window.AddEventListener("resize", new Action(() => {
				self.onresize(null);
			}));

			// Inject Block Factory Main Workspace.
			var toolbox = Document.GetElementById("blockfactory_toolbox");
			BlockFactory.mainWorkspace = Blockly.Core.inject("blockly", new Dictionary<string, object> {
				{ "collapse", false },
				{ "toolbox", toolbox },
				{ "media", "../../media/"} });

			// Add tab handlers for switching between Block Factory and Block Exporter.
			this.addTabHandlers(this.tabMap);

			// Assign exporter change listeners.
			this.assignExporterChangeListeners();

			// Create the root block on Block Factory main workspace.
			if (BlocklyStorage.InWindow() && Window.Location.Hash != null && Window.Location.Hash.Length > 1) {
				BlocklyStorage.retrieveXml(Window.Location.Hash.Substring(1),
										   BlockFactory.mainWorkspace);
			} else {
				BlockFactory.showStarterBlock();
			}
			BlockFactory.mainWorkspace.clearUndo();

			// Add Block Factory event listeners.
			this.addBlockFactoryEventListeners();

			// Workspace Factory init.
			WorkspaceFactoryInit.initWorkspaceFactory(this.workspaceFactoryController);
		}
	}
}
