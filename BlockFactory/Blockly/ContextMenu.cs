/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2011 Google Inc.
 * https://developers.google.com/blockly/
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * @fileoverview Functionality for the right-click context menus.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	public class ContextMenu
	{
		/// <summary>
		/// Which block is the context menu attached to?
		/// </summary>
		public static BlockSvg currentBlock;

		/// <summary>
		/// Construct the menu based on the list of options and show the menu.
		/// </summary>
		/// <param name="e">Mouse event.</param>
		/// <param name="options">Array of menu options.</param>
		/// <param name="rtl">True if RTL, false if LTR.</param>
		public static void show(MouseEvent e, ContextMenuOption[] options, bool rtl)
		{
			WidgetDiv.show(Core.ContextMenu, rtl, null);
			if (options.Length == 0) {
				ContextMenu.hide();
				return;
			}
			/* Here's what one option object looks like:
			  {text: 'Make It So',
			   enabled: true,
			   callback: Blockly.MakeItSo}
			*/
			var menu = new goog.ui.Menu();
			menu.setRightToLeft(rtl);
			foreach (var option in options) {
				var menuItem = new goog.ui.MenuItem(option.text);
				menuItem.setRightToLeft(rtl);
				menu.addChild(menuItem, true);
				menuItem.setEnabled(option.enabled);
				if (option.enabled) {
					goog.events.listen(menuItem, goog.ui.Component.EventType.ACTION,
									   option.callback);
				}
			}
			goog.events.listen(menu, goog.ui.Component.EventType.ACTION,
							new Action<goog.events.Event>((ev) => ContextMenu.hide()));
			// Record windowSize and scrollOffset before adding menu.
			var windowSize = goog.dom.getViewportSize();
			var scrollOffset = goog.style.getViewportPageOffset(Document.Instance);
			var div = WidgetDiv.DIV;
			menu.render(div);
			var menuDom = menu.getElement();
			Core.addClass_(menuDom, "blocklyContextMenu");
			// Prevent system context menu when right-clicking a Blockly context menu.
			Core.bindEventWithChecks_(menuDom, "contextmenu", null, new Action<Event>(Core.noEvent));
			// Record menuSize after adding menu.
			var menuSize = goog.style.getSize(menuDom);

			// Position the menu.
			var x = e.ClientX + scrollOffset.x;
			var y = e.ClientY + scrollOffset.y;
			// Flip menu vertically if off the bottom.
			if (e.ClientY + menuSize.height >= windowSize.height) {
				y -= menuSize.height;
			}
			// Flip menu horizontally if off the edge.
			if (rtl) {
				if (menuSize.width >= e.ClientX) {
					x += menuSize.width;
				}
			}
			else {
				if (e.ClientX + menuSize.width >= windowSize.width) {
					x -= menuSize.width;
				}
			}
			WidgetDiv.position(x, y, windowSize, scrollOffset, rtl);

			menu.setAllowAutoFocus(true);
			// 1ms delay is required for focusing on context menus because some other
			// mouse event is still waiting in the queue and clears focus.
			Window.SetTimeout(() => { menuDom.Focus(); }, 1);
			ContextMenu.currentBlock = null;  // May be set by Blockly.Block.
		}

		/// <summary>
		/// Hide the context menu.
		/// </summary>
		public static void hide()
		{
			WidgetDiv.hideIfOwner(Core.ContextMenu);
			ContextMenu.currentBlock = null;
		}

		/// <summary>
		/// Create a callback function that creates and configures a block,
		/// then places the new block next to the original.
		/// </summary>
		/// <param name="block">block Original block.</param>
		/// <param name="xml">XML representation of new block.</param>
		/// <returns>Function that creates a block.</returns>
		public static Action<goog.events.Event> callbackFactory(Block block, Element xml)
		{
			return new Action<goog.events.Event>((e) => {
				Events.disable();
				BlockSvg newBlock;
				try {
					newBlock = (BlockSvg)Xml.domToBlock(xml, block.workspace);
					// Move the new block next to the old block.
					var xy = block.getRelativeToSurfaceXY();
					if (block.RTL) {
						xy.x -= Core.SNAP_RADIUS;
					}
					else {
						xy.x += Core.SNAP_RADIUS;
					}
					xy.y += Core.SNAP_RADIUS * 2;
					newBlock.moveBy(xy.x, xy.y);
				}
				finally {
					Events.enable();
				}
				if (Events.isEnabled() && !newBlock.isShadow()) {
					Events.fire(new Events.Create(newBlock));
				}
				newBlock.select();
			});
		}
	}
}
