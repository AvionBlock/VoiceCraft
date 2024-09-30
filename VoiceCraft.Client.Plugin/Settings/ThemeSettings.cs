using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.Plugin.Settings
{
    public partial class ThemeSettings : Setting
    {
        [ObservableProperty]
        private string _selectedTheme = "Dark";
    }
}