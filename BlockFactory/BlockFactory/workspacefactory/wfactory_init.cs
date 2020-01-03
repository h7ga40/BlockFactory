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
 * @fileoverview Contains the init functions for the workspace factory tab.
 * Adds click handlers to buttons and dropdowns, adds event listeners for
 * keydown events and Blockly events, and configures the initial setup of
 * the page.
 *
 * @author Emma Dauterman (evd2014)
 */

using System;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	/// <summary>
	/// Namespace for workspace factory initialization methods.
	/// </summary>
	public static class WorkspaceFactoryInit
	{
		/// <summary>
		/// Initialization for workspace factory tab.
		/// </summary>
		/// <param  name="controller">The controller for the workspace
		/// factory tab.</param>
		public static void initWorkspaceFactory(WorkspaceFactoryController controller)
		{
			// Disable category editing buttons until categories are created.
			((HTMLButtonElement)Document.GetElementById("button_remove")).Disabled = true;
			((HTMLButtonElement)Document.GetElementById("button_up")).Disabled = true;
			((HTMLButtonElement)Document.GetElementById("button_down")).Disabled = true;
			((HTMLButtonElement)Document.GetElementById("button_editCategory")).Disabled = true;

			initColorPicker_(controller);
			addWorkspaceFactoryEventListeners_(controller);
			assignWorkspaceFactoryClickHandlers_(controller);
			addWorkspaceFactoryOptionsListeners_(controller);

			// Check standard options and apply the changes to update the view.
			controller.setStandardOptionsAndUpdate();
		}

		/// <summary>
		/// Initialize the color picker in workspace factory.
		/// </summary>
		/// <param  name="controller">The controller for the workspace
		/// factory tab.</param>
		private static void initColorPicker_(WorkspaceFactoryController controller)
		{
			// Array of Blockly category colors, variety of hues with saturation 45%
			// and value 65% as specified in Blockly Developer documentation:
			// developers.google.com/blockly/guides/create-custom-blocks/define-blocks
			var colors = new JsArray<string> {
				"#A6795C", "#A69F5C", "#88A65C", "#5EA65C", "#5CA67E", "#5CA6A4", "#5C83A6",
				"#5E5CA6", "#835CA6", "#A65CA4", "#A65C7E", "#A65C5E",
				"#A6725C", "#A6975C", "#90A65C", "#66A65C", "#5CA677", "#5CA69C", "#5C8BA6",
				"#5C61A6", "#7C5CA6", "#A15CA6", "#A65C86", "#A65C61",
				"#A66A5C", "#A6905C", "#97A65C", "#6FA65C", "#5CA66F", "#5CA695", "#5C92A6",
				"#5C6AA6", "#745CA6", "#9A5CA6", "#A65C8D", "#A65C66",
				"#A6635C", "#A6885C", "#9FA65C", "#79A65C", "#5CA668", "#5CA68D", "#5C9AA6",
				"#5C74A6", "#6D5CA6", "#925CA6", "#A65C95", "#A65C6F",
				"#A65C5C", "#A6815C", "#A6A65C", "#81A65C", "#5CA661", "#5CA686", "#5CA1A6",
				"#5C7CA6", "#665CA6", "#8B5CA6", "#A65C9C", "#A65C77"
			};

			// Create color picker with specific set of Blockly colors.
			var colorPicker = new goog.ui.ColorPicker();
			colorPicker.setSize(12);
			colorPicker.setColors(colors);

			// Create and render the popup color picker and attach to button.
			var popupPicker = new goog.ui.PopupColorPicker(null, colorPicker);
			popupPicker.render();
			popupPicker.attach(Document.GetElementById("dropdown_color"));
			popupPicker.setFocusable(true);
			goog.events.listen(popupPicker, "change", new Action<Event>((e) => {
				controller.changeSelectedCategoryColor(popupPicker.getSelectedColor());
				BlockFactory.blocklyFactory.closeModal();
			}));
		}

		/// <summary>
		/// Assign click handlers for workspace factory.
		/// </summary>
		/// <param name="controller"> The controller for the workspace
		/// factory tab.</param>
		private static void assignWorkspaceFactoryClickHandlers_(WorkspaceFactoryController controller)
		{

			// Import Custom Blocks button.
			Document.GetElementById("button_importBlocks").AddEventListener
				("click",
				new Action(() => {
					BlockFactory.blocklyFactory.openModal("dropdownDiv_importBlocks");
				}));
			Document.GetElementById("input_importBlocksJson").AddEventListener
				("change",
				new Action<Event>((@event) => {
					controller.importBlocks(((HTMLInputElement)@event.Target).files[0], "JSON");
				}));
			Document.GetElementById("input_importBlocksJson").AddEventListener
				("click", new Action(() => { BlockFactory.blocklyFactory.closeModal(); }));
			Document.GetElementById("input_importBlocksJs").AddEventListener
				("change",
				new Action<Event>((@event) => {
					controller.importBlocks(((HTMLInputElement)@event.Target).files[0], "JavaScript");
				}));
			Document.GetElementById("input_importBlocksJs").AddEventListener
				("click", new Action(() => { BlockFactory.blocklyFactory.closeModal(); }));

			// Load to Edit button.
			Document.GetElementById("button_load").AddEventListener
				("click",
				new Action(() => {
					BlockFactory.blocklyFactory.openModal("dropdownDiv_load");
				}));
			Document.GetElementById("input_loadToolbox").AddEventListener
				("change",
				new Action<Event>((@event) => {
					controller.importFile(((HTMLInputElement)@event.Target).files[0],
			  WorkspaceFactoryController.MODE_TOOLBOX);
				}));
			Document.GetElementById("input_loadToolbox").AddEventListener
				("click", new Action(() => { BlockFactory.blocklyFactory.closeModal(); }));
			Document.GetElementById("input_loadPreload").AddEventListener
				("change",
				new Action<Event>((@event) => {
					controller.importFile(((HTMLInputElement)@event.Target).files[0],
			  WorkspaceFactoryController.MODE_PRELOAD);
				}));
			Document.GetElementById("input_loadPreload").AddEventListener
				("click", new Action(() => { BlockFactory.blocklyFactory.closeModal(); }));

			// Export button.
			Document.GetElementById("dropdown_exportOptions").AddEventListener
				("click",
				new Action(() => {
					controller.exportInjectFile();
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_exportToolbox").AddEventListener
				("click",
				new Action(() => {
					controller.exportXmlFile(WorkspaceFactoryController.MODE_TOOLBOX);
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_exportPreload").AddEventListener
				("click",
				new Action(() => {
					controller.exportXmlFile(WorkspaceFactoryController.MODE_PRELOAD);
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_exportAll").AddEventListener
				("click",
				new Action(() => {
					controller.exportInjectFile();
					controller.exportXmlFile(WorkspaceFactoryController.MODE_TOOLBOX);
					controller.exportXmlFile(WorkspaceFactoryController.MODE_PRELOAD);
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("button_export").AddEventListener
				("click",
				new Action(() => {
					BlockFactory.blocklyFactory.openModal("dropdownDiv_export");
				}));

			// Clear button.
			Document.GetElementById("button_clear").AddEventListener
				("click",
				new Action(() => {
					controller.clearAll();
				}));

			// Toolbox and Workspace tabs.
			Document.GetElementById("tab_toolbox").AddEventListener
				("click",
				new Action(() => {
					controller.setMode(WorkspaceFactoryController.MODE_TOOLBOX);
				}));
			Document.GetElementById("tab_preload").AddEventListener
				("click",
				new Action(() => {
					controller.setMode(WorkspaceFactoryController.MODE_PRELOAD);
				}));

			// "+" button.
			Document.GetElementById("button_add").AddEventListener
				("click",
				new Action(() => {
					BlockFactory.blocklyFactory.openModal("dropdownDiv_add");
				}));
			Document.GetElementById("dropdown_newCategory").AddEventListener
				("click",
				new Action(() => {
					controller.addCategory();
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_loadCategory").AddEventListener
				("click",
				new Action(() => {
					controller.loadCategory();
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_separator").AddEventListener
				("click",
				new Action(() => {
					controller.addSeparator();
					BlockFactory.blocklyFactory.closeModal();
				}));
			Document.GetElementById("dropdown_loadStandardToolbox").AddEventListener
				("click",
				new Action(() => {
					controller.loadStandardToolbox();
					BlockFactory.blocklyFactory.closeModal();
				}));

			// "-" button.
			Document.GetElementById("button_remove").AddEventListener
				("click",
				new Action(() => {
					controller.removeElement();
				}));

			// Up/Down buttons.
			Document.GetElementById("button_up").AddEventListener
				("click",
				new Action(() => {
					controller.moveElement(-1);
				}));
			Document.GetElementById("button_down").AddEventListener
				("click",
				new Action(() => {
					controller.moveElement(1);
				}));

			// Edit Category button.
			Document.GetElementById("button_editCategory").AddEventListener
				("click",
				new Action(() => {
					BlockFactory.blocklyFactory.openModal("dropdownDiv_editCategory");
				}));
			Document.GetElementById("dropdown_name").AddEventListener
				("click",
				new Action(() => {
					controller.changeCategoryName();
					BlockFactory.blocklyFactory.closeModal();
				}));

			// Make/Remove Shadow buttons.
			Document.GetElementById("button_addShadow").AddEventListener
				("click",
				new Action(() => {
					controller.addShadow();
					WorkspaceFactoryInit.displayAddShadow_(false);
					WorkspaceFactoryInit.displayRemoveShadow_(true);
				}));
			Document.GetElementById("button_removeShadow").AddEventListener
				("click",
				new Action(() => {
					controller.removeShadow();
					WorkspaceFactoryInit.displayAddShadow_(true);
					WorkspaceFactoryInit.displayRemoveShadow_(false);

					// Disable shadow editing button if turning invalid shadow block back
					// to normal block.
					if (Blockly.Core.selected.getSurroundParent() == null) {
						((HTMLButtonElement)Document.GetElementById("button_addShadow")).Disabled = true;
					}
				}));

			// Help button on workspace tab.
			Document.GetElementById("button_optionsHelp").AddEventListener
				("click", new Action(() => {
					Window.Open("https://developers.google.com/blockly/guides/get-started/web#configuration");
				}));

			// Reset to Default button on workspace tab.
			Document.GetElementById("button_standardOptions").AddEventListener
				("click", new Action(() => {
					controller.setStandardOptionsAndUpdate();
				}));
		}

		/// <summary>
		/// Add event listeners for workspace factory.
		/// </summary>
		/// <param name="controller"> The controller for the workspace
		/// factory tab.</param>
		private static void addWorkspaceFactoryEventListeners_(WorkspaceFactoryController controller)
		{
			// Use up and down arrow keys to move categories.
			Window.AddEventListener("keydown", new Action<Event>((e) => {
				// Don't let arrow keys have any effect if not in Workspace Factory
				// editing the toolbox.
				if (!(controller.keyEventsEnabled && controller.selectedMode
					== WorkspaceFactoryController.MODE_TOOLBOX)) {
					return;
				}

				if (e.KeyCode == 38) {
					// Arrow up.
					controller.moveElement(-1);
				}
				else if (e.KeyCode == 40) {
					// Arrow down.
					controller.moveElement(1);
				}
			}));

			// Determines if a block breaks shadow block placement rules.
			// Breaks rules if (1) a shadow block no longer has a valid
			// parent, or (2) a normal block is inside of a shadow block.
			var isInvalidBlockPlacement = new Func<Blockly.Block, bool>((block) => {
				return ((controller.isUserGenShadowBlock(block.id) &&
					block.getSurroundParent() == null) ||
					(!controller.isUserGenShadowBlock(block.id) && block.getSurroundParent() != null
					&& controller.isUserGenShadowBlock(block.getSurroundParent().id)));
			});

			// Add change listeners for toolbox workspace in workspace factory.
			controller.toolboxWorkspace.addChangeListener((e) => {
				// Listen for Blockly move and delete events to update preview.
				// Not listening for Blockly create events because causes the user to drop
				// blocks when dragging them into workspace. Could cause problems if ever
				// load blocks into workspace directly without calling updatePreview.
				if (e.type == Blockly.Events.MOVE || e.type == Blockly.Events.DELETE ||
					  e.type == Blockly.Events.CHANGE) {
					controller.saveStateFromWorkspace();
					controller.updatePreview();
				}

				// Listen for Blockly UI events to correctly enable the "Edit Block" button.
				// Only enable "Edit Block" when a block is selected and it has a
				// surrounding parent, meaning it is nested in another block (blocks that
				// are not nested in parents cannot be shadow blocks).
				if (e.type == Blockly.Events.MOVE || (e.type == Blockly.Events.UI &&
					((Blockly.Events.Ui)e).element == "selected")) {
					var selected = Blockly.Core.selected;

					// Show shadow button if a block is selected. Show "Add Shadow" if
					// a block is not a shadow block, show "Remove Shadow" if it is a
					// shadow block.
					if (selected != null) {
						var isShadow = controller.isUserGenShadowBlock(selected.id);
						WorkspaceFactoryInit.displayAddShadow_(!isShadow);
						WorkspaceFactoryInit.displayRemoveShadow_(isShadow);
					}
					else {
						WorkspaceFactoryInit.displayAddShadow_(false);
						WorkspaceFactoryInit.displayRemoveShadow_(false);
					}

					if (selected != null && selected.getSurroundParent() != null &&
						!controller.isUserGenShadowBlock(selected.getSurroundParent().id)) {
						// Selected block is a valid shadow block or could be a valid shadow
						// block.

						// Enable block editing and remove warnings if the block is not a
						// variable user-generated shadow block.
						((HTMLButtonElement)Document.GetElementById("button_addShadow")).Disabled = false;
						((HTMLButtonElement)Document.GetElementById("button_removeShadow")).Disabled = false;

						if (!FactoryUtils.hasVariableField(selected) &&
							controller.isDefinedBlock(selected)) {
							selected.setWarningText(null);
						}
					}
					else {
						// Selected block cannot be a valid shadow block.

						if (selected != null && isInvalidBlockPlacement(selected)) {
							// Selected block breaks shadow block rules.
							// Invalid shadow block if (1) a shadow block no longer has a valid
							// parent, or (2) a normal block is inside of a shadow block.

							if (!controller.isUserGenShadowBlock(selected.id)) {
								// Warn if a non-shadow block is nested inside a shadow block.
								selected.setWarningText("Only shadow blocks can be nested inside\n"
									+ "other shadow blocks.");
							}
							else if (!FactoryUtils.hasVariableField(selected)) {
								// Warn if a shadow block is invalid only if not replacing
								// warning for variables.
								selected.setWarningText("Shadow blocks must be nested inside other"
									+ " blocks.");

							}

							// Give editing options so that the user can make an invalid shadow
							// block a normal block.
							((HTMLButtonElement)Document.GetElementById("button_removeShadow")).Disabled = false;
							((HTMLButtonElement)Document.GetElementById("button_addShadow")).Disabled = true;
						}
						else {
							// Selected block does not break any shadow block rules, but cannot
							// be a shadow block.

							// Remove possible "invalid shadow block placement" warning.
							if (selected != null && controller.isDefinedBlock(selected) &&
								(!FactoryUtils.hasVariableField(selected) ||
								!controller.isUserGenShadowBlock(selected.id))) {
								selected.setWarningText(null);
							}

							// No block selected that is a shadow block or could be a valid shadow
							// block. Disable block editing.
							((HTMLButtonElement)Document.GetElementById("button_addShadow")).Disabled = true;
							((HTMLButtonElement)Document.GetElementById("button_removeShadow")).Disabled = true;
						}
					}
				}

				// Convert actual shadow blocks added from the toolbox to user-generated
				// shadow blocks.
				if (e.type == Blockly.Events.CREATE) {
					controller.convertShadowBlocks();

					// Let the user create a Variables or Functions category if they use
					// blocks from either category.

					// Get all children of a block and add them to childList.
					Action<Blockly.Block, JsArray<Blockly.Block>> getAllChildren = null;
					getAllChildren = new Action<Blockly.Block, JsArray<Blockly.Block>>((block, childList) => {
						childList.Push(block);
						var children = block.getChildren();
						for (var i = 0; i < children.Length; i++) {
							var child = children[i];
							getAllChildren(child, childList);
						}
					});

					var newBaseBlock = controller.toolboxWorkspace.getBlockById(e.blockId);
					var allNewBlocks = new JsArray<Blockly.Block>();
					getAllChildren(newBaseBlock, allNewBlocks);
					var variableCreated = false;
					var procedureCreated = false;

					// Check if the newly created block or any of its children are variable
					// or procedure blocks.
					for (var i = 0; i < allNewBlocks.Length; i++) {
						var block = allNewBlocks[i];
						if (FactoryUtils.hasVariableField(block)) {
							variableCreated = true;
						}
						else if (FactoryUtils.isProcedureBlock(block)) {
							procedureCreated = true;
						}
					}

					// If any of the newly created blocks are variable or procedure blocks,
					// prompt the user to create the corresponding standard category.
					if (variableCreated && !controller.hasVariablesCategory()) {
						if (Window.Confirm("Your new block has a variables field. To use this block "
							+ "fully, you will need a Variables category. Do you want to add "
							+ "a Variables category to your custom toolbox?")) {
							controller.setMode(WorkspaceFactoryController.MODE_TOOLBOX);
							controller.loadCategoryByName("variables");
						}
					}

					if (procedureCreated && !controller.hasProceduresCategory()) {
						if (Window.Confirm("Your new block is a function block. To use this block "
							+ "fully, you will need a Functions category. Do you want to add "
							+ "a Functions category to your custom toolbox?")) {
							controller.setMode(WorkspaceFactoryController.MODE_TOOLBOX);
							controller.loadCategoryByName("functions");
						}
					}
				}
			});
		}

		/// <summary>
		/// Display or hide the add shadow button.
		/// </summary>
		/// <param name="show">True if the add shadow button should be shown, false
		/// otherwise.</param> 
		public static void displayAddShadow_(bool show)
		{
			((HTMLButtonElement)Document.GetElementById("button_addShadow")).Style.Display =
				show ? Display.InlineBlock : Display.None;
		}

		/// <summary>
		/// Display or hide the remove shadow button.
		/// </summary>
		/// <param  name="show">True if the remove shadow button should be shown, false
		/// otherwise.</param> 
		public static void displayRemoveShadow_(bool show)
		{
			((HTMLButtonElement)Document.GetElementById("button_removeShadow")).Style.Display =
				show ? Display.InlineBlock : Display.None;
		}

		/// <summary>
		/// Add listeners for workspace factory options input elements.
		/// </summary>
		/// <param name="controller"> The controller for the workspace
		/// factory tab.</param>
		private static void addWorkspaceFactoryOptionsListeners_(WorkspaceFactoryController controller)
		{
			// Checking the grid checkbox displays grid options.
			((HTMLInputElement)Document.GetElementById("option_grid_checkbox")).AddEventListener("change",
				new Action<Event>((e) => {
					((HTMLDivElement)Document.GetElementById("grid_options")).Style.Display =
					((HTMLInputElement)Document.GetElementById("option_grid_checkbox")).Checked ?
					Display.Block : Display.None;
				}));

			// Checking the grid checkbox displays zoom options.
			Document.GetElementById("option_zoom_checkbox").AddEventListener("change",
				new Action<Event>((e) => {
					((HTMLDivElement)Document.GetElementById("zoom_options")).Style.Display =
					((HTMLInputElement)Document.GetElementById("option_zoom_checkbox")).Checked ?
					Display.Block : Display.None;
				}));

			Document.GetElementById("option_readOnly_checkbox").AddEventListener("change",
			new Action<Event>((e) => {
				var checkbox = (HTMLInputElement)Document.GetElementById("option_readOnly_checkbox");
				BlockFactory.blocklyFactory.ifCheckedEnable(!checkbox.Checked,
					new JsArray<string> { "readonly1", "readonly2", "readonly3", "readonly4", "readonly5",
					"readonly6", "readonly7" });
			}));

			Document.GetElementById("option_infiniteBlocks_checkbox").AddEventListener("change",
			new Action<Event>((e) => {
				((HTMLDivElement)Document.GetElementById("maxBlockNumber_option")).Style.Display =
					((HTMLInputElement)Document.GetElementById("option_infiniteBlocks_checkbox")).Checked ?
					Display.None : Display.Block;
			}));

			// Generate new options every time an options input is updated.
			var optionsElements = Document.GetElementsByClassName("optionsInput");
			for (var i = 0; i < optionsElements.Length; i++) {
				optionsElements[i].AddEventListener("change", new Action<Event>((e) => {
					controller.generateNewOptions();
				}));
			}
		}
	}
}
