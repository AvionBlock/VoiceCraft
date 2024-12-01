using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using OpusSharp.Core;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class CreditsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _voicecraftVersion = "N.A.";

        [ObservableProperty]
        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

        [ObservableProperty]
        private string _opusVersion = OpusInfo.Version();
    }
}