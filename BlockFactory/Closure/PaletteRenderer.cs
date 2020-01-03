// Copyright 2007 The Closure Library Authors. All Rights Reserved.
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
 * @fileoverview Renderer for {@link goog.ui.Palette}s.
 *
 * @author attila@google.com (Attila Bodis)
 */
using System;
using System.Collections.Generic;
using Bridge;
using Bridge.Html5;

namespace goog.ui
{
	public class PaletteRenderer : ControlRenderer
	{
		private static PaletteRenderer instance_;

		public static new PaletteRenderer getInstance()
		{
			if (instance_ == null)
				instance_ = new PaletteRenderer();
			return instance_;
		}

		/// <summary>
		/// Default renderer for {@link goog.ui.Palette}s.  Renders the palette as an
		/// HTML table wrapped in a DIV, with one palette item per cell:
		///
		///    <div class="goog-palette">
		///      <table class="goog-palette-table">
		///        <tbody class="goog-palette-body">
		///          <tr class="goog-palette-row">
		///            <td class="goog-palette-cell">...Item 0...</td>
		///            <td class="goog-palette-cell">...Item 1...</td>
		///            ...
		///          </tr>
		///          <tr class="goog-palette-row">
		///            ...
		///          </tr>
		///        </tbody>
		///      </table>
		///    </div>
		/// </summary>
		public PaletteRenderer()
		{
		}

		/// <summary>
		/// Globally unique ID sequence for cells rendered by this renderer class.
		/// </summary>
		private static int cellId_ = 0;


		/// <summary>
		/// Default CSS class to be applied to the root element of components rendered
		/// by this renderer.
		/// </summary>
		public static new string CSS_CLASS = le.getCssName("goog-palette");


		/// <summary>
		/// Returns the palette items arranged in a table wrapped in a DIV, with the
		/// renderer's own CSS class and additional state-specific classes applied to
		/// it.
		/// </summary>
		/// <param name="palette">goog.ui.Palette to render.</param>
		/// <returns>Root element for the palette.</returns>
		public override HTMLElement createDom(goog.ui.Control palette)
		{
			var classNames = this.getClassNames(palette);
			var element = palette.getDomHelper().createDom(
				goog.dom.TagName.DIV, classNames != null ? classNames.Join(" ") : null,
				this.createGrid(
					palette.getContent(), ((Palette)palette).getSize(),
					palette.getDomHelper()));
			goog.a11y.aria.setRole(element, goog.a11y.aria.Role.GRID);
			return element;
		}


		/// <summary>
		/// Returns the given items in a table with {@code size.width} columns and
		/// {@code size.height} rows.  If the table is too big, empty cells will be
		/// created as needed.  If the table is too small, the items that don't fit
		/// will not be rendered.
		/// </summary>
		/// <param name="items"> Palette items.</param>
		/// <param name="size"> Palette size (columns x rows); both dimensions
		/// must be specified as numbers.</param>
		/// <param name="dom"> DOM helper for document interaction.</param>
		/// <returns>Palette table element.</returns>
		public HTMLElement createGrid(ControlContent items, goog.math.Size size, goog.dom.DomHelper dom)
		{
			var rows = new JsArray<Node>();
			for (int row = 0, index = 0; row < size.height; row++) {
				var cells = new JsArray<Node>();
				for (var column = 0; column < size.width; column++) {
					var item = items != null ? items.AsArray()[index++] : null;
					cells.Push(this.createCell(item, dom));
				}
				rows.Push(this.createRow(cells, dom));
			}

			return this.createTable(rows, dom);
		}


		/// <summary>
		/// Returns a table element (or equivalent) that wraps the given rows.
		/// </summary>
		/// <param name="rows">Array of row elements.</param>
		/// <param name="dom">DOM helper for document interaction.</param>
		/// <returns>Palette table element.</returns>
		public HTMLElement createTable(JsArray<Node> rows, goog.dom.DomHelper dom)
		{
			var table = (HTMLTableElement)dom.createDom(
				goog.dom.TagName.TABLE, le.getCssName(this.getCssClass(), "table"),
				dom.createDom(
					goog.dom.TagName.TBODY, le.getCssName(this.getCssClass(), "body"),
					rows));
			table.CellSpacing = "0";
			table.CellPadding = "0";
			return table;
		}


		/// <summary>
		/// Returns a table row element (or equivalent) that wraps the given cells.
		/// </summary>
		/// <param name="cells">Array of cell elements.</param>
		/// <param name="dom">DOM helper for document interaction.</param>
		/// <returns>Row element.</returns>
		public HTMLElement createRow(JsArray<Node> cells, goog.dom.DomHelper dom)
		{
			var row = dom.createDom(
				goog.dom.TagName.TR, le.getCssName(this.getCssClass(), "row"), cells);
			goog.a11y.aria.setRole(row, goog.a11y.aria.Role.ROW);
			return row;
		}


		/// <summary>
		/// Returns a table cell element (or equivalent) that wraps the given palette
		/// item (which must be a DOM node).
		/// </summary>
		/// <param name="node">Palette item.</param>
		/// <param name="dom">DOM helper for document interaction.</param>
		/// <returns>Cell element.</returns>
		public HTMLElement createCell(Union<string, Node, NodeList, JsArray<Node>> node, goog.dom.DomHelper dom)
		{
			var cell = dom.createDom(
				goog.dom.TagName.TD, new Dictionary<string, string>{
				{ "class", le.getCssName(this.getCssClass(), "cell") },
				// Cells must have an ID, for accessibility, so we generate one here.
				{ "id", le.getCssName(this.getCssClass(), "cell-") +
			goog.ui.PaletteRenderer.cellId_++}
		  },
		  node);
			goog.a11y.aria.setRole(cell, goog.a11y.aria.Role.GRIDCELL);
			// Initialize to an unselected state.
			goog.a11y.aria.setState(cell, goog.a11y.aria.State.SELECTED, false);

			if (goog.dom.getTextContent(cell) == null && goog.a11y.aria.getLabel(cell) == null) {
				var ariaLabelForCell = this.findAriaLabelForCell_(cell);
				if (ariaLabelForCell != null) {
					goog.a11y.aria.setLabel(cell, ariaLabelForCell);
				}
			}
			return cell;
		}


		/// <summary>
		/// Descends the DOM and tries to find an aria label for a grid cell
		/// from the first child with a label or title.
		/// </summary>
		/// <param name="cell"> The cell.</param>
		/// <returns>The label to use.</returns>
		private string findAriaLabelForCell_(HTMLElement cell)
		{
			var iter = NodeIterator(cell);
			var label = "";
			foreach (var node in iter) {
				if (node.NodeType == NodeType.Element) {
					label =
						goog.a11y.aria.getLabel((HTMLElement)node) ?? ((HTMLElement)node).Title;
					break;
				}
			}
			return label;
		}

		private IEnumerable<Node> NodeIterator(Node node)
		{
			foreach (var subnode in node.ChildNodes) {
				yield return subnode;
				foreach (var subsubnode in NodeIterator(subnode)) {
					yield return subsubnode;
				}
			}
		}

		/// <summary>
		/// Overrides {@link goog.ui.ControlRenderer#canDecorate} to always return false.
		/// </summary>
		/// <param name="element"> Ignored.</param>
		/// <returns>False, since palettes don't support the decorate flow (for
		///    now).</returns>
		public override bool canDecorate(Element element)
		{
			return false;
		}


		/// <summary>
		/// Overrides {@link goog.ui.ControlRenderer#decorate} to be a no-op, since
		/// palettes don't support the decorate flow (for now).
		/// </summary>
		/// <param name="palette">Ignored.</param>
		/// <param name="element"> Ignored.</param>
		/// <returns>Always null.</returns>
		public override HTMLElement decorate(goog.ui.Control palette, HTMLElement element)
		{
			return null;
		}


		/// <summary>
		/// Overrides {@link goog.ui.ControlRenderer#setContent} for palettes.  Locates
		/// the HTML table representing the palette grid, and replaces the contents of
		/// each cell with a new element from the array of nodes passed as the second
		/// argument.  If the new content has too many items the table will have more
		/// rows added to fit, if there are less items than the table has cells, then the
		/// left over cells will be empty.
		/// </summary>
		/// <param name="element"> Root element of the palette control.</param>
		/// <param name="content">Array of items to replace existing
		/// palette items.</param>
		public override void setContent(HTMLElement element, goog.ui.ControlContent content)
		{
			var items = content.As<JsArray<Node>>();
			if (element != null) {
				var tbody = (HTMLTableSectionElement)goog.dom.getElementsByTagNameAndClass(
					goog.dom.TagName.TBODY, le.getCssName(this.getCssClass(), "body"),
					element)[0];
				if (tbody != null) {
					var index = 0;
					foreach (HTMLTableRowElement row in tbody.Rows) {
						foreach (var cell in row.Cells) {
							goog.dom.removeChildren(cell);
							if (items != null) {
								var item = items[index++];
								if (item != null) {
									goog.dom.appendChild(cell, item);
								}
							}
						}
					}

					// Make space for any additional items.
					if (index < items.Length) {
						var cells = new JsArray<Node>();
						var dom = goog.dom.getDomHelper(element);
						var width = ((HTMLTableRowElement)tbody.Rows[0]).Cells.Length;
						while (index < items.Length) {
							var item = items[index++];
							cells.Push(this.createCell(item, dom));
							if (cells.Length == width) {
								var row = this.createRow(cells, dom);
								goog.dom.appendChild(tbody, row);
								cells.Clear();
							}
						}
						if (cells.Length > 0) {
							while (cells.Length < width) {
								cells.Push(this.createCell("", dom));
							}
							var row = this.createRow(cells, dom);
							goog.dom.appendChild(tbody, row);
						}
					}
				}
				// Make sure the new contents are still unselectable.
				goog.style.setUnselectable(element, true, goog.userAgent.GECKO);
			}
		}


		/// <summary>
		/// Returns the item corresponding to the given node, or null if the node is
		/// neither a palette cell nor part of a palette item.
		/// </summary>
		/// <param name="palette">Palette in which to look for the item.</param>
		/// <param name="node"> Node to look for.</param>
		/// <returns>The corresponding palette item (null if not found).</returns>
		public Node getContainingItem(goog.ui.Palette palette, Node node)
		{
			var root = palette.getElement();
			while (node != null && node.NodeType == NodeType.Element && node != root) {
				if (((Element)node).TagName.ToLower() == goog.dom.TagName.TD) {
					if (goog.dom.classlist.contains((HTMLElement)node,
						le.getCssName(this.getCssClass(), "cell")))
						return node.FirstChild;
				}
				node = node.ParentNode;
			}

			return null;
		}


		/// <summary>
		/// Updates the highlight styling of the palette cell containing the given node
		/// based on the value of the Boolean argument.
		/// </summary>
		/// <param name="palette">Palette containing the item.</param>
		/// <param name="node">Item whose cell is to be highlighted or un-highlighted.</param>
		/// <param name="highlight">If true, the cell is highlighted; otherwise it is
		///    un-highlighted.</param>
		public void highlightCell(
			goog.ui.Palette palette, Node node, bool highlight)
		{
			if (node != null) {
				var cell = this.getCellForItem(node);
				goog.asserts.assert(cell != null);
				goog.dom.classlist.enable(
					cell, le.getCssName(this.getCssClass(), "cell-hover"), highlight);
				// See http://www.w3.org/TR/2006/WD-aria-state-20061220/#activedescendent
				// for an explanation of the activedescendent.
				if (highlight) {
					goog.a11y.aria.setState(
						palette.getElementStrict(), goog.a11y.aria.State.ACTIVEDESCENDANT,
						cell.Id);
				}
				else if (
					cell.Id ==
					goog.a11y.aria.getState(
						palette.getElementStrict(),
						goog.a11y.aria.State.ACTIVEDESCENDANT)) {
					goog.a11y.aria.removeState(
						palette.getElementStrict(), goog.a11y.aria.State.ACTIVEDESCENDANT);
				}
			}
		}


		/// <summary>
		/// </summary>
		/// <param name="node"> Item whose cell is to be returned.</param>
		/// <returns>The grid cell for the palette item.</returns>
		public HTMLElement getCellForItem(Node node)
		{
			return (HTMLElement)(node != null ? node.ParentNode : null);
		}


		/// <summary>
		/// Updates the selection styling of the palette cell containing the given node
		/// based on the value of the Boolean argument.
		/// </summary>
		/// <param name="palette">Palette containing the item.</param>
		/// <param name="node">Item whose cell is to be selected or deselected.</param>
		/// <param name="select">If true, the cell is selected; otherwise it is
		/// deselected.</param>
		public void selectCell(goog.ui.Palette palette, Node node, bool select)
		{
			if (node != null) {
				var cell = (HTMLElement)node.ParentNode;
				goog.dom.classlist.enable(
					cell, le.getCssName(this.getCssClass(), "cell-selected"), select);
				goog.a11y.aria.setState(cell, goog.a11y.aria.State.SELECTED, select);
			}
		}


		/// <summary>
		/// Returns the CSS class to be applied to the root element of components
		/// rendered using this renderer.
		/// </summary>
		/// <returns>Renderer-specific CSS class.</returns>
		public override string getCssClass()
		{
			return goog.ui.PaletteRenderer.CSS_CLASS;
		}
	}
}
