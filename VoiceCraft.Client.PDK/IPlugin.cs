using Microsoft.Extensions.DependencyInjection;

namespace VoiceCraft.Client.PDK
{
    public interface IPlugin
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        void Load(ServiceCollection serviceCollection);
        void Initialize(IServiceProvider serviceProvider);
    }
}
