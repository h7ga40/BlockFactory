// Copyright 2005 The Closure Library Authors. All Rights Reserved.
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


using System;
using Bridge;
using Bridge.Html5;
/**
* @fileoverview A base class for event objects.
*
*/
namespace goog
{
	public static partial class events
	{
		public class Event
		{
			/// <summary>
			/// Event type.
			/// </summary>
			public string type;
			/// <summary>
			/// TODO(tbreisacher): The type should probably be
			/// EventTarget|goog.events.EventTarget.
			/// 
			/// Target of the event.
			/// </summary>
			public object target;
			/// <summary>
			/// Object that had the listener attached.
			/// </summary>
			public object currentTarget;
			/// <summary>
			/// Whether to cancel the event in internal capture/bubble processing for IE.
			/// </summary>
			internal bool propagationStopped_;

			/// <summary>
			/// Whether the default action has been prevented.
			/// This is a property to match the W3C specification at
			/// {@link http://www.w3.org/TR/DOM-Level-3-Events/
			/// #events-event-type-defaultPrevented}.
			/// Must be treated as read-only outside the class.
			/// </summary>
			public bool defaultPrevented;

			/// <summary>
			/// Return value for in internal capture/bubble processing for IE.
			/// </summary>
			internal bool returnValue_ = true;
#if true
			/// <summary>
			/// Whether control was pressed at time of event.
			/// </summary>
			public bool ctrlKey;

			/// <summary>
			/// Whether alt was pressed at time of event.
			/// </summary>
			public bool altKey;

			/// <summary>
			/// Whether shift was pressed at time of event.
			/// </summary>
			public bool shiftKey;

			/// <summary>
			/// Whether the meta key was pressed at time of event.
			/// </summary>
			public bool metaKey;

			/// <summary>
			/// History state object, only set for PopState events where it's a copy of the
			/// state object provided to pushState or replaceState.
			/// </summary>
			public object state;

			/// <summary>
			/// Whether the default platform modifier key was pressed at time of event.
			/// (This is control for all platforms except Mac, where it's Meta.)
			/// </summary>
			public bool platformModifierKey;
#endif
			/// <summary>
			/// A base class for event objects, so that they can support preventDefault and
			/// stopPropagation.
			/// 
			/// @suppress {underscore} Several properties on this class are technically
			///     public, but referencing these properties outside this package is strongly
			///     discouraged.
			/// 
			/// </summary>
			/// <param name="type">Event Type.</param>
			/// <param name="opt_target">Reference to the object that is the target of
			/// this event. It has to implement the {@code EventTarget} interface
			/// declared at {@link http://developer.mozilla.org/en/DOM/EventTarget}.
			/// </param>
			public Event(string type, object opt_target = null)
			{
				this.type = type;
				this.target = opt_target;
			}

			public Event(Bridge.Html5.Event e, object opt_target = null)
			{
				this.type = e.Type;
				this.target = opt_target;
			}

			/// <summary>
			/// Stops event propagation.
			/// </summary>
			public virtual void stopPropagation()
			{
				this.propagationStopped_ = true;
			}

			/// <summary>
			/// Prevents the default action, for example a link redirecting to a url.
			/// </summary>
			public virtual void preventDefault()
			{
				this.defaultPrevented = true;
				this.returnValue_ = false;
			}

			/// <summary>
			/// Stops the propagation of the event. It is equivalent to
			/// {@code e.stopPropagation()}, but can be used as the callback argument of
			/// {@link goog.events.listen} without declaring another function.
			/// </summary>
			/// <param name="e">An event.</param>
			public static void stopPropagation(Event e)
			{
				e.stopPropagation();
			}

			/// <summary>
			/// Prevents the default action. It is equivalent to
			/// {@code e.preventDefault()}, but can be used as the callback argument of
			/// {@link goog.events.listen} without declaring another function.
			/// </summary>
			/// <param name="e">An event.</param>
			public static void preventDefault(Event e)
			{
				e.preventDefault();
			}
		}
	}
}
