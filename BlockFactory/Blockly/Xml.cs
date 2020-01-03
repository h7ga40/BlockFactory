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
 * @fileoverview XML reader and writer.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using Bridge;
using Bridge.Html5;
using System.Text.RegularExpressions;

namespace Blockly
{
	public static class Xml
	{
		/// <summary>
		/// Encode a block tree as XML.
		/// </summary>
		/// <param name="workspace">workspace The workspace containing blocks.</param>
		/// <param name="opt_noId">True if the encoder should skip the block ids.</param>
		/// <returns>XML document.</returns>
		public static Element workspaceToDom(Workspace workspace, bool opt_noId = false)
		{
			var xml = goog.dom.createDom("xml");
			var blocks = workspace.getTopBlocks(true);
			foreach (var block in blocks) {
				xml.AppendChild(Xml.blockToDomWithXY(block, opt_noId));
			}
			return xml;
		}

		/// <summary>
		/// Encode a block subtree as XML with XY coordinates.
		/// </summary>
		/// <param name="block">The root block to encode.</param>
		/// <param name="opt_noId">True if the encoder should skip the block id.</param>
		/// <returns>Tree of XML elements.</returns>
		public static Element blockToDomWithXY(Block block, bool opt_noId = false)
		{
			var width = 0.0;  // Not used in LTR.
			if (block.workspace.RTL) {
				width = block.workspace.getWidth();
			}
			var element = Xml.blockToDom(block, opt_noId);
			var xy = block.getRelativeToSurfaceXY();
			element.SetAttribute("x",
				System.Math.Round(block.workspace.RTL ? width - xy.x : xy.x).ToString());
			element.SetAttribute("y", System.Math.Round(xy.y).ToString());
			return element;
		}

		/// <summary>
		/// Encode a block subtree as XML.
		/// </summary>
		/// <param name="block">The root block to encode.</param>
		/// <returns>Tree of XML elements.</returns>
		public static Element blockToDom(Block block, bool opt_noId = false)
		{
			var element = goog.dom.createDom(block.isShadow() ? "shadow" : "block");
			element.SetAttribute("type", block.type);
			if (!opt_noId) {
				element.SetAttribute("id", block.id);
			}
			if (true/*block.mutationToDom*/) {
				// Custom data for an advanced block.
				var mutation = block.mutationToDom();
				if (mutation != null && (mutation.HasChildNodes() || mutation.Attributes.Length > 0)) {
					element.AppendChild(mutation);
				}
			}
			Element container = null;
			var fieldToDom = new Action<Field>((field) => {
				if (field.name != null && field.EDITABLE) {
					container = goog.dom.createDom("field", null, field.getValue());
					container.SetAttribute("name", field.name);
					element.AppendChild(container);
				}
			});
			foreach (var input in block.inputList) {
				foreach (var field in input.fieldRow) {
					fieldToDom(field);
				}
			}

			var commentText = block.getCommentText();
			if (!String.IsNullOrEmpty(commentText)) {
				var commentElement = goog.dom.createDom("comment", null, commentText);
				if (((BlockSvg)block).comment != null) {
					commentElement.SetAttribute("pinned", ((BlockSvg)block).comment.isVisible().ToString());
					var hw = ((BlockSvg)block).comment.getBubbleSize();
					commentElement.SetAttribute("h", hw.height.ToString());
					commentElement.SetAttribute("w", hw.width.ToString());
				}
				element.AppendChild(commentElement);
			}

			if (block.data != null) {
				var dataElement = goog.dom.createDom("data", null, block.data);
				element.AppendChild(dataElement);
			}

			Element shadow;
			foreach (var input in block.inputList) {
				var empty = true;
				if (input.type == Core.DUMMY_INPUT) {
					continue;
				}
				else {
					var childBlock = input.connection.targetBlock();
					if (input.type == Core.INPUT_VALUE) {
						container = goog.dom.createDom("value");
					}
					else if (input.type == Core.NEXT_STATEMENT) {
						container = goog.dom.createDom("statement");
					}
					shadow = input.connection.getShadowDom();
					if (shadow != null && (childBlock == null || !childBlock.isShadow())) {
						container.AppendChild(Xml.cloneShadow_(shadow));
					}
					if (childBlock != null) {
						container.AppendChild(Xml.blockToDom(childBlock, opt_noId));
						empty = false;
					}
				}
				container.SetAttribute("name", input.name);
				if (!empty) {
					element.AppendChild(container);
				}
			}
			if (block.inputsInlineDefault != block.inputsInline) {
				element.SetAttribute("inline", block.inputsInline.ToString());
			}
			if (block.isCollapsed()) {
				element.SetAttribute("collapsed", true.ToString());
			}
			if (block.disabled) {
				element.SetAttribute("disabled", true.ToString());
			}
			if (!block.isDeletable() && !block.isShadow()) {
				element.SetAttribute("deletable", false.ToString());
			}
			if (!block.isMovable() && !block.isShadow()) {
				element.SetAttribute("movable", false.ToString());
			}
			if (!block.isEditable()) {
				element.SetAttribute("editable", false.ToString());
			}

			var nextBlock = block.getNextBlock();
			if (nextBlock != null) {
				container = goog.dom.createDom("next", null,
					Xml.blockToDom(nextBlock, opt_noId));
				element.AppendChild(container);
			}
			shadow = block.nextConnection?.getShadowDom();
			if (shadow != null && (nextBlock == null || !nextBlock.isShadow())) {
				container.AppendChild(Xml.cloneShadow_(shadow));
			}

			return element;
		}

		/// <summary>
		/// Deeply clone the shadow's DOM so that changes don't back-wash to the block.
		/// </summary>
		/// <param name="shadow">A tree of XML elements.</param>
		/// <returns>A tree of XML elements.</returns>
		private static Element cloneShadow_(Element shadow)
		{
			shadow = (Element)shadow.CloneNode(true);
			// Walk the tree looking for whitespace.  Don't prune whitespace in a tag.
			var node = shadow;
			Node textNode;
			while (node != null) {
				if (node.FirstChild != null) {
					node = (Element)node.FirstChild;
				}
				else {
					while (node != null && node.NextSibling == null) {
						textNode = node;
						node = (Element)node.ParentNode;
						if (textNode.NodeType == NodeType.Text && ((Text)textNode).Data.Trim() == "" &&
							node.FirstChild != textNode) {
							// Prune whitespace after a tag.
							goog.dom.removeNode(textNode);
						}
					}
					if (node != null) {
						textNode = node;
						node = (Element)node.NextSibling;
						if (textNode.NodeType == NodeType.Text && ((Text)textNode).Data.Trim() == "") {
							// Prune whitespace before a tag.
							goog.dom.removeNode(textNode);
						}
					}
				}
			}
			return shadow;
		}

		/// <summary>
		/// Converts a DOM structure into plain text.
		/// Currently the text format is fairly ugly: all one line with no whitespace.
		/// </summary>
		/// <param name="dom">A tree of XML elements.</param>
		/// <returns>Text representation.</returns>
		public static string domToText(Element dom)
		{
			var oSerializer = new XMLSerializer();
			return oSerializer.SerializeToString(dom);
		}

		/// <summary>
		/// Converts a DOM structure into properly indented text.
		/// </summary>
		/// <param name="dom">A tree of XML elements.</param>
		/// <returns>Text representation.</returns>
		public static string domToPrettyText(Element dom)
		{
			// This function is not guaranteed to be correct for all XML.
			// But it handles the XML that Blockly generates.
			var blob = Xml.domToText(dom);
			// Place every open and close tag on its own line.
			var lines = blob.Split("<");
			// Indent every line.
			var indent = "";
			for (var i = 1; i < lines.Length; i++) {
				var line = lines[i];
				if (line[0] == '/') {
					indent = indent.Substring(2);
				}
				lines[i] = indent + "<" + line;
				if (line[0] != '/' && line.Slice(-2) != "/>") {
					indent += "  ";
				}
			}
			// Pull simple tags back together.
			// E.g. <foo></foo>
			var text = lines.Join("\n");
			text = text.Replace(new Regex(@"(<(\w+)\b[^>]*>[^\n]*)\n *<\/\2>", RegexOptions.Multiline), "$1</$2>");
			// Trim leading blank line.
			return text.Replace(new Regex(@"^\n"), "");
		}

		/// <summary>
		/// Converts plain text into a DOM structure.
		/// Throws an error if XML doesn't parse.
		/// </summary>
		/// <param name="text">Text representation.</param>
		/// <returns>A tree of XML elements.</returns>
		public static Element textToDom(string text)
		{
			var oParser = new DOMParser();
			var dom = oParser.ParseFromString(text, "text/xml");
			// The DOM should have one and only one top-level node, an XML tag.
			if (dom == null || dom.FirstChild == null ||
				dom.FirstChild.NodeName.ToLowerCase() != "xml" ||
				dom.FirstChild != dom.LastChild) {
				// Whatever we got back from the parser is not XML.
				goog.asserts.fail("Xml.textToDom did not obtain a valid XML tree.");
			}
			return (Element)dom.FirstChild;
		}

		/// <summary>
		/// Decode an XML DOM and create blocks on the workspace.
		/// </summary>
		/// <param name="xml">XML DOM.</param>
		/// <param name="workspace">The workspace.</param>
		public static void domToWorkspace(Element xml, Workspace workspace)
		{
			var width = 0.0;  // Not used in LTR.
			if (workspace.RTL) {
				width = workspace.getWidth();
			}
			Field.startCache();
			// Safari 7.1.3 is known to provide node lists with extra references to
			// children beyond the lists" length.  Trust the length, do not use the
			// looping pattern of checking the index for an object.
			var childCount = xml.ChildNodes.Length;
			var existingGroup = Events.getGroup();
			if (existingGroup == null) {
				Events.setGroup(true);
			}
			((WorkspaceSvg)workspace).setResizesEnabled(false);
			for (var i = 0; i < childCount; i++) {
				var xmlChild_ = xml.ChildNodes[i];
				var name = xmlChild_.NodeName.ToLowerCase();
				if (name == "block" ||
					(name == "shadow" && !Events.recordUndo)) {
					var xmlChild = (Element)xmlChild_;
					// Allow top-level shadow blocks if recordUndo is disabled since
					// that means an undo is in progress.  Such a block is expected
					// to be moved to a nested destination in the next operation.
					var block = Xml.domToBlock(xmlChild, workspace);
					var blockX = Double.TryParse(xmlChild.GetAttribute("x"), out var x) ? x : Double.NaN;
					var blockY = Double.TryParse(xmlChild.GetAttribute("y"), out var y) ? y : Double.NaN;
					if (!Double.IsNaN(blockX) && !Double.IsNaN(blockY)) {
						block.moveBy(workspace.RTL ? width - blockX : blockX, blockY);
					}
				}
				else if (name == "shadow") {
					goog.asserts.fail("Shadow block cannot be a top-level block.");
				}
			}
			if (existingGroup == null) {
				Events.setGroup(false);
			}
			Field.stopCache();

			workspace.updateVariableList(false);
			((WorkspaceSvg)workspace).setResizesEnabled(false);
		}

		/// <summary>
		/// Decode an XML block tag and create a block (and possibly sub blocks) on the
		/// workspace.
		/// </summary>
		/// <param name="xmlBlock">XML block element.</param>
		/// <param name="workspace">The workspace.</param>
		/// <returns>The root block created.</returns>
		public static Block domToBlock(Element xmlBlock, Workspace workspace)
		{
			BlockSvg topBlock;
			// Create top-level block.
			Events.disable();
			try {
				topBlock = Xml.domToBlockHeadless_(xmlBlock, workspace);
				if (workspace.rendered) {
					// Hide connections to speed up assembly.
					topBlock.setConnectionsHidden(true);
					// Generate list of all blocks.
					var blocks = topBlock.getDescendants();
					// Render each block.
					for (var i = blocks.Length - 1; i >= 0; i--) {
						((BlockSvg)blocks[i]).initSvg();
					}
					for (var i = blocks.Length - 1; i >= 0; i--) {
						((BlockSvg)blocks[i]).render(false);
					}
					// Populating the connection database may be defered until after the
					// blocks have rendered.
					Window.SetTimeout(() => {
						if (topBlock.workspace != null) {  // Check that the block hasn't been deleted.
							topBlock.setConnectionsHidden(false);
						}
					}, 1);
					topBlock.updateDisabled();
					// Allow the scrollbars to resize and move based on the new contents.
					// TODO(@picklesrus): #387. Remove when domToBlock avoids resizing.
					((WorkspaceSvg)workspace).resizeContents();
				}
			}
			finally {
				Events.enable();
			}
			if (Events.isEnabled()) {
				Events.fire(new Events.Create(topBlock));
			}
			return topBlock;
		}

		/// <summary>
		/// Decode an XML block tag and create a block (and possibly sub blocks) on the
		/// workspace.
		/// </summary>
		/// <param name="xmlBlock">XML block element.</param>
		/// <param name="workspace">The workspace.</param>
		/// <returns>The root block created.</returns>
		private static BlockSvg domToBlockHeadless_(Element xmlBlock, Workspace workspace)
		{
			BlockSvg block = null;
			var prototypeName = xmlBlock.GetAttribute("type");
			goog.asserts.assert(prototypeName != null, "Block type unspecified: %s",
								xmlBlock.OuterHTML);
			var id = xmlBlock.GetAttribute("id");
			block = (BlockSvg)workspace.newBlock(prototypeName, id);

			BlockSvg blockChild = null;
			foreach (var xmlChild_ in xmlBlock.ChildNodes) {
				if (xmlChild_.NodeType == NodeType.Text) {
					// Ignore any text at the <block> level.  It's all whitespace anyway.
					continue;
				}
				Element xmlChild = (Element)xmlChild_;
				Input input;

				// Find any enclosed blocks or shadows in this tag.
				Element childBlockNode = null;
				Element childShadowNode = null;
				foreach (var grandchildNode in xmlChild.ChildNodes) {
					if (grandchildNode.NodeType == NodeType.Element) {
						if (grandchildNode.NodeName.ToLowerCase() == "block") {
							childBlockNode = (Element)grandchildNode;
						}
						else if (grandchildNode.NodeName.ToLowerCase() == "shadow") {
							childShadowNode = (Element)grandchildNode;
						}
					}
				}
				// Use the shadow block if there is no child block.
				if (childBlockNode == null && childShadowNode != null) {
					childBlockNode = childShadowNode;
				}

				var name = xmlChild.GetAttribute("name");
				switch (xmlChild.NodeName.ToLowerCase()) {
				case "mutation":
					// Custom data for an advanced block.
					if (true/*block.domToMutation*/) {
						block.domToMutation(xmlChild);
						if (true/*block.initSvg*/) {
							// Mutation may have added some elements that need initalizing.
							block.initSvg();
						}
					}
					break;
				case "comment":
					block.setCommentText(xmlChild.TextContent);
					var visible = xmlChild.GetAttribute("pinned");
					if (visible != null && !block.isInFlyout) {
						// Give the renderer a millisecond to render and position the block
						// before positioning the comment bubble.
						Window.SetTimeout(() => {
							if (block.comment != null /*&& block.comment.setVisible*/) {
								block.comment.setVisible(visible == "true");
							}
						}, 1);
					}
					var bubbleW = Script.ParseFloat(xmlChild.GetAttribute("w") ?? "0.0");
					var bubbleH = Script.ParseFloat(xmlChild.GetAttribute("h") ?? "0.0");
					if (!Double.IsNaN(bubbleW) && !Double.IsNaN(bubbleH) &&
						block.comment != null /*&& block.comment.setVisible*/) {
						block.comment.setBubbleSize(bubbleW, bubbleH);
					}
					break;
				case "data":
					block.data = xmlChild.TextContent;
					break;
				case "title":
				// Titles were renamed to field in December 2013.
				// Fall through.
				case "field":
					var field = block.getField(name);
					if (field == null) {
						Console.WriteLine("Ignoring non-existent field " + name + " in block " +
							prototypeName);
						break;
					}
					field.setValue(xmlChild.TextContent);
					break;
				case "value":
				case "statement":
					input = block.getInput(name);
					if (input == null) {
						Console.WriteLine("Ignoring non-existent input " + name + " in block " +
							prototypeName);
						break;
					}
					if (childShadowNode != null) {
						input.connection.setShadowDom(childShadowNode);
					}
					if (childBlockNode != null) {
						blockChild = Xml.domToBlockHeadless_(childBlockNode,
							workspace);
						if (blockChild.outputConnection != null) {
							input.connection.connect(blockChild.outputConnection);
						}
						else if (blockChild.previousConnection != null) {
							input.connection.connect(blockChild.previousConnection);
						}
						else {
							goog.asserts.fail(
								"Child block does not have output or previous statement.");
						}
					}
					break;
				case "next":
					if (childShadowNode != null && block.nextConnection != null) {
						block.nextConnection.setShadowDom(childShadowNode);
					}
					if (childBlockNode != null) {
						goog.asserts.assert(block.nextConnection != null,
							"Next statement does not exist.");
						// If there is more than one XML "next" tag.
						goog.asserts.assert(!block.nextConnection.isConnected(),
							"Next statement is already connected.");
						blockChild = Xml.domToBlockHeadless_(childBlockNode,
							workspace);
						goog.asserts.assert(blockChild.previousConnection != null,
							"Next block does not have previous statement.");
						block.nextConnection.connect(blockChild.previousConnection);
					}
					break;
				default:
					// Unknown tag; ignore.  Same principle as HTML parsers.
					Console.WriteLine("Ignoring unknown tag: " + xmlChild.NodeName);
					break;
				}
			}

			var inline = xmlBlock.GetAttribute("inline");
			if (inline != null) {
				block.setInputsInline(inline == "true");
			}
			var disabled = xmlBlock.GetAttribute("disabled");
			if (disabled != null) {
				block.setDisabled(disabled == "true");
			}
			var deletable = xmlBlock.GetAttribute("deletable");
			if (deletable != null) {
				block.setDeletable(deletable == "true");
			}
			var movable = xmlBlock.GetAttribute("movable");
			if (movable != null) {
				block.setMovable(movable == "true");
			}
			var editable = xmlBlock.GetAttribute("editable");
			if (editable != null) {
				block.setEditable(editable == "true");
			}
			var collapsed = xmlBlock.GetAttribute("collapsed");
			if (collapsed != null) {
				block.setCollapsed(collapsed == "true");
			}
			if (xmlBlock.NodeName.ToLowerCase() == "shadow") {
				// Ensure all children are also shadows.
				var children = block.getChildren();
				foreach (var child in children) {
					goog.asserts.assert(child.isShadow(),
										"Shadow block not allowed non-shadow child.");
				}
				block.setShadow(true);
			}
			return block;
		}

		/// <summary>
		/// Remove any 'next' block (statements in a stack).
		/// </summary>
		/// <param name="xmlBlock">XML block element.</param>
		public static void deleteNext(Element xmlBlock)
		{
			foreach (var child in xmlBlock.ChildNodes) {
				if (child.NodeName.ToLowerCase() == "next") {
					xmlBlock.RemoveChild(child);
					break;
				}
			}
		}
	}
}
