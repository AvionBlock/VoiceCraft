using System;
using System.Collections.Generic;

namespace VoiceCraft.Client.Services;

public abstract class HotKeyService : IDisposable
{
    public HotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions)
    {
        foreach (var registeredHotKeyAction in registeredHotKeyActions)
            HotKeyActions.Add(registeredHotKeyAction.DefaultKeyCombo, registeredHotKeyAction);
    }

    public Dictionary<string, HotKeyAction> HotKeyActions { get; } = new();

    public virtual void Dispose()
    {
        //We do nothing by default
        GC.SuppressFinalize(this);
    }

    public abstract void Initialize();
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

public class MuteAction(VoiceCraftService voiceCraftService) : HotKeyAction
{
    public override string Title => "Mute";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0M";

    public override void Press()
    {
        voiceCraftService.Muted = !voiceCraftService.Muted;
    }
}

public class DeafenAction(VoiceCraftService voiceCraftService) : HotKeyAction
{
    public override string Title => "Deafen";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0D";

    public override void Press()
    {
        voiceCraftService.Deafened = !voiceCraftService.Deafened;
    }
}