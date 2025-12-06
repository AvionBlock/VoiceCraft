using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.VoiceCraft;

using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServerDetailsViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;
        private readonly IAudioManager _audioManager;

        [ObservableProperty]
        ServerModel server;

        [ObservableProperty]
        SettingsModel settings;

        [ObservableProperty]
        string pingDetails = "Pinging...";

        public ServerDetailsViewModel(IDatabaseService databaseService, INavigationService navigationService, IAudioManager audioManager)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _audioManager = audioManager;

            Settings = _databaseService.Settings;

            var data = _navigationService.GetNavigationData<ServerModel>();
            if (data != null)
            {
                Server = data;
                _ = Task.Run(async () => PingDetails = await VoiceCraftClient.PingAsync(Server.IP, Server.Port));
            }
            else
            {
                Server = new ServerModel(); // Fallback
            }
        }

        [RelayCommand]
        public async Task Connect()
        {
            if (!await _audioManager.RequestInputPermissions()) return;

            // Validate and fix settings before connecting
            if (Settings.ClientPort < 1025 || Settings.ClientPort > 65535)
            {
                Settings.ClientPort = 8080;
                await _databaseService.SaveSettings();
            }

            if(Settings.JitterBufferSize > 2000 || Settings.JitterBufferSize < 40)
            {
                Settings.JitterBufferSize = 80;
                await _databaseService.SaveSettings();
            }

            if (Settings.InputDevice > _audioManager.GetInputDeviceCount())
            {
                Settings.InputDevice = 0;
            }
            if (Settings.OutputDevice > _audioManager.GetOutputDeviceCount())
            {
                Settings.OutputDevice = 0;
            }

            await _navigationService.NavigateTo(nameof(Voice), Server, $"startMode={true}");
        }
    }
}
