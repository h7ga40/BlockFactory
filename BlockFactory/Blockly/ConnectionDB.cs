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
 * @fileoverview Components for managing connections between blocks.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace Blockly
{
	public class ConnectionDB
	{
		JsArray<RenderedConnection> array_ = new JsArray<RenderedConnection>();

		/// <summary>
		/// Database of connections.
		/// Connections are stored in order of their vertical component.  This way
		/// connections in an area may be looked up quickly using a binary search.
		/// </summary>
		public ConnectionDB()
		{
		}

		/// <summary>
		/// Add a connection to the database.  Must not already exist in DB.
		/// </summary>
		/// <param name="connection">The connection to be added.</param>
		public void addConnection(RenderedConnection connection)
		{
			if (connection.inDB_) {
				throw new Exception("Connection already in database.");
			}
			if (connection.getSourceBlock().isInFlyout) {
				// Don't bother maintaining a database of connections in a flyout.
				return;
			}
			var position = this.findPositionForConnection_(connection);
			this.array_.Splice(position, 0, connection);
			connection.inDB_ = true;
		}

		/// <summary>
		/// Find the given connection.
		/// Starts by doing a binary search to find the approximate location, then
		/// linearly searches nearby for the exact connection.
		/// </summary>
		/// <param name="conn">The connection to find.</param>
		/// <returns>The index of the connection, or -1 if the connection was
		/// not found.</returns>
		public int findConnection(Connection conn)
		{
			if (this.array_.Length == 0) {
				return -1;
			}

			var bestGuess = this.findPositionForConnection_(conn);
			if (bestGuess >= this.array_.Length) {
				// Not in list
				return -1;
			}

			var yPos = conn.y_;
			// Walk forward and back on the y axis looking for the connection.
			var pointerMin = bestGuess;
			var pointerMax = bestGuess;
			while (pointerMin >= 0 && this.array_[pointerMin].y_ == yPos) {
				if (this.array_[pointerMin] == conn) {
					return pointerMin;
				}
				pointerMin--;
			}

			while (pointerMax < this.array_.Length && this.array_[pointerMax].y_ == yPos) {
				if (this.array_[pointerMax] == conn) {
					return pointerMax;
				}
				pointerMax++;
			}
			return -1;
		}

		/// <summary>
		/// Finds a candidate position for inserting this connection into the list.
		/// This will be in the correct y order but makes no guarantees about ordering in
		/// the x axis.
		/// </summary>
		/// <param name="connection">The connection to insert.</param>
		/// <returns>The candidate index.</returns>
		private int findPositionForConnection_(Connection connection)
		{
			if (this.array_.Length == 0) {
				return 0;
			}
			var pointerMin = 0;
			var pointerMax = this.array_.Length;
			while (pointerMin < pointerMax) {
				var pointerMid = (int)System.Math.Floor((pointerMin + pointerMax) / 2.0);
				if (this.array_[pointerMid].y_ < connection.y_) {
					pointerMin = pointerMid + 1;
				}
				else if (this.array_[pointerMid].y_ > connection.y_) {
					pointerMax = pointerMid;
				}
				else {
					pointerMin = pointerMid;
					break;
				}
			}
			return pointerMin;
		}

		/// <summary>
		/// Remove a connection from the database.  Must already exist in DB.
		/// </summary>
		/// <param name="connection">The connection to be removed.</param>
		internal void removeConnection_(Connection connection)
		{
			if (!connection.inDB_) {
				throw new Exception("Connection not in database.");
			}
			var removalIndex = this.findConnection(connection);
			if (removalIndex == -1) {
				throw new Exception("Unable to find connection in connectionDB.");
			}
			connection.inDB_ = false;
			this.array_.Splice(removalIndex, 1);
		}

		/// <summary>
		/// Find all nearby connections to the given connection.
		/// Type checking does not apply, since this function is used for bumping.
		/// </summary>
		/// <param name="connection">The connection whose neighbours
		/// should be returned.</param>
		/// <param name="maxRadius">The maximum radius to another connection.</param>
		/// <returns>List of connections.</returns>
		public Connection[] getNeighbours(RenderedConnection connection, double maxRadius)
		{
			var db = this.array_;
			var currentX = connection.x_;
			var currentY = connection.y_;

			// Binary search to find the closest y location.
			var pointerMin = 0;
			var pointerMax = db.Length - 2;
			var pointerMid = pointerMax;
			while (pointerMin < pointerMid) {
				if (db[pointerMid].y_ < currentY) {
					pointerMin = pointerMid;
				}
				else {
					pointerMax = pointerMid;
				}
				pointerMid = (int)System.Math.Floor((pointerMin + pointerMax) / 2.0);
			}

			var neighbours = new JsArray<Connection>();
			/**
			 * Computes if the current connection is within the allowed radius of another
			 * connection.
			 * This function is a closure and has access to outside variables.
			 * @param {number} yIndex The other connection's index in the database.
			 * @return {boolean} True if the current connection's vertical distance from
			 *     the other connection is less than the allowed radius.
			 */
			var checkConnection_ = new Func<int, bool>((yIndex) => {
				var dx = currentX - db[yIndex].x_;
				var dy = currentY - db[yIndex].y_;
				var r = System.Math.Sqrt(dx * dx + dy * dy);
				if (r <= maxRadius) {
					neighbours.Push(db[yIndex]);
				}
				return dy < maxRadius;
			});

			// Walk forward and back on the y axis looking for the closest x,y point.
			pointerMin = pointerMid;
			pointerMax = pointerMid;
			if (db.Length != 0) {
				while (pointerMin >= 0 && checkConnection_(pointerMin)) {
					pointerMin--;
				}
				do {
					pointerMax++;
				} while (pointerMax < db.Length && checkConnection_(pointerMax));
			}

			return neighbours;
		}

		/// <summary>
		/// Is the candidate connection close to the reference connection.
		/// Extremely fast; only looks at Y distance.
		/// </summary>
		/// <param name="index">Index in database of candidate connection.</param>
		/// <param name="baseY">Reference connection's Y value.</param>
		/// <param name="maxRadius">The maximum radius to another connection.</param>
		/// <returns>True if connection is in range.</returns>
		private bool isInYRange_(int index, double baseY, double maxRadius)
		{
			return (System.Math.Abs(this.array_[index].y_ - baseY) <= maxRadius);
		}

		/// <summary>
		/// Find the closest compatible connection to this connection.
		/// </summary>
		/// <param name="conn">The connection searching for a compatible
		/// mate.</param>
		/// <param name="maxRadius">The maximum radius to another connection.</param>
		/// <param name="dxy">Offset between this connection's location
		/// in the database and the current location (as a result of dragging).</param>
		/// <returns>Contains two properties:' connection' which is either another connection or null,
		/// and 'radius' which is the distance.</returns>
		public RenderedConnection.Closest searchForClosest(RenderedConnection conn, double maxRadius,
			goog.math.Coordinate dxy)
		{
			// Don't bother.
			if (this.array_.Length == 0) {
				return new RenderedConnection.Closest { connection = null, radius = maxRadius };
			}

			// Stash the values of x and y from before the drag.
			var baseY = conn.y_;
			var baseX = conn.x_;

			conn.x_ = baseX + dxy.x;
			conn.y_ = baseY + dxy.y;

			// findPositionForConnection finds an index for insertion, which is always
			// after any block with the same y index.  We want to search both forward
			// and back, so search on both sides of the index.
			var closestIndex = this.findPositionForConnection_(conn);

			Connection bestConnection = null;
			var bestRadius = maxRadius;
			RenderedConnection temp;

			// Walk forward and back on the y axis looking for the closest x,y point.
			var pointerMin = closestIndex - 1;
			while (pointerMin >= 0 && this.isInYRange_(pointerMin, conn.y_, maxRadius)) {
				temp = this.array_[pointerMin];
				if (conn.isConnectionAllowed(temp, bestRadius)) {
					bestConnection = temp;
					bestRadius = temp.distanceFrom(conn);
				}
				pointerMin--;
			}

			var pointerMax = closestIndex;
			while (pointerMax < this.array_.Length && this.isInYRange_(pointerMax, conn.y_,
				maxRadius)) {
				temp = this.array_[pointerMax];
				if (conn.isConnectionAllowed(temp, bestRadius)) {
					bestConnection = temp;
					bestRadius = temp.distanceFrom(conn);
				}
				pointerMax++;
			}

			// Reset the values of x and y.
			conn.x_ = baseX;
			conn.y_ = baseY;

			// If there were no valid connections, bestConnection will be null.
			return new RenderedConnection.Closest { connection = bestConnection, radius = bestRadius };
		}

		/// <summary>
		/// Initialize a set of connection DBs for a specified workspace.
		/// </summary>
		/// <param name="workspace">The workspace this DB is for.</param>
		public static void init(WorkspaceSvg workspace)
		{
			// Create four databases, one for each connection type.
			var dbList = new ConnectionDB[5];
			dbList[Core.INPUT_VALUE] = new ConnectionDB();
			dbList[Core.OUTPUT_VALUE] = new ConnectionDB();
			dbList[Core.NEXT_STATEMENT] = new ConnectionDB();
			dbList[Core.PREVIOUS_STATEMENT] = new ConnectionDB();
			workspace.connectionDBList = dbList;
		}
	}
}
