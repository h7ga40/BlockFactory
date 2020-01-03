// Copyright 2006 The Closure Library Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/**
 * @fileoverview Definition of the Popup class.
 *
 * @author eae@google.com (Emil A Eklund)
 * @see ../demos/popup.html
 */

using System;
using Bridge;
using Bridge.Html5;
using goog.positioning;

namespace goog.ui
{
	public class Popup : PopupBase
	{
		/// <summary>
		/// Corner of the popup to used in the positioning algorithm.
		/// </summary>
		private goog.positioning.Corner popupCorner_ = goog.positioning.Corner.TOP_START;

		/// <summary>
		/// Positioning helper object.
		/// </summary>
		private goog.positioning.AbstractPosition position_;

		/// <summary>
		/// The Popup class provides functionality for displaying an absolutely
		/// positioned element at a particular location in the window. It's designed to
		/// be used as the foundation for building controls like a menu or tooltip. The
		/// Popup class includes functionality for displaying a Popup near adjacent to
		/// an anchor element.
		///
		/// This works cross browser and thus does not use IE's createPopup feature
		/// which supports extending outside the edge of the brower window.
		/// </summary>
		/// <param name="opt_element">A DOM element for the popup.</param>
		/// <param name="opt_position">A positioning helper
		///     object.</param>
		public Popup(HTMLElement opt_element = null, goog.positioning.AbstractPosition opt_position = null)
			: base(opt_element)
		{
			position_ = opt_position;
		}

		/// <summary>
		/// Margin for the popup used in positioning algorithms.
		/// </summary>
		private goog.math.Box margin_;


		/// <summary>
		/// Returns the corner of the popup to used in the positioning algorithm.
		/// </summary>
		/// <returns>The popup corner used for positioning.</returns>
		public goog.positioning.Corner getPinnedCorner()
		{
			return this.popupCorner_;
		}


		/// <summary>
		/// Sets the corner of the popup to used in the positioning algorithm.
		/// </summary>
		/// <param name="corner"> The popup corner used for
		/// positioning.</param>
		public void setPinnedCorner(goog.positioning.Corner corner)
		{
			this.popupCorner_ = corner;
			if (this.isVisible()) {
				this.reposition();
			}
		}


		/// <summary>
		/// </summary>
		/// <returns>The position helper object
		///     associated with the popup.</returns>
		public goog.positioning.AbstractPosition getPosition()
		{
			return this.position_;
		}

		/// <summary>
		/// Sets the position helper object associated with the popup.
		/// </summary>
		/// <param name="position"> A position helper object.</param>
		public void setPosition(goog.positioning.AbstractPosition position)
		{
			this.position_ = position;
			if (this.isVisible()) {
				this.reposition();
			}
		}

		/// <summary>
		/// Returns the margin to place around the popup.
		/// </summary>
		/// <returns>The margin.</returns>
		public goog.math.Box getMargin()
		{
			return this.margin_;
		}


		/// <summary>
		/// Sets the margin to place around the popup.
		/// </summary>
		/// <param name="arg1"> Top value or Box.</param>
		/// <param name="opt_arg2"> Right value.</param>
		/// <param name="opt_arg3"> Bottom value.</param>
		/// <param name="opt_arg4"> Left value.</param>
		public void setMargin(Union<goog.math.Box, double> arg1, double opt_arg2 = Double.NaN, double opt_arg3 = Double.NaN, double opt_arg4 = Double.NaN)
		{
			if (arg1 == null || arg1.Is<goog.math.Box>()) {
				this.margin_ = arg1.As<goog.math.Box>();
			}
			else {
				this.margin_ = new goog.math.Box(
					arg1.As<double>(),
					opt_arg2,
					opt_arg3,
					opt_arg4);
			}
			if (this.isVisible()) {
				this.reposition();
			}
		}

		/// <summary>
		/// Repositions the popup according to the current state.
		/// </summary>
		public override void reposition()
		{
			if (this.position_ == null) {
				return;
			}

			var hideForPositioning = !this.isVisible() &&
				this.getType() != goog.ui.PopupBase.Type.MOVE_OFFSCREEN;
			var el = this.getElement();
			if (hideForPositioning) {
				el.Style.Visibility = Visibility.Hidden;
				goog.style.setElementShown(el, true);
			}

			this.position_.reposition(el, this.popupCorner_, this.margin_);

			if (hideForPositioning) {
				// NOTE(eae): The visibility property is reset to "visible" by the show_
				// method in PopupBase. Resetting it here causes flickering in some
				// situations, even if set to visible after the display property has been
				// set to none by the call below.
				goog.style.setElementShown(el, false);
			}
		}
	}
}
