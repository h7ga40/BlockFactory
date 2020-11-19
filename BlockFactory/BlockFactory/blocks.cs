/*
 * Blockly Demos: Block Factory Blocks
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

/*
 * @fileoverview Blocks for Blockly's Block Factory application.
 * @author fraser@google.com (Neil Fraser)
 */
using System;
using System.Collections.Generic;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	// Base of new block.
	public class FactoryBase : Blockly.BlockSvg
	{
		public const string type_name = "factory_base";

		public FactoryBase(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(120);
			this.appendDummyInput()
				.appendField("name")
				.appendField(new Blockly.FieldTextInput("block_type"), "NAME");
			this.appendStatementInput("INPUTS")
				.setCheck("Input")
				.appendField("inputs");
			var dropdown = new Blockly.FieldDropdown(new JsArray<Blockly.DropdownItemInfo>(){
				new Blockly.DropdownItemInfo("automatic inputs", "AUTO"),
				new Blockly.DropdownItemInfo("external inputs", "EXT"),
				new Blockly.DropdownItemInfo("inline inputs", "INT")});
			this.appendDummyInput()
				.appendField(dropdown, "INLINE");
			dropdown = new Blockly.FieldDropdown(new JsArray<Blockly.DropdownItemInfo>(){
				new Blockly.DropdownItemInfo("no connections", "NONE"),
				new Blockly.DropdownItemInfo("← left output", "LEFT"),
				new Blockly.DropdownItemInfo("↕ top+bottom connections", "BOTH"),
				new Blockly.DropdownItemInfo("↑ top connection", "TOP"),
				new Blockly.DropdownItemInfo("↓ bottom connection", "BOTTOM")},
				(field, option) => {
					((FactoryBase)field.sourceBlock_).updateShape_(option);
					// Connect a shadow block to this new input. 
					((FactoryBase)field.sourceBlock_).spawnOutputShadow_(option);
					return Script.Undefined;
				});
			this.appendDummyInput()
				.appendField(dropdown, "CONNECTIONS");
			this.appendValueInput("COLOUR")
				.setCheck("Colour")
				.appendField("colour");
			this.setTooltip("Build a custom block by plugging\n" +
				"fields, inputs and other blocks here.");
			this.setHelpUrl(
				"https://developers.google.com/blockly/guides/create-custom-blocks/block-factory");
		}

		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("connections", this.getFieldValue("CONNECTIONS"));
			return container;
		}

		public override void domToMutation(Element xmlElement)
		{
			var connections = xmlElement.GetAttribute("connections");
			this.updateShape_(connections);
		}

		private void spawnOutputShadow_(string option)
		{
			// Helper method for deciding which type of outputs this block needs
			// to attach shaddow blocks to.
			switch (option) {
			case "LEFT":
				this.connectOutputShadow_("OUTPUTTYPE");
				break;
			case "TOP":
				this.connectOutputShadow_("TOPTYPE");
				break;
			case "BOTTOM":
				this.connectOutputShadow_("BOTTOMTYPE");
				break;
			case "BOTH":
				this.connectOutputShadow_("TOPTYPE");
				this.connectOutputShadow_("BOTTOMTYPE");
				break;
			}
		}

		private void connectOutputShadow_(string outputType)
		{
			// Helper method to create & connect shadow block.
			var type = (BlockSvg)this.workspace.newBlock("type_null");
			type.setShadow(true);
			type.outputConnection.connect(this.getInput(outputType).connection);
			type.initSvg();
			type.render();
		}

		private void updateShape_(string option)
		{
			var outputExists = this.getInput("OUTPUTTYPE");
			var topExists = this.getInput("TOPTYPE");
			var bottomExists = this.getInput("BOTTOMTYPE");
			if (option == "LEFT") {
				if (outputExists == null) {
					this.addTypeInput_("OUTPUTTYPE", "output type");
				}
			}
			else if (outputExists != null) {
				this.removeInput("OUTPUTTYPE");
			}
			if (option == "TOP" || option == "BOTH") {
				if (topExists == null) {
					this.addTypeInput_("TOPTYPE", "top type");
				}
			}
			else if (topExists != null) {
				this.removeInput("TOPTYPE");
			}
			if (option == "BOTTOM" || option == "BOTH") {
				if (bottomExists == null) {
					this.addTypeInput_("BOTTOMTYPE", "bottom type");
				}
			}
			else if (bottomExists != null) {
				this.removeInput("BOTTOMTYPE");
			}
		}

		private void addTypeInput_(string name, string label)
		{
			this.appendValueInput(name)
				.setCheck("Type")
				.appendField(label);
			this.moveInputBefore(name, "COLOUR");
		}
	}

	/// <summary>
	/// Value input.
	/// </summary>
	public class InputValue : Blockly.BlockSvg
	{
		public const string type_name = "input_value";

		public InputValue(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "value input %1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_input" },
						{ "name", "INPUTNAME" },
						{ "text", "NAME" }
					},
					new Dictionary<string, object> {
						{ "type", "input_dummy" }
					}
				} },
				{ "message1", BlockFactory.FIELD_MESSAGE },
				{ "args1", BlockFactory.FIELD_ARGS },
				{ "message2", BlockFactory.TYPE_MESSAGE },
				{ "args2", BlockFactory.TYPE_ARGS },
				{ "previousStatement", "Input" },
				{ "nextStatement", "Input" },
				{ "colour", 210 },
				{ "tooltip", "A value socket for horizontal connections." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=71" }
			});
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.inputNameCheck(this);
		}
	}

	/// <summary>
	/// Statement input.
	/// </summary>
	public class InputStatement : Blockly.BlockSvg
	{
		public const string type_name = "input_statement";

		public InputStatement(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "statement input %1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{ "type", "field_input" },
						{ "name", "INPUTNAME" },
						{ "text", "NAME" }
					},
					new Dictionary<string, object> {
						{ "type", "input_dummy" }
					}
				} },
				{ "message1", BlockFactory.FIELD_MESSAGE },
				{ "args1", BlockFactory.FIELD_ARGS },
				{ "message2", BlockFactory.TYPE_MESSAGE },
				{ "args2", BlockFactory.TYPE_ARGS },
				{ "previousStatement", "Input" },
				{ "nextStatement", "Input" },
				{ "colour", 210 },
				{ "tooltip", "A statement socket for enclosed vertical stacks." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=246" }
			});
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.inputNameCheck(this);
		}
	}

	/// <summary>
	/// Dummy input.
	/// </summary>
	public class InputDummy : Blockly.BlockSvg
	{
		public const string type_name = "input_dummy";

		public InputDummy(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "dummy input" },
				{ "message1", BlockFactory.FIELD_MESSAGE },
				{ "args1", BlockFactory.FIELD_ARGS },
				{ "previousStatement", "Input" },
				{ "nextStatement", "Input" },
				{ "colour", 210 },
				{ "tooltip", "For adding fields on a separate row with no " +
							"connections. Alignment options (left, right, centre) " +
							"apply only to multi-line fields." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=293" }
			});
		}
	}

	/// <summary>
	/// Text value.
	/// </summary>
	public class FieldStatic : Blockly.BlockSvg
	{
		public const string type_name = "field_static";

		public FieldStatic(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("text")
				.appendField(new Blockly.FieldTextInput(""), "TEXT");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Static text that serves as a label.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=88");
		}
	}

	/// <summary>
	/// Text input.
	/// </summary>
	public class FieldInput : Blockly.BlockSvg
	{
		public const string type_name = "field_input";

		public FieldInput(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("text input")
				.appendField(new Blockly.FieldTextInput("default"), "TEXT")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("An input field for the user to enter text.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=319");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Numeric input.
	/// </summary>
	public class FieldNumber : Blockly.BlockSvg
	{
		public const string type_name = "field_number";

		public FieldNumber(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("numeric input")
				.appendField(new Blockly.FieldNumber("0"), "VALUE")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.appendDummyInput()
				.appendField("min")
				.appendField(new Blockly.FieldNumber("-Infinity"), "MIN")
				.appendField("max")
				.appendField(new Blockly.FieldNumber("Infinity"), "MAX")
				.appendField("precision")
				.appendField(new Blockly.FieldNumber("0", 0), "PRECISION");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("An input field for the user to enter a number.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=319");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Angle input.
	/// </summary>
	public class FieldAngle : Blockly.BlockSvg
	{
		public const string type_name = "field_angle";

		public FieldAngle(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("angle input")
				.appendField(new Blockly.FieldAngle("90"), "ANGLE")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("An input field for the user to enter an angle.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=372");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Dropdown menu.
	/// </summary>
	public class FieldDropdown : Blockly.BlockSvg
	{
		public const string type_name = "field_dropdown";
		internal int optionCount_;

		public FieldDropdown(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.appendDummyInput()
				.appendField("dropdown")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.optionCount_ = 3;
			this.updateShape_();
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setMutator(new Blockly.Mutator(new string[] { "field_dropdown_option" }));
			this.setColour(160);
			this.setTooltip("Dropdown menu with a list of options.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=386");
		}

		public override Element mutationToDom()
		{
			// Create XML to represent menu options.
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("options", this.optionCount_);
			return container;
		}

		public override void domToMutation(Element xmlElement)
		{
			// Parse XML to restore the menu options.
			this.optionCount_ = Int32.Parse(xmlElement.GetAttribute("options"));
			this.updateShape_();
		}

		public override Block decompose(Workspace workspace)
		{
			// Populate the mutator's dialog with this block's components.
			var containerBlock = (BlockSvg)workspace.newBlock("field_dropdown_container");
			containerBlock.initSvg();
			var connection = containerBlock.getInput("STACK").connection;
			for (var i = 0; i < this.optionCount_; i++) {
				var optionBlock = (BlockSvg)workspace.newBlock("field_dropdown_option");
				optionBlock.initSvg();
				connection.connect(optionBlock.previousConnection);
				connection = optionBlock.nextConnection;
			}
			return containerBlock;
		}

		public override void compose(Block containerBlock)
		{
			// Reconfigure this block based on the mutator dialog's components.
			var optionBlock = containerBlock.getInputTargetBlock("STACK");
			// Count number of inputs.
			var data = new JsArray<string[]>();
			while (optionBlock != null) {
				data.Push(new string[] { (string)optionBlock["userData_"], (string)optionBlock["cpuData_"] });
				optionBlock = optionBlock.nextConnection == null ? null :
					optionBlock.nextConnection.targetBlock();
			}
			this.optionCount_ = data.Length;
			this.updateShape_();
			// Restore any data.
			for (var i = 0; i < this.optionCount_; i++) {
				this.setFieldValue(data[i][0] ?? "option", "USER" + i);
				this.setFieldValue(data[i][1] ?? "OPTIONNAME", "CPU" + i);
			}
		}

		public override void saveConnections(Block containerBlock)
		{
			// Store names and values for each option.
			var optionBlock = containerBlock.getInputTargetBlock("STACK");
			var i = 0;
			while (optionBlock != null) {
				optionBlock["userData_"] = this.getFieldValue("USER" + i);
				optionBlock["cpuData_"] = this.getFieldValue("CPU" + i);
				i++;
				optionBlock = optionBlock.nextConnection == null ? null :
					optionBlock.nextConnection.targetBlock();
			}
		}

		private void updateShape_()
		{
			// Modify this block to have the correct number of options.
			// Add new options.
			var i = 0;
			for (; i < this.optionCount_; i++) {
				if (this.getInput("OPTION" + i) == null) {
					this.appendDummyInput("OPTION" + i)
						.appendField(new Blockly.FieldTextInput("option"), "USER" + i)
						.appendField(",")
						.appendField(new Blockly.FieldTextInput("OPTIONNAME"), "CPU" + i);
				}
			}
			// Remove deleted options.
			while (this.getInput("OPTION" + i) != null) {
				this.removeInput("OPTION" + i);
				i++;
			}
		}

		protected override void onchange(Events.Abstract ev)
		{
			if (this.workspace != null && this.optionCount_ < 1) {
				this.setWarningText("Drop down menu must\nhave at least one option.");
			}
			else {
				BlockFactory.fieldNameCheck(this);
			}
		}
	}

	/// <summary>
	/// Container.
	/// </summary>
	public class FieldDropdownContainer : Blockly.BlockSvg
	{
		public const string type_name = "field_dropdown_container";

		public FieldDropdownContainer(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("add options");
			this.appendStatementInput("STACK");
			this.setTooltip("Add, remove, or reorder options\n" +
							"to reconfigure this dropdown menu.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=386");
			this.contextMenu = false;
		}
	}

	/// <summary>
	/// Add option.
	/// </summary>
	public class FieldDropdownOption : Blockly.BlockSvg
	{
		public const string type_name = "field_dropdown_option";

		public FieldDropdownOption(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("option");
			this.setPreviousStatement(true);
			this.setNextStatement(true);
			this.setTooltip("Add a new option to the dropdown menu.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=386");
			this.contextMenu = false;
		}
	}

	/// <summary>
	/// Checkbox.
	/// </summary>
	public class FieldCheckbox : Blockly.BlockSvg
	{
		public const string type_name = "field_checkbox";

		public FieldCheckbox(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("checkbox")
				.appendField(new Blockly.FieldCheckbox("TRUE"), "CHECKED")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Checkbox field.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=485");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Colour input.
	/// </summary>
	public class FieldColour : Blockly.BlockSvg
	{
		public const string type_name = "field_colour";

		public FieldColour(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("colour")
				.appendField(new Blockly.FieldColour("#ff0000"), "COLOUR")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Colour input field.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=495");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Date input.
	/// </summary>
	public class FieldDate : Blockly.BlockSvg
	{
		public const string type_name = "field_date";

		public FieldDate(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("date")
				.appendField(new Blockly.FieldDate(""), "DATE")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Date input field.");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Dropdown for variables.
	/// </summary>
	public class FieldVariable : Blockly.BlockSvg
	{
		public const string type_name = "field_variable";

		public FieldVariable(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			this.appendDummyInput()
				.appendField("variable")
				.appendField(new Blockly.FieldTextInput("item"), "TEXT")
				.appendField(",")
				.appendField(new Blockly.FieldTextInput("NAME"), "FIELDNAME");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Dropdown menu for variable names.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=510");
		}

		protected override void onchange(Events.Abstract ev)
		{
			BlockFactory.fieldNameCheck(this);
		}
	}

	/// <summary>
	/// Image.
	/// </summary>
	public class FieldImage : Blockly.BlockSvg
	{
		public const string type_name = "field_image";

		public FieldImage(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.setColour(160);
			var src = "https://www.gstatic.com/codesite/ph/images/star_on.gif";
			this.appendDummyInput()
				.appendField("image")
				.appendField(new Blockly.FieldTextInput(src), "SRC");
			this.appendDummyInput()
				.appendField("width")
				.appendField(new Blockly.FieldNumber("15", 0, Double.NaN, 1), "WIDTH")
				.appendField("height")
				.appendField(new Blockly.FieldNumber("15", 0, Double.NaN, 1), "HEIGHT")
				.appendField("alt text")
				.appendField(new Blockly.FieldTextInput("*"), "ALT");
			this.setPreviousStatement(true, "Field");
			this.setNextStatement(true, "Field");
			this.setTooltip("Static image (JPEG, PNG, GIF, SVG, BMP).\n" +
							"Retains aspect ratio regardless of height and width.\n" +
							"Alt text is for when collapsed.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=567");
		}
	}

	/// <summary>
	/// Group of types.
	/// </summary>
	public class TypeGroup : Blockly.BlockSvg
	{
		public const string type_name = "type_group";
		internal int typeCount_;

		public TypeGroup(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.typeCount_ = 2;
			this.updateShape_();
			this.setOutput(true, "Type");
			this.setMutator(new Blockly.Mutator(new string[] { "type_group_item" }));
			this.setColour(230);
			this.setTooltip("Allows more than one type to be accepted.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=677");
		}

		public override Element mutationToDom()
		{
			// Create XML to represent a group of types.
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("types", this.typeCount_);
			return container;
		}

		public override void domToMutation(Element xmlElement)
		{
			// Parse XML to restore the group of types.
			this.typeCount_ = Int32.Parse(xmlElement.GetAttribute("types"));
			this.updateShape_();
			for (var i = 0; i < this.typeCount_; i++) {
				this.removeInput("TYPE" + i);
			}
			for (var i = 0; i < this.typeCount_; i++) {
				var input = this.appendValueInput("TYPE" + i)
								.setCheck("Type");
				if (i == 0) {
					input.appendField("any of");
				}
			}
		}

		public override Block decompose(Workspace workspace)
		{
			// Populate the mutator's dialog with this block's components.
			var containerBlock = (BlockSvg)workspace.newBlock("type_group_container");
			containerBlock.initSvg();
			var connection = containerBlock.getInput("STACK").connection;
			for (var i = 0; i < this.typeCount_; i++) {
				var typeBlock = (BlockSvg)workspace.newBlock("type_group_item");
				typeBlock.initSvg();
				connection.connect(typeBlock.previousConnection);
				connection = typeBlock.nextConnection;
			}
			return containerBlock;
		}

		public override void compose(Block containerBlock)
		{
			// Reconfigure this block based on the mutator dialog's components.
			var typeBlock = containerBlock.getInputTargetBlock("STACK");
			// Count number of inputs.
			var connections = new JsArray<Connection>();
			while (typeBlock != null) {
				connections.Push((Connection)typeBlock["valueConnection_"]);
				typeBlock = typeBlock.nextConnection == null ? null :
					typeBlock.nextConnection.targetBlock();
			}
			// Disconnect any children that don't belong.
			for (var i = 0; i < this.typeCount_; i++) {
				var connection = this.getInput("TYPE" + i).connection.targetConnection;
				if (connection != null && connections.IndexOf(connection) == -1) {
					connection.disconnect();
				}
			}
			this.typeCount_ = connections.Length;
			this.updateShape_();
			// Reconnect any child blocks.
			for (var i = 0; i < this.typeCount_; i++) {
				Blockly.Mutator.reconnect(connections[i], this, "TYPE" + i);
			}
		}

		public override void saveConnections(Block containerBlock)
		{
			// Store a pointer to any connected child blocks.
			var typeBlock = containerBlock.getInputTargetBlock("STACK");
			var i = 0;
			while (typeBlock != null) {
				var input = this.getInput("TYPE" + i);
				typeBlock["valueConnection_"] = input == null ? null : input.connection.targetConnection;
				i++;
				typeBlock = typeBlock.nextConnection == null ? null :
					typeBlock.nextConnection.targetBlock();
			}
		}

		private void updateShape_()
		{
			// Modify this block to have the correct number of inputs.
			// Add new inputs.
			var i = 0;
			for (; i < this.typeCount_; i++) {
				if (this.getInput("TYPE" + i) == null) {
					var input = this.appendValueInput("TYPE" + i);
					if (i == 0) {
						input.appendField("any of");
					}
				}
			}
			// Remove deleted inputs.
			while (this.getInput("TYPE" + i) != null) {
				this.removeInput("TYPE" + i);
				i++;
			}
		}
	}

	/// <summary>
	/// Container.
	/// </summary>
	public class TypeGroupContainer : Blockly.BlockSvg
	{
		public const string type_name = "type_group_container";

		public TypeGroupContainer(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "add types %1 %2" },
				{ "args0", new object[] {
					new Dictionary<string, object> { { "type", "input_dummy"} },
					new Dictionary<string, object> { { "type", "input_statement" }, {"name", "STACK" } }
				} },
				{ "colour", 230 },
				{ "tooltip", "Add, or remove allowed type." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=677" }
			});
		}
	}

	/// <summary>
	/// Add type.
	/// </summary>
	public class TypeGroupItem : Blockly.BlockSvg
	{
		public const string type_name = "type_group_item";

		public TypeGroupItem(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "type" },
				{ "previousStatement", null },
				{ "nextStatement", null },
				{ "colour", 230 },
				{ "tooltip", "Add a new allowed type." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=677"}
			});
		}
	}

	/// <summary>
	/// Null type.
	/// </summary>
	public class TypeNull : Blockly.BlockSvg
	{
		public const string type_name = "type_null";
		public const string valueType = null;

		public TypeNull(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "any" },
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Any type is allowed." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=602" }
			});
		}
	}

	/// <summary>
	/// Boolean type.
	/// </summary>
	public class TypeBoolean : Blockly.BlockSvg
	{
		public const string type_name = "type_boolean";
		public const string valueType = "Boolean";

		public TypeBoolean(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "Boolean" },
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Booleans (true/false) are allowed." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=602" }
			});
		}
	}

	public class TypeNumber : Blockly.BlockSvg
	{
		public const string type_name = "type_number";
		public const string valueType = "Number";

		public TypeNumber(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "Number" },
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Numbers (int/float) are allowed." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=602"}
			});
		}
	}

	/// <summary>
	/// String type.
	/// </summary>
	public class TypeString : Blockly.BlockSvg
	{
		public const string type_name = "type_string";
		public const string valueType = "String";

		public TypeString(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "String" },
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Strings (text) are allowed." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=602" }
			});
		}
	}

	/// <summary>
	/// List type.
	/// </summary>
	public class TypeList : Blockly.BlockSvg
	{
		public const string type_name = "type_list";
		public const string valueType = "Array";

		public TypeList(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "Array" },
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Arrays (lists) are allowed." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=602" }
			});
		}
	}

	public class TypeOther : Blockly.BlockSvg
	{
		public const string type_name = "type_other";

		public TypeOther(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.jsonInit(new Dictionary<string, object> {
				{ "message0", "other %1" },
				{ "args0", new object[] {
					new Dictionary<string, object> {
						{"type", "field_input" },
						{ "name", "TYPE" },
						{ "text", ""} }
					}
				},
				{ "output", "Type" },
				{ "colour", 230 },
				{ "tooltip", "Custom type to allow." },
				{ "helpUrl", "https://www.youtube.com/watch?v=s2_xaEvcVI0#t=702" }
			});
		}
	}

	/// <summary>
	/// Set the colour of the block.
	/// </summary>
	public class ColourHue : Blockly.BlockSvg
	{
		public const string type_name = "colour_hue";

		public ColourHue(Blockly.Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			this.appendDummyInput()
				.appendField("hue:")
				.appendField(new Blockly.FieldAngle("0", this.validator), "HUE");
			this.setOutput(true, "Colour");
			this.setTooltip("Paint the block with this colour.");
			this.setHelpUrl("https://www.youtube.com/watch?v=s2_xaEvcVI0#t=55");
		}

		public object validator(Field field, string text)
		{
			// Update the current block's colour to match.
			var hue = Script.ParseFloat(text);
			if (!Double.IsNaN(hue)) {
				field.sourceBlock_.setColour((int)hue);
			}
			return hue;
		}

		public override Element mutationToDom()
		{
			var container = Document.CreateElement<Element>("mutation");
			container.SetAttribute("colour", this.getColour());
			return container;
		}

		public override void domToMutation(Element xmlElement)
		{
			this.setColour(xmlElement.GetAttribute("colour"));
		}
	}

	public static partial class BlockFactory
	{
		public static readonly string FIELD_MESSAGE = "fields %1 %2";
		public static readonly object[] FIELD_ARGS =
			new object[] {
				new Dictionary<string, object> {
					{ "type", "field_dropdown" },
					{ "name", "ALIGN" },
					{ "options", new JsArray<DropdownItemInfo> {
						new DropdownItemInfo("left", "LEFT"),
						new DropdownItemInfo("right", "RIGHT"),
						new DropdownItemInfo("centre", "CENTRE") }
					}
				},
				new Dictionary<string, object> {
					{ "type", "input_statement" },
					{ "name", "FIELDS" },
					{ "check", "Field" }
				}
			};
		public static readonly string TYPE_MESSAGE = "type %1";
		public static readonly object[] TYPE_ARGS =
			new object[] {
				new Dictionary<string, object> {
					{ "type", "input_value" },
					{ "name", "TYPE" },
					{ "check", "Type" },
					{ "align", "RIGHT" }
				}
			};

		public static AppController blocklyFactory;

		static BlockFactory()
		{
			Core.Blocks = new Blocks();
			Core.Blocks.Add("factory_base", typeof(FactoryBase));
			Core.Blocks.Add("input_value", typeof(InputValue));
			Core.Blocks.Add("input_statement", typeof(InputStatement));
			Core.Blocks.Add("input_dummy", typeof(InputDummy));
			Core.Blocks.Add("field_static", typeof(FieldStatic));
			Core.Blocks.Add("field_input", typeof(FieldInput));
			Core.Blocks.Add("field_number", typeof(FieldNumber));
			Core.Blocks.Add("field_angle", typeof(FieldAngle));
			Core.Blocks.Add("field_dropdown", typeof(FieldDropdown));
			Core.Blocks.Add("field_dropdown_container", typeof(FieldDropdownContainer));
			Core.Blocks.Add("field_dropdown_option", typeof(FieldDropdownOption));
			Core.Blocks.Add("field_checkbox", typeof(FieldCheckbox));
			Core.Blocks.Add("field_colour", typeof(FieldColour));
			Core.Blocks.Add("field_date", typeof(FieldDate));
			Core.Blocks.Add("field_variable", typeof(FieldVariable));
			Core.Blocks.Add("field_image", typeof(FieldImage));
			Core.Blocks.Add("type_group", typeof(TypeGroup));
			Core.Blocks.Add("type_group_container", typeof(TypeGroupContainer));
			Core.Blocks.Add("type_group_item", typeof(TypeGroupItem));
			Core.Blocks.Add("type_null", typeof(TypeNull));
			Core.Blocks.Add("type_boolean", typeof(TypeBoolean));
			Core.Blocks.Add("type_number", typeof(TypeNumber));
			Core.Blocks.Add("type_string", typeof(TypeString));
			Core.Blocks.Add("type_list", typeof(TypeList));
			Core.Blocks.Add("type_other", typeof(TypeOther));
			Core.Blocks.Add("colour_hue", typeof(ColourHue));
			Core.Procedures = new Procedures();
			Core.Variables = new Variables();
			Core.Names = new Names("");
		}

		/// <summary>
		/// Check to see if more than one field has this name.
		/// Highly inefficient (On^2), but n is small.
		/// </summary>
		/// <param name="referenceBlock">Block to check.</param>
		public static void fieldNameCheck(Blockly.Block referenceBlock)
		{
			if (referenceBlock.workspace == null) {
				// Block has been deleted.
				return;
			}
			var name = referenceBlock.getFieldValue("FIELDNAME").ToLowerCase();
			var count = 0;
			var blocks = referenceBlock.workspace.getAllBlocks();
			for (var i = 0; i < blocks.Length; i++) {
				var block = blocks[i];
				var otherName = block.getFieldValue("FIELDNAME");
				if (!block.disabled && !block.getInheritedDisabled() &&
					otherName != null && otherName.ToLowerCase() == name) {
					count++;
				}
			}
			var msg = (count > 1) ?
				"There are " + count + " field blocks\n with this name." : null;
			referenceBlock.setWarningText(msg);
		}

		/// <summary>
		/// Check to see if more than one input has this name.
		/// Highly inefficient (On^2), but n is small.
		/// </summary>
		/// <param name="referenceBlock">Block to check.</param>
		public static void inputNameCheck(Blockly.Block referenceBlock)
		{
			if (referenceBlock.workspace == null) {
				// Block has been deleted.
				return;
			}
			var name = referenceBlock.getFieldValue("INPUTNAME").ToLowerCase();
			var count = 0;
			var blocks = referenceBlock.workspace.getAllBlocks();
			for (var i = 0; i < blocks.Length; i++) {
				var block = blocks[i];
				var otherName = block.getFieldValue("INPUTNAME");
				if (!block.disabled && !block.getInheritedDisabled() &&
					otherName != null && otherName.ToLowerCase() == name) {
					count++;
				}
			}
			var msg = (count > 1) ?
				"There are " + count + " input blocks\n with this name." : null;
			referenceBlock.setWarningText(msg);
		}
	}
}
