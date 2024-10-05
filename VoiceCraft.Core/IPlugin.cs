using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public interface IPlugin
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }

        int Priority { get; }
        IEnumerable<Guid> ClientDependencies { get; }
        IEnumerable<Guid> ServerDependencies { get; }

        void Load(ServiceCollection serviceCollection);
        void Initialize(IServiceProvider serviceProvider);
    }
}
