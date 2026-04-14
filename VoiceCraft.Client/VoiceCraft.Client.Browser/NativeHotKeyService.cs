using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser;

public class NativeHotKeyService : HotKeyService
{
    public NativeHotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions, SettingsService settingsService) : base(registeredHotKeyActions, settingsService)
    {
    }

    protected override void InitializeCore()
    {
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
