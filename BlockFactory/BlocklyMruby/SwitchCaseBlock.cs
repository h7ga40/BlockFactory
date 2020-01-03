using System;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlocklyMruby
{
	public class SwitchCaseNumberBlock : BlockSvg
	{
		public const string type_name = "switch_case_number";

		internal JsArray<Tuple<string, string>> cases_;
		internal int defaultCount_;

		public SwitchCaseNumberBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setHelpUrl("http://www.example.com/");
			setColour(210);
			appendValueInput("SWITCH")
				.appendField("右の値が");
			setPreviousStatement(true);
			setNextStatement(true);
			setMutator(new Mutator(new[] {
				SwitchCaseNumberConstBlock.type_name,
				SwitchCaseNumberRangeBlock.type_name,
				SwitchCaseNumberDefaultBlock.type_name }));
			setTooltip(new Func<string>(() => {
				if ((cases_.Length == 0) && (defaultCount_ == 0)) {
					return "条件に合うブロックを実行";
				}
				else if ((cases_.Length == 0) && (defaultCount_ != 0)) {
					return "条件に合うブロックを実行、合うものがなければ最後のブロックを実行";
				}
				else if ((cases_.Length != 0) && (defaultCount_ == 0)) {
					return "条件に合うブロックを実行";
				}
				else if ((cases_.Length != 0) && (defaultCount_ != 0)) {
					return "条件に合うブロックを実行、合うものがなければ最後のブロックを実行";
				}
				return "";
			}));
			cases_ = new JsArray<Tuple<string, string>> { new Tuple<string, string>("0", null) };
			defaultCount_ = 0;
			updateShape_();
		}

		/// <summary>
		/// Create XML to represent the number of else-if and else inputs.
		/// </summary>
		/// <returns>XML storage element.</returns>
		public Element mutationToDom(bool opt_caseIds)
		{
			if ((cases_.Length == 0) && (defaultCount_ == 0)) {
				return null;
			}
			var container = Document.CreateElement<Element>("mutation");
			for (var i = 0; i < cases_.Length; i++) {
				Element caseInfo;
				var value = getFieldValue("CONST" + i);
				if (value != null) {
					caseInfo = Document.CreateElement<Element>("case");
					caseInfo.SetAttribute("value", value);
				}
				else {
					var min = getFieldValue("RANGE_MIN" + i);
					var max = getFieldValue("RANGE_MAX" + i);
					if (min != null && max != null) {
						caseInfo = Document.CreateElement<Element>("case");
						caseInfo.SetAttribute("value", min + ".." + max);
					}
					else
						continue;
				}
				container.AppendChild(caseInfo);
			}
			container.SetAttribute("default", defaultCount_.ToString());
			return container;
		}

		/// <summary>
		/// Parse XML to restore the else-if and else inputs.
		/// </summary>
		/// <param name="xmlElement">XML storage element.</param>
		public override void domToMutation(Element xmlElement)
		{
			cases_ = new JsArray<Tuple<string, string>>();
			foreach (Element childNode in xmlElement.ChildNodes) {
				if (childNode.NodeName.ToLower() == "case") {
					var value = childNode.GetAttribute("value");
					if (value != null)
						cases_.Push(new Tuple<string, string>(value, null));
					else {
						var min = childNode.GetAttribute("minimum");
						var max = childNode.GetAttribute("maximum");
						if ((min != null) && (max != null)) {
							cases_.Push(new Tuple<string, string>(min, max));
						}
					}
				}
			}
			var count = xmlElement.GetAttribute("default");
			defaultCount_ = count == null ? 0 : Int32.Parse(count);
			updateShape_();
		}

		/// <summary>
		/// Populate the mutator's dialog with this block's components.
		/// </summary>
		/// <param name="workspace">Mutator's workspace.</param>
		/// <returns>Root block in mutator.</returns>
		public override Block decompose(Workspace workspace)
		{
			var containerBlock = (BlockSvg)workspace.newBlock(SwitchCaseNumberContainerBlock.type_name);
			containerBlock.initSvg();
			var connection = containerBlock.getInput("STACK").connection;
			for (var i = 0; i < cases_.Length; i++) {
				BlockSvg caseBlock;
				var value = getFieldValue("CONST" + i);
				if (value != null) {
					caseBlock = (BlockSvg)workspace.newBlock(SwitchCaseNumberConstBlock.type_name);
					caseBlock.setFieldValue(value, "CONST");
				}
				else {
					var min = getFieldValue("RANGE_MIN" + i);
					var max = getFieldValue("RANGE_MAX" + i);
					if ((min != null) && (max != null)) {
						caseBlock = (BlockSvg)workspace.newBlock(SwitchCaseNumberRangeBlock.type_name);
						caseBlock.setFieldValue(min, "RANGE_MIN");
						caseBlock.setFieldValue(max, "RANGE_MAX");
					}
					else
						continue;
				}
				caseBlock.initSvg();
				connection.connect(caseBlock.previousConnection);
				connection = caseBlock.nextConnection;
			}
			if (defaultCount_ != 0) {
				var defaultBlock = (BlockSvg)workspace.newBlock(SwitchCaseNumberDefaultBlock.type_name);
				defaultBlock.initSvg();
				connection.connect(defaultBlock.previousConnection);
			}
			return containerBlock;
		}

		/// <summary>
		/// Reconfigure this block based on the mutator dialog's components.
		/// </summary>
		/// <param name="containerBlock">Root block in mutator.</param>
		public override void compose(Block containerBlock)
		{
			var clauseBlock = containerBlock.getInputTargetBlock("STACK");
			// Count number of inputs.
			cases_ = new JsArray<Tuple<string, string>>();
			defaultCount_ = 0;
			var statementConnections = new JsArray<Connection>();
			Connection defaultStatementConnection = null;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case SwitchCaseNumberConstBlock.type_name: {
					var value = clauseBlock.getFieldValue("CONST");
					cases_.Push(new Tuple<string, string>(value, null));
					statementConnections.Push(((SwitchCaseNumberConstBlock)clauseBlock).statementConnection_);
				}
				break;
				case SwitchCaseNumberRangeBlock.type_name: {
					var range_min = clauseBlock.getFieldValue("RANGE_MIN");
					var range_max = clauseBlock.getFieldValue("RANGE_MAX");
					cases_.Push(new Tuple<string, string>(range_min, range_max));
					statementConnections.Push(((SwitchCaseNumberRangeBlock)clauseBlock).statementConnection_);
				}
				break;
				case SwitchCaseNumberDefaultBlock.type_name: {
					defaultCount_++;
					defaultStatementConnection = ((SwitchCaseNumberDefaultBlock)clauseBlock).statementConnection_;
				}
				break;
				default:
					throw new Exception("Unknown block type.");
				}
				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
			updateShape_();
			// Reconnect any child blocks.
			for (var i = 0; i < cases_.Length; i++) {
				Mutator.reconnect(statementConnections[i], this, "DO" + i);
			}
			Mutator.reconnect(defaultStatementConnection, this, "DEFAULT_DO");
		}

		/// <summary>
		/// Store pointers to any connected child blocks.
		/// </summary>
		/// <param name="containerBlock">Root block in mutator.</param>
		public override void saveConnections(Block containerBlock)
		{
			var clauseBlock = containerBlock.getInputTargetBlock("STACK");
			var i = 0;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case SwitchCaseNumberConstBlock.type_name: {
					var inputDo = getInput("DO" + i);
					((SwitchCaseNumberConstBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
					i++;
				}
				break;
				case SwitchCaseNumberRangeBlock.type_name: {
					var inputDo = getInput("DO" + i);
					((SwitchCaseNumberRangeBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
					i++;
				}
				break;
				case SwitchCaseNumberDefaultBlock.type_name: {
					var inputDo = getInput("DEFAULT_DO");
					((SwitchCaseNumberDefaultBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
				}
				break;
				default:
					throw new Exception("Unknown block type.");
				}

				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
		}

		/// <summary>
		/// Modify this block to have the correct number of inputs.
		/// </summary>
		private void updateShape_()
		{
			// Delete everything.
			if (getInput("DEFAULT") != null) {
				removeInput("DEFAULT");
				removeInput("DEFAULT_DO");
			}
			var i = 0;
			while (getInput("CASE" + i) != null) {
				removeInput("CASE" + i);
				removeInput("DO" + i);
				i++;
			}
			// Rebuild block.
			i = 0;
			foreach (var c in cases_) {
				if (c.Item2 == null) {
					appendDummyInput("CASE" + i)
						.appendField(new FieldNumber(c.Item1, Double.NegativeInfinity, Double.PositiveInfinity, 0), "CONST" + i)
						.appendField("の");
				}
				else {
					appendDummyInput("CASE" + i)
						.appendField(new FieldNumber(c.Item1, Double.NegativeInfinity, Double.PositiveInfinity, 0), "RANGE_MIN" + i)
						.appendField("から")
						.appendField(new FieldNumber(c.Item2, Double.NegativeInfinity, Double.PositiveInfinity, 0), "RANGE_MAX" + i)
						.appendField("の");
				}
				appendStatementInput("DO" + i)
					.appendField("とき");
				i++;
			}
			if (defaultCount_ != 0) {
				appendDummyInput("DEFAULT")
					.appendField("その他の");
				appendStatementInput("DEFAULT_DO")
					.appendField("とき");
			}
		}
	}

	public class SwitchCaseNumberContainerBlock : BlockSvg
	{
		public const string type_name = "switch_case_number_container";

		public SwitchCaseNumberContainerBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			appendDummyInput()
				.appendField("条件");
			appendStatementInput("STACK");
			setColour(210);
			setTooltip("");
			contextMenu = false;
		}
	}

	public class SwitchCaseNumberConstBlock : BlockSvg
	{
		public const string type_name = "switch_case_number_const";
		public Connection statementConnection_;

		public SwitchCaseNumberConstBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("固定値")
				.appendField("0", "CONST");
			setPreviousStatement(true);
			setNextStatement(true);
			setTooltip("固定値の条件");
			contextMenu = false;
		}
	}

	public class SwitchCaseNumberRangeBlock : BlockSvg
	{
		public const string type_name = "switch_case_number_range";
		public Connection statementConnection_;

		public SwitchCaseNumberRangeBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("範囲")
				.appendField("1", "RANGE_MIN")
				.appendField("から")
				.appendField("2", "RANGE_MAX");
			setPreviousStatement(true);
			setNextStatement(true);
			setTooltip("範囲の条件");
			contextMenu = false;
		}
	}

	public class SwitchCaseNumberDefaultBlock : BlockSvg
	{
		public const string type_name = "switch_case_number_default";
		public Connection statementConnection_;

		public SwitchCaseNumberDefaultBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("その他");
			setPreviousStatement(true);
			setTooltip("条件に当てはまらなかった場合");
			contextMenu = false;
		}
	}

	public class SwitchCaseTextBlock : BlockSvg
	{
		public const string type_name = "switch_case_text";

		internal JsArray<Tuple<string, string>> cases_;
		internal int defaultCount_;

		public SwitchCaseTextBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setHelpUrl("http://www.example.com/");
			setColour(210);
			appendValueInput("SWITCH")
				.appendField("右の値が");
			setPreviousStatement(true);
			setNextStatement(true);
			setMutator(new Mutator(new[] {
				SwitchCaseTextConstBlock.type_name,
				SwitchCaseTextRangeBlock.type_name,
				SwitchCaseTextDefaultBlock.type_name }));
			setTooltip(new Func<string>(() => {
				if ((cases_.Length == 0) && (defaultCount_ == 0)) {
					return "条件に合うブロックを実行";
				}
				else if ((cases_.Length == 0) && (defaultCount_ != 0)) {
					return "条件に合うブロックを実行、合うものがなければ最後のブロックを実行";
				}
				else if ((cases_.Length != 0) && (defaultCount_ == 0)) {
					return "条件に合うブロックを実行";
				}
				else if ((cases_.Length != 0) && (defaultCount_ != 0)) {
					return "条件に合うブロックを実行、合うものがなければ最後のブロックを実行";
				}
				return "";
			}));
			cases_ = new JsArray<Tuple<string, string>> { new Tuple<string, string>("0", null) };
			defaultCount_ = 0;
			updateShape_();
		}

		/// <summary>
		/// Create XML to represent the text of else-if and else inputs.
		/// </summary>
		/// <returns>XML storage element.</returns>
		public Element mutationToDom(bool opt_caseIds)
		{
			if ((cases_.Length == 0) && (defaultCount_ == 0)) {
				return null;
			}
			var container = Document.CreateElement<Element>("mutation");
			for (var i = 0; i < cases_.Length; i++) {
				Element caseInfo;
				var value = getFieldValue("CONST" + i);
				if (value != null) {
					caseInfo = Document.CreateElement<Element>("case");
					caseInfo.SetAttribute("value", value);
				}
				else {
					var min = getFieldValue("RANGE_MIN" + i);
					var max = getFieldValue("RANGE_MAX" + i);
					if (min != null && max != null) {
						caseInfo = Document.CreateElement<Element>("case");
						caseInfo.SetAttribute("value", min + ".." + max);
					}
					else
						continue;
				}
				container.AppendChild(caseInfo);
			}
			container.SetAttribute("default", defaultCount_.ToString());
			return container;
		}

		/// <summary>
		/// Parse XML to restore the else-if and else inputs.
		/// </summary>
		/// <param name="xmlElement">XML storage element.</param>
		public override void domToMutation(Element xmlElement)
		{
			cases_ = new JsArray<Tuple<string, string>>();
			foreach (Element childNode in xmlElement.ChildNodes) {
				if (childNode.NodeName.ToLower() == "case") {
					var value = childNode.GetAttribute("value");
					if (value != null)
						cases_.Push(new Tuple<string, string>(value, null));
					else {
						var min = childNode.GetAttribute("minimum");
						var max = childNode.GetAttribute("maximum");
						if ((min != null) && (max != null))
							cases_.Push(new Tuple<string, string>(min, max));
					}
				}
			}
			var count = xmlElement.GetAttribute("default");
			defaultCount_ = count == null ? 0 : Int32.Parse(count);
			updateShape_();
		}

		/// <summary>
		/// Populate the mutator's dialog with this block's components.
		/// </summary>
		/// <param name="workspace">Mutator's workspace.</param>
		/// <returns>Root block in mutator.</returns>
		public override Block decompose(Workspace workspace)
		{
			var containerBlock = (BlockSvg)workspace.newBlock(SwitchCaseTextContainerBlock.type_name);
			containerBlock.initSvg();
			var connection = containerBlock.getInput("STACK").connection;
			for (var i = 0; i < cases_.Length; i++) {
				BlockSvg caseBlock;
				var value = getFieldValue("CONST" + i);
				if (value != null) {
					caseBlock = (BlockSvg)workspace.newBlock(SwitchCaseTextConstBlock.type_name);
					caseBlock.setFieldValue(value, "CONST");
				}
				else {
					var min = getFieldValue("RANGE_MIN" + i);
					var max = getFieldValue("RANGE_MAX" + i);
					if ((min != null) && (max != null)) {
						caseBlock = (BlockSvg)workspace.newBlock(SwitchCaseTextRangeBlock.type_name);
						caseBlock.setFieldValue(min, "RANGE_MIN");
						caseBlock.setFieldValue(max, "RANGE_MAX");
					}
					else
						continue;
				}
				caseBlock.initSvg();
				connection.connect(caseBlock.previousConnection);
				connection = caseBlock.nextConnection;
			}
			if (defaultCount_ != 0) {
				var defaultBlock = (BlockSvg)workspace.newBlock(SwitchCaseTextDefaultBlock.type_name);
				defaultBlock.initSvg();
				connection.connect(defaultBlock.previousConnection);
			}
			return containerBlock;
		}

		/// <summary>
		/// Reconfigure this block based on the mutator dialog's components.
		/// </summary>
		/// <param name="containerBlock">Root block in mutator.</param>
		public override void compose(Block containerBlock)
		{
			var clauseBlock = containerBlock.getInputTargetBlock("STACK");
			// Count text of inputs.
			cases_ = new JsArray<Tuple<string, string>>();
			defaultCount_ = 0;
			var statementConnections = new JsArray<Connection>();
			Connection defaultStatementConnection = null;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case SwitchCaseTextConstBlock.type_name: {
					var value = clauseBlock.getFieldValue("CONST");
					cases_.Push(new Tuple<string, string>(value, null));
					statementConnections.Push(((SwitchCaseTextConstBlock)clauseBlock).statementConnection_);
				}
				break;
				case SwitchCaseTextRangeBlock.type_name: {
					var range_min = clauseBlock.getFieldValue("RANGE_MIN");
					var range_max = clauseBlock.getFieldValue("RANGE_MAX");
					cases_.Push(new Tuple<string, string>(range_min, range_max));
					statementConnections.Push(((SwitchCaseTextRangeBlock)clauseBlock).statementConnection_);
				}
				break;
				case SwitchCaseTextDefaultBlock.type_name: {
					defaultCount_++;
					defaultStatementConnection = ((SwitchCaseTextDefaultBlock)clauseBlock).statementConnection_;
				}
				break;
				default:
					throw new Exception("Unknown block type.");
				}
				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
			updateShape_();
			// Reconnect any child blocks.
			for (var i = 0; i <= cases_.Length; i++) {
				Mutator.reconnect(statementConnections[i], this, "DO" + i);
			}
			Mutator.reconnect(defaultStatementConnection, this, "DEFAULT_DO");
		}

		/// <summary>
		/// Store pointers to any connected child blocks.
		/// </summary>
		/// <param name="containerBlock">Root block in mutator.</param>
		public override void saveConnections(Block containerBlock)
		{
			var clauseBlock = containerBlock.getInputTargetBlock("STACK");
			var i = 0;
			while (clauseBlock != null) {
				switch (clauseBlock.type) {
				case SwitchCaseTextConstBlock.type_name: {
					var inputDo = getInput("DO" + i);
					((SwitchCaseTextConstBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
					i++;
				}
				break;
				case SwitchCaseTextRangeBlock.type_name: {
					var inputDo = getInput("DO" + i);
					((SwitchCaseTextRangeBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
					i++;
				}
				break;
				case SwitchCaseTextDefaultBlock.type_name: {
					var inputDo = getInput("DEFAULT_DO");
					((SwitchCaseTextDefaultBlock)clauseBlock).statementConnection_ =
						(inputDo != null) ? inputDo.connection.targetConnection : null;
				}
				break;
				default:
					throw new Exception("Unknown block type.");
				}

				clauseBlock = (clauseBlock.nextConnection != null) ?
					clauseBlock.nextConnection.targetBlock() : null;
			}
		}

		/// <summary>
		/// Modify this block to have the correct text of inputs.
		/// </summary>
		private void updateShape_()
		{
			// Delete everything.
			if (getInput("DEFAULT") != null) {
				removeInput("DEFAULT");
				removeInput("DEFAULT_DO");
			}
			var i = 0;
			while (getInput("CASE" + i) != null) {
				removeInput("CASE" + i);
				removeInput("DO" + i);
				i++;
			}
			// Rebuild block.
			i = 0;
			foreach (var c in cases_) {
				if (c.Item2 == null) {
					appendDummyInput("CASE" + i)
						.appendField(new FieldTextInput(c.Item1), "CONST" + i)
						.appendField("の");
				}
				else {
					appendDummyInput("CASE" + i)
						.appendField(new FieldTextInput(c.Item1), "RANGE_MIN" + i)
						.appendField("から")
						.appendField(new FieldTextInput(c.Item2), "RANGE_MAX" + i)
						.appendField("の");
				}
				appendStatementInput("DO" + i)
					.appendField("とき");
				i++;
			}
			if (defaultCount_ != 0) {
				appendDummyInput("DEFAULT")
					.appendField("その他の");
				appendStatementInput("DEFAULT_DO")
					.appendField("とき");
			}
		}
	}

	public class SwitchCaseTextContainerBlock : BlockSvg
	{
		public const string type_name = "switch_case_text_container";

		public SwitchCaseTextContainerBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		public override void init()
		{
			appendDummyInput()
				.appendField("条件");
			appendStatementInput("STACK");
			setColour(210);
			setTooltip("");
			contextMenu = false;
		}
	}

	public class SwitchCaseTextConstBlock : BlockSvg
	{
		public const string type_name = "switch_case_text_const";
		public Connection statementConnection_;

		public SwitchCaseTextConstBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("固定値")
				.appendField("0", "CONST");
			setPreviousStatement(true);
			setNextStatement(true);
			setTooltip("固定値の条件");
			contextMenu = false;
		}
	}

	public class SwitchCaseTextRangeBlock : BlockSvg
	{
		public const string type_name = "switch_case_text_range";
		public Connection statementConnection_;

		public SwitchCaseTextRangeBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("範囲")
				.appendField("a", "RANGE_MIN")
				.appendField("から")
				.appendField("b", "RANGE_MAX");
			setPreviousStatement(true);
			setNextStatement(true);
			setTooltip("範囲の条件");
			contextMenu = false;
		}
	}

	public class SwitchCaseTextDefaultBlock : BlockSvg
	{
		public const string type_name = "switch_case_text_default";
		public Connection statementConnection_;

		public SwitchCaseTextDefaultBlock(Workspace workspace)
			: base(workspace, type_name)
		{
		}

		/// <summary>
		/// Block for swicth/case/default condition.
		/// </summary>
		public override void init()
		{
			setColour(210);
			appendDummyInput()
				.appendField("その他");
			setPreviousStatement(true);
			setTooltip("条件に当てはまらなかった場合");
			contextMenu = false;
		}
	}

	partial class Ruby
	{
		public node switch_case_number(SwitchCaseNumberBlock block)
		{
			// case/when/else condition.
			var argument0 = valueToCode(block, "SWITCH");
			if (argument0 == null) argument0 = new int_node(this, -1);
			var code = new JsArray<case_node.when_t>();
			for (int n = 0; n <= block.cases_.Length; n++) {
				var branch = statementToCode(block, "DO" + n);
				var argument1 = block.getFieldValue("CONST" + n);
				if (argument1 != null) {
					var when = new case_node.when_t() { body = branch };
					when.value.Push(new int_node(this, argument1 == null ? 0 : Int32.Parse(argument1)));
					code.Push(when);
				}
				else {
					var min = block.getFieldValue("RANGE_MIN" + n);
					var max = block.getFieldValue("RANGE_MAX" + n);
					if ((min != null) && (max != null)) {
						var when = new case_node.when_t() { body = branch };
						when.value.Push(new dot2_node(this,
							new int_node(this, min == null ? 0 : Int32.Parse(min)),
							new int_node(this, max == null ? 0 : Int32.Parse(max))));
						code.Push(when);
					}
				}
			}
			if (block.defaultCount_ != 0) {
				var branch = statementToCode(block, "DEFAULT_DO");
				if (branch != null) {
					var when = new case_node.when_t() { body = branch };
					code.Push(when);
				}
			}
			return new case_node(this, argument0, code);
		}

		public node switch_case_text(SwitchCaseTextBlock block)
		{
			// case/when/else condition.
			var argument0 = valueToCode(block, "SWITCH");
			if (argument0 == null) argument0 = new str_node(this, "");
			var code = new JsArray<case_node.when_t>();
			for (int n = 0; n <= block.cases_.Length; n++) {
				var branch = statementToCode(block, "DO" + n);
				var argument1 = block.getFieldValue("CONST" + n);
				if (argument1 != null) {
					var when = new case_node.when_t() { body = branch };
					when.value.Push(new str_node(this, argument1));
					code.Push(when);
				}
				else {
					var min = block.getFieldValue("RANGE_MIN" + n);
					var max = block.getFieldValue("RANGE_MAX" + n);
					if ((min != null) && (max != null)) {
						var when = new case_node.when_t() { body = branch };
						when.value.Push(new dot2_node(this, new str_node(this, min), new str_node(this, max)));
						code.Push(when);
					}
				}
			}
			if (block.defaultCount_ != 0) {
				var branch = statementToCode(block, "DEFAULT_DO");
				if (branch != null) {
					var when = new case_node.when_t() { body = branch };
					code.Push(when);
				}
			}
			return new case_node(this, argument0, code);
		}
	}
}
