using Xunit;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Tests.World;

public class VoiceCraftWorldTests
{
    [Fact]
    public void AddEntity_DuplicateId_Throws()
    {
        using var world = new VoiceCraftWorld();
        world.AddEntity(new VoiceCraftEntity(1));

        Assert.Throws<InvalidOperationException>(() => world.AddEntity(new VoiceCraftEntity(1)));
    }

    [Fact]
    public void DestroyEntity_RemovesEntityAndRaisesEvent()
    {
        using var world = new VoiceCraftWorld();
        var entity = new VoiceCraftEntity(1);
        VoiceCraftEntity? destroyed = null;
        world.OnEntityDestroyed += x => destroyed = x;
        world.AddEntity(entity);

        world.DestroyEntity(1);

        Assert.Null(world.GetEntity(1));
        Assert.Same(entity, destroyed);
        Assert.True(entity.Destroyed);
    }

    [Fact]
    public void GetNextId_SkipsOccupiedIds_AndResetsAfterClear()
    {
        using var world = new VoiceCraftWorld();
        world.AddEntity(new VoiceCraftEntity(0));
        world.AddEntity(new VoiceCraftEntity(1));

        Assert.Equal(2, world.GetNextId());

        world.ClearEntities();

        Assert.Equal(0, world.GetNextId());
    }

    [Fact]
    public void ClearEntities_DestroysAllEntities()
    {
        using var world = new VoiceCraftWorld();
        var first = new VoiceCraftEntity(1);
        var second = new VoiceCraftEntity(2);
        world.AddEntity(first);
        world.AddEntity(second);

        world.ClearEntities();

        Assert.Empty(world.Entities);
        Assert.True(first.Destroyed);
        Assert.True(second.Destroyed);
    }
}
