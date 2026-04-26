using LiteNetLib.Utils;
using Xunit;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.NetPeers;
using VoiceCraft.Network.Systems;
using VoiceCraft.Network.World;

namespace VoiceCraft.Network.Tests.Performance;

[Collection("AllocationRegression")]
public class AllocationRegressionTests
{
    [Fact]
    public void AudioEffectsSnapshot_RepeatedReads_AreNearlyAllocationFree()
    {
        using var effectSystem = new AudioEffectSystem();
        effectSystem.SetEffect(1, new FakeVisibleEffect(true));

        _ = effectSystem.AudioEffectsSnapshot;

        var allocated = MeasureAllocatedBytes(
            () => GC.KeepAlive(effectSystem.AudioEffectsSnapshot),
            iterations: 10_000);

        Assert.InRange(allocated, 0, 4096);
    }

    [Fact]
    public void AudioEffects_Getter_Allocates_More_Than_Snapshot_Reads()
    {
        using var effectSystem = new AudioEffectSystem();
        AddVisibleEffects(effectSystem, 4);

        _ = effectSystem.AudioEffectsSnapshot;
        _ = effectSystem.AudioEffects;

        var snapshotAllocated = MeasureAllocatedBytes(
            () => GC.KeepAlive(effectSystem.AudioEffectsSnapshot),
            iterations: 5_000);
        var getterAllocated = MeasureAllocatedBytes(
            () => GC.KeepAlive(effectSystem.AudioEffects),
            iterations: 1_000);

        Assert.True(
            getterAllocated > snapshotAllocated + 25_000,
            $"Expected AudioEffects getter to allocate meaningfully more than snapshot reads. Snapshot={snapshotAllocated}, Getter={getterAllocated}");
    }

    [Fact]
    public void Snapshot_Instance_IsStable_OnRead_AndChanges_OnMutation()
    {
        using var effectSystem = new AudioEffectSystem();
        AddVisibleEffects(effectSystem, 2);

        var snapshotBeforeRead = effectSystem.AudioEffectsSnapshot;
        var readAllocated = MeasureAllocatedBytes(
            () => GC.KeepAlive(effectSystem.AudioEffectsSnapshot),
            iterations: 5_000);
        var snapshotAfterRead = effectSystem.AudioEffectsSnapshot;

        effectSystem.SetEffect(4, new FakeVisibleEffect(true));
        var snapshotAfterMutation = effectSystem.AudioEffectsSnapshot;

        Assert.InRange(readAllocated, 0, 4096);
        Assert.Same(snapshotBeforeRead, snapshotAfterRead);
        Assert.NotSame(snapshotAfterRead, snapshotAfterMutation);
    }

    [Theory]
    [InlineData(16, 2)]
    [InlineData(32, 4)]
    [InlineData(48, 6)]
    public void VisibilityUpdate_Allocates_Less_Than_LegacyStyle_EffectEnumeration(
        int entityCount,
        int effectCount)
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);

        AddEntities(world, entityCount);
        AddVisibleEffects(effectSystem, effectCount);

        visibilitySystem.Update();
        RunLegacyStyleVisibilityPass(world, effectSystem);

        var currentAllocated = MeasureAllocatedBytes(visibilitySystem.Update, iterations: 25);
        var legacyAllocated = MeasureAllocatedBytes(
            () => RunLegacyStyleVisibilityPass(world, effectSystem),
            iterations: 25);

        Assert.True(
            currentAllocated < legacyAllocated,
            $"Expected current visibility update to allocate less than legacy-style effect access. Current={currentAllocated}, Legacy={legacyAllocated}");
    }

    [Fact]
    public void VisibilityUpdate_Allocation_Remains_Roughly_Flat_As_EffectCount_Grows()
    {
        var lowEffectAllocated = MeasureCurrentVisibilityUpdateAllocations(entityCount: 40, effectCount: 1);
        var highEffectAllocated = MeasureCurrentVisibilityUpdateAllocations(entityCount: 40, effectCount: 8);

        Assert.True(
            highEffectAllocated < lowEffectAllocated * 2 + 8_192,
            $"Expected current visibility allocations to stay roughly flat as effect count grows. Low={lowEffectAllocated}, High={highEffectAllocated}");
    }

    [Fact]
    public void LegacyStyleVisibilityPass_Allocations_Grow_With_EffectCount()
    {
        var lowEffectAllocated = MeasureLegacyVisibilityAllocations(entityCount: 40, effectCount: 1);
        var highEffectAllocated = MeasureLegacyVisibilityAllocations(entityCount: 40, effectCount: 8);

        Assert.True(
            highEffectAllocated > lowEffectAllocated + 25_000,
            $"Expected legacy-style allocations to grow materially with more effects. Low={lowEffectAllocated}, High={highEffectAllocated}");
    }

    [Theory]
    [InlineData(16, 2)]
    [InlineData(32, 2)]
    [InlineData(64, 2)]
    public void VisibilityUpdate_SteadyState_Allocations_Scale_With_WorldSize_Not_EffectLookups(
        int entityCount,
        int iterations)
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);

        AddEntities(world, entityCount);
        AddVisibleEffects(effectSystem, 4);

        visibilitySystem.Update();

        var allocated = MeasureAllocatedBytes(visibilitySystem.Update, iterations: iterations);

        Assert.True(
            allocated < 400_000,
            $"Expected steady-state visibility allocations to stay bounded for {entityCount} entities. Allocated={allocated}");
    }

    private static long MeasureCurrentVisibilityUpdateAllocations(int entityCount, int effectCount)
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();
        var visibilitySystem = new VisibilitySystem(world, effectSystem);

        AddEntities(world, entityCount);
        AddVisibleEffects(effectSystem, effectCount);
        visibilitySystem.Update();

        return MeasureAllocatedBytes(visibilitySystem.Update, iterations: 20);
    }

    private static long MeasureLegacyVisibilityAllocations(int entityCount, int effectCount)
    {
        using var world = new VoiceCraftWorld();
        using var effectSystem = new AudioEffectSystem();

        AddEntities(world, entityCount);
        AddVisibleEffects(effectSystem, effectCount);
        RunLegacyStyleVisibilityPass(world, effectSystem);

        return MeasureAllocatedBytes(() => RunLegacyStyleVisibilityPass(world, effectSystem), iterations: 20);
    }

    private static int RunLegacyStyleVisibilityPass(VoiceCraftWorld world, AudioEffectSystem effectSystem)
    {
        var visibleCount = 0;
        foreach (var entity in world.Entities)
        {
            var visibleNetworkEntities = world.Entities.OfType<VoiceCraftNetworkEntity>();
            foreach (var possibleEntity in visibleNetworkEntities)
            {
                if (possibleEntity.Id == entity.Id) continue;
                if ((entity.TalkBitmask & possibleEntity.ListenBitmask) == 0) continue;

                var visible = true;
                foreach (var effect in effectSystem.AudioEffects)
                {
                    if (effect.Value is not IVisible visibleEffect) continue;
                    if (visibleEffect.Visibility(entity, possibleEntity, effect.Key)) continue;

                    visible = false;
                    break;
                }

                if (visible)
                    visibleCount++;
            }
        }

        return visibleCount;
    }

    private static void AddEntities(VoiceCraftWorld world, int entityCount)
    {
        for (var i = 0; i < entityCount; i++)
            world.AddEntity(CreateNetworkEntity(i + 1));
    }

    private static void AddVisibleEffects(AudioEffectSystem effectSystem, int effectCount)
    {
        for (var i = 0; i < effectCount; i++)
            effectSystem.SetEffect((ushort)(1 << i), new FakeVisibleEffect(true));
    }

    private static VoiceCraftNetworkEntity CreateNetworkEntity(int id)
    {
        return new VoiceCraftNetworkEntity(
            new FakeNetPeer(Guid.NewGuid(), Guid.NewGuid(), "en-US", PositioningType.Client),
            id);
    }

    private static long MeasureAllocatedBytes(Action action, int iterations)
    {
        action();

        var best = long.MaxValue;
        for (var sample = 0; sample < 4; sample++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < iterations; i++)
                action();
            var after = GC.GetTotalAllocatedBytes(true);

            best = Math.Min(best, after - before);
        }

        return best;
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

[CollectionDefinition("AllocationRegression", DisableParallelization = true)]
public sealed class AllocationRegressionCollection;
