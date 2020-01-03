// https://bridge.net/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bridge.Html5;
using System.Web.Script.Serialization;

namespace Bridge
{
	public static class Script
	{
		public static object instance;
		public static DBNull Undefined = DBNull.Value;

		static System.Windows.Forms.HtmlDocument document;
		static System.Reflection.IReflect objectCreater;
		static System.Reflection.IReflect objectNewer;
		static System.Reflection.IReflect xmlSerializerNewer;
		static System.Reflection.IReflect domParserNewer;
		static System.Reflection.IReflect audioNewer;
		static System.Reflection.IReflect getter;
		static System.Reflection.IReflect setter;
		static System.Reflection.IReflect definedChecker;
		static System.Reflection.IReflect deleter;
		static System.Reflection.IReflect memberDeleter;
		static System.Reflection.IReflect enumerator;
		static System.Reflection.IReflect eventDispatcher;
		static System.Reflection.IReflect defaultPreventer;
		static System.Reflection.IReflect propagationStoper;
		static System.Reflection.IReflect uriEncoder;
		static System.Reflection.IReflect uriDecoder;
		static System.Reflection.IReflect jsonStringify;
		static System.Reflection.IReflect jsonParse;
		static System.Reflection.IReflect svgDomCreateer;
		static System.Reflection.IReflect prettyOnePrinter;
		static System.Reflection.IReflect fileReaderCresater;

		internal static void Init(System.Windows.Forms.HtmlDocument document)
		{
			Script.document = document;
			objectCreater = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "Object.create" });
			objectNewer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "Object" });
			xmlSerializerNewer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "XMLSerializer" });
			domParserNewer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "DOMParser" });
			audioNewer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "Audio" });
			getter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.Get" });
			setter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.Set" });
			definedChecker = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.IsDefined" });
			deleter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.Delete" });
			memberDeleter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.DeleteMember" });
			enumerator = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.ForEach" });
			eventDispatcher = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.DispatchEvent" });
			defaultPreventer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.PreventDefault" });
			propagationStoper = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.StopPropagation" });
			uriEncoder = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "encodeURI" });
			uriDecoder = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "decodeURI" });
			jsonStringify = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "JSON.stringify" });
			jsonParse = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "JSON.parse" });
			svgDomCreateer = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.CreateSvgDom" });
			prettyOnePrinter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.PrettyPrintOne" });
			fileReaderCresater = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.CresatFileReader" });
		}

		public static object InvokeMember(object scope, string name, params object[] args)
		{
			var reflect = scope as System.Reflection.IReflect;
			try {
				return reflect.InvokeMember(name,
					System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.OptionalParamBinding,
					null,
					scope,
					args,
					null,
					System.Globalization.CultureInfo.CurrentCulture,
					new string[0]);
			}
			catch (MissingMethodException) {
			}
			return null;
		}

		public static object CreateObject(object proto)
		{
			if (proto == null)
				return InvokeMember(objectCreater, "call", new object[] { objectCreater, DBNull.Value });
			else
				return InvokeMember(objectCreater, "call", new object[] { objectCreater, proto });
		}

		public static object NewObject()
		{
			return InvokeMember(objectNewer, "call", new object[] { objectNewer });
		}

		internal static string EncodeURIComponent(string content)
		{
			return (string)document.InvokeScript("encodeURIComponent", new object[] { content });
		}

		public static object NewXMLSerializer()
		{
			return InvokeMember(xmlSerializerNewer, "call", new object[] { xmlSerializerNewer });
		}

		public static object NewDOMParser()
		{
			return InvokeMember(domParserNewer, "call", new object[] { domParserNewer });
		}

		public static object NewAudio()
		{
			return InvokeMember(objectNewer, "call", new object[] { objectNewer });
		}

		public static object NewAudio(string filename)
		{
			return InvokeMember(objectNewer, "call", new object[] { objectNewer, filename });
		}

		public static object Get(object scope, string name)
		{
			return InvokeMember(getter, "call", new object[] { getter, scope, name });
		}

		public static object Get(object scope, int index)
		{
			return InvokeMember(getter, "call", new object[] { getter, scope, index });
		}

		public static T Get<T>(object scope, string name)
		{
			return (T)InvokeMember(getter, "call", new object[] { getter, scope, name });
		}

		public static void Set(object scope, string name, object value)
		{
			InvokeMember(setter, "call", new object[] { setter, scope, name, value });
		}

		public static void Set(object scope, int index, object value)
		{
			InvokeMember(setter, "call", new object[] { setter, scope, index, value });
		}

		public static bool IsDefined(object scope, string name)
		{
			return (bool)InvokeMember(definedChecker, "call", new object[] { definedChecker, scope, name });
		}

		static Random random = new Random();

		public static double Random()
		{
			return (double)random.NextDouble();
		}

		public static void Delete<T>(ref T obj)
		{
			if (obj is EventTarget eventTarget) {
				InvokeMember(deleter, "call", new object[] { deleter, eventTarget.Instance });
			}
			else if (Marshal.IsComObject(obj)) {
				InvokeMember(deleter, "call", new object[] { deleter, obj });
			}
			obj = default;
		}

		public static void DeleteMemebr(object obj, string name)
		{
			InvokeMember(memberDeleter, "call", new object[] { memberDeleter, obj, name });
		}

		public static bool DispatchEvent(object target, object ev)
		{
			var ret = InvokeMember(eventDispatcher, "call", new object[] { eventDispatcher, target, ev });
			if (ret is null)
				return false;
			if (ret is bool b)
				return b;
			return false;
		}

		public static void PreventDefault(object ev)
		{
			InvokeMember(defaultPreventer, "call", new object[] { defaultPreventer, ev });
		}

		public static void StopPropagation(object ev)
		{
			InvokeMember(propagationStoper, "call", new object[] { propagationStoper, ev });
		}

		public static bool IsFinite(double n)
		{
			return Double.MinValue <= n && Double.MaxValue >= n;
		}

		public static string EncodeURI(string url)
		{
			return (string)InvokeMember(uriEncoder, "call", new object[] { uriEncoder, url });
		}

		public static string DecodeURI(string url)
		{
			return (string)InvokeMember(uriDecoder, "call", new object[] { uriDecoder, url });
		}

		public static object CreateSvgDom(object container, object options)
		{
			return InvokeMember(svgDomCreateer, "call", new object[] { svgDomCreateer, container, options });
		}

		internal static void Eval(string code)
		{
			document.InvokeScript("eval", new object[] { code });
		}

		public static string JsonStringify(object value)
		{
			return (string)InvokeMember(jsonStringify, "call", new object[] { jsonStringify, value, });
		}

		public static string JsonStringify(object value, object[] replacer)
		{
			return (string)InvokeMember(jsonStringify, "call", new object[] { jsonStringify, value, replacer });
		}

		public static string JsonStringify(object value, object[] replacer, string space)
		{
			return (string)InvokeMember(jsonStringify, "call", new object[] { jsonStringify, value, replacer, space });
		}

		public static object JsonParse(string text)
		{
			return InvokeMember(jsonParse, "call", new object[] { jsonParse, text });
		}

		class Enumerator : HtmlToClrEventProxy
		{
			private Action<string, object> handler;

			public Enumerator(Action<string, object> handler)
			{
				this.handler = handler;
			}

			protected override object InvokeClrEvent(object[] eventArgs)
			{
				handler((string)eventArgs[0], eventArgs[1]);
				return null;
			}
		}

		public static void ForEach(object json, Action<string, object> p)
		{
			InvokeMember(enumerator, "call", new object[] { enumerator, new Enumerator(p) });
		}

		public static object Keys(object obj)
		{
			var keysGetter = (System.Reflection.IReflect)document.InvokeScript("eval", new object[] { "bridge.Keys" });
			return InvokeMember(enumerator, "call", new object[] { keysGetter, obj });
		}

		public static string PrettyPrintOne(string code, string lang)
		{
			return (string)InvokeMember(prettyOnePrinter, "call", new object[] { prettyOnePrinter, code, lang });
		}

		public static object CresateFileReader()
		{
			return InvokeMember(fileReaderCresater, "call", new object[] { fileReaderCresater });
		}

		public static double ParseFloat(string text)
		{
			if (String.IsNullOrEmpty(text))
				return Double.NaN;

			switch (text) {
			case "NaN":
				return Double.NaN;
			case "Infinity":
				return Double.PositiveInfinity;
			case "-Infinity":
				return Double.NegativeInfinity;
			default:
				return Double.TryParse(text, out var value) ? value : Double.NaN;
			}
		}
	}

	public class Date
	{
		DateTime dateTime;

		public Date()
		{
			dateTime = DateTime.Now;
		}

		public Date(string text)
		{
			dateTime = DateTime.Parse(text);
		}

		public Date(TimeSpan timeSpan)
		{
			dateTime = new DateTime(timeSpan.Ticks);
		}

		public string ToIsoString(bool nazo = false)
		{
			return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
		}

		public static double operator -(Date a, Date b)
		{
			return (a.dateTime - b.dateTime).TotalMilliseconds;
		}

		public long valueOf()
		{
			return dateTime.Ticks / TimeSpan.TicksPerMillisecond;
		}
	}

	public class Union<T1, T2>
	{
		object value;

		public Union(object value)
		{
			if (value is T1 || value is T2 || value is null) {
				this.value = value;
				return;
			}
			var type = value.GetType();
			if (type.IsSubclassOf(typeof(T1)) || type.IsSubclassOf(typeof(T2)) || value is null)
				this.value = value;
			else
				throw new ArgumentException();
		}

		public object Value {
			get { return value; }
		}

		public T As<T>() { if (value is T) return (T)value; else return default(T); }

		public bool Is<T>() { return value is T; }

		public static implicit operator Union<T1, T2>(T1 value)
		{
			return new Union<T1, T2>(value);
		}

		public static implicit operator Union<T1, T2>(T2 value)
		{
			return new Union<T1, T2>(value);
		}

		public static explicit operator T1(Union<T1, T2> any)
		{
			return any == null ? default(T1) : (T1)any.value;
		}

		public static explicit operator T2(Union<T1, T2> any)
		{
			return any == null ? default(T2) : (T2)any.value;
		}

		public override string ToString()
		{
			return value.ToString();
		}
	}

	public class Union<T1, T2, T3>
	{
		object value;

		public Union(object value)
		{
			if (value is T1 || value is T2 || value is T3 || value is null) {
				this.value = value;
				return;
			}
			var type = value.GetType();
			if (type.IsSubclassOf(typeof(T1)) || type.IsSubclassOf(typeof(T2)) || type.IsSubclassOf(typeof(T3)) || value is null)
				this.value = value;
			else
				throw new ArgumentException();
		}

		public object Value {
			get { return value; }
		}

		public T As<T>() { if (value is T) return (T)value; else return default(T); }

		public bool Is<T>() { return value is T; }

		public static implicit operator Union<T1, T2, T3>(T1 value)
		{
			return new Union<T1, T2, T3>(value);
		}

		public static implicit operator Union<T1, T2, T3>(T2 value)
		{
			return new Union<T1, T2, T3>(value);
		}

		public static implicit operator Union<T1, T2, T3>(T3 value)
		{
			return new Union<T1, T2, T3>(value);
		}

		public static explicit operator T1(Union<T1, T2, T3> any)
		{
			return (T1)any.value;
		}

		public static explicit operator T2(Union<T1, T2, T3> any)
		{
			return (T2)any.value;
		}

		public static explicit operator T3(Union<T1, T2, T3> any)
		{
			return (T3)any.value;
		}

		public override string ToString()
		{
			return value.ToString();
		}
	}

	public class Union<T1, T2, T3, T4>
	{
		object value;

		public Union(object value)
		{
			if (value is T1 || value is T2 || value is T3 || value is T4 || value is null) {
				this.value = value;
				return;
			}
			var type = value.GetType();
			if (type.IsSubclassOf(typeof(T1)) || type.IsSubclassOf(typeof(T2)) || type.IsSubclassOf(typeof(T3)) || type.IsSubclassOf(typeof(T4)) || value is null)
				this.value = value;
			else
				throw new ArgumentException();
		}

		public object Value {
			get { return value; }
		}

		public T As<T>() { if (value is T) return (T)value; else return default(T); }

		public bool Is<T>() { return value is T; }

		public static implicit operator Union<T1, T2, T3, T4>(T1 value)
		{
			return new Union<T1, T2, T3, T4>(value);
		}

		public static implicit operator Union<T1, T2, T3, T4>(T2 value)
		{
			return new Union<T1, T2, T3, T4>(value);
		}

		public static implicit operator Union<T1, T2, T3, T4>(T3 value)
		{
			return new Union<T1, T2, T3, T4>(value);
		}

		public static implicit operator Union<T1, T2, T3, T4>(T4 value)
		{
			return new Union<T1, T2, T3, T4>(value);
		}

		public static explicit operator T1(Union<T1, T2, T3, T4> any)
		{
			return (T1)any.value;
		}

		public static explicit operator T2(Union<T1, T2, T3, T4> any)
		{
			return (T2)any.value;
		}

		public static explicit operator T3(Union<T1, T2, T3, T4> any)
		{
			return (T3)any.value;
		}

		public static explicit operator T4(Union<T1, T2, T3, T4> any)
		{
			return (T4)any.value;
		}

		public override string ToString()
		{
			return value.ToString();
		}
	}

	public static class StringExtention
	{
		public static string Replace(this string str, Regex regex, string dst)
		{
			return regex.Replace(str, dst);
		}

		public static string Replace(this string str, Regex regex, Func<string, string, string> func)
		{
			var result = new System.Text.StringBuilder();
			var ms = regex.Matches(str);
			int i = 0;
			foreach (Match m in ms) {
				result.Append(str.Substring(i, m.Index));
				result.Append(func(m.Groups[0].Value, m.Groups[1].Value));
				i = m.Index + m.Length;
			}
			if (i < str.Length)
				result.Append(str.Substring(i, str.Length - i));
			return result.ToString();
		}

		public static string Replace(this string str, Regex regex, Func<string, string, string, string> func)
		{
			var result = new System.Text.StringBuilder();
			var ms = regex.Matches(str);
			int i = 0;
			foreach (Match m in ms) {
				result.Append(str.Substring(i, m.Index));
				result.Append(func(m.Groups[0].Value, m.Groups[1].Value, m.Groups[2].Value));
				i = m.Index + m.Length;
			}
			if (i < str.Length)
				result.Append(str.Substring(i, str.Length - i));
			return result.ToString();
		}

		public static JsArray<string> Split(this string str, Regex regex)
		{
			var result = new JsArray<string>();
			var ms = regex.Matches(str);
			int i = 0;
			foreach (Match m in ms) {
				result.Push(str.Substring(i, m.Index - i));
				i = m.Index + m.Length;
			}
			if (i < str.Length)
				result.Push(str.Substring(i, str.Length - i));
			return result;
		}

		public static string[] Match(this string str, Regex regex)
		{
			var result = new JsArray<string>();
			if ((regex.Options & RegexOptions.Multiline) != 0) {
				var ms = regex.Matches(str);
				foreach (Match m in ms) {
					if (m.Success)
						result.Push(m.Value);
					else
						result.Push(null);
				}
			}
			else {
				var m = regex.Match(str);
				if (!m.Success)
					return null;
				foreach (Group g in m.Groups) {
					if (g.Success)
						result.Push(g.Value);
					else
						result.Push(null);
				}
			}
			return result.ToArray();
		}

		public static string Join(this string[] str, string sep)
		{
			return String.Join(sep, str);
		}

		public static string CharAt(this string str, int index)
		{
			if (index >= 0)
				return str[index].ToString();
			else
				return str[str.Length + index].ToString();
		}

		public static string[] Split(this string str, string sep)
		{
			return str.Split(new string[] { sep }, StringSplitOptions.None);
		}

		public static string Slice(this string str, int start)
		{
			if (start >= 0)
				return str.Substring(start, str.Length - start);
			else
				return str.Substring(str.Length + start, -start);
		}

		public static string Slice(this string str, int start, int end)
		{
			if (start < 0) {
				start = str.Length + start;
			}
			if (end < 0) {
				end = str.Length + end;
			}

			if (start > end)
				return "";

			return str.Substring(start, end - start);
		}

		public static string Substr(this string str, int start)
		{
			if (start < 0)
				start = str.Length + start;
			return str.Substring(start);
		}

		public static string Substr(this string str, int start, int length)
		{
			return str.Substring(start, length);
		}

		public static int LocaleCompare(this string str, string b)
		{
			return str.CompareTo(b);
		}

		public static string ToLowerCase(this string str)
		{
			return str.ToLower();
		}

		public static string ToUpperCase(this string str)
		{
			return str.ToUpper();
		}
	}

	public static class RegexExtention
	{
		public static bool Test(this Regex regex, string str)
		{
			return regex.IsMatch(str);
		}

		public static string[] Exec(this Regex regex, string str)
		{
			var result = new JsArray<string>();
			var m = regex.Match(str);
			if (!m.Success)
				return null;
			foreach (Group g in m.Groups) {
				if (g.Success)
					result.Push(g.Value);
				else
					result.Push(null);
			}
			return result.ToArray();
		}
	}

	public static class StringListExtention
	{
		public static string Join(this IEnumerable<string> list, string separator)
		{
			return String.Join(separator, list);
		}
	}

	public class JsArray<T> : List<T>
	{
		public JsArray()
		{
		}

		public JsArray(int length)
			: base(length)
		{
			for (int i = 0; i < length; i++)
				base.Add(default);
		}

		public JsArray(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public int Length {
			get { return Count; }
		}

		public new T this[int index] {
			get { return base[index]; }
			set { base[index] = value; }
		}

		public void Push(T item, params T[] items)
		{
			Add(item);
			AddRange(items);
		}

		public void PushRange(IEnumerable<T> items)
		{
			AddRange(items);
		}

		public T Pop()
		{
			if (Count == 0)
				return default;

			var result = base[Count - 1];
			RemoveAt(Count - 1);
			return result;
		}

		public JsArray<T> Concat(IEnumerable<T> items)
		{
			var result = new JsArray<T>(this);
			result.AddRange(items);
			return result;
		}

		public JsArray<T> Splice(int start, int len, params T[] items)
		{
			var result = new JsArray<T>();

			if (start == 0 && len == Count) {
				result.PushRange(this);
				Clear();
			}
			else {
				for (int i = 0; i < len; i++) {
					result.Add(base[start]);
					RemoveAt(start);
				}
			}

			for (int i = 0; i < items.Length; i++) {
				Insert(start + i, items[i]);
			}

			return result;
		}

		public static implicit operator T[](JsArray<T> target)
		{
			return target.ToArray();
		}

		public JsArray<T> Clone()
		{
			var clone = new JsArray<T>();
			clone.AddRange(this);
			return clone;
		}

		public T Shift()
		{
			if (Count == 0)
				return default;

			var result = base[0];
			RemoveAt(0);
			return result;
		}

		public int Unshift(params T[] ts)
		{
			AddRange(ts);
			return Count;
		}

		public JsArray<T> Slice(int start)
		{
			int end;
			if (start >= 0)
				end = Count - start;
			else {
				start = Count + start;
				end = -start;
			}
			var result = new JsArray<T>();
			for (int i = start; i < end; i++) result.Push(base[i]);
			return result;
		}

		public JsArray<T> Slice(int start, int end)
		{
			if (start < 0) {
				start = Count + start;
			}
			if (end < 0) {
				end = Count + end;
			}

			if (start > end)
				return new JsArray<T>();
			var result = new JsArray<T>();
			for (int i = start; i < end; i++) result.Push(base[i]);
			return result;
		}

		public JsArray<RT> Map<RT>(Func<T, RT> p)
		{
			var result = new JsArray<RT>();
			foreach (var i in this) result.Push(p(i));
			return result;
		}
	}

	public static class JsArrayExtention
	{
		public static void Concat<T>(this List<T> first, IEnumerable<T> second)
		{
			first.AddRange(second);
		}

		public static string Join<T>(this List<T> list, string separator)
		{
			return String.Join(separator, list);
		}
	}

	public class JSON
	{
		public static string Stringify(string value)
		{
			return Script.JsonStringify(value);
		}

		public static string Stringify(string[] value)
		{
			return Script.JsonStringify(value);
		}

		public static string Stringify(Dictionary<string, object> value)
		{
			var s = new JavaScriptSerializer();
			return s.Serialize(value);
		}

		public static string Stringify(Dictionary<string, object> value, object[] replacer)
		{
			var s = new JavaScriptSerializer();
			return Script.JsonStringify(Script.JsonParse(s.Serialize(value)), replacer);
		}

		public static string Stringify(Dictionary<string, object> value, object[] replacer, string space)
		{
			var s = new JavaScriptSerializer();
			return Script.JsonStringify(Script.JsonParse(s.Serialize(value)), replacer, space);
		}

		public static object Parse(string text)
		{
			if (String.IsNullOrEmpty(text))
				return new Dictionary<string, object>();

			var s = new JavaScriptSerializer();
			return s.Deserialize<object>(text);
		}
	}

	public class XMLSerializer
	{
		internal mshtml.IDOMXmlSerializer instance;

		public XMLSerializer()
		{
			instance = (mshtml.IDOMXmlSerializer)Script.NewXMLSerializer();
		}

		public string SerializeToString(Node dom)
		{
			return instance.serializeToString((mshtml.IHTMLDOMNode)dom.Instance);
		}
	}

	public class DOMParser
	{
		internal mshtml.IDOMParser instance;

		public DOMParser()
		{
			instance = (mshtml.IDOMParser)Script.NewDOMParser();
		}

		public Node ParseFromString(string text, string mimeType)
		{
			return Node.Create(instance.parseFromString(text, mimeType));
		}
	}
}

namespace Bridge.Html5
{
	public static class Window
	{
		public static WindowInstance Instance { get; internal set; }
		public static DocumentInstance Document {
			get { return Bridge.Html5.Document.Instance; }
		}
		public static NavigatorInstance Navigator {
			get { return Bridge.Html5.Navigator.Instance; }
		}
		public static URL URL {
			get { return Instance.URL; }
		}
		public static Location Location {
			get { return Instance.Location; }
		}
		public static Storage LocalStorage {
			get { return Instance.LocalStorage; }
		}
		public static double ScrollX {
			get { return Instance.ScrollX; }
		}
		public static double ScrollY {
			get { return Instance.ScrollY; }
		}
		public static int PageXOffset {
			get { return Instance.PageXOffset; }
		}
		public static int PageYOffset {
			get { return Instance.PageYOffset; }
		}

		public static int SetTimeout(Action handler, int delay)
		{
			return Instance.SetTimeout(handler, delay);
		}

		public static void ClearTimeout(int id)
		{
			Instance.ClearTimeout(id);
		}

		public static CSSStyleDeclaration GetComputedStyle(HTMLElement el, string pseudoElt)
		{
			return Instance.GetComputedStyle(el, pseudoElt);
		}

		public static bool Confirm(string message)
		{
			return Instance.Confirm(message);
		}

		public static void Alert(string message)
		{
			Instance.Alert(message);
		}

		public static string Prompt(string title, string message)
		{
			return Instance.Prompt(title, message);
		}

		public static void Open(string url)
		{
			Instance.Open(url);
		}

		public static void Open(string url, string windowName)
		{
			Instance.Open(url, windowName);
		}

		public static void DispatchEvent(Event @event)
		{
			Instance.DispatchEvent(@event);
		}

		internal static void AddEventListener(string type, Delegate listener, bool useCapture = false)
		{
			Instance.AddEventListener(type, listener, useCapture);
		}

		internal static HTMLAudioElement NewAudio(string filename = null)
		{
			return Instance.NewAudio(filename);
		}
	}

	public class WindowInstance
	{
		internal object instance;
		List<EventListener> listeners = new List<EventListener>();
		Storage localStorage;

		public DocumentInstance Document {
			get { return Bridge.Html5.Document.Instance; }
		}
		public NavigatorInstance Navigator {
			get { return Bridge.Html5.Navigator.Instance; }
		}
		public URL URL {
			get { var result = Script.Get(instance, "URL"); if (result is DBNull || result == null) return null; return new URL(result); }
		}
		public Storage LocalStorage {
			get { if (localStorage == null) localStorage = new Storage(this); return localStorage; }
		}
		public double ScrollX {
			get { var result = Script.Get(instance, "scrollX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public double ScrollY {
			get { var result = Script.Get(instance, "scrollY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public int PageXOffset {
			get { var result = Script.Get(instance, "pageXOffset"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}
		public int PageYOffset {
			get { var result = Script.Get(instance, "pageYOffset"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}
		public Location Location {
			get { var result = Script.Get(instance, "location"); if (result is DBNull || result == null) return null; return new Location(result); }
		}

		public WindowInstance(object instance)
		{
			this.instance = instance;
		}

		class Timeout : HtmlToClrEventProxy
		{
			private Action handler;

			public Timeout(Action handler)
			{
				this.handler = handler;
			}

			protected override object InvokeClrEvent(object[] eventArgs)
			{
				handler();
				return null;
			}
		}

		public int SetTimeout(Action handler, int delay)
		{
			return ((mshtml.IHTMLWindow3)instance).setTimeout(new Timeout(handler), delay);
		}

		public void ClearTimeout(int id)
		{
			((mshtml.IHTMLWindow2)instance).clearTimeout(id);
		}

		public CSSStyleDeclaration GetComputedStyle(HTMLElement el, string pseudoElt)
		{
			return CSSStyleDeclaration.Create(Script.InvokeMember(instance, "getComputedStyle", el.Instance, pseudoElt));
		}

		public bool Confirm(string message)
		{
			return (bool)Script.InvokeMember(instance, "confirm", message);
		}

		public void Alert(string message)
		{
			Script.InvokeMember(instance, "alert", message);
		}

		public string Prompt(string title, string message)
		{
			return (string)Script.InvokeMember(instance, "prompt", title, message);
		}

		public void Open(string url)
		{
			Script.InvokeMember(instance, "open", url);
		}

		public void Open(string url, string windowName)
		{
			Script.InvokeMember(instance, "open", url, windowName);
		}

		public void AddEventListener(string type, Delegate handler, bool useCapture = false)
		{
			var listener = new EventListener(type, handler, useCapture);
			listeners.Add(listener);
			Script.InvokeMember(instance, "addEventListener", type, listener, useCapture);
		}

		public void RemoveEventListener(string type, Delegate handler, bool useCapture = false)
		{
			var finder = new FindLisner(type, handler, useCapture);
			var listener = listeners.FirstOrDefault(new Func<EventListener, bool>(finder.Find));
			if (listener != null) {
				listeners.Remove(listener);
				Script.InvokeMember(instance, "removeEventListener", type, listener, useCapture);
			}
			else
				throw new ArgumentException();
		}

		class FindLisner
		{
			private string type;
			private Delegate handler;
			private bool useCapture;

			public FindLisner(string type, Delegate handler, bool useCapture)
			{
				this.type = type;
				this.handler = handler;
				this.useCapture = useCapture;
			}

			public bool Find(EventListener l)
			{
				return l.Type == type && l.Handler == handler;
			}
		}

		public bool DispatchEvent(Event ev)
		{
			return Script.DispatchEvent(instance, ev.instance);
		}

		public HTMLAudioElement NewAudio(string filename = null)
		{
			if (filename == null)
				return new HTMLAudioElement(Script.NewAudio());
			else
				return new HTMLAudioElement(Script.NewAudio(filename));
		}
	}

	public enum CompatMode
	{
		BackCompat,
		CSS1Compat
	}

	public static class Document
	{
		public static DocumentInstance Instance { get; internal set; }
		public static HTMLBodyElement Body {
			get { return Instance.Body; }
		}
		public static WindowInstance DefaultView {
			get { return Instance.DefaultView; }
		}
		public static CompatMode CompatMode {
			get { return Instance.CompatMode; }
		}
		public static WindowInstance ParentWindow {
			get { return Instance.ParentWindow; }
		}
		public static HTMLElement ScrollingElement {
			get { return Instance.ScrollingElement; }
		}
		public static int? DocumentMode {
			get { return Instance.DocumentMode; }
		}
		public static DOMImplementation Implementation {
			get { return Instance.Implementation; }
		}
		public static HTMLHeadElement Head {
			get { return Instance.Head; }
		}
		public static HTMLElement ActiveElement {
			get { return Instance.ActiveElement; }
		}

		public static T CreateElement<T>(string tagName) where T : Element
		{
			return Instance.CreateElement<T>(tagName);
		}

		public static T CreateElementNS<T>(string ns, string tagname) where T : Element
		{
			return Instance.CreateElementNS<T>(ns, tagname);
		}

		public static Text CreateTextNode(string text)
		{
			return Instance.CreateTextNode(text);
		}

		public static void ExecCommand(string commandName, bool showDefaultUI, bool valueArgument)
		{
			Instance.ExecCommand(commandName, showDefaultUI, valueArgument);
		}

		public static object CreateEvent(string type)
		{
			return Instance.CreateEvent(type);
		}

		public static object CreateEvent(string type, Dictionary<string, object> dictionary)
		{
			return Instance.CreateEvent(type, dictionary);
		}

		public static NodeList GetElementsByClassName(string className)
		{
			return Instance.GetElementsByClassName(className);
		}

		public static Element GetElementById(string id)
		{
			return Instance.GetElementById(id);
		}

		public static Element QuerySelector(string query)
		{
			return Instance.QuerySelector(query);
		}

		public static void AddEventListener(string type, Delegate handler, bool useCapture)
		{
			Instance.AddEventListener(type, handler, useCapture);
		}

		public static void RemoveEventListener(string type, Delegate handler, bool useCapture)
		{
			Instance.RemoveEventListener(type, handler, useCapture);
		}
	}

	public class DocumentInstance : Node
	{
		public HTMLBodyElement Body {
			get { return new HTMLBodyElement(((mshtml.HTMLDocument)instance).body); }
		}
		public HTMLHeadElement Head {
			get { return new HTMLHeadElement(Script.Get(instance, "head")); }
		}
		public WindowInstance DefaultView {
			get { return new WindowInstance(Script.Get(instance, "defaultView")); }
		}
		public HTMLElement DocumentElement {
			get { return new HTMLElement(((mshtml.HTMLDocument)instance).documentElement); }
		}
		public WindowInstance ParentWindow {
			get { return new WindowInstance(((mshtml.HTMLDocument)instance).parentWindow); }
		}
		public HTMLElement ScrollingElement {
			get { return HTMLElement.Create(Script.Get(instance, "scrollingElement")); }
		}
		public int? DocumentMode {
			get {
				var ret = Script.Get(instance, "documentMode");
				if (ret is DBNull || ret == null)
					return null;
				return Convert.ToInt32(ret);
			}
		}

		public CompatMode CompatMode {
			get {
				switch (((mshtml.HTMLDocument)instance).compatMode.ToLower()) {
				case "backcompat": return CompatMode.BackCompat;
				case "css1compat": return CompatMode.CSS1Compat;
				default: throw new Exception();
				}
			}
		}

		public DOMImplementation Implementation {
			get { return new DOMImplementation(Script.Get(instance, "implementation")); }
		}

		public HTMLElement ActiveElement {
			get { return HTMLElement.Create(Script.Get(instance, "activeElement")); }
		}

		public DocumentInstance(object instance)
			: base(instance)
		{
		}

		public T CreateElement<T>(string tagName) where T : Element
		{
			return (T)Element.Create(((mshtml.HTMLDocument)instance).createElement(tagName));
		}

		public T CreateElementNS<T>(string ns, string tagName) where T : Element
		{
			return (T)Element.Create(Script.InvokeMember(instance, "createElementNS", ns, tagName));
		}

		public Text CreateTextNode(string text)
		{
			return new Text(((mshtml.HTMLDocument)instance).createTextNode(text));
		}

		public void ExecCommand(string commandName, bool showDefaultUI, bool valueArgument)
		{
			((mshtml.HTMLDocument)instance).execCommand(commandName, showDefaultUI, valueArgument);
		}

		public DocumentFragment CreateDocumentFragment()
		{
			return new DocumentFragment(((mshtml.HTMLDocument)instance).createDocumentFragment());
		}

		public Element GetElementById(string id)
		{
			return Element.Create(((mshtml.HTMLDocument)instance).getElementById(id));
		}

		public NodeList GetElementsByClassName(string className)
		{
			return new NodeList(Script.InvokeMember(instance, "getElementsByClassName", className));
		}

		public NodeList GetElementsByTagName(string tagName)
		{
			return new NodeList(((mshtml.HTMLDocument)instance).getElementsByTagName(tagName));
		}

		public object CreateEvent(string type)
		{
			// "UIEvents", "MouseEvents", "MutationEvents", "HTMLEvents"
			//var ev = Script.InvokeMember(instance, "createEvent","HTMLEvents", type);
			var ev = Script.NewObject();
			Script.Set(ev, "interface", "HTMLEvents");
			Script.Set(ev, "type", type);
			return ev;
		}

		internal object CreateEvent(string type, Dictionary<string, object> dictionary)
		{
			var ev = Script.NewObject();
			Script.Set(ev, "interface", "HTMLEvents");
			Script.Set(ev, "type", type);

			foreach (var kvp in dictionary) {
				Script.Set(ev, kvp.Key, kvp.Value);
			}

			return ev;
		}

		public Element QuerySelector(string query)
		{
			return Element.Create(Script.InvokeMember(instance, "querySelector", query));
		}

		public NodeList QuerySelectorAll(string query)
		{
			return new NodeList(Script.InvokeMember(instance, "querySelectorAll", query));
		}

		public XPathNSResolver CreateNSResolver(object documentElement)
		{
			return XPathNSResolver.Create(Script.InvokeMember(instance, "createNSResolver", documentElement));
		}

		public XPathResult Evaluate(string xpathExpression, Node contextNode, object namespaceResolver, object resultType, object result)
		{
			return XPathResult.Create(Script.InvokeMember(instance, "evaluate", xpathExpression, contextNode, namespaceResolver, resultType, result));
		}
	}

	public class DocumentFragment : Node
	{
		public DocumentFragment(object instance)
			: base(instance)
		{
		}
	}

	public class DOMImplementation
	{
		private object instance;

		public DOMImplementation(object instance)
		{
			this.instance = instance;
		}

		public bool HasFeature(string feature, string version)
		{
			return (bool)Script.InvokeMember(instance, "hasFeature", feature, version);
		}
	}

	public class XPathResult
	{
		public const int ORDERED_NODE_SNAPSHOT_TYPE = 7;
		private object instance;

		public XPathResult(object instance)
		{
			this.instance = instance;
		}

		public int SnapshotLength { get; internal set; }

		internal static XPathResult Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new XPathResult(instance);
		}

		public Node SnapshotItem(int i)
		{
			return Node.Create(Script.InvokeMember(instance, "snapshotItem", i));
		}
	}

	public class XPathNSResolver
	{
		private object instance;

		public XPathNSResolver(object instance)
		{
			this.instance = instance;
		}

		public static XPathNSResolver Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new XPathNSResolver(instance);
		}
	}

	public static class Navigator
	{
		public static NavigatorInstance Instance { get; internal set; }

		public static string UserAgent {
			get { return Instance.UserAgent; }
		}
		public static string Platform {
			get { return Instance.Platform; }
		}
		public static string AppVersion {
			get { return Instance.AppVersion; }
		}
	}

	public class NavigatorInstance
	{
		internal mshtml.HTMLNavigator instance;

		public NavigatorInstance(object instance)
		{
			this.instance = (mshtml.HTMLNavigator)instance;
		}

		public string UserAgent {
			get { return instance.userAgent; }
		}
		public string Platform {
			get { return instance.platform; }
		}
		public string AppVersion {
			get { return instance.appVersion; }
		}
	}

	public class URL
	{
		public object instance;

		public URL(object instance)
		{
			this.instance = instance;
		}

		public string CreateObjectURL(Blob data)
		{
			return (string)Script.InvokeMember(instance, "createObjectURL", data);
		}
	}

	public class Location
	{
		private object instance;

		public string Hash {
			get { return (string)Script.Get(instance, "hash"); }
			set { Script.Set(instance, "hash", value); }
		}

		public string Href {
			get { return (string)Script.Get(instance, "href"); }
		}

		public Location(object instance)
		{
			this.instance = instance;
		}
	}

	public class Storage
	{
		private WindowInstance window;
		Dictionary<string, object> keyValues = new Dictionary<string, object>();

		public Storage(WindowInstance window)
		{
			this.window = window;
		}

		public object this[string name] {
			get { return keyValues.TryGetValue(name, out var value) ? value : null; }
			set { keyValues[name] = value; }
		}

		internal void SetItem(string url, string v)
		{
			throw new NotImplementedException();
		}
	}

	public class XMLHttpRequest
	{
		Dictionary<string, object> keyValues = new Dictionary<string, object>();
		public string Name { get; internal set; }
		public Action OnReadyStateChange { get; internal set; }
		public int ReadyState { get; internal set; }
		public int Status { get; internal set; }
		public string ResponseText { get; internal set; }

		public object this[string name] {
			get { return keyValues.TryGetValue(name, out var value) ? value : null; }
			set { keyValues[name] = value; }
		}

		internal void Abort()
		{
			throw new NotImplementedException();
		}

		internal void Open(string v, string url)
		{
			throw new NotImplementedException();
		}

		internal void SetRequestHeader(string v1, string v2)
		{
			throw new NotImplementedException();
		}

		internal void Send(string v)
		{
			throw new NotImplementedException();
		}
	}

	public class FileReader : EventTarget
	{
		public const ushort EMPTY = 0;
		public const ushort LOADING = 1;
		public const ushort DONE = 2;

		public string Result { get; internal set; }
		public Action<Event> OnLoad { get; internal set; }

		public FileReader(object instance)
			: base(instance)
		{
		}

		public FileReader()
			: base(Script.CresateFileReader())
		{
		}

		public void ReadAsText(Blob blob, string label = null)
		{
			Script.InvokeMember(instance, "readAsText", blob.instance, label);
		}
	}

	public class ClientRect
	{
		public ClientRect(mshtml.IHTMLRect instance)
		{
			Left = instance.left;
			Top = instance.top;
			Right = instance.right;
			Bottom = instance.bottom;
		}

		public double Left { get; }
		public double Top { get; }
		public double Right { get; }
		public double Bottom { get; }
		public double Width {
			get { return Right - Left; }
		}
		public double Height {
			get { return Bottom - Top; }
		}
	}

	public class EventListener : HtmlToClrEventProxy
	{
		public string Type { get; }
		public Delegate Handler { get; }
		public bool UseCapture { get; }

		System.Reflection.ConstructorInfo eventConstructor;

		public EventListener(string type, Delegate handler, bool useCapture)
			: base()
		{
			Type = type;
			Handler = handler;
			UseCapture = useCapture;

			var prms = handler.Method.GetParameters();
			if (prms.Length == 1) {
				var evType = prms[0].ParameterType;
				if (evType != typeof(Event)
					&& !evType.IsSubclassOf(typeof(Event)))
					throw new ArgumentException();
				eventConstructor = evType.GetConstructor(new Type[] { typeof(object) });
			}
			else if (prms.Length == 0) {
				eventConstructor = null;
			}
			else {
				throw new ArgumentException();
			}
		}

		protected override object InvokeClrEvent(object[] eventArgs)
		{
			if (eventConstructor != null) {
				var ev = eventConstructor.Invoke(new object[] { eventArgs[1] });
				return Handler.DynamicInvoke(ev);
			}
			else {
				return Handler.DynamicInvoke();
			}
		}
	}

	public class EventTarget : IDisposable
	{
		protected object instance;
		List<EventListener> listeners = new List<EventListener>();
		Dictionary<string, object> kvp = new Dictionary<string, object>();

		internal object Instance { get => instance; }

		public object this[string name] {
			get { if (kvp.TryGetValue(name, out var result)) return result; return null; }
			set { kvp[name] = value; }
		}

		public string Type {
			get {
				var result = Script.Get(instance, "type");
				if (result is DBNull || result == null) return null;
				return (string)result;
			}
		}

		public EventTarget(object instance)
		{
			this.instance = instance;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~EventTarget()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;
			while (listeners.Count > 0) {
				var listener = listeners[0];
				listeners.Remove(listener);
				Script.InvokeMember(instance, "removeEventListener", listener.Type, listener, listener.UseCapture);
			}
		}

		public bool IsDefined(string name)
		{
			return Script.IsDefined(instance, name);
		}

		public void AddEventListener(string type, Delegate handler, bool useCapture = false)
		{
			var listener = new EventListener(type, handler, useCapture);
			listeners.Add(listener);
			Script.InvokeMember(instance, "addEventListener", type, listener, useCapture);
		}

		public void RemoveEventListener(string type, Delegate handler, bool useCapture = false)
		{
			var finder = new FindLisner(type, handler, useCapture);
			var listener = listeners.FirstOrDefault(new Func<EventListener, bool>(finder.Find));
			if (listener != null) {
				listeners.Remove(listener);
				Script.InvokeMember(instance, "removeEventListener", type, listener, useCapture);
			}
			else
				throw new ArgumentException();
		}

		class FindLisner
		{
			private string type;
			private Delegate handler;
			private bool useCapture;

			public FindLisner(string type, Delegate handler, bool useCapture)
			{
				this.type = type;
				this.handler = handler;
				this.useCapture = useCapture;
			}

			public bool Find(EventListener l)
			{
				return l.Type == type && l.Handler == handler;
			}
		}

		public bool DispatchEvent(Event ev)
		{
			return Script.DispatchEvent(instance, ev.instance);
		}

		public static EventTarget Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;

			if (Script.IsDefined(instance, "nodeType"))
				return Node.Create(instance);

			return new EventTarget(instance);
		}

		public override bool Equals(object obj)
		{
			return obj is global::Bridge.Html5.EventTarget target &&
				   global::System.Collections.Generic.EqualityComparer<object>.Default.Equals(this.instance, target.instance);
		}

		public override int GetHashCode()
		{
			return -162508190 + EqualityComparer<object>.Default.GetHashCode(instance);
		}

		public static bool operator ==(EventTarget a, EventTarget b)
		{
			if ((a is null) || (b is null))
				return (object)a == (object)b;
			return (object)a.instance == (object)b.instance;
		}

		public static bool operator !=(EventTarget a, EventTarget b)
		{
			if ((a is null) || (b is null))
				return (object)a != (object)b;
			return (object)a.instance != (object)b.instance;
		}
	}

	public class Event
	{
		internal object instance;

		public string Identifier {
			get { var result = Script.Get(instance, "identifier"); if (result is DBNull) return null; return (string)result; }
		}
		public string Type {
			get { var result = Script.Get(instance, "type"); if (result is DBNull) return null; return (string)result; }
			set { Script.Set(instance, "type", value); }
		}
		public EventTarget Target {
			get { var result = Script.Get(instance, "target"); if (result is DBNull) return null; return EventTarget.Create(result); }
			set { Script.Set(instance, "target", value.Instance); }
		}
		public EventTarget CurrentTarget {
			get { return EventTarget.Create(Script.Get(instance, "currentTarget")); }
			set { Script.Set(instance, "currentTarget", value.Instance); }
		}
		public EventTarget RelatedTarget {
			get { return EventTarget.Create(Script.Get(instance, "relatedTarget")); }
			set { Script.Set(instance, "relatedTarget", value.Instance); }
		}
		public bool Repeat {
			get { var result = Script.Get(instance, "repeat"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "repeat", value); }
		}
		public bool AltKey {
			get { var result = Script.Get(instance, "altKey"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "altKey", value); }
		}
		public bool CtrlKey {
			get { var result = Script.Get(instance, "ctrlKey"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "ctrlKey", value); }
		}
		public bool ShiftKey {
			get { var result = Script.Get(instance, "shiftKey"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "shiftKey", value); }
		}
		public bool MetaKey {
			get { var result = Script.Get(instance, "metaKey"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "metaKey", value); }
		}
		public int KeyCode {
			get { var result = Script.Get(instance, "keyCode"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
			set { Script.Set(instance, "keyCode", value); }
		}
		public string KeyIdentifier {
			get { var result = Script.Get(instance, "keyIdentifier"); if (result is DBNull || result == null) return null; return (string)result; }
		}
		public int CharCode {
			get { var result = Script.Get(instance, "charCode"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
			set { Script.Set(instance, "charCode", value); }
		}
		public double ClientX {
			get { var result = Script.Get(instance, "clientX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "clientX", value); }
		}
		public double ClientY {
			get { var result = Script.Get(instance, "clientY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "clientY", value); }
		}
		public double OffsetX {
			get { var result = Script.Get(instance, "offsetX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "offsetX", value); }
		}
		public double OffsetY {
			get { var result = Script.Get(instance, "offsetY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "offsetY", value); }
		}
		public double LayerX {
			get { var result = Script.Get(instance, "layerX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "layerX", value); }
		}
		public double LayerY {
			get { var result = Script.Get(instance, "layerY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "layerY", value); }
		}
		public int Button {
			get { var result = Script.Get(instance, "button"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
			set { Script.Set(instance, "button", value); }
		}
		public double DeltaX {
			get { var result = Script.Get(instance, "deltaX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public double DeltaY {
			get { var result = Script.Get(instance, "deltaY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public double PageX {
			get { var result = Script.Get(instance, "pageX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public double PageY {
			get { var result = Script.Get(instance, "pageY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}

		public TouchList ChangedTouches {
			get {
				var touch = Script.Get(instance, "changedTouches");
				if ((touch is DBNull) || (touch == null))
					return null;
				return new TouchList(touch);
			}
		}

		public Date Date {
			get {
				var date = Script.Get(instance, "date");
				if ((date is DBNull) || (date == null))
					return null;
				return new Date(date.ToString());
			}
		}

		public bool Bubbles {
			get { var result = Script.Get(instance, "bubbles"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
		}
		public bool Cancelable {
			get { var result = Script.Get(instance, "cancelable"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
		}
		public WindowInstance View {
			get { var result = Script.Get(instance, "view"); if (result is DBNull || result == null) return null; return new WindowInstance(result); }
		}
		public int Detail {
			get { var result = Script.Get(instance, "detail"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}
		public double ScreenX {
			get { var result = Script.Get(instance, "screenX"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}
		public double ScreenY {
			get { var result = Script.Get(instance, "screenY"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
		}

		public Node SrcElement {
			get { var result = Script.Get(instance, "srcElement"); if (result is DBNull || result == null) return null; return Node.Create(result); }
		}

		public Node FromElement {
			get { var result = Script.Get(instance, "fromElement"); if (result is DBNull || result == null) return null; return Node.Create(result); }
		}

		public Node ToElement {
			get { var result = Script.Get(instance, "toElement"); if (result is DBNull || result == null) return null; return Node.Create(result); }
		}

		public object State {
			get { var result = Script.Get(instance, "state"); if (result is DBNull || result == null) return null; return result; }
		}

		public object this[string name] {
			get { return Script.Get(instance, name); }
			set { Script.Set(instance, name, value); }
		}

		public Event(object instance)
		{
			if (instance is string str) {
				this.instance = Document.CreateEvent(str);
			}
			else {
				this.instance = instance;
			}
		}

		~Event()
		{
			if (instance != null) {
				instance = null;
			}
		}

		internal static T Create<T>(string type) where T : Event
		{
			var ctor = typeof(T).GetConstructor(new Type[] { typeof(object) });
			return (T)ctor.Invoke(new object[] { type });
		}

		public bool IsDefined(string name)
		{
			return Script.IsDefined(instance, name);
		}

		public void addMember(string name, object value)
		{
			Script.Set(instance, name, value);
		}

		public virtual void PreventDefault()
		{
			Script.PreventDefault(instance);
		}

		public virtual void StopPropagation()
		{
			Script.StopPropagation(instance);
		}
	}

	public class KeyboardEvent : Event
	{
		public KeyboardEvent(object instance)
			: base(instance)
		{
		}
	}

	public class MouseEvent : KeyboardEvent/*Event*/
	{
		public MouseEvent(object instance)
			: base(instance)
		{
		}

		public MouseEvent(string type, Dictionary<string, object> dictionary)
			: this(Document.CreateEvent(type, dictionary))
		{
		}

		internal void initMouseEvent(string typeArg, bool bubbles, bool cancelable, WindowInstance view, int detail, int screenX, int screenY, int clientX, int clientY, bool ctrlKey, bool altKey, bool shiftKey, bool metaKey, ushort button, EventTarget eventTarget)
		{
			Script.InvokeMember(instance, "initMouseEvent", typeArg, bubbles, cancelable, (mshtml.IHTMLWindow2)view.instance, detail, screenX, screenY, clientX, clientY, ctrlKey, altKey, shiftKey, metaKey, button, eventTarget?.Instance);
		}
	}

	public class WheelEvent : MouseEvent/*Event*/
	{
		public WheelEvent(object instance)
			: base(instance)
		{
		}
	}

	public class TouchEvent : WheelEvent/*Event*/
	{
		public TouchEvent(object instance)
			: base(instance)
		{
		}
	}

	public class Touch
	{
		internal object instance;

		public Touch(object instance)
		{
			this.instance = instance;
		}

		public double ClientX {
			get { return Script.Get<double>(instance, "clientX"); }
		}
		public double ClientY {
			get { return Script.Get<double>(instance, "clientY"); }
		}
		public int Identifier {
			get { return Script.Get<int>(instance, "identifier"); }
		}

		public double PageX {
			get { return Script.Get<double>(instance, "pageX"); }
		}
		public double PageY {
			get { return Script.Get<double>(instance, "pageY"); }
		}
		public int ScreenX {
			get { return Script.Get<int>(instance, "screenX"); }
		}
		public int ScreenY {
			get { return Script.Get<int>(instance, "screenY"); }
		}
	}

	public class TouchList
	{
		internal object instance;

		public TouchList(object instance)
		{
			this.instance = instance;
		}

		public Touch this[int index] {
			get { return new Touch(Script.Get(instance, index)); }
			set { Script.Set(instance, index, value.instance); }
		}

		public int Length {
			get { return Script.Get<int>(instance, "length"); }
		}
	}

	[Flags]
	public enum DocumentPosition
	{
		Disconnected = 1,
		Preceding = 2,
		Following = 4,
		Contains = 8,
		ContainedBy = 16,
		ImplementationSpecific = 32
	}

	public enum NodeType
	{
		Element = 1,
		Attribute = 2,
		Text = 3,
		CDATA = 4,
		EntityReference = 5,
		Entity = 6,
		ProcessingInstruction = 7,
		Comment = 8,
		Document = 9,
		DocumentType = 10,
		DocumentFragment = 11,
		Notation = 12
	}

	public class Node : EventTarget
	{
		public string TextContent {
			get { return Script.Get<string>(instance, "textContent"); }
			set { Script.Set(instance, "textContent", value); }
		}
		public NodeType NodeType {
			get { return (NodeType)Convert.ToInt32(((mshtml.IHTMLDOMNode)instance).nodeType); }
		}

		public Node()
			: base(null)
		{
		}

		public Node(object instance)
			: base(instance)
		{
		}

		public static new Node Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;

			var type = (NodeType)Convert.ToInt32(((mshtml.IHTMLDOMNode)instance).nodeType);
			switch (type) {
			case NodeType.Text:
				return new Text(instance);
			case NodeType.Document:
				return new DocumentInstance(instance);
			case NodeType.DocumentFragment:
				return new DocumentFragment(instance);
			case NodeType.Element:
				return Element.Create(instance);
			case NodeType.Attribute:
				return Attr.Create(instance);
			default:
				return new Node(instance);
			}
		}

		public string NodeName {
			get { return ((mshtml.IHTMLDOMNode)instance).nodeName; }
		}
		public object NodeValue {
			get { return ((mshtml.IHTMLDOMNode)instance).nodeValue; }
		}
		public NodeList ChildNodes {
			get { return new NodeList(((mshtml.IHTMLDOMNode)instance).childNodes); }
		}
		public Node ParentNode {
			get { return Node.Create(((mshtml.IHTMLDOMNode)instance).parentNode); }
		}
		public Node NextSibling {
			get { return Node.Create(((mshtml.IHTMLDOMNode)instance).nextSibling); }
		}
		public Node PreviousSibling {
			get { return Node.Create(((mshtml.IHTMLDOMNode)instance).previousSibling); }
		}
		public Node FirstChild {
			get { return Node.Create(((mshtml.IHTMLDOMNode)instance).firstChild); }
		}
		public Node LastChild {
			get { return Node.Create(((mshtml.IHTMLDOMNode)instance).lastChild); }
		}
		public DocumentInstance OwnerDocument {
			get { return new DocumentInstance(Script.Get(instance, "ownerDocument")); }
		}

		public bool HasChildNodes() { return ((mshtml.IHTMLDOMNode)instance).hasChildNodes(); }

		public void AppendChild(Node parameter)
		{
			((mshtml.IHTMLDOMNode)instance).appendChild((mshtml.IHTMLDOMNode)parameter.instance);
		}

		public Node RemoveChild(Node child)
		{
			return Node.Create(((mshtml.IHTMLDOMNode)instance).removeChild((mshtml.IHTMLDOMNode)child.instance));
		}

		public Node ReplaceChild(Node newChild, Node oldChild)
		{
			return Node.Create(((mshtml.IHTMLDOMNode)instance).replaceChild((mshtml.IHTMLDOMNode)newChild.instance, (mshtml.IHTMLDOMNode)oldChild.instance));
		}

		public Node InsertBefore(Node newElement, Node referenceElement)
		{
			if (referenceElement == null)
				return Node.Create(((mshtml.IHTMLDOMNode)instance).insertBefore((mshtml.IHTMLDOMNode)newElement.instance));
			return Node.Create(((mshtml.IHTMLDOMNode)instance).insertBefore((mshtml.IHTMLDOMNode)newElement.instance, referenceElement.instance));
		}

		public Node CloneNode(bool deep = true)
		{
			return Node.Create(((mshtml.IHTMLDOMNode)instance).cloneNode(deep));
		}

		public bool Contains(Node node)
		{
			return ((mshtml.IHTMLElement)instance).contains((mshtml.IHTMLElement)node.instance);
		}

		public DocumentPosition CompareDocumentPosition(Node node)
		{
			return (DocumentPosition)Script.InvokeMember(instance, "compareDocumentPosition", node.instance);
		}
	}

	public class NodeList : IEnumerable<Node>
	{
		internal object instance;

		public NodeList(object instance)
		{
			this.instance = instance;
		}

		public Node this[int index] {
			get {
				return Node.Create(Script.Get(instance, index.ToString()));
			}
		}

		public int Length {
			get { var result = Script.Get(instance, "length"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}

		public class Enumerator : IEnumerator<Node>
		{
			private NodeList nodeList;
			private int index = -1;

			public Enumerator(NodeList nodeList)
			{
				this.nodeList = nodeList;
			}

			Node IEnumerator<Node>.Current {
				get {
					return Node.Create(Script.Get(nodeList.instance, index.ToString()));
				}
			}

			public object Current {
				get {
					return ((IEnumerator<Node>)this).Current;
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~Enumerator()
			{
				Dispose(false);
			}

			protected virtual void Dispose(bool disposing)
			{
			}

			public bool MoveNext()
			{
				index++;
				return index < nodeList.Length;
			}

			public void Reset()
			{
				index = -1;
			}
		}

		public IEnumerator<Node> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class Attr : Node
	{
		public bool Specified {
			get { return ((mshtml.IHTMLDOMAttribute)instance).specified; }
		}

		public static new Attr Create(object instance)
		{
			return new Attr(instance);
		}

		public Attr(object instance)
			: base(instance)
		{
		}
	}

	public enum ElementType
	{
		Anchor,
		Area,
		Audio,
		Base,
		Body,
		BR,
		Button,
		Canvas,
		DataList,
		Div,
		DList,
		Embed,
		FieldSet,
		Form,
		Head,
		H1,
		H2,
		H3,
		H4,
		H5,
		H6,
		HR,
		Html,
		IFrame,
		Image,
		Input,
		Keygen,
		Label,
		Legend,
		LI,
		Link,
		Map,
		Media,
		Meta,
		Meter,
		Mod,
		Object,
		OList,
		OptGroup,
		Option,
		Output,
		Paragraph,
		Param,
		Pre,
		Progress,
		Quote,
		Script,
		Select,
		Source,
		Span,
		Style,
		TableCaption,
		TableCell,
		TableCol,
		TableDataCell,
		Table,
		TableHeaderCell,
		TableRow,
		TableSection,
		TextArea,
		Title,
		Track,
		UList,
		Video
	}

	public class NamedNodeMap
	{
		internal object instance;

		public NamedNodeMap(object instance)
		{
			this.instance = instance;
		}

		public int Length {
			get { var result = Script.Get(instance, "length"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}
	}

	public class Element : Node
	{
		public string Id {
			get { return ((mshtml.IHTMLElement)instance).id; }
			set { ((mshtml.IHTMLElement)instance).id = value; }
		}
		public string InnerHTML {
			get { return ((mshtml.IHTMLElement)instance).innerHTML; }
			set { ((mshtml.IHTMLElement)instance).innerHTML = value; }
		}
		public string OuterHTML {
			get { return ((mshtml.IHTMLElement)instance).outerHTML; }
		}
		public string ClassName {
			get {
				var result = Script.Get(instance, "className");
				if (result is DBNull || result == null) return null;
				if (result is string) return (string)result;
				return Script.Get<string>(instance, "baseVal");
			}
			set { Script.Set(instance, "className", value); }
		}
		public string TagName {
			get { return ((mshtml.IHTMLElement)instance).tagName; }
		}
		public int ClientLeft {
			get { return ((mshtml.IHTMLElement2)instance).clientLeft; }
		}
		public int ClientTop {
			get { return ((mshtml.IHTMLElement2)instance).clientTop; }
		}
		public int ClientWidth {
			get { return ((mshtml.IHTMLElement2)instance).clientWidth; }
		}
		public int ClientHeight {
			get { return ((mshtml.IHTMLElement2)instance).clientHeight; }
		}
		public int ScrollLeft {
			get { return ((mshtml.IHTMLElement2)instance).scrollLeft; }
			set { ((mshtml.IHTMLElement2)instance).scrollLeft = value; }
		}
		public int ScrollTop {
			get { return ((mshtml.IHTMLElement2)instance).scrollTop; }
			set { ((mshtml.IHTMLElement2)instance).scrollTop = value; }
		}
		public int ScrollWidth {
			get { return ((mshtml.IHTMLElement2)instance).scrollWidth; }
		}
		public int ScrollHeight {
			get { return ((mshtml.IHTMLElement2)instance).scrollHeight; }
		}
		public NamedNodeMap Attributes {
			get { return new NamedNodeMap(Script.Get(instance, "attributes")); }
		}
		public Element FirstElementChild {
			get { return Element.Create(Script.Get(instance, "firstElementChild")); }
		}
		public Element ParentElement {
			get { return Element.Create(((mshtml.IHTMLElement)instance).parentElement); }
		}
		public HTMLCollection Children {
			get { return HTMLCollection.Create(Script.Get(instance, "children")); }
		}

		public Element()
			: base(null)
		{
		}

		public Element(object instance)
			: base(instance)
		{
			if (instance is ElementType) {
				string tagname = ToTagName((ElementType)instance);
				this.instance = Document.CreateElement<Element>(tagname).instance;
			}
		}

		public static string ToTagName(ElementType elementType)
		{
			var tagname = elementType.ToString().ToLower();
			switch (tagname) {
			case "anchor": tagname = "a"; break;
			case "datalist": tagname = "dl"; break;
			case "olist": tagname = "ol"; break;
			case "paragraph": tagname = "p"; break;
			case "quote": tagname = "q"; break;
			case "tablecaption": tagname = "caption"; break;
			case "tabledatacell": tagname = "td"; break;
			case "tableheadercell": tagname = "th"; break;
			case "tablerow": tagname = "tr"; break;
			case "ulist": tagname = "ul"; break;
			}
			return tagname;
		}

		public string GetAttribute(string name)
		{
			var ret = ((mshtml.IHTMLElement6)instance).getAttribute(name);
			if ((ret == null) || (ret is DBNull))
				return null;
			return ret.ToString();
		}

		public Attr GetAttributeNode(string name)
		{
			var ret = ((mshtml.IHTMLElement6)instance).getAttributeNode(name);
			if ((ret == null) || (ret is DBNull))
				return null;
			return Attr.Create(ret);
		}

		public virtual void SetAttribute(string name, object value)
		{
			((mshtml.IHTMLElement6)instance).setAttribute(name, ref value);
		}

		public void SetAttributeNS(object namespaceURI, string name, object value)
		{
			((mshtml.IHTMLElement6)instance).setAttributeNS(ref namespaceURI, name, ref value);
		}

		public void RemoveAttribute(string name)
		{
			((mshtml.IHTMLElement6)instance).removeAttribute(name);
		}

		public NodeList GetElementsByTagName(string tagname)
		{
			return new NodeList(((mshtml.IHTMLElement2)instance).getElementsByTagName(tagname));
		}

		public NodeList GetElementsByClassName(string className)
		{
			return new NodeList(Script.InvokeMember(instance, "getElementsByClassName", className));
		}

		public NodeList QuerySelectorAll(string query)
		{
			return new NodeList(Script.InvokeMember(instance, "querySelectorAll", query));
		}

		public ClientRect GetBoundingClientRect()
		{
			return new ClientRect(Script.InvokeMember(instance, "getBoundingClientRect", ((mshtml.IHTMLElement)instance).document));
		}

		public static new Element Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;

			var namespaceURI = Script.Get(instance, "namespaceURI");
			var tagName = ((mshtml.IHTMLElement)instance).tagName.ToLower();
			if (namespaceURI is DBNull || namespaceURI == null)
				return Element.Create(instance, tagName);

			switch ((string)namespaceURI) {
			case "http://www.w3.org/1999/xhtml":
				return HTMLElement.Create(instance);
			case "http://www.w3.org/2000/svg":
				return SVGElement.Create(instance);
			default:
				return Element.Create(instance, tagName, (string)namespaceURI);
			}
		}

		public static Element Create(object instance, string tagName, string namespaceURI = null)
		{
			var result = new Element(instance);
			Script.Set(result.instance, "tagName", tagName);
			if (namespaceURI != null)
				Script.Set(result.instance, "namespaceURI", namespaceURI);
			return result;
		}
	}

	public class Text : Node
	{
		public Text(object instance)
			: base(instance)
		{
			if (instance is string) {
				this.instance = Document.CreateTextNode((string)instance).instance;
			}
		}

		public string Data {
			get { return ((mshtml.HTMLDOMTextNode)instance).data; }
			set { ((mshtml.HTMLDOMTextNode)instance).data = value; }
		}
	}

	public enum Direction
	{
		Inherit,
		Ltr,
		Rtl
	}

	public enum Visibility
	{
		Inherit,
		Visible,
		Hidden,
		Collapse
	}

	public enum Position
	{
		Inherit,
		Static,
		Relative,
		Absolute,
		Fixed,
		Sticky
	}

	public enum Display
	{
		Blank,
		None,
		Inline,
		Block,
		ListItem,
		InlineBlock,
		InlineTable,
		Table,
		TableCaption,
		TableCell,
		TableColumn,
		TableColumnGroup,
		TableFooterGroup,
		TableHeaderGroup,
		TableRow,
		TableRowGroup,
		Flex,
		InlineFlex,
		Grid,
		InlineGrid
	}

	public enum BoxSizing
	{
		Blank,
		None,
		BorderBox,
		PaddingBox,
		ContentBox,
		Inherit,
	}

	public enum Overflow
	{
		Blank,
		Inherit,
		Visible,
		Hidden,
		Scroll,
		Auto
	}

	public class CSSStyleDeclaration
	{
		internal object instance;

		public CSSStyleDeclaration(object instance)
		{
			this.instance = instance;
		}

		public static CSSStyleDeclaration Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new CSSStyleDeclaration(instance);
		}

		public string MarginLeft {
			get { return Script.Get<string>(instance, "marginLeft"); }
			set { Script.Set(instance, "marginLeft", value); }
		}
		public string FillOpacity {
			get { return Script.Get<string>(instance, "fillOpacity"); }
			set { Script.Set(instance, "fillOpacity", value); }
		}
		public string Fill {
			get { return Script.Get<string>(instance, "fill"); }
			set { Script.Set(instance, "fill", value); }
		}
		public BoxSizing BoxSizing {
			get {
				var boxSizing = Script.Get<string>(instance, "boxSizing");
				if (boxSizing == null)
					return BoxSizing.Blank;
				switch (boxSizing.ToLower()) {
				case "": return BoxSizing.Blank;
				case "none": return BoxSizing.None;
				case "border-box": return BoxSizing.BorderBox;
				case "padding-box": return BoxSizing.PaddingBox;
				case "content-box": return BoxSizing.ContentBox;
				case "inherit": return BoxSizing.Inherit;
				default: throw new Exception();
				}
			}
			set {
				switch (value) {
				case BoxSizing.Blank: Script.Set(instance, "boxSizing", ""); break;
				case BoxSizing.None: Script.Set(instance, "boxSizing", "none"); break;
				case BoxSizing.BorderBox: Script.Set(instance, "boxSizing", "border-box"); break;
				case BoxSizing.PaddingBox: Script.Set(instance, "boxSizing", "padding-box"); break;
				case BoxSizing.ContentBox: Script.Set(instance, "boxSizing", "content-box"); break;
				case BoxSizing.Inherit: Script.Set(instance, "boxSizing", "inherit"); break;
				default: throw new Exception();
				}
			}
		}
		public Overflow Overflow {
			get {
				var overflow = Script.Get<string>(instance, "overflow");
				if (overflow == null)
					return Overflow.Blank;
				switch (overflow.ToLower()) {
				case "": return Overflow.Blank;
				case "inherit": return Overflow.Inherit;
				case "visible": return Overflow.Visible;
				case "hidden": return Overflow.Hidden;
				case "scroll": return Overflow.Scroll;
				case "auto": return Overflow.Auto;
				default: throw new Exception();
				}
			}
			set {
				switch (value) {
				case Overflow.Blank: Script.Set(instance, "overflow", ""); break;
				case Overflow.Inherit: Script.Set(instance, "overflow", "inherit"); break;
				case Overflow.Visible: Script.Set(instance, "overflow", "visible"); break;
				case Overflow.Hidden: Script.Set(instance, "overflow", "hidden"); break;
				case Overflow.Scroll: Script.Set(instance, "overflow", "scroll"); break;
				case Overflow.Auto: Script.Set(instance, "overflow", "auto"); break;
				default: throw new Exception();
				}
			}
		}
		public Display Display {
			get {
				var display = Script.Get<string>(instance, "display");
				if (display == null)
					return Display.Blank;
				switch (display.ToLower()) {
				case "": return Display.Blank;
				case "none": return Display.None;
				case "inline": return Display.Inline;
				case "block": return Display.Block;
				case "list-item": return Display.ListItem;
				case "inline-block": return Display.InlineBlock;
				case "inline-table": return Display.InlineTable;
				case "table": return Display.Table;
				case "table-caption": return Display.TableCaption;
				case "table-cell": return Display.TableCell;
				case "table-column": return Display.TableColumn;
				case "table-column-group": return Display.TableColumnGroup;
				case "table-footer-group": return Display.TableFooterGroup;
				case "table-header-group": return Display.TableHeaderGroup;
				case "table-row": return Display.TableRow;
				case "table-row-group": return Display.TableRowGroup;
				case "flex": return Display.Flex;
				case "inline-flex": return Display.InlineFlex;
				case "grid": return Display.Grid;
				case "inline-grid": return Display.InlineGrid;
				default: throw new Exception();
				}
			}
			set {
				switch (value) {
				case Display.Blank: Script.Set(instance, "display", ""); break;
				case Display.None: Script.Set(instance, "display", "none"); break;
				case Display.Inline: Script.Set(instance, "display", "inline"); break;
				case Display.Block: Script.Set(instance, "display", "block"); break;
				case Display.ListItem: Script.Set(instance, "display", "list-item"); break;
				case Display.InlineBlock: Script.Set(instance, "display", "inline-block"); break;
				case Display.InlineTable: Script.Set(instance, "display", "inline-table"); break;
				case Display.Table: Script.Set(instance, "display", "table"); break;
				case Display.TableCaption: Script.Set(instance, "display", "table-caption"); break;
				case Display.TableCell: Script.Set(instance, "display", "table-cell"); break;
				case Display.TableColumn: Script.Set(instance, "display", "table-column"); break;
				case Display.TableColumnGroup: Script.Set(instance, "display", "table-column-group"); break;
				case Display.TableFooterGroup: Script.Set(instance, "display", "table-footer-group"); break;
				case Display.TableHeaderGroup: Script.Set(instance, "display", "table-header-group"); break;
				case Display.TableRow: Script.Set(instance, "display", "table-row"); break;
				case Display.TableRowGroup: Script.Set(instance, "display", "table-row-group"); break;
				case Display.Flex: Script.Set(instance, "display", "flex"); break;
				case Display.InlineFlex: Script.Set(instance, "display", "inline-flex"); break;
				case Display.Grid: Script.Set(instance, "display", "grid"); break;
				case Display.InlineGrid: Script.Set(instance, "display", "inline-grid"); break;
				default: throw new Exception();
				}
			}
		}
		public string FontSize {
			get { return Script.Get<string>(instance, "fontSize"); }
			set { Script.Set(instance, "fontSize", value); }
		}
		public string Width {
			get { return Script.Get<string>(instance, "width"); }
			set { Script.Set(instance, "width", value); }
		}
		public string Height {
			get { return Script.Get<string>(instance, "height"); }
			set { Script.Set(instance, "height", value); }
		}
		public string Left {
			get { return Script.Get<string>(instance, "left"); }
			set { Script.Set(instance, "left", value); }
		}
		public string Top {
			get { return Script.Get<string>(instance, "top"); }
			set { Script.Set(instance, "top", value); }
		}
		public string Cursor {
			get { return Script.Get<string>(instance, "cursor"); }
			set { Script.Set(instance, "cursor", value); }
		}
		public string Opacity {
			get { return Script.Get<string>(instance, "opacity"); }
			set { Script.Set(instance, "opacity", value); }
		}
		public string BorderRight {
			get { return Script.Get<string>(instance, "borderRight"); }
			set { Script.Set(instance, "borderRight", value); }
		}
		public string BorderLeft {
			get { return Script.Get<string>(instance, "borderLeft"); }
			set { Script.Set(instance, "borderLeft", value); }
		}
		public Direction Direction {
			get {
				switch ((Script.Get<string>(instance, "direction")).ToLower()) {
				case "inherit": return Direction.Inherit;
				case "ltr": return Direction.Ltr;
				case "rtl": return Direction.Rtl;
				default: throw new Exception();
				}
			}
			set { Script.Set(instance, "direction", value.ToString().ToLower()); }
		}
		public string Bottom {
			get { return Script.Get<string>(instance, "bottom"); }
			set { Script.Set(instance, "bottom", value); }
		}
		public string Right {
			get { return Script.Get<string>(instance, "right"); }
			set { Script.Set(instance, "right", value); }
		}
		public string BackgroundColor {
			get { return Script.Get<string>(instance, "backgroundColor"); }
			set { Script.Set(instance, "backgroundColor", value); }
		}
		public Visibility Visibility {
			get {
				switch ((Script.Get<string>(instance, "visibility")).ToLower()) {
				case "inherit": return Visibility.Inherit;
				case "visible": return Visibility.Visible;
				case "hidden": return Visibility.Hidden;
				case "collapse": return Visibility.Collapse;
				default: throw new Exception();
				}
			}
			set { Script.Set(instance, "visibility", value.ToString().ToLower()); }
		}
		public Position Position {
			get {
				switch ((Script.Get<string>(instance, "position")).ToLower()) {
				case "inherit": return Position.Inherit;
				case "static": return Position.Static;
				case "relative": return Position.Relative;
				case "absolute": return Position.Absolute;
				case "fixed": return Position.Fixed;
				case "sticky": return Position.Sticky;
				default: throw new Exception();
				}
			}
			set { Script.Set(instance, "position", value.ToString().ToLower()); }
		}
		public string BackgroundPosition {
			get { return Script.Get<string>(instance, "backgroundPosition"); }
			set { Script.Set(instance, "backgroundPosition", value); }
		}
		public string PaddingRight {
			get { return Script.Get<string>(instance, "paddingRight"); }
			set { Script.Set(instance, "paddingRight", value); }
		}
		public string PaddingLeft {
			get { return Script.Get<string>(instance, "paddingLeft"); }
			set { Script.Set(instance, "paddingLeft", value); }
		}
		public string CssText {
			get { return Script.Get<string>(instance, "cssText"); }
			set { Script.Set(instance, "cssText", value); }
		}
		public string PointerEvents {
			get { return Script.Get<string>(instance, "pointerEvents"); }
			set { Script.Set(instance, "pointerEvents", value); }
		}
		public string BorderLeftWidth {
			get { return Script.Get<string>(instance, "borderLeftWidth"); }
			set { Script.Set(instance, "borderLeftWidth", value); }
		}
		public string BorderLeftStyle {
			get { return Script.Get<string>(instance, "borderLeftStyle"); }
			set { Script.Set(instance, "borderLeftStyle", value); }
		}
		public string BorderColor {
			get { return Script.Get<string>(instance, "borderColor"); }
			set { Script.Set(instance, "borderColor", value); }
		}

		public object this[string name] {
			get { var result = Script.Get(instance, name); return result; }
			set { Script.Set(instance, name, value); }
		}

		public string GetPropertyValue(string property)
		{
			return (string)Script.InvokeMember(instance, "getPropertyValue", property);
		}
	}

	public class CSSStyleSheet
	{
		internal object instance;

		public CSSStyleSheet(object instance)
		{
			this.instance = instance;
		}

		public void DeleteRule(int index)
		{
			Script.InvokeMember(instance, "deleteRule", index);
		}

		public void InsertRule(string rule, int index)
		{
			Script.InvokeMember(instance, "insertRule", rule, index);
		}
	}

	public class DOMTokenList : IEnumerable<string>
	{
		internal object instance;

		public DOMTokenList(object instance)
		{
			this.instance = instance;
		}

		public string this[int index] {
			get {
				return Script.Get<string>(instance, index.ToString());
			}
		}

		public int Length {
			get { var result = Script.Get(instance, "length"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}

		public class Enumerator : IEnumerator<string>
		{
			private DOMTokenList nodeList;
			private int index = -1;

			public Enumerator(DOMTokenList jquery)
			{
				this.nodeList = jquery;
			}

			string IEnumerator<string>.Current {
				get {
					return Script.Get<string>(nodeList.instance, index.ToString());
				}
			}

			public object Current {
				get {
					return ((IEnumerator<string>)this).Current;
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~Enumerator()
			{
				Dispose(false);
			}

			protected virtual void Dispose(bool disposing)
			{
			}

			public bool MoveNext()
			{
				index++;
				return index < nodeList.Length;
			}

			public void Reset()
			{
				index = -1;
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(string className)
		{
			Script.InvokeMember(instance, "add", className);
		}

		public void Remove(string className)
		{
			Script.InvokeMember(instance, "remove", className);
		}
	}

	public class HTMLCollection : IEnumerable<HTMLElement>
	{
		internal object instance;

		public HTMLCollection(object instance)
		{
			this.instance = instance;
		}

		public static HTMLCollection Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;
			return new HTMLCollection(instance);
		}

		public HTMLElement this[int index] {
			get {
				return HTMLElement.Create(Script.Get(instance, index.ToString()));
			}
		}

		public int Length {
			get { return ((mshtml.HTMLElementCollection)instance).length; }
		}

		public class Enumerator : IEnumerator<HTMLElement>
		{
			private HTMLCollection collection;
			private int index = -1;

			public Enumerator(HTMLCollection collection)
			{
				this.collection = collection;
			}

			HTMLElement IEnumerator<HTMLElement>.Current {
				get {
					return HTMLElement.Create(Script.Get(collection.instance, index.ToString()));
				}
			}

			public object Current {
				get {
					return ((IEnumerator<HTMLElement>)this).Current;
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~Enumerator()
			{
				Dispose(false);
			}

			protected virtual void Dispose(bool disposing)
			{
			}

			public bool MoveNext()
			{
				index++;
				return index < collection.Length;
			}

			public void Reset()
			{
				index = -1;
			}
		}

		public IEnumerator<HTMLElement> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class HTMLElement : Element
	{
		public CSSStyleDeclaration Style {
			get { return CSSStyleDeclaration.Create(((mshtml.IHTMLElement)instance).style); }
		}
		public CSSStyleDeclaration RuntimeStyle {
			get { return CSSStyleDeclaration.Create(((mshtml.IHTMLElement2)instance).runtimeStyle); }
		}
		public CSSStyleDeclaration CurrentStyle {
			get { return CSSStyleDeclaration.Create(Script.Get(instance, "clientStyle")); }
		}
		public double OffsetLeft {
			get { return ((mshtml.IHTMLElement)instance).offsetLeft; }
			set { Script.Set(instance, "offsetLeft", value); }
		}
		public double OffsetTop {
			get { return ((mshtml.IHTMLElement)instance).offsetTop; }
			set { Script.Set(instance, "offsetTop", value); }
		}
		public double OffsetWidth {
			get { return ((mshtml.IHTMLElement)instance).offsetWidth; }
			set { Script.Set(instance, "offsetWidth", value); }
		}
		public double OffsetHeight {
			get { return ((mshtml.IHTMLElement)instance).offsetHeight; }
			set { Script.Set(instance, "offsetHeight", value); }
		}
		public bool IsContentEditable {
			get { var result = Script.Get(instance, "isContentEditable"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
		}
		public int TabIndex {
			get { var result = Script.Get(instance, "tabIndex"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
			set { Script.Set(instance, "tabIndex", value); }
		}
		public string Title {
			get { return ((mshtml.IHTMLElement)instance).title; }
			set { ((mshtml.IHTMLElement)instance).title = value; }
		}
		public DOMTokenList ClassList {
			get { return null; /*var list = Script.Get(instance, "classList"); if (list is DBNull || list == null) return null; return new DOMTokenList(list);*/ }
		}
		public bool HideFocus {
			get { var result = Script.Get(instance, "hideFocus"); if (result is DBNull || result == null) return false; return Convert.ToBoolean(result); }
			set { Script.Set(instance, "hideFocus", value); }
		}

		public HTMLElement(object instance)
			: base(instance)
		{
		}

		public static new HTMLElement Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;

			var tagName = ((mshtml.IHTMLElement)instance).tagName.ToLower();
			switch (tagName) {
			case "body":
				return new HTMLBodyElement(instance);
			case "head":
				return new HTMLHeadElement(instance);
			case "style":
				return new HTMLStyleElement(instance);
			case "div":
				return new HTMLDivElement(instance);
			case "input":
				return new HTMLInputElement(instance);
			case "textarea":
				return new HTMLTextAreaElement(instance);
			case "audio":
				return new HTMLAudioElement(instance);
			case "td":
				return new HTMLTableDataCellElement(instance);
			case "table":
				return new HTMLTableElement(instance);
			case "tbody":
				return new HTMLTableSectionElement(instance);
			case "tr":
				return new HTMLTableRowElement(instance);
			case "pre":
				return new HTMLPreElement(instance);
			case "a":
				return new HTMLAnchorElement(instance);
			case "th":
				return new HTMLTableCellElement(instance);
			case "button":
				return new HTMLButtonElement(instance);
			case "select":
				return new HTMLSelectElement(instance);
			//case "a":
			case "area":
			//case "audio":
			case "base":
			//case "body":
			case "br":
			//case "button":
			case "canvas":
			case "dl":
			//case "div":
			case "dlist":
			case "embed":
			case "fieldset":
			case "form":
			//case "head":
			case "h1":
			case "h2":
			case "h3":
			case "h4":
			case "h5":
			case "h6":
			case "hr":
			case "html":
			case "iframe":
			case "image":
			//case "input":
			case "keygen":
			case "label":
			case "legend":
			case "li":
			case "link":
			case "map":
			case "media":
			case "meta":
			case "meter":
			case "mod":
			case "object":
			case "ol":
			case "optgroup":
			case "option":
			case "output":
			case "p":
			case "param":
			//case "pre":
			case "progress":
			case "q":
			case "script":
			//case "select":
			case "source":
			case "span":
			//case "style":
			case "caption":
			case "tablecell":
			case "tablecol":
			//case "td":
			//case "table":
			//case "th":
			//case "tr":
			case "tablesection":
			//case "textarea":
			case "title":
			case "track":
			case "ul":
			case "video":
			default:
				return new HTMLElement(instance);
			}
		}

		public void Focus()
		{
			Script.InvokeMember(instance, "focus");
		}

		public void Blur()
		{
			Script.InvokeMember(instance, "blur");
		}
	}

	public class HTMLBodyElement : HTMLElement
	{
		public HTMLBodyElement(object instance)
			: base(instance)
		{
		}

		public CSSStyleDeclaration runtimeStyle {
			get { return CSSStyleDeclaration.Create(Script.Get(instance, "runtimeStyle")); }
		}
	}

	public class HTMLHeadElement : HTMLElement
	{
		public HTMLHeadElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLDivElement : HTMLElement
	{
		public HTMLDivElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLPreElement : HTMLElement
	{
		public HTMLPreElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLAnchorElement : HTMLElement
	{
		public string Href {
			get { return Script.Get<string>(instance, "href"); }
			set { Script.Set(instance, "href", value); }
		}
		public string Download {
			get { return Script.Get<string>(instance, "download"); }
			set { Script.Set(instance, "download", value); }
		}

		public HTMLAnchorElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLTableElement : HTMLElement
	{
		public string CellSpacing {
			get { return Script.Get<string>(instance, "cellSpacing"); }
			set { Script.Set(instance, "cellSpacing", value); }
		}
		public string CellPadding {
			get { return Script.Get<string>(instance, "cellPadding"); }
			set { Script.Set(instance, "cellPadding", value); }
		}
		public HTMLCollection Rows {
			get { return HTMLCollection.Create(Script.Get(instance, "rows")); }
		}

		public HTMLTableElement(object instance)
			: base(instance)
		{
		}

		internal HTMLTableRowElement InsertRow(int index)
		{
			return HTMLTableRowElement.Create(Script.InvokeMember(instance, "insertRow", index));
		}

		internal void DeleteRow(int index)
		{
			Script.InvokeMember(instance, "deleteRow", index);
		}
	}

	public class HTMLTableDataCellElement : HTMLElement
	{
		public HTMLTableDataCellElement(object instance)
			: base(instance)
		{
		}

		public int ColSpan {
			get { var result = Script.Get(instance, "colSpan"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
			set { Script.Set(instance, "colSpan", value); }
		}
	}

	public class HTMLTableSectionElement : HTMLElement
	{
		public HTMLCollection Rows {
			get { return HTMLCollection.Create(Script.Get(instance, "rows")); }
		}

		public HTMLTableSectionElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLTableRowElement : HTMLElement
	{
		public HTMLCollection Cells {
			get { return HTMLCollection.Create(Script.Get(instance, "cells")); }
		}

		public HTMLTableRowElement(object instance)
			: base(instance)
		{
		}

		public static new HTMLTableRowElement Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new HTMLTableRowElement(instance);
		}

		internal HTMLTableCellElement InsertCell(int index)
		{
			return HTMLTableCellElement.Create(Script.InvokeMember(instance, "insertRow", index));
		}
	}

	public class HTMLTableCellElement : HTMLElement
	{
		public HTMLTableCellElement(object instance)
			: base(instance)
		{
		}

		public static new HTMLTableCellElement Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new HTMLTableCellElement(instance);
		}
	}

	public class HTMLLabelElement : HTMLElement
	{
		public HTMLLabelElement(object instance)
			: base(instance)
		{
		}

		public string HtmlFor {
			get { return Script.Get<string>(instance, "htmlFor"); }
			set { Script.Set(instance, "htmlFor", value); }
		}
	}

	public class HTMLInputElement : HTMLElement
	{
		public string Value {
			get { return Script.Get<string>(instance, "value"); }
			set { Script.Set(instance, "value", value); }
		}
		public string DefaultValue {
			get { return Script.Get<string>(instance, "defaultValue"); }
			set { Script.Set(instance, "defaultValue", value); }
		}
		public bool Checked {
			get { return Script.Get<bool>(instance, "checked"); }
			set { Script.Set(instance, "checked", value); }
		}
		public FileList files {
			get { return FileList.Create(Script.Get<bool>(instance, "files")); }
		}

		public bool Disabled { get; internal set; }

		public HTMLInputElement(object instance)
			: base(instance)
		{
		}

		public void Select()
		{
			Script.InvokeMember(instance, "select");
		}
	}

	public class HTMLStyleElement : HTMLElement
	{
		public HTMLStyleElement(object instance)
			: base(instance)
		{
		}

		public CSSStyleSheet Sheet {
			get { return new CSSStyleSheet(Script.Get(instance, "sheet")); }
		}
	}

	public class HTMLTextAreaElement : HTMLElement
	{
		public string Value {
			get { return Script.Get<string>(instance, "value"); }
			set { Script.Set(instance, "value", value); }
		}

		public bool Disabled {
			get { return Script.Get<bool>(instance, "disabled"); }
			set { Script.Set(instance, "disabled", value); }
		}

		public HTMLTextAreaElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLButtonElement : HTMLElement
	{
		public bool Disabled {
			get { return Script.Get<bool>(instance, "disabled"); }
			set { Script.Set(instance, "disabled", value); }
		}

		public string Value {
			get { return Script.Get<string>(instance, "value"); }
			set { Script.Set(instance, "value", value); }
		}

		public HTMLButtonElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLSelectElement : HTMLElement
	{
		public bool Disabled {
			get { return Script.Get<bool>(instance, "disabled"); }
			set { Script.Set(instance, "disabled", value); }
		}

		public string Value {
			get { return Script.Get<string>(instance, "value"); }
			set { Script.Set(instance, "value", value); }
		}

		public HTMLSelectElement(object instance)
			: base(instance)
		{
		}
	}

	public class HTMLAudioElement : HTMLElement
	{
		public HTMLAudioElement(object instance)
			: base(instance)
		{
		}

		public double Volume {
			get { var result = Script.Get(instance, "volume"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "volume", value); }
		}

		public void Play()
		{
			Script.InvokeMember(instance, "play");
		}

		public void Pause()
		{
			Script.InvokeMember(instance, "pause");
		}

		public string CanPlayType(string mediaType)
		{
			return (string)Script.InvokeMember(instance, "canPlayType", mediaType);
		}
	}

	public class HTMLIFrameElement : HTMLElement
	{
		public HTMLIFrameElement(object instance)
			: base(instance)
		{
		}

		public DocumentInstance ContentDocument {
			get { var result = Script.Get(instance, "contentDocument"); if (result is DBNull || result == null) return null; return new DocumentInstance(result); }
		}

		public WindowInstance ContentWindow {
			get { var result = Script.Get(instance, "contentWindow"); if (result is DBNull || result == null) return null; return new WindowInstance(result); }
		}
	}

	public class SVGMatrix
	{
		internal object instance;

		public double a {
			get { var result = Script.Get(instance, "a"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "a", value); }
		}
		public double b {
			get { var result = Script.Get(instance, "b"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "b", value); }
		}
		public double c {
			get { var result = Script.Get(instance, "c"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "c", value); }
		}
		public double d {
			get { var result = Script.Get(instance, "d"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "d", value); }
		}
		public double e {
			get { var result = Script.Get(instance, "e"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "e", value); }
		}
		public double f {
			get { var result = Script.Get(instance, "f"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "f", value); }
		}

		public SVGMatrix(object instance)
		{
			this.instance = instance;
		}

		public SVGMatrix Translate(double x, double y)
		{
			return new SVGMatrix(Script.InvokeMember(instance, "translate", x, y));
		}

		public SVGMatrix Scale(double a)
		{
			return new SVGMatrix(Script.InvokeMember(instance, "scale", a));
		}

		public SVGMatrix Inverse()
		{
			return new SVGMatrix(Script.InvokeMember(instance, "inverse"));
		}
	}

	public class SVGPoint
	{
		internal object instance;

		public double x {
			get { var result = Script.Get(instance, "x"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "x", value); }
		}
		public double y {
			get { var result = Script.Get(instance, "y"); if (result is DBNull || result == null) return 0.0; return Convert.ToDouble(result); }
			set { Script.Set(instance, "y", value); }
		}

		public SVGPoint(object instance)
		{
			this.instance = instance;
		}

		public SVGPoint matrixTransform(SVGMatrix matrix)
		{
			return new SVGPoint(Script.InvokeMember(instance, "matrixTransform", matrix.instance));
		}
	}

	public class SVGRect
	{
		public double x;
		public double y;
		public double width;
		public double height;

		public SVGRect()
		{
		}

		public SVGRect(object instance)
		{
			x = Convert.ToDouble(Script.Get(instance, "x"));
			y = Convert.ToDouble(Script.Get(instance, "y"));
			width = Convert.ToDouble(Script.Get(instance, "width"));
			height = Convert.ToDouble(Script.Get(instance, "height"));
		}
	}

	public class SVGAnimatedString
	{
		internal object instance;

		public string baseVal {
			get { return Script.Get<string>(instance, "baseVal"); }
		}

		public SVGAnimatedString(object instance)
		{
			this.instance = instance;
		}
	}

	public class SVGElement : Element
	{
		public CSSStyleDeclaration currentStyle {
			get { if (Script.Get(instance, "currentStyle") == null) return null; return CSSStyleDeclaration.Create(Script.Get(instance, "currentStyle")); }
			set { Script.Set(instance, "currentStyle", value.instance); }
		}
		public CSSStyleDeclaration runtimeStyle {
			get { if (Script.Get(instance, "runtimeStyle") == null) return null; return CSSStyleDeclaration.Create(Script.Get(instance, "runtimeStyle")); }
			set { Script.Set(instance, "runtimeStyle", value.instance); }
		}
		public CSSStyleDeclaration style {
			get { if (Script.Get(instance, "style") == null) return null; return CSSStyleDeclaration.Create(Script.Get(instance, "style")); }
		}
		public SVGElement ownerSVGElement {
			get { if (Script.Get(instance, "ownerSVGElement") == null) return null; return new SVGElement(Script.Get(instance, "ownerSVGElement")); }
		}
		public SVGAnimatedString className {
			get { if (Script.Get(instance, "className") == null) return null; return new SVGAnimatedString(Script.Get(instance, "className")); }
		}

		public object tooltip {
			get {
				var result = Script.Get(instance, "tooltip");
				return result is DBNull ? null : result;
			}
			set { Script.Set(instance, "tooltip", value); }
		}

		public SVGElement(object instance)
			: base(instance)
		{
		}

		public static new SVGElement Create(object instance)
		{
			if (instance is DBNull || instance == null)
				return null;

			var tagName = (Script.Get<string>(instance, "tagName")).ToLower();
			switch (tagName) {
			case "svg":
				return new SVGSVGElement(instance);
			case "text":
				return new SVGTextElement(instance);
			case "g":
				return new SVGGElement(instance);
			case "path":
				return new SVGPathElement(instance);
			case "rect":
				return new SVGRectElement(instance);
			case "filter":
				return new SVGElement(instance);
			}
			return new SVGElement(instance);
		}

		public static new SVGElement Create(object instance, string tagName, string namespaceURI = null)
		{
			var result = new SVGElement(instance);
			Script.Set(result.instance, "tagName", tagName);
			if (namespaceURI != null)
				Script.Set(result.instance, "namespaceURI", namespaceURI);
			return result;
		}

		public SVGPoint createSVGPoint()
		{
			return new SVGPoint(Script.InvokeMember(instance, "createSVGPoint"));
		}

		public SVGRect getBBox()
		{
			return new SVGRect(Script.InvokeMember(instance, "getBBox"));
		}

		public double getComputedTextLength()
		{
			return Convert.ToDouble(Script.InvokeMember(instance, "getComputedTextLength"));
		}

		public SVGMatrix getCTM()
		{
			return new SVGMatrix(Script.InvokeMember(instance, "getCTM"));
		}

		public SVGMatrix getScreenCTM()
		{
			return new SVGMatrix(Script.InvokeMember(instance, "getScreenCTM"));
		}
	}

	public class SVGSVGElement : SVGElement
	{
		public SVGSVGElement(object instance)
			: base(instance)
		{
		}
	}

	public class SVGTextElement : SVGElement
	{
		public SVGTextElement(object instance)
			: base(instance)
		{
		}
	}

	public class SVGGElement : SVGElement
	{
		public SVGGElement(object instance)
			: base(instance)
		{
		}
	}

	public class SVGPathElement : SVGElement
	{
		public SVGPathElement(object instance)
			: base(instance)
		{
		}
	}

	public class SVGRectElement : SVGElement
	{
		public SVGRectElement(object instance)
			: base(instance)
		{
		}
	}

	public class Blob
	{
		internal object instance;

		public Blob(object instance)
		{
			this.instance = instance;
		}

		public Blob(string[] v, Dictionary<string, object> dictionary)
		{

		}
	}

	public class File : Blob
	{
		public File(object instance)
			: base(instance)
		{
		}

		public string Name { get; internal set; }

		internal static File Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new File(instance);
		}
	}

	public class FileList : IEnumerable<File>
	{
		internal object instance;

		public FileList(object instance)
		{
			this.instance = instance;
		}

		public static FileList Create(object instance)
		{
			if (instance is DBNull || instance is null)
				return null;
			return new FileList(instance);
		}

		public File this[int index] {
			get {
				return File.Create(Script.Get(instance, index.ToString()));
			}
		}

		public int Length {
			get { var result = Script.Get(instance, "length"); if (result is DBNull || result == null) return 0; return Convert.ToInt32(result); }
		}

		public class Enumerator : IEnumerator<File>
		{
			private FileList nodeList;
			private int index = -1;

			public Enumerator(FileList nodeList)
			{
				this.nodeList = nodeList;
			}

			File IEnumerator<File>.Current {
				get {
					return File.Create(Script.Get(nodeList.instance, index.ToString()));
				}
			}

			public object Current {
				get {
					return ((IEnumerator<File>)this).Current;
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~Enumerator()
			{
				Dispose(false);
			}

			protected virtual void Dispose(bool disposing)
			{
			}

			public bool MoveNext()
			{
				index++;
				return index < nodeList.Length;
			}

			public void Reset()
			{
				index = -1;
			}
		}

		public IEnumerator<File> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
