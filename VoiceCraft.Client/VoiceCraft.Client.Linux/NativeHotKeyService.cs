using System;
using System.Collections.Generic;
using System.Text;
using SharpHook;
using SharpHook.Data;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux;

public class NativeHotKeyService : HotKeyService, IDisposable
{
    private EventLoopGlobalHook _hook;
    private List<KeyCode> _pressedKeys = [];
    private StringBuilder _stringBuilder = new();
    
    public NativeHotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions) : base(registeredHotKeyActions)
    {
        _hook = new EventLoopGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _ = _hook.RunAsync();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (_pressedKeys.Contains(e.Data.KeyCode)) return;
        _pressedKeys.Add(e.Data.KeyCode);
        _stringBuilder.Clear();
        
        for(var i = 0; i < _pressedKeys.Count; i++)
        {
            var cleanKey = _pressedKeys[i].ToString().Replace("Vc", "");
            _stringBuilder.Append($"{cleanKey}{(i < _pressedKeys.Count - 1? "\0" : "")}");
        }
        
        if (HotKeyActions.TryGetValue(_stringBuilder.ToString(), out var action))
        {
            action.Press();
        }
    }
    
    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (!_pressedKeys.Contains(e.Data.KeyCode)) return;
        _stringBuilder.Clear();
        
        for(var i = 0; i < _pressedKeys.Count; i++)
        {
            var cleanKey = _pressedKeys[i].ToString().Replace("Vc", "");
            _stringBuilder.Append($"{cleanKey}{(i < _pressedKeys.Count - 1? "\0" : "")}");
        }
        _pressedKeys.Remove(e.Data.KeyCode);

        if (HotKeyActions.TryGetValue(_stringBuilder.ToString(), out var action))
        {
            action.Release();
        }
    }

    public void Dispose()
    {
        _hook.KeyPressed -= OnKeyPressed;
        _hook.Stop();
        _hook.Dispose();
        GC.SuppressFinalize(this);
    }
}