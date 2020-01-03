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
 * @fileoverview Touch handling for Blockly.
 * @author fenichel@google.com (Rachel Fenichel)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class Touch
	{
		/// <summary>
		/// Which touch events are we currently paying attention to?
		/// </summary>
		private static string touchIdentifier_;

		/// <summary>
		/// Wrapper function called when a touch mouseUp occurs during a drag operation.
		/// </summary>
		internal static JsArray<EventWrapInfo> onTouchUpWrapper_;

		/// <summary>
		/// The TOUCH_MAP lookup dictionary specifies additional touch events to fire,
		/// in conjunction with mouse events.
		/// </summary>
		public static Dictionary<string, object[]> TOUCH_MAP = new Dictionary<string, object[]>();

		static Touch()
		{
			if (goog.events.BrowserFeature.TOUCH_ENABLED) {
				Touch.TOUCH_MAP.Add("mousedown", new[] { "touchstart" });
				Touch.TOUCH_MAP.Add("mousemove", new[] { "touchmove" });
				Touch.TOUCH_MAP.Add("mouseup", new[] { "touchend", "touchcancel" });
			}
		}

		/// <summary>
		/// Clear the touch identifier that tracks which touch stream to pay attention
		/// to.  This ends the current drag/gesture and allows other pointers to be
		/// captured.
		/// </summary>
		public static void clearTouchIdentifier()
		{
			Touch.touchIdentifier_ = null;
		}

		/// <summary>
		/// Decide whether Blockly should handle or ignore this event.
		/// Mouse and touch events require special checks because we only want to deal
		/// with one touch stream at a time.  All other events should always be handled.
		/// </summary>
		/// <param name="e">The event to check.</param>
		/// <returns>True if this event should be passed through to the
		/// registered handler; false if it should be blocked.</returns>
		public static bool shouldHandleEvent(Event e)
		{
			return !Touch.isMouseOrTouchEvent(e) ||
				Touch.checkTouchIdentifier(e);
		}

		/// <summary>
		/// Check whether the touch identifier on the event matches the current saved
		/// identifier.  If there is no identifier, that means it's a mouse event and
		/// we'll use the identifier "mouse".  This means we won't deal well with
		/// multiple mice being used at the same time.  That seems okay.
		/// If the current identifier was unset, save the identifier from the
		/// event.  This starts a drag/gesture, during which touch events with other
		/// identifiers will be silently ignored.
		/// </summary>
		/// <param name="e">Mouse event or touch event.</param>
		/// <returns>Whether the identifier on the event matches the current
		/// saved identifier.</returns>
		public static bool checkTouchIdentifier<T>(T e) where T : Event
		{
			var identifier = (e.ChangedTouches != null && e.ChangedTouches[0] != null &&
				/*e.ChangedTouches[0].Identifier != Script.Undefined &&*/
				e.ChangedTouches[0].Identifier != 0) ?
				e.ChangedTouches[0].Identifier.ToString() : "mouse";

			// if (Blockly.touchIdentifier_ )is insufficient because android touch
			// identifiers may be zero.
			if ((object)Touch.touchIdentifier_ != Script.Undefined &&
				Touch.touchIdentifier_ != null) {
				// We're already tracking some touch/mouse event.  Is this from the same
				// source?
				return Touch.touchIdentifier_ == identifier;
			}
			if (e.Type == "mousedown" || e.Type == "touchstart") {
				// No identifier set yet, and this is the start of a drag.  Set it and
				// return.
				Touch.touchIdentifier_ = identifier;
				return true;
			}
			// There was no identifier yet, but this wasn't a start event so we're going
			// to ignore it.  This probably means that another drag finished while this
			// pointer was down.
			return false;
		}

		/// <summary>
		/// Set an event's clientX and clientY from its first changed touch.  Use this to
		/// make a touch event work in a mouse event handler.
		/// </summary>
		/// <param name="e">A touch event.</param>
		internal static void setClientFromTouch<T>(T e) where T : Event
		{
			if (e.Type.StartsWith("touch")) {
				// Map the touch event's properties to the event.
				var touchPoint = e.ChangedTouches[0];
				e.ClientX = touchPoint.ClientX;
				e.ClientY = touchPoint.ClientY;
			}
		}

		/// <summary>
		/// Check whether a given event is a mouse or touch event.
		/// </summary>
		/// <param name="e">An event.</param>
		/// <returns>if it is a mouse or touch event; false otherwise.</returns>
		public static bool isMouseOrTouchEvent(Event e)
		{
			return e.Type.StartsWith("touch") || e.Type.StartsWith("mouse");
		}

		/// <summary>
		/// Split an event into an array of events, one per changed touch or mouse
		/// point.
		/// </summary>
		/// <param name="e">A mouse event or a touch event with one or more changed
		/// touches.</param>
		/// <returns>An array of mouse or touch events.  Each touch
		/// event will have exactly one changed touch.</returns>
		internal static T[] splitEventByTouches<T>(T e) where T : Event
		{
			var events = new JsArray<T>();
			if (e.ChangedTouches != null) {
				for (var i = 0; i < e.ChangedTouches.Length; i++) {
					var newEvent = Event.Create<T>(e.Type);
					newEvent["type"] = e["type"];
					newEvent["changedTouches"] = e.ChangedTouches[i].instance;
					newEvent["target"] = e["target"];
					newEvent["stopPropagation"] = e["stopPropagation"];
					newEvent["preventDefault"] = e["preventDefault"];
					events[i] = newEvent;
				}
			}
			else {
				events.Push(e);
			}
			return events;
		}
	}
}
