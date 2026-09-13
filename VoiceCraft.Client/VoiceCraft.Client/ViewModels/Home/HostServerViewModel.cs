using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class HostServerViewModel(IBackgroundService backgroundService) : ViewModelBase
{
    [RelayCommand]
    private void StartServer()
    {
        backgroundService.StartServiceAsync<VoiceCraftServerService>((x, updateTitle, updateDescription) =>
        {
            var runtimeOptions = new Server.RuntimeOptions()
            {
                DisableCommands = true,
                DisableColor = true,
                DisableAnsi = true,
            };
            x.RunAsync(runtimeOptions).GetAwaiter().GetResult();
        });
    }
}