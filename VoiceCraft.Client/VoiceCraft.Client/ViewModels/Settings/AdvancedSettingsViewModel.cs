using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class AdvancedSettingsViewModel(
    NavigationService navigationService,
    NotificationService notificationService,
    SettingsService settingsService) : ViewModelBase
{
    [RelayCommand]
    private void TriggerGc()
    {
        try
        {
            var previousSnapshot = GC.GetTotalMemory(false);
            GC.Collect();
            notificationService.SendNotification(
                "Settings.Advanced.Notification.GC.Badge",
                $"Settings.Advanced.Notification.GC.Triggered:{Math.Max(previousSnapshot - GC.GetTotalMemory(false), 0) / 1000000}");
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                "Settings.Advanced.Notification.GC.Badge",
                ex.Message);
        }
    }

    [RelayCommand]
    private static void Crash()
    {
        throw new Exception("Task failed successfully.");
    }

    [RelayCommand]
    private async Task ResetAllSettings()
    {
        try
        {
            await settingsService.ResetToDefaultsAsync();
            notificationService.SendSuccessNotification(
                "Settings.Advanced.Notification.Reset.Badge",
                "Settings.Advanced.Notification.Reset.Done");
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                "Settings.Advanced.Notification.Reset.Badge",
                ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}
