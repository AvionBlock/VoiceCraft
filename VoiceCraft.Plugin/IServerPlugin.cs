using Microsoft.Extensions.DependencyInjection;

namespace VoiceCraft.Plugin
{
    public interface IServerPlugin
    {
        Guid PluginId { get; }
        public void Load(ServiceCollection services);
        public void Initialize(IServiceCollection services);
    }
}
