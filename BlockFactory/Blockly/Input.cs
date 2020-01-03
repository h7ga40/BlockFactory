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
using System;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Input
	{
		public int type;
		public string name;
		public BlockSvg sourceBlock_;
		public Connection connection;
		public JsArray<Field> fieldRow = new JsArray<Field>();

		internal double renderWidth;
		internal double renderHeight;
		internal double fieldWidth;

		/// <summary>
		/// Class for an input with an optional field.
		/// </summary>
		/// <param name="type">The type of the input.</param>
		/// <param name="name">Language-neutral identifier which may used to find this
		/// input again.</param>
		/// <param name="block">The block containing this input.</param>
		/// <param name="connection">Optional connection for this input.</param>
		public Input(int type, string name, Block block, Connection connection)
		{
			this.type = type;
			this.name = name;
			this.sourceBlock_ = (BlockSvg)block;
			this.connection = connection;
		}

		/// <summary>
		/// Alignment of input's fields (left, right or centre).
		/// </summary>
		public double align = Core.ALIGN_LEFT;

		/// <summary>
		/// Is the input visible?
		/// </summary>
		public bool visible_ = true;

		/// <summary>
		/// Add an item to the end of the input's field row.
		/// </summary>
		/// <param name="field">Something to add as a field.</param>
		/// <param name="opt_name">Language-neutral identifier which may used to find
		/// this field again.Should be unique to the host block.</param>
		/// <returns>The input being append to (to allow chaining).</returns>
		public Input appendField(Union<string, Field> field_, string opt_name = null)
		{
			// Empty string, Null or undefined generates no field, unless field is named.
			if (field_ == null && opt_name == null) {
				return this;
			}
			Field field;
			// Generate a FieldLabel when given a plain text field.
			if (field_.Is<string>()) {
				field = new FieldLabel(field_.As<string>());
			}
			else {
				field = field_.As<Field>();
			}
			field.setSourceBlock(this.sourceBlock_);
			if (this.sourceBlock_.rendered) {
				field.init();
			}
			field.name = opt_name;

			if (field.prefixField != null) {
				// Add any prefix.
				this.appendField(field.prefixField);
			}
			// Add the field to the field row.
			this.fieldRow.Push(field);
			if (field.suffixField != null) {
				// Add any suffix.
				this.appendField(field.suffixField);
			}

			if (this.sourceBlock_.rendered) {
				this.sourceBlock_.render();
				// Adding a field will cause the block to change shape.
				this.sourceBlock_.bumpNeighbours_();
			}
			return this;
		}

		/// <summary>
		/// Add an item to the end of the input's field row.
		/// </summary>
		/// <param name="field">Something to add as a field.</param>
		/// <param name="opt_name">Language-neutral identifier which may used to find
		/// this field again.Should be unique to the host block.</param>
		/// <returns>The input being append to (to allow chaining).</returns>
		public Input appendTitle(Union<string, Field> field, string opt_name = null)
		{
			Console.WriteLine("Deprecated call to appendTitle, use appendField instead.");
			return this.appendField(field, opt_name);
		}

		/// <summary>
		/// Remove a field from this input.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		public void removeField(string name)
		{
			int i = 0;
			foreach (var field in this.fieldRow) {
				if (field.name == name) {
					field.dispose();
					this.fieldRow.Splice(i, 1);
					if (this.sourceBlock_.rendered) {
						this.sourceBlock_.render();
						// Removing a field will cause the block to change shape.
						this.sourceBlock_.bumpNeighbours_();
					}
					return;
				}
				i++;
			}
			goog.asserts.fail("Field \"%s\" not found.", name);
		}

		/// <summary>
		/// Gets whether this input is visible or not.
		/// </summary>
		/// <returns>True if visible.</returns>
		public bool isVisible()
		{
			return this.visible_;
		}

		/// <summary>
		/// Sets whether this input is visible or not.
		/// Used to collapse/uncollapse a block.
		/// </summary>
		/// <param name="visible">True if visible.</param>
		/// <returns>List of blocks to render.</returns>
		public Block[] setVisible(bool visible)
		{
			var renderList = new Block[0];
			if (this.visible_ == visible) {
				return renderList;
			}
			this.visible_ = visible;

			var display = visible ? Display.Block : Display.None;
			foreach (var field in this.fieldRow) {
				field.setVisible(visible);
			}
			if (this.connection != null) {
				// Has a connection.
				if (visible) {
					renderList = ((RenderedConnection)this.connection).unhideAll();
				}
				else {
					((RenderedConnection)this.connection).hideAll();
				}
				var child = (BlockSvg)this.connection.targetBlock();
				if (child != null) {
					child.getSvgRoot().style.Display = display;
					if (!visible) {
						child.rendered = false;
					}
				}
			}
			return renderList;
		}

		/// <summary>
		/// Change a connection's compatibility.
		/// </summary>
		/// <param name="check">Compatible value type or
		/// list of value types.Null if all types are compatible.</param>
		/// <returns>The input being modified (to allow chaining).</returns>
		public Input setCheck(Union<string, string[]> check)
		{
			if (this.connection == null) {
				throw new Exception("This input does not have a connection.");
			}
			this.connection.setCheck(check);
			return this;
		}

		/// <summary>
		/// Change the alignment of the connection's field(s).
		/// </summary>
		/// <param name="align">align One of Blockly.ALIGN_LEFT, ALIGN_CENTRE, ALIGN_RIGHT.
		/// In RTL mode directions are reversed, and ALIGN_RIGHT aligns to the left.</param>
		/// <returns>The input being modified (to allow chaining).</returns>
		public Input setAlign(double align)
		{
			this.align = align;
			if (this.sourceBlock_.rendered) {
				this.sourceBlock_.render();
			}
			return this;
		}

		/// <summary>
		/// Initialize the fields on this input.
		/// </summary>
		public void init()
		{
			if (!this.sourceBlock_.workspace.rendered) {
				return;  // Headless blocks don't need fields initialized.
			}
			foreach (var i in this.fieldRow) {
				i.init();
			}
		}

		/// <summary>
		/// Sever all links to this input.
		/// </summary>
		public void dispose()
		{
			foreach (var field in this.fieldRow) {
				field.dispose();
			}
			if (this.connection != null) {
				this.connection.dispose();
			}
			this.sourceBlock_ = null;
		}
	}
}
