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

namespace Castle.MicroKernel.Tests.SubContainers
{
	using System;
	using System.Collections.Generic;

	using Castle.Core;
	using Castle.MicroKernel.Registration;
	using Castle.MicroKernel.Tests.ClassComponents;

	using CastleTests.Components;

	using NUnit.Framework;

	/// <summary>
	///   Summary description for SubContainersTestCase.
	/// </summary>
	[TestFixture]
	public class SubContainersTestCase
	{
		[SetUp]
		public void Init()
		{
			kernel = new DefaultKernel();
		}

		[TearDown]
		public void Dispose()
		{
			kernel.Dispose();
		}

		private IKernel kernel;

		/// <summary>
		///   collects events in an array list, used for ensuring we are cleaning up the parent kernel
		///   event subscriptions correctly.
		/// </summary>
		private class EventsCollector
		{
			public const string Added = "added";
			public const string Removed = "removed";

			private readonly List<string> events;
			private readonly object expectedSender;

			public EventsCollector(object expectedSender)
			{
				this.expectedSender = expectedSender;
				events = new List<string>();
			}

			public List<string> Events
			{
				get { return events; }
			}

			public void AddedAsChildKernel(object sender, EventArgs e)
			{
				Assert.AreEqual(expectedSender, sender);
				events.Add(Added);
			}

			public void RemovedAsChildKernel(object sender, EventArgs e)
			{
				Assert.AreEqual(expectedSender, sender);
				events.Add(Removed);
			}
		}

		[Test]
		[ExpectedException(typeof(KernelException),
			ExpectedMessage =
				"You can not change the kernel parent once set, use the RemoveChildKernel and AddChildKernel methods together to achieve this."
			)]
		public void AddChildKernelToTwoParentsThrowsException()
		{
			IKernel kernel2 = new DefaultKernel();

			IKernel subkernel = new DefaultKernel();

			kernel.AddChildKernel(subkernel);
			Assert.AreEqual(kernel, subkernel.Parent);

			kernel2.AddChildKernel(subkernel);
		}

		[Test]
		public void ChildDependenciesIsSatisfiedEvenWhenComponentTakesLongToBeAddedToParentContainer()
		{
			var container = new DefaultKernel();
			var childContainer = new DefaultKernel();

			container.AddChildKernel(childContainer);
			childContainer.Register(Component.For(typeof(UsesIEmptyService)).Named("component"));

			container.Register(
				Component.For(typeof(IEmptyService)).ImplementedBy(typeof(EmptyServiceA)).Named("service1"));

			var comp = childContainer.Resolve<UsesIEmptyService>();
		}

		[Test]
		public void ChildDependenciesSatisfiedAmongContainers()
		{
			IKernel subkernel = new DefaultKernel();

			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));
			kernel.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));

			kernel.AddChildKernel(subkernel);
			subkernel.Register(Component.For(typeof(DefaultSpamService)).Named("spamservice"));

			var spamservice = subkernel.Resolve<DefaultSpamService>("spamservice");

			Assert.IsNotNull(spamservice);
			Assert.IsNotNull(spamservice.MailSender);
			Assert.IsNotNull(spamservice.TemplateEngine);
		}

		[Test]
		public void ChildKernelFindsAndCreateParentComponent()
		{
			IKernel subkernel = new DefaultKernel();

			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			kernel.AddChildKernel(subkernel);

			Assert.IsTrue(subkernel.HasComponent(typeof(DefaultTemplateEngine)));
			Assert.IsNotNull(subkernel.Resolve<DefaultTemplateEngine>());
		}

		[Test]
		public void ChildKernelOverloadsParentKernel1()
		{
			var instance1 = new DefaultTemplateEngine();
			var instance2 = new DefaultTemplateEngine();

			// subkernel added with already registered components that overload parent components.

			IKernel subkernel = new DefaultKernel();
			subkernel.Register(Component.For<DefaultTemplateEngine>().Named("engine").Instance(instance1));
			Assert.AreEqual(instance1, subkernel.Resolve<DefaultTemplateEngine>("engine"));

			kernel.Register(Component.For<DefaultTemplateEngine>().Named("engine").Instance(instance2));
			Assert.AreEqual(instance2, kernel.Resolve<DefaultTemplateEngine>("engine"));

			kernel.AddChildKernel(subkernel);
			Assert.AreEqual(instance1, subkernel.Resolve<DefaultTemplateEngine>("engine"));
			Assert.AreEqual(instance2, kernel.Resolve<DefaultTemplateEngine>("engine"));
		}

		[Test]
		public void ChildKernelOverloadsParentKernel2()
		{
			var instance1 = new DefaultTemplateEngine();
			var instance2 = new DefaultTemplateEngine();

			IKernel subkernel = new DefaultKernel();
			kernel.AddChildKernel(subkernel);

			// subkernel added first, then populated with overloaded components after

			kernel.Register(Component.For<DefaultTemplateEngine>().Named("engine").Instance(instance2));
			Assert.AreEqual(instance2, kernel.Resolve<DefaultTemplateEngine>("engine"));
			Assert.AreEqual(instance2, subkernel.Resolve<DefaultTemplateEngine>("engine"));

			subkernel.Register(Component.For<DefaultTemplateEngine>().Named("engine").Instance(instance1));
			Assert.AreEqual(instance1, subkernel.Resolve<DefaultTemplateEngine>("engine"));
			Assert.AreEqual(instance2, kernel.Resolve<DefaultTemplateEngine>("engine"));
		}

		[Test]
		public void DependenciesSatisfiedAmongContainers()
		{
			IKernel subkernel = new DefaultKernel();

			kernel.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));
			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			kernel.AddChildKernel(subkernel);

			subkernel.Register(Component.For(typeof(DefaultSpamService)).Named("spamservice"));

			var spamservice = subkernel.Resolve<DefaultSpamService>("spamservice");

			Assert.IsNotNull(spamservice);
			Assert.IsNotNull(spamservice.MailSender);
			Assert.IsNotNull(spamservice.TemplateEngine);
		}

		[Test]
		public void DependenciesSatisfiedAmongContainersUsingEvents()
		{
			IKernel subkernel = new DefaultKernel();

			subkernel.Register(Component.For(typeof(DefaultSpamServiceWithConstructor)).Named("spamservice"));

			kernel.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));
			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			kernel.AddChildKernel(subkernel);

			var spamservice =
				subkernel.Resolve<DefaultSpamServiceWithConstructor>("spamservice");

			Assert.IsNotNull(spamservice);
			Assert.IsNotNull(spamservice.MailSender);
			Assert.IsNotNull(spamservice.TemplateEngine);
		}

		[Test]
		[ExpectedException(typeof(ComponentNotFoundException))]
		public void ParentKernelFindsAndCreateChildComponent()
		{
			IKernel subkernel = new DefaultKernel();

			subkernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			kernel.AddChildKernel(subkernel);

			Assert.IsFalse(kernel.HasComponent(typeof(DefaultTemplateEngine)));
			object engine = kernel.Resolve<DefaultTemplateEngine>();
		}

		[Test]
		public void RemoveChildKernelCleansUp()
		{
			IKernel subkernel = new DefaultKernel();
			var eventCollector = new EventsCollector(subkernel);
			subkernel.RemovedAsChildKernel += eventCollector.RemovedAsChildKernel;
			subkernel.AddedAsChildKernel += eventCollector.AddedAsChildKernel;

			kernel.AddChildKernel(subkernel);
			Assert.AreEqual(kernel, subkernel.Parent);
			Assert.AreEqual(1, eventCollector.Events.Count);
			Assert.AreEqual(EventsCollector.Added, eventCollector.Events[0]);

			kernel.RemoveChildKernel(subkernel);
			Assert.IsNull(subkernel.Parent);
			Assert.AreEqual(2, eventCollector.Events.Count);
			Assert.AreEqual(EventsCollector.Removed, eventCollector.Events[1]);
		}

		[Test]
		public void RemovingChildKernelUnsubscribesFromParentEvents()
		{
			IKernel subkernel = new DefaultKernel();
			var eventCollector = new EventsCollector(subkernel);
			subkernel.RemovedAsChildKernel += eventCollector.RemovedAsChildKernel;
			subkernel.AddedAsChildKernel += eventCollector.AddedAsChildKernel;

			kernel.AddChildKernel(subkernel);
			kernel.RemoveChildKernel(subkernel);
			kernel.AddChildKernel(subkernel);
			kernel.RemoveChildKernel(subkernel);

			Assert.AreEqual(4, eventCollector.Events.Count);
			Assert.AreEqual(EventsCollector.Added, eventCollector.Events[0]);
			Assert.AreEqual(EventsCollector.Removed, eventCollector.Events[1]);
			Assert.AreEqual(EventsCollector.Added, eventCollector.Events[2]);
			Assert.AreEqual(EventsCollector.Removed, eventCollector.Events[3]);
		}

		[Test]
		[Ignore(
			"Support for this was removed due to issues with scoping (SimpleComponent1 would become visible from parent container)."
			)]
		public void Requesting_parent_component_with_child_dependency_from_child_component()
		{
			var subkernel = new DefaultKernel();
			kernel.AddChildKernel(subkernel);

			kernel.Register(Component.For<UsesSimpleComponent1>());
			subkernel.Register(Component.For<SimpleComponent1>());

			subkernel.Resolve<UsesSimpleComponent1>();
		}

		[Test]
		public void SameLevelDependenciesSatisfied()
		{
			IKernel child = new DefaultKernel();

			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));
			kernel.Register(Component.For(typeof(DefaultSpamService)).Named("spamservice"));

			kernel.AddChildKernel(child);

			child.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));

			var spamservice = child.Resolve<DefaultSpamService>("spamservice");

			Assert.IsNotNull(spamservice);
			Assert.IsNotNull(spamservice.MailSender);
			Assert.IsNotNull(spamservice.TemplateEngine);
		}

		[Test]
		public void Singleton_WithNonSingletonDependencies_DoesNotReResolveDependencies()
		{
			kernel.Register(Component.For(typeof(DefaultSpamService)).Named("spamservice"));
			kernel.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));

			IKernel subkernel1 = new DefaultKernel();
			subkernel1.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));
			kernel.AddChildKernel(subkernel1);

			IKernel subkernel2 = new DefaultKernel();
			subkernel2.Register(
				Component.For(typeof(DefaultTemplateEngine)).Named("templateengine").LifeStyle.Is(LifestyleType.Transient));
			kernel.AddChildKernel(subkernel2);

			var templateengine1 = subkernel1.Resolve<DefaultTemplateEngine>("templateengine");
			var spamservice1 = subkernel1.Resolve<DefaultSpamService>("spamservice");
			Assert.IsNotNull(spamservice1);
			Assert.AreEqual(spamservice1.TemplateEngine.Key, templateengine1.Key);

			var templateengine2 = subkernel2.Resolve<DefaultTemplateEngine>("templateengine");
			var spamservice2 = subkernel2.Resolve<DefaultSpamService>("spamservice");
			Assert.IsNotNull(spamservice2);
			Assert.AreEqual(spamservice1, spamservice2);
			Assert.AreEqual(spamservice1.TemplateEngine.Key, templateengine1.Key);
			Assert.AreNotEqual(spamservice2.TemplateEngine.Key, templateengine2.Key);
		}

		[Test]
		[Ignore(
			"Support for this was removed due to issues with scoping (SimpleComponent1 would become visible from parent container)."
			)]
		public void Three_level_hierarchy([Values(0, 1, 2)] int parentComponentContainer,
		                                  [Values(0, 1, 2)] int childComponentContainer)
		{
			var subKernel = new DefaultKernel();
			var subSubKernel = new DefaultKernel();
			kernel.AddChildKernel(subKernel);
			subKernel.AddChildKernel(subSubKernel);
			var containers = new[]
			{
				kernel,
				subKernel,
				subSubKernel
			};

			containers[parentComponentContainer].Register(Component.For<UsesSimpleComponent1>());
			containers[childComponentContainer].Register(Component.For<SimpleComponent1>());

			subSubKernel.Resolve<UsesSimpleComponent1>();
		}

		[Test]
		public void UseChildComponentsForParentDependenciesWhenRequestedFromChild()
		{
			IKernel subkernel = new DefaultKernel();

			kernel.Register(Component.For(typeof(DefaultSpamService)).Named("spamservice").LifeStyle.Is(LifestyleType.Transient));
			kernel.Register(Component.For(typeof(DefaultMailSenderService)).Named("mailsender"));
			kernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			kernel.AddChildKernel(subkernel);
			subkernel.Register(Component.For(typeof(DefaultTemplateEngine)).Named("templateengine"));

			var templateengine = kernel.Resolve<DefaultTemplateEngine>("templateengine");
			var sub_templateengine = subkernel.Resolve<DefaultTemplateEngine>("templateengine");

			var spamservice = subkernel.Resolve<DefaultSpamService>("spamservice");
			Assert.AreNotEqual(spamservice.TemplateEngine, templateengine);
			Assert.AreEqual(spamservice.TemplateEngine, sub_templateengine);

			spamservice = kernel.Resolve<DefaultSpamService>("spamservice");
			Assert.AreNotEqual(spamservice.TemplateEngine, sub_templateengine);
			Assert.AreEqual(spamservice.TemplateEngine, templateengine);
		}
	}
}