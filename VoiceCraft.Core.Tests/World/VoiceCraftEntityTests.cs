using System.Numerics;
using Xunit;
using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Tests.World;

public class VoiceCraftEntityTests
{
    [Fact]
    public void Name_TooLong_Throws()
    {
        var entity = new VoiceCraftEntity(1);
        var tooLong = new string('a', Constants.MaxStringLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => entity.Name = tooLong);
    }

    [Fact]
    public void WorldId_TooLong_Throws()
    {
        var entity = new VoiceCraftEntity(1);
        var tooLong = new string('a', Constants.MaxStringLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => entity.WorldId = tooLong);
    }

    [Fact]
    public void CaveFactor_And_MuffleFactor_AreClamped()
    {
        var entity = new VoiceCraftEntity(1);

        entity.CaveFactor = 2.0f;
        entity.MuffleFactor = -1.0f;

        Assert.Equal(1.0f, entity.CaveFactor, 3);
        Assert.Equal(0.0f, entity.MuffleFactor, 3);
    }

    [Fact]
    public void NonFiniteSpatialValues_AreSanitized()
    {
        var entity = new VoiceCraftEntity(1);

        entity.Position = new Vector3(float.NaN, float.PositiveInfinity, 3);
        entity.Rotation = new Vector2(float.NegativeInfinity, 5);
        entity.CaveFactor = float.NaN;
        entity.MuffleFactor = float.PositiveInfinity;

        Assert.Equal(new Vector3(0, 0, 3), entity.Position);
        Assert.Equal(new Vector2(0, 5), entity.Rotation);
        Assert.Equal(0.0f, entity.CaveFactor, 3);
        Assert.Equal(0.0f, entity.MuffleFactor, 3);
    }

    [Fact]
    public void AddVisibleEntity_IgnoresSelfAndDuplicates()
    {
        var entity = new VoiceCraftEntity(1);
        var other = new VoiceCraftEntity(2);
        var addCount = 0;
        entity.OnVisibleEntityAdded += (_, _) => addCount++;

        entity.AddVisibleEntity(entity);
        entity.AddVisibleEntity(other);
        entity.AddVisibleEntity(other);

        Assert.Single(entity.VisibleEntities);
        Assert.Equal(1, addCount);
    }

    [Fact]
    public void TrimDeadEntities_RemovesDestroyedVisibleEntities()
    {
        var entity = new VoiceCraftEntity(1);
        var other = new VoiceCraftEntity(2);
        entity.AddVisibleEntity(other);

        other.Destroy();
        entity.TrimDeadEntities();

        Assert.Empty(entity.VisibleEntities);
    }

    [Fact]
    public void ReceiveAudio_UpdatesLoudnessAndSpeakingState()
    {
        var entity = new VoiceCraftEntity(1);

        entity.ReceiveAudio([1, 2, 3], 42, 0.75f);

        Assert.True(entity.IsSpeaking);
        Assert.Equal(0.75f, entity.Loudness, 3);
    }

    [Fact]
    public void Position_And_Rotation_Update()
    {
        var entity = new VoiceCraftEntity(1);
        var position = new Vector3(1, 2, 3);
        var rotation = new Vector2(4, 5);

        entity.Position = position;
        entity.Rotation = rotation;

        Assert.Equal(position, entity.Position);
        Assert.Equal(rotation, entity.Rotation);
    }
}
