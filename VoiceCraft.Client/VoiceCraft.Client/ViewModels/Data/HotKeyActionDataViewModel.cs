using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class HotKeyActionDataViewModel(HotKeyAction hotKeyAction, string keybind) : ObservableObject
{
    public HotKeyAction Action { get; } = hotKeyAction;
    [ObservableProperty] private string _keybind = keybind.Replace("\0", " + ");
    [ObservableProperty] private string _title = $"Settings.HotKey.Actions.{hotKeyAction.Title}";
}
