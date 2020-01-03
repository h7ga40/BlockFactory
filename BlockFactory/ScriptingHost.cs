using System;
using System.Runtime.InteropServices;
using Bridge;
using Bridge.Html5;

namespace BlockFactoryApp
{
	[ComVisible(true)]
	public class ScriptingHost
	{
		public dynamic window;
		public dynamic document;
		public dynamic navigator;
		public dynamic localStorage;
		public dynamic bridge;

		public bool DocumentCompleted { get; set; }

		public ScriptingHost()
		{
			DocumentCompleted = false;
		}

		public void Init()
		{
			Window.Instance = new WindowInstance(window);
			Document.Instance = new DocumentInstance(document);
			Navigator.Instance = new NavigatorInstance(navigator);
			Script.instance = bridge;
			DocumentCompleted = true;
		}

		public void confirmLeavePage()
		{
			BlockFactoryApp.BlockFactory.blocklyFactory.confirmLeavePage();
		}
	}
}
