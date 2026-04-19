using LiteNetLib.Utils;
using Xunit;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Tests.Systems;

public class VisibilitySystemTests
{
    [Fact]
    public void Update_AddsVisibleNetworkEntity_WhenBitmaskMatchesAndEffectsAllow()
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);
        var source = new VoiceCraftEntity(1);
        var target = CreateNetworkEntity(2);

        world.AddEntity(source);
        world.AddEntity(target);

        visibilitySystem.Update();

        Assert.Contains(target, source.VisibleEntities);
    }

    [Fact]
    public void Update_RemovesVisibleNetworkEntity_WhenBitmaskNoLongerMatches()
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);
        var source = new VoiceCraftEntity(1);
        var target = CreateNetworkEntity(2);

        world.AddEntity(source);
        world.AddEntity(target);
        visibilitySystem.Update();

        source.TalkBitmask = 0;
        visibilitySystem.Update();

        Assert.DoesNotContain(target, source.VisibleEntities);
    }

    [Fact]
    public void Update_RemovesVisibleNetworkEntity_WhenVisibleEffectBlocks()
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);
        var source = new VoiceCraftEntity(1);
        var target = CreateNetworkEntity(2);

        world.AddEntity(source);
        world.AddEntity(target);
        visibilitySystem.Update();
        effectSystem.SetEffect(1, new FakeVisibleEffect(false));

        visibilitySystem.Update();

        Assert.DoesNotContain(target, source.VisibleEntities);
    }

    private static VoiceCraftNetworkEntity CreateNetworkEntity(int id)
    {
        return new VoiceCraftNetworkEntity(
            new FakeNetPeer(Guid.NewGuid(), Guid.NewGuid(), "en-US", PositioningType.Client),
            id);
    }

    private sealed class FakeNetPeer(Guid userGuid, Guid serverUserGuid, string locale, PositioningType positioningType)
        : VoiceCraftNetPeer(userGuid, serverUserGuid, locale, positioningType)
    {
        public override VcConnectionState ConnectionState => VcConnectionState.Connected;
    }

    private sealed class FakeVisibleEffect(bool result) : IAudioEffect, IVisible
    {
        public EffectType EffectType => EffectType.Visibility;

        public bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask)
        {
            return result;
        }

        public void Process(VoiceCraftEntity from, VoiceCraftEntity to, ushort effectBitmask, Span<float> buffer)
        {
        }

        public void Reset()
        {
        }

        public void Serialize(NetDataWriter writer)
        {
        }

        public void Deserialize(NetDataReader reader)
        {
        }

        public void Dispose()
        {
        }
    }
}
