using System;
using Avalonia.Controls.Notifications;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Services;

public class NotificationService(
    SettingsService settingsService)
{
    private static WindowNotificationManager? _notificationManager;

    public static void SetNotificationManager(WindowNotificationManager manager)
    {
        _notificationManager = manager;
    }

    public void SendNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        _notificationManager?.Show(new Notification(
            Localizer.Get(title),
            Localizer.Get(message),
            NotificationType.Information,
            TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)));
    }

    public void SendSuccessNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        _notificationManager?.Show(new Notification(
            Localizer.Get(title),
            Localizer.Get(message),
            NotificationType.Success,
            TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)));
    }

    public void SendErrorNotification(string title, string message)
    {
        if (settingsService.NotificationSettings.DisableNotifications) return;
        _notificationManager?.Show(new Notification(
            Localizer.Get(title),
            Localizer.Get(message),
            NotificationType.Error,
            TimeSpan.FromMilliseconds(settingsService.NotificationSettings.DismissDelayMs)));
    }
}