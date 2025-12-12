using System;
using Message.Avalonia;
using Message.Avalonia.Models;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Services;

public class NotificationService(
    SettingsService settingsService)
{
    public void SendNotification(string message, string? title = null)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowInformationMessage(message, new MessageOptions()
        {
            Title = title,
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }

    public void SendSuccessNotification(string message, string? title = null)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowSuccessMessage(message, new MessageOptions()
        {
            Title = title,
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }

    public void SendErrorNotification(string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowErrorMessage(message, new MessageOptions()
        {
            Title = Localizer.Get("Notification.Badges.Error"),
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }
}