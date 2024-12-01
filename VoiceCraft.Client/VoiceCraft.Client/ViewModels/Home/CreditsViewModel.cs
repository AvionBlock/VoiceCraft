using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class CreditsViewModel : ViewModelBase
    {
        public override string Title { get; protected set; } = "Credits";

        [ObservableProperty]
        private string _voicecraftVersion = "N.A.";

        [ObservableProperty]
        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

        [ObservableProperty]
        private string _opusVersion = OpusInfo.Version();
    }
}
