// Copyright 2008 The Closure Library Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/**
 * @fileoverview Renderer for {@link goog.ui.Menu}s.
 *
 * @author robbyw@google.com (Robby Walker)
 */
using System;
using Bridge.Html5;

namespace goog.ui
{
	public class MenuRenderer : ContainerRenderer
	{
		private static MenuRenderer instance_;

		internal static new MenuRenderer getInstance()
		{
			if (instance_ == null)
				instance_ = new MenuRenderer();
			return instance_;
		}

		/// <summary>
		/// Default CSS class to be applied to the root element of toolbars rendered
		/// by this renderer.
		/// </summary>
		public static readonly new string CSS_CLASS = le.getCssName("goog-menu");

		/// <summary>
		/// Returns the CSS class to be applied to the root element of containers
		/// rendered using this renderer.
		/// </summary>
		/// <returns>Renderer-specific CSS class.</returns>
		public override string getCssClass()
		{
			return goog.ui.MenuRenderer.CSS_CLASS;
		}

		internal bool containsElement(Menu menu, HTMLElement element)
		{
			throw new NotImplementedException();
		}
	}
}
