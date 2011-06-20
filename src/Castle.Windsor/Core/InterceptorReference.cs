// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

namespace Castle.Core
{
	using System;
	using System.Diagnostics;
	using System.Linq;

	using Castle.Core.Internal;
	using Castle.DynamicProxy;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Context;
	using Castle.MicroKernel.Proxy;
	using Castle.MicroKernel.Resolvers;
	using Castle.MicroKernel.Util;

	/// <summary>
	///   Represents an reference to a Interceptor component.
	/// </summary>
	[Serializable]
	public class InterceptorReference : IReference<IInterceptor>, IEquatable<InterceptorReference>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly DependencyModel dependencyModel;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly string serviceOverrideComponent;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Type serviceType;

		/// <summary>
		///   Initializes a new instance of the <see cref = "InterceptorReference" /> class.
		/// </summary>
		/// <param name = "componentKey">The component key.</param>
		public InterceptorReference(String componentKey)
		{
			if (componentKey == null)
			{
				throw new ArgumentNullException("componentKey");
			}
			serviceOverrideComponent = componentKey;
			dependencyModel = new DependencyModel(BuildDependencyName(), typeof(IInterceptor), false);
		}

		/// <summary>
		///   Initializes a new instance of the <see cref = "InterceptorReference" /> class.
		/// </summary>
		/// <param name = "serviceType">Type of the service.</param>
		public InterceptorReference(Type serviceType)
		{
			if (serviceType == null)
			{
				throw new ArgumentNullException("serviceType");
			}
			this.serviceType = serviceType;
			dependencyModel = new DependencyModel(BuildDependencyName(), serviceType, false);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			return Equals(obj as InterceptorReference);
		}

		public override int GetHashCode()
		{
			var result = 0;
			result = 29*result + (serviceType != null ? serviceType.GetHashCode() : 0);
			result = 29*result + (serviceOverrideComponent != null ? serviceOverrideComponent.GetHashCode() : 0);
			return result;
		}

		public override string ToString()
		{
			if (serviceType == null)
			{
				return serviceOverrideComponent;
			}
			return serviceType.Name ?? string.Empty;
		}

		public bool Equals(InterceptorReference other)
		{
			if (other == null)
			{
				return false;
			}
			if (!Equals(serviceType, other.serviceType))
			{
				return false;
			}
			if (!Equals(serviceOverrideComponent, other.serviceOverrideComponent))
			{
				return false;
			}
			return true;
		}

		private Type GetHandlerType(IHandler handler)
		{
			try
			{
				return serviceType ??
					   handler.ComponentModel.Services.SingleOrDefault(s => s == typeof(IInterceptor)) ??
					   handler.ComponentModel.Services.Single(s => s.Is<IInterceptor>());
			}
			catch (InvalidOperationException e)
			{
				throw new DependencyResolverException(
					string.Format(
						"Ambiguous service - interceptor {0} has more than one service compabtible with type {1}. Register the interceptor explicitly as {1} or pick single type compabtible with this interface",
						handler.ComponentModel.Name, typeof(IInterceptor).Name), e);
			}
		}

		private IHandler GetInterceptorHandler(IKernel kernel)
		{
			if (serviceType != null)
			{
				return kernel.GetHandler(serviceType);
			}
			return kernel.GetHandler(serviceOverrideComponent);
		}

		private CreationContext RebuildContext(Type handlerType, CreationContext current)
		{
			if (handlerType.ContainsGenericParameters)
			{
				return current;
			}

			return new CreationContext(handlerType, current, true);
		}

		void IReference<IInterceptor>.Attach(ComponentModel component)
		{
			var reference = ReferenceExpressionUtil.BuildReference(serviceOverrideComponent ?? serviceType.FullName);
			component.Parameters.Add(dependencyModel.DependencyKey, reference);
			component.Dependencies.Add(dependencyModel);
		}

		void IReference<IInterceptor>.Detach(ComponentModel component)
		{
			throw new NotSupportedException();
		}

		IInterceptor IReference<IInterceptor>.Resolve(IKernel kernel, CreationContext context)
		{
			var handler = GetInterceptorHandler(kernel);
			if (handler == null)
			{
				throw new DependencyResolverException(string.Format("The interceptor {0} could not be resolved", ToString()));
			}

			if (handler.IsBeingResolvedInContext(context))
			{
				throw new DependencyResolverException(
					string.Format(
						"Cycle detected - interceptor {0} wants to use itself as its interceptor. This usually signifies a bug in custom {1}",
						handler.ComponentModel.Name, typeof(IModelInterceptorsSelector).Name));
			}

			var contextForInterceptor = RebuildContext(GetHandlerType(handler), context);
			return (IInterceptor)handler.Resolve(contextForInterceptor);
		}

		/// <summary>
		///   Gets an <see cref = "InterceptorReference" /> for the component key.
		/// </summary>
		/// <param name = "key">The component key.</param>
		/// <returns>The <see cref = "InterceptorReference" /></returns>
		public static InterceptorReference ForKey(String key)
		{
			return new InterceptorReference(key);
		}

		/// <summary>
		///   Gets an <see cref = "InterceptorReference" /> for the service.
		/// </summary>
		/// <param name = "service">The service.</param>
		/// <returns>The <see cref = "InterceptorReference" /></returns>
		public static InterceptorReference ForType(Type service)
		{
			return new InterceptorReference(service);
		}

		/// <summary>
		///   Gets an <see cref = "InterceptorReference" /> for the service.
		/// </summary>
		/// <typeparam name = "T">The service type.</typeparam>
		/// <returns>The <see cref = "InterceptorReference" /></returns>
		public static InterceptorReference ForType<T>()
		{
			return new InterceptorReference(typeof(T));
		}

		private static string BuildDependencyName()
		{
			return "interceptor-" + Guid.NewGuid().ToString("N");
		}
	}
}