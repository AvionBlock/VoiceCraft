using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Settings;

namespace VoiceCraft.Client.Views.Settings;

public partial class HotKeySettingsView : UserControl
{
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<string> _pressedMouseButtons = [];
    private HotKeySettingsViewModel? _observedViewModel;

    public HotKeySettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void HotKeyCapture_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HotKeySettingsViewModel viewModel) return;
        if (!viewModel.IsRebinding || e.Key is Key.None or Key.System) return;

        _pressedKeys.Add(e.Key);
        UpdatePreview(viewModel);
        e.Handled = true;
    }

    private void HotKeyCapture_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HotKeySettingsViewModel viewModel) return;
        if (!viewModel.IsRebinding ||e.Key is Key.None or Key.System) return;

        _pressedKeys.Remove(e.Key);
        e.Handled = true;
    }

    private void HotKeyCapture_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not HotKeySettingsViewModel viewModel) return;
        if (!viewModel.IsRebinding) return;

        var mouseButton = GetMouseButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

        if (mouseButton == null)
            return;

        _pressedMouseButtons.Add(mouseButton);
        UpdatePreview(viewModel);
        e.Handled = true;
    }

    private void HotKeyCapture_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not HotKeySettingsViewModel viewModel) return;
        if (!viewModel.IsRebinding) return;
        
        var mouseButton = e.InitialPressMouseButton switch
        {
            MouseButton.Left => HotKeyService.NormalizeMouseButton("Left"),
            MouseButton.Right => HotKeyService.NormalizeMouseButton("Right"),
            MouseButton.Middle => HotKeyService.NormalizeMouseButton("Middle"),
            MouseButton.XButton1 => HotKeyService.NormalizeMouseButton("XButton1"),
            MouseButton.XButton2 => HotKeyService.NormalizeMouseButton("XButton2"),
            _ => null
        };

        if (mouseButton == null)
            return;

        _pressedMouseButtons.Remove(mouseButton);
        e.Handled = true;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_observedViewModel != null)
            _observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _observedViewModel = DataContext as HotKeySettingsViewModel;

        if (_observedViewModel != null)
            _observedViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not HotKeySettingsViewModel viewModel || e.PropertyName != nameof(HotKeySettingsViewModel.IsRebinding))
            return;

        ResetCapture(clearPreview: !viewModel.IsRebinding);
    }

    private void UpdatePreview(HotKeySettingsViewModel viewModel)
    {
        var keys = _pressedKeys.Select(key => key.ToString()).ToList();
        keys.AddRange(_pressedMouseButtons);
        viewModel.UpdateBindingPreviewCommand.Execute(string.Join(" + ", keys));
    }

    private static string? GetMouseButton(PointerUpdateKind updateKind)
    {
        return updateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => HotKeyService.NormalizeMouseButton("Left"),
            PointerUpdateKind.RightButtonPressed => HotKeyService.NormalizeMouseButton("Right"),
            PointerUpdateKind.MiddleButtonPressed => HotKeyService.NormalizeMouseButton("Middle"),
            PointerUpdateKind.XButton1Pressed => HotKeyService.NormalizeMouseButton("XButton1"),
            PointerUpdateKind.XButton2Pressed => HotKeyService.NormalizeMouseButton("XButton2"),
            _ => null
        };
    }

    private void ResetCapture(bool clearPreview)
    {
        _pressedKeys.Clear();
        _pressedMouseButtons.Clear();
        if (clearPreview && DataContext is HotKeySettingsViewModel viewModel)
            viewModel.ClearBindingPreviewCommand.Execute(null);
    }
}
