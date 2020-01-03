/*
 * @license
 * Blockly Demos: Block Factory
 *
 * Copyright 2016 Google Inc.
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

/*
 * @fileoverview Javascript for Block Library's Storage Class.
 * Depends on Block Library for its namespace.
 *
 * @author quachtina96 (Tina Quach)
 */

using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	public class BlockLibraryStorage
	{
		public string name;
		public Dictionary<string, object> blocks { get; private set; }

		/// <summary>
		/// Represents a block library's storage.
		/// </summary>
		/// <param name="blockLibraryName"> Desired name of Block Library, also used
		/// to create the key for where it's stored in local storage.</param>
		/// <param name="opt_blocks">Object mapping block type to XML.</param>
		public BlockLibraryStorage(string blockLibraryName, Dictionary<string, object> opt_blocks = null)
		{
			// Add prefix to this.name to avoid collisions in local storage.
			this.name = "BlockLibraryStorage." + blockLibraryName;
			if (opt_blocks == null) {
				// Initialize this.blocks by loading from local storage.
				this.loadFromLocalStorage();
				if (this.blocks == null) {
					this.blocks = new Dictionary<string, object>();
					// The line above is equivalent of {} except that this object is TRULY
					// empty. It doesn't have built-in attributes/functions such as length or
					// toString.
					this.saveToLocalStorage();
				}
			}
			else {
				this.blocks = opt_blocks;
				this.saveToLocalStorage();
			}
		}

		/// <summary>
		/// Reads the named block library from local storage and saves it in this.blocks.
		/// </summary>
		public void loadFromLocalStorage()
		{
			// goog.global is synonymous to window, and allows for flexibility
			// between browsers.
			var @object = Window.LocalStorage[this.name];
			this.blocks = @object != null ? (Dictionary<string, object>)JSON.Parse((string)@object) : null;
		}

		/// <summary>
		/// Writes the current block library (this.blocks) to local storage.
		/// </summary>
		public void saveToLocalStorage()
		{
			Window.LocalStorage[this.name] = JSON.Stringify(this.blocks);
		}

		/// <summary>
		/// Clears the current block library.
		/// </summary>
		public void clear()
		{
			this.blocks = new Dictionary<string, object>();
			// The line above is equivalent of {} except that this object is TRULY
			// empty. It doesn't have built-in attributes/functions such as length or
			// toString.
		}

		/// <summary>
		/// Saves block to block library.
		/// </summary>
		/// <param name="blockType">Type of block.</param>
		/// <param name="blockXML">The block's XML pulled from workspace.</param>
		public void addBlock(string blockType, Element blockXML)
		{
			var prettyXml = Blockly.Xml.domToPrettyText(blockXML);
			this.blocks[blockType] = prettyXml;
		}

		/// <summary>
		/// Removes block from current block library (this.blocks).
		/// </summary>
		/// <param name="blockType"> Type of block.</param>
		public void removeBlock(string blockType)
		{
			Script.DeleteMemebr(this.blocks, blockType);
		}

		/// <summary>
		/// Returns the XML of given block type stored in current block library
		/// (this.blocks).
		/// </summary>
		/// <param name="blockType"> Type of block.</param>
		/// <returns>The XML that represents the block type or null.</returns>
		public Element getBlockXml(string blockType)
		{
			if (this.blocks.TryGetValue(blockType, out var xml)) {
				return Blockly.Xml.textToDom(xml.ToString());
			}
			return null;
		}


		/// <summary>
		/// Returns map of each block type to its corresponding XML stored in current
		/// block library (this.blocks).
		/// </summary>
		/// <param name="blockTypes"> Types of blocks.</param>
		/// <returns>Map of block type to corresponding XML.</returns>
		public Dictionary<string, Element> getBlockXmlMap(JsArray<string> blockTypes)
		{
			var blockXmlMap = new Dictionary<string, Element>();
			for (var i = 0; i < blockTypes.Length; i++) {
				var blockType = blockTypes[i];
				var xml = this.getBlockXml(blockType);
				blockXmlMap[blockType] = xml;
			}
			return blockXmlMap;
		}

		/// <summary>
		/// Returns array of all block types stored in current block library.
		/// </summary>
		/// <returns>Array of block types stored in library.</returns>
		public JsArray<string> getBlockTypes()
		{
			return new JsArray<string>(this.blocks.Keys);
		}

		/// <summary>
		/// Checks to see if block library is empty.
		/// </summary>
		/// <returns>True if empty, false otherwise.</returns>
		public bool isEmpty()
		{
			foreach (var blockType in this.blocks) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Returns array of all block types stored in current block library.
		/// </summary>
		/// <returns>Map of block type to corresponding XML text.</returns>
		public Dictionary<string, object> getBlockXmlTextMap()
		{
			return this.blocks;
		}

		/// <summary>
		/// Returns boolean of whether or not a given blockType is stored in block
		/// library.
		/// </summary>
		/// <param name="blockType"> Type of block.</param>
		/// <returns>Whether or not blockType is stored in block library.</returns>
		public bool has(string blockType)
		{
			return this.blocks.ContainsKey(blockType);
		}
	}
}
