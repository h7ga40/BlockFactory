// Copyright 2007 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview This file contains a class for working with keyboard events
 * that repeat consistently across browsers and platforms. It also unifies the
 * key code so that it is the same in all browsers and platforms.
 *
 * Different web browsers have very different keyboard event handling. Most
 * importantly is that only certain browsers repeat keydown events:
 * IE, Opera, FF/Win32, and Safari 3 repeat keydown events.
 * FF/Mac and Safari 2 do not.
 *
 * For the purposes of this code, "Safari 3" means WebKit 525+, when WebKit
 * decided that they should try to match IE's key handling behavior.
 * Safari 3.0.4, which shipped with Leopard (WebKit 523), has the
 * Safari 2 behavior.
 *
 * Firefox, Safari, Opera prevent on keypress
 *
 * IE prevents on keydown
 *
 * Firefox does not fire keypress for shift, ctrl, alt
 * Firefox does fire keydown for shift, ctrl, alt, meta
 * Firefox does not repeat keydown for shift, ctrl, alt, meta
 *
 * Firefox does not fire keypress for up and down in an input
 *
 * Opera fires keypress for shift, ctrl, alt, meta
 * Opera does not repeat keypress for shift, ctrl, alt, meta
 *
 * Safari 2 and 3 do not fire keypress for shift, ctrl, alt
 * Safari 2 does not fire keydown for shift, ctrl, alt
 * Safari 3 *does* fire keydown for shift, ctrl, alt
 *
 * IE provides the keycode for keyup/down events and the charcode (in the
 * keycode field) for keypress.
 *
 * Mozilla provides the keycode for keyup/down and the charcode for keypress
 * unless it's a non text modifying key in which case the keycode is provided.
 *
 * Safari 3 provides the keycode and charcode for all events.
 *
 * Opera provides the keycode for keyup/down event and either the charcode or
 * the keycode (in the keycode field) for keypress events.
 *
 * Firefox x11 doesn't fire keydown events if a another key is already held down
 * until the first key is released. This can cause a key event to be fired with
 * a keyCode for the first key and a charCode for the second key.
 *
 * Safari in keypress
 *
 *        charCode keyCode which
 * ENTER:       13      13    13
 * F1:       63236   63236 63236
 * F8:       63243   63243 63243
 * ...
 * p:          112     112   112
 * P:           80      80    80
 *
 * Firefox, keypress:
 *
 *        charCode keyCode which
 * ENTER:        0      13    13
 * F1:           0     112     0
 * F8:           0     119     0
 * ...
 * p:          112       0   112
 * P:           80       0    80
 *
 * Opera, Mac+Win32, keypress:
 *
 *         charCode keyCode which
 * ENTER: undefined      13    13
 * F1:    undefined     112     0
 * F8:    undefined     119     0
 * ...
 * p:     undefined     112   112
 * P:     undefined      80    80
 *
 * IE7, keydown
 *
 *         charCode keyCode     which
 * ENTER: undefined      13 undefined
 * F1:    undefined     112 undefined
 * F8:    undefined     119 undefined
 * ...
 * p:     undefined      80 undefined
 * P:     undefined      80 undefined
 *
 * @author arv@google.com (Erik Arvidsson)
 * @author eae@google.com (Emil A Eklund)
 * @see ../demos/keyhandler.html
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog
{
	public static partial class events
	{
		public class KeyHandler : EventTarget
		{
			public static class EventType
			{
				public const string KEY = "key";
			}

			/// <summary>
			/// If true, the KeyEvent fires on keydown. Otherwise, it fires on keypress.
			/// </summary>
			private static readonly bool USES_KEYDOWN_ = goog.userAgent.IE ||
				goog.userAgent.EDGE ||
				goog.userAgent.WEBKIT && goog.userAgent.isVersionOrHigher("525");

			/// <summary>
			/// If true, the alt key flag is saved during the key down and reused when
			/// handling the key press. FF on Mac does not set the alt flag in the key press
			/// event.
			/// </summary>
			private static readonly bool SAVE_ALT_FOR_KEYPRESS_ =
				goog.userAgent.MAC && goog.userAgent.GECKO;

			/// <summary>
			/// This is the element that we will listen to the real keyboard events on.
			/// </summary>
			Union<DocumentInstance, HTMLElement> element_;
			/// <summary>
			/// The key for the key press listener.
			/// </summary>
			private Listener keyPressKey_;
			/// <summary>
			/// The key for the key down listener.
			/// </summary>
			private Listener keyDownKey_;
			/// <summary>
			/// The key for the key up listener.
			/// </summary>
			private Listener keyUpKey_;
			/// <summary>
			/// Used to detect keyboard repeat events.
			/// </summary>
			private KeyCodes lastKey_ = (KeyCodes)(-1);
			/// <summary>
			/// Keycode recorded for key down events. As most browsers don't report the
			/// keycode in the key press event we need to record it in the key down phase.
			/// </summary>
			private KeyCodes keyCode_ = (KeyCodes)(-1);
			/// <summary>
			/// Alt key recorded for key down events. FF on Mac does not report the alt key
			/// flag in the key press event, we need to record it in the key down phase.
			/// </summary>
			private bool altKey_;

			private bool handled_todo_;


			private static Dictionary<string, KeyCodes> safariKey_ = new Dictionary<string, KeyCodes>() {
				{ "3", KeyCodes.ENTER },             // 13
				{ "12", KeyCodes.NUMLOCK },          // 144
				{ "63232", KeyCodes.UP },            // 38
				{ "63233", KeyCodes.DOWN },          // 40
				{ "63234", KeyCodes.LEFT },          // 37
				{ "63235", KeyCodes.RIGHT },         // 39
				{ "63236", KeyCodes.F1 },            // 112
				{ "63237", KeyCodes.F2 },            // 113
				{ "63238", KeyCodes.F3 },            // 114
				{ "63239", KeyCodes.F4 },            // 115
				{ "63240", KeyCodes.F5 },            // 116
				{ "63241", KeyCodes.F6 },            // 117
				{ "63242", KeyCodes.F7 },            // 118
				{ "63243", KeyCodes.F8 },            // 119
				{ "63244", KeyCodes.F9 },            // 120
				{ "63245", KeyCodes.F10 },           // 121
				{ "63246", KeyCodes.F11 },           // 122
				{ "63247", KeyCodes.F12 },           // 123
				{ "63248", KeyCodes.PRINT_SCREEN },  // 44
				{ "63272", KeyCodes.DELETE },        // 46
				{ "63273", KeyCodes.HOME },          // 36
				{ "63275", KeyCodes.END },           // 35
				{ "63276", KeyCodes.PAGE_UP },       // 33
				{ "63277", KeyCodes.PAGE_DOWN },     // 34
				{ "63289", KeyCodes.NUMLOCK },       // 144
				{ "63302", KeyCodes.INSERT },        // 45
			};

			/// <summary>
			/// An enumeration of key identifiers currently part of the W3C draft for DOM3
			/// and their mappings to keyCodes.
			/// http://www.w3.org/TR/DOM-Level-3-Events/keyset.html#KeySet-Set
			/// This is currently supported in Safari and should be platform independent.
			/// </summary>
			private static Dictionary<string, KeyCodes> keyIdentifier_ = new Dictionary<string, KeyCodes> {
				{ "Up", KeyCodes.UP },               // 38
				{ "Down", KeyCodes.DOWN },           // 40
				{ "Left", KeyCodes.LEFT },           // 37
				{ "Right", KeyCodes.RIGHT },         // 39
				{ "Enter", KeyCodes.ENTER },         // 13
				{ "F1", KeyCodes.F1 },               // 112
				{ "F2", KeyCodes.F2 },               // 113
				{ "F3", KeyCodes.F3 },               // 114
				{ "F4", KeyCodes.F4 },               // 115
				{ "F5", KeyCodes.F5 },               // 116
				{ "F6", KeyCodes.F6 },               // 117
				{ "F7", KeyCodes.F7 },               // 118
				{ "F8", KeyCodes.F8 },               // 119
				{ "F9", KeyCodes.F9 },               // 120
				{ "F10", KeyCodes.F10 },             // 121
				{ "F11", KeyCodes.F11 },             // 122
				{ "F12", KeyCodes.F12 },             // 123
				{ "U+007F", KeyCodes.DELETE },       // 46
				{ "Home", KeyCodes.HOME },           // 36
				{ "End", KeyCodes.END },             // 35
				{ "PageUp", KeyCodes.PAGE_UP },      // 33
				{ "PageDown", KeyCodes.PAGE_DOWN },  // 34
				{ "Insert", KeyCodes.INSERT }        // 45
			};

			public KeyHandler(HTMLElement opt_element = null, bool opt_capture = false)
				: base(opt_element)
			{
				if (opt_element != null) {
					this.attach(opt_element, opt_capture);
				}
			}

			/// <summary>
			/// Records the keycode for browsers that only returns the keycode for key up/
			/// down events. For browser/key combinations that doesn't trigger a key pressed
			/// event it also fires the patched key event.
			/// </summary>
			/// <param name="e">The key down event.</param>
			private void handleKeyDown_(BrowserEvent e)
			{
				// Ctrl-Tab and Alt-Tab can cause the focus to be moved to another window
				// before we've caught a key-up event.  If the last-key was one of these we
				// reset the state.
				if (goog.userAgent.WEBKIT || goog.userAgent.EDGE) {
					if (this.lastKey_ == goog.events.KeyCodes.CTRL && !e.ctrlKey ||
						this.lastKey_ == goog.events.KeyCodes.ALT && !e.altKey ||
						goog.userAgent.MAC && this.lastKey_ == goog.events.KeyCodes.META &&
							!e.metaKey) {
						this.resetState();
					}
				}

				if (this.lastKey_ == (KeyCodes)(-1)) {
					if (e.ctrlKey && e.keyCode != (int)goog.events.KeyCodes.CTRL) {
						this.lastKey_ = goog.events.KeyCodes.CTRL;
					}
					else if (e.altKey && e.keyCode != (int)goog.events.KeyCodes.ALT) {
						this.lastKey_ = goog.events.KeyCodes.ALT;
					}
					else if (e.metaKey && e.keyCode != (int)goog.events.KeyCodes.META) {
						this.lastKey_ = goog.events.KeyCodes.META;
					}
				}

				if (goog.events.KeyHandler.USES_KEYDOWN_ &&
					!goog.events.KeyCodes_firesKeyPressEvent(
						(KeyCodes)e.keyCode, this.lastKey_, e.shiftKey, e.ctrlKey, e.altKey,
						e.metaKey)) {
					this.handleEvent(e);
				}
				else {
					this.keyCode_ = goog.events.KeyCodes_normalizeKeyCode((KeyCodes)e.keyCode);
					if (goog.events.KeyHandler.SAVE_ALT_FOR_KEYPRESS_) {
						this.altKey_ = e.altKey;
					}
				}
			}

			/// <summary>
			/// Resets the stored previous values. Needed to be called for webkit which will
			/// not generate a key up for meta key operations. This should only be called
			/// when having finished with repeat key possibilities.
			/// </summary>
			public void resetState()
			{
				this.lastKey_ = (KeyCodes)(-1);
				this.keyCode_ = (KeyCodes)(-1);
			}

			/// <summary>
			/// Clears the stored previous key value, resetting the key repeat status. Uses
			/// -1 because the Safari 3 Windows beta reports 0 for certain keys (like Home
			/// and End.)
			/// </summary>
			/// <param name="e">The keyup event.</param>
			private void handleKeyup_(BrowserEvent e)
			{
				this.resetState();
				this.altKey_ = e.altKey;
			}

			/// <summary>
			/// Handles the events on the element.
			/// </summary>
			/// <param name="e">The keyboard event sent from the
			/// browser.</param>
			public void handleEvent(BrowserEvent e)
			{
				var be = (Bridge.Html5.KeyboardEvent)e.getBrowserEvent();
				KeyCodes? keyCode;
				int charCode;
				var altKey = be.AltKey;

				// IE reports the character code in the keyCode field for keypress events.
				// There are two exceptions however, Enter and Escape.
				if (goog.userAgent.IE && e.type == goog.events.EventType.KEYPRESS) {
					keyCode = this.keyCode_;
					charCode = keyCode != goog.events.KeyCodes.ENTER &&
							keyCode != goog.events.KeyCodes.ESC ?
						be.KeyCode :
						0;

					// Safari reports the character code in the keyCode field for keypress
					// events but also has a charCode field.
				}
				else if (
					(goog.userAgent.WEBKIT || goog.userAgent.EDGE) &&
					e.type == goog.events.EventType.KEYPRESS) {
					keyCode = this.keyCode_;
					charCode = be.CharCode >= 0 && be.CharCode < 63232 &&
							goog.events.KeyCodes_isCharacterKey(keyCode.Value) ?
						be.CharCode :
						0;

					// Opera reports the keycode or the character code in the keyCode field.
				}
				else if (goog.userAgent.OPERA && !goog.userAgent.WEBKIT) {
					keyCode = this.keyCode_;
					charCode = goog.events.KeyCodes_isCharacterKey(keyCode.Value) ? be.KeyCode : 0;

					// Mozilla reports the character code in the charCode field.
				}
				else {
					keyCode = be.KeyCode != 0 ? (KeyCodes)be.KeyCode : this.keyCode_;
					charCode = be.CharCode != 0 ? be.CharCode : 0;
					if (goog.events.KeyHandler.SAVE_ALT_FOR_KEYPRESS_) {
						altKey = this.altKey_;
					}
					// On the Mac, shift-/ triggers a question mark char code and no key code
					// (normalized to WIN_KEY), so we synthesize the latter.
					if (goog.userAgent.MAC && charCode == (int)goog.events.KeyCodes.QUESTION_MARK &&
						keyCode == goog.events.KeyCodes.WIN_KEY) {
						keyCode = goog.events.KeyCodes.SLASH;
					}
				}

				keyCode = goog.events.KeyCodes_normalizeKeyCode(keyCode.Value);
				var key = keyCode.Value;
				var keyIdentifier = be.KeyIdentifier;

				// Correct the key value for certain browser-specific quirks.
				if (keyCode.HasValue) {
					if ((int)keyCode >= 63232 && goog.events.KeyHandler.safariKey_.ContainsKey(keyCode.Value.ToString())) {
						// NOTE(nicksantos): Safari 3 has fixed this problem,
						// this is only needed for Safari 2.
						key = goog.events.KeyHandler.safariKey_[keyCode.Value.ToString()];
					}
					else {
						// Safari returns 25 for Shift+Tab instead of 9.
						if ((int)keyCode == 25 && e.shiftKey) {
							key = (KeyCodes)9;
						}
					}
				}
				else if (
				  keyIdentifier != null && goog.events.KeyHandler.keyIdentifier_.ContainsKey(keyIdentifier)) {
					// This is needed for Safari Windows because it currently doesn't give a
					// keyCode/which for non printable keys.
					key = goog.events.KeyHandler.keyIdentifier_[keyIdentifier];
				}

				// If we get the same keycode as a keydown/keypress without having seen a
				// keyup event, then this event was caused by key repeat.
				var repeat = key == this.lastKey_;
				this.lastKey_ = key;

				var ev = new goog.events.KeyEvent((int)key, charCode, repeat, be);
				ev.altKey = altKey;
				this.dispatchEvent(ev);
			}

			/// <summary>
			/// Returns the element listened on for the real keyboard events.
			/// </summary>
			/// <returns>The element listened on for the real
			/// keyboard events.</returns>
			public Union<DocumentInstance, HTMLElement> getElement()
			{
				return element_;
			}

			/// <summary>
			/// Adds the proper key event listeners to the element.
			/// </summary>
			/// <param name="element">The element to listen on.</param>
			/// <param name="opt_capture">Whether to listen for browser events in
			/// capture phase (defaults to false).</param>
			public void attach(HTMLElement element, bool opt_capture = false)
			{
				if (this.keyUpKey_ != null) {
					this.detach();
				}

				this.element_ = element;

				this.keyPressKey_ = goog.events.listen(
					element_.As<Bridge.Html5.EventTarget>(), goog.events.EventType.KEYPRESS, new Action<BrowserEvent>(handleEvent), opt_capture);

				// Most browsers (Safari 2 being the notable exception) doesn't include the
				// keyCode in keypress events (IE has the char code in the keyCode field and
				// Mozilla only included the keyCode if there's no charCode). Thus we have to
				// listen for keydown to capture the keycode.
				this.keyDownKey_ = goog.events.listen(
					element_.As<Bridge.Html5.EventTarget>(), goog.events.EventType.KEYDOWN, new Action<BrowserEvent>(handleKeyDown_),
					opt_capture, this);


				this.keyUpKey_ = goog.events.listen(
					element_.As<Bridge.Html5.EventTarget>(), goog.events.EventType.KEYUP, new Action<BrowserEvent>(handleKeyup_),
					opt_capture, this);
			}

			/// <summary>
			/// Removes the listeners that may exist.
			/// </summary>
			public void detach()
			{
				if (this.keyPressKey_ != null) {
					goog.events.unlistenByKey(this.keyPressKey_);
					goog.events.unlistenByKey(this.keyDownKey_);
					goog.events.unlistenByKey(this.keyUpKey_);
					this.keyPressKey_ = null;
					this.keyDownKey_ = null;
					this.keyUpKey_ = null;
				}
				this.element_ = null;
				this.lastKey_ = (KeyCodes)(-1);
				this.keyCode_ = (KeyCodes)(-1);
			}

			public override void disposeInternal()
			{
				base.disposeInternal();
				this.detach();
			}
		}

		public class KeyEvent : BrowserEvent
		{
#if false
			/// <summary>
			/// Keycode of key press.
			/// </summary>
			public int keyCode;

			/// <summary>
			/// Unicode character code.
			/// </summary>
			public int charCode;
#endif
			/// <summary>
			/// True if this event was generated by keyboard auto-repeat (i.e., the user is
			/// holding the key down.)
			/// </summary>
			public bool repeat;

			public KeyEvent(int keyCode, int charCode, bool repeat, Bridge.Html5.Event browserEvent)
				: base(browserEvent)
			{
				this.keyCode = keyCode;
				this.charCode = charCode;
				this.repeat = repeat;
			}
		}
	}
}
