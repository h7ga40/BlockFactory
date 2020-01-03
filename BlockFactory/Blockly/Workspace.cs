/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2012 Google Inc.
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

/**
 * @fileoverview Object representing a workspace.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Workspace
	{
		public string id;
		public Options options;
		private static Dictionary<string, Workspace> WorkspaceDB_ = new Dictionary<string, Workspace>();
		public bool isInFlyout;
		public bool isMutator;
		public bool RTL;
		public bool horizontalLayout;
		public int toolboxPosition;
		private JsArray<Block> topBlocks_ = new JsArray<Block>();
		private JsArray<Action<Events.Abstract>> listeners_ = new JsArray<Action<Events.Abstract>>();
		protected JsArray<Events.Abstract> undoStack_ = new JsArray<Events.Abstract>();
		protected JsArray<Events.Abstract> redoStack_ = new JsArray<Events.Abstract>();
		internal Dictionary<string, Block> blockDB_ = new Dictionary<string, Block>();

		/// <summary>
		/// A list of all of the named variables in the workspace, including variables
		/// that are not currently in use.
		/// </summary>
		public JsArray<string> variableList = new JsArray<string>();

		/// <summary>
		/// Workspaces may be headless.
		/// True if visible.  False if headless.
		/// </summary>
		public bool rendered;

		/// <summary>
		/// Maximum number of undo events in stack.
		/// 0 to turn off undo, Infinity for unlimited.
		/// </summary>
		public int MAX_UNDO = 1024;

		public bool isFlyout;
		public Workspace targetWorkspace;
		public ConnectionDB[] connectionDBList;

		/// <summary>
		/// Class for a workspace.  This is a data structure that contains blocks.
		/// There is no UI, and can be created headlessly.
		/// </summary>
		/// <param name="opt_options">Dictionary of options.</param>
		public Workspace(Options opt_options = null)
		{
			id = Core.genUid();
			WorkspaceDB_[this.id] = this;
			options = opt_options ?? new Options();
			RTL = options.RTL;
			horizontalLayout = options.horizontalLayout;
			toolboxPosition = options.toolboxPosition;
		}

		/// <summary>
		/// Dispose of this workspace.
		/// Unlink from all DOM elements to prevent memory leaks.
		/// </summary>
		public void dispose()
		{
			this.listeners_.Clear();
			this.clear();
			// Remove from workspace database.
			WorkspaceDB_.Remove(this.id);
			//Script.Delete(WorkspaceDB_[this.id]);
		}

		/// <summary>
		/// Angle away from the horizontal to sweep for blocks.  Order of execution is
		/// generally top to bottom, but a small angle changes the scan to give a bit of
		/// a left to right bias (reversed in RTL).  Units are in degrees.
		/// See: http://tvtropes.org/pmwiki/pmwiki.php/Main/DiagonalBilling.
		/// </summary>
		public static int SCAN_ANGLE = 3;

		/// <summary>
		/// Add a block to the list of top blocks.
		/// </summary>
		/// <param name="block">Block to remove.</param>
		public void addTopBlock(Block block)
		{
			this.topBlocks_.Push(block);
			if (this.isFlyout) {
				// This is for the (unlikely) case where you have a variable in a block in
				// an always-open flyout.  It needs to be possible to edit the block in the
				// flyout, so the contents of the dropdown need to be correct.
				var variables = Core.Variables.allUsedVariables(block);
				for (var i = 0; i < variables.Length; i++) {
					if (this.variableList.IndexOf(variables[i]) == -1) {
						this.variableList.Push(variables[i]);
					}
				}
			}
		}

		/// <summary>
		/// Remove a block from the list of top blocks.
		/// </summary>
		/// <param name="block">Block to remove.</param>
		public void removeTopBlock(Block block)
		{
			if (!this.topBlocks_.Remove(block)) {
				throw new Exception("Block not present in workspace\'s list of top-most blocks.");
			}
		}

		/// <summary>
		/// Finds the top-level blocks and returns them.  Blocks are optionally sorted
		/// by position; top to bottom (with slight LTR or RTL bias).
		/// </summary>
		/// <param name="ordered">Sort the list if true.</param>
		/// <returns>The top-level block objects.</returns>
		public JsArray<Block> getTopBlocks(bool ordered)
		{
			// Copy the topBlocks_ list.
			var blocks = new JsArray<Block>();
			this.topBlocks_.ForEach((b) => blocks.Push(b));
			if (ordered && blocks.Length > 1) {
				var offset = System.Math.Sin(goog.math.toRadians(Workspace.SCAN_ANGLE));
				if (this.RTL) {
					offset *= -1;
				}
				blocks.Sort((a, b) => {
					var aXY = a.getRelativeToSurfaceXY();
					var bXY = b.getRelativeToSurfaceXY();
					var aa = (aXY.y + offset * aXY.x);
					var bb = (bXY.y + offset * bXY.x);
					if (aa == bb) return 0;
					if (aa > bb) return 1; else return -1;
				});
			}
			return blocks;
		}

		/// <summary>
		/// Find all blocks in workspace.  No particular order.
		/// </summary>
		/// <returns></returns>
		public JsArray<Block> getAllBlocks()
		{
			var blocks = this.getTopBlocks(false);
			for (var i = 0; i < blocks.Length; i++) {
				Array.ForEach(blocks[i].getChildren(), (c) => blocks.Push(c));
			}
			return blocks;
		}

		/// <summary>
		/// Dispose of all blocks in workspace.
		/// </summary>
		public virtual void clear()
		{
			var existingGroup = Events.getGroup();
			if (existingGroup == null) {
				Events.setGroup(true);
			}
			while (this.topBlocks_.Length > 0) {
				this.topBlocks_[0].dispose();
			}
			if (existingGroup == null) {
				Events.setGroup(false);
			}

			this.variableList.Clear();
		}

		/// <summary>
		/// Walk the workspace and update the list of variables to only contain ones in
		/// use on the workspace.  Use when loading new workspaces from disk.
		/// </summary>
		/// <param name="clearList">True if the old variable list should be cleared.</param>
		public void updateVariableList(bool clearList)
		{
			// TODO: Sort
			if (!this.isFlyout) {
				// Update the list in place so that the flyout's references stay correct.
				if (clearList) {
					this.variableList.Clear();
				}
				var allVariables = Core.Variables.allUsedVariables(this);
				for (var i = 0; i < allVariables.Length; i++) {
					this.createVariable(allVariables[i]);
				}
			}
		}

		/// <summary>
		/// Rename a variable by updating its name in the variable list.
		/// </summary>
		/// <param name="oldName">Variable to rename.</param>
		/// <param name="newName">New variable name.</param>
		public void renameVariable(string oldName, string newName)
		{
			// Find the old name in the list.
			var variableIndex = this.variableIndexOf(oldName);
			var newVariableIndex = this.variableIndexOf(newName);
			string oldCase = null;

			// We might be renaming to an existing name but with different case.  If so,
			// we will also update all of the blocks using the new name to have the
			// correct case.
			if (newVariableIndex != -1 &&
				this.variableList[newVariableIndex] != newName) {
				oldCase = this.variableList[newVariableIndex];
			}

			Events.setGroup(true);
			var blocks = this.getAllBlocks();
			// Iterate through every block.
			for (var i = 0; i < blocks.Length; i++) {
				blocks[i].renameVar(oldName, newName);
				if (oldCase != null) {
					blocks[i].renameVar(oldCase, newName);
				}
			}
			Events.setGroup(false);


			if (variableIndex == newVariableIndex ||
				variableIndex != -1 && newVariableIndex == -1) {
				// Only changing case, or renaming to a completely novel name.
				this.variableList[variableIndex] = newName;
			}
			else if (variableIndex != -1 && newVariableIndex != -1) {
				// Renaming one existing variable to another existing variable.
				this.variableList.Splice(variableIndex, 1);
				// The case might have changed.
				this.variableList[newVariableIndex] = newName;
			}
			else {
				this.variableList.Push(newName);
				Console.WriteLine("Tried to rename an non-existent variable.");
			}
		}

		/// <summary>
		/// Create a variable with the given name.
		/// TODO: #468
		/// </summary>
		/// <param name="name">The new variable's name.</param>
		public virtual void createVariable(string name)
		{
			var index = this.variableIndexOf(name);
			if (index == -1) {
				this.variableList.Push(name);
			}
		}

		/// <summary>
		/// Find all the uses of a named variable.
		/// </summary>
		/// <param name="name">Name of variable.</param>
		/// <returns>Array of block usages.</returns>
		public Block[] getVariableUses(string name)
		{
			var uses = new JsArray<Block>();
			var blocks = this.getAllBlocks();
			// Iterate through every block and check the name.
			for (var i = 0; i < blocks.Length; i++) {
				var blockVariables = blocks[i].getVars();
				if (blockVariables != null) {
					for (var j = 0; j < blockVariables.Length; j++) {
						var varName = blockVariables[j];
						// Variable name may be null if the block is only half-built.
						if (varName != null && Core.Names.equals(varName, name)) {
							uses.Push(blocks[i]);
						}
					}
				}
			}
			return uses;
		}

		/// <summary>
		/// Delete a variables and all of its uses from this workspace.
		/// </summary>
		/// <param name="name">Name of variable to delete.</param>
		public void deleteVariable(string name)
		{
			var variableIndex = this.variableIndexOf(name);
			if (variableIndex != -1) {
				var uses = this.getVariableUses(name);
				if (uses.Length > 1) {
					foreach (var block in uses) {
						if (block.type == "procedures_defnoreturn" ||
						  block.type == "procedures_defreturn") {
							var procedureName = block.getFieldValue("NAME");
							Window.Alert(
								Msg.CANNOT_DELETE_VARIABLE_PROCEDURE.Replace("%1", name).
								Replace("%2", procedureName));
							return;
						}
					}
					var ok = Window.Confirm(
						Msg.DELETE_VARIABLE_CONFIRMATION.Replace("%1", uses.Length.ToString()).
						Replace("%2", name));
					if (!ok) {
						return;
					}
				}

				Events.setGroup(true);
				for (var i = 0; i < uses.Length; i++) {
					((BlockSvg)uses[i]).dispose(true, false);
				}
				Events.setGroup(false);
				this.variableList.Splice(variableIndex, 1);
			}
		}

		/// <summary>
		/// Check whether a variable exists with the given name.  The check is
		/// case-insensitive.
		/// </summary>
		/// <param name="name">The name to check for.</param>
		/// <returns>The index of the name in the variable list, or -1 if it is
		/// not present.</returns>
		internal int variableIndexOf(string name)
		{
			int i = 0;
			foreach (var varname in this.variableList) {
				if (Core.Names.equals(varname, name)) {
					return i;
				}
				i++;
			}
			return -1;
		}

		/// <summary>
		/// Returns the horizontal offset of the workspace.
		/// Intended for LTR/RTL compatibility in XML.
		/// Not relevant for a headless workspace.
		/// </summary>
		/// <returns>Width.</returns>
		public virtual double getWidth()
		{
			return 0;
		}

		/// <summary>
		/// Obtain a newly created block.
		/// </summary>
		/// <param name="prototypeName">Name of the language object containing
		/// type-specific functions for this block.</param>
		/// <param name="opt_id">Optional ID.  Use this ID if provided, otherwise
		/// create a new id.</param>
		/// <returns>The created block.</returns>
		public Block newBlock(string prototypeName, string opt_id = null)
		{
			Block block = null;
			if (!String.IsNullOrEmpty(prototypeName)) {
				var prototype = Core.Blocks[prototypeName];
				goog.asserts.assertObject(prototype,
					"Error: \"%s\" is an unknown language block.", prototypeName);
				if (prototype.Is<Type>()) {
					var ctor = prototype.As<Type>().GetConstructor(new Type[] { typeof(Workspace) });
					block = (Block)ctor.Invoke(new object[] { this });
				}
				else {
					block = new RuntimeBlock(this, prototypeName, prototype.As<string>());
				}
			}
			block.postCreate(opt_id);
			return block;
		}

		/// <summary>
		/// The number of blocks that may be added to the workspace before reaching
		/// the maxBlocks.
		/// </summary>
		/// <returns></returns>
		public int remainingCapacity()
		{
			if (Double.IsNaN(this.options.maxBlocks)) {
				return /*Script.Infinity*/Int32.MaxValue;
			}
			return this.options.maxBlocks - this.getAllBlocks().Length;
		}

		/// <summary>
		/// Undo or redo the previous action.
		/// </summary>
		/// <param name="redo">False if undo, true if redo.</param>
		public void undo(bool redo)
		{
			var inputStack = redo ? this.redoStack_ : this.undoStack_;
			var outputStack = redo ? this.undoStack_ : this.redoStack_;
			var inputEvent = inputStack.Pop();
			if (inputEvent == null) {
				return;
			}
			var events = new JsArray<Events.Abstract> { inputEvent };
			// Do another undo/redo if the next one is of the same group.
			while (inputStack.Length != 0 && inputEvent.group != null &&
				inputEvent.group == inputStack[inputStack.Length - 1].group) {
				events.Push(inputStack.Pop());
			}
			// Push these popped events on the opposite stack.
			foreach (var e in events) {
				outputStack.Push(e);
			}
			events = Events.filter(events, redo);
			Events.recordUndo = false;
			foreach (var e in events) {
				e.run(redo);
			}
			Events.recordUndo = true;
		}

		/// <summary>
		/// Clear the undo/redo stacks.
		/// </summary>
		public void clearUndo()
		{
			this.undoStack_.Clear();
			this.redoStack_.Clear();
			// Stop any events already in the firing queue from being undoable.
			Events.clearPendingUndo();
		}

		/// <summary>
		/// When something in this workspace changes, call a function.
		/// </summary>
		/// <param name="func">Function to call.</param>
		/// <returns>Function that can be passed to
		/// removeChangeListener.</returns>
		public Action<Events.Abstract> addChangeListener(Action<Events.Abstract> func)
		{
			this.listeners_.Push(func);
			return func;
		}

		/// <summary>
		/// Stop listening for this workspace's changes.
		/// </summary>
		/// <param name="func">Function to stop calling.</param>
		public void removeChangeListener(Action<Events.Abstract> func)
		{
			this.listeners_.Remove(func);
		}

		/// <summary>
		/// Fire a change ev.
		/// </summary>
		/// <param name="ev">Event to fire.</param>
		public void fireChangeListener(Events.Abstract ev)
		{
			if (ev.recordUndo) {
				this.undoStack_.Push(ev);
				this.redoStack_.Clear();
				while (this.undoStack_.Length > this.MAX_UNDO) {
					this.undoStack_.Shift();
				}
			}
			for (var i = 0; i < this.listeners_.Length; i++) {
				this.listeners_[i](ev);
			}
		}

		/// <summary>
		/// Find the block on this workspace with the specified ID.
		/// </summary>
		/// <param name="id">ID of block to find.</param>
		/// <returns>The sought after block or null if not found.</returns>
		public Block getBlockById(string id)
		{
			this.blockDB_.TryGetValue(id, out var block);
			return block;
		}

		/// <summary>
		/// Find the workspace with the specified ID.
		/// </summary>
		/// <param name="id">ID of workspace to find.</param>
		/// <returns>The sought after workspace or null if not found.</returns>
		public static Workspace getById(string id)
		{
			WorkspaceDB_.TryGetValue(id, out var result);
			return result;
		}
	}
}
