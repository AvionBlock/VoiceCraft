using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public interface IClientPlugin
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        int Priority { get; }
        IEnumerable<PluginDependency> ServerDependencies { get; }

        void Initialize(ServiceCollection serviceCollection);
        void Execute(IServiceProvider serviceProvider);
    }
}
