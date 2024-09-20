using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Core.Settings
{
    public partial class ThemeSettings : Setting<ThemeSettings>
    {
        [ObservableProperty]
        private string _selectedTheme = "Dark";
    }
}