using Microsoft.Extensions.DependencyInjection;

namespace VoiceCraft.Client.PDK
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
