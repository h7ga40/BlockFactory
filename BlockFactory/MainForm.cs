using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlockFactoryApp;
using Blockly;
using BlocklyMruby;
using Bridge;
using Bridge.Html5;
using goog.html;
using goog.ui.tree;

namespace BlockFactoryApp
{
	public partial class MainForm : Form, IClassWorkspace
	{
		public Workspace Workspace { get; private set; }

		public BlocklyView View { get; private set; }

		public Ruby RubyCode { get; private set; }

		public string Identifier { get; private set; }

		static MainForm()
		{
			// http://www.osadasoft.com/c-webbrowser%e3%82%b3%e3%83%b3%e3%83%88%e3%83%ad%e3%83%bc%e3%83%ab%e3%81%ae%e3%83%ac%e3%83%b3%e3%83%80%e3%83%aa%e3%83%b3%e3%82%b0%e3%83%a2%e3%83%bc%e3%83%89%e3%82%92%e3%83%87%e3%83%95%e3%82%a9/
			try {
				var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
					@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
				regkey.SetValue(Path.GetFileName(Application.ExecutablePath).ToLower(), 11001);
				regkey.Close();

				//regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
				//	@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_DOCUMENT_COMPATIBLE_MODE");
				//regkey.SetValue(Path.GetFileName(Application.ExecutablePath).ToLower(), 11001);
				//regkey.Close();
			}
			catch (Exception) {
			}
		}

		public MainForm()
		{
			InitializeComponent();
			webBrowser1.ObjectForScripting = new ScriptingHost();
			webBrowser1.DocumentText = Properties.Resources.Canvas;
		}

		private void WebBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			Script.Init(webBrowser1.Document);
#if false
			var htmlDiv = (HTMLDivElement)goog.dom.createDom(goog.dom.TagName.DIV);
			htmlDiv.SetAttribute("dir", "LTR");
			htmlDiv.Style.BoxSizing = BoxSizing.BorderBox;
			Document.Body.AppendChild(htmlDiv);
			//Document.Body.Style.Overflow = Overflow.Hidden;
			htmlDiv.Id = "Main";
			Identifier = "Main";
			View = new BlocklyView(Identifier);
			var toolbox = Properties.Resources.Toolbox;
			Workspace = View.Init(Identifier, toolbox);
			View.ReloadToolbox(this);
#else
			BlockFactory.blocklyFactory = new AppController();
			BlockFactory.blocklyFactory.init();
#endif
		}

		private static void CreateTreeNodes(goog.ui.tree.TreeNode node, JsArray<object> data)
		{
			node.setText((string)data[0]);
			if (data.Length > 1) {
				var children = (JsArray<object>)data[1];
				foreach (var child in children) {
					var childNode = node.getTree().createNode("");
					node.add(childNode);

					CreateTreeNodes(childNode, (JsArray<object>)child);
				}
			}
		}

		public string GetImageUrl()
		{
			return "img/no_image.png";
		}

		public bool IsPreset()
		{
			return true;
		}

		public string ToCode(string filename)
		{
			throw new NotImplementedException();
			/*if (Collections.EcnlTaskWorkspace == null)
				return "";

			RubyCode = new Ruby(filename);
			var result = RubyCode.defineMainLoop(this, Collections.EcnlTaskWorkspace.Identifier);
			View.Changed = false;
			return result;*/
		}

		public void Inactivate()
		{
		}

		public void ReloadToolbox(Element toolbox)
		{
			toolbox.AppendChild(Document.CreateElement<Element>("sep"));
			var xml = (new DOMParser()).ParseFromString(Properties.Resources.Toolbox, "text/xml");
			var categories = xml.ChildNodes[0];
			foreach (var item in categories.ChildNodes) {
				if (item.NodeName != "category")
					continue;
				toolbox.AppendChild(item);
			}
		}

		public void OpenModifyView(Action<bool> callback)
		{
			View.ReloadToolbox(this);
			callback(true);
		}

		public string Template(string template)
		{
			throw new NotImplementedException();
		}
	}
}
