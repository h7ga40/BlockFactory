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
 * @fileoverview Core JavaScript library for Blockly.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public partial class Core
	{
		public static Msg Msg;
		public static Procedures Procedures;
		public static Variables Variables;
		public static Names Names;
		public static Blocks Blocks;
		public static ContextMenu ContextMenu;

		/// <summary>
		/// The main workspace most recently used.
		/// Set by Blockly.WorkspaceSvg.prototype.markFocused
		/// </summary>
		public static WorkspaceSvg mainWorkspace;

		/// <summary>
		/// Currently selected block.
		/// </summary>
		public static BlockSvg selected;

		/// <summary>
		/// Currently highlighted connection (during a drag).
		/// </summary>
		internal static Connection highlightedConnection_;

		/// <summary>
		/// Connection on dragged block that matches the highlighted connection.
		/// </summary>
		internal static Connection localConnection_;

		/// <summary>
		/// All of the connections on blocks that are currently being dragged.
		/// </summary>
		internal static JsArray<Connection> draggingConnections_ = new JsArray<Connection>();

		/// <summary>
		/// Contents of the local clipboard.
		/// </summary>
		internal static Element clipboardXml_;

		/// <summary>
		/// Is the mouse dragging a block?
		/// DRAG_NONE - No drag operation.
		/// DRAG_STICKY - Still inside the sticky DRAG_RADIUS.
		/// DRAG_FREE - Freely draggable.
		/// </summary>
		internal static int dragMode_ = Core.DRAG_NONE;

		/// <summary>
		/// Source of the local clipboard.
		/// </summary>
		internal static WorkspaceSvg clipboardSource_;

		static Core()
		{
			Msg = new Msg();
			ContextMenu = new ContextMenu();
		}

		/// <summary>
		/// Convert a hue (HSV model) into an RGB hex triplet.
		/// </summary>
		/// <param name="hue">Hue on a colour wheel (0-360).</param>
		/// <returns>RGB code, e.g. '#5ba65b'.</returns>
		public static string hueToRgb(double hue)
		{
			return goog.color.hsvToHex(hue, Core.HSV_SATURATION,
				Core.HSV_VALUE * 255);
		}

		/// <summary>
		/// Returns the dimensions of the specified SVG image.
		/// </summary>
		/// <param name="svg">SVG image.</param>
		/// <returns>Contains width and height properties.</returns>
		public static goog.math.Size svgSize(Element svg)
		{
			return new goog.math.Size((double)svg["cachedWidth_"], (double)svg["cachedHeight_"]);
		}

		/// <summary>
		/// Size the workspace when the contents change.  This also updates
		/// scrollbars accordingly.
		/// </summary>
		/// <param name="workspace"></param>
		public static void resizeSvgContents(WorkspaceSvg workspace)
		{
			workspace.resizeContents();
		}

		/// <summary>
		/// Size the SVG image to completely fill its container. Call this when the view
		/// actually changes sizes (e.g. on a window resize/device orientation change).
		/// See Blockly.resizeSvgContents to resize the workspace when the contents
		/// change (e.g. when a block is added or removed).
		/// Record the height/width of the SVG image.
		/// </summary>
		/// <param name="workspace">Any workspace in the SVG.</param>
		public static void svgResize(WorkspaceSvg workspace)
		{
			var mainWorkspace = workspace;
			while (mainWorkspace.options.parentWorkspace != null) {
				mainWorkspace = (WorkspaceSvg)mainWorkspace.options.parentWorkspace;
			}
			var svg = mainWorkspace.getParentSvg();
			var div = (HTMLDivElement)svg.ParentNode;
			if (div == null) {
				// Workspace deleted, or something.
				return;
			}
			var width = div.OffsetWidth;
			var height = div.OffsetHeight;
			if ((double?)svg["cachedWidth_"] != width) {
				svg.SetAttribute("width", width + "px");
				svg["cachedWidth_"] = width;
			}
			if ((double?)svg["cachedHeight_"] != height) {
				svg.SetAttribute("height", height + "px");
				svg["cachedHeight_"] = height;
			}
			mainWorkspace.resize();
		}

		private static void onKeyDown_(Event e)
		{
			if (Core.mainWorkspace.options.readOnly || Core.isTargetInput_(e)) {
				// No key actions on readonly workspaces.
				// When focused on an HTML text input widget, don't trap any keys.
				return;
			}
			var deleteBlock = false;
			if (e.KeyCode == 27) {
				// Pressing esc closes the context menu.
				Core.hideChaff();
			}
			else if (e.KeyCode == 8 || e.KeyCode == 46) {
				// Delete or backspace.
				// Stop the browser from going back to the previous page.
				// Do this first to prevent an error in the delete code from resulting in
				// data loss.
				e.PreventDefault();
				if (Core.selected != null && Core.selected.isDeletable()) {
					deleteBlock = true;
				}
			}
			else if (e.AltKey || e.CtrlKey || e.MetaKey) {
				if (Core.selected != null &&
					Core.selected.isDeletable() && Core.selected.isMovable()) {
					if (e.KeyCode == 67) {
						// 'c' for copy.
						Core.hideChaff();
						Core.copy_(Core.selected);
					}
					else if (e.KeyCode == 88) {
						// 'x' for cut.
						Core.copy_(Core.selected);
						deleteBlock = true;
					}
				}
				if (e.KeyCode == 86) {
					// 'v' for paste.
					if (Core.clipboardXml_ != null) {
						Events.setGroup(true);
						Core.clipboardSource_.paste(Core.clipboardXml_);
						Events.setGroup(false);
					}
				}
				else if (e.KeyCode == 90) {
					// 'z' for undo 'Z' is for redo.
					Core.hideChaff();
					Core.mainWorkspace.undo(e.ShiftKey);
				}
			}
			if (deleteBlock) {
				// Common code for delete and cut.
				Events.setGroup(true);
				Core.hideChaff();
				var heal = Core.dragMode_ != Core.DRAG_FREE;
				Core.selected.dispose(heal, true);
				if (Core.highlightedConnection_ != null) {
					((RenderedConnection)Core.highlightedConnection_).unhighlight();
					Core.highlightedConnection_ = null;
				}
				Events.setGroup(false);
			}
		}

		/// <summary>
		/// Stop binding to the global mouseup and mousemove events.
		/// </summary>
		internal static void terminateDrag_()
		{
			BlockSvg.terminateDrag();
			Flyout.terminateDrag_();
		}

		/// <summary>
		/// Copy a block onto the local clipboard.
		/// </summary>
		/// <param name="block">Block to be copied.</param>
		private static void copy_(Block block)
		{
			var xmlBlock = Xml.blockToDom(block);
			if (Core.dragMode_ != Core.DRAG_FREE) {
				Xml.deleteNext(xmlBlock);
			}
			// Encode start position in XML.
			var xy = block.getRelativeToSurfaceXY();
			xmlBlock.SetAttribute("x", (block.RTL ? -xy.x : xy.x).ToString());
			xmlBlock.SetAttribute("y", xy.y.ToString());
			Core.clipboardXml_ = xmlBlock;
			Core.clipboardSource_ = (WorkspaceSvg)block.workspace;
		}

		/// <summary>
		/// Duplicate this block and its children.
		/// </summary>
		/// <param name="block">Block to be copied.</param>
		internal static void duplicate_(Block block)
		{
			// Save the clipboard.
			var clipboardXml = Core.clipboardXml_;
			var clipboardSource = Core.clipboardSource_;

			// Create a duplicate via a copy/paste operation.
			Core.copy_(block);
			((WorkspaceSvg)block.workspace).paste(Core.clipboardXml_);

			// Restore the clipboard.
			Core.clipboardXml_ = clipboardXml;
			Core.clipboardSource_ = clipboardSource;
		}

		/// <summary>
		/// Cancel the native context menu, unless the focus is on an HTML input widget.
		/// </summary>
		/// <param name="e">Mouse down ev.</param>
		private static void onContextMenu_(Event e)
		{
			if (!Core.isTargetInput_(e)) {
				// When focused on an HTML text input widget, don't cancel the context menu.
				e.PreventDefault();
			}
		}

		/// <summary>
		/// Close tooltips, context menus, dropdown selections, etc.
		/// </summary>
		/// <param name="opt_allowToolbox">If true, don't close the toolbox.</param>
		public static void hideChaff(bool opt_allowToolbox = false)
		{
			Tooltip.hide();
			WidgetDiv.hide();
			if (!opt_allowToolbox) {
				var workspace = Core.getMainWorkspace();
				if (workspace.toolbox_ != null &&
					workspace.toolbox_.flyout_ != null &&
					workspace.toolbox_.flyout_.autoClose) {
					workspace.toolbox_.clearSelection();
				}
			}
		}

		/// <summary>
		/// When something in Blockly's workspace changes, call a function.
		/// </summary>
		/// <param name="func">Function to call.</param>
		/// <returns>Opaque data that can be passed to removeChangeListener.</returns>
		public static Action<Events.Abstract> addChangeListener(Action<Events.Abstract> func)
		{
			// Backwards compatability from before there could be multiple workspaces.
			Console.WriteLine("Deprecated call to Blockly.addChangeListener, " +
						 "use workspace.addChangeListener instead.");
			return getMainWorkspace().addChangeListener(func);
		}

		/// <summary>
		/// Returns the main workspace.  Returns the last used main workspace (based on
		/// focus).  Try not to use this function, particularly if there are multiple
		/// Blockly instances on a page.
		/// </summary>
		/// <returns>The main workspace.</returns>
		public static WorkspaceSvg getMainWorkspace()
		{
			return mainWorkspace;
		}

		internal static Dictionary<string, Action<FlyoutButton>> flyoutButtonCallbacks_ = new Dictionary<string, Action<FlyoutButton>>();

		/// <summary>
		/// Register a callback function associated with a given key, for clicks on
		/// buttons and labels in the flyout.
		/// For instance, a button specified by the XML
		/// <button text="create variable" callbackKey="CREATE_VARIABLE"></button>
		/// should be matched by a call to
		/// registerButtonCallback("CREATE_VARIABLE", yourCallbackFunction).
		/// </summary>
		/// <param name="key">The name to use to look up this function.</param>
		/// <param name="func">The function to call when the
		/// given button is clicked.</param>
		internal static void registerButtonCallback(string key, Action<FlyoutButton> func)
		{
			Core.flyoutButtonCallbacks_[key] = func;
		}

		/// <summary>
		/// Wrapper to window.alert() that app developers may override to
		/// provide alternatives to the modal browser window.
		/// </summary>
		/// <param name="message">The message to display to the user.</param>
		/// <param name="opt_callback">The callback when the alert is dismissed.</param>
		internal static void alert(string message, Action opt_callback = null)
		{
			Window.Alert(message);
			opt_callback?.Invoke();
		}

		/// <summary>
		/// Wrapper to window.prompt() that app developers may override to provide
		/// alternatives to the modal browser window. Built-in browser prompts are
		/// often used for better text input experience on mobile device. We strongly
		/// recommend testing mobile when overriding this.
		/// </summary>
		/// <param name="message">The message to display to the user.</param>
		/// <param name="defaultValue">The value to initialize the prompt with.</param>
		/// <param name="callback">The callback for handling user reponse.</param>
		internal static void prompt(string message, string defaultValue, Action<string> callback)
		{
			callback(Window.Prompt(message, defaultValue));
		}

		/// <summary>
		/// PID of queued long-press task.
		/// </summary>
		internal static int longPid_;
		internal static JsArray<EventWrapInfo> onMouseMoveWrapper_;

		/// <summary>
		/// Context menus on touch devices are activated using a long-press.
		/// Unfortunately the contextmenu touch event is currently (2015) only suported
		/// by Chrome.  This function is fired on any touchstart event, queues a task,
		/// which after about a second opens the context menu.  The tasks is killed
		/// if the touch event terminates early.
		/// </summary>
		/// <param name="e">Touch start event.</param>
		/// <param name="uiObject">The block or workspace
		/// under the touchstart event.</param>
		internal static void longStart_(TouchEvent e, Union<BlockSvg, WorkspaceSvg> uiObject)
		{
			Core.longStop_(e);
			Core.longPid_ = Window.SetTimeout(() => {
				e.Button = 2;  // Simulate a right button click.
				if (uiObject.Is<BlockSvg>())
					uiObject.As<BlockSvg>().onMouseDown_(e);
				else
					uiObject.As<WorkspaceSvg>().onMouseDown_(e);
			}, Core.LONGPRESS);
		}

		/// <summary>
		/// Nope, that's not a long-press.  Either touchend or touchcancel was fired,
		/// or a drag hath begun.  Kill the queued long-press task.
		/// </summary>
		internal static void longStop_(Event e)
		{
			if (Core.longPid_ != 0) {
				Window.ClearTimeout(Core.longPid_);
				Core.longPid_ = 0;
			}
		}

		/// <summary>
		/// Handle a mouse-up anywhere on the page.
		/// </summary>
		/// <param name="e">Mouse up event.</param>
		internal static void onMouseUp_(Event e)
		{
			var workspace = Core.getMainWorkspace();
			if (workspace.dragMode_ == Core.DRAG_NONE) {
				return;
			}
			Touch.clearTouchIdentifier();
			Css.setCursor(Css.Cursor.OPEN);
			workspace.dragMode_ = Core.DRAG_NONE;
			// Unbind the touch event if it exists.
			if (Touch.onTouchUpWrapper_ != null) {
				Core.unbindEvent_(Touch.onTouchUpWrapper_);
				Touch.onTouchUpWrapper_ = null;
			}
			if (Core.onMouseMoveWrapper_ != null) {
				Core.unbindEvent_(Core.onMouseMoveWrapper_);
				Core.onMouseMoveWrapper_ = null;
			}
		}

		/// <summary>
		/// Handle a mouse-move on SVG drawing surface.
		/// </summary>
		/// <param name="e">Mouse move event.</param>
		internal static void onMouseMove_(Event e)
		{
			var workspace = Core.getMainWorkspace();
			if (workspace.dragMode_ != Core.DRAG_NONE) {
				var dx = (double)e.ClientX - workspace.startDragMouseX;
				var dy = (double)e.ClientY - workspace.startDragMouseY;
				var metrics = workspace.startDragMetrics;
				var x = workspace.startScrollX + dx;
				var y = workspace.startScrollY + dy;
				x = System.Math.Min(x, -metrics.contentLeft);
				y = System.Math.Min(y, -metrics.contentTop);
				x = System.Math.Max(x, metrics.viewWidth - metrics.contentLeft -
							 metrics.contentWidth);
				y = System.Math.Max(y, metrics.viewHeight - metrics.contentTop -
							 metrics.contentHeight);

				// Move the scrollbars and the page will scroll automatically.
				workspace.scrollbar.set(-x - metrics.contentLeft,
										-y - metrics.contentTop);
				// Cancel the long-press if the drag has moved too far.
				if (System.Math.Sqrt(dx * dx + dy * dy) > Core.DRAG_RADIUS) {
					Core.longStop_(e);
					workspace.dragMode_ = Core.DRAG_FREE;
				}
				e.StopPropagation();
				e.PreventDefault();
			}
		}
	}

	public class Metrics
	{
		///<summary>Height of the visible rectangle</summary>
		public double viewHeight;
		///<summary>Width of the visible rectangle</summary>
		public double viewWidth;
		///<summary>Height of the contents</summary>
		public double contentHeight;
		///<summary>Width of the contents</summary>
		public double contentWidth;
		///<summary>Offset of top edge of visible rectangle from parent</summary>
		public double viewTop;
		///<summary>Offset of the top-most content from the y=0 coordinate</summary>
		public double contentTop;
		///<summary>Top-edge of view</summary>
		public double absoluteTop;
		///<summary>Offset of the left edge of visible rectangle from parent</summary>
		public double viewLeft;
		///<summary>Offset of the left-most content from the x=0 coordinate</summary>
		public double contentLeft;
		///<summary>Left-edge of view</summary>
		public double absoluteLeft;
		public double toolboxWidth;
		public double toolboxHeight;
		public double flyoutWidth;
		public double flyoutHeight;
		internal int toolboxPosition;
		internal double x;
		internal double y;
	}

	public class Rectangle
	{
		public goog.math.Coordinate topLeft;
		public goog.math.Coordinate bottomRight;
	}

	public class ContextMenuOption
	{
		public bool enabled;
		public string text;
		public Action<goog.events.Event> callback;
	}

	public class EventWrapInfo
	{
		public Node node;
		public string name;
		public Delegate func;

		public EventWrapInfo(Node node, string name, Delegate func)
		{
			this.node = node;
			this.name = name;
			this.func = func;
		}
	}

	public class CaseInsensitiveCompare : IComparer<string>
	{
		public int Compare(string x, string y)
		{
			return x.ToLower().CompareTo(y.ToLower());
		}

		public static CaseInsensitiveCompare caseInsensitiveCompare = new CaseInsensitiveCompare();
	}
}
