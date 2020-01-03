/**
 * @license
 * Visual Blocks Editor
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

/**
 * @fileoverview Events fired as a result of actions in Blockly's editor.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public static class Events
	{
		/// <summary>
		/// Group ID for new events.  Grouped events are indivisible.
		/// </summary>
		internal static string group_ = "";

		/// <summary>
		/// Sets whether events should be added to the undo stack.
		/// </summary>
		public static bool recordUndo = true;

		/// <summary>
		/// Allow change events to be created and fired.
		/// </summary>
		private static int disabled_;

		/// <summary>
		/// Name of event that creates a block.
		/// </summary>
		public const string CREATE = "create";

		/// <summary>
		/// Name of event that deletes a block.
		/// </summary>
		public const string DELETE = "delete";

		/// <summary>
		/// Name of event that changes a block.
		/// </summary>
		public const string CHANGE = "change";

		/// <summary>
		/// Name of event that moves a block.
		/// </summary>
		public const string MOVE = "move";

		/// <summary>
		/// Name of event that records a UI change.
		/// </summary>
		public const string UI = "ui";

		/// <summary>
		/// List of events queued for firing.
		/// </summary>
		private static JsArray<Events.Abstract> FIRE_QUEUE_ = new JsArray<Abstract>();

		/// <summary>
		/// Create a custom event and fire it.
		/// </summary>
		/// <param name="ev">Custom data for event.</param>
		public static void fire(Events.Abstract ev)
		{
			if (!Events.isEnabled()) {
				return;
			}
			if (Events.FIRE_QUEUE_.Length == 0) {
				// First event added; schedule a firing of the event queue.
				Window.SetTimeout(Events.fireNow_, 0);
			}
			Events.FIRE_QUEUE_.Push(ev);
		}

		/// <summary>
		/// Fire all queued events.
		/// </summary>
		private static void fireNow_()
		{
			var queue = Events.filter(Events.FIRE_QUEUE_, true);
			Events.FIRE_QUEUE_.Clear();
			foreach (var ev in queue) {
				var workspace = Workspace.getById(ev.workspaceId);
				if (workspace != null) {
					workspace.fireChangeListener(ev);
				}
			}
		}

		/// <summary>
		/// Filter the queued events and merge duplicates.
		/// </summary>
		/// <param name="queueIn"></param>
		/// <param name="forward"></param>
		/// <returns></returns>
		public static JsArray<Events.Abstract> filter(JsArray<Events.Abstract> queueIn, bool forward)
		{
			var queue = new JsArray<Events.Abstract>(queueIn.Clone());
			if (!forward) {
				// Undo is merged in reverse order.
				queue.Reverse();
			}
			// Merge duplicates.  O(n^2), but n should be very small.
			for (var i = 0; i < queue.Length; i++) {
				var event1 = queue[i];
				for (var j = i + 1; j < queue.Length; j++) {
					var event2 = queue[j];
					if (event1.type == event2.type &&
						event1.blockId == event2.blockId &&
						event1.workspaceId == event2.workspaceId) {
						if (event1.type == Events.MOVE) {
							// Merge move events.
							((Events.Move)event1).newParentId = ((Events.Move)event2).newParentId;
							((Events.Move)event1).newInputName = ((Events.Move)event2).newInputName;
							((Events.Move)event1).newCoordinate = ((Events.Move)event2).newCoordinate;
							queue.Splice(j, 1);
							j--;
						}
						else if (event1.type == Events.CHANGE &&
							((Events.Change)event1).element == ((Events.Change)event2).element &&
							((Events.Change)event1).name == ((Events.Change)event2).name) {
							// Merge change events.
							((Events.Change)event1).newValue = ((Events.Change)event2).newValue;
							queue.Splice(j, 1);
							j--;
						}
						else if (event1.type == Events.UI &&
							((Events.Ui)event2).element == "click" &&
							(((Events.Ui)event1).element == "commentOpen" ||
							((Events.Ui)event1).element == "mutatorOpen" ||
							((Events.Ui)event1).element == "warningOpen")) {
							// Merge change events.
							((Events.Ui)event1).newValue = ((Events.Ui)event2).newValue;
							queue.Splice(j, 1);
							j--;
						}
					}
				}
			}
			// Remove null events.
			for (var i = queue.Length - 1; i >= 0; i--) {
				if (queue[i].isNull()) {
					queue.Splice(i, 1);
				}
			}
			if (!forward) {
				// Restore undo order.
				queue.Reverse();
			}
			// Move mutation events to the top of the queue.
			// Intentionally skip first event.
			Events.Abstract ev;
			for (var i = 1; i < queue.Length; i++) {
				ev = queue[i];
				if (ev.type == Events.CHANGE &&
					((Events.Change)ev).element == "mutation") {
					queue.Unshift(queue.Splice(i, 1)[0]);
				}
			}
			return queue;
		}

		/// <summary>
		/// Modify pending undo events so that when they are fired they don't land
		/// in the undo stack.  Called by Blockly.Workspace.clearUndo.
		/// </summary>
		public static void clearPendingUndo()
		{
			foreach (var ev in Events.FIRE_QUEUE_) {
				ev.recordUndo = false;
			}
		}

		/// <summary>
		/// Stop sending events.  Every call to this function MUST also call enable.
		/// </summary>
		public static void disable()
		{
			Events.disabled_++;
		}

		/// <summary>
		/// Start sending events.  Unless events were already disabled when the
		/// corresponding call to disable was made.
		/// </summary>
		public static void enable()
		{
			Events.disabled_--;
		}

		/// <summary>
		/// Returns whether events may be fired or not.
		/// </summary>
		/// <returns>True if enabled.</returns>
		public static bool isEnabled()
		{
			return Events.disabled_ == 0;
		}

		/// <summary>
		/// Current group.
		/// </summary>
		/// <returns>ID string.</returns>
		public static string getGroup()
		{
			return Events.group_;
		}

		/// <summary>
		/// Start or stop a group.
		/// </summary>
		/// <param name="state">True to start new group, false to end group.
		/// String to set group explicitly.</param>
		public static void setGroup(Union<bool, string> state)
		{
			if (state.Is<bool>()) {
				Events.group_ = state.As<bool>() ? Core.genUid() : "";
			}
			else {
				Events.group_ = state.As<string>();
			}
		}

		/// <summary>
		/// Compute a list of the IDs of the specified block and all its descendants.
		/// </summary>
		/// <param name="block">The root block.</param>
		/// <returns>List of block IDs.</returns>
		public static JsArray<string> getDescendantIds_(Block block)
		{
			var ids = new JsArray<string>();
			var descendants = block.getDescendants();
			foreach (var descendant in descendants) {
				ids.Push(descendant.id);
			}
			return ids;
		}

		/// <summary>
		/// Decode the JSON into an event.
		/// </summary>
		/// <param name="json">JSON representation.</param>
		/// <param name="workspace">Target workspace for event.</param>
		/// <returns>The event represented by the JSON.</returns>
		public static Events.Abstract fromJson(EventJsonData json, Workspace workspace)
		{
			Events.Abstract ev;
			switch (json.type.ToString()) {
			case Events.CREATE:
				ev = new Events.Create(null);
				break;
			case Events.DELETE:
				ev = new Events.Delete(null);
				break;
			case Events.CHANGE:
				ev = new Events.Change(null, null, null, null, null);
				break;
			case Events.MOVE:
				ev = new Events.Move(null);
				break;
			case Events.UI:
				ev = new Events.Ui(null, null, null, null);
				break;
			default:
				throw new Exception("Unknown ev type.");
			}
			ev.fromJson(json);
			ev.workspaceId = workspace.id;
			return ev;
		}

		/// <summary>
		/// Enable/disable a block depending on whether it is properly connected.
		/// Use this on applications where all blocks should be connected to a top block.
		/// Recommend setting the 'disable' option to 'false' in the config so that
		/// users don't try to reenable disabled orphan blocks.
		/// </summary>
		/// <param name="ev">Custom data for event.</param>
		public static void disableOrphans(Events.Abstract ev)
		{
			if (ev.type == Events.MOVE ||
				ev.type == Events.CREATE) {
				Events.disable();
				var workspace = Workspace.getById(ev.workspaceId);
				var block = workspace.getBlockById(ev.blockId);
				if (block != null) {
					if (block.getParent() != null && !block.getParent().disabled) {
						var children = block.getDescendants();
						foreach (var child in children) {
							child.setDisabled(false);
						}
					}
					else if ((block.outputConnection != null || block.previousConnection != null) &&
							 Core.dragMode_ == Core.DRAG_NONE) {
						do {
							block.setDisabled(true);
							block = block.getNextBlock();
						} while (block != null);
					}
				}
				Events.enable();
			}
		}

		public class Abstract
		{
			/// <summary>
			/// One of Blockly.Events.CREATE, Blockly.Events.DELETE, Blockly.Events.CHANGE, Blockly.Events.MOVE, Blockly.Events.UI.
			/// </summary>
			public string type;

			/// <summary>
			/// UUID of workspace. The workspace can be found with Workspace.getById(ev.workspaceId)
			/// </summary>
			public string workspaceId;

			/// <summary>
			/// UUID of block. The block can be found with workspace.getBlockById(ev.blockId)
			/// </summary>
			public string blockId;

			/// <summary>
			/// UUID of group. Some events are part of an indivisible group, such as inserting a statement in a stack.
			/// </summary>
			public string group;

			public bool recordUndo;

			/// <summary>
			/// Abstract class for an event.
			/// </summary>
			/// <param name="block">The block.</param>
			/// <param name="type"></param>
			public Abstract(Block block, string type)
			{
				this.type = type;
				if (block != null) {
					blockId = block.id;
					workspaceId = block.workspace.id;
				}
				group = Events.group_;
				recordUndo = Events.recordUndo;
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public virtual EventJsonData toJson()
			{
				var json = new EventJsonData();
				json.type = this.type;
				if (this.blockId != null) {
					json.blockId = this.blockId;
				}
				if (this.group != null) {
					json.group = this.group;
				}
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public virtual void fromJson(EventJsonData json)
			{
				this.blockId = json.blockId.ToString();
				this.group = json.group.ToString();
			}

			/// <summary>
			/// Does this event record any change of state?
			/// </summary>
			/// <returns>True if null, false if something changed.</returns>
			public virtual bool isNull()
			{
				return false;
			}

			/// <summary>
			/// Run an event.
			/// </summary>
			/// <param name="forward">True if run forward, false if run backward (undo).</param>
			public virtual void run(bool forward)
			{
				// Defined by subclasses.
			}
		}

		public class Create : Abstract
		{
			/// <summary>
			/// An XML tree defining the new block and any connected child blocks.
			/// </summary>
			public Element xml;

			/// <summary>
			/// An array containing the UUIDs of the new block and any connected child blocks.
			/// </summary>
			public JsArray<string> ids;

			/// <summary>
			/// Class for a block creation ev.
			/// </summary>
			/// <param name="block">The created block.  Null for a blank ev.</param>
			public Create(Block block)
				 : base(block, Events.CREATE)
			{
				if (block == null) {
					return;  // Blank ev to be populated by fromJson.
				}
				xml = Xml.blockToDomWithXY(block);
				ids = Events.getDescendantIds_(block);
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public override EventJsonData toJson()
			{
				var json = base.toJson();
				json.xml = Xml.domToText(this.xml);
				json.ids = this.ids;
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public override void fromJson(EventJsonData json)
			{
				base.fromJson(json);
				this.xml = (Element)Xml.textToDom("<xml>" + json.xml + "</xml>").FirstChild;
				this.ids = json.ids;
			}

			/// <summary>
			/// Run a creation event.
			/// </summary>
			/// <param name="forward">True if run forward, false if run backward (undo).</param>
			public override void run(bool forward)
			{
				var workspace = Workspace.getById(this.workspaceId);
				if (forward) {
					var xml = goog.dom.createDom("xml");
					xml.AppendChild(this.xml);
					Xml.domToWorkspace(xml, workspace);
				}
				else {
					foreach (var id in this.ids) {
						var block = (BlockSvg)workspace.getBlockById(id);
						if (block != null) {
							block.dispose(false, false);
						}
						else if (id == this.blockId) {
							// Only complain about root-level block.
							Console.WriteLine("Can't uncreate non-existant block: " + id);
						}
					}
				}
			}
		}
		public class Delete : Abstract
		{
			/// <summary>
			/// An XML tree defining the deleted block and any connected child blocks.
			/// </summary>
			public Element oldXml;

			/// <summary>
			/// An array containing the UUIDs of the deleted block and any connected child blocks.
			/// </summary>
			public JsArray<string> ids;

			/// <summary>
			/// Class for a block deletion ev.
			/// </summary>
			/// <param name="block">The deleted block.  Null for a blank ev.</param>
			public Delete(Block block)
				: base(block, Events.DELETE)
			{
				if (block == null) {
					return;  // Blank ev to be populated by fromJson.
				}
				if (block.getParent() != null) {
					throw new Exception("Connected blocks cannot be deleted.");
				}
				oldXml = Xml.blockToDomWithXY(block);
				ids = Events.getDescendantIds_(block);
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public override EventJsonData toJson()
			{
				var json = base.toJson();
				json.ids = this.ids;
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public override void fromJson(EventJsonData json)
			{
				base.fromJson(json);
				this.ids = json.ids;
			}

			/// <summary>
			/// Run a deletion event.
			/// </summary>
			/// <param name="forward"></param>
			public override void run(bool forward)
			{
				var workspace = Workspace.getById(this.workspaceId);
				if (forward) {
					foreach (var id in this.ids) {
						var block = (BlockSvg)workspace.getBlockById(id);
						if (block != null) {
							block.dispose(false, false);
						}
						else if (id == this.blockId) {
							// Only complain about root-level block.
							Console.WriteLine("Can't delete non-existant block: " + id);
						}
					}
				}
				else {
					var xml = goog.dom.createDom("xml");
					xml.AppendChild(this.oldXml);
					Xml.domToWorkspace(xml, workspace);
				}
			}
		}

		public class Change : Abstract
		{
			/// <summary>
			/// One of 'field', 'comment', 'collapsed', 'disabled', 'inline', 'mutate'
			/// </summary>
			public string element;

			/// <summary>
			/// Name of the field if this is a change to a field.
			/// </summary>
			public string name;

			/// <summary>
			/// Original value.
			/// </summary>
			public string oldValue;

			/// <summary>
			/// Changed value.
			/// </summary>
			public string newValue;

			/// <summary>
			/// Class for a block change ev.
			/// </summary>
			/// <param name="block">The changed block.  Null for a blank ev.</param>
			/// <param name="element">One of 'field', 'comment', 'disabled', etc.</param>
			/// <param name="name">Name of input or field affected, or null.</param>
			/// <param name="oldValue">Previous value of element.</param>
			/// <param name="newValue">New value of element.</param>
			public Change(Block block, string element, string name, string oldValue, string newValue)
				: base(block, Events.CHANGE)
			{
				if (block == null) {
					return;  // Blank ev to be populated by fromJson.
				}
				this.element = element;
				this.name = name;
				this.oldValue = oldValue;
				this.newValue = newValue;
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public override EventJsonData toJson()
			{
				var json = base.toJson();
				json.element = this.element;
				if (this.name != null) {
					json.name = this.name;
				}
				json.newValue = this.newValue;
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public override void fromJson(EventJsonData json)
			{
				base.fromJson(json);
				this.element = json.element.ToString();
				this.name = json.name.ToString();
				this.newValue = json.newValue.ToString();
			}

			/// <summary>
			/// Does this event record any change of state?
			/// </summary>
			/// <returns>True if something changed.</returns>
			public override bool isNull()
			{
				return this.oldValue == this.newValue;
			}

			/// <summary>
			/// Run a change event.
			/// </summary>
			/// <param name="forward">True if run forward, false if run backward (undo).</param>
			public override void run(bool forward)
			{
				var workspace = Workspace.getById(this.workspaceId);
				var block = (BlockSvg)workspace.getBlockById(this.blockId);
				if (block == null) {
					Console.WriteLine("Can't change non-existant block: " + this.blockId);
					return;
				}
				if (block.mutator != null) {
					// Close the mutator (if open) since we don't want to update it.
					block.mutator.setVisible(false);
				}
				var value = forward ? this.newValue : this.oldValue;
				switch (this.element) {
				case "field":
					var field = block.getField(this.name);
					if (field != null) {
						// Run the validator for any side-effects it may have.
						// The validator's opinion on validity is ignored.
						field.callValidator(value);
						field.setValue(value);
					}
					else {
						Console.WriteLine("Can't set non-existant field: " + this.name);
					}
					break;
				case "comment":
					block.setCommentText(value);
					break;
				case "collapsed":
					block.setCollapsed(value == "true");
					break;
				case "disabled":
					block.setDisabled(value == "true");
					break;
				case "inline":
					block.setInputsInline(value == "true");
					break;
				case "mutation":
					var oldMutation = "";
					if (true/*block.mutationToDom*/) {
						var oldMutationDom = block.mutationToDom();
						oldMutation = oldMutationDom != null ? oldMutationDom.ToString() : Xml.domToText(oldMutationDom);
					}
					if (true/*block.domToMutation*/) {
						value = value != null ? value : "<mutation></mutation>";
						var dom = Xml.textToDom("<xml>" + value + "</xml>");
						block.domToMutation((Element)dom.FirstChild);
					}
					Events.fire(new Events.Change(
						block, "mutation", null, oldMutation, value));
					break;
				default:
					Console.WriteLine("Unknown change type: " + this.element);
					break;
				}
			}
		}

		public class Move : Abstract
		{
			/// <summary>
			/// UUID of old parent block. Undefined if it was a top level block.
			/// </summary>
			public string oldParentId;

			/// <summary>
			/// Name of input on old parent. Undefined if it was a top level block or parent's next block.
			/// </summary>
			public string oldInputName;

			/// <summary>
			/// X and Y coordinates if it was a top level block. Undefined if it had a parent.
			/// </summary>
			public goog.math.Coordinate oldCoordinate;

			/// <summary>
			/// UUID of new parent block. Undefined if it is a top level block.
			/// </summary>
			public string newParentId;

			/// <summary>
			/// Name of input on new parent. Undefined if it is a top level block or parent's next block.
			/// </summary>
			public string newInputName;

			/// <summary>
			/// X and Y coordinates if it is a top level block. Undefined if it has a parent.
			/// </summary>
			public goog.math.Coordinate newCoordinate;

			/// <summary>
			/// Class for a block move ev.  Created before the move.
			/// </summary>
			/// <param name="block">The moved block.  Null for a blank ev.</param>
			public Move(Block block)
				: base(block, Events.MOVE)
			{
				if (block == null) {
					return;  // Blank ev to be populated by fromJson.
				}
				var location = this.currentLocation_();
				this.oldParentId = location.parentId;
				this.oldInputName = location.inputName;
				this.oldCoordinate = location.coordinate;
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public override EventJsonData toJson()
			{
				var json = base.toJson();
				if (this.newParentId != null) {
					json.newParentId = this.newParentId;
				}
				if (this.newInputName != null) {
					json.newInputName = this.newInputName;
				}
				if (this.newCoordinate != null) {
					json.newCoordinate = System.Math.Round(this.newCoordinate.x) + "," +
						System.Math.Round(this.newCoordinate.y);
				}
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public override void fromJson(EventJsonData json)
			{
				base.fromJson(json);
				this.newParentId = json.newParentId.ToString();
				this.newInputName = json.newInputName.ToString();
				if (json.newCoordinate != null) {
					var xy = (json.newCoordinate.ToString()).Split(",");
					this.newCoordinate =
						new goog.math.Coordinate(Script.ParseFloat(xy[0]), Script.ParseFloat(xy[1]));
				}
			}

			/**
			 * Record the block's new location.  Called after the move.
			 */
			public void recordNew()
			{
				var location = this.currentLocation_();
				this.newParentId = location.parentId;
				this.newInputName = location.inputName;
				this.newCoordinate = location.coordinate;
			}

			class Location
			{
				public string parentId;
				public string inputName;
				public goog.math.Coordinate coordinate;

				public Location()
				{
				}

				public Location(string parentId, string inputName, goog.math.Coordinate coordinate)
				{
					this.parentId = parentId;
					this.inputName = inputName;
					this.coordinate = coordinate;
				}
			}

			/// <summary>
			/// Returns the parentId and input if the block is connected,
			/// or the XY location if disconnected.
			/// </summary>
			/// <returns>Collection of location info.</returns>
			private Location currentLocation_()
			{
				var workspace = Workspace.getById(this.workspaceId);
				var block = workspace.getBlockById(this.blockId);
				var location = new Location();
				var parent = block.getParent();
				if (parent != null) {
					location.parentId = parent.id;
					var input = parent.getInputWithBlock(block);
					if (input != null) {
						location.inputName = input.name;
					}
				}
				else {
					location.coordinate = block.getRelativeToSurfaceXY();
				}
				return location;
			}

			/// <summary>
			/// Does this event record any change of state?
			/// </summary>
			/// <returns>True if something changed.</returns>
			public override bool isNull()
			{
				return this.oldParentId == this.newParentId &&
					this.oldInputName == this.newInputName &&
					goog.math.Coordinate.equals(this.oldCoordinate, this.newCoordinate);
			}

			/// <summary>
			/// Run a move event.
			/// </summary>
			/// <param name="forward">True if run forward, false if run backward (undo).</param>
			public override void run(bool forward)
			{
				var workspace = Workspace.getById(this.workspaceId);
				var block = workspace.getBlockById(this.blockId);
				if (block == null) {
					Console.WriteLine("Can't move non-existant block: " + this.blockId);
					return;
				}
				var parentId = forward ? this.newParentId : this.oldParentId;
				var inputName = forward ? this.newInputName : this.oldInputName;
				var coordinate = forward ? this.newCoordinate : this.oldCoordinate;
				Block parentBlock = null;
				if (parentId != null) {
					parentBlock = workspace.getBlockById(parentId);
					if (parentBlock == null) {
						Console.WriteLine("Can't connect to non-existant block: " + parentId);
						return;
					}
				}
				if (block.getParent() != null) {
					block.unplug();
				}
				if (coordinate != null) {
					var xy = block.getRelativeToSurfaceXY();
					block.moveBy(coordinate.x - xy.x, coordinate.y - xy.y);
				}
				else {
					var blockConnection = block.outputConnection != null ? block.outputConnection : block.previousConnection;
					Connection parentConnection = null;
					if (inputName != null) {
						var input = parentBlock.getInput(inputName);
						if (input != null) {
							parentConnection = input.connection;
						}
					}
					else if (blockConnection.type == Core.PREVIOUS_STATEMENT) {
						parentConnection = parentBlock.nextConnection;
					}
					if (parentConnection != null) {
						blockConnection.connect(parentConnection);
					}
					else {
						Console.WriteLine("Can't connect to non-existant input: " + inputName);
					}
				}
			}
		}

		public class Ui : Abstract
		{
			/// <summary>
			/// One of 'selected', 'category', 'click', 'commentOpen', 'mutatorOpen', 'warningOpen'
			/// </summary>
			public string element;

			/// <summary>
			/// Original value.
			/// </summary>
			public object oldValue;

			/// <summary>
			/// Changed value.
			/// </summary>
			public object newValue;

			/// <summary>
			/// Class for a UI ev.
			/// </summary>
			/// <param name="block">The affected block.</param>
			/// <param name="element">One of 'selected', 'comment', 'mutator', etc.</param>
			/// <param name="oldValue">Previous value of element.</param>
			/// <param name="newValue">New value of element.</param>
			public Ui(Block block, string element, string oldValue, string newValue)
				: base(block, Events.UI)
			{
				this.element = element;
				this.oldValue = oldValue;
				this.newValue = newValue;
			}

			/// <summary>
			/// Encode the event as JSON.
			/// </summary>
			/// <returns>JSON representation.</returns>
			public override EventJsonData toJson()
			{
				var json = base.toJson();
				json.element = this.element;
				if (this.newValue != Script.Undefined) {
					json.newValue = this.newValue;
				}
				return json;
			}

			/// <summary>
			/// Decode the JSON event.
			/// </summary>
			/// <param name="json">JSON representation.</param>
			public override void fromJson(EventJsonData json)
			{
				base.fromJson(json);
				this.element = json.element.ToString();
				this.newValue = json.newValue.ToString();
			}
		}
	}

	public class EventJsonData
	{
		public string type;
		public string blockId;
		public string group;
		public string xml;
		public JsArray<string> ids;
		public string element;
		public string name;
		public object newValue;
		public string newParentId;
		public string newInputName;
		public string newCoordinate;
	}
}
