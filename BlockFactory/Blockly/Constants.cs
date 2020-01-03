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
 * @fileoverview Blockly constants.
 * @author fenichel@google.com (Rachel Fenichel)
 */

namespace Blockly
{
	public partial class Core
	{
		/// <summary>
		/// Number of pixels the mouse must move before a drag starts.
		/// </summary>
		public const int DRAG_RADIUS = 5;

		/// <summary>
		/// Maximum misalignment between connections for them to snap together.
		/// </summary>
		public const int SNAP_RADIUS = 20;

		/// <summary>
		/// Delay in ms between trigger and bumping unconnected block out of alignment.
		/// </summary>
		public const int BUMP_DELAY = 250;

		/// <summary>
		/// Number of characters to truncate a collapsed block to.
		/// </summary>
		public const int COLLAPSE_CHARS = 30;

		/// <summary>
		/// Length in ms for a touch to become a long press.
		/// </summary>
		public const int LONGPRESS = 750;

		/// <summary>
		/// Prevent a sound from playing if another sound preceded it within this many
		/// miliseconds.
		/// </summary>
		public const int SOUND_LIMIT = 100;

		/// <summary>
		/// The richness of block colours, regardless of the hue.
		/// Must be in the range of 0 (inclusive) to 1 (exclusive).
		/// </summary>
		public static double HSV_SATURATION = 0.45;

		/// <summary>
		/// The intensity of block colours, regardless of the hue.
		/// Must be in the range of 0 (inclusive) to 1 (exclusive).
		/// </summary>
		public static double HSV_VALUE = 0.65;

		public class Sprite
		{
			internal int width;
			internal int height;
			internal string url;
		}

		/// <summary>
		/// Sprited icons and images.
		/// </summary>
		public static Sprite SPRITE = new Sprite {
			width = 96,
			height = 124,
			url = "sprites.png"
		};

		/// <summary>
		/// Required name space for SVG elements.
		/// </summary>
		public static readonly string SVG_NS = "http://www.w3.org/2000/svg";

		/// <summary>
		/// Required name space for HTML elements.
		/// </summary>
		public static readonly string HTML_NS = "http://www.w3.org/1999/xhtml";

		/// <summary>
		/// ENUM for a right-facing value input.  E.g. 'set item to' or 'return'.
		/// </summary>
		public const int INPUT_VALUE = 1;

		/// <summary>
		/// ENUM for a left-facing value output.  E.g. 'random fraction'.
		/// </summary>
		public const int OUTPUT_VALUE = 2;

		/// <summary>
		/// ENUM for a down-facing block stack.  E.g. 'if-do' or 'else'.
		/// </summary>
		public const int NEXT_STATEMENT = 3;

		/// <summary>
		/// ENUM for an up-facing block stack.  E.g. 'break out of loop'.
		/// </summary>
		public const int PREVIOUS_STATEMENT = 4;

		/// <summary>
		/// ENUM for an dummy input.  Used to add field(s) with no input.
		/// </summary>
		public const int DUMMY_INPUT = 5;

		/// <summary>
		/// ENUM for left alignment.
		/// </summary>
		public const int ALIGN_LEFT = -1;

		/// <summary>
		/// ENUM for centre alignment.
		/// </summary>
		public const int ALIGN_CENTRE = 0;

		/// <summary>
		/// ENUM for right alignment.
		/// </summary>
		public const int ALIGN_RIGHT = 1;

		/// <summary>
		/// ENUM for no drag operation.
		/// </summary>
		public const int DRAG_NONE = 0;

		/// <summary>
		/// ENUM for inside the sticky DRAG_RADIUS.
		/// </summary>
		public const int DRAG_STICKY = 1;

		/// <summary>
		/// ENUM for inside the non-sticky DRAG_RADIUS, for differentiating between
		/// clicks and drags.
		/// </summary>
		public const int DRAG_BEGIN = 1;

		/// <summary>
		/// ENUM for freely draggable (outside the DRAG_RADIUS, if one applies).
		/// </summary>
		public const int DRAG_FREE = 2;

		/// <summary>
		/// Lookup table for determining the opposite type of a connection.
		/// </summary>
		public static int[] OPPOSITE_TYPE = {
			0,
			OUTPUT_VALUE,
			INPUT_VALUE,
			PREVIOUS_STATEMENT,
			NEXT_STATEMENT
		};

		/// <summary>
		/// ENUM for toolbox and flyout at top of screen.
		/// </summary>
		public const int TOOLBOX_AT_TOP = 0;

		/// <summary>
		/// ENUM for toolbox and flyout at bottom of screen.
		/// </summary>
		public const int TOOLBOX_AT_BOTTOM = 1;

		/// <summary>
		/// ENUM for toolbox and flyout at left of screen.
		/// </summary>
		public const int TOOLBOX_AT_LEFT = 2;

		/// <summary>
		/// ENUM for toolbox and flyout at right of screen.
		/// </summary>
		public const int TOOLBOX_AT_RIGHT = 3;
	}
}
