using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.Views.Settings;

public partial class HotKeySettingsView : UserControl
{
    private readonly HashSet<Key> _pressedKeys = [];

    public HotKeySettingsView()
    {
        InitializeComponent();
    }

    private void HotKeyCapture_OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (sender is InputElement inputElement)
            inputElement.Focus();
    }

    private void HotKeyCapture_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HotKeySettingsViewModel viewModel) return;
        if (e.Key is Key.None or Key.System) return;

        _pressedKeys.Add(e.Key);
        var keys = new List<string>();
        foreach (var key in _pressedKeys)
            keys.Add(key.ToString());
        viewModel.CaptureBindingCommand.Execute(string.Join(" + ", keys));
        _pressedKeys.Clear();
        e.Handled = true;
    }
}
