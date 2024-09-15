using CommunityToolkit.Mvvm.ComponentModel;
using OpusSharp.Core;
using System;
using System.Reflection;

namespace VoiceCraft.Client.Ignore.HomeViews
{
    public partial class CreditsViewModel : ViewModelBase
    {
        public override string Title { get => "Credits"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private string _voicecraftVersion = "N.A.";

        [ObservableProperty]
        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

        [ObservableProperty]
        private string _opusVersion = OpusInfo.Version();

        [ObservableProperty]
        private string _openAlVersion = "N.A.";
    }
}
