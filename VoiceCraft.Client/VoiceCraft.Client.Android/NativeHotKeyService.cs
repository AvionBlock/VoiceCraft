using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

public class NativeHotKeyService : HotKeyService
{
    public NativeHotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions) : base(registeredHotKeyActions)
    {
    }

    public override void Initialize()
    {
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}