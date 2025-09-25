using System.Collections.Generic;
using VoiceCraft.Client.Processes;

namespace VoiceCraft.Client.Services;

public abstract class HotKeyService
{
    public HotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions)
    {
        foreach (var registeredHotKeyAction in registeredHotKeyActions)
            HotKeyActions.Add(registeredHotKeyAction.DefaultKeyCombo, registeredHotKeyAction);
    }

    public Dictionary<string, HotKeyAction> HotKeyActions { get; } = new();
}

public abstract class HotKeyAction
{
    public abstract string Title { get; }
    public abstract string DefaultKeyCombo { get; }

    public virtual void Press()
    {
    }

    public virtual void Release()
    {
    }
}

public class MuteAction(BackgroundService backgroundService) : HotKeyAction
{
    private readonly BackgroundService _backgroundService = backgroundService;
    public override string Title => "Mute";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0M";

    public override void Press()
    {
        _backgroundService.TryGetBackgroundProcess<VoipBackgroundProcess>(out var process);
        process?.ToggleMute(!process.Muted);
    }
}

public class DeafenAction(BackgroundService backgroundService) : HotKeyAction
{
    private readonly BackgroundService _backgroundService = backgroundService;
    public override string Title => "Deafen";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0D";

    public override void Press()
    {
        _backgroundService.TryGetBackgroundProcess<VoipBackgroundProcess>(out var process);
        process?.ToggleDeafen(!process.Deafened);
    }
}