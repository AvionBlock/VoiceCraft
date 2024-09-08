using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title { get => "Servers"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ObservableCollection<ServerModel> _servers = new ObservableCollection<ServerModel>();

        public ServersViewModel()
        {
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
            _servers.Add(new ServerModel("Test", "127.0.0.1", 9050, 0));
        }
    }
}
