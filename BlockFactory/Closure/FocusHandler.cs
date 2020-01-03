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
 * @fileoverview This event handler allows you to catch focusin and focusout
 * events on  descendants. Unlike the "focus" and "blur" events which do not
 * propagate consistently, and therefore must be added to the element that is
 * focused, this allows you to attach one listener to an ancester and you will
 * be notified when the focus state changes of ony of its descendants.
 * @author arv@google.com (Erik Arvidsson)
 * @see ../demos/focushandler.html
 */

using System;
using Bridge;
using Bridge.Html5;

namespace goog
{
	public static partial class events
	{
		public class FocusHandler : EventTarget
		{
			public static class EventType
			{
				public const string FOCUSIN = "focusin";
				public const string FOCUSOUT = "focusout";
			}

			/// <summary>
			/// This is the element that we will listen to the real focus events on.
			/// </summary>
			HTMLElement element_;
			/// <summary>
			/// In IE we use focusin/focusout and in other browsers we use a capturing
			/// listner for focus/blur
			/// </summary>
			public string typeIn;
			public string typeOut;
			/// <summary>
			/// Store the listen key so it easier to unlisten in dispose.
			/// </summary>
			private Listener listenKeyIn_;
			/// <summary>
			/// Store the listen key so it easier to unlisten in dispose.
			/// </summary>
			private Listener listenKeyOut_;

			public FocusHandler(HTMLElement element)
				: base(element)
			{
				element_ = element;
				typeIn = goog.userAgent.IE ? "focusin" : "focus";
				typeOut = goog.userAgent.IE ? "focusout" : "blur";

				listenKeyIn_ =
					goog.events.listen(element_, typeIn, new Action<BrowserEvent>(handleEvent), !goog.userAgent.IE);
				listenKeyOut_ =
					goog.events.listen(element_, typeOut, new Action<BrowserEvent>(handleEvent), !goog.userAgent.IE);
			}

			/// <summary>
			/// This handles the underlying events and dispatches a new event.
			/// </summary>
			/// <param name="e">The underlying browser event.</param>
			public void handleEvent(BrowserEvent e)
			{
				var be = e.getBrowserEvent();
				var ev = new BrowserEvent(be);
				ev.type = e.type == "focusin" || e.type == "focus" ?
					goog.events.FocusHandler.EventType.FOCUSIN :
					goog.events.FocusHandler.EventType.FOCUSOUT;
				this.dispatchEvent(ev);
			}

			public override void disposeInternal()
			{
				base.disposeInternal();
				goog.events.unlistenByKey(this.listenKeyIn_);
				goog.events.unlistenByKey(this.listenKeyOut_);
				Script.Delete(ref this.element_);
			}
		}
	}
}
