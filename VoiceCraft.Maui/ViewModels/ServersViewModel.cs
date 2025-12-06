using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Views.Desktop;

namespace VoiceCraft.Maui.ViewModels;

/// <summary>
/// ViewModel for the servers list page.
/// </summary>
public partial class ServersViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<ServerModel> _servers = [];

    [ObservableProperty]
    private SettingsModel _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServersViewModel"/> class.
    /// </summary>
    public ServersViewModel(IDatabaseService databaseService, INavigationService navigationService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;

        LoadServers();
        _settings = _databaseService.Settings;

        _databaseService.OnServerAdded += ServerAdded;
        _databaseService.OnServerRemoved += ServerRemoved;
    }

    private async void LoadServers()
    {
        await _databaseService.Initialization;
        foreach (var server in _databaseService.Servers)
        {
            Servers.Add(server);
        }
    }

    [RelayCommand]
    private async Task DeleteServer(ServerModel server)
    {
        await _databaseService.RemoveServer(server);
    }

    [RelayCommand]
    private async Task GoToAddServer()
    {
        await _navigationService.NavigateTo(nameof(AddServer));
    }

    [RelayCommand]
    private async Task GoToEditServer(ServerModel server)
    {
        await _navigationService.NavigateTo(nameof(EditServer), server);
    }

    [RelayCommand]
    private async Task GoToServer(ServerModel server)
    {
        await _navigationService.NavigateTo(nameof(ServerDetails), server);
    }

    private void ServerAdded(ServerModel server) => Servers.Add(server);

    private void ServerRemoved(ServerModel server) => Servers.Remove(server);
}

