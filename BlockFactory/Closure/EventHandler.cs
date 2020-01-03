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

/**
 * @fileoverview Class to create objects which want to handle multiple events
 * and have their listeners easily cleaned up via a dispose method.
 *
 * Example:
 * <pre>
 * function Something() {
 *   Something.base(this);
 *
 *   ... set up object ...
 *
 *   // Add event listeners
 *   this.listen(this.starEl, goog.events.EventType.CLICK, this.handleStar);
 *   this.listen(this.headerEl, goog.events.EventType.CLICK, this.expand);
 *   this.listen(this.collapseEl, goog.events.EventType.CLICK, this.collapse);
 *   this.listen(this.infoEl, goog.events.EventType.MOUSEOVER, this.showHover);
 *   this.listen(this.infoEl, goog.events.EventType.MOUSEOUT, this.hideHover);
 * }
 * goog.inherits(Something, goog.events.EventHandler);
 *
 * Something.prototype.disposeInternal = function() {
 *   Something.base(this, 'disposeInternal');
 *   goog.dom.removeNode(this.container);
 * };
 *
 *
 * // Then elsewhere:
 *
 * var activeSomething = null;
 * function openSomething() {
 *   activeSomething = new Something();
 * }
 *
 * function closeSomething() {
 *   if (activeSomething) {
 *     activeSomething.dispose();  // Remove event listeners
 *     activeSomething = null;
 *   }
 * }
 * </pre>
 *
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog
{
	public static partial class events
	{
		public class EventHandler
		{
			private bool disposed_;
			/// <summary>
			/// TODO(mknichel): Rename this to this.scope_ and fix the classes in google3
			/// that access this private variable. :(
			/// </summary>
			private EventTarget handler_;
			/// <summary> 
			/// Keys for events that are being listened to.
			/// </summary>
			private Dictionary<int, Listener> keys_ = new Dictionary<int, Listener>();

			/// <summary>
			/// Utility array used to unify the cases of listening for an array of types
			/// and listening for a single event, without using recursion or allocating
			/// an array each time.
			/// </summary>
			private static JsArray<string> typeArray_ = new JsArray<string>();

			/// <summary>
			/// Super class for objects that want to easily manage a number of event
			/// listeners.  It allows a short cut to listen and also provides a quick way
			/// to remove all events listeners belonging to this object.
			/// </summary>
			/// <param name="opt_scope">Object in whose scope to call the listeners.</param>
			public EventHandler(EventTarget opt_scope = null)
			{
				this.handler_ = opt_scope;
			}

			public void dispose()
			{
				this.disposed_ = true;
			}

			/// <summary>
			/// Listen to an event on a Listenable.  If the function is omitted then the
			/// EventHandler's handleEvent method will be used.
			/// </summary>
			/// <param name="src">Event source.</param>
			/// <param name="type">Event type to listen for or array of event types.</param>
			/// <param name="opt_fn">Optional callback function to be used as the listener or an object
			/// with handleEvent function.</param>
			/// <param name="opt_capture">Optional whether to use capture phase.</param>
			/// <returns>This object, allowing for chaining of calls.</returns>
			public EventHandler listen<T>(Union<Bridge.Html5.EventTarget, Listenable> src, Union<string, JsArray<string>> type,
				Action<T> opt_fn = null, bool opt_capture = true) where T : goog.events.Event
			{
				var self = this;
				return self.listen_(src, type, opt_fn, opt_capture);
			}
			public EventHandler listen<T>(Union<Bridge.Html5.EventTarget, Listenable> src, Union<string, JsArray<string>> type,
				Func<T, bool> opt_fn = null, bool opt_capture = true) where T : goog.events.Event
			{
				var self = this;
				return self.listen_(src, type, opt_fn, opt_capture);
			}

			/// <summary>
			/// Listen to an event on a Listenable.  If the function is omitted then the
			/// EventHandler's handleEvent method will be used.
			/// </summary>
			/// <param name="src">Event source.</param>
			/// <param name="type_">Event type to listen for or array of event types.</param>
			/// <param name="opt_fn">Optional callback function to be used as the listener or an object with
			/// handleEvent function.</param>
			/// <param name="opt_capture">Optional whether to use capture phase.</param>
			/// <param name="opt_scope">Object in whose scope to call the listener.</param>
			/// <returns>This object, allowing for chaining of calls.</returns>
			private EventHandler listen_(Union<Bridge.Html5.EventTarget, Listenable> src, Union<string, JsArray<string>> type_,
				Delegate opt_fn = null, bool opt_capture = false, object opt_scope = null)
			{
				var self = this;
				JsArray<string> type;
				if (!type_.Is<JsArray<string>>()) {
					if (type_ != null) {
						goog.events.EventHandler.typeArray_ = new JsArray<string> { type_.As<string>() };
					}
					type = goog.events.EventHandler.typeArray_;
				}
				else {
					type = type_.As<JsArray<string>>();
				}
				for (var i = 0; i < type.Length; i++) {
					var listenerObj = goog.events.listen(
						src, type[i], opt_fn ?? new Action<Event>(self.handleEvent), opt_capture || false,
						opt_scope ?? self.handler_ ?? (object)self);

					if (listenerObj == null) {
						// When goog.events.listen run on OFF_AND_FAIL or OFF_AND_SILENT
						// (goog.events.CaptureSimulationMode) in IE8-, it will return null
						// value.
						return self;
					}

					var key = listenerObj.GetHashCode();
					self.keys_[key] = listenerObj;
				}

				return self;
			}

			public EventHandler unlisten(Bridge.Html5.EventTarget src, Union<string, JsArray<string>> type,
				Delegate opt_fn = null, bool opt_capture = false, object opt_scope = null)
			{
				return unlisten(new EventTarget(src), type, opt_fn, opt_capture, opt_scope);
			}

			public EventHandler unlisten(EventTarget src, Union<string, JsArray<string>> type,
				Delegate opt_fn = null, bool opt_capture = false, object opt_scope = null)
			{
				var self = this;
				if (type.Is<JsArray<string>>()) {
					foreach (var i in type.As<JsArray<string>>()) {
						self.unlisten(src, i, opt_fn, opt_capture, opt_scope);
					}
				}
				else {
					var listener = goog.events.getListener(
						src, type.As<string>(), opt_fn ?? new Action<Event>(self.handleEvent), opt_capture,
						opt_scope ?? self.handler_ ?? (object)self);

					if (listener != null) {
						goog.events.unlistenByKey(listener);
						self.keys_.Remove(listener.GetHashCode());
					}
				}

				return self;
			}

			public EventHandler unlisten(HTMLElement src, Union<string, JsArray<string>> type,
				Delegate opt_fn = null, bool opt_capture = false, object opt_scope = null)
			{
				return unlisten(new EventTarget(src), type, opt_fn, opt_capture, opt_scope);
			}

			public void removeAll()
			{
				foreach (var item in keys_) {
					if (this.keys_.ContainsKey(item.Key)) {
						goog.events.unlistenByKey(item.Value);
					}
				}
				keys_ = new Dictionary<int, Listener>();
			}

			/// <summary>
			/// Default event handler
			/// </summary>
			/// <param name="e"> Event object.</param>
			public virtual void handleEvent(Event e)
			{
				throw new Exception("EventHandler.handleEvent not implemented");
			}
		}
	}
}
