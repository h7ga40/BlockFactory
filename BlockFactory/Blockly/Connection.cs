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
 * @fileoverview Components for creating connections between blocks.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Connection
	{
		protected Block sourceBlock_;
		public int type;

		/// <summary>
		/// Class for a connection between blocks.
		/// </summary>
		/// <param name="source">The block establishing this connection.</param>
		/// <param name="type">The type of the connection.</param>
		public Connection(Block source, int type)
		{
			/**
			 * @type {!Blockly.Block}
			 * @private
			 */
			this.sourceBlock_ = source;
			/** @type {number} */
			this.type = type;
			// Shortcut for the databases for this connection's workspace.
			if (source.workspace.connectionDBList != null) {
				this.db_ = source.workspace.connectionDBList[type];
				this.dbOpposite_ =
					source.workspace.connectionDBList[Core.OPPOSITE_TYPE[type]];
				this.hidden_ = this.db_ == null;
			}
		}

		/**
		 * Constants for checking whether two connections are compatible.
		 */
		public const int CAN_CONNECT = 0;
		public const int REASON_SELF_CONNECTION = 1;
		public const int REASON_WRONG_TYPE = 2;
		public const int REASON_TARGET_NULL = 3;
		public const int REASON_CHECKS_FAILED = 4;
		public const int REASON_DIFFERENT_WORKSPACES = 5;
		public const int REASON_SHADOW_PARENT = 6;

		/// <summary>
		/// Connection this connection connects to.  Null if not connected.
		/// </summary>
		public Connection targetConnection;

		/// <summary>
		/// List of compatible value types.  Null if all types are compatible.
		/// </summary>
		private string[] check_;

		/// <summary>
		/// DOM representation of a shadow block, or null if none.
		/// </summary>
		private Element shadowDom_;

		/// <summary>
		/// Horizontal location of this connection.
		/// </summary>
		internal double x_;

		/// <summary>
		/// Vertical location of this connection.
		/// </summary>
		internal double y_;

		/// <summary>
		/// Has this connection been added to the connection database?
		/// </summary>
		internal bool inDB_;

		/// <summary>
		/// Connection database for connections of this type on the current workspace.
		/// </summary>
		protected ConnectionDB db_;

		/// <summary>
		/// Connection database for connections compatible with this type on the
		/// current workspace.
		/// </summary>
		protected ConnectionDB dbOpposite_;

		/// <summary>
		/// Whether this connections is hidden (not tracked in a database) or not.
		/// </summary>
		protected bool hidden_;

		protected static SVGElement highlightedPath_;

		/// <summary>
		/// Connect two connections together.  This is the connection on the superior
		/// block.
		/// </summary>
		/// <param name="childConnection"></param>
		protected virtual void connect_(Connection childConnection)
		{
			var parentConnection = this;
			var parentBlock = parentConnection.getSourceBlock();
			var childBlock = childConnection.getSourceBlock();
			// Disconnect any existing parent on the child connection.
			if (childConnection.isConnected()) {
				childConnection.disconnect();
			}
			if (parentConnection.isConnected()) {
				// Other connection is already connected to something.
				// Disconnect it and reattach it or bump it as needed.
				var orphanBlock = parentConnection.targetBlock();
				var shadowDom = parentConnection.getShadowDom();
				// Temporarily set the shadow DOM to null so it does not respawn.
				parentConnection.setShadowDom(null);
				// Displaced shadow blocks dissolve rather than reattaching or bumping.
				if (orphanBlock.isShadow()) {
					// Save the shadow block so that field values are preserved.
					shadowDom = Xml.blockToDom(orphanBlock);
					orphanBlock.dispose();
					orphanBlock = null;
				}
				else if (parentConnection.type == Core.INPUT_VALUE) {
					// Value connections.
					// If female block is already connected, disconnect and bump the male.
					if (orphanBlock.outputConnection == null) {
						throw new Exception("Orphan block does not have an output connection.");
					}
					// Attempt to reattach the orphan at the end of the newly inserted
					// block.  Since this block may be a row, walk down to the end
					// or to the first (and only) shadow block.
					var connection = Connection.lastConnectionInRow_(
						childBlock, orphanBlock);
					if (connection != null) {
						orphanBlock.outputConnection.connect(connection);
						orphanBlock = null;
					}
				}
				else if (parentConnection.type == Core.NEXT_STATEMENT) {
					// Statement connections.
					// Statement blocks may be inserted into the middle of a stack.
					// Split the stack.
					if (orphanBlock.previousConnection == null) {
						throw new Exception("Orphan block does not have a previous connection.");
					}
					// Attempt to reattach the orphan at the bottom of the newly inserted
					// block.  Since this block may be a stack, walk down to the end.
					var newBlock = childBlock;
					while (newBlock.nextConnection != null) {
						var nextBlock = newBlock.getNextBlock();
						if (nextBlock != null && !nextBlock.isShadow()) {
							newBlock = nextBlock;
						}
						else {
							if (orphanBlock.previousConnection.checkType_(
								newBlock.nextConnection)) {
								newBlock.nextConnection.connect(orphanBlock.previousConnection);
								orphanBlock = null;
							}
							break;
						}
					}
				}
				if (orphanBlock != null) {
					// Unable to reattach orphan.
					parentConnection.disconnect();
					if (Events.recordUndo) {
						// Bump it off to the side after a moment.
						var group = Events.getGroup();
						Window.SetTimeout(() => {
							// Verify orphan hasn't been deleted or reconnected (user on meth).
							if (orphanBlock.workspace != null && orphanBlock.getParent() == null) {
								Events.setGroup(group);
								if (orphanBlock.outputConnection != null) {
									((RenderedConnection)orphanBlock.outputConnection).bumpAwayFrom_(parentConnection);
								}
								else if (orphanBlock.previousConnection != null) {
									((RenderedConnection)orphanBlock.previousConnection).bumpAwayFrom_(parentConnection);
								}
								Events.setGroup(false);
							}
						}, Core.BUMP_DELAY);
					}
				}
				// Restore the shadow DOM.
				parentConnection.setShadowDom(shadowDom);
			}

			Events.Move e = null;
			if (Events.isEnabled()) {
				e = new Events.Move(childBlock);
			}
			// Establish the connections.
			Connection.connectReciprocally_(parentConnection, childConnection);
			// Demote the inferior block so that one is a child of the superior one.
			childBlock.setParent(parentBlock);
			if (e != null) {
				e.recordNew();
				Events.fire(e);
			}
		}

		/// <summary>
		/// Sever all links to this connection (not including from the source object).
		/// </summary>
		public void dispose()
		{
			if (this.isConnected()) {
				throw new Exception("Disconnect connection before disposing of it.");
			}
			if (this.inDB_) {
				this.db_.removeConnection_(this);
			}
			if (Core.highlightedConnection_ == this) {
				Core.highlightedConnection_ = null;
			}
			if (Core.localConnection_ == this) {
				Core.localConnection_ = null;
			}
			this.db_ = null;
			this.dbOpposite_ = null;
		}

		/// <summary>
		/// Get the source block for this connection.
		/// </summary>
		/// <returns>The source block, or null if there is none.</returns>
		public Block getSourceBlock()
		{
			return this.sourceBlock_;
		}

		/// <summary>
		/// Does the connection belong to a superior block (higher in the source stack)?
		/// </summary>
		/// <returns></returns>
		public bool isSuperior()
		{
			return this.type == Core.INPUT_VALUE ||
				this.type == Core.NEXT_STATEMENT;
		}

		/// <summary>
		/// Is the connection connected?
		/// </summary>
		/// <returns></returns>
		public bool isConnected()
		{
			return this.targetConnection != null;
		}

		/// <summary>
		/// Checks whether the current connection can connect with the target
		/// connection.
		/// </summary>
		/// <param name="target">Connection to check compatibility with.</param>
		/// <returns>Blockly.Connection.CAN_CONNECT if the connection is legal,
		/// an error code otherwise.</returns>
		private int canConnectWithReason_(Connection target)
		{
			if (target == null) {
				return Connection.REASON_TARGET_NULL;
			}
			Block blockA;
			Block blockB;
			if (this.isSuperior()) {
				blockA = this.sourceBlock_;
				blockB = target.getSourceBlock();
			}
			else {
				blockB = this.sourceBlock_;
				blockA = target.getSourceBlock();
			}
			if (blockA != null && blockA == blockB) {
				return Connection.REASON_SELF_CONNECTION;
			}
			else if (target.type != Core.OPPOSITE_TYPE[this.type]) {
				return Connection.REASON_WRONG_TYPE;
			}
			else if (blockA != null && blockB != null && blockA.workspace != blockB.workspace) {
				return Connection.REASON_DIFFERENT_WORKSPACES;
			}
			else if (!this.checkType_(target)) {
				return Connection.REASON_CHECKS_FAILED;
			}
			else if (blockA.isShadow() && !blockB.isShadow()) {
				return Connection.REASON_SHADOW_PARENT;
			}
			return Connection.CAN_CONNECT;
		}

		/// <summary>
		/// Checks whether the current connection and target connection are compatible
		/// and throws an exception if they are not.
		/// </summary>
		/// <param name="target">The connection to check compatibility
		/// with.</param>
		private void checkConnection_(Connection target)
		{
			switch (this.canConnectWithReason_(target)) {
			case Connection.CAN_CONNECT:
				break;
			case Connection.REASON_SELF_CONNECTION:
				throw new Exception("Attempted to connect a block to itself.");
			case Connection.REASON_DIFFERENT_WORKSPACES:
				// Usually this means one block has been deleted.
				throw new Exception("Blocks not on same workspace.");
			case Connection.REASON_WRONG_TYPE:
				throw new Exception("Attempt to connect incompatible types.");
			case Connection.REASON_TARGET_NULL:
				throw new Exception("Target connection is null.");
			case Connection.REASON_CHECKS_FAILED:
				throw new Exception("Connection checks failed.");
			case Connection.REASON_SHADOW_PARENT:
				throw new Exception("Connecting non-shadow to shadow block.");
			default:
				throw new Exception("Unknown connection failure: this should never happen!");
			}
		}

		/// <summary>
		/// Check if the two connections can be dragged to connect to each other.
		/// </summary>
		/// <param name="candidate">A nearby connection to check.</param>
		/// <returns>True if the connection is allowed, false otherwise.</returns>
		public bool isConnectionAllowed(Connection candidate)
		{
			// Type checking.
			var canConnect = this.canConnectWithReason_(candidate);
			if (canConnect != Connection.CAN_CONNECT) {
				return false;
			}

			// Don't offer to connect an already connected left (male) value plug to
			// an available right (female) value plug.  Don't offer to connect the
			// bottom of a statement block to one that's already connected.
			if (candidate.type == Core.OUTPUT_VALUE ||
				candidate.type == Core.PREVIOUS_STATEMENT) {
				if (candidate.isConnected() || this.isConnected()) {
					return false;
				}
			}

			// Offering to connect the left (male) of a value block to an already
			// connected value pair is ok, we'll splice it in.
			// However, don't offer to splice into an immovable block.
			if (candidate.type == Core.INPUT_VALUE && candidate.isConnected() &&
				!candidate.targetBlock().isMovable() &&
				!candidate.targetBlock().isShadow()) {
				return false;
			}

			// Don't let a block with no next connection bump other blocks out of the
			// stack.  But covering up a shadow block or stack of shadow blocks is fine.
			// Similarly, replacing a terminal statement with another terminal statement
			// is allowed.
			if (this.type == Core.PREVIOUS_STATEMENT &&
				candidate.isConnected() &&
				this.sourceBlock_.nextConnection == null &&
				!candidate.targetBlock().isShadow() &&
				candidate.targetBlock().nextConnection != null) {
				return false;
			}

			// Don't let blocks try to connect to themselves or ones they nest.
			if (Array.IndexOf(Core.draggingConnections_, candidate) != -1) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Connect this connection to another connection.
		/// </summary>
		/// <param name="otherConnection">Connection to connect to.</param>
		public void connect(Connection otherConnection)
		{
			if (this.targetConnection == otherConnection) {
				// Already connected together.  NOP.
				return;
			}
			this.checkConnection_(otherConnection);
			// Determine which block is superior (higher in the source stack).
			if (this.isSuperior()) {
				// Superior block.
				this.connect_(otherConnection);
			}
			else {
				// Inferior block.
				otherConnection.connect_(this);
			}
		}

		/// <summary>
		/// Update two connections to target each other.
		/// </summary>
		/// <param name="first">The first connection to update.</param>
		/// <param name="second">The second conneciton to update.</param>
		private static void connectReciprocally_(Connection first, Connection second)
		{
			goog.asserts.assert(first != null && second != null, "Cannot connect null connections.");
			first.targetConnection = second;
			second.targetConnection = first;
		}

		/// <summary>
		/// Does the given block have one and only one connection point that will accept
		/// an orphaned block?
		/// </summary>
		/// <param name="block">The superior block.</param>
		/// <param name="orphanBlock">The inferior block.</param>
		/// <returns>The suitable connection point on 'block',
		/// or null.</returns>
		private static Connection singleConnection_(Block block, Block orphanBlock)
		{
			Connection connection = null;
			for (var i = 0; i < block.inputList.Length; i++) {
				var thisConnection = block.inputList[i].connection;
				if (thisConnection != null && thisConnection.type == Core.INPUT_VALUE &&
					orphanBlock.outputConnection.checkType_(thisConnection)) {
					if (connection != null) {
						return null;  // More than one connection.
					}
					connection = thisConnection;
				}
			}
			return connection;
		}

		/// <summary>
		/// Walks down a row a blocks, at each stage checking if there are any
		/// connections that will accept the orphaned block.  If at any point there
		/// are zero or multiple eligible connections, returns null.  Otherwise
		/// returns the only input on the last block in the chain.
		/// Terminates early for shadow blocks.
		/// </summary>
		/// <param name="startBlock">The block on which to start the search.</param>
		/// <param name="orphanBlock">The block that is looking for a home.</param>
		/// <returns>The suitable connection point on the chain
		/// of blocks, or null.</returns>
		private static Connection lastConnectionInRow_(Block startBlock, Block orphanBlock)
		{
			var newBlock = startBlock;
			Connection connection;
			while ((connection = Connection.singleConnection_(newBlock, orphanBlock)) != null) {
				// '=' is intentional in line above.
				newBlock = connection.targetBlock();
				if (newBlock == null || newBlock.isShadow()) {
					return connection;
				}
			}
			return null;
		}

		/// <summary>
		/// Disconnect this connection.
		/// </summary>
		public void disconnect()
		{
			var otherConnection = this.targetConnection;
			goog.asserts.assert(otherConnection != null, "Source connection not connected.");
			goog.asserts.assert(otherConnection.targetConnection == this,
				"Target connection not connected to source connection.");

			Block parentBlock, childBlock;
			Connection parentConnection;
			if (this.isSuperior()) {
				// Superior block.
				parentBlock = this.sourceBlock_;
				childBlock = otherConnection.getSourceBlock();
				parentConnection = this;
			}
			else {
				// Inferior block.
				parentBlock = otherConnection.getSourceBlock();
				childBlock = this.sourceBlock_;
				parentConnection = otherConnection;
			}
			this.disconnectInternal_(parentBlock, childBlock);
			parentConnection.respawnShadow_();
		}

		/// <summary>
		/// Disconnect two blocks that are connected by this connection.
		/// </summary>
		/// <param name="parentBlock">The superior block.</param>
		/// <param name="childBlock">The inferior block.</param>
		protected virtual void disconnectInternal_(Block parentBlock, Block childBlock)
		{
			Events.Move e = null;
			if (Events.isEnabled()) {
				e = new Events.Move(childBlock);
			}
			var otherConnection = this.targetConnection;
			otherConnection.targetConnection = null;
			this.targetConnection = null;
			childBlock.setParent(null);
			if (e != null) {
				e.recordNew();
				Events.fire(e);
			}
		}

		/// <summary>
		/// Respawn the shadow block if there was one connected to the this connection.
		/// </summary>
		protected virtual void respawnShadow_()
		{
			var parentBlock = this.getSourceBlock();
			var shadow = this.getShadowDom();
			if (parentBlock.workspace != null && shadow != null && Events.recordUndo) {
				var blockShadow =
					Xml.domToBlock(shadow, parentBlock.workspace);
				if (blockShadow.outputConnection != null) {
					this.connect(blockShadow.outputConnection);
				}
				else if (blockShadow.previousConnection != null) {
					this.connect(blockShadow.previousConnection);
				}
				else {
					throw new Exception("Child block does not have output or previous statement.");
				}
			}
		}

		/// <summary>
		/// Returns the block that this connection connects to.
		/// </summary>
		/// <returns>The connected block or null if none is connected.</returns>
		public Block targetBlock()
		{
			if (this.isConnected()) {
				return this.targetConnection.getSourceBlock();
			}
			return null;
		}

		/// <summary>
		/// Is this connection compatible with another connection with respect to the
		/// value type system.  E.g. square_root("Hello") is not compatible.
		/// </summary>
		/// <param name="otherConnection">Connection to compare against.</param>
		/// <returns>True if the connections share a type.</returns>
		public bool checkType_(Connection otherConnection)
		{
			if (this.check_ == null || otherConnection.check_ == null) {
				// One or both sides are promiscuous enough that anything will fit.
				return true;
			}
			// Find any intersection in the check lists.
			foreach (var i in this.check_) {
				if (Array.IndexOf(otherConnection.check_, i) != -1) {
					return true;
				}
			}
			// No intersection.
			return false;
		}

		/// <summary>
		/// Change a connection's compatibility.
		/// </summary>
		/// <param name="check">Compatible value type or list of value types.
		/// Null if all types are compatible.</param>
		/// <returns>The connection being modified
		/// (to allow chaining).</returns>
		public Connection setCheck(Union<string, string[]> check)
		{
			if (check != null) {
				// Ensure that check is in an array.
				if (check.Is<string>()) {
					this.check_ = new string[] { check.As<string>() };
				}
				else {
					this.check_ = check.As<string[]>();
				}
				// The new value type may not be compatible with the existing connection.
				if (this.isConnected() && !this.checkType_(this.targetConnection)) {
					var child = this.isSuperior() ? this.targetBlock() : this.sourceBlock_;
					child.unplug();
					// Bump away.
					this.sourceBlock_.bumpNeighbours_();
				}
			}
			else {
				this.check_ = null;
			}
			return this;
		}

		/// <summary>
		/// Change a connection's shadow block.
		/// </summary>
		/// <param name="shadow">DOM representation of a block or null.</param>
		public void setShadowDom(Element shadow)
		{
			this.shadowDom_ = shadow;
		}

		/// <summary>
		/// Return a connection's shadow block.
		/// </summary>
		/// <returns>shadow DOM representation of a block or null.</returns>
		public Element getShadowDom()
		{
			return this.shadowDom_;
		}
	}
}
