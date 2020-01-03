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

/// <summary>
/// Controls the UI elements for workspace factory, mainly the category tabs.
/// Also includes downloading files because that interacts directly with the DOM.
/// Depends on WorkspaceFactoryController (for adding mouse listeners). Tabs for
/// each category are stored in tab map, which associates a unique ID for a
/// category with a particular tab.
/// </summary>
/// <author>Emma Dauterman (edauterman)</author>

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{

	public class WorkspaceFactoryView
	{
		public Dictionary<string, HTMLElement> tabMap;

		/// <summary>
		/// Class for a WorkspaceFactoryView
		/// </summary>
		public WorkspaceFactoryView()
		{
			// For each tab, maps ID of a ListElement to the td DOM element.
			this.tabMap = new Dictionary<string, HTMLElement>();
		}

		/// <summary>
		/// Adds a category tab to the UI, and updates tabMap accordingly.
		/// </summary>
		/// <param name="name"> The name of the category being created</param>
		/// <param name="id"> ID of category being created</param>
		/// <returns>DOM element created for tab</returns>
		public Element addCategoryRow(string name, string id)
		{
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			var count = table.Rows.Length;

			// Delete help label and enable category buttons if it's the first category.
			if (count == 0) {
				Document.GetElementById("categoryHeader").TextContent = "Your categories:";
			}

			// Create tab.
			var row = table.InsertRow(count);
			var nextEntry = row.InsertCell(0);
			// Configure tab.
			nextEntry.Id = this.createCategoryIdName(name);
			nextEntry.TextContent = name;
			// Store tab.
			this.tabMap[id] = ((HTMLTableRowElement)table.Rows[count]).Cells[0];
			// Return tab.
			return nextEntry;
		}

		/// <summary>
		/// Deletes a category tab from the UI and updates tabMap accordingly.
		/// </summary>
		/// <param name="id">ID of category to be deleted.</param>
		/// <param name="index"> The name of the category to be deleted.</param>
		public void deleteElementRow(string id, int index)
		{
			// Delete tab entry.
			Script.DeleteMemebr(this.tabMap, id);
			// Delete tab row.
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			var count = table.Rows.Length;
			table.DeleteRow(index);

			// If last category removed, add category help text and disable category
			// buttons.
			this.addEmptyCategoryMessage();
		}

		/// <summary>
		/// If there are no toolbox elements created, adds a help message to show
		/// where categories will appear. Should be called when deleting list elements
		/// in case the last element is deleted.
		/// </summary>
		public void addEmptyCategoryMessage()
		{
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			if (table.Rows.Length > 0) {
				Document.GetElementById("categoryHeader").TextContent =
					"You currently have no categories.";
			}
		}

		/// <summary>
		/// Given the index of the currently selected element, updates the state of
		/// the buttons that allow the user to edit the list elements. Updates the edit
		/// and arrow buttons. Should be called when adding or removing elements
		/// or when changing to a new element or when swapping to a different element.
		/// TODO(evd2014): Switch to using CSS to add/remove styles.
		/// </summary>
		/// <param name="selectedIndex"> The index of the currently selected category,
		/// -1 if no categories created.</param>
		/// <param name="selected"> The selected ListElement.</param>
		public void updateState(int selectedIndex, ListElement selected)
		{
			// Disable/enable editing buttons as necessary.
			((HTMLButtonElement)Document.GetElementById("button_editCategory")).Disabled = selectedIndex < 0 ||
				selected.type != ListElement.TYPE_CATEGORY;
			((HTMLButtonElement)Document.GetElementById("button_remove")).Disabled = selectedIndex < 0;
			((HTMLButtonElement)Document.GetElementById("button_up")).Disabled = selectedIndex <= 0;
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			((HTMLButtonElement)Document.GetElementById("button_down")).Disabled = selectedIndex >=
				table.Rows.Length - 1 || selectedIndex < 0;
			// Disable/enable the workspace as necessary.
			this.disableWorkspace(this.shouldDisableWorkspace(selected));
		}

		/// <summary>
		/// Determines the DOM ID for a category given its name.
		/// </summary>
		/// <param name="name"> Name of category</param>
		/// <returns>ID of category tab</returns>
		public string createCategoryIdName(string name)
		{
			return "tab_" + name;
		}

		/// <summary>
		/// Switches a tab on or off.
		/// </summary>
		/// <param name="id"> ID of the tab to switch on or off.</param>
		/// <param name="selected"> True if tab should be on, false if tab should be off.</param>
		public void setCategoryTabSelection(string id, bool selected)
		{
			if (!this.tabMap.ContainsKey(id)) {
				return;   // Exit if tab does not exist.
			}
			this.tabMap[id].ClassName = selected ? "tabon" : "taboff";
		}

		/// <summary>
		/// Used to bind a click to a certain DOM element (used for category tabs).
		/// Taken directly from code.js
		/// </summary>
		/// <param name="e1">Tab element or corresponding ID string.</param>
		/// <param name="func"> Function to be executed on click.</param>
		public void bindClick(Union<string, Element> el, Delegate func)
		{
			if (el.Is<string>()) {
				el = Document.GetElementById(el.As<string>());
			}
			el.As<Element>().AddEventListener("click", func, true);
			el.As<Element>().AddEventListener("touchend", func, true);
		}

		/// <summary>
		/// Creates a file and downloads it. In some browsers downloads, and in other
		/// browsers, opens new tab with contents.
		/// </summary>
		/// <param name="filename"> Name of file</param>
		/// <param name="data">Blob containing contents to download</param>
		public void createAndDownloadFile(string filename, Blob data)
		{
			var clickEvent = new MouseEvent("click", new Dictionary<string, object>{
				{ "view", Window.Instance },
				{ "bubbles", true },
				{ "cancelable", false }
			});
			var a = Document.CreateElement<HTMLAnchorElement>("a");
			a.Href = Window.URL.CreateObjectURL(data);
			a.Download = filename;
			a.TextContent = "Download file!";
			a.DispatchEvent(clickEvent);
		}

		/// <summary>
		/// Given the ID of a certain category, updates the corresponding tab in
		/// the DOM to show a new name.
		/// </summary>
		/// <param name="newName"></param> Name of string to be displayed on tab
		/// <param name="id"> ID of category to be updated</param>
		public void updateCategoryName(string newName, string id)
		{
			this.tabMap[id].TextContent = newName;
			this.tabMap[id].Id = this.createCategoryIdName(newName);
		}

		/// <summary>
		/// Moves a tab from one index to another. Adjusts index inserting before
		/// based on if inserting before or after. Checks that the indexes are in
		/// bounds, throws error if not.
		/// </summary>
		/// <param name="id"> The ID of the category to move.</param>
		/// <param name="newIndex"> The index to move the category to.</param>
		/// <param name="oldIndex"></param> The index the category is currently at.
		public void moveTabToIndex(string id, int newIndex, int oldIndex)
		{
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			// Check that indexes are in bounds.
			if (newIndex < 0 || newIndex >= table.Rows.Length || oldIndex < 0 ||
				oldIndex >= table.Rows.Length) {
				throw new Exception("Index out of bounds when moving tab in the view.");
			}

			if (newIndex < oldIndex) {
				// Inserting before.
				var row = table.InsertRow(newIndex);
				row.AppendChild(this.tabMap[id]);
				table.DeleteRow(oldIndex + 1);
			}
			else {
				// Inserting after.
				var row = table.InsertRow(newIndex + 1);
				row.AppendChild(this.tabMap[id]);
				table.DeleteRow(oldIndex);
			}
		}

		/// <summary>
		/// Given a category ID and color, use that color to color the left border of the
		/// tab for that category.
		/// </summary>
		/// <param name="id"> The ID of the category to color.</param>
		/// <param name="color">The color for to be used for the border of the tab.
		/// Must be a valid CSS string.</param>
		public void setBorderColor(string id, string color)
		{
			var tab = this.tabMap[id];
			tab.Style.BorderLeftWidth = "8px";
			tab.Style.BorderLeftStyle = "solid";
			tab.Style.BorderColor = color;
		}

		/// <summary>
		/// Given a separator ID, creates a corresponding tab in the view, updates
		/// tab map, and returns the tab.
		/// </summary>
		/// <param name="id">The ID of the separator.</param>
		/// <param name="opt_element">The td DOM element representing the separator.</param>
		public HTMLTableCellElement addSeparatorTab(string id, Element opt_element = null)
		{
			var table = (HTMLTableElement)Document.GetElementById("categoryTable");
			var count = table.Rows.Length;

			if (count == 0) {
				Document.GetElementById("categoryHeader").TextContent = "Your categories:";
			}
			// Create separator.
			var row = table.InsertRow(count);
			var nextEntry = row.InsertCell(0);
			// Configure separator.
			nextEntry.Style.Height = "10px";
			// Store and return separator.
			this.tabMap[id] = ((HTMLTableRowElement)table.Rows[count]).Cells[0];
			return nextEntry;
		}

		/// <summary>
		/// Disables or enables the workspace by putting a div over or under the
		/// toolbox workspace, depending on the value of disable. Used when switching
		/// to/from separators where the user shouldn't be able to drag blocks into
		/// the workspace.
		/// </summary>
		/// <param name="disable">True if the workspace should be disabled, false
		/// if it should be enabled.</param>
		public void disableWorkspace(bool disable)
		{
			if (disable) {
				Document.GetElementById("toolbox_section").ClassName = "disabled";
				((HTMLDivElement)Document.GetElementById("toolbox_blocks")).Style.PointerEvents = "none";
			}
			else {
				Document.GetElementById("toolbox_section").ClassName = "";
				((HTMLDivElement)Document.GetElementById("toolbox_blocks")).Style.PointerEvents = "auto";
			}
		}

		/// <summary>
		/// Determines if the workspace should be disabled. The workspace should be
		/// disabled if category is a separator or has VARIABLE or PROCEDURE tags.
		/// </summary>
		/// <returns>True if the workspace should be disabled, false otherwise.</returns>
		public bool shouldDisableWorkspace(ListElement category)
		{
			return category != null && category.type != ListElement.TYPE_FLYOUT &&
				(category.type == ListElement.TYPE_SEPARATOR ||
				category.custom == "VARIABLE" || category.custom == "PROCEDURE");
		}

		/// <summary>
		/// Removes all categories and separators in the view. Clears the tabMap to
		/// reflect this.
		/// </summary>
		public void clearToolboxTabs()
		{
			this.tabMap = new Dictionary<string, HTMLElement>();
			var oldCategoryTable = (HTMLTableElement)Document.GetElementById("categoryTable");
			var newCategoryTable = Document.CreateElement<HTMLTableElement>("table");
			newCategoryTable.Id = "categoryTable";
			newCategoryTable.Style.Width = "auto";
			oldCategoryTable.ParentElement.ReplaceChild(newCategoryTable,
				oldCategoryTable);
		}

		/// <summary>
		/// Given a set of blocks currently loaded user-generated shadow blocks, visually
		/// marks them without making them actual shadow blocks (allowing them to still
		/// be editable and movable).
		/// </summary>
		/// <param name="blocks">Array of user-generated shadow blocks
		/// currently loaded.</param>
		public void markShadowBlocks(JsArray<Blockly.Block> blocks)
		{
			for (var i = 0; i < blocks.Length; i++) {
				this.markShadowBlock((Blockly.BlockSvg)blocks[i]);
			}
		}

		/// <summary>
		/// Visually marks a user-generated shadow block as a shadow block in the
		/// workspace without making the block an actual shadow block (allowing it
		/// to be moved and edited).
		/// </summary>
		/// <param name="block">The block that should be marked as a shadow
		/// block (must be rendered).</param> 
		public void markShadowBlock(Blockly.BlockSvg block)
		{
			// Add Blockly CSS for user-generated shadow blocks.
			Blockly.Core.addClass_(block.svgGroup_, "shadowBlock");
			// If not a valid shadow block, add a warning message.
			if (block.getSurroundParent() == null) {
				block.setWarningText("Shadow blocks must be nested inside" +
					" other blocks to be displayed.");
			}
			if (FactoryUtils.hasVariableField(block)) {
				block.setWarningText("Cannot make variable blocks shadow blocks.");
			}
		}

		/// <summary>
		/// Removes visual marking for a shadow block given a rendered block.
		/// </summary>
		/// <param name="block">The block that should be unmarked as a shadow
		/// block (must be rendered).</param>
		public void unmarkShadowBlock(Blockly.BlockSvg block)
		{
			// Remove Blockly CSS for user-generated shadow blocks.
			if (Blockly.Core.hasClass_(block.svgGroup_, "shadowBlock")) {
				Blockly.Core.removeClass_(block.svgGroup_, "shadowBlock");
			}
		}

		/// <summary>
		/// Sets the tabs for modes according to which mode the user is currenly
		/// editing in.
		/// </summary>
		/// <param name="mode">The mode being switched to
		/// (WorkspaceFactoryController.MODE_TOOLBOX or WorkspaceFactoryController.MODE_PRELOAD).</param>
		public void setModeSelection(string mode)
		{
			Document.GetElementById("tab_preload").ClassName = mode ==
				WorkspaceFactoryController.MODE_PRELOAD ? "tabon" : "taboff";
			((HTMLDivElement)Document.GetElementById("preload_div")).Style.Display = mode ==
				WorkspaceFactoryController.MODE_PRELOAD ? Display.Block : Display.None;
			Document.GetElementById("tab_toolbox").ClassName = mode ==
				WorkspaceFactoryController.MODE_TOOLBOX ? "tabon" : "taboff";
			((HTMLElement)Document.GetElementById("toolbox_div")).Style.Display = mode ==
				WorkspaceFactoryController.MODE_TOOLBOX ? Display.Block : Display.None;
		}

		/// <summary>
		/// Updates the help text above the workspace depending on the selected mode.
		/// </summary>
		/// <param name="mode">The selected mode (WorkspaceFactoryController.MODE_TOOLBOX or
		/// WorkspaceFactoryController.MODE_PRELOAD).</param>
		public void updateHelpText(string mode)
		{
			string helpText;
			if (mode == WorkspaceFactoryController.MODE_TOOLBOX) {
				helpText = "Drag blocks into the workspace to configure the toolbox " +
					"in your custom workspace.";
			}
			else {
				helpText = "Drag blocks into the workspace to pre-load them in your " +
					"custom workspace.";

			}
			Document.GetElementById("editHelpText").TextContent = helpText;
		}

		/// <summary>
		/// Sets the basic options that are not dependent on if there are categories
		/// or a single flyout of blocks. Updates checkboxes and text fields.
		/// </summary>
		public void setBaseOptions()
		{
			// Set basic options.
			((HTMLInputElement)Document.GetElementById("option_css_checkbox")).Checked = true;
			((HTMLInputElement)Document.GetElementById("option_infiniteBlocks_checkbox")).Checked = true;
			((HTMLInputElement)Document.GetElementById("option_maxBlocks_number")).Value = "100";
			((HTMLInputElement)Document.GetElementById("option_media_text")).Value =
				"https://blockly-demo.appspot.com/static/media/";
			((HTMLInputElement)Document.GetElementById("option_readOnly_checkbox")).Checked = false;
			((HTMLInputElement)Document.GetElementById("option_rtl_checkbox")).Checked = false;
			((HTMLInputElement)Document.GetElementById("option_sounds_checkbox")).Checked = true;

			// Uncheck grid and zoom options and hide suboptions.
			((HTMLInputElement)Document.GetElementById("option_grid_checkbox")).Checked = false;
			((HTMLDivElement)Document.GetElementById("grid_options")).Style.Display = Display.None;
			((HTMLInputElement)Document.GetElementById("option_zoom_checkbox")).Checked = false;
			((HTMLDivElement)Document.GetElementById("zoom_options")).Style.Display = Display.None;

			// Set grid options.
			((HTMLInputElement)Document.GetElementById("gridOption_spacing_number")).Value = "0";
			((HTMLInputElement)Document.GetElementById("gridOption_length_number")).Value = "1";
			((HTMLInputElement)Document.GetElementById("gridOption_colour_text")).Value = "#888";
			((HTMLInputElement)Document.GetElementById("gridOption_snap_checkbox")).Checked = false;

			// Set zoom options.
			((HTMLInputElement)Document.GetElementById("zoomOption_controls_checkbox")).Checked = true;
			((HTMLInputElement)Document.GetElementById("zoomOption_wheel_checkbox")).Checked = true;
			((HTMLInputElement)Document.GetElementById("zoomOption_startScale_number")).Value = "1.0";
			((HTMLInputElement)Document.GetElementById("zoomOption_maxScale_number")).Value = "3";
			((HTMLInputElement)Document.GetElementById("zoomOption_minScale_number")).Value = "0.3";
			((HTMLInputElement)Document.GetElementById("zoomOption_scaleSpeed_number")).Value = "1.2";
		}

		/// <summary>
		/// Updates category specific options depending on if there are categories
		/// currently present. Updates checkboxes and text fields in the view.
		/// </summary>
		/// <param name="hasCategories"> True if categories are present, false if all
		/// blocks are displayed in a single flyout.</param>
		public void setCategoryOptions(bool hasCategories)
		{
			((HTMLInputElement)Document.GetElementById("option_collapse_checkbox")).Checked = hasCategories;
			((HTMLInputElement)Document.GetElementById("option_comments_checkbox")).Checked = hasCategories;
			((HTMLInputElement)Document.GetElementById("option_disable_checkbox")).Checked = hasCategories;
			((HTMLInputElement)Document.GetElementById("option_scrollbars_checkbox")).Checked = hasCategories;
			((HTMLInputElement)Document.GetElementById("option_trashcan_checkbox")).Checked = hasCategories;
		}
	}
}
