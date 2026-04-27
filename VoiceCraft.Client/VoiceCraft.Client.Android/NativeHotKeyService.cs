using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Android;

public class NativeHotKeyService(IEnumerable<HotKeyAction> registeredHotKeyActions, SettingsService settingsService)
    : HotKeyService(registeredHotKeyActions, settingsService)
{
    public override void Initialize()
    {
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}