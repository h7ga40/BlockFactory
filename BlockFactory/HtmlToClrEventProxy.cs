// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Bridge
{
	public class HtmlToClrEventArgs : EventArgs
	{
		public object[] EventArgs { get; }
		public HtmlToClrEventArgs(object[] eventArgs)
		{
			EventArgs = eventArgs;
		}
	}

	/// <summary>
	///  This class is here for IHTML*3.AttachHandler style eventing.
	///  We need a way of routing requests for DISPID(0) to a particular CLR event without creating
	///  a public class.  In order to accomplish this we implement IReflect and handle InvokeMethod
	///  to call back on a CLR event handler.
	/// </summary>
	public abstract class HtmlToClrEventProxy : IReflect
	{
		private readonly IReflect typeIReflectImplementation;

		public HtmlToClrEventProxy()
		{
			Type htmlToClrEventProxyType = typeof(HtmlToClrEventProxy);
			typeIReflectImplementation = htmlToClrEventProxyType as IReflect;
		}

		[DispId(0)]
		public object OnHtmlEvent(object[] eventArgs)
		{
			return InvokeClrEvent(eventArgs);
		}

		protected abstract object InvokeClrEvent(object[] eventArgs);

		#region IReflect

		Type IReflect.UnderlyingSystemType {
			get {
				var ret = typeIReflectImplementation.UnderlyingSystemType;
				return ret;
			}
		}

		// Methods
		FieldInfo IReflect.GetField(string name, BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetField(name, bindingAttr);
			return ret;
		}
		FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetFields(bindingAttr);
			return ret;
		}
		MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetMember(name, bindingAttr);
			return ret;
		}
		MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetMembers(bindingAttr);
			return ret;
		}
		MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetMethod(name, bindingAttr);
			return ret;
		}
		MethodInfo IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			var ret = typeIReflectImplementation.GetMethod(name, bindingAttr, binder, types, modifiers);
			return ret;
		}
		MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetMethods(bindingAttr);
			return ret;
		}
		PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetProperties(bindingAttr);
			return ret;
		}
		PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr)
		{
			var ret = typeIReflectImplementation.GetProperty(name, bindingAttr);
			return ret;
		}
		PropertyInfo IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			var ret = typeIReflectImplementation.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
			return ret;
		}

		// InvokeMember:
		// If we get a call for DISPID=0, fire the CLR event.
		//
		object IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
		{
			//
			if (name == "[DISPID=0]") {
				// we know we're getting called back to fire the event - translate this now into a CLR event.
				var ret = OnHtmlEvent(args);
				// since there's no return value for void, return null.
				return ret;
			}
			else {
				var ret = typeIReflectImplementation.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
				return ret;
			}
		}
		#endregion
	}
}
