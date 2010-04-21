// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Web.Actors.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using Actions;
	using Channels;
	using Extensions;
	using Internal;
	using Logging;
	using Reflection;

	public class StandardActorConfiguration<TActor> :
		ActorConfiguration<TActor>
		where TActor : class
	{
		private static readonly ILogger _log = Logger.GetLogger<StandardActorConfiguration<TActor>>();
		private ActorInstanceProvider<TActor> _actorInstanceProvider;

		private Action<RouteConfiguration> _configure;
		private ActionQueueProvider _queueProvider;

		public StandardActorConfiguration()
		{
			_queueProvider = ThreadPoolQueueProvider;
			_actorInstanceProvider = new TransientActorInstanceProvider<TActor>(_queueProvider);
			_configure = DefaultConfigureAction;
		}

		public ActorConfigurator All()
		{
			var actions = new List<Action<RouteConfiguration>>();

			Type actorType = typeof (TActor);

			actorType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(x => x.PropertyType.Implements<Channel>())
				.Each(property =>
					{
						Type inputType = property.PropertyType.GetGenericTypeDeclarations(typeof (Channel<>)).Single();

						_log.Debug(x => x.Write("Configuring Channel<{0}> for {1}", inputType.Name, typeof (TActor).Name));

						actions.Add(configurator =>
							{
								var args = new object[] {configurator, property};

								this.FastInvoke(new[] {inputType}, "AddRoute", args);
							});
					});

			if (actions.Count == 0)
				throw new ArgumentException("No channels were found for actor: " + typeof (TActor).Name);

			_configure = configurator => actions.Each(r => r(configurator));

			return this;
		}

		public ActorConfigurator PerThread()
		{
			if (_actorInstanceProvider.GetType() == typeof (ThreadStaticActorInstanceProvider<TActor>))
				return this;

			_actorInstanceProvider = new ThreadStaticActorInstanceProvider<TActor>(_actorInstanceProvider);
			return this;
		}

		public ActorConfigurator<TActor> Channel<TChannel>(Expression<Func<TActor, TChannel>> expression)
		{
			PropertyInfo property = expression.GetMemberPropertyInfo();

			Type inputType = property.PropertyType.GetGenericTypeDeclarations(typeof (Channel<>)).Single();

			_log.Debug(x => x.Write("Configuring Channel<{0}> for {1}", inputType.Name, typeof (TActor).Name));

			_configure = configurator =>
				{
					var args = new object[] {configurator, property};

					this.FastInvoke(new[] {inputType}, "AddRoute", args);

					// TODO need to make this keep track of each channel that needs to be added
				};

			return this;
		}

		public void Apply(RouteConfiguration configuration)
		{
			_configure(configuration);
		}

		private void AddRoute<TInput>(RouteConfiguration configuration, PropertyInfo property)
		{
			var channelProvider = new ActorChannelProvider<TActor, TInput>(_actorInstanceProvider, property);

			configuration.AddRoute<TActor, TInput>(channelProvider, property);
		}

		private static void DefaultConfigureAction(RouteConfigurator obj)
		{
			throw new InvalidOperationException("No channels have been specified for the actor: " + typeof (TActor).Name);
		}

		private static ActionQueue ThreadPoolQueueProvider()
		{
			return new ThreadPoolActionQueue();
		}
	}
}