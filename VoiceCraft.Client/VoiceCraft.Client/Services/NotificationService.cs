using Avalonia.Notification;
using System;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.Services
{
    public class NotificationService(INotificationMessageManager notificationMessageManager, SettingsService settingsService)
    {
        public void SendNotification(string message, Action<INotificationMessageButton>? onDismiss = null)
        {
            var notificationSettings = settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundBrush"))
                    .HasBadge("Server")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => {}))
                    .Queue();
            }
        }

        public void SendSuccessNotification(string message, Action<INotificationMessageButton>? onDismiss = null)
        {
            var notificationSettings = settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                    .HasBadge("Server")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => {}))
                    .Queue();
            }
        }

        public void SendErrorNotification(string message, Action<INotificationMessageButton>? onDismiss = null)
        {
            var notificationSettings = settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMs))
                    .Dismiss().WithButton("Dismiss", onDismiss ?? (_ => {}))
                    .Queue();
            }
        }
    }
}