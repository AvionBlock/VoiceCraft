using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Services;
using VoiceCraft.Core.Settings;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title { get => "Servers"; protected set => throw new NotSupportedException(); }
        private HistoryRouter<ViewModelBase> _router;
        private SettingsService _settings;

        [ObservableProperty]
        private ServersSettings _servers;

        [ObservableProperty]
        private Server? _selectedServer;

        public ServersViewModel(HistoryRouter<ViewModelBase> router, SettingsService settings)
        {
            _router = router;
            _settings = settings;
            _servers = settings.Get<ServersSettings>(App.SettingsId);
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
            /*
            if (server == null) return; //Somehow can be null.
            var model = _router.GoTo<EditServerViewModel>();
            model.Server = server;
            */
        }
    }
}