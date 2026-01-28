using System;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Settings;

public partial class AdvancedSettingsViewModel(
    NavigationService navigationService,
    NotificationService notificationService) : ViewModelBase
{
    [RelayCommand]
    private void TriggerGc()
    {
        try
        {
            var previousSnapshot = GC.GetTotalMemory(false);
            GC.Collect();
            notificationService.SendNotification(
                Localizer.Get(
                    $"Notification.GC.Triggered:{Math.Max(previousSnapshot - GC.GetTotalMemory(false), 0) / 1000000}"),
                Localizer.Get("Notification.GC.Badge"));
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(ex.Message);
        }
    }

    [RelayCommand]
    private static void Crash()
    {
        throw new Exception("Task failed successfully.");
    }

    [RelayCommand]
    private void Cancel()
    {
        if (DisableBackButton) return;
        navigationService.Back();
    }
}