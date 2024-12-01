using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Settings
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
