using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class CreditsViewModel : ViewModelBase
    {
        public override string Title => "Credits";

        [ObservableProperty]
        private string _voicecraftVersion = "N.A.";

        [ObservableProperty]
        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

        [ObservableProperty]
        private string _opusVersion = "N.A."; //OpusInfo.Version();

        [ObservableProperty]
        private string _openAlVersion = "N.A.";
    }
}