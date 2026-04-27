using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Modals;

namespace VoiceCraft.Client.Views.Modals;

public partial class HotKeyCaptureView : UserControl
{
    private readonly HashSet<Key> _pressedKeys = [];
    private readonly HashSet<string> _pressedMouseButtons = [];
    
    public HotKeyCaptureView()
    {
        InitializeComponent();
    }
    
    private void HotKeyCapture_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.None or Key.System) return;

        _pressedKeys.Add(e.Key);
        UpdatePreview();
        e.Handled = true;
    }

    private void HotKeyCapture_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.None or Key.System) return;

        _pressedKeys.Remove(e.Key);
        e.Handled = true;
    }
    
    private void HotKeyCapture_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var mouseButton = GetMouseButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind);

        if (mouseButton == null)
            return;

        _pressedMouseButtons.Add(mouseButton);
        UpdatePreview();
        e.Handled = true;
    }

    private void HotKeyCapture_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
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
    
    private void Visual_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if(sender is Control control)
            control.Focus();
    }
    
    private void UpdatePreview()
    {
        if (DataContext is not HotKeyCaptureViewModel viewModel) return;
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
}