using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class ServersViewModel : ViewModelBase
    {
        public override string Title { get => "Servers"; protected set => throw new NotSupportedException(); }
        private HistoryRouter<ViewModelBase> _router;

        [ObservableProperty]
        private ServerModel? _selectedServer;

        [ObservableProperty]
        private SettingsModel _settings = default!;

        partial void OnSelectedServerChanged(ServerModel? value)
        {
            if (value == null) return;
            var model = _router.GoTo<ServerViewModel>();
            model.SelectedServer = value;
            SelectedServer = null;
        }

        public ServersViewModel(HistoryRouter<ViewModelBase> router, SettingsModel settings)
        {
            _router = router;
            _settings = settings;
        }
    }
}
