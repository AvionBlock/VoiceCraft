using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels
{
    public partial class ServerViewModel : ViewModelBase
    {
        public override string Title { get => "Server"; protected set => throw new NotSupportedException(); }
        private HistoryRouter<ViewModelBase> _router;

        [ObservableProperty]
        private ServerModel _selectedServer = default!;

        [ObservableProperty]
        private string _pingInfo = "Pinging...";

        public ServerViewModel(HistoryRouter<ViewModelBase> router)
        {
            _router = router;
        }

        [RelayCommand]
        public void Cancel()
        {
            PingInfo = "Pinging...";
            _router.Back();
        }
    }
}
