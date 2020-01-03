/**
 * @license
 * Visual Blocks Editor
 *
 * Copyright 2012 Google Inc.
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
 * @fileoverview Loading and saving blocks with localStorage and cloud storage.
 * @author q.neutron@gmail.com (Quynh Neutron)
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Bridge.Html5;

namespace Blockly
{
	/**
     * Backup code blocks to localStorage.
     * @param {!Blockly.WorkspaceSvg} workspace Workspace.
     * @private
     */
	public static class BlocklyStorage
	{
		internal static string HTTPREQUEST_ERROR;
		internal static string LINK_ALERT;
		internal static string HASH_ERROR;
		internal static string XML_ERROR;

		internal static bool InWindow()
		{
			return true;
		}

		/// <summary>
		/// Backup code blocks to localStorage.
		/// </summary>
		/// <param name="workspace">Workspace.</param>
		private static void backupBlocks_(WorkspaceSvg workspace)
		{
			if (Window.LocalStorage != null) {
				var xml = Blockly.Xml.workspaceToDom(workspace);
				// Gets the current URL, not including the hash.
				var url = Window.Location.Href.Split('#')[0];
				Window.LocalStorage.SetItem(url, Blockly.Xml.domToText(xml));
			}
		}

		/// <summary>
		/// Bind the localStorage backup function to the unload event.
		/// </summary>
		/// <param name="opt_workspace">Workspace.</param>
		public static void backupOnUnload(WorkspaceSvg opt_workspace = null)
		{
			var workspace = opt_workspace ?? Blockly.Core.getMainWorkspace();
			Window.AddEventListener("unload",
				new Action(() => { BlocklyStorage.backupBlocks_(workspace); }), false);
		}

		/// <summary>
		/// Restore code blocks from localStorage.
		/// </summary>
		/// <param name="opt_workspace">Workspace.</param>
		public static void restoreBlocks(WorkspaceSvg opt_workspace = null)
		{
			var url = Window.Location.Href.Split('#')[0];
			if (Window.LocalStorage != null && Window.LocalStorage[url] != null) {
				var workspace = opt_workspace ?? Blockly.Core.getMainWorkspace();
				var xml = Blockly.Xml.textToDom((string)Window.LocalStorage[url]);
				Blockly.Xml.domToWorkspace(xml, workspace);
			}
		}

		/// <summary>
		/// Save blocks to database and return a link containing key to XML.
		/// </summary>
		/// <param name="opt_workspace">Workspace.</param>
		public static void link(WorkspaceSvg opt_workspace = null)
		{
			var workspace = opt_workspace ?? Blockly.Core.getMainWorkspace();
			var xml = Blockly.Xml.workspaceToDom(workspace);
			var data = Blockly.Xml.domToText(xml);
			BlocklyStorage.makeRequest_("/storage", "xml", data, workspace);
		}

		/// <summary>
		/// Retrieve XML text from database using given key.
		/// </summary>
		/// <param name="key">Key to XML, obtained from href.</param>
		/// <param name="opt_workspace">Workspace.</param>
		public static void retrieveXml(string key, WorkspaceSvg opt_workspace = null)
		{
			var workspace = opt_workspace ?? Blockly.Core.getMainWorkspace();
			BlocklyStorage.makeRequest_("/storage", "key", key, workspace);
		}

		/// <summary>
		/// Global reference to current AJAX request.
		/// </summary>
		private static XMLHttpRequest httpRequest_ = null;

		/// <summary>
		/// Fire a new AJAX request.
		/// </summary>
		/// <param name="url">URL to fetch.</param>
		/// <param name="name">Name of parameter.</param>
		/// <param name="content">Content of parameter.</param>
		/// <param name="workspace">Workspace.</param>
		private static void makeRequest_(string url, string name, string content, WorkspaceSvg workspace)
		{
			if (BlocklyStorage.httpRequest_ != null) {
				// AJAX call is in-flight.
				BlocklyStorage.httpRequest_.Abort();
			}
			BlocklyStorage.httpRequest_ = new XMLHttpRequest();
			BlocklyStorage.httpRequest_.Name = name;
			BlocklyStorage.httpRequest_.OnReadyStateChange =
				new Action(BlocklyStorage.handleRequest_);
			BlocklyStorage.httpRequest_.Open("POST", url);
			BlocklyStorage.httpRequest_.SetRequestHeader("Content-Type",
				"application/x-www-form-urlencoded");
			BlocklyStorage.httpRequest_.Send(name + "=" + Script.EncodeURIComponent(content));
			BlocklyStorage.httpRequest_["workspace"] = workspace;
		}

		/// <summary>
		/// Callback function for AJAX call.
		/// </summary>
		private static void handleRequest_()
		{
			if (BlocklyStorage.httpRequest_.ReadyState == 4) {
				if (BlocklyStorage.httpRequest_.Status != 200) {
					BlocklyStorage.alert(BlocklyStorage.HTTPREQUEST_ERROR + "\n" +
						"httpRequest_.status: " + BlocklyStorage.httpRequest_.Status);
				}
				else {
					var data = BlocklyStorage.httpRequest_.ResponseText.Trim();
					if (BlocklyStorage.httpRequest_.Name == "xml") {
						Window.Location.Hash = data;
						BlocklyStorage.alert(BlocklyStorage.LINK_ALERT.Replace("%1",
							Window.Location.Href));
					}
					else if (BlocklyStorage.httpRequest_.Name == "key") {
						if (data.Length == 0) {
							BlocklyStorage.alert(BlocklyStorage.HASH_ERROR.Replace("%1",
								Window.Location.Hash));
						}
						else {
							BlocklyStorage.loadXml_(data, (WorkspaceSvg)BlocklyStorage.httpRequest_["workspace"]);
						}
					}
					BlocklyStorage.monitorChanges_((WorkspaceSvg)BlocklyStorage.httpRequest_["workspace"]);
				}
				BlocklyStorage.httpRequest_ = null;
			}
		}

		/// <summary>
		/// Start monitoring the workspace.  If a change is made that changes the XML,
		/// clear the key from the URL.  Stop monitoring the workspace once such a
		/// change is detected.
		/// </summary>
		/// <param name="workspace">Workspace.</param>
		private static void monitorChanges_(WorkspaceSvg workspace)
		{
			var startXmlDom = Blockly.Xml.workspaceToDom(workspace);
			var startXmlText = Blockly.Xml.domToText(startXmlDom);
			Action<Events.Abstract> bindData = null;
			var change = new Action<Events.Abstract>((e) => {
				var xmlDom = Blockly.Xml.workspaceToDom(workspace);
				var xmlText = Blockly.Xml.domToText(xmlDom);
				if (startXmlText != xmlText) {
					Window.Location.Hash = "";
					workspace.removeChangeListener(bindData);
				}
			});
			bindData = workspace.addChangeListener(change);
		}

		/// <summary>
		/// Load blocks from XML.
		/// </summary>
		/// <param name="xml">Text representation of XML.</param>
		/// <param name="workspace">Workspace.</param>
		private static void loadXml_(string xmlStr, WorkspaceSvg workspace)
		{
			Element xml;
			try {
				xml = Blockly.Xml.textToDom(xmlStr);
			}
			catch (Exception) {
				BlocklyStorage.alert(BlocklyStorage.XML_ERROR + "\nXML: " + xmlStr);
				return;
			}
			// Clear the workspace to avoid merge.
			workspace.clear();
			Blockly.Xml.domToWorkspace(xml, workspace);
		}

		/// <summary>
		/// Present a text message to the user.
		/// Designed to be overridden if an app has custom dialogs, or a butter bar.
		/// </summary>
		/// <param name="message">Text to alert.</param>
		public static void alert(string message)
		{
			Window.Alert(message);
		}
	}
}
