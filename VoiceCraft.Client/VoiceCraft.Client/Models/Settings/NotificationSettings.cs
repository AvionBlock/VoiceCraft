using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class NotificationSettings : Setting<NotificationSettings>
    {
        public override event Action<NotificationSettings>? OnUpdated;

        public ushort DismissDelayMs
        {
            get => _dismissDelayMs;
            set
            {
                _dismissDelayMs = value;
                OnUpdated?.Invoke(this);
            }
        }

        public bool DisableNotifications
        {
            get => _disableNotifications;
            set
            {
                _disableNotifications = value;
                OnUpdated?.Invoke(this);
            }
        }

        private ushort _dismissDelayMs = 2000;
        private bool _disableNotifications;

        public override object Clone()
        {
            var clone = (NotificationSettings)MemberwiseClone();
            clone.OnUpdated = null;
            return clone;
        }
    }
}