using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

public class NativeHotKeyService : HotKeyService
{
    private readonly StringBuilder _stringBuilder = new();

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