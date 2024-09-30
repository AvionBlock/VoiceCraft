using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class NotificationSettings : Setting
    {
        [ObservableProperty]
        private ushort _dismissDelayMS = 2000;
        [ObservableProperty]
        private bool _disableNotifications = false;
    }
}
