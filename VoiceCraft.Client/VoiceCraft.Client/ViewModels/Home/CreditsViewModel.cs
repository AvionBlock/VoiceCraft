using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using OpusSharp.Core;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CreditsViewModel : ViewModelBase
{
    //private readonly Bitmap? _defaultIcon = LoadImage("avares://VoiceCraft.Client/Assets/Contributors/vc.png");

    [ObservableProperty] private string _appVersion = string.Empty;

    [ObservableProperty] private string _codec = string.Empty;

    [ObservableProperty] private ObservableCollection<ContributorViewModel> _contributors;

    [ObservableProperty] private string _version = string.Empty;

    public CreditsViewModel()
    {
        _contributors =
        [
            new ContributorViewModel(
                "SineVector241",
                ["Credits.Roles.Author", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/sinePlushie.png")),
            new ContributorViewModel(
                "Miniontoby",
                ["Credits.Roles.Translator", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/minionToby.png")),
            new ContributorViewModel(
                "Unny",
                ["Credits.Roles.Translator"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/unny.png"))
        ];

        Localizer.Instance.OnLanguageChanged += UpdateLocalizations;
        UpdateLocalizations();
    }

    private static Bitmap? LoadImage(string path)
    {
        return AssetLoader.Exists(new Uri(path)) ? new Bitmap(AssetLoader.Open(new Uri(path))) : null;
    }

    private void UpdateLocalizations(string language = "")
    {
        AppVersion =
            Localizer.Get(
                $"Credits.AppVersion:{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A."}");
        Version = Localizer.Get($"Credits.Version:{VoiceCraftClient.Version.ToString()}");
        Codec = Localizer.Get($"Credits.Codec:{OpusInfo.Version()}");
    }
}