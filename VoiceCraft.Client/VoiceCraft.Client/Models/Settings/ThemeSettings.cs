using Avalonia.Styling;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class ThemeSettings : Setting
    {
        public string SelectedTheme = ThemeVariant.Dark.ToString();

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}