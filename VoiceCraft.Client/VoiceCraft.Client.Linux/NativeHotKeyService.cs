using System;
using System.Collections.Generic;
using System.Text;
using SharpHook;
using SharpHook.Data;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux;

public class NativeHotKeyService : HotKeyService
{
    private readonly EventLoopGlobalHook _hook;
    private readonly List<string> _pressedInputs = [];

    public NativeHotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions, SettingsService settingsService) : base(registeredHotKeyActions, settingsService)
    {
        _hook = new EventLoopGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.MousePressed += OnMousePressed;
        _hook.MouseReleased += OnMouseReleased;
    }

    protected override void InitializeCore()
    {
        _ = _hook.RunAsync();
    }

    public override void Dispose()
    {
        _hook.KeyPressed -= OnKeyPressed;
        _hook.MousePressed -= OnMousePressed;
        _hook.MouseReleased -= OnMouseReleased;
        try
        {
            _hook.Dispose();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        GC.SuppressFinalize(this);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        ProcessPressedInput(e.Data.KeyCode.ToString().Replace("Vc", ""));
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        ProcessReleasedInput(e.Data.KeyCode.ToString().Replace("Vc", ""));
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        ProcessPressedInput(HotKeyService.NormalizeMouseButton(e.Data.Button.ToString()));
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        ProcessReleasedInput(HotKeyService.NormalizeMouseButton(e.Data.Button.ToString()));
    }

    private void ProcessPressedInput(string input)
    {
        if (_pressedInputs.Contains(input)) return;
        _pressedInputs.Add(input);
        if (HotKeyActions.TryGetValue(HotKeyService.NormalizeKeyCombo(_pressedInputs), out var action))
            action.Press();
    }

    private void ProcessReleasedInput(string input)
    {
        if (!_pressedInputs.Contains(input)) return;
        var keyCombo = HotKeyService.NormalizeKeyCombo(_pressedInputs);
        _pressedInputs.Remove(input);
        if (HotKeyActions.TryGetValue(keyCombo, out var action))
            action.Release();
    }
}
