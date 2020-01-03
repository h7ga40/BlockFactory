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
 * @fileoverview An event manager for both native browser event
 * targets and custom JavaScript event targets
 * ({@code goog.events.Listenable}). This provides an abstraction
 * over browsers' event systems.
 *
 * It also provides a simulation of W3C event model's capture phase in
 * Internet Explorer (IE 8 and below). Caveat: the simulation does not
 * interact well with listeners registered directly on the elements
 * (bypassing goog.events) or even with listeners registered via
 * goog.events in a separate JS binary. In these cases, we provide
 * no ordering guarantees.
 *
 * The listeners will receive a "patched" event object. Such event object
 * contains normalized values for certain event properties that differs in
 * different browsers.
 *
 * Example usage:
 * <pre>
 * goog.events.listen(myNode, 'click', function(e) { alert('woo') });
 * goog.events.listen(myNode, 'mouseover', mouseHandler, true);
 * goog.events.unlisten(myNode, 'mouseover', mouseHandler, true);
 * goog.events.removeAll(myNode);
 * </pre>
 *
 *                                            in IE and event object patching]
 * @author arv@google.com (Erik Arvidsson)
 *
 * @see ../demos/events.html
 * @see ../demos/event-propagation.html
 * @see ../demos/stopevent.html
 */

// IMPLEMENTATION NOTES:
// goog.events stores an auxiliary data structure on each EventTarget
// source being listened on. This allows us to take advantage of GC,
// having the data structure GC'd when the EventTarget is GC'd. This
// GC behavior is equivalent to using W3C DOM Events directly.

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using goog.ui;

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog
{
	public static partial class events
	{
		public class EventType
		{
			public const string TOUCHSTART = "touchstart";
			public const string TOUCHEND = "touchend";
			public const string FOCUS = "focus";
			public const string BLUR = "blur";
			public const string CHANGE = "change";
			public const string MOUSEDOWN = "mousedown";
			public const string MOUSEUP = "mouseup";
			public const string MOUSEOVER = "mouseover";
			public const string MOUSEOUT = "mouseout";
			public const string CLICK = "click";
			public const string DBLCLICK = "dblclick";
			public const string FOCUSIN = "focusin";
			public const string FOCUSOUT = "focusout";
			public const string KEY = "key";
			public const string KEYPRESS = "keypress";
			public const string KEYDOWN = "keydown";
			public const string KEYUP = "keyup";
			public const string CONTEXTMENU = "contextmenu";
			public const string SELECT = "select";
			public const string DEACTIVATE = "deactivate";
		}

		/// <summary>
		/// Different capture simulation mode for IE8-.
		/// </summary>
		public enum CaptureSimulationMode
		{
			/// <summary>
			/// Does not perform capture simulation. Will asserts in IE8- when you
			/// add capture listeners.
			/// </summary>
			OFF_AND_FAIL = 0,
			/// <summary>
			/// Does not perform capture simulation, silently ignore capture
			/// listeners.
			/// </summary>
			OFF_AND_SILENT = 1,
			/// <summary>
			/// Performs capture simulation.
			/// </summary>
			ON = 2,
		}

		/// <summary>
		/// Property name on a native event target for the listener map
		/// associated with the event target.
		/// </summary>
		private static readonly string LISTENER_MAP_PROP_ = "closure_lm_" + (Script.Random() * 1e6);

		/// <summary>
		/// String used to prepend to IE event types.
		/// </summary>
		private const string onString_ = "on";

		/// <summary>
		/// Map of computed "on<eventname>" strings for IE event types. Caching
		/// this removes an extra object allocation in goog.events.listen which
		/// improves IE6 performance.
		/// </summary>
		private static Dictionary<string, string> onStringMap_ = new Dictionary<string, string>();

		/// <summary>
		/// The capture simulation mode for IE8-. By default,
		/// this is ON.
		/// </summary>
		public const CaptureSimulationMode CAPTURE_SIMULATION_MODE = CaptureSimulationMode.ON;

		/// <summary>
		/// Estimated count of total native listeners.
		/// </summary>
		private static int listenerCountEstimate_ = 0;

		public class BrowserFeature
		{
			/// <summary>
			/// Whether the button attribute of the event is W3C compliant.  False in
			/// Internet Explorer prior to version 9; document-version dependent.
			/// </summary>
			public static readonly bool HAS_W3C_BUTTON =
				!goog.userAgent.IE || goog.userAgent.isDocumentModeOrHigher(9);

			/// <summary>
			/// Whether the browser supports full W3C event model.
			/// </summary>
			public static readonly bool HAS_W3C_EVENT_SUPPORT =
				!goog.userAgent.IE || goog.userAgent.isDocumentModeOrHigher(9);

			/// <summary>
			/// To prevent default in IE7-8 for certain keydown events we need set the
			/// keyCode to -1.
			/// </summary>
			public static bool SET_KEY_CODE_TO_PREVENT_DEFAULT =
				goog.userAgent.IE && !goog.userAgent.isVersionOrHigher("9");

			public static bool TOUCH_ENABLED;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="type"></param>
		/// <param name="capture"></param>
		/// <param name="eventObject"></param>
		/// <returns></returns>
		private static object fireListeners_(object obj, string type, bool capture, object eventObject)
		{
			var retval = true;

			var listenerMap = goog.events.getListenerMap_(
				(Bridge.Html5.EventTarget)obj);
			if (listenerMap != null) {
				// TODO(chrishenry): Original code avoids array creation when there
				// is no listener, so we do the same. If this optimization turns
				// out to be not required, we can replace this with
				// listenerMap.getListeners(type, capture) instead, which is simpler.
				var listenerArray = listenerMap.listeners[type.ToString()];
				if (listenerArray != null) {
					listenerArray = listenerArray.Concat(new JsArray<Listener>());
					for (var i = 0; i < listenerArray.Length; i++) {
						var listener = listenerArray[i];
						// We might not have a listener if the listener was removed.
						if (listener != null && listener.capture == capture && !listener.removed) {
							var result = goog.events.fireListener(listener, eventObject);
							retval = retval && (result != false);
						}
					}
				}
			}
			return retval;
		}

		/// <summary>
		/// Fires a listener with a set of arguments
		/// </summary>
		/// <param name="listener">The listener object to call.</param>
		/// <param name="eventObject">The event object to pass to the listener.</param>
		/// <returns>Result of listener.</returns>
		public static bool fireListener(Listener listener, object eventObject)
		{
			var listenerFn = listener.listener;
			var listenerHandler = listener.handler ?? listenerFn.Target/*listener.src*/;

			if (listener.callOnce) {
				goog.events.unlistenByKey(listener);
			}
			var ret = listenerFn.Method.Invoke(listenerHandler, new object[] { eventObject });
			if (ret is bool)
				return (bool)ret;
			return false;
		}

		/// <summary>
		/// Handles an event and dispatches it to the correct listeners. This
		/// function is a proxy for the real listener the user specified.
		/// </summary>
		/// <param name="listener">The listener object.</param>
		/// <param name="opt_evt">Optional event object that gets passed in via the
		/// native event handlers.</param>
		/// <returns>Result of the event handler.</returns>
		private static bool handleBrowserEvent_(Bridge.Html5.EventTarget _this, goog.events.Listener listener, Bridge.Html5.Event opt_evt = null)
		{
			if (listener.removed) {
				return true;
			}

			// Synthesize event propagation if the browser does not support W3C
			// event model.
			if (!goog.events.BrowserFeature.HAS_W3C_EVENT_SUPPORT) {
				var ieEvent = opt_evt ??
					((Bridge.Html5.Event)goog.le.getObjectByName("window.event"));
				var evt = new goog.events.BrowserEvent(ieEvent, _this);
				/** @type {boolean} */
				var retval = true;

				if (goog.events.CAPTURE_SIMULATION_MODE ==
					goog.events.CaptureSimulationMode.ON) {
					// If we have not marked this event yet, we should perform capture
					// simulation.
					if (!goog.events.isMarkedIeEvent_(ieEvent)) {
						goog.events.markIeEvent_(ieEvent);

						var ancestors = new JsArray<Node>();
						for (var parent = (Node)evt.currentTarget; parent != null;
							 parent = parent.ParentNode) {
							ancestors.Push(parent);
						}

						// Fire capture listeners.
						var type = listener.type;
						for (var i = ancestors.Length - 1; !evt.propagationStopped_ && i >= 0;
							 i--) {
							evt.currentTarget = ancestors[i];
							var result =
								goog.events.fireListeners_(ancestors[i], type, true, evt);
							retval = retval && result != null;
						}

						// Fire bubble listeners.
						//
						// We can technically rely on IE to perform bubble event
						// propagation. However, it turns out that IE fires events in
						// opposite order of attachEvent registration, which broke
						// some code and tests that rely on the order. (While W3C DOM
						// Level 2 Events TR leaves the event ordering unspecified,
						// modern browsers and W3C DOM Level 3 Events Working Draft
						// actually specify the order as the registration order.)
						for (var i = 0; !evt.propagationStopped_ && i < ancestors.Length; i++) {
							evt.currentTarget = ancestors[i];
							var result =
								goog.events.fireListeners_(ancestors[i], type, false, evt);
							retval = retval && result != null;
						}
					}
				}
				else {
					retval = goog.events.fireListener(listener, evt);
				}
				return retval;
			}

			// Otherwise, simply fire the listener.
			return goog.events.fireListener(
				listener, new goog.events.BrowserEvent(opt_evt, _this));
		}

		/// <summary>
		/// This is used to mark the IE event object so we do not do the Closure pass
		/// twice for a bubbling event.
		/// </summary>
		/// <param name="e"></param>
		private static void markIeEvent_(Bridge.Html5.Event e)
		{
			// Only the keyCode and the returnValue can be changed. We use keyCode for
			// non keyboard events.
			// event.returnValue is a bit more tricky. It is undefined by default. A
			// boolean false prevents the default action. In a window.onbeforeunload and
			// the returnValue is non undefined it will be alerted. However, we will only
			// modify the returnValue for keyboard events. We can get a problem if non
			// closure events sets the keyCode or the returnValue

			var useReturnValue = false;

			if (e.KeyCode == 0) {
				// We cannot change the keyCode in case that srcElement is input[type=file].
				// We could test that that is the case but that would allocate 3 objects.
				// If we use try/catch we will only allocate extra objects in the case of a
				// failure.
				/** @preserveTry */
				try {
					e.KeyCode = -1;
					return;
				}
				catch (Exception) {
					useReturnValue = true;
				}
			}

			if (useReturnValue ||
				((bool?)e["returnValue"]) != true) {
				e["returnValue"] = true;
			}
		}

		/// <summary>
		/// This is used to check if an IE event has already been handled by the Closure
		/// system so we do not do the Closure pass twice for a bubbling event.
		/// </summary>
		/// <param name="e">The IE browser event.</param>
		/// <returns>True if the event object has been marked.</returns>
		private static bool isMarkedIeEvent_(Bridge.Html5.Event e)
		{
			return e.KeyCode < 0 || ((bool?)e["returnValue"]) != null;
		}

		class Proxy : Listener
		{
			public Proxy()
				: base(null)
			{
			}

			public void SetInstance(Func<Bridge.Html5.TouchEvent/*暫定*/, bool> func)
			{
				Instance = func;
			}

			public bool proxyCallbackFunction(Bridge.Html5.TouchEvent eventObject)
			{
				if (goog.events.BrowserFeature.HAS_W3C_EVENT_SUPPORT) {
					return goog.events.handleBrowserEvent_(src.As<Bridge.Html5.EventTarget>(), this, eventObject);
				}
				else {
					var v = /*proxyCallbackFunction*/goog.events.handleBrowserEvent_(src.As<Bridge.Html5.EventTarget>(), this, eventObject);
					// NOTE(chrishenry): In IE, we hack in a capture phase. However, if
					// there is inline event handler which tries to prevent default (for
					// example <a href="..." onclick="return false">...</a>) in a
					// descendant element, the prevent default will be overridden
					// by this listener if this listener were to return true. Hence, we
					// return undefined.
					if (!v) return v;
					/*??*/
					return false;
				}
			}
		}

		/// <summary>
		/// Helper function for returning a proxy function.
		/// </summary>
		/// <returns>A new or reused function object.</returns>
		public static Listener getProxy()
		{
			// Use a local var f to prevent one allocation.
			var f = new Proxy();
			f.SetInstance(new Func<Bridge.Html5.TouchEvent/*暫定*/, bool>(f.proxyCallbackFunction));
			return f;
		}

		public static Listener listen(Union<Bridge.Html5.EventTarget, Listenable> src, Union<string, JsArray<string>> type, Delegate listener_,
			bool opt_capt = false, object opt_handler = null)
		{
			if (type.Is<JsArray<string>>()) {
				foreach (var i in type.As<JsArray<string>>()) {
					goog.events.listen(src, i, listener_, opt_capt, opt_handler);
				}
				return null;
			}

			var listener = goog.events.wrapListener(listener_);
			if (src.As<Listenable>().isImplementedBy()) {
				return src.As<Listenable>().listen(type.As<string>(), listener, opt_capt, opt_handler);
			}
			else {
				return goog.events.listen_(src.As<Bridge.Html5.EventTarget>(), type.As<string>(), listener, false, opt_capt, opt_handler);
			}
		}

		/// <summary>
		/// Note that a one-off listener will not change an existing listener,
		/// if any. On the other hand a normal listener will change existing
		/// one-off listener to become a normal listener.
		/// </summary>
		/// <param name="src">The node to listen to events on.</param>
		/// <param name="type">Event type.</param>
		/// <param name="listener">Callback function.</param>
		/// <param name="callOnce">Whether the listener is a one-off
		/// listener or otherwise.</param>
		/// <param name="opt_capt">Whether to fire in capture phase (defaults to
		/// false).</param>
		/// <param name="opt_handler">Element in whose scope to call the listener.</param>
		/// <returns>Unique key for the listener.</returns>
		private static Listener listen_(Bridge.Html5.EventTarget src, string type, Delegate listener,
			bool callOnce, bool opt_capt = false, object opt_handler = null)
		{
			if (type == null) {
				throw new Exception("Invalid event type");
			}

			var capture = !!opt_capt;
			if (capture && !goog.events.BrowserFeature.HAS_W3C_EVENT_SUPPORT) {
				if (goog.events.CAPTURE_SIMULATION_MODE ==
					goog.events.CaptureSimulationMode.OFF_AND_FAIL) {
					goog.asserts.fail("Can not register capture listener in IE8-.");
					return null;
				}
				else if (
					goog.events.CAPTURE_SIMULATION_MODE ==
					goog.events.CaptureSimulationMode.OFF_AND_SILENT) {
					return null;
				}
			}

			var listenerMap = goog.events.getListenerMap_(src);
			if (listenerMap == null) {
				src[goog.events.LISTENER_MAP_PROP_] = listenerMap =
					new goog.events.ListenerMap(src);
			}

			var listenerObj =
				listenerMap.add(type, listener, callOnce, opt_capt, opt_handler);

			// If the listenerObj already has a proxy, it has been set up
			// previously. We simply return.
			if (listenerObj.proxy != null) {
				return listenerObj;
			}

			var proxy = goog.events.getProxy();
			listenerObj.proxy = proxy;

			proxy.src = src;
			proxy.listener = listenerObj.listener;
#if false
			// Attach the proxy through the browser's API
			if (Script.IsDefined(src, "addEventListener")) {
				src.AddEventListener(type, proxy.Instance, capture);
			}
			else if (Script.IsDefined(src, "attachEvent")) {
				// The else if above used to be an unconditional else. It would call
				// exception on IE11, spoiling the day of some callers. The previous
				// incarnation of this code, from 2007, indicates that it replaced an
				// earlier still version that caused excess allocations on IE6.
				Script.Get<Action<object, Delegate>>(src, "attachEvent")(goog.events.getOnString_(type), proxy.Instance);
			}
			else {
				throw new Exception("addEventListener and attachEvent are unavailable.");
			}
#else
			src.AddEventListener(type, proxy.Instance, capture);
#endif
			goog.events.listenerCountEstimate_++;
			return listenerObj;
		}

		/// <summary>
		/// Removes an event listener which was added with listen().
		/// </summary>
		/// <param name="src">The target to stop listening to events on.</param>
		/// <param name="type">Event type or array of event types to unlisten to.</param>
		/// <param name="listener">The listener function to remove.</param>
		/// <param name="opt_capt">In DOM-compliant browsers, this determines
		/// whether the listener is fired during the capture or bubble phase of the
		/// event.</param>
		/// <param name="opt_handler">Element in whose scope to call the listener.</param>
		/// <returns>indicating whether the listener was there to remove.</returns>
		public static bool unlisten(Union<Bridge.Html5.EventTarget, Listenable> src, Union<string, JsArray<string>> type,
			Union<Delegate, Listener> listener_, bool opt_capt = false, object opt_handler = null)
		{
			if (type.Is<JsArray<string>>()) {
				foreach (var i in type.As<JsArray<string>>()) {
					goog.events.unlisten(src, i, listener_, opt_capt, opt_handler);
				}
				return false;
			}

			var listener = goog.events.wrapListener(listener_);
			if (src.As<Listenable>().isImplementedBy()) {
				return src.As<Listenable>().unlisten(type.As<string>(), listener, opt_capt,
					opt_handler);
			}

			if (src == null) {
				// TODO(chrishenry): We should tighten the API to only accept
				// non-null objects, or add an assertion here.
				return false;
			}

			var capture = !!opt_capt;
			var listenerMap = goog.events.getListenerMap_(src.As<Bridge.Html5.EventTarget>());
			if (listenerMap != null) {
				var listenerObj = listenerMap.getListener(type.As<string>(), listener, capture,
					opt_handler);
				if (listenerObj != null) {
					return goog.events.unlistenByKey(listenerObj);
				}
			}

			return false;
		}

		public static Listener getListener(Union<Bridge.Html5.EventTarget, Listenable> src, string type, Union<Delegate, Listener> listener_, bool opt_capt = false, object opt_handler = null)
		{
			// TODO(chrishenry): Change type from ?string to string, or add assertion.
			var listener = goog.events.wrapListener(listener_);
			var capture = !!opt_capt;
			if (src.As<Listenable>().isImplementedBy()) {
				return src.As<Listenable>().getListener(type, listener, capture, opt_handler);
			}

			if (src == null) {
				// TODO(chrishenry): We should tighten the API to only accept
				// non-null objects, or add an assertion here.
				return null;
			}

			var listenerMap = goog.events.getListenerMap_(
				src.As<Bridge.Html5.EventTarget>());
			if (listenerMap != null) {
				return listenerMap.getListener(type, listener, capture, opt_handler);
			}
			return null;
		}

		/// <summary>
		/// </summary>
		/// <param name="src">The source object.</param>
		/// <returns>A listener map for the given
		/// source object, or null if none exists.</returns>
		private static ListenerMap getListenerMap_(Bridge.Html5.EventTarget src)
		{
			var listenerMap = src[LISTENER_MAP_PROP_];
			// IE serializes the property as well (e.g. when serializing outer
			// HTML). So we must check that the value is of the correct type.
			return listenerMap as ListenerMap;
		}

		/// <summary>
		/// Removes an event listener which was added with listen() by the key
		/// returned by listen().
		/// </summary>
		///
		/// <param name="key"> The key returned by listen() for this</param>
		///     event listener.
		/// <returns>indicating whether the listener was there to remove.</returns>
		public static bool unlistenByKey(Listener key)
		{
			var listener = key;
			if (listener == null || listener.removed) {
				return false;
			}

			var listenable = listener.src.As<Listenable>();
			if (listenable.isImplementedBy()) {
				return listenable.unlistenByKey(listener);
			}

			var src = listener.src.As<Bridge.Html5.EventTarget>();
			var type = listener.type;
			var proxy = listener.proxy;
#if false
			if (Script.IsDefined(src, "removeEventListener")) {
				src.RemoveEventListener(type, proxy.Instance, listener.capture);
			}
			else if (Script.IsDefined(src, "detachEvent")) {
				Script.Get<Action<object, Delegate>>(src, "detachEvent")(goog.events.getOnString_(type), proxy.Instance);
			}
#else
			src.RemoveEventListener(type, proxy.Instance, listener.capture);
#endif
			goog.events.listenerCountEstimate_--;

			var listenerMap = goog.events.getListenerMap_(
				(Bridge.Html5.EventTarget)src);
			// TODO(chrishenry): Try to remove this conditional and execute the
			// first branch always. This should be safe.
			if (listenerMap != null) {
				listenerMap.removeByKey(listener);
				if (listenerMap.getTypeCount() == 0) {
					// Null the src, just because this is simple to do (and useful
					// for IE <= 7).
					listenerMap.src = null;
					// We don't use delete here because IE does not allow delete
					// on a window object.
					src[goog.events.LISTENER_MAP_PROP_] = null;
				}
			}
			else {
				listener.markAsRemoved();
			}

			return true;
		}

		/// <summary>
		/// Returns a string with on prepended to the specified type. This is used for IE
		/// which expects "on" to be prepended. This function caches the string in order
		/// to avoid extra allocations in steady state.
		/// </summary>
		/// <param name="type"> Event type.</param>
		/// <returns>The type string with "on" prepended.</returns>
		private static object getOnString_(string type)
		{
			if (goog.events.onStringMap_.ContainsKey(type)) {
				return goog.events.onStringMap_[type];
			}
			return goog.events.onStringMap_[type] = goog.events.onString_ + type;
		}

		/// <summary>
		/// Dispatches an event (or event like object) and calls all listeners
		/// listening for events of this type. The type of the event is decided by the
		/// type property on the event object.
		/// 
		/// If any of the listeners returns false OR calls preventDefault then this
		/// function will return false.  If one of the capture listeners calls
		/// stopPropagation, then the bubble listeners won't fire.
		/// </summary>
		/// <param name="src">The event target.</param>
		/// <param name="e">Event object.</param>
		/// <returns>If anyone called preventDefault on the event object (or
		/// if any of the handlers returns false) this will also return false.
		/// If there are no handlers, or if all handlers return true, this returns
		/// true.</returns>
		public static bool dispatchEvent(Listenable src, Event e)
		{
			goog.asserts.assert(
				src.isImplementedBy(),
				"Can not use goog.events.dispatchEvent with " +
					"non-goog.events.Listenable instance.");
			return src.dispatchEvent(e);
		}

		private static readonly string LISTENER_WRAPPER_PROP_ =
			"__closure_events_fn_" + (Script.Random() * 1e9);

		public static Delegate wrapListener(Union<Delegate, Listener> listener_)
		{
			goog.asserts.assert(listener_.Value != null, "Listener can not be null.");

			if (listener_.Is<Delegate>()) {
				return listener_.As<Delegate>();
			}

			var listener = listener_.As<Listener>();
			goog.asserts.assert(
				listener.handleEvent != null, "An object listener must have handleEvent method.");
			if (listener[goog.events.LISTENER_WRAPPER_PROP_] == null) {
				listener[goog.events.LISTENER_WRAPPER_PROP_] = new Func<Bridge.Html5.Event, bool>((e) => {
					return listener.handleEvent(e);
				});
			}
			return (Delegate)listener[goog.events.LISTENER_WRAPPER_PROP_];
		}
	}
}
