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
