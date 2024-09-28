using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.Views;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title => "Servers";

        private NavigationService _navigator;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server? _selectedServer;

        public ServersViewModel(NavigationService navigator, SettingsService settings)
        {
            _navigator = navigator;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(Plugin.PluginId);
        }

        [RelayCommand]
        public async Task DeleteServer(Server server)
        {
            Servers.RemoveServer(server);
            await _settings.SaveAsync();
        }

        [RelayCommand]
        public void EditServer(Server? server)
        {
            if (server == null) return; //Somehow can be null.
            var model = _navigator.NavigateTo<EditServerView>();
            model.Server = server;
        }
    }
}