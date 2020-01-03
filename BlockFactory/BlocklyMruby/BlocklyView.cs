using System;
using System.Collections.Generic;
using System.Linq;
using Blockly;
using Bridge;
using Bridge.Html5;

namespace BlocklyMruby
{
	public interface IModel
	{
		string Identifier { get; }
	}

	public interface IClassWorkspace : IModel
	{
		Workspace Workspace { get; }
		BlocklyView View { get; }
		Ruby RubyCode { get; }
		string GetImageUrl();
		bool IsPreset();
		string ToCode(string filename);
		void Activate();
		void Inactivate();
		void ReloadToolbox(Element toolbox);
		void OpenModifyView(Action<bool> callback);
		string Template(string template);
	}

	public class BlocklyView
	{
		string identifier;
		public string Identifier {
			get { return identifier; }
			set {
				if (identifier != value) {
					identifier = value;
					IdentifierChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		public Dictionary<string, FlyoutCategoryHandler> FlyoutCategoryHandlers = new Dictionary<string, FlyoutCategoryHandler>();
		public event EventHandler<Events.Create> BlockCreated;
		public event EventHandler<Events.Delete> BlockDeleted;
		public event EventHandler<Events.Change> BlockChanged;
		public event EventHandler<Events.Move> BlockMoveed;
		public event EventHandler<Events.Ui> UiEvent;
		public event EventHandler<EventArgs> IdentifierChanged;
		public bool Changed = true;

		private static int No = 1;
		private WorkspaceSvg _Workspace;
		private string _WorkspaceElementId;
		private int _IdNo;
		private string toolbox;

		static BlocklyView()
		{
			Core.Blocks = new Blocks();
			Core.Blocks.Add(ColourPickerBlock.type_name, typeof(ColourPickerBlock));
			Core.Blocks.Add(ColourRandomBlock.type_name, typeof(ColourRandomBlock));
			Core.Blocks.Add(ColourRGBBlock.type_name, typeof(ColourRGBBlock));
			Core.Blocks.Add(ColourBlendBlock.type_name, typeof(ColourBlendBlock));
			Core.Blocks.Add(ListsCreateEmptyBlock.type_name, typeof(ListsCreateEmptyBlock));
			Core.Blocks.Add(ListsCreateWithBlock.type_name, typeof(ListsCreateWithBlock));
			Core.Blocks.Add(ListsCreateWithContainerBlock.type_name, typeof(ListsCreateWithContainerBlock));
			Core.Blocks.Add(ListsCreateWithItemBlock.type_name, typeof(ListsCreateWithItemBlock));
			Core.Blocks.Add(ListsRepeatBlock.type_name, typeof(ListsRepeatBlock));
			Core.Blocks.Add(ListsLengthBlock.type_name, typeof(ListsLengthBlock));
			Core.Blocks.Add(ListsIsEmptyBlock.type_name, typeof(ListsIsEmptyBlock));
			Core.Blocks.Add(ListsIndexOfBlock.type_name, typeof(ListsIndexOfBlock));
			Core.Blocks.Add(ListsGetIndexBlock.type_name, typeof(ListsGetIndexBlock));
			Core.Blocks.Add(ListsSetIndexBlock.type_name, typeof(ListsSetIndexBlock));
			Core.Blocks.Add(ListsGetSublistBlock.type_name, typeof(ListsGetSublistBlock));
			Core.Blocks.Add(ListsSortBlock.type_name, typeof(ListsSortBlock));
			Core.Blocks.Add(ListsSplitBlock.type_name, typeof(ListsSplitBlock));
			Core.Blocks.Add(ControlsIfBlock.type_name, typeof(ControlsIfBlock));
			Core.Blocks.Add(ControlsIfIfBlock.type_name, typeof(ControlsIfIfBlock));
			Core.Blocks.Add(ControlsIfElseIfBlock.type_name, typeof(ControlsIfElseIfBlock));
			Core.Blocks.Add(ControlsIfElseBlock.type_name, typeof(ControlsIfElseBlock));
			Core.Blocks.Add(SwitchCaseNumberBlock.type_name, typeof(SwitchCaseNumberBlock));
			Core.Blocks.Add(SwitchCaseNumberContainerBlock.type_name, typeof(SwitchCaseNumberContainerBlock));
			Core.Blocks.Add(SwitchCaseNumberConstBlock.type_name, typeof(SwitchCaseNumberConstBlock));
			Core.Blocks.Add(SwitchCaseNumberRangeBlock.type_name, typeof(SwitchCaseNumberRangeBlock));
			Core.Blocks.Add(SwitchCaseNumberDefaultBlock.type_name, typeof(SwitchCaseNumberDefaultBlock));
			Core.Blocks.Add(SwitchCaseTextBlock.type_name, typeof(SwitchCaseTextBlock));
			Core.Blocks.Add(SwitchCaseTextContainerBlock.type_name, typeof(SwitchCaseTextContainerBlock));
			Core.Blocks.Add(SwitchCaseTextConstBlock.type_name, typeof(SwitchCaseTextConstBlock));
			Core.Blocks.Add(SwitchCaseTextRangeBlock.type_name, typeof(SwitchCaseTextRangeBlock));
			Core.Blocks.Add(SwitchCaseTextDefaultBlock.type_name, typeof(SwitchCaseTextDefaultBlock));
			Core.Blocks.Add(LogicCompareBlock.type_name, typeof(LogicCompareBlock));
			Core.Blocks.Add(LogicOperationBlock.type_name, typeof(LogicOperationBlock));
			Core.Blocks.Add(LogicNegateBlock.type_name, typeof(LogicNegateBlock));
			Core.Blocks.Add(LogicBooleanBlock.type_name, typeof(LogicBooleanBlock));
			Core.Blocks.Add(LogicNullBlock.type_name, typeof(LogicNullBlock));
			Core.Blocks.Add(LogicTernaryBlock.type_name, typeof(LogicTernaryBlock));
			Core.Blocks.Add(ControlsRepeatExtBlock.type_name, typeof(ControlsRepeatExtBlock));
			Core.Blocks.Add(ControlsRepeatBlock.type_name, typeof(ControlsRepeatBlock));
			Core.Blocks.Add(ControlsWhileUntilBlock.type_name, typeof(ControlsWhileUntilBlock));
			Core.Blocks.Add(ControlsForBlock.type_name, typeof(ControlsForBlock));
			Core.Blocks.Add(ControlsForEachBlock.type_name, typeof(ControlsForEachBlock));
			Core.Blocks.Add(ControlsFlowStatementsBlock.type_name, typeof(ControlsFlowStatementsBlock));
			Core.Blocks.Add(MathNumberBlock.type_name, typeof(MathNumberBlock));
			Core.Blocks.Add(MathArithmeticBlock.type_name, typeof(MathArithmeticBlock));
			Core.Blocks.Add(MathSingleBlock.type_name, typeof(MathSingleBlock));
			Core.Blocks.Add(MathTrigBlock.type_name, typeof(MathTrigBlock));
			Core.Blocks.Add(MathConstantBlock.type_name, typeof(MathConstantBlock));
			Core.Blocks.Add(MathNumberPropertyBlock.type_name, typeof(MathNumberPropertyBlock));
			Core.Blocks.Add(MathChangeBlock.type_name, typeof(MathChangeBlock));
			Core.Blocks.Add(MathRoundBlock.type_name, typeof(MathRoundBlock));
			Core.Blocks.Add(MathOnListBlock.type_name, typeof(MathOnListBlock));
			Core.Blocks.Add(MathModuloBlock.type_name, typeof(MathModuloBlock));
			Core.Blocks.Add(MathConstrainBlock.type_name, typeof(MathConstrainBlock));
			Core.Blocks.Add(MathRandomIntBlock.type_name, typeof(MathRandomIntBlock));
			Core.Blocks.Add(MathRandomFloatBlock.type_name, typeof(MathRandomFloatBlock));
			Core.Blocks.Add(ProceduresDefnoreturnBlock.type_name, typeof(ProceduresDefnoreturnBlock));
			Core.Blocks.Add(ProceduresDefreturnBlock.type_name, typeof(ProceduresDefreturnBlock));
			Core.Blocks.Add(ProceduresMutatorcontainerBlock.type_name, typeof(ProceduresMutatorcontainerBlock));
			Core.Blocks.Add(ProceduresMutatorargBlock.type_name, typeof(ProceduresMutatorargBlock));
			Core.Blocks.Add(ProceduresCallnoreturnBlock.type_name, typeof(ProceduresCallnoreturnBlock));
			Core.Blocks.Add(ProceduresCallreturnBlock.type_name, typeof(ProceduresCallreturnBlock));
			Core.Blocks.Add(ProceduresIfreturnBlock.type_name, typeof(ProceduresIfreturnBlock));
			Core.Blocks.Add(TextBlock.type_name, typeof(TextBlock));
			Core.Blocks.Add(TextJoinBlock.type_name, typeof(TextJoinBlock));
			Core.Blocks.Add(TextCreateJoinContainerBlock.type_name, typeof(TextCreateJoinContainerBlock));
			Core.Blocks.Add(TextCreateJoinItemBlock.type_name, typeof(TextCreateJoinItemBlock));
			Core.Blocks.Add(TextAppendBlock.type_name, typeof(TextAppendBlock));
			Core.Blocks.Add(TextLengthBlock.type_name, typeof(TextLengthBlock));
			Core.Blocks.Add(TextIsEmptyBlock.type_name, typeof(TextIsEmptyBlock));
			Core.Blocks.Add(TextIndexOfBlock.type_name, typeof(TextIndexOfBlock));
			Core.Blocks.Add(TextCharAtBlock.type_name, typeof(TextCharAtBlock));
			Core.Blocks.Add(TextGetSubstringBlock.type_name, typeof(TextGetSubstringBlock));
			Core.Blocks.Add(TextChangeCaseBlock.type_name, typeof(TextChangeCaseBlock));
			Core.Blocks.Add(TextTrimBlock.type_name, typeof(TextTrimBlock));
			Core.Blocks.Add(TextPrintBlock.type_name, typeof(TextPrintBlock));
			Core.Blocks.Add(TextPromptExtBlock.type_name, typeof(TextPromptExtBlock));
			Core.Blocks.Add(TextPromptBlock.type_name, typeof(TextPromptBlock));
			Core.Blocks.Add(VariablesGetBlock.type_name, typeof(VariablesGetBlock));
			Core.Blocks.Add(VariablesSetBlock.type_name, typeof(VariablesSetBlock));
			Core.Procedures = new Procedures();
			Core.Variables = new Variables();
			Core.Names = new Names("");
		}

		public BlocklyView(string identifier)
		{
			Identifier = identifier;

			FlyoutCategoryHandlers.Add(Core.Procedures.NAME_TYPE, Procedures.flyoutCategory);
			FlyoutCategoryHandlers.Add(Core.Variables.NAME_TYPE, Variables.flyoutCategory);
		}

		public Workspace Init(string id, Union<string, Element> toolbox)
		{
			this.toolbox = toolbox.Is<string>() ? toolbox.As<string>() : toolbox.As<Element>().InnerHTML;
			var tab = Document.GetElementById(id);
			// <div dir="LTR" id="blockly-div"></div>
			var div = (HTMLDivElement)goog.dom.createDom("div");
			div.SetAttribute("dir", "LTR");
			div.SetAttribute("class", "blockly-div");
			_IdNo = No++;
			_WorkspaceElementId = "blockly-div" + _IdNo;
			div.SetAttribute("id", _WorkspaceElementId);
			div.SetAttribute("style", "z-index: " + _IdNo);
			div.Style.Left = "0";
			div.Style.Top = "0";
			div.Style.Width = "100%";
			div.Style.Height = "100%";
			div.Style.Position = Position.Absolute;
			tab.AppendChild(div);

			Core.HSV_SATURATION = 1.0;
			Core.HSV_VALUE = 0.8;
			_Workspace = Core.inject(_WorkspaceElementId, new Dictionary<string, object>() {
				{ "toolbox", toolbox.Value ?? Document.GetElementById("toolbox") },
				{ "collapse", true },
				{ "comments", true },
				{ "disable", true },
				{ "maxBlocks", Int32.MaxValue },
				{ "trashcan", true },
				{ "horizontalLayout", false },
				{ "toolboxPosition", "start" },
				{ "css", true },
				{ "rtl", false },
				{ "scrollbars", true },
				{ "sounds", false },
				{ "oneBasedIndex", false },
				{ "zoom", new Dictionary<string, object>() {
					{ "controls", true },
					{ "wheel", true },
					{ "startScale", 0.8 },
					{ "maxcale", 3 },
					{ "minScale", 0.3}
				} }
			});

			if (No != 2)
				Hide();
			else
				Show();

			_Workspace.toolbox_.flyout_.flyoutCategory =
					new Func<Union<string, JsArray<Node>, NodeList>, Workspace, JsArray<Node>>(FlyoutCategory);

			_Workspace.addChangeListener(Workspace_Changed);

			return _Workspace;
		}

		internal void Dispose()
		{
			var el = Document.GetElementById(_WorkspaceElementId);
			el.ParentNode.RemoveChild(el);
			_Workspace.clear();
			_Workspace.dispose();
		}

		internal void Show()
		{
			var div = Document.GetElementById(_WorkspaceElementId);
			div.SetAttribute("style", "z-index: " + (_IdNo + 100));

			Core.mainWorkspace = _Workspace;
		}

		internal void Hide()
		{
			var div = Document.GetElementById(_WorkspaceElementId);
			div.SetAttribute("style", "z-index: " + _IdNo);
		}

		private void Workspace_Changed(Events.Abstract e)
		{
			Changed = true;

			switch (e.type) {
			case Blockly.Events.CREATE:
				var cre = (Events.Create)e;
				BlockCreated?.Invoke(this, cre);
				break;
			case Blockly.Events.DELETE:
				var del = (Events.Delete)e;
				BlockDeleted?.Invoke(this, del);
				break;
			case Blockly.Events.CHANGE:
				var chg = (Events.Change)e;
				BlockChanged?.Invoke(this, chg);
				break;
			case Blockly.Events.MOVE:
				var mov = (Events.Move)e;
				BlockMoveed?.Invoke(this, mov);
				break;
			case Blockly.Events.UI:
				var ui = (Events.Ui)e;
				UiEvent?.Invoke(this, ui);
				break;
			}
		}

		internal void ReloadToolbox(IClassWorkspace workspace)
		{
			if (_Workspace != workspace.Workspace)
				throw new Exception();

			Core.mainWorkspace = _Workspace;
			Core.hideChaff();

			var toolbox = Document.CreateElement<Element>("xml");
			toolbox.InnerHTML = this.toolbox ?? Document.GetElementById("toolbox").InnerHTML;
			workspace.ReloadToolbox(toolbox);

			_Workspace.updateToolbox(toolbox);
		}

		public virtual JsArray<Node> FlyoutCategory(Union<string, JsArray<Node>, NodeList> name, Workspace workspace)
		{
			if (!name.Is<string>())
				return name.As<JsArray<Node>>();

			FlyoutCategoryHandler handler;
			if (FlyoutCategoryHandlers.TryGetValue(name.As<string>(), out handler)) {
				return handler(workspace);
			}
			return new JsArray<Node>();
		}
	}

	public delegate JsArray<Node> FlyoutCategoryHandler(Workspace workspace);
}
