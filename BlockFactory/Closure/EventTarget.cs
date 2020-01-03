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
 * @fileoverview A disposable implementation of a custom
 * listenable/event target. See also: documentation for
 * {@code goog.events.Listenable}.
 *
 * @author arv@google.com (Erik Arvidsson) [Original implementation]
 * @see ../demos/eventtarget.html
 * @see goog.events.Listenable
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Bridge;
using Bridge.Html5;

namespace goog
{
	public static partial class events
	{
		public class EventTarget : Listenable
		{
			private bool disposed_;
			protected Bridge.Html5.EventTarget orgTarget_;
			/// <summary>
			/// Maps of event type to an array of listeners.
			/// </summary>
			private ListenerMap eventTargetListeners_;
			/// <summary>
			/// The object to use for event.target. Useful when mixing in an
			/// EventTarget to another object.
			/// </summary>
			private object actualEventTarget_;
			/// <summary>
			/// Parent event target, used during event bubbling.
			/// 
			/// TODO(chrishenry): Change this to goog.events.Listenable. This
			/// currently breaks people who expect getParentEventTarget to return
			/// goog.events.EventTarget.
			/// </summary>
			private EventTarget parentEventTarget_;

			private Dictionary<string, object> map = new Dictionary<string, object>();
			public object this[string prop] {
				get {
					map.TryGetValue(prop, out var obj);
					return obj;
				}
				set {
					map[prop] = value;
				}
			}

			public EventTarget(Bridge.Html5.EventTarget opt_element = null)
			{
				orgTarget_ = opt_element;
				eventTargetListeners_ = new ListenerMap(this);
				actualEventTarget_ = this;
				this.addImplementation();
			}

			public void dispose()
			{
				this.disposed_ = true;
			}

			public bool isDisposed()
			{
				return this.disposed_;
			}

			public void registerDisposable(EventTarget disposable)
			{
			}

			public void registerDisposable(EventHandler disposable)
			{
			}

			/// <summary>
			/// An artificial cap on the number of ancestors you can have. This is mainly
			/// for loop detection.
			/// </summary>
			private const int MAX_ANCESTORS_ = 1000;


			/// <summary>
			/// Returns the parent of this event target to use for bubbling.
			/// </summary>
			/// <returns>The parent EventTarget or null if
			/// there is no parent.</returns>
			public virtual EventTarget getParentEventTarget()
			{
				return this.parentEventTarget_;
			}

			/// <summary>
			/// Sets the parent of this event target to use for capture/bubble
			/// mechanism.
			/// </summary>
			/// <param name="parent"> Parent listenable (null if none).</param>
			public virtual void setParentEventTarget(EventTarget parent)
			{
				this.parentEventTarget_ = parent;
			}

			/// <summary>
			/// Adds an event listener to the event target. The same handler can only be
			/// added once per the type. Even if you add the same handler multiple times
			/// using the same type then it will only be called once when the event is
			/// dispatched.
			/// </summary>
			/// <param name="type">The type of the event to listen for.</param>
			/// <param name="handler">The function
			/// to handle the event. The handler can also be an object that implements
			/// the handleEvent method which takes the event object as argument.</param>
			/// <param name="opt_capture">In DOM-compliant browsers, this determines
			/// whether the listener is fired during the capture or bubble phase
			/// of the event.</param>
			/// <param name="opt_handlerScope">Object in whose scope to call
			/// the listener.</param>
			public void addEventListener(string type, Delegate handler, bool opt_capture = false,
				object opt_handlerScope = null)
			{
				if (orgTarget_ != null)
					orgTarget_.AddEventListener(type, handler, opt_capture);
				else
					goog.events.listen(this, type, handler, opt_capture, opt_handlerScope);
			}

			/// <summary>
			/// Removes an event listener from the event target. The handler must be the
			/// same object as the one added. If the handler has not been added then
			/// nothing is done.
			/// </summary>
			/// <param name="type">The type of the event to listen for.</param>
			/// <param name="handler">The function
			/// to handle the event. The handler can also be an object that implements
			/// the handleEvent method which takes the event object as argument.</param>
			/// <param name="opt_capture">In DOM-compliant browsers, this determines
			/// whether the listener is fired during the capture or bubble phase
			/// of the event.</param>
			/// <param name="opt_handlerScope">Object in whose scope to call
			/// the listener.</param>
			public void removeEventListener(string type, Delegate handler,
				bool opt_capture = false, object opt_handlerScope = null)
			{
				if (orgTarget_ != null)
					orgTarget_.RemoveEventListener(type, handler, opt_capture);
				else
					goog.events.unlisten(this, type, handler, opt_capture, opt_handlerScope);
			}

			public bool dispatchEvent(Union<string, Event> e)
			{
				this.assertInitialized_();

				JsArray<EventTarget> ancestorsTree = null;
				var ancestor = this.getParentEventTarget();
				if (ancestor != null) {
					ancestorsTree = new JsArray<EventTarget>();
					var ancestorCount = 1;
					for (; ancestor != null; ancestor = ancestor.getParentEventTarget()) {
						ancestorsTree.Push(ancestor);
						goog.asserts.assert(
							(++ancestorCount < goog.events.EventTarget.MAX_ANCESTORS_),
							"infinite loop");
					}
				}

				return goog.events.EventTarget.dispatchEventInternal_(
					this.actualEventTarget_, e, ancestorsTree);
			}

			/// <summary>
			/// Removes listeners from this object.  Classes that extend EventTarget may
			/// need to override this method in order to remove references to DOM Elements
			/// and additional listeners.
			/// </summary>
			public virtual void disposeInternal()
			{
				//this.removeAllListeners();
				this.parentEventTarget_ = null;
			}

			public Listener listen(string type, Delegate listener, bool opt_useCapture = false,
				object opt_listenerScope = null)
			{
				this.assertInitialized_();
				return this.eventTargetListeners_.add(
					type, listener, false /* callOnce */, opt_useCapture,
					opt_listenerScope);
			}

			public bool unlisten(string type, Delegate listener, bool opt_useCapture = false,
				object opt_listenerScope = null)
			{
				return this.eventTargetListeners_.remove(
					type, listener, opt_useCapture, opt_listenerScope);
			}

			public bool unlistenByKey(Listener key)
			{
				return this.eventTargetListeners_.removeByKey(key);
			}

			public bool fireListeners(string type, bool capture, Event eventObject)
			{
				// TODO(chrishenry): Original code avoids array creation when there
				// is no listener, so we do the same. If this optimization turns
				// out to be not required, we can replace this with
				// getListeners(type, capture) instead, which is simpler.
				if (!this.eventTargetListeners_.listeners.TryGetValue(type, out var listenerArray) || listenerArray == null) {
					return true;
				}
				listenerArray = listenerArray.Concat(new Listener[0]);

				var rv = true;
				for (var i = 0; i < listenerArray.Length; ++i) {
					var listener = listenerArray[i];
					// We might not have a listener if the listener was removed.
					if (listener != null && !listener.removed && listener.capture == capture) {
						var listenerFn = listener.listener;
						var listenerHandler = listener.handler ?? listenerFn.Target/*listener.src*/;

						if (listener.callOnce) {
							this.unlistenByKey(listener);
						}
						rv = listenerFn.Method.Invoke(listenerHandler, new object[] { eventObject }) != null ? rv : false;
					}
				}

				return rv && eventObject.returnValue_ != false;
			}

			public Listener getListener(string type, Delegate listener, bool capture,
				object opt_listenerScope = null)
			{
				return this.eventTargetListeners_.getListener(
					type, listener, capture, opt_listenerScope);
			}

			/// <summary>
			/// Asserts that the event target instance is initialized properly.
			/// </summary>
			private void assertInitialized_()
			{
				goog.asserts.assert(
					this.eventTargetListeners_ != null,
					"Event target is not initialized. Did you call the superclass " +
						"(goog.events.EventTarget) constructor?");
			}

			/// <summary>
			/// Dispatches the given event on the ancestorsTree.
			/// </summary>
			/// <param name="target">he target to dispatch on.</param>
			/// <param name="e_">The event object.</param>
			/// <param name="opt_ancestorsTree">The ancestors
			/// tree of the target, in reverse order from the closest ancestor
			/// to the root event target. May be null if the target has no ancestor.</param>
			/// <returns>If anyone called preventDefault on the event object (or
			/// if any of the listeners returns false) this will also return false.</returns>
			private static bool dispatchEventInternal_(object target, Union<string, Event> e_,
				JsArray<EventTarget> opt_ancestorsTree = null)
			{
				var type = e_.Is<Event>() ? e_.As<Event>().type : e_.As<string>();

				Event e;
				// If accepting a string or object, create a custom event object so that
				// preventDefault and stopPropagation work with the event.
				if (e_.Is<string>()) {
					e = new Event(e_.As<string>(), target);
				}
				else {
					e = e_.As<Event>();
					e.target = e.target ?? target;
				}

				var rv = true;
				EventTarget currentTarget;

				// Executes all capture listeners on the ancestors, if any.
				if (opt_ancestorsTree != null) {
					for (var i = opt_ancestorsTree.Length - 1; !e.propagationStopped_ && i >= 0;
						 i--) {
						e.currentTarget = currentTarget = opt_ancestorsTree[i];
						rv = currentTarget.fireListeners(type, true, e) && rv;
					}
				}

				// Executes capture and bubble listeners on the target.
				if (!e.propagationStopped_) {
					e.currentTarget = currentTarget = (EventTarget)target;
					rv = currentTarget.fireListeners(type, true, e) && rv;
					if (!e.propagationStopped_) {
						rv = currentTarget.fireListeners(type, false, e) && rv;
					}
				}

				// Executes all bubble listeners on the ancestors, if any.
				if (opt_ancestorsTree != null) {
					for (var i = 0; !e.propagationStopped_ && i < opt_ancestorsTree.Length; i++) {
						e.currentTarget = currentTarget = opt_ancestorsTree[i];
						rv = currentTarget.fireListeners(type, false, e) && rv;
					}
				}

				return rv;
			}

			public Listener listenOnce(string type, Delegate listener, bool opt_capt, object opt_handler)
			{
				throw new NotImplementedException();
			}
		}
	}
}
