using VoiceCraft.Core.World;
using VoiceCraft.Network.Audio.Effects;
using VoiceCraft.Network.Systems;
using Xunit;

namespace VoiceCraft.Network.Tests.Systems;

public class AudioEffectSystemTests
{
    [Fact]
    public void SetEffect_AssignsDictionaryBitmaskToEffect()
    {
        using var system = new AudioEffectSystem();
        var effect = new ProximityEffect();

        system.SetEffect(4, effect);

        Assert.Equal((ushort)4, effect.Bitmask);
    }

    [Fact]
    public void SetEffect_SameTypeUpdatesExistingEffectAndDisposesTemporaryUpdate()
    {
        using var system = new AudioEffectSystem();
        var existing = new ProximityEffect { MaxRange = 10 };
        var update = new ProximityEffect { MaxRange = 25 };
        var updateDisposed = 0;
        update.OnDisposed += _ => updateDisposed++;
        system.SetEffect(2, existing);

        system.SetEffect(2, update);

        Assert.True(system.TryGetEffect(2, out var stored));
        Assert.Same(existing, stored);
        Assert.Equal(25, existing.MaxRange);
        Assert.Equal((ushort)2, existing.Bitmask);
        Assert.Equal(1, updateDisposed);
    }
}
