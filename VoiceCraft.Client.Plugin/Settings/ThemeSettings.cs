using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class ThemeSettings : Setting<ThemeSettings>
    {
        [ObservableProperty]
        private string _selectedTheme = "Dark";
    }
}