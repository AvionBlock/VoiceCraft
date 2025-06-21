using System;
using Avalonia.Notification;
using Avalonia.Threading;

namespace VoiceCraft.Client.Services;

public class NotificationService(INotificationMessageManager notificationMessageManager, SettingsService settingsService)
{
    public void SendNotification(string badge, string message, Action<INotificationMessageButton>? onDismiss = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var notificationSettings = settingsService.NotificationSettings;
            if (!notificationSettings.DisableNotifications)
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("NotificationAccentBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("NotificationBackgroundBrush"))
                    .HasBadge(badge)
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => { }))
                    .Queue();
        });
    }

    public void SendSuccessNotification(string badge, string message, Action<INotificationMessageButton>? onDismiss = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var notificationSettings = settingsService.NotificationSettings;
            if (!notificationSettings.DisableNotifications)
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("NotificationAccentSuccessBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("NotificationBackgroundSuccessBrush"))
                    .HasBadge(badge)
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => { }))
                    .Queue();
        });
    }

    public void SendErrorNotification(string message, Action<INotificationMessageButton>? onDismiss = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var notificationSettings = settingsService.NotificationSettings;
            if (!notificationSettings.DisableNotifications)
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("NotificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("NotificationBackgroundErrorBrush"))
                    .HasBadge(Locales.Locales.Notification_Badges_Error)
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => { }))
                    .Queue();
        });
    }
}