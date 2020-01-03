using System;
using System.Runtime.InteropServices;

namespace mshtml
{
	[Guid("3051077D-98B5-11CF-BB82-00AA00BDCE0B")]
	[TypeLibType(4160)]
	public interface IDOMXmlSerializer
	{
		[DispId(1000)]
		string serializeToString(IHTMLDOMNode pNode);
	}


	[Guid("30510781-98B5-11CF-BB82-00AA00BDCE0B")]
	[TypeLibType(4160)]
	public interface IDOMParser
	{
		[DispId(1000)]
		IHTMLDocument2 parseFromString(string xmlSource, string mimeType);
	}

	[Guid("305106F8-98B5-11CF-BB82-00AA00BDCE0B")]
	[TypeLibType(4160)]
	public interface IHTMLElement6
	{
		[DispId(-2147416859)]
		object getAttributeNS(ref object pvarNS, string strAttributeName);
		[DispId(-2147416858)]
		void setAttributeNS(ref object pvarNS, string strAttributeName, ref object pvarAttributeValue);
		[DispId(-2147416857)]
		void removeAttributeNS(ref object pvarNS, string strAttributeName);
		[DispId(-2147416862)]
		IHTMLDOMAttribute2 getAttributeNodeNS(ref object pvarNS, string bstrName);
		[DispId(-2147416861)]
		IHTMLDOMAttribute2 setAttributeNodeNS(IHTMLDOMAttribute2 pattr);
		[DispId(-2147416860)]
		bool hasAttributeNS(ref object pvarNS, string name);
		[DispId(-2147416852)]
		object getAttribute(string strAttributeName);
		[DispId(-2147416851)]
		void setAttribute(string strAttributeName, ref object pvarAttributeValue);
		[DispId(-2147416850)]
		void removeAttribute(string strAttributeName);
		[DispId(-2147416856)]
		IHTMLDOMAttribute2 getAttributeNode(string strAttributeName);
		[DispId(-2147416855)]
		IHTMLDOMAttribute2 setAttributeNode(IHTMLDOMAttribute2 pattr);
		[DispId(-2147416854)]
		IHTMLDOMAttribute2 removeAttributeNode(IHTMLDOMAttribute2 pattr);
		[DispId(-2147416853)]
		bool hasAttribute(string name);
		[DispId(-2147416849)]
		IHTMLElementCollection getElementsByTagNameNS(ref object varNS, string bstrLocalName);
		[DispId(-2147416845)]
		IHTMLElementCollection getElementsByClassName(string v);
		[DispId(-2147416834)]
		bool msMatchesSelector(string v);
		[DispId(-2147416833)]
		bool hasAttributes();

		[DispId(-2147416847)]
		string tagName { get; }
		[DispId(-2147416846)]
		string nodeName { get; }
		[DispId(-2147412084)]
		object onabort { get; set; }
		[DispId(-2147411978)]
		object oncanplay { get; set; }
		[DispId(-2147411977)]
		object oncanplaythrough { get; set; }
		[DispId(-2147412082)]
		object onchange { get; set; }
		[DispId(-2147411976)]
		object ondurationchange { get; set; }
		[DispId(-2147411975)]
		object onemptied { get; set; }
		[DispId(-2147411974)]
		object onended { get; set; }
		[DispId(-2147412083)]
		object onerror { get; set; }
		[DispId(-2147411985)]
		object oninput { get; set; }
		[DispId(-2147412080)]
		object onload { get; set; }
		[DispId(-2147411973)]
		object onloadeddata { get; set; }
		[DispId(-2147411972)]
		object onloadedmetadata { get; set; }
		[DispId(-2147411971)]
		object onloadstart { get; set; }
		[DispId(-2147411970)]
		object onpause { get; set; }
		[DispId(-2147411969)]
		object onplay { get; set; }
		[DispId(-2147411968)]
		object onplaying { get; set; }
		[DispId(-2147411967)]
		object onprogress { get; set; }
		[DispId(-2147411966)]
		object onratechange { get; set; }
		[DispId(-2147412100)]
		object onreset { get; set; }
		[DispId(-2147411965)]
		object onseeked { get; set; }
		[DispId(-2147411964)]
		object onseeking { get; set; }
		[DispId(-2147412102)]
		object onselect { get; set; }
		[DispId(-2147411963)]
		object onstalled { get; set; }
		[DispId(-2147412101)]
		object onsubmit { get; set; }
		[DispId(-2147411962)]
		object onsuspend { get; set; }
		[DispId(-2147411961)]
		object ontimeupdate { get; set; }
		[DispId(-2147411960)]
		object onvolumechange { get; set; }
		[DispId(-2147411959)]
		object onwaiting { get; set; }
	}
}
