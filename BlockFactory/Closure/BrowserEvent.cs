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
 * @fileoverview A patched, standardized event object for browser events.
 *
 * <pre>
 * The patched event object contains the following members:
 * - type           {string}    Event type, e.g. 'click'
 * - target         {Object}    The element that actually triggered the event
 * - currentTarget  {Object}    The element the listener is attached to
 * - relatedTarget  {Object}    For mouseover and mouseout, the previous object
 * - offsetX        {number}    X-coordinate relative to target
 * - offsetY        {number}    Y-coordinate relative to target
 * - clientX        {number}    X-coordinate relative to viewport
 * - clientY        {number}    Y-coordinate relative to viewport
 * - screenX        {number}    X-coordinate relative to the edge of the screen
 * - screenY        {number}    Y-coordinate relative to the edge of the screen
 * - button         {number}    Mouse button. Use isButton() to test.
 * - keyCode        {number}    Key-code
 * - ctrlKey        {boolean}   Was ctrl key depressed
 * - altKey         {boolean}   Was alt key depressed
 * - shiftKey       {boolean}   Was shift key depressed
 * - metaKey        {boolean}   Was meta key depressed
 * - defaultPrevented {boolean} Whether the default action has been prevented
 * - state          {Object}    History state object
 *
 * NOTE: The keyCode member contains the raw browser keyCode. For normalized
 * key and character code use {@link goog.events.KeyHandler}.
 * </pre>
 *
 * @author arv@google.com (Erik Arvidsson)
 */

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
		public class BrowserEvent : Event
		{
			/// <summary>
			/// For mouseover and mouseout events, the related object for the event.
			/// </summary>
			public Node relatedTarget;

			/// <summary>
			/// X-coordinate relative to target.
			/// </summary>
			public int offsetX;

			/// <summary>
			/// Y-coordinate relative to target.
			/// </summary>
			public int offsetY;

			/// <summary>
			/// X-coordinate relative to the window.
			/// </summary>
			public int clientX;

			/// <summary>
			/// Y-coordinate relative to the window.
			/// </summary>
			public int clientY;

			/// <summary>
			/// X-coordinate relative to the monitor.
			/// </summary>
			public int screenX;

			/// <summary>
			/// Y-coordinate relative to the monitor.
			/// </summary>
			public int screenY;

			/// <summary>
			/// Which mouse button was pressed.
			/// </summary>
			public int button;

			/// <summary>
			/// Keycode of key press.
			/// </summary>
			public int keyCode;

			/// <summary>
			/// Keycode of key press.
			/// </summary>
			public int charCode;
#if false
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
			/// The browser event object.
			/// </summary>
			private Bridge.Html5.Event event_;

			private bool bubbles;
			private bool cancelable;
			private object view;
			private int detail;

			/// <summary>
			/// Accepts a browser event object and creates a patched, cross browser event
			/// object.
			/// The content of this object will not be initialized if no event object is
			/// provided. If this is the case, init() needs to be invoked separately.
			/// </summary>
			/// <param name="opt_e">Browser event object.</param>
			/// <param name="opt_currentTarget">Current target for event.</param>
			public BrowserEvent(Bridge.Html5.Event opt_e, Bridge.Html5.EventTarget opt_currentTarget = null)
				: base(opt_e)
			{
				if (opt_e != null) {
					this.init(opt_e, opt_currentTarget);
				}
			}

			/// <summary>
			/// Normalized button constants for the mouse.
			/// </summary>
			public enum MouseButton
			{
				LEFT,
				MIDDLE,
				RIGHT
			}

			/// <summary>
			/// Static data for mapping mouse buttons.
			/// </summary>
			public static int[] IEButtonMap = new int[] {
				1,  // LEFT
				4,  // MIDDLE
				2   // RIGHT
			};

			/// <summary>
			/// Accepts a browser event object and creates a patched, cross browser event
			/// object.
			/// </summary>
			/// <param name="e">Browser event object.</param>
			/// <param name="opt_currentTarget">Current target for event.</param>
			public void init(Bridge.Html5.Event e, Bridge.Html5.EventTarget opt_currentTarget)
			{
				var type = this.type = e.Type;

				/**
				 * On touch devices use the first "changed touch" as the relevant touch.
				 * @type {Touch}
				 */
				var relevantTouch = e.ChangedTouches != null ? e.ChangedTouches[0] : null;

				// TODO(nicksantos): Change this.target to type EventTarget.
				this.target = (e.Target) ?? e.SrcElement;

				// TODO(nicksantos): Change this.currentTarget to type EventTarget.
				this.currentTarget = opt_currentTarget;

				var relatedTarget = (Node)e.RelatedTarget;
				if (relatedTarget != null) {
					// There's a bug in FireFox where sometimes, relatedTarget will be a
					// chrome element, and accessing any property of it will get a permission
					// denied exception. See:
					// https://bugzilla.mozilla.org/show_bug.cgi?id=497780
					if (goog.userAgent.GECKO) {
						if (!goog.reflect.canAccessProperty(relatedTarget, "nodeName")) {
							relatedTarget = null;
						}
					}
					// TODO(arv): Use goog.events.EventType when it has been refactored into its
					// own file.
				}
				else if (type == goog.events.EventType.MOUSEOVER) {
					relatedTarget = e.FromElement;
				}
				else if (type == goog.events.EventType.MOUSEOUT) {
					relatedTarget = e.ToElement;
				}

				this.relatedTarget = relatedTarget;

				if (relevantTouch != null) {
					this.clientX = Script.IsDefined(relevantTouch, "clientX") ?
						(int)relevantTouch.ClientX :
						(int)relevantTouch.PageX;
					this.clientY = Script.IsDefined(relevantTouch, "clientY") ?
						(int)relevantTouch.ClientY :
						(int)relevantTouch.PageY;
					this.screenX = relevantTouch.ScreenX;
					this.screenY = relevantTouch.ScreenY;
				}
				else {
					// Webkit emits a lame warning whenever layerX/layerY is accessed.
					// http://code.google.com/p/chromium/issues/detail?id=101733
					this.offsetX = (goog.userAgent.WEBKIT || e.IsDefined("offsetX")) ?
						(int)e.OffsetX :
						(int)e.LayerX;
					this.offsetY = (goog.userAgent.WEBKIT || e.IsDefined("offsetY")) ?
						(int)e.OffsetY :
						(int)e.LayerY;
					this.clientX = e.IsDefined("clientX") ? (int)e.ClientX : (int)e.PageX;
					this.clientY = e.IsDefined("clientY") ? (int)e.ClientY : (int)e.PageY;
					this.screenX = e.IsDefined("screenX") ? (int)e.ScreenX : 0;
					this.screenY = e.IsDefined("screenY") ? (int)e.ScreenY : 0;
				}

				this.button = e.Button;

				this.keyCode = e.IsDefined("keyCode") ? e.KeyCode : 0;
				this.charCode = e.IsDefined("charCode") ? e.CharCode : (type == "keypress" ? e.KeyCode : 0);
				this.ctrlKey = e.CtrlKey;
				this.altKey = e.AltKey;
				this.shiftKey = e.ShiftKey;
				this.metaKey = e.MetaKey;
				this.platformModifierKey = goog.userAgent.MAC ? e.MetaKey : e.CtrlKey;
				this.state = e.State;
				this.event_ = e;
				if (e.IsDefined("defaultPrevented")) {
					this.preventDefault();
				}
			}

			/// <summary>
			/// Tests to see which button was pressed during the event. This is really only
			/// useful in IE and Gecko browsers. And in IE, it's only useful for
			/// mousedown/mouseup events, because click only fires for the left mouse button.
			/// 
			/// Safari 2 only reports the left button being clicked, and uses the value '1'
			/// instead of 0. Opera only reports a mousedown event for the middle button, and
			/// no mouse events for the right button. Opera has default behavior for left and
			/// middle click that can only be overridden via a configuration setting.
			/// 
			/// There's a nice table of this mess at http://www.unixpapa.com/js/mouse.html.
			/// 
			/// </summary>
			/// <param name="button">The button</param>
			/// <returns>True if button was pressed.</returns>
			public bool isButton(MouseButton button)
			{
				if (!goog.events.BrowserFeature.HAS_W3C_BUTTON) {
					if (this.type == "click") {
						return button == MouseButton.LEFT;
					}
					else {
						return (this.button & IEButtonMap[(int)button]) != 0;
					}
				}
				else {
					return this.button == (int)button;
				}
			}

			/// <summary>
			/// Whether this has an "action"-producing mouse button.
			/// 
			/// By definition, this includes left-click on windows/linux, and left-click
			/// without the ctrl key on Macs.
			/// </summary>
			/// <returns>The result.</returns>
			public bool isMouseActionButton()
			{
				// Webkit does not ctrl+click to be a right-click, so we
				// normalize it to behave like Gecko and Opera.
				return this.isButton(MouseButton.LEFT) &&
					!(goog.userAgent.WEBKIT && goog.userAgent.MAC && this.ctrlKey);
			}


			internal void initMouseEvent(string typeArg, bool bubbles, bool cancelable,
				object view, int detail, double screenX, double screenY,
				double clientX, double clientY, bool ctrlKey, bool altKey, bool shiftKey,
				bool metaKey, MouseButton button, Bridge.Html5.EventTarget target)
			{
				this.type = typeArg;
				this.bubbles = bubbles;
				this.cancelable = cancelable;
				this.view = view;
				this.detail = detail;
				this.screenX = (int)screenX;
				this.screenY = (int)screenY;
				this.clientX = (int)clientX;
				this.clientY = (int)clientY;
				this.ctrlKey = ctrlKey;
				this.altKey = altKey;
				this.shiftKey = shiftKey;
				this.metaKey = metaKey;
				this.button = (int)button;
				this.target = target;
			}

			public override void stopPropagation()
			{
				base.stopPropagation();
				if (Script.IsDefined(this.event_.instance, "stopPropagation")) {
					this.event_.StopPropagation();
				}
				else {
					Script.Set(this.event_.instance, "cancelBubble", true);
				}
			}

			public override void preventDefault()
			{
				base.preventDefault();
				var be = this.event_;
#if false
				if (!Script.IsDefined(be, "preventDefault")) {
					be.ReturnValue = false;
					if (goog.events.BrowserFeature.SET_KEY_CODE_TO_PREVENT_DEFAULT) {
						/** @preserveTry */
						try {
							// Most keys can be prevented using returnValue. Some special keys
							// require setting the keyCode to -1 as well:
							//
							// In IE7:
							// F3, F5, F10, F11, Ctrl+P, Crtl+O, Ctrl+F (these are taken from IE6)
							//
							// In IE8:
							// Ctrl+P, Crtl+O, Ctrl+F (F1-F12 cannot be stopped through the event)
							//
							// We therefore do this for all function keys as well as when Ctrl key
							// is pressed.
							var VK_F1 = 112;
							var VK_F12 = 123;
							if (be.CtrlKey || be.KeyCode >= VK_F1 && be.KeyCode <= VK_F12) {
								be.KeyCode = -1;
							}
						}
						catch (Exception ex) {
							// IE throws an 'access denied' exception when trying to change
							// keyCode in some situations (e.g. srcElement is input[type=file],
							// or srcElement is an anchor tag rewritten by parent's innerHTML).
							// Do nothing in this case.
						}
					}
				}
				else {
					be.PreventDefault();
				}
#else
				be.PreventDefault();
#endif
			}

			/// <summary>
			/// The underlying browser event object.
			/// </summary>
			/// <returns>The underlying browser event object.</returns>
			public Bridge.Html5.Event getBrowserEvent()
			{
				return event_;
			}
		}
	}
}
