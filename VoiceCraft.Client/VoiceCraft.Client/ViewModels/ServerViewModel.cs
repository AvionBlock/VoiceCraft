using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels
{
    public partial class ServerViewModel : ViewModelBase
    {
        public override string Title { get => SelectedServer?.Name ?? string.Empty; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ServerModel _selectedServer = default!;

        [ObservableProperty]
        private string _pingInfo = "Pinging...";

        [RelayCommand]
        public void Cancel()
        {
            PingInfo = "Pinging...";
        }
    }
}
