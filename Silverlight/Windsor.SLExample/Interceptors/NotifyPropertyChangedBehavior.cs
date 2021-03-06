// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Samples.WindsorSilverlight.Interceptors
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;

	using Castle.DynamicProxy;

	public class NotifyPropertyChangedBehavior : IInterceptor
	{
		private PropertyChangedEventHandler handler;

		#region IInterceptor Members

		public void Intercept(IInvocation invocation)
		{
			var methodName = invocation.Method.Name;
			var arguments = invocation.Arguments;
			var proxy = invocation.Proxy;
			var isEditableObject = proxy is IEditableObject;

			if (invocation.Method.DeclaringType.Equals(typeof (INotifyPropertyChanged)))
			{
				if (methodName == "add_PropertyChanged")
				{
					StoreHandler((Delegate) arguments[0]);
				}
				if (methodName == "remove_PropertyChanged")
				{
					RemoveHandler((Delegate) arguments[0]);
				}
			}

			if (!ShouldProceedWithInvocation(methodName))
				return;

			invocation.Proceed();

			NotifyPropertyChanged(methodName, proxy, isEditableObject);
		}

		#endregion

		protected void OnPropertyChanged(Object sender, PropertyChangedEventArgs e)
		{
			var eventHandler = handler;
			if (eventHandler != null) eventHandler(sender, e);
		}

		protected void RemoveHandler(Delegate @delegate)
		{
			handler = (PropertyChangedEventHandler) Delegate.Remove(handler, @delegate);
		}

		protected void StoreHandler(Delegate @delegate)
		{
			handler = (PropertyChangedEventHandler) Delegate.Combine(handler, @delegate);
		}

		protected void NotifyPropertyChanged(string methodName, object proxy, bool isEditableObject)
		{
			if ("CancelEdit".Equals(methodName) && isEditableObject)
			{
				var properties = proxy.GetType()
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(p => p.CanWrite);

				foreach (var prop in properties)
				{
					OnPropertyChanged(proxy, new PropertyChangedEventArgs(prop.Name));
				}
			}

			if (methodName.StartsWith("set_"))
			{
				var propertyName = methodName.Substring(4);

				var args = new PropertyChangedEventArgs(propertyName);
				OnPropertyChanged(proxy, args);
			}
		}

		protected bool ShouldProceedWithInvocation(string methodName)
		{
			var methodsWithoutTarget = new[] {"add_PropertyChanged", "remove_PropertyChanged"};
			return !methodsWithoutTarget.Contains(methodName);
		}
	}
}