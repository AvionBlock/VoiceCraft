using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class AddServerViewModel : ViewModelBase
    {
        public override string Title { get => "Add Server"; protected set => throw new NotSupportedException(); }
        public event EventHandler<ServerModel>? OnServerAdded;

        [ObservableProperty]
        private SettingsModel _settings;

        [ObservableProperty]
        private ServerModel _server = new ServerModel("", "", 9050, 0);

        public AddServerViewModel(SettingsModel settings)
        {
            _settings = settings;
        }

        [RelayCommand]
        public void AddServer()
        {
            Settings.Servers.Add(Server);
            Server = new ServerModel("", "", 9050, 0);
            _ = Settings.SaveAsync();
            OnServerAdded?.Invoke(this, Server);
        }
    }
}
