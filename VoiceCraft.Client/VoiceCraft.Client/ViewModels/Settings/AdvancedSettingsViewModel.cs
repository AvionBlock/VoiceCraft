using System;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;

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
            //TODO Locale This!
            notificationService.SendNotification(
                $"Garbage Collection Triggered. Memory Cleared: {Math.Max(previousSnapshot - GC.GetTotalMemory(false), 0) / 1000000}mb",
                Locales.Locales.Notification_Badges_GC);
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
        navigationService.Back();
    }
}