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
 * @fileoverview Methods for graphically rendering a block as SVG.
 * @author fenichel@google.com (Rachel Fenichel)
 */
using Bridge;
using System;
using System.Collections.Generic;

namespace Blockly
{
	public partial class BlockSvg
	{
		/// <summary>
		/// Horizontal space between elements.
		/// </summary>
		public const int SEP_SPACE_X = 10;

		/// <summary>
		/// Vertical space between elements.
		/// </summary>
		public const int SEP_SPACE_Y = 10;

		/// <summary>
		/// Vertical padding around inline elements.
		/// </summary>
		public const int INLINE_PADDING_Y = 5;

		/// <summary>
		/// Minimum height of a block.
		/// </summary>
		public const int MIN_BLOCK_Y = 25;

		/// <summary>
		/// Height of horizontal puzzle tab.
		/// </summary>
		public const int TAB_HEIGHT = 20;

		/// <summary>
		/// Width of vertical tab (inc left margin).
		/// </summary>
		public const int NOTCH_WIDTH = 30;

		/// <summary>
		/// Rounded corner radius.
		/// </summary>
		public const int CORNER_RADIUS = 8;

		/// <summary>
		/// Do blocks with no previous or output connections have a 'hat' on top?
		/// </summary>
		public static bool START_HAT = false;

		/// <summary>
		/// Height of the top hat.
		/// </summary>
		public const int START_HAT_HEIGHT = 15;

		/// <summary>
		/// Width of horizontal puzzle tab.
		/// </summary>
		public const int TAB_WIDTH = 8;

		/// <summary>
		/// Path of the top hat's curve's highlight in LTR.
		/// </summary>
		public static readonly string START_HAT_PATH = "c 30,-" +
			BlockSvg.START_HAT_HEIGHT + " 70,-" +
			BlockSvg.START_HAT_HEIGHT + " 100,0";

		/// <summary>
		/// Path of the top hat's curve's highlight in LTR.
		/// </summary>
		public static readonly string START_HAT_HIGHLIGHT_LTR =
			"c 17.8,-9.2 45.3,-14.9 75,-8.7 M 100.5,0.5";

		/// <summary>
		/// Path of the top hat's curve's highlight in RTL.
		/// </summary>
		public static readonly string START_HAT_HIGHLIGHT_RTL =
			"m 25,-8.7 c 29.7,-6.2 57.2,-0.5 75,8.7";
		/// <summary>
		/// Distance from shape edge to intersect with a curved corner at 45 degrees.
		/// Applies to highlighting on around the inside of a curve.
		/// </summary>
		public static readonly double DISTANCE_45_INSIDE = (1.0 - System.Math.Sqrt(1.0 / 2.0)) *
			(BlockSvg.CORNER_RADIUS - 0.5) + 0.5;

		/// <summary>
		/// Distance from shape edge to intersect with a curved corner at 45 degrees.
		/// Applies to highlighting on around the outside of a curve.
		/// </summary>
		public static readonly double DISTANCE_45_OUTSIDE = (1.0 - System.Math.Sqrt(1.0 / 2.0)) *
			(BlockSvg.CORNER_RADIUS + 0.5) - 0.5;

		/// <summary>
		/// SVG path for drawing next/previous notch from left to right.
		/// </summary>
		public const string NOTCH_PATH_LEFT = "l 6,4 3,0 6,-4";

		/// <summary>
		/// SVG path for drawing next/previous notch from left to right with
		/// </summary>
		public const string NOTCH_PATH_LEFT_HIGHLIGHT = "l 6,4 3,0 6,-4";

		/// <summary>
		/// SVG path for drawing next/previous notch from right to left.
		/// </summary>
		public const string NOTCH_PATH_RIGHT = "l -6,4 -3,0 -6,-4";

		/// <summary>
		/// SVG path for drawing jagged teeth at the end of collapsed blocks.
		/// </summary>
		public const string JAGGED_TEETH = "l 8,0 0,4 8,4 -16,8 8,4";

		/// <summary>
		/// Height of SVG path for jagged teeth at the end of collapsed blocks.
		/// </summary>
		public const int JAGGED_TEETH_HEIGHT = 20;

		/// <summary>
		/// Width of SVG path for jagged teeth at the end of collapsed blocks.
		/// </summary>
		public const int JAGGED_TEETH_WIDTH = 15;

		/// <summary>
		/// SVG path for drawing a horizontal puzzle tab from top to bottom.
		/// </summary>
		public static readonly string TAB_PATH_DOWN = "v 5 c 0,10 -" + BlockSvg.TAB_WIDTH +
			",-8 -" + BlockSvg.TAB_WIDTH + ",7.5 s " +
			BlockSvg.TAB_WIDTH + ",-2.5 " + BlockSvg.TAB_WIDTH + ",7.5";

		/// <summary>
		/// SVG path for drawing a horizontal puzzle tab from top to bottom with
		/// highlighting from the upper-right.
		/// </summary>
		public static readonly string TAB_PATH_DOWN_HIGHLIGHT_RTL = "v 6.5 m -" +
			(BlockSvg.TAB_WIDTH * 0.97) + ",3 q -" +
			(BlockSvg.TAB_WIDTH * 0.05) + ",10 " +
			(BlockSvg.TAB_WIDTH * 0.3) + ",9.5 m " +
			(BlockSvg.TAB_WIDTH * 0.67) + ",-1.9 v 1.4";

		/// <summary>
		/// SVG start point for drawing the top-left corner.
		/// </summary>
		public static readonly string TOP_LEFT_CORNER_START =
			"m 0," + BlockSvg.CORNER_RADIUS;

		/// <summary>
		/// SVG start point for drawing the top-left corner's highlight in RTL.
		/// </summary>
		public static readonly string TOP_LEFT_CORNER_START_HIGHLIGHT_RTL =
			"m " + BlockSvg.DISTANCE_45_INSIDE + "," +
			BlockSvg.DISTANCE_45_INSIDE;

		/// <summary>
		/// SVG start point for drawing the top-left corner's highlight in LTR.
		/// </summary>
		public static readonly string TOP_LEFT_CORNER_START_HIGHLIGHT_LTR =
			"m 0.5," + (BlockSvg.CORNER_RADIUS - 0.5);

		/// <summary>
		/// SVG path for drawing the rounded top-left corner.
		/// </summary>
		public static readonly string TOP_LEFT_CORNER =
			"A " + BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS + " 0 0,1 " +
			BlockSvg.CORNER_RADIUS + ",0";

		/// <summary>
		/// SVG path for drawing the highlight on the rounded top-left corner.
		/// </summary>
		public static readonly string TOP_LEFT_CORNER_HIGHLIGHT =
			"A " + (BlockSvg.CORNER_RADIUS - 0.5) + "," +
			(BlockSvg.CORNER_RADIUS - 0.5) + " 0 0,1 " +
			BlockSvg.CORNER_RADIUS + ",0.5";

		/// <summary>
		/// SVG path for drawing the top-left corner of a statement input.
		/// Includes the top notch, a horizontal space, and the rounded inside corner.
		/// </summary>
		public static readonly string INNER_TOP_LEFT_CORNER =
			BlockSvg.NOTCH_PATH_RIGHT + " h -" +
			(BlockSvg.NOTCH_WIDTH - 15 - BlockSvg.CORNER_RADIUS) +
			" a " + BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS + " 0 0,0 -" +
			BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS;

		/// <summary>
		/// SVG path for drawing the bottom-left corner of a statement input.
		/// Includes the rounded inside corner.
		/// </summary>
		public static readonly string INNER_BOTTOM_LEFT_CORNER =
			"a " + BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS + " 0 0,0 " +
			BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS;

		/// <summary>
		/// SVG path for drawing highlight on the bottom-left corner of a statement
		/// input in RTL.
		/// </summary>
		public static readonly string INNER_TOP_LEFT_CORNER_HIGHLIGHT_RTL =
			"a " + BlockSvg.CORNER_RADIUS + "," +
			BlockSvg.CORNER_RADIUS + " 0 0,0 " +
			(-BlockSvg.DISTANCE_45_OUTSIDE - 0.5) + "," +
			(BlockSvg.CORNER_RADIUS -
			BlockSvg.DISTANCE_45_OUTSIDE);

		/// <summary>
		/// SVG path for drawing highlight on the bottom-left corner of a statement
		/// input in RTL.
		/// </summary>
		public static readonly string INNER_BOTTOM_LEFT_CORNER_HIGHLIGHT_RTL =
			"a " + (BlockSvg.CORNER_RADIUS + 0.5) + "," +
			(BlockSvg.CORNER_RADIUS + 0.5) + " 0 0,0 " +
			(BlockSvg.CORNER_RADIUS + 0.5) + "," +
			(BlockSvg.CORNER_RADIUS + 0.5);

		/// <summary>
		/// SVG path for drawing highlight on the bottom-left corner of a statement
		/// input in LTR.
		/// </summary>
		public static readonly string INNER_BOTTOM_LEFT_CORNER_HIGHLIGHT_LTR =
			"a " + (BlockSvg.CORNER_RADIUS + 0.5) + "," +
			(BlockSvg.CORNER_RADIUS + 0.5) + " 0 0,0 " +
			(BlockSvg.CORNER_RADIUS -
			BlockSvg.DISTANCE_45_OUTSIDE) + "," +
			(BlockSvg.DISTANCE_45_OUTSIDE + 0.5);

		public bool rendered;

		/// <summary>
		/// Render the block.
		/// Lays out and reflows a block based on its contents and settings.
		/// </summary>
		/// <param name="opt_bubble">If false, just render this block.
		/// If true, also render block's parent, grandparent, etc.  Defaults to true.</param>
		public void render(bool opt_bubble = true)
		{
			Field.startCache();
			this.rendered = true;

			var cursorX = BlockSvg.SEP_SPACE_X;
			if (this.RTL) {
				cursorX = -cursorX;
			}
			// Move the icons into position.
			var icons = this.getIcons();
			for (var i = 0; i < icons.Length; i++) {
				cursorX = icons[i].renderIcon(cursorX);
			}
			cursorX += this.RTL ?
				BlockSvg.SEP_SPACE_X : -BlockSvg.SEP_SPACE_X;
			// If there are no icons, cursorX will be 0, otherwise it will be the
			// width that the first label needs to move over by.

			var inputRows = this.renderCompute_(cursorX);
			this.renderDraw_(cursorX, inputRows);
			this.renderMoveConnections_();

			if (opt_bubble) {
				// Render all blocks above this one (propagate a reflow).
				var parentBlock = (BlockSvg)this.getParent();
				if (parentBlock != null) {
					parentBlock.render(true);
				}
				else {
					// Top-most block.  Fire an event to allow scrollbars to resize.
					((WorkspaceSvg)this.workspace).resizeContents();
				}
			}
			Field.stopCache();
		}

		/// <summary>
		/// Render a list of fields starting at the specified location.
		/// </summary>
		/// <param name="fieldList">List of fields.</param>
		/// <param name="cursorX">X-coordinate to start the fields.</param>
		/// <param name="cursorY">Y-coordinate to start the fields.</param>
		/// <returns>X-coordinate of the end of the field row (plus a gap).</returns>
		private double renderFields_(Field[] fieldList, double cursorX, double cursorY)
		{
			cursorY += BlockSvg.INLINE_PADDING_Y;
			if (this.RTL) {
				cursorX = -cursorX;
			}
			foreach (var field in fieldList) {
				var root = field.getSvgRoot();
				if (root == null) {
					continue;
				}
				if (this.RTL) {
					cursorX -= field.renderSep + field.renderWidth;
					root.SetAttribute("transform",
						"translate(" + cursorX + "," + cursorY + ")");
					if (field.renderWidth != 0) {
						cursorX -= BlockSvg.SEP_SPACE_X;
					}
				}
				else {
					root.SetAttribute("transform",
						"translate(" + (cursorX + field.renderSep) + "," + cursorY + ")");
					if (field.renderWidth != 0) {
						cursorX += field.renderSep + field.renderWidth +
							BlockSvg.SEP_SPACE_X;
					}
				}
			}
			return this.RTL ? -cursorX : cursorX;
		}

		class InputRow : List<object>
		{
			internal int type;
			internal double rightEdge;
			internal double height;
			internal bool thicker;
			internal double statementEdge;
			internal bool hasValue;
			internal bool hasStatement;
			internal bool hasDummy;

			public int Length { get { return Count; } }

			internal void Push(object row)
			{
				Add(row);
			}
		}

		/// <summary>
		/// Computes the height and widths for each row and field.
		/// </summary>
		/// <param name="iconWidth">Offset of first row due to icons.</param>
		/// <returns>2D array of objects, each containing
		/// position information.</returns>
		private InputRow renderCompute_(double iconWidth)
		{
			var inputList = this.inputList;
			var inputRows = new InputRow();
			inputRows.rightEdge = iconWidth + BlockSvg.SEP_SPACE_X * 2;
			if (this.previousConnection != null || this.nextConnection != null) {
				inputRows.rightEdge = System.Math.Max(inputRows.rightEdge,
					BlockSvg.NOTCH_WIDTH + BlockSvg.SEP_SPACE_X);
			}
			var fieldValueWidth = 0.0;  // Width of longest external value field.
			var fieldStatementWidth = 0.0;  // Width of longest statement field.
			var hasValue = false;
			var hasStatement = false;
			var hasDummy = false;
			int? lastType = null;
			var isInline = this.getInputsInline() && !this.isCollapsed();
			int i = 0;
			foreach (var input in inputList) {
			InputRow row;
				if (!input.isVisible()) {
					i++;
					continue;
				}
				if (!isInline || !lastType.HasValue ||
					lastType == Core.NEXT_STATEMENT ||
					input.type == Core.NEXT_STATEMENT) {
					// Create new row.
					lastType = input.type;
					row = new InputRow();
					if (isInline && input.type != Core.NEXT_STATEMENT) {
						row.type = BlockSvg.INLINE;
					}
					else {
						row.type = input.type;
					}
					row.height = 0.0;
					inputRows.Push(row);
				}
				else {
					row = (InputRow)inputRows[inputRows.Length - 1];
				}
				row.Push(input);

				// Compute minimum input size.
				input.renderHeight = BlockSvg.MIN_BLOCK_Y;
				// The width is currently only needed for inline value inputs.
				if (isInline && input.type == Core.INPUT_VALUE) {
					input.renderWidth = BlockSvg.TAB_WIDTH +
						BlockSvg.SEP_SPACE_X * 1.25;
				}
				else {
					input.renderWidth = 0;
				}
				// Expand input size if there is a connection.
				if (input.connection != null && input.connection.isConnected()) {
					var linkedBlock = (BlockSvg)input.connection.targetBlock();
					var bBox = linkedBlock.getHeightWidth();
					input.renderHeight = System.Math.Max(input.renderHeight, bBox.height);
					input.renderWidth = System.Math.Max(input.renderWidth, bBox.width);
				}
				// Blocks have a one pixel shadow that should sometimes overhang.
				if (!isInline && i == inputList.Length - 1) {
					// Last value input should overhang.
					input.renderHeight--;
				}
				else if (!isInline && input.type == Core.INPUT_VALUE &&
				  inputList[i + 1] != null && inputList[i + 1].type == Core.NEXT_STATEMENT) {
					// Value input above statement input should overhang.
					input.renderHeight--;
				}

				row.height = System.Math.Max(row.height, input.renderHeight);
				input.fieldWidth = 0;
				if (inputRows.Length == 1) {
					// The first row gets shifted to accommodate any icons.
					input.fieldWidth += this.RTL ? -iconWidth : iconWidth;
				}
				var previousFieldEditable = false;
				int j = 0;
				foreach (var field in input.fieldRow) {
					if (j != 0) {
						input.fieldWidth += BlockSvg.SEP_SPACE_X;
					}
					// Get the dimensions of the field.
					var fieldSize = field.getSize();
					field.renderWidth = fieldSize.width;
					field.renderSep = (previousFieldEditable && field.EDITABLE) ?
						BlockSvg.SEP_SPACE_X : 0;
					input.fieldWidth += field.renderWidth + field.renderSep;
					row.height = System.Math.Max(row.height, fieldSize.height);
					previousFieldEditable = field.EDITABLE;
					j++;
				}

				if (row.type != BlockSvg.INLINE) {
					if (row.type == Core.NEXT_STATEMENT) {
						hasStatement = true;
						fieldStatementWidth = System.Math.Max(fieldStatementWidth, input.fieldWidth);
					}
					else {
						if (row.type == Core.INPUT_VALUE) {
							hasValue = true;
						}
						else if (row.type == Core.DUMMY_INPUT) {
							hasDummy = true;
						}
						fieldValueWidth = System.Math.Max(fieldValueWidth, input.fieldWidth);
					}
				}
				i++;
			}

			// Make inline rows a bit thicker in order to enclose the values.
			foreach (InputRow row in inputRows) {
				row.thicker = false;
				if (row.type == BlockSvg.INLINE) {
					foreach (Input input in row) {
						if (input.type == Core.INPUT_VALUE) {
							row.height += 2 * BlockSvg.INLINE_PADDING_Y;
							row.thicker = true;
							break;
						}
					}
				}
			}

			// Compute the statement edge.
			// This is the width of a block where statements are nested.
			inputRows.statementEdge = 2 * BlockSvg.SEP_SPACE_X +
				fieldStatementWidth;
			// Compute the preferred right edge.  Inline blocks may extend beyond.
			// This is the width of the block where external inputs connect.
			if (hasStatement) {
				inputRows.rightEdge = System.Math.Max(inputRows.rightEdge,
					inputRows.statementEdge + BlockSvg.NOTCH_WIDTH);
			}
			if (hasValue) {
				inputRows.rightEdge = System.Math.Max(inputRows.rightEdge, fieldValueWidth +
					BlockSvg.SEP_SPACE_X * 2 + BlockSvg.TAB_WIDTH);
			}
			else if (hasDummy) {
				inputRows.rightEdge = System.Math.Max(inputRows.rightEdge, fieldValueWidth +
					BlockSvg.SEP_SPACE_X * 2);
			}

			inputRows.hasValue = hasValue;
			inputRows.hasStatement = hasStatement;
			inputRows.hasDummy = hasDummy;
			return inputRows;
		}

		internal bool startHat_;
		private bool squareTopLeftCorner_;
		private bool squareBottomLeftCorner_;

		/// <summary>
		/// Draw the path of the block.
		/// Move the fields to the correct locations.
		/// </summary>
		/// <param name="iconWidth">Offset of first row due to icons.</param>
		/// <param name="inputRows">2D array of objects, each
		/// containing position information.</param>
		private void renderDraw_(double iconWidth, InputRow inputRows)
		{
			this.startHat_ = false;
			// Reset the height to zero and let the rendering process add in
			// portions of the block height as it goes. (e.g. hats, inputs, etc.)
			this.height = 0;
			// Should the top and bottom left corners be rounded or square?
			if (this.outputConnection != null) {
				this.squareTopLeftCorner_ = true;
				this.squareBottomLeftCorner_ = true;
			}
			else {
				this.squareTopLeftCorner_ = false;
				this.squareBottomLeftCorner_ = false;
				// If this block is in the middle of a stack, square the corners.
				if (this.previousConnection != null) {
					var prevBlock = this.previousConnection.targetBlock();
					if (prevBlock != null && prevBlock.getNextBlock() == this) {
						this.squareTopLeftCorner_ = true;
					}
				}
				else if (BlockSvg.START_HAT) {
					// No output or previous connection.
					this.squareTopLeftCorner_ = true;
					this.startHat_ = true;
					this.height += BlockSvg.START_HAT_HEIGHT;
					inputRows.rightEdge = System.Math.Max(inputRows.rightEdge, 100);
				}
				var nextBlock = this.getNextBlock();
				if (nextBlock != null) {
					this.squareBottomLeftCorner_ = true;
				}
			}

			// Assemble the block's path.
			var steps = new JsArray<string>();
			var inlineSteps = new JsArray<string>();
			// The highlighting applies to edges facing the upper-left corner.
			// Since highlighting is a two-pixel wide border, it would normally overhang
			// the edge of the block by a pixel. So undersize all measurements by a pixel.
			var highlightSteps = new JsArray<string>();
			var highlightInlineSteps = new JsArray<string>();

			this.renderDrawTop_(steps, highlightSteps, inputRows.rightEdge);
			var cursorY = this.renderDrawRight_(steps, highlightSteps, inlineSteps,
				highlightInlineSteps, inputRows, iconWidth);
			this.renderDrawBottom_(steps, highlightSteps, cursorY);
			this.renderDrawLeft_(steps, highlightSteps);

			var pathString = steps.Join(" ") + "\n" + inlineSteps.Join(" ");
			this.svgPath_.SetAttribute("d", pathString);
			this.svgPathDark_.SetAttribute("d", pathString);
			pathString = highlightSteps.Join(" ") + "\n" + highlightInlineSteps.Join(" ");
			this.svgPathLight_.SetAttribute("d", pathString);
			if (this.RTL) {
				// Mirror the block's path.
				this.svgPath_.SetAttribute("transform", "scale(-1 1)");
				this.svgPathLight_.SetAttribute("transform", "scale(-1 1)");
				this.svgPathDark_.SetAttribute("transform", "translate(1,1) scale(-1 1)");
			}
		}

		/// <summary>
		/// Update all of the connections on this block with the new locations calculated
		/// in renderCompute.  Also move all of the connected blocks based on the new
		/// connection locations.
		/// </summary>
		private void renderMoveConnections_()
		{
			var blockTL = this.getRelativeToSurfaceXY();
			// Don't tighten previous or output connecitons because they are inferior
			// connections.
			if (this.previousConnection != null) {
				this.previousConnection.moveToOffset(blockTL);
			}
			if (this.outputConnection != null) {
				this.outputConnection.moveToOffset(blockTL);
			}

			for (var i = 0; i < this.inputList.Length; i++) {
				var conn = (RenderedConnection)this.inputList[i].connection;
				if (conn != null) {
					conn.moveToOffset(blockTL);
					if (conn.isConnected()) {
						conn.tighten_();
					}
				}
			}

			if (this.nextConnection != null) {
				this.nextConnection.moveToOffset(blockTL);
				if (this.nextConnection.isConnected()) {
					this.nextConnection.tighten_();
				}
			}
		}

		/// <summary>
		/// Render the top edge of the block.
		/// </summary>
		/// <param name="steps">Path of block outline.</param>
		/// <param name="highlightSteps">Path of block highlights.</param>
		/// <param name="rightEdge">Minimum width of block.</param>
		private void renderDrawTop_(JsArray<string> steps, JsArray<string> highlightSteps, double rightEdge)
		{
			// Position the cursor at the top-left starting point.
			if (this.squareTopLeftCorner_) {
				steps.Push("m 0,0");
				highlightSteps.Push("m 0.5,0.5");
				if (this.startHat_) {
					steps.Push(BlockSvg.START_HAT_PATH);
					highlightSteps.Push(this.RTL ?
						BlockSvg.START_HAT_HIGHLIGHT_RTL :
						BlockSvg.START_HAT_HIGHLIGHT_LTR);
				}
			}
			else {
				steps.Push(BlockSvg.TOP_LEFT_CORNER_START);
				highlightSteps.Push(this.RTL ?
					BlockSvg.TOP_LEFT_CORNER_START_HIGHLIGHT_RTL :
					BlockSvg.TOP_LEFT_CORNER_START_HIGHLIGHT_LTR);
				// Top-left rounded corner.
				steps.Push(BlockSvg.TOP_LEFT_CORNER);
				highlightSteps.Push(BlockSvg.TOP_LEFT_CORNER_HIGHLIGHT);
			}

			// Top edge.
			if (this.previousConnection != null) {
				steps.Push("H", (BlockSvg.NOTCH_WIDTH - 15).ToString());
				highlightSteps.Push("H", (BlockSvg.NOTCH_WIDTH - 15).ToString());
				steps.Push(BlockSvg.NOTCH_PATH_LEFT);
				highlightSteps.Push(BlockSvg.NOTCH_PATH_LEFT_HIGHLIGHT);

				var connectionX = (this.RTL ?
					-BlockSvg.NOTCH_WIDTH : BlockSvg.NOTCH_WIDTH);
				this.previousConnection.setOffsetInBlock(connectionX, 0);
			}
			steps.Push("H", rightEdge.ToString());
			highlightSteps.Push("H", (rightEdge - 0.5).ToString());
			this.width = rightEdge;
		}

		/// <summary>
		/// Render the right edge of the block.
		/// </summary>
		/// <param name="steps">Path of block outline.</param>
		/// <param name="highlightSteps">Path of block highlights.</param>
		/// <param name="inlineSteps">Inline block outlines.</param>
		/// <param name="highlightInlineSteps">Inline block highlights.</param>
		/// <param name="inputRows">2D array of objects, each
		/// containing position information.</param>
		/// <param name="iconWidth">Offset of first row due to icons.</param>
		/// <returns>Height of block.</returns>
		private double renderDrawRight_(JsArray<string> steps, JsArray<string> highlightSteps,
			JsArray<string> inlineSteps, JsArray<string> highlightInlineSteps, InputRow inputRows, double iconWidth)
		{
			double cursorX;
			var cursorY = 0.0;
			double connectionX, connectionY;
			int y = 0;
			foreach (InputRow row in inputRows) {
				cursorX = BlockSvg.SEP_SPACE_X;
				if (y == 0) {
					cursorX += this.RTL ? -iconWidth : iconWidth;
				}
				highlightSteps.Push("M", (inputRows.rightEdge - 0.5) + "," +
					(cursorY + 0.5));
				if (this.isCollapsed()) {
					// Jagged right edge.
					var input = (Input)row[0];
					var fieldX = cursorX;
					var fieldY = cursorY;
					this.renderFields_(input.fieldRow, fieldX, fieldY);
					steps.Push(BlockSvg.JAGGED_TEETH);
					highlightSteps.Push("h 8");
					var remainder = row.height - BlockSvg.JAGGED_TEETH_HEIGHT;
					steps.Push("v", remainder.ToString());
					if (this.RTL) {
						highlightSteps.Push("v 3.9 l 7.2,3.4 m -14.5,8.9 l 7.3,3.5");
						highlightSteps.Push("v", (remainder - 0.7).ToString());
					}
					this.width += BlockSvg.JAGGED_TEETH_WIDTH;
				}
				else if (row.type == BlockSvg.INLINE) {
					// Inline inputs.
					foreach (Input input in row) {
						var fieldX = cursorX;
						var fieldY = cursorY;
						if (row.thicker) {
							// Lower the field slightly.
							fieldY += BlockSvg.INLINE_PADDING_Y;
						}
						// TODO: Align inline field rows (left/right/centre).
						cursorX = this.renderFields_(input.fieldRow, fieldX, fieldY);
						if (input.type != Core.DUMMY_INPUT) {
							cursorX += input.renderWidth + BlockSvg.SEP_SPACE_X;
						}
						if (input.type == Core.INPUT_VALUE) {
							inlineSteps.Push("M", (cursorX - BlockSvg.SEP_SPACE_X) +
											 "," + (cursorY + BlockSvg.INLINE_PADDING_Y));
							inlineSteps.Push("h", (BlockSvg.TAB_WIDTH - 2 -
											 input.renderWidth).ToString());
							inlineSteps.Push(BlockSvg.TAB_PATH_DOWN);
							inlineSteps.Push("v", (input.renderHeight + 1 -
												  BlockSvg.TAB_HEIGHT).ToString());
							inlineSteps.Push("h", (input.renderWidth + 2 -
											 BlockSvg.TAB_WIDTH).ToString());
							inlineSteps.Push("z");
							if (this.RTL) {
								// Highlight right edge, around back of tab, and bottom.
								highlightInlineSteps.Push("M",
									(cursorX - BlockSvg.SEP_SPACE_X - 2.5 +
									 BlockSvg.TAB_WIDTH - input.renderWidth) + "," +
									(cursorY + BlockSvg.INLINE_PADDING_Y + 0.5));
								highlightInlineSteps.Push(
									BlockSvg.TAB_PATH_DOWN_HIGHLIGHT_RTL);
								highlightInlineSteps.Push("v",
									(input.renderHeight - BlockSvg.TAB_HEIGHT + 2.5).ToString());
								highlightInlineSteps.Push("h",
									(input.renderWidth - BlockSvg.TAB_WIDTH + 2).ToString());
							}
							else {
								// Highlight right edge, bottom.
								highlightInlineSteps.Push("M",
									(cursorX - BlockSvg.SEP_SPACE_X + 0.5) + "," +
									(cursorY + BlockSvg.INLINE_PADDING_Y + 0.5));
								highlightInlineSteps.Push("v", (input.renderHeight + 1).ToString());
								highlightInlineSteps.Push("h", (BlockSvg.TAB_WIDTH - 2 -
															   input.renderWidth).ToString());
								// Short highlight glint at bottom of tab.
								highlightInlineSteps.Push("M",
									(cursorX - input.renderWidth - BlockSvg.SEP_SPACE_X +
									 0.9) + "," + (cursorY + BlockSvg.INLINE_PADDING_Y +
									 BlockSvg.TAB_HEIGHT - 0.7));
								highlightInlineSteps.Push("l",
									(BlockSvg.TAB_WIDTH * 0.46) + ",-2.1");
							}
							// Create inline input connection.
							if (this.RTL) {
								connectionX = -cursorX -
									BlockSvg.TAB_WIDTH + BlockSvg.SEP_SPACE_X +
									input.renderWidth + 1;
							}
							else {
								connectionX = cursorX +
									BlockSvg.TAB_WIDTH - BlockSvg.SEP_SPACE_X -
									input.renderWidth - 1;
							}
							connectionY = cursorY + BlockSvg.INLINE_PADDING_Y + 1;
							((RenderedConnection)input.connection).setOffsetInBlock(connectionX, connectionY);
						}
					}

					cursorX = System.Math.Max(cursorX, inputRows.rightEdge);
					this.width = System.Math.Max(this.width, cursorX);
					steps.Push("H", cursorX.ToString());
					highlightSteps.Push("H", (cursorX - 0.5).ToString());
					steps.Push("v", row.height.ToString());
					if (this.RTL) {
						highlightSteps.Push("v", (row.height - 1).ToString());
					}
				}
				else if (row.type == Core.INPUT_VALUE) {
					// External input.
					var input = (Input)row[0];
					var fieldX = cursorX;
					var fieldY = cursorY;
					if (input.align != Core.ALIGN_LEFT) {
						var fieldRightX = inputRows.rightEdge - input.fieldWidth -
							BlockSvg.TAB_WIDTH - 2 * BlockSvg.SEP_SPACE_X;
						if (input.align == Core.ALIGN_RIGHT) {
							fieldX += fieldRightX;
						}
						else if (input.align == Core.ALIGN_CENTRE) {
							fieldX += fieldRightX / 2;
						}
					}
					this.renderFields_(input.fieldRow, fieldX, fieldY);
					steps.Push(BlockSvg.TAB_PATH_DOWN);
					var v = row.height - BlockSvg.TAB_HEIGHT;
					steps.Push("v", v.ToString());
					if (this.RTL) {
						// Highlight around back of tab.
						highlightSteps.Push(BlockSvg.TAB_PATH_DOWN_HIGHLIGHT_RTL);
						highlightSteps.Push("v", (v + 0.5).ToString());
					}
					else {
						// Short highlight glint at bottom of tab.
						highlightSteps.Push("M", (inputRows.rightEdge - 5) + "," +
							(cursorY + BlockSvg.TAB_HEIGHT - 0.7));
						highlightSteps.Push("l", (BlockSvg.TAB_WIDTH * 0.46) +
							",-2.1");
					}
					// Create external input connection.
					connectionX = this.RTL ? -inputRows.rightEdge - 1 :
						inputRows.rightEdge + 1;
					((RenderedConnection)input.connection).setOffsetInBlock(connectionX, cursorY);
					if (input.connection.isConnected()) {
						this.width = System.Math.Max(this.width, inputRows.rightEdge +
							((BlockSvg)input.connection.targetBlock()).getHeightWidth().width -
							BlockSvg.TAB_WIDTH + 1);
					}
				}
				else if (row.type == Core.DUMMY_INPUT) {
					// External naked field.
					var input = (Input)row[0];
					var fieldX = cursorX;
					var fieldY = cursorY;
					if (input.align != Core.ALIGN_LEFT) {
						var fieldRightX = inputRows.rightEdge - input.fieldWidth -
							2 * BlockSvg.SEP_SPACE_X;
						if (inputRows.hasValue) {
							fieldRightX -= BlockSvg.TAB_WIDTH;
						}
						if (input.align == Core.ALIGN_RIGHT) {
							fieldX += fieldRightX;
						}
						else if (input.align == Core.ALIGN_CENTRE) {
							fieldX += fieldRightX / 2;
						}
					}
					this.renderFields_(input.fieldRow, fieldX, fieldY);
					steps.Push("v", row.height.ToString());
					if (this.RTL) {
						highlightSteps.Push("v", (row.height - 1).ToString());
					}
				}
				else if (row.type == Core.NEXT_STATEMENT) {
					// Nested statement.
					var input = (Input)row[0];
					if (y == 0) {
						// If the first input is a statement stack, add a small row on top.
						steps.Push("v", BlockSvg.SEP_SPACE_Y.ToString());
						if (this.RTL) {
							highlightSteps.Push("v", (BlockSvg.SEP_SPACE_Y - 1).ToString());
						}
						cursorY += BlockSvg.SEP_SPACE_Y;
					}
					var fieldX = cursorX;
					var fieldY = cursorY;
					if (input.align != Core.ALIGN_LEFT) {
						var fieldRightX = inputRows.statementEdge - input.fieldWidth -
							2 * BlockSvg.SEP_SPACE_X;
						if (input.align == Core.ALIGN_RIGHT) {
							fieldX += fieldRightX;
						}
						else if (input.align == Core.ALIGN_CENTRE) {
							fieldX += fieldRightX / 2;
						}
					}
					this.renderFields_(input.fieldRow, fieldX, fieldY);
					cursorX = inputRows.statementEdge + BlockSvg.NOTCH_WIDTH;
					steps.Push("H", cursorX.ToString());
					steps.Push(BlockSvg.INNER_TOP_LEFT_CORNER);
					steps.Push("v", (row.height - 2 * BlockSvg.CORNER_RADIUS).ToString());
					steps.Push(BlockSvg.INNER_BOTTOM_LEFT_CORNER);
					steps.Push("H", inputRows.rightEdge.ToString());
					if (this.RTL) {
						highlightSteps.Push("M",
							(cursorX - BlockSvg.NOTCH_WIDTH +
							 BlockSvg.DISTANCE_45_OUTSIDE) +
							"," + (cursorY + BlockSvg.DISTANCE_45_OUTSIDE));
						highlightSteps.Push(
							BlockSvg.INNER_TOP_LEFT_CORNER_HIGHLIGHT_RTL);
						highlightSteps.Push("v",
							(row.height - 2 * BlockSvg.CORNER_RADIUS).ToString());
						highlightSteps.Push(
							BlockSvg.INNER_BOTTOM_LEFT_CORNER_HIGHLIGHT_RTL);
						highlightSteps.Push("H", (inputRows.rightEdge - 0.5).ToString());
					}
					else {
						highlightSteps.Push("M",
							(cursorX - BlockSvg.NOTCH_WIDTH +
							 BlockSvg.DISTANCE_45_OUTSIDE) + "," +
							(cursorY + row.height - BlockSvg.DISTANCE_45_OUTSIDE));
						highlightSteps.Push(
							BlockSvg.INNER_BOTTOM_LEFT_CORNER_HIGHLIGHT_LTR);
						highlightSteps.Push("H", (inputRows.rightEdge - 0.5).ToString());
					}
					// Create statement connection.
					connectionX = this.RTL ? -cursorX : cursorX + 1;
					((RenderedConnection)input.connection).setOffsetInBlock(connectionX, cursorY + 1);

					if (input.connection.isConnected()) {
						this.width = System.Math.Max(this.width, inputRows.statementEdge +
							((BlockSvg)input.connection.targetBlock()).getHeightWidth().width);
					}
					if (y == inputRows.Length - 1 ||
						((InputRow)inputRows[y + 1]).type == Core.NEXT_STATEMENT) {
						// If the final input is a statement stack, add a small row underneath.
						// Consecutive statement stacks are also separated by a small divider.
						steps.Push("v", BlockSvg.SEP_SPACE_Y.ToString());
						if (this.RTL) {
							highlightSteps.Push("v", (BlockSvg.SEP_SPACE_Y - 1).ToString());
						}
						cursorY += BlockSvg.SEP_SPACE_Y;
					}
				}
				cursorY += row.height;
				y++;
			}
			if (inputRows.Length == 0) {
				cursorY = BlockSvg.MIN_BLOCK_Y;
				steps.Push("V", cursorY.ToString());
				if (this.RTL) {
					highlightSteps.Push("V", (cursorY - 1).ToString());
				}
			}
			return cursorY;
		}

		/// <summary>
		/// Render the bottom edge of the block.
		/// </summary>
		/// <param name="steps">Path of block outline.</param>
		/// <param name="highlightSteps">Path of block highlights.</param>
		/// <param name="cursorY">Height of block.</param>
		private void renderDrawBottom_(JsArray<string> steps, JsArray<string> highlightSteps, double cursorY)
		{
			this.height += cursorY + 1;  // Add one for the shadow.
			if (this.nextConnection != null) {
				steps.Push("H", (BlockSvg.NOTCH_WIDTH + (this.RTL ? 0.5 : -0.5)) +
					" " + BlockSvg.NOTCH_PATH_RIGHT);
				// Create next block connection.
				double connectionX;
				if (this.RTL) {
					connectionX = -BlockSvg.NOTCH_WIDTH;
				}
				else {
					connectionX = BlockSvg.NOTCH_WIDTH;
				}
				this.nextConnection.setOffsetInBlock(connectionX, cursorY + 1);
				this.height += 4;  // Height of tab.
			}

			// Should the bottom-left corner be rounded or square?
			if (this.squareBottomLeftCorner_) {
				steps.Push("H 0");
				if (!this.RTL) {
					highlightSteps.Push("M", "0.5," + (cursorY - 0.5));
				}
			}
			else {
				steps.Push("H", BlockSvg.CORNER_RADIUS.ToString());
				steps.Push("a", BlockSvg.CORNER_RADIUS + "," +
						   BlockSvg.CORNER_RADIUS + " 0 0,1 -" +
						   BlockSvg.CORNER_RADIUS + ",-" +
						   BlockSvg.CORNER_RADIUS);
				if (!this.RTL) {
					highlightSteps.Push("M", BlockSvg.DISTANCE_45_INSIDE + "," +
						(cursorY - BlockSvg.DISTANCE_45_INSIDE));
					highlightSteps.Push("A", (BlockSvg.CORNER_RADIUS - 0.5) + "," +
						(BlockSvg.CORNER_RADIUS - 0.5) + " 0 0,1 " +
						"0.5," + (cursorY - BlockSvg.CORNER_RADIUS));
				}
			}
		}

		/// <summary>
		/// Render the left edge of the block.
		/// </summary>
		/// <param name="steps">Path of block outline.</param>
		/// <param name="highlightSteps">Path of block highlights.</param>
		private void renderDrawLeft_(JsArray<string> steps, JsArray<string> highlightSteps)
		{
			if (this.outputConnection != null) {
				// Create output connection.
				this.outputConnection.setOffsetInBlock(0, 0);
				steps.Push("V", BlockSvg.TAB_HEIGHT.ToString());
				steps.Push("c 0,-10 -" + BlockSvg.TAB_WIDTH + ",8 -" +
					BlockSvg.TAB_WIDTH + ",-7.5 s " + BlockSvg.TAB_WIDTH +
					",2.5 " + BlockSvg.TAB_WIDTH + ",-7.5");
				if (this.RTL) {
					highlightSteps.Push("M", (BlockSvg.TAB_WIDTH * -0.25) + ",8.4");
					highlightSteps.Push("l", (BlockSvg.TAB_WIDTH * -0.45) + ",-2.1");
				}
				else {
					highlightSteps.Push("V", (BlockSvg.TAB_HEIGHT - 1.5).ToString());
					highlightSteps.Push("m", (BlockSvg.TAB_WIDTH * -0.92) +
										",-0.5 q " + (BlockSvg.TAB_WIDTH * -0.19) +
										",-5.5 0,-11");
					highlightSteps.Push("m", (BlockSvg.TAB_WIDTH * 0.92) +
										",1 V 0.5 H 1");
				}
				this.width += BlockSvg.TAB_WIDTH;
			}
			else if (!this.RTL) {
				if (this.squareTopLeftCorner_) {
					// Statement block in a stack.
					highlightSteps.Push("V", "0.5");
				}
				else {
					highlightSteps.Push("V", BlockSvg.CORNER_RADIUS.ToString());
				}
			}
			steps.Push("z");
		}
	}
}
