using Avalonia.Notification;
using System;
using VoiceCraft.Client.Settings;

namespace VoiceCraft.Client.Services
{
    public class NotificationService
    {
        private readonly INotificationMessageManager _notificationMessageManager;
        private readonly SettingsService _settingsService;

        public NotificationService(INotificationMessageManager notificationMessageManager, SettingsService settingsService)
        {
            _notificationMessageManager = notificationMessageManager;
            _settingsService = settingsService;
        }

        public void SendNotification(string message)
        {
            var notificationSettings = _settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                _notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundBrush"))
                    .HasBadge("Server")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }

        public void SendSuccessNotification(string message)
        {
            var notificationSettings = _settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                _notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                    .HasBadge("Server")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }

        public void SendErrorNotification(string message)
        {
            var notificationSettings = _settingsService.Get<NotificationSettings>();
            if (!notificationSettings.DisableNotifications)
            {
                _notificationMessageManager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(message)
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }
    }
}
