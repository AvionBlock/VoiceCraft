using Microsoft.Extensions.DependencyInjection;
using System;

namespace VoiceCraft.Core
{
    public interface IPlugin
    {
        void Load(ServiceCollection serviceCollection);
        void Initialize(IServiceProvider serviceProvider);
    }
}
