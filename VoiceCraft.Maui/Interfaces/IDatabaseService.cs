using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Interfaces;

/// <summary>
/// Interface for database operations including settings and server management.
/// </summary>
public interface IDatabaseService
{
    /// <summary>Gets the application settings.</summary>
    SettingsModel Settings { get; }

    /// <summary>Gets the list of saved servers.</summary>
    List<ServerModel> Servers { get; }

    /// <summary>Gets the initialization task.</summary>
    Task Initialization { get; }

    /// <summary>Occurs when a server is added.</summary>
    event Action<ServerModel>? OnServerAdded;

    /// <summary>Occurs when a server is removed.</summary>
    event Action<ServerModel>? OnServerRemoved;

    /// <summary>Adds a new server.</summary>
    Task AddServer(ServerModel server);

    /// <summary>Edits an existing server.</summary>
    Task EditServer(ServerModel server);

    /// <summary>Removes a server.</summary>
    Task RemoveServer(ServerModel server);

    /// <summary>Saves all data to persistent storage.</summary>
    Task SaveAllAsync();

    /// <summary>Saves servers to persistent storage.</summary>
    Task SaveServers();

    /// <summary>Saves settings to persistent storage.</summary>
    Task SaveSettings();
}

