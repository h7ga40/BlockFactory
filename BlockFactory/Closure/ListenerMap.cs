// Copyright 2013 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview A map of listeners that provides utility functions to
 * deal with listeners on an event target. Used by
 * {@code goog.events.EventTarget}.
 *
 * WARNING: Do not use this class from outside goog.events package.
 *
 * @visibility {//closure/goog/bin/sizetests:__pkg__}
 * @visibility {//closure/goog/events:__pkg__}
 * @visibility {//closure/goog/labs/events:__pkg__}
 */
using System;
using System.Collections.Generic;
using Bridge;

namespace goog
{
	public static partial class events
	{
		public class ListenerMap
		{
			public Union<Bridge.Html5.EventTarget, Listenable> src;
			public Dictionary<string, JsArray<Listener>> listeners { get; } =
				new Dictionary<string, JsArray<Listener>>();
			private int typeCount_;

			public ListenerMap(Union<Bridge.Html5.EventTarget, Listenable> src)
			{
				this.src = src;
				typeCount_ = 0;
			}

			/// <summary>
			/// </summary>
			/// <returns>The count of event types in this map that actually
			/// have registered listeners.</returns>
			public int getTypeCount()
			{
				return this.typeCount_;
			}

			/// <summary>
			/// Adds an event listener. A listener can only be added once to an
			/// object and if it is added again the key for the listener is
			/// returned.
			/// 
			/// Note that a one-off listener will not change an existing listener,
			/// if any. On the other hand a normal listener will change existing
			/// one-off listener to become a normal listener.
			/// </summary>
			/// <param name="type">The listener event type.</param>
			/// <param name="listener">This listener callback method.</param>
			/// <param name="callOnce">Whether the listener is a one-off listener.</param>
			/// <param name="opt_useCapture">The capture mode of the listener.</param>
			/// <param name="opt_listenerScope">Object in whose scope to call the
			/// listener.</param>
			/// <returns>Unique key for the listener.</returns>
			public Listener add(string type, Delegate listener, bool callOnce, bool opt_useCapture, object opt_listenerScope)
			{
				var typeStr = type.ToString();
				if (!this.listeners.TryGetValue(typeStr, out var listenerArray)) {
					listenerArray = this.listeners[typeStr] = new JsArray<Listener>();
					this.typeCount_++;
				}

				Listener listenerObj;
				var index = goog.events.ListenerMap.findListenerIndex_(
					listenerArray, listener, opt_useCapture, opt_listenerScope);
				if (index > -1) {
					listenerObj = listenerArray[index];
					if (!callOnce) {
						// Ensure that, if there is an existing callOnce listener, it is no
						// longer a callOnce listener.
						listenerObj.callOnce = false;
					}
				}
				else {
					listenerObj = new goog.events.Listener(
						listener, null, this.src, typeStr, !!opt_useCapture, opt_listenerScope);
					listenerObj.callOnce = callOnce;
					listenerArray.Push(listenerObj);
				}
				return listenerObj;
			}

			/// <summary>
			/// Removes a matching listener.
			/// </summary>
			/// <param name="type">The listener event type.</param>
			/// <param name="listener">This listener callback method</param>
			/// <param name="opt_useCapture">The capture mode of the listener.</param>
			/// <param name="opt_listenerScope">Object in whose scope to call the
			/// listener.</param>
			/// <returns>Whether any listener was removed.</returns>
			public bool remove(string type, Delegate listener, bool opt_useCapture = false,
				object opt_listenerScope = null)
			{
				var typeStr = type.ToString();
				if (!this.listeners.ContainsKey(typeStr)) {
					return false;
				}

				var listenerArray = this.listeners[typeStr];
				var index = goog.events.ListenerMap.findListenerIndex_(
					listenerArray, listener, opt_useCapture, opt_listenerScope);
				if (index > -1) {
					var listenerObj = listenerArray[index];
					listenerObj.markAsRemoved();
					listenerArray.Remove(listenerObj);
					if (listenerArray.Length == 0) {
						this.listeners.Remove(typeStr);
						this.typeCount_--;
					}
					return true;
				}
				return false;
			}

			/// <summary>
			/// Removes the given listener object.
			/// </summary>
			/// <param name="listener">The listener to remove.</param>
			/// <returns>Whether the listener is removed.</returns>
			public bool removeByKey(Listener listener)
			{
				var type = listener.type;
				if (!this.listeners.ContainsKey(type)) {
					return false;
				}

				var removed = this.listeners[type].Remove(listener);
				if (removed) {
					/** @type {!goog.events.Listener} */
					(listener).markAsRemoved();
					if (this.listeners[type].Length == 0) {
						this.listeners.Remove(type);
						this.typeCount_--;
					}
				}
				return removed;
			}

			/// <summary>
			/// Gets the goog.events.ListenableKey for the event or null if no such
			/// listener is in use.
			/// </summary>
			/// <param name="type">The type of the listener to retrieve.</param>
			/// <param name="listener">The listener function to get.</param>
			/// <param name="capture">Whether the listener is a capturing listener.</param>
			/// <param name="opt_listenerScope">Object in whose scope to call the
			/// listener.</param>
			/// <returns>the found listener or null if not found.</returns>
			public Listener getListener(string type, Delegate listener, bool capture, object opt_listenerScope)
			{
				var i = -1;
				if (this.listeners.TryGetValue(type, out var listenerArray)) {
					i = goog.events.ListenerMap.findListenerIndex_(
						listenerArray, listener, capture, opt_listenerScope);
				}
				return i > -1 ? listenerArray[i] : null;
			}

			/// <summary>
			/// Finds the index of a matching goog.events.Listener in the given
			/// listenerArray.
			/// </summary>
			/// <param name="listenerArray">Array of listener.</param>
			/// <param name="listener">The listener function.</param>
			/// <param name="opt_useCapture">The capture flag for the listener.</param>
			/// <param name="opt_listenerScope">The listener scope.</param>
			/// <returns>The index of the matching listener within the listenerArray.</returns>
			private static int findListenerIndex_(JsArray<Listener> listenerArray, Delegate listener, bool opt_useCapture, object opt_listenerScope)
			{
				for (var i = 0; i < listenerArray.Length; ++i) {
					var listenerObj = listenerArray[i];
					if (!listenerObj.removed && listenerObj.listener == listener &&
						listenerObj.capture == !!opt_useCapture &&
						listenerObj.handler == opt_listenerScope) {
						return i;
					}
				}
				return -1;
			}
		}
	}
}
