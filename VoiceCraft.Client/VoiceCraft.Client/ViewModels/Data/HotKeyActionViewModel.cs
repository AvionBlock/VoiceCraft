using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class HotKeyActionViewModel(HotKeyAction hotKeyAction, string keybind) : ObservableObject
{
    [ObservableProperty] private string _keybind = keybind.Replace("\0", " + ");
    [ObservableProperty] private string _title = $"Settings.HotKey.Actions.{hotKeyAction.Title}";
}