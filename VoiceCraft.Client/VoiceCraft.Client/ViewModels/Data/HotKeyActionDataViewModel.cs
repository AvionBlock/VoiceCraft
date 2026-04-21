using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class HotKeyActionDataViewModel(HotKeyAction hotKeyAction, string keybind) : ObservableObject
{
    public HotKeyAction Action { get; } = hotKeyAction;
    [ObservableProperty] public partial string Keybind { get; set; } = keybind.Replace("\0", " + ");
    [ObservableProperty] public partial string Title { get; set; } = hotKeyAction.Title;
}