using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class NotificationSettings : Setting
    {
        public ushort DismissDelayMS = 2000;
        public bool DisableNotifications = false;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}