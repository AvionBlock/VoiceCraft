using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.Services;

public abstract class HotKeyService : IDisposable
{
    private readonly Dictionary<string, HotKeyAction> _actionsById;
    private readonly Dictionary<string, HotKeyAction> _hotKeyActions = new(StringComparer.Ordinal);
    private readonly HotKeySettings _hotKeySettings;
    protected IReadOnlyDictionary<string, HotKeyAction> HotKeyActions => _hotKeyActions;

    protected HotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions, SettingsService settingsService)
    {
        _hotKeySettings = settingsService.HotKeySettings;
        _actionsById = registeredHotKeyActions.ToDictionary(x => x.Id, StringComparer.Ordinal);
        _hotKeySettings.OnUpdated += OnHotKeySettingsUpdated;
        ReloadBindings();
    }

    public event Action? OnBindingsChanged;

    public virtual void Dispose()
    {
        _hotKeySettings.OnUpdated -= OnHotKeySettingsUpdated;
        GC.SuppressFinalize(this);
    }

    public abstract void Initialize();

    public IReadOnlyList<HotKeyBinding> GetBindings()
    {
        return _actionsById.Values
            .Select(action => new HotKeyBinding(action, GetBindingForAction(action)))
            .ToArray();
    }

    public void SetBinding(string actionId, string keyCombo)
    {
        if (!_actionsById.ContainsKey(actionId))
            throw new ArgumentException($"Unknown hotkey action '{actionId}'.", nameof(actionId));

        keyCombo = NormalizeKeyCombo(keyCombo);
        var bindings = new Dictionary<string, string>(_hotKeySettings.Bindings, StringComparer.Ordinal);

        foreach (var existingBinding in bindings.Where(x => x.Value == keyCombo && x.Key != actionId).ToArray())
            bindings.Remove(existingBinding.Key);

        bindings[actionId] = keyCombo;
        _hotKeySettings.Bindings = bindings;
    }

    public static string NormalizeKeyCombo(IEnumerable<string> keys)
    {
        return NormalizeKeyCombo(string.Join("\0", keys.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    public static string NormalizeKeyCombo(string keyCombo)
    {
        var keys = keyCombo
            .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        return string.Join("\0", keys);
    }

    public static string NormalizeMouseButton(string buttonName)
    {
        return buttonName switch
        {
            "Left" => "MouseLeft",
            "Right" => "MouseRight",
            "Middle" => "MouseMiddle",
            "XButton1" => "MouseButton4",
            "XButton2" => "MouseButton5",
            "Button1" => "MouseLeft",
            "Button2" => "MouseRight",
            "Button3" => "MouseMiddle",
            "Button4" => "MouseButton4",
            "Button5" => "MouseButton5",
            _ => buttonName.StartsWith("Mouse", StringComparison.Ordinal) ? buttonName : $"Mouse{buttonName}"
        };
    }
    
    private string GetBindingForAction(HotKeyAction action)
    {
        return _hotKeySettings.Bindings.GetValueOrDefault(action.Id) ?? action.DefaultKeyCombo;
    }

    private void ReloadBindings()
    {
        _hotKeyActions.Clear();
        foreach (var action in _actionsById.Values)
            _hotKeyActions[GetBindingForAction(action)] = action;
    }

    private void OnHotKeySettingsUpdated(HotKeySettings _)
    {
        ReloadBindings();
        OnBindingsChanged?.Invoke();
    }
}

public abstract class HotKeyAction
{
    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract string DefaultKeyCombo { get; }

    public virtual void Press()
    {
    }

    public virtual void Release()
    {
    }
}

public sealed class HotKeyBinding(HotKeyAction action, string keyCombo)
{
    public HotKeyAction Action { get; } = action;
    public string KeyCombo { get; } = keyCombo;
}

public class MuteAction(IBackgroundService backgroundService) : HotKeyAction
{
    public override string Id => "Mute";
    public override string Title => "Mute";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0M";

    public override void Press()
    {
        var service = backgroundService.GetService<VoiceCraftService>();
        if(service == null) return;
        service.Muted = !service.Muted;
    }
}

public class DeafenAction(IBackgroundService backgroundService) : HotKeyAction
{
    public override string Id => "Deafen";
    public override string Title => "Deafen";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0D";

    public override void Press()
    {
        var service = backgroundService.GetService<VoiceCraftService>();
        if(service == null) return;
        service.Deafened = !service.Deafened;
    }
}

public class PushToTalkAction(IBackgroundService backgroundService, PushToTalkCueService cueService) : HotKeyAction
{
    private bool _active;
    private bool _restoreMutedState;

    public override string Id => "PushToTalk";
    public override string Title => "PushToTalk";
    public override string DefaultKeyCombo => "LeftControl";

    public override void Press()
    {
        if (_active) return;
        var service = backgroundService.GetService<VoiceCraftService>();
        if (service == null) return;
        _active = true;
        _restoreMutedState = service.Muted;
        service.Muted = false;
        cueService.PlayActivatedCue();
    }

    public override void Release()
    {
        if (!_active) return;
        var service = backgroundService.GetService<VoiceCraftService>();
        _active = false;
        if (service == null) return;
        if (_restoreMutedState)
            service.Muted = true;
        _restoreMutedState = false;
        cueService.PlayReleasedCue();
    }
}
