using System;
using Message.Avalonia;
using Message.Avalonia.Models;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Services;

public class NotificationService(
    SettingsService settingsService)
{
    public void SendNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowInformationMessage(Localizer.Get(message), new MessageOptions
        {
            Title = Localizer.Get(title),
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }

    public void SendSuccessNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowSuccessMessage(Localizer.Get(message), new MessageOptions
        {
            Title = Localizer.Get(title),
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }

    public void SendErrorNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        MessageManager.Default.ShowErrorMessage(Localizer.Get(message), new MessageOptions
        {
            Title = Localizer.Get(title),
            Duration = TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)
        });
    }
}