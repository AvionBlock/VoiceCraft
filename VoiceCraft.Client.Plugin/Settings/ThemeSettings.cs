using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class ThemeSettings : Setting
    {
        [ObservableProperty]
        private string _selectedTheme = "Dark";

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}