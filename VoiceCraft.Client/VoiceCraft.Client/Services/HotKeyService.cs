using System;
using System.Collections.Generic;

namespace VoiceCraft.Client.Services;

public abstract class HotKeyService : IDisposable
{
    protected HotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions)
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

public class MuteAction(IBackgroundService backgroundService) : HotKeyAction
{
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
    public override string Title => "Deafen";
    public override string DefaultKeyCombo => "LeftControl\0LeftShift\0D";

    public override void Press()
    {
        var service = backgroundService.GetService<VoiceCraftService>();
        if(service == null) return;
        service.Deafened = !service.Deafened;
    }
}