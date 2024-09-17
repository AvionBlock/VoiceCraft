using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Core.Settings
{
    public partial class ThemeSettings : Setting<ThemeSettings>
    {
        [ObservableProperty]
        public string _selectedTheme = "Default";
    }
}