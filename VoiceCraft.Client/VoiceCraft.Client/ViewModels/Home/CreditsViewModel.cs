using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Jeek.Avalonia.Localization;
using OpusSharp.Core;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Network;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CreditsViewModel : ViewModelBase
{
    //private readonly Bitmap? _defaultIcon = LoadImage("avares://VoiceCraft.Client/Assets/Contributors/vc.png");

    [ObservableProperty] private string _appVersion = string.Empty;
    
    [ObservableProperty] private string _version = string.Empty;

    [ObservableProperty] private string _codec = string.Empty;

    [ObservableProperty] private ObservableCollection<Contributor> _contributors;

    public CreditsViewModel()
    {
        _contributors =
        [
            new Contributor(
                "SineVector241",
                ["Credits.Roles.Author", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/sinePlushie.png")),
            new Contributor(
                "Miniontoby",
                ["Credits.Roles.Translator", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/minionToby.png")),
            new Contributor(
                "Unny",
                ["Credits.Roles.Translator"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/unny.png"))
        ];
        
        Localizer.LanguageChanged += (_, _) => UpdateLocalizations();
        UpdateLocalizations();
    }

    private static Bitmap? LoadImage(string path)
    {
        return AssetLoader.Exists(new Uri(path)) ? new Bitmap(AssetLoader.Open(new Uri(path))) : null;
    }
    
    private void UpdateLocalizations()
    {
        AppVersion = Locales.Locales.Credits_AppVersion.Replace("{version}", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.");
        Version = Locales.Locales.Credits_Version.Replace("{version}", VoiceCraftClient.Version.ToString());
        Codec = Locales.Locales.Credits_Codec.Replace("{version}", OpusInfo.Version());
    }
}