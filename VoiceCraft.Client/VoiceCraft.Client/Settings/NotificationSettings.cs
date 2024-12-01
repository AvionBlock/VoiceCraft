using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Settings
{
    public partial class NotificationSettings : Setting
    {
        [ObservableProperty]
        private ushort _dismissDelayMS = 2000;
        [ObservableProperty]
        private bool _disableNotifications = false;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
