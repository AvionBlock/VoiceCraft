namespace VoiceCraft.Plugin
{
    public interface IPlugin
    {
        Guid PluginId { get; }
        public void Load(ServiceCollection services);
        public void Initialize(IServiceCollection services);
    }
}
