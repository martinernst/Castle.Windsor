﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

namespace Castle.MicroKernel.Registration
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	///   Describes the source of types to register.
	/// </summary>
	public abstract class FromDescriptor : IRegistration
	{
		private readonly Predicate<Type> additionalFilters;
		private readonly IList<BasedOnDescriptor> criterias;
		private bool allowMultipleMatches;

		internal FromDescriptor(Predicate<Type> additionalFilters)
		{
			this.additionalFilters = additionalFilters;
			allowMultipleMatches = false;
			criterias = new List<BasedOnDescriptor>();
		}

		protected abstract IEnumerable<Type> SelectedTypes(IKernel kernel);

		/// <summary>
		///   Allows a type to be registered multiple times.
		/// </summary>
		public FromDescriptor AllowMultipleMatches()
		{
			allowMultipleMatches = true;
			return this;
		}

		/// <summary>
		///   Returns the descriptor for accepting a type.
		/// </summary>
		/// <typeparam name = "T">The base type.</typeparam>
		/// <returns>The descriptor for the type.</returns>
		public BasedOnDescriptor BasedOn<T>()
		{
			return BasedOn(typeof(T));
		}

		/// <summary>
		///   Returns the descriptor for accepting a type.
		/// </summary>
		/// <param name = "basedOn">The base type.</param>
		/// <returns>The descriptor for the type.</returns>
		public BasedOnDescriptor BasedOn(Type basedOn)
		{
			var descriptor = new BasedOnDescriptor(basedOn, this, additionalFilters);
			criterias.Add(descriptor);
			return descriptor;
		}

		/// <summary>
		///   Returns the descriptor for accepting any type from given solutions.
		/// </summary>
		/// <returns></returns>
		public BasedOnDescriptor Pick()
		{
			return BasedOn<object>();
		}

		/// <summary>
		///   Returns the descriptor for accepting a type based on a condition.
		/// </summary>
		/// <param name = "accepted">The accepting condition.</param>
		/// <returns>The descriptor for the type.</returns>
		public BasedOnDescriptor Where(Predicate<Type> accepted)
		{
			var descriptor = new BasedOnDescriptor(typeof(object), this, additionalFilters).If(accepted);
			criterias.Add(descriptor);
			return descriptor;
		}

		void IRegistration.Register(IKernel kernel)
		{
			if (criterias.Count == 0)
			{
				return;
			}

			foreach (var type in SelectedTypes(kernel))
			{
				foreach (var criteria in criterias)
				{
					if (criteria.TryRegister(type, kernel) && !allowMultipleMatches)
					{
						break;
					}
				}
			}
		}
	}
}