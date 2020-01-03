/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2011 Google Inc.
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
 * @fileoverview The class representing one block.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public class Block
	{
		public string id;
		public RenderedConnection outputConnection;
		public RenderedConnection nextConnection;
		public RenderedConnection previousConnection;
		public JsArray<Input> inputList = new JsArray<Input>();
		public bool? inputsInline;
		public bool disabled;
		public Union<string, Func<string>> tooltip = "";
		public bool contextMenu = true;

		protected Block parentBlock_;
		protected JsArray<Block> childBlocks_ = new JsArray<Block>();
		private bool deletable_ = true;
		private bool movable_ = true;
		private bool editable_ = true;
		private bool isShadow_;
		protected bool collapsed_;
		private string comment_;
		private goog.math.Coordinate xy_ = new goog.math.Coordinate(0, 0);
		public Workspace workspace;
		public bool isInFlyout;
		public bool isInMutator;
		public bool RTL;
		public readonly string type;
		public bool? inputsInlineDefault;
		private Action<Events.Abstract> onchangeWrapper_;

		protected Block[] prevBlocks_;
		internal string contextMenuMsg_;

		/// <summary>
		/// Class for one block.
		/// Not normally called directly, workspace.newBlock() is preferred.
		/// </summary>
		/// <param name="workspace">The block's workspace.</param>
		/// <param name="prototypeName">Name of the language object containing
		/// type-specific functions for this block.</param>
		public Block(Workspace workspace, string prototypeName)
		{
			this.workspace = workspace;
			this.isInFlyout = workspace.isInFlyout;
			this.isInMutator = workspace.isMutator;
			this.RTL = workspace.RTL;
			this.type = prototypeName;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="opt_id">Optional ID.  Use this ID if provided, otherwise
		/// create a new id.</param>
		internal void postCreate(string opt_id = null)
		{
			this.id = ((opt_id != null) && workspace.getBlockById(opt_id) == null) ?
				opt_id : Core.genUid();
			workspace.blockDB_[this.id] = this;

			workspace.addTopBlock(this);

			// Call an initialization function, if it exists.
			if (true/*goog.isFunction(this.init)*/) {
				this.init();
			}
			// Record initial inline state.
			/** @type {boolean|undefined} */
			this.inputsInlineDefault = this.inputsInline;
			if (Events.isEnabled()) {
				Events.fire(new Events.Create(this));
			}
			// Bind an onchange function, if it exists.
			if (true/*goog.isFunction(this.onchange)*/) {
				this.onchangeWrapper_ = new Action<Events.Abstract>((ev) => this.onchange(ev));
				this.workspace.addChangeListener(this.onchangeWrapper_);
			}
		}

		public virtual void init()
		{
		}

		protected virtual void onchange(Events.Abstract ev)
		{
		}

		/// <summary>
		/// Obtain a newly created block.
		/// </summary>
		/// <param name="workspace">The block's workspace.</param>
		/// <param name="prototypeName">Name of the language object containing
		/// type-specific functions for this block.</param>
		/// <returns>The created block.</returns>
		public static Block obtain(Workspace workspace, string prototypeName)
		{
			Console.WriteLine("Deprecated call to Blockly.Block.obtain, " +
						"use workspace.newBlock instead.");
			return workspace.newBlock(prototypeName);
		}

		/// <summary>
		/// Optional text data that round-trips beween blocks and XML.
		/// Has no effect. May be used by 3rd parties for meta information.
		/// </summary>
		public string data;

		/// <summary>
		/// Colour of the block in '#RRGGBB' format.
		/// </summary>
		private string colour_ = "#000000";

		/// <summary>
		/// Dispose of this block.
		/// </summary>
		/// <param name="healStack">If true, then try to heal any gap by connecting
		/// the next statement with the previous statement.  Otherwise, dispose of
		/// all children of this block.</param>
		public virtual void dispose(bool healStack = false, bool animate = false)
		{
			if (this.workspace == null) {
				// Already deleted.
				return;
			}
			// Terminate onchange event calls.
			if (this.onchangeWrapper_ != null) {
				this.workspace.removeChangeListener(this.onchangeWrapper_);
			}
			this.unplug(healStack);
			if (Events.isEnabled()) {
				Events.fire(new Events.Delete(this));
			}
			Events.disable();

			try {
				// This block is now at the top of the workspace.
				// Remove this block from the workspace's list of top-most blocks.
				if (this.workspace != null) {
					this.workspace.removeTopBlock(this);
					// Remove from block database.
					this.workspace.blockDB_.Remove(this.id);
					//Script.Delete(this.workspace.blockDB_[this.id]);
					this.workspace = null;
				}

				// Just deleting this block from the DOM would result in a memory leak as
				// well as corruption of the connection database.  Therefore we must
				// methodically step through the blocks and carefully disassemble them.

				// First, dispose of all my children.
				for (var i = this.childBlocks_.Length - 1; i >= 0; i--) {
					this.childBlocks_[i].dispose(false);
				}
				// Then dispose of myself.
				// Dispose of all inputs and their fields.
				foreach (var input in this.inputList) {
					input.dispose();
				}
				this.inputList.Clear();
				// Dispose of any remaining connections (next/previous/output).
				var connections = this.getConnections_(true);
				for (var i = 0; i < connections.Length; i++) {
					var connection = connections[i];
					if (connection.isConnected()) {
						connection.disconnect();
					}
					connections[i].dispose();
				}
			}
			finally {
				Events.enable();
			}
		}

		/// <summary>
		/// Unplug this block from its superior block.  If this block is a statement,
		/// optionally reconnect the block underneath with the block on top.
		/// </summary>
		/// <param name="opt_healStack">opt_healStack Disconnect child statement and reconnect
		/// stack.  Defaults to false.</param>
		public void unplug(bool opt_healStack = false)
		{
			if (this.outputConnection != null) {
				if (this.outputConnection.isConnected()) {
					// Disconnect from any superior block.
					this.outputConnection.disconnect();
				}
			}
			else if (this.previousConnection != null) {
				Connection previousTarget = null;
				if (this.previousConnection.isConnected()) {
					// Remember the connection that any next statements need to connect to.
					previousTarget = this.previousConnection.targetConnection;
					// Detach this block from the parent's tree.
					this.previousConnection.disconnect();
				}
				Block nextBlock = this.getNextBlock();
				if (opt_healStack && nextBlock != null) {
					// Disconnect the next statement.
					var nextTarget = this.nextConnection.targetConnection;
					nextTarget.disconnect();
					if (previousTarget != null && previousTarget.checkType_(nextTarget)) {
						// Attach the next statement to the previous statement.
						previousTarget.connect(nextTarget);
					}
				}
			}
		}

		/// <summary>
		/// Returns all connections originating from this block.
		/// </summary>
		/// <returns>Array of connections.</returns>
		internal virtual JsArray<RenderedConnection> getConnections_(bool all)
		{
			var myConnections = new JsArray<RenderedConnection>();
			if (this.outputConnection != null) {
				myConnections.Push(this.outputConnection);
			}
			if (this.previousConnection != null) {
				myConnections.Push(this.previousConnection);
			}
			if (this.nextConnection != null) {
				myConnections.Push(this.nextConnection);
			}
			foreach (var input in this.inputList) {
				if (input.connection != null) {
					myConnections.Push((RenderedConnection)input.connection);
				}
			}
			return myConnections;
		}

		/// <summary>
		/// Walks down a stack of blocks and finds the last next connection on the stack.
		/// </summary>
		/// <returns>The last next connection on the stack, or null.</returns>
		protected Connection lastConnectionInStack_()
		{
			var nextConnection = this.nextConnection;
			while (nextConnection != null) {
				var nextBlock = nextConnection.targetBlock();
				if (nextBlock == null) {
					// Found a next connection with nothing on the other side.
					return nextConnection;
				}
				nextConnection = nextBlock.nextConnection;
			}
			// Ran out of next connections.
			return null;
		}

		/// <summary>
		/// Bump unconnected blocks out of alignment.  Two blocks which aren't actually
		/// connected should not coincidentally line up on screen.
		/// </summary>
		internal void bumpNeighbours_()
		{
			if (this.workspace == null) {
				return;  // Deleted block.
			}
			if (Core.dragMode_ != Core.DRAG_NONE) {
				return;  // Don't bump blocks during a drag.
			}
			var rootBlock = this.getRootBlock();
			if (rootBlock.isInFlyout) {
				return;  // Don't move blocks around in a flyout.
			}
			// Loop though every connection on this block.
			var myConnections = this.getConnections_(false);
			foreach (var connection in myConnections) {
				// Spider down from this block bumping all sub-blocks.
				if (connection.isConnected() && connection.isSuperior()) {
					connection.targetBlock().bumpNeighbours_();
				}

				var neighbours = connection.neighbours_(Core.SNAP_RADIUS);
				foreach (RenderedConnection otherConnection in neighbours) {
					// If both connections are connected, that's probably fine.  But if
					// either one of them is unconnected, then there could be confusion.
					if (!connection.isConnected() || !otherConnection.isConnected()) {
						// Only bump blocks if they are from different tree structures.
						if (otherConnection.getSourceBlock().getRootBlock() != rootBlock) {
							// Always bump the inferior block.
							if (connection.isSuperior()) {
								otherConnection.bumpAwayFrom_(connection);
							}
							else {
								connection.bumpAwayFrom_(otherConnection);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Return the parent block or null if this block is at the top level.
		/// </summary>
		/// <returns>The block that holds the current block.</returns>
		public Block getParent()
		{
			// Look at the DOM to see if we are nested in another block.
			return this.parentBlock_;
		}

		/// <summary>
		/// Return the input that connects to the specified block.
		/// </summary>
		/// <param name="block">A block connected to an input on this block.</param>
		/// <returns>The input that connects to the specified block.</returns>
		public Input getInputWithBlock(Block block)
		{
			foreach (var input in this.inputList) {
				if (input.connection?.targetBlock() == block) {
					return input;
				}
			}
			return null;
		}

		/// <summary>
		/// Return the parent block that surrounds the current block, or null if this
		/// block has no surrounding block.  A parent block might just be the previous
		/// statement, whereas the surrounding block is an if statement, while loop, etc.
		/// </summary>
		/// <returns>The block that surrounds the current block.</returns>
		public Block getSurroundParent()
		{
			var block = this;
			Block prevBlock;
			do {
				prevBlock = block;
				block = block.getParent();
				if (block == null) {
					// Ran off the top.
					return null;
				}
			} while (block.getNextBlock() == prevBlock);
			// This block is an enclosing parent, not just a statement in a stack.
			return block;
		}

		/// <summary>
		/// Return the next statement block directly connected to this block.
		/// </summary>
		/// <returns>The next statement block or null.</returns>
		public Block getNextBlock()
		{
			return this.nextConnection?.targetBlock();
		}

		/// <summary>
		/// Return the top-most block in this block's tree.
		/// This will return itself if this block is at the top level.
		/// </summary>
		/// <returns>The root block.</returns>
		public Block getRootBlock()
		{
			Block rootBlock;
			var block = this;
			do {
				rootBlock = block;
				block = rootBlock.parentBlock_;
			} while (block != null);
			return rootBlock;
		}

		/// <summary>
		/// Find all the blocks that are directly nested inside this one.
		/// Includes value and block inputs, as well as any following statement.
		/// Excludes any connection on an output tab or any preceding statement.
		/// </summary>
		/// <returns>Array of blocks.</returns>
		public Block[] getChildren()
		{
			return this.childBlocks_;
		}

		/// <summary>
		/// Set parent of this block to be a new block or null.
		/// </summary>
		/// <param name="newParent">newParent New parent block.</param>
		public virtual void setParent(Block newParent)
		{
			if (newParent == this.parentBlock_) {
				return;
			}
			if (this.parentBlock_ != null) {
				// Remove this block from the old parent's child list.
				this.parentBlock_.childBlocks_.Remove(this);

				// Disconnect from superior blocks.
				if (this.previousConnection != null && this.previousConnection.isConnected()) {
					throw new Exception("Still connected to previous block.");
				}
				if (this.outputConnection != null && this.outputConnection.isConnected()) {
					throw new Exception("Still connected to parent block.");
				}
				this.parentBlock_ = null;
				// This block hasn't actually moved on-screen, so there's no need to update
				// its connection locations.
			}
			else {
				// Remove this block from the workspace's list of top-most blocks.
				this.workspace.removeTopBlock(this);
			}

			this.parentBlock_ = newParent;
			if (newParent != null) {
				// Add this block to the new parent's child list.
				newParent.childBlocks_.Push(this);
			}
			else {
				this.workspace.addTopBlock(this);
			}
		}

		/// <summary>
		/// Find all the blocks that are directly or indirectly nested inside this one.
		/// Includes this block in the list.
		/// Includes value and block inputs, as well as any following statements.
		/// Excludes any connection on an output tab or any preceding statements.
		/// </summary>
		/// <returns>Flattened array of blocks.</returns>
		public Block[] getDescendants()
		{
			var blocks = new JsArray<Block> { this };
			foreach (var child in this.childBlocks_) {
				Array.ForEach(child.getDescendants(), (i) => blocks.Push(i));
			}
			return blocks;
		}

		/// <summary>
		/// Get whether this block is deletable or not.
		/// </summary>
		/// <returns>True if deletable.</returns>
		public bool isDeletable()
		{
			return this.deletable_ && !this.isShadow_ &&
				!(this.workspace != null && this.workspace.options.readOnly);
		}

		/// <summary>
		/// Set whether this block is deletable or not.
		/// </summary>
		/// <param name="deletable">True if deletable.</param>
		public void setDeletable(bool deletable)
		{
			this.deletable_ = deletable;
		}

		/// <summary>
		/// Get whether this block is movable or not.
		/// </summary>
		/// <returns>True if movable.</returns>
		public bool isMovable()
		{
			return this.movable_ && !this.isShadow_ &&
				!(this.workspace != null && this.workspace.options.readOnly);
		}

		/// <summary>
		/// Set whether this block is movable or not.
		/// </summary>
		/// <param name="movable">True if movable.</param>
		public virtual void setMovable(bool movable)
		{
			this.movable_ = movable;
		}

		/// <summary>
		/// Get whether this block is a shadow block or not.
		/// </summary>
		/// <returns>True if a shadow.</returns>
		public bool isShadow()
		{
			return this.isShadow_;
		}

		/// <summary>
		/// Set whether this block is a shadow block or not.
		/// </summary>
		/// <param name="shadow">True if a shadow.</param>
		public virtual void setShadow(bool shadow)
		{
			this.isShadow_ = shadow;
		}

		/// <summary>
		/// Get whether this block is editable or not.
		/// </summary>
		/// <returns>True if editable.</returns>
		public bool isEditable()
		{
			return this.editable_ && !(this.workspace != null && this.workspace.options.readOnly);
		}

		/// <summary>
		/// Set whether this block is editable or not.
		/// </summary>
		/// <param name="editable">True if editable.</param>
		public virtual void setEditable(bool editable)
		{
			this.editable_ = editable;
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					field.updateEditable();
				}
			}
		}

		/// <summary>
		/// Set whether the connections are hidden (not tracked in a database) or not.
		/// Recursively walk down all child blocks (except collapsed blocks).
		/// </summary>
		/// <param name="hidden">True if connections are hidden.</param>
		public void setConnectionsHidden(bool hidden)
		{
			if (!hidden && this.isCollapsed()) {
				if (this.outputConnection != null) {
					this.outputConnection.setHidden(hidden);
				}
				if (this.previousConnection != null) {
					this.previousConnection.setHidden(hidden);
				}
				if (this.nextConnection != null) {
					this.nextConnection.setHidden(hidden);
					var child = this.nextConnection.targetBlock();
					if (child != null) {
						child.setConnectionsHidden(hidden);
					}
				}
			}
			else {
				var myConnections = this.getConnections_(true);
				foreach (var connection in myConnections) {
					connection.setHidden(hidden);
					if (connection.isSuperior()) {
						var child = connection.targetBlock();
						if (child != null) {
							child.setConnectionsHidden(hidden);
						}
					}
				}
			}
		}

		protected Union<string, Func<string>> helpUrl;

		/// <summary>
		/// Set the URL of this block's help page.
		/// </summary>
		/// <param name="url">URL string for block help, or function that
		/// returns a URL.  Null for no help.</param>
		public void setHelpUrl(Union<string, Func<string>> url)
		{
			this.helpUrl = url;
		}

		/// <summary>
		/// Change the tooltip text for a block.
		/// </summary>
		/// <param name="newTip">newTip Text for tooltip or a parent element to
		/// link to for its tooltip.  May be a function that returns a string.</param>
		public void setTooltip(Union<string, Func<string>> newTip)
		{
			this.tooltip = newTip;
		}

		/// <summary>
		/// Get the colour of a block.
		/// </summary>
		/// <returns>#RRGGBB string.</returns>
		public string getColour()
		{
			return this.colour_;
		}

		/// <summary>
		/// Change the colour of a block.
		/// </summary>
		/// <param name="colour">HSV hue value, or #RRGGBB string.</param>
		public virtual void setColour(Union<int, string> _colour)
		{
			string colour = _colour.ToString();
			int cnt;
			var hue = Script.ParseFloat(colour);
			if (!Double.IsNaN(hue)) {
				this.colour_ = Core.hueToRgb(hue);
			}
			else if (!String.IsNullOrEmpty(colour) && (cnt = colour.Match(new Regex(@"^#[0-9a-fA-F]+$")).Length) > 0 && (cnt <= 6)) {
				// @"^#[0-9a-fA-F]{6}$"
				this.colour_ = colour;
			}
			else {
				throw new Exception("Invalid colour: " + colour);
			}
		}

		/// <summary>
		/// Returns the named field from a block.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <returns>Named field, or null if field does not exist.</returns>
		public Field getField(string name)
		{
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					if (field.name == name) {
						return field;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Return all variables referenced by this block.
		/// </summary>
		/// <returns>List of variable names.</returns>
		public virtual string[] getVars()
		{
			var vars = new JsArray<string>();
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					if (field is FieldVariable) {
						vars.Push(field.getValue());
					}
				}
			}
			return vars;
		}

		/// <summary>
		/// Notification that a variable is renaming.
		/// If the name matches one of this block's variables, rename it.
		/// </summary>
		/// <param name="oldName">Previous name of variable.</param>
		/// <param name="newName">Renamed variable.</param>
		public virtual void renameVar(string oldName, string newName)
		{
			foreach (var input in this.inputList) {
				foreach (var field in input.fieldRow) {
					if (field is FieldVariable &&
						Core.Names.equals(oldName, field.getValue())) {
						field.setValue(newName);
					}
				}
			}
		}

		/// <summary>
		/// Returns the language-neutral value from the field of a block.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <returns>Value from the field or null if field does not exist.</returns>
		public string getFieldValue(string name)
		{
			var field = this.getField(name);
			if (field != null) {
				return field.getValue();
			}
			return null;
		}

		/// <summary>
		/// Returns the language-neutral value from the field of a block.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <returns>Value from the field or null if field does not exist.</returns>
		public string getTitleValue(string name)
		{
			Console.WriteLine("Deprecated call to getTitleValue, use getFieldValue instead.");
			return this.getFieldValue(name);
		}

		/// <summary>
		/// Change the field value for a block (e.g. 'CHOOSE' or 'REMOVE').
		/// </summary>
		/// <param name="newValue">Value to be the new field.</param>
		/// <param name="name">The name of the field.</param>
		public void setFieldValue(string newValue, string name)
		{
			var field = this.getField(name);
			goog.asserts.assertObject(field, "Field \"%s\" not found.", name);
			field.setValue(newValue);
		}

		/// <summary>
		/// Change the field value for a block (e.g. 'CHOOSE' or 'REMOVE').
		/// </summary>
		/// <param name="newValue">Value to be the new field.</param>
		/// <param name="name">The name of the field.</param>
		public void setTitleValue(string newValue, string name)
		{
			Console.WriteLine("Deprecated call to setTitleValue, use setFieldValue instead.");
			this.setFieldValue(newValue, name);
		}

		/// <summary>
		/// Set whether this block can chain onto the bottom of another block.
		/// </summary>
		/// <param name="newBoolean">True if there can be a previous statement.</param>
		/// <param name="opt_check">Statement type or
		/// list of statement types.  Null/undefined if any type could be connected.</param>
		public virtual void setPreviousStatement(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			if (newBoolean) {
				if (this.previousConnection == null) {
					goog.asserts.assert(this.outputConnection == null,
						"Remove output connection prior to adding previous connection.");
					this.previousConnection =
						this.makeConnection_(Core.PREVIOUS_STATEMENT);
				}
				this.previousConnection.setCheck(opt_check);
			}
			else {
				if (this.previousConnection != null) {
					goog.asserts.assert(!this.previousConnection.isConnected(),
						"Must disconnect previous statement before removing connection.");
					this.previousConnection.dispose();
					this.previousConnection = null;
				}
			}
		}

		/// <summary>
		/// Set whether another block can chain onto the bottom of this block.
		/// </summary>
		/// <param name="newBoolean">True if there can be a next statement.</param>
		/// <param name="opt_check">Statement type or
		/// list of statement types.  Null/undefined if any type could be connected.</param>
		public virtual void setNextStatement(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			if (newBoolean) {
				if (this.nextConnection == null) {
					this.nextConnection = this.makeConnection_(Core.NEXT_STATEMENT);
				}
				this.nextConnection.setCheck(opt_check);
			}
			else {
				if (this.nextConnection != null) {
					goog.asserts.assert(!this.nextConnection.isConnected(),
						"Must disconnect next statement before removing connection.");
					this.nextConnection.dispose();
					this.nextConnection = null;
				}
			}
		}

		/// <summary>
		/// Set whether this block returns a value.
		/// </summary>
		/// <param name="newBoolean">True if there is an output.</param>
		/// <param name="opt_check">Returned type or list
		/// of returned types.  Null or undefined if any type could be returned
		/// (e.g. variable get).</param>
		public virtual void setOutput(bool newBoolean, Union<string, string[]> opt_check = null)
		{
			if (newBoolean) {
				if (this.outputConnection == null) {
					goog.asserts.assert(this.previousConnection == null,
						"Remove previous connection prior to adding output connection.");
					this.outputConnection = this.makeConnection_(Core.OUTPUT_VALUE);
				}
				this.outputConnection.setCheck(opt_check);
			}
			else {
				if (this.outputConnection != null) {
					goog.asserts.assert(!this.outputConnection.isConnected(),
						"Must disconnect output value before removing connection.");
					this.outputConnection.dispose();
					this.outputConnection = null;
				}
			}
		}

		/// <summary>
		/// Set whether value inputs are arranged horizontally or vertically.
		/// </summary>
		/// <param name="newBoolean">True if inputs are horizontal.</param>
		public virtual void setInputsInline(bool newBoolean)
		{
			if (this.inputsInline != newBoolean) {
				Events.fire(new Events.Change(
					this, "inline", null, this.inputsInline.ToString(), newBoolean.ToString()));
				this.inputsInline = newBoolean;
			}
		}

		/// <summary>
		/// Get whether value inputs are arranged horizontally or vertically.
		/// </summary>
		/// <returns>True if inputs are horizontal.</returns>
		public bool getInputsInline()
		{
			if (this.inputsInline.HasValue) {
				// Set explicitly.
				return this.inputsInline.Value;
			}
			// Not defined explicitly.  Figure out what would look best.
			for (var i = 1; i < this.inputList.Length; i++) {
				if (this.inputList[i - 1].type == Core.DUMMY_INPUT &&
					this.inputList[i].type == Core.DUMMY_INPUT) {
					// Two dummy inputs in a row.  Don't inline them.
					return false;
				}
			}
			for (var i = 1; i < this.inputList.Length; i++) {
				if (this.inputList[i - 1].type == Core.INPUT_VALUE &&
					this.inputList[i].type == Core.DUMMY_INPUT) {
					// Dummy input after a value input.  Inline them.
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Set whether the block is disabled or not.
		/// </summary>
		/// <param name="disabled">True if disabled.</param>
		public virtual void setDisabled(bool disabled)
		{
			if (this.disabled != disabled) {
				Events.fire(new Events.Change(
					this, "disabled", null, this.disabled.ToString(), disabled.ToString()));
				this.disabled = disabled;
			}
		}

		/// <summary>
		/// Get whether the block is disabled or not due to parents.
		/// The block's own disabled property is not considered.
		/// </summary>
		/// <returns>True if disabled.</returns>
		public bool getInheritedDisabled()
		{
			var block = this;
			while (true) {
				block = block.getSurroundParent();
				if (block == null) {
					// Ran off the top.
					return false;
				}
				else if (block.disabled) {
					return true;
				}
			}
		}

		/// <summary>
		/// Get whether the block is collapsed or not.
		/// </summary>
		/// <returns>True if collapsed.</returns>
		public bool isCollapsed()
		{
			return this.collapsed_;
		}

		/// <summary>
		/// Set whether the block is collapsed or not.
		/// </summary>
		/// <param name="collapsed">True if collapsed.</param>
		public virtual void setCollapsed(bool collapsed)
		{
			if (this.collapsed_ != collapsed) {
				Events.fire(new Events.Change(
					this, "collapsed", null, this.collapsed_.ToString(), collapsed.ToString()));
				this.collapsed_ = collapsed;
			}
		}

		/// <summary>
		/// Create a human-readable text representation of this block and any children.
		/// </summary>
		/// <param name="opt_maxLength">Truncate the string to this length.</param>
		/// <returns>Text of block.</returns>
		public string toString(int opt_maxLength = 0, string opt_emptyToken = null)
		{
			var text = new JsArray<string>();
			var emptyFieldPlaceholder = opt_emptyToken ?? "?";
			if (this.collapsed_) {
				text.Push(this.getInput("_TEMP_COLLAPSED_INPUT").fieldRow[0].text_);
			}
			else {
				foreach (var input in this.inputList) {
					foreach (var field in input.fieldRow) {
						text.Push(field.getText());
					}
					if (input.connection != null) {
						var child = input.connection.targetBlock();
						if (child != null) {
							text.Push(child.toString(0, opt_emptyToken));
						}
						else {
							text.Push(emptyFieldPlaceholder);
						}
					}
				}
			}
			var str = text.Join(" ").Trim() ?? "???";
			if (opt_maxLength != 0) {
				// TODO: Improve truncation so that text from this block is given priority.
				// E.g. "1+2+3+4+5+6+7+8+9=0" should be "...6+7+8+9=0", not "1+2+3+4+5...".
				// E.g. "1+2+3+4+5=6+7+8+9+0" should be "...4+5=6+7...".
				if (str.Length > opt_maxLength)
					str = str.Substring(0, opt_maxLength - 3) + "...";
			}
			return str;
		}

		/// <summary>
		/// Shortcut for appending a value input row.
		/// </summary>
		/// <param name="name">Language-neutral identifier which may used to find this
		/// input again.  Should be unique to this block.</param>
		/// <returns>The input object created.</returns>
		public Input appendValueInput(string name)
		{
			return this.appendInput_(Core.INPUT_VALUE, name);
		}

		/// <summary>
		/// Shortcut for appending a statement input row.
		/// </summary>
		/// <param name="name">Language-neutral identifier which may used to find this
		/// input again.  Should be unique to this block.</param>
		/// <returns>The input object created.</returns>
		public Input appendStatementInput(string name)
		{
			return this.appendInput_(Core.NEXT_STATEMENT, name);
		}

		/// <summary>
		/// Shortcut for appending a dummy input row.
		/// </summary>
		/// <param name="opt_name">Language-neutral identifier which may used to find
		/// this input again.  Should be unique to this block.</param>
		/// <returns>The input object created.</returns>
		public Input appendDummyInput(string opt_name = null)
		{
			return this.appendInput_(Core.DUMMY_INPUT, opt_name ?? "");
		}

		/// <summary>
		/// Initialize this block using a cross-platform, internationalization-friendly
		/// JSON description.
		/// </summary>
		/// <param name="json">Structured data describing the block.</param>
		public void jsonInit(Dictionary<string, object> json)
		{
			// Validate inputs.
			goog.asserts.assert(!json.ContainsKey("output") ||
				!json.ContainsKey("previousStatement"),
				"Must not have both an output and a previousStatement.");

			// Set basic properties of block.
			if (json.TryGetValue("colour", out var colour)) {
				this.setColour(json["colour"].ToString());
			}

			// Interpolate the message blocks.
			var i = 0;
			while (json.ContainsKey("message" + i)) {
				this.interpolate_((string)json["message" + i], json.ContainsKey("args" + i) ? getArray(json["args" + i]) : new object[0],
					json.ContainsKey("lastDummyAlign" + i) ? json["lastDummyAlign" + i].ToString() : null);
				i++;
			}

			if (json.TryGetValue("inputsInline", out var inputsInline)) {
				this.setInputsInline((bool)json["inputsInline"]);
			}
			// Set output and previous/next connections.
			if (json.TryGetValue("output", out var output)) {
				this.setOutput(true, new Union<string, string[]>(json["output"]));
			}
			if (json.TryGetValue("previousStatement", out var previousStatement)) {
				this.setPreviousStatement(true, new Union<string, string[]>(json["previousStatement"]));
			}
			if (json.TryGetValue("nextStatement", out var nextStatement)) {
				this.setNextStatement(true, new Union<string, string[]>(json["nextStatement"]));
			}
			if (json.TryGetValue("tooltip", out var tooltip)) {
				this.setTooltip(new Union<string, Func<string>>(json["tooltip"]));
			}
			if (json.TryGetValue("helpUrl", out var helpUrl)) {
				this.setHelpUrl(new Union<string, Func<string>>(json["helpUrl"].ToString()));
			}
		}

		private object[] getArray(object args)
		{
			if (args is object[] array)
				return array;
			if (args is List<object> list)
				return list.ToArray();
			var enumer = args as System.Collections.IEnumerable;
			if (enumer == null)
				throw new Exception();
			list = new List<object>();
			foreach (var i in enumer) {
				list.Add(i);
			}
			return list.ToArray();
		}

		/// <summary>
		/// Interpolate a message description onto the block.
		/// </summary>
		/// <param name="message">Text contains interpolation tokens (%1, %2, ...)
		/// that match with fields or inputs defined in the args array.</param>
		/// <param name="args">Array of arguments to be interpolated.</param>
		/// <param name="lastDummyAlign">If a dummy input is added at the end,
		/// how should it be aligned?</param>
		private void interpolate_(string message, object[] args, string lastDummyAlign)
		{
			var tokens = Core.utils.tokenizeInterpolation(message);
			// Interpolate the arguments.  Build a list of elements.
			var indexDup = new HashSet<int>();
			var indexCount = 0;
			var elements = new JsArray<object>();
			for (var i = 0; i < tokens.Length; i++) {
				var _token = tokens[i];
				if (_token.Is<int>()) {
					int token = _token.As<int>();
					goog.asserts.assert((double)token > 0 && token <= args.Length,
						"Message index \"%s\" out of range.", token.ToString());
					goog.asserts.assert(!indexDup.Contains(token),
						"Message index \"%s\" duplicated.", token.ToString());
					indexDup.Add(token);
					indexCount++;
					elements.Push(args[token - 1]);
				}
				else {
					var token = _token.As<string>();
					token = token.Trim(' ', '\t', '\r', '\n', '\0');
					if (!String.IsNullOrEmpty(token)) {
						elements.Push(token);
					}
				}
			}
			goog.asserts.assert(indexCount == args.Length,
				"Message does not reference all %s arg(s).", args.Length.ToString());
			// Add last dummy input if needed.
			if (elements.Length > 0 && (elements[elements.Length - 1] is string ||
				((string)((Dictionary<string, object>)elements[elements.Length - 1])["type"]).StartsWith("field_"))) {
				var dummyInput = new Dictionary<string, object> { { "type", "input_dummy" } };
				if (!String.IsNullOrEmpty(lastDummyAlign)) {
					dummyInput["align"] = lastDummyAlign;
				}
				elements.Push(dummyInput);
			}
			// Lookup of alignment constants.
			var alignmentLookup = new Dictionary<string, object>() {
				{ "LEFT", Core.ALIGN_LEFT },
				{ "RIGHT", Core.ALIGN_RIGHT },
				{ "CENTRE", Core.ALIGN_CENTRE }
			};
			// Populate block with inputs and fields.
			var fieldStack = new JsArray<Tuple<Union<string, Field>, string>>();
			foreach (var i in elements) {
				if (i is string) {
					var element = (string)i;
					fieldStack.Push(new Tuple<Union<string, Field>, string>(element, null));
				}
				else {
					var el = i;
					Field field = null;
					Input input = null;
					bool altRepeat;
					do {
						altRepeat = false;
						if (el is string) {
							field = new FieldLabel((string)el);
						}
						else {
							var element = (Dictionary<string, object>)el;
							switch (element.TryGetValue("type", out var type) ? (string)type : null) {
							case "input_value": {
								input = this.appendValueInput(element.TryGetValue("name", out var name) ? (string)name : null);
								break;
							}
							case "input_statement": {
								input = this.appendStatementInput(element.TryGetValue("name", out var name) ? (string)name : null);
								break;
							}
							case "input_dummy": {
								input = this.appendDummyInput(element.TryGetValue("name", out var name) ? (string)name : null);
								break;
							}
							case "field_label": {
								field = new FieldLabel(element.TryGetValue("text", out var text) ? (string)text : null, (string)element["class"]);
								break;
							}
							case "field_input": {
								field = new FieldTextInput(element.TryGetValue("text", out var text) ? (string)text : null);
								if (element.TryGetValue("spellcheck", out var spellcheck) && spellcheck is bool) {
									((FieldTextInput)field).setSpellcheck((bool)spellcheck);
								}
								break;
							}
							case "field_angle": {
								field = new FieldAngle(element.TryGetValue("angle", out var angle) ? angle.ToString() : null);
								break;
							}
							case "field_checkbox": {
								field = new FieldCheckbox((element.TryGetValue("checked", out var bchecked) ? (bool)bchecked : false) ? "TRUE" : "FALSE");
								break;
							}
							case "field_colour": {
								field = new FieldColour(element.TryGetValue("colour", out var colour) ? (string)colour : null);
								break;
							}
							case "field_variable": {
								field = new FieldVariable(element.TryGetValue("variable", out var variable) ? (string)variable : null);
								break;
							}
							case "field_dropdown": {
								field = new FieldDropdown(element.TryGetValue("options", out var options) ? getDropdownItem(options) : null);
								break;
							}
							case "field_image": {
								field = new FieldImage(element.TryGetValue("src", out var src) ? (string)src : null,
									element.TryGetValue("width", out var width) ? Convert.ToDouble(width) : Double.NaN,
									element.TryGetValue("height", out var height) ? Convert.ToDouble(height) : Double.NaN,
									(string)element["alt"]);
								break;
							}
							case "field_number": {
								field = new FieldNumber(
									element.TryGetValue("value", out var value) ? Convert.ToDouble(value).ToString() : "",
									element.TryGetValue("min", out var min) ? Convert.ToDouble(min) : Double.NaN,
									element.TryGetValue("max", out var max) ? Convert.ToDouble(max) : Double.NaN,
									element.TryGetValue("precision", out var precision) ? Convert.ToDouble(precision) : Double.NaN);
								break;
							}
							case "field_date": {
								field = new FieldDate(element.TryGetValue("date", out var date) ? date.ToString() : null);
								break;
							}
							// Fall through if FieldDate is not compiled in.
							default:
								// Unknown field.
								if (element.ContainsKey("alt")) {
									el = element["alt"];
									altRepeat = true;
								}
								break;
							}
						}
					} while (altRepeat);
					if (field != null) {
						var element = (Dictionary<string, object>)el;
						fieldStack.Push(new Tuple<Union<string, Field>, string>(field, element.TryGetValue("name", out var name) ? (string)name : null));
					}
					else if (input != null) {
						var element = (Dictionary<string, object>)el;
						if (element.TryGetValue("check", out var check)) {
							input.setCheck(new Union<string, string[]>(check));
						}
						if (element.TryGetValue("align", out var align)) {
							input.setAlign((int)alignmentLookup[(string)align]);
						}
						for (var j = 0; j < fieldStack.Length; j++) {
							input.appendField(fieldStack[j].Item1, fieldStack[j].Item2);
						}
						fieldStack.Clear();
					}
				}
			}
		}

		private JsArray<DropdownItemInfo> getDropdownItem(object options_)
		{
			if (options_ is object[] options) {
				var result = new JsArray<DropdownItemInfo>();
				foreach (Dictionary<string, object> item in options) {
					result.Add(new DropdownItemInfo((string)item["text"], (string)item["value"]));
				}
				return result;
			}
			return (JsArray<DropdownItemInfo>)options_;
		}

		/// <summary>
		/// Add a value input, statement input or local variable to this block.
		/// </summary>
		/// <param name="type">Either Blockly.INPUT_VALUE or Blockly.NEXT_STATEMENT or
		/// Blockly.DUMMY_INPUT.</param>
		/// <param name="name">Language-neutral identifier which may used to find this
		/// input again.  Should be unique to this block.</param>
		/// <returns>The input object created.</returns>
		protected virtual Input appendInput_(int type, string name)
		{
			Connection connection = null;
			if (type == Core.INPUT_VALUE || type == Core.NEXT_STATEMENT) {
				connection = this.makeConnection_(type);
			}
			var input = new Input(type, name, this, connection);
			// Append input to list.
			this.inputList.Push(input);
			return input;
		}

		/// <summary>
		/// Move a named input to a different location on this block.
		/// </summary>
		/// <param name="name">The name of the input to move.</param>
		/// <param name="refName">Name of input that should be after the moved input,
		/// or null to be the input at the end.</param>
		public void moveInputBefore(string name, string refName)
		{
			if (name == refName) {
				return;
			}
			// Find both inputs.
			var inputIndex = -1;
			var refIndex = !String.IsNullOrEmpty(refName) ? -1 : this.inputList.Length;
			int i = 0;
			foreach (var input in this.inputList) {
				if (input.name == name) {
					inputIndex = i;
					if (refIndex != -1) {
						break;
					}
				}
				else if (!String.IsNullOrEmpty(refName) && input.name == refName) {
					refIndex = i;
					if (inputIndex != -1) {
						break;
					}
				}
				i++;
			}
			goog.asserts.assert(inputIndex != -1, "Named input \"%s\" not found.", name);
			goog.asserts.assert(refIndex != -1, "Reference input \"%s\" not found.",
								refName);
			this.moveNumberedInputBefore(inputIndex, refIndex);
		}

		/// <summary>
		/// Move a numbered input to a different location on this block.
		/// </summary>
		/// <param name="inputIndex">Index of the input to move.</param>
		/// <param name="refIndex">Index of input that should be after the moved input.</param>
		public virtual void moveNumberedInputBefore(int inputIndex, int refIndex)
		{
			// Validate arguments.
			goog.asserts.assert(inputIndex != refIndex, "Can't move input to itself.");
			goog.asserts.assert(inputIndex < this.inputList.Length,
								"Input index " + inputIndex + " out of bounds.");
			goog.asserts.assert(refIndex <= this.inputList.Length,
								"Reference input " + refIndex + " out of bounds.");
			// Remove input.
			var input = this.inputList[inputIndex];
			this.inputList.Splice(inputIndex, 1);
			if (inputIndex < refIndex) {
				refIndex--;
			}
			// Reinsert input.
			this.inputList.Splice(refIndex, 0, input);
		}

		/// <summary>
		/// Remove an input from this block.
		/// </summary>
		/// <param name="name">The name of the input.</param>
		/// <param name="opt_quiet">True to prevent error if input is not present.</param>
		public virtual void removeInput(string name, bool opt_quiet = false)
		{
			int i = 0;
			foreach (var input in this.inputList) {
				if (input.name == name) {
					if (input.connection != null && input.connection.isConnected()) {
						input.connection.setShadowDom(null);
						var block = input.connection.targetBlock();
						if (block.isShadow()) {
							// Destroy any attached shadow block.
							block.dispose();
						}
						else {
							// Disconnect any attached normal block.
							block.unplug();
						}
					}
					input.dispose();
					this.inputList.Splice(i, 1);
					return;
				}
				i++;
			}
			if (!opt_quiet) {
				goog.asserts.fail("Input \"%s\" not found.", name);
			}
		}

		/// <summary>
		/// Fetches the named input object.
		/// </summary>
		/// <param name="name">The name of the input.</param>
		/// <returns>The input object, or null if input does not exist.</returns>
		public Input getInput(string name)
		{
			foreach (var input in this.inputList) {
				if (input.name == name) {
					return input;
				}
			}
			// This input does not exist.
			return null;
		}

		/// <summary>
		/// Fetches the block attached to the named input.
		/// </summary>
		/// <param name="name">The name of the input.</param>
		/// <returns>The attached value block, or null if the input is
		/// either disconnected or if the input does not exist.</returns>
		public Block getInputTargetBlock(string name)
		{
			var input = this.getInput(name);
			return (input != null && input.connection != null) ? input.connection.targetBlock() : null;
		}

		/// <summary>
		/// Returns the comment on this block (or '' if none).
		/// </summary>
		/// <returns>Block's comment.</returns>
		public virtual string getCommentText()
		{
			return this.comment_.ToString() ?? "";
		}

		/// <summary>
		/// Set this block's comment text.
		/// </summary>
		/// <param name="text">text The text, or null to delete.</param>
		public virtual void setCommentText(string text)
		{
			if (this.comment_.ToString() != text) {
				Events.fire(new Events.Change(
					this, "comment", null, this.comment_.ToString(), text ?? ""));
				this.comment_ = text;
			}
		}

		/// <summary>
		/// Set this block's warning text.
		/// </summary>
		/// <param name="text">The text, or null to delete.</param>
		public virtual void setWarningText(string text, string opt_id = null)
		{
			// NOP.
		}

		/// <summary>
		/// Give this block a mutator dialog.
		/// </summary>
		/// <param name="mutator">A mutator dialog instance or null to remove.</param>
		public virtual void setMutator(Mutator mutator)
		{
			// NOP.
		}

		/// <summary>
		/// Return the coordinates of the top-left corner of this block relative to the
		/// drawing surface's origin (0,0).
		/// </summary>
		/// <returns>Object with .x and .y properties.</returns>
		public virtual goog.math.Coordinate getRelativeToSurfaceXY()
		{
			return this.xy_;
		}

		/// <summary>
		/// Move a block by a relative offset.
		/// </summary>
		/// <param name="dx">Horizontal offset.</param>
		/// <param name="dy">Vertical offset.</param>
		public virtual void moveBy(double dx, double dy)
		{
			goog.asserts.assert(this.parentBlock_ == null, "Block has parent.");
			var ev = new Events.Move(this);
			this.xy_.translate(dx, dy);
			ev.recordNew();
			Events.fire(ev);
		}

		/// <summary>
		/// Create a connection of the specified type.
		/// </summary>
		/// <param name="type">The type of the connection to create.</param>
		/// <returns>A new connection of the specified type.</returns>
		public virtual RenderedConnection makeConnection_(int type)
		{
			return new RenderedConnection(this, type);
		}

		public virtual void saveConnections(Block containerBlock)
		{
		}

		public virtual Block decompose(Workspace workspace)
		{
			return null;
		}

		public virtual void compose(Block containerBlock)
		{
		}

		public virtual Element mutationToDom()
		{
			return null;
		}

		public virtual void domToMutation(Element xmlElement)
		{
		}

		Dictionary<string, object> kvp = new Dictionary<string, object>();

		public object this[string name] {
			get { if (kvp.TryGetValue(name, out var result)) return result; return null; }
			set { kvp[name] = value; }
		}
	}
}
