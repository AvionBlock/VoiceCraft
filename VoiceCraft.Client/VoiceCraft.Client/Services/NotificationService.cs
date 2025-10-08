using System;
using Avalonia.Threading;
using Message.Avalonia;
using Message.Avalonia.Models;

namespace VoiceCraft.Client.Services;

public class NotificationService(
    SettingsService settingsService)
{
    public void SendNotification(string message, string? title = null)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        Dispatcher.UIThread.Invoke(() =>
        {
            MessageManager.Default.ShowInformationMessage(message, new MessageOptions()
            {
                Title = title,
                Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
            });
        });
    }

    public void SendSuccessNotification(string message, string? title = null)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        Dispatcher.UIThread.Invoke(() =>
        {
            MessageManager.Default.ShowSuccessMessage(message, new MessageOptions()
            {
                Title = title,
                Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
            });
        });
    }

    public void SendErrorNotification(string message, string? title = null)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        Dispatcher.UIThread.Invoke(() =>
        {
            MessageManager.Default.ShowErrorMessage(message, new MessageOptions()
            {
                Title = title,
                Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
            });
        });
    }
}