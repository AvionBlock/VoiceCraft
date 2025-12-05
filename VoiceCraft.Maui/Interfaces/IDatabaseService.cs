using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Interfaces
{
    public interface IDatabaseService
    {
        SettingsModel Settings { get; }
        List<ServerModel> Servers { get; }
        Task Initialization { get; }

        event Action<ServerModel>? OnServerAdded;
        event Action<ServerModel>? OnServerRemoved;

        Task AddServer(ServerModel server);
        Task EditServer(ServerModel server);
        Task RemoveServer(ServerModel server);
        Task SaveAllAsync();
        Task SaveServers();
        Task SaveSettings();
    }
}
