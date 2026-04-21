using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels;

public partial class TelemetryConsentViewModel(
    NavigationService navigationService,
    SettingsService settingsService,
    ClientTelemetry clientTelemetry) : ViewModelBase
{
    public override bool DisableBackButton { get; protected set; } = true;

    [RelayCommand]
    private async Task Accept()
    {
        settingsService.TelemetrySettings.Enabled = true;
        settingsService.TelemetrySettings.ConsentShown = true;
        await settingsService.SaveImmediate();
        _ = clientTelemetry.ReportStartupAsync(3);
        navigationService.PopModal();
    }

    [RelayCommand]
    private async Task Decline()
    {
        settingsService.TelemetrySettings.Enabled = false;
        settingsService.TelemetrySettings.ConsentShown = true;
        await settingsService.SaveImmediate();
        navigationService.PopModal();
    }
}
