// Copyright 2009 The Closure Library Authors. All Rights Reserved.
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

/// <summary>
/// Google's common JavaScript library
/// https://developers.google.com/closure/library/
/// </summary>
namespace goog
{
	public static partial class events
	{
		public class Listener
		{
			public Delegate listener;
			/// <summary>
			/// A wrapper over the original listener. This is used solely to
			/// handle native browser events (it is used to simulate the capture
			/// phase and to patch the event object).
			/// </summary>
			public Listener proxy;
			/// <summary>
			/// Object or node that callback is listening to
			/// </summary>
			public Union<Bridge.Html5.EventTarget, Listenable> src;
			/// <summary>
			/// The event type.
			/// </summary>
			public string type;
			/// <summary>
			/// Whether the listener is being called in the capture or bubble phase
			/// </summary>
			public bool capture;
			/// <summary>
			/// Optional object whose context to execute the listener in
			/// </summary>
			public object handler;
			/// <summary>
			/// Whether to remove the listener after it has been called.
			/// </summary>
			public bool callOnce;
			/// <summary>
			/// Whether the listener has been removed.
			/// </summary>
			public bool removed;

			System.Collections.Generic.Dictionary<string, object> keyValues = new System.Collections.Generic.Dictionary<string, object>();

			public object this[string name] {
				get { return keyValues.TryGetValue(name, out var result) ? result : null; }
				set { keyValues[name] = value; }
			}

			// ガーベージコレクタ対策用
			Func<Bridge.Html5.Event, bool> _handleEvent;

			public Func<Bridge.Html5.Event, bool> handleEvent {
				get {
					return _handleEvent;
				}
				set {
					_handleEvent = value;
				}
			}

			Delegate instance;
			public Delegate Instance { get => instance; protected set { instance = value; listener = value; } }

			public Listener(Delegate listener, Listener proxy = null, Union<Bridge.Html5.EventTarget, Listenable> src = null,
				string type = null, bool capture = false, object opt_handler = null)
			{
				Instance = listener;

				this.listener = listener;
				this.proxy = proxy;
				this.src = src;
				this.type = type;
				this.capture = capture;
				this.handler = opt_handler;
				this.callOnce = false;
				this.removed = false;
			}

			/// <summary>
			/// Marks this listener as removed. This also remove references held by
			/// this listener object (such as listener and event source).
			/// </summary>
			public void markAsRemoved()
			{
				this.removed = true;
				this.listener = null;
				this.proxy = null;
				this.src = null;
				this.handler = null;
			}
		}
	}
}
