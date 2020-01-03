// Copyright 2012 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview An interface for a listenable JavaScript object.
 * @author chrishenry@google.com (Chris Henry)
 */

using System;
using Bridge;
using Bridge.Html5;
using goog.ui;

namespace goog
{
	public static partial class events
	{
		public interface Listenable
		{
			void disposeInternal();
			Listener listen(string type, Delegate listener, bool opt_useCapture = false,
				object opt_listenerScope = null);
			bool unlisten(string type, Delegate listener, bool opt_useCapture = false,
							object opt_listenerScope = null);
			bool unlistenByKey(Listener key);
			bool dispatchEvent(Union<string, Event> e);
			EventTarget getParentEventTarget();
			Listener getListener(string type, Delegate listener, bool capture,
				object opt_listenerScope = null);
			object this[string prop] { get; set; }
		}

		private static int uniqueIdCounter_;

		public static string getUniqueId(string identifier)
		{
			return identifier + '_' + goog.events.uniqueIdCounter_++;
		}

		public static Listener listenOnce(EventTarget src, Union<string, JsArray<string>> type_, Delegate listener, bool opt_capt = false, object opt_handler = null)
		{
			if (type_.Is<JsArray<string>>()) {
				var type = type_.As<JsArray<string>>();
				for (var i = 0; i < type.Length; i++) {
					goog.events.listenOnce(src, type[i], listener, opt_capt, opt_handler);
				}
				return null;
			}
			else {
				var type = type_.As<string>();
				listener = goog.events.wrapListener(listener);
				if (src.isImplementedBy()) {
					return src.listenOnce(type, listener, opt_capt, opt_handler);
				}
				else {
					//return goog.events.listen_(src, type, listener,
					//	/* callOnce */ true, opt_capt, opt_handler);
					throw new NotImplementedException();
				}
			}
		}
	}

	public static class ListenableExtention
	{
		public static readonly string IMPLEMENTED_BY_PROP =
			"closure_listenable_" + (Script.Random() * 1e6);

		public static void addImplementation(this events.Listenable obj)
		{
			obj[IMPLEMENTED_BY_PROP] = true;
		}

		public static bool isImplementedBy(this events.Listenable obj)
		{
			return obj != null && (bool)obj[IMPLEMENTED_BY_PROP];
		}
	}
}
