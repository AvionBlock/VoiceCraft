using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class NotificationSettings : Setting<NotificationSettings>
    {
        public override event Action<NotificationSettings>? OnUpdated;

        public ushort DismissDelayMS
        {
            get => _dismissDelayMS;
            set
            {
                _dismissDelayMS = value;
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

        private ushort _dismissDelayMS = 2000;
        private bool _disableNotifications;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}