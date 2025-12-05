using OpusSharp.Core;
using SimpleToolkit.Core;

namespace VoiceCraft.Maui
{
    public partial class App : Application
    {
        public static string Version = AppInfo.Current.VersionString;
        public static string OpusVersion = OpusInfo.Version();
        
        public App(AppShell shell)
        {
            InitializeComponent();

            MainPage = shell;
        }
    }
}