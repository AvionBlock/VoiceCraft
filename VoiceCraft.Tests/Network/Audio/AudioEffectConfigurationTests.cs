using System.Numerics;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Audio.Effects;

namespace VoiceCraft.Tests.Network.Audio;

public class AudioEffectConfigurationTests
{
    [Theory]
    [InlineData("overworld", "overworld", true)]
    [InlineData("overworld", "nether", false)]
    [InlineData("", "overworld", false)]
    [InlineData("overworld", "", false)]
    public void VisibilityEffect_RequiresMatchingWorlds(string sourceWorld, string targetWorld, bool expected)
    {
        var source = new VoiceCraftEntity(1) { WorldId = sourceWorld };
        var target = new VoiceCraftEntity(2) { WorldId = targetWorld };
        using var effect = new VisibilityEffect { Bitmask = 1 };

        Assert.Equal(expected, effect.Visibility(source, target, 1));
    }

    [Fact]
    public void VisibilityEffect_AllowsOtherWorldsWhenMaskedOff()
    {
        var source = new VoiceCraftEntity(1) { WorldId = "overworld", TalkBitmask = 0 };
        var target = new VoiceCraftEntity(2) { WorldId = "nether" };
        using var effect = new VisibilityEffect { Bitmask = 1 };

        Assert.True(effect.Visibility(source, target, 1));
    }

    [Theory]
    [InlineData(10f, true)]
    [InlineData(10.01f, false)]
    public void ProximityEffect_UsesInclusiveMaxRange(float distance, bool expected)
    {
        var source = new VoiceCraftEntity(1);
        var target = new VoiceCraftEntity(2) { Position = new Vector3(distance, 0, 0) };
        using var effect = new ProximityEffect { Bitmask = 1, MaxRange = 10 };

        Assert.Equal(expected, effect.Visibility(source, target, 1));
    }

    [Fact]
    public void ProximityEffect_UsesWiderRangeOverrideAndIgnoresDisabledMask()
    {
        var source = new VoiceCraftEntity(1);
        var target = new VoiceCraftEntity(2) { Position = new Vector3(15, 0, 0) };
        using var effect = new ProximityEffect { Bitmask = 1, MaxRange = 10 };
        source.SetProperty("ProximityEffect:MaxRange", 5f);
        target.SetProperty("ProximityEffect:MaxRange", 20f);

        Assert.Equal(20f, effect.EvaluateMaxRangeProperty(source, target));
        Assert.True(effect.Visibility(source, target, 1));

        source.TalkBitmask = 0;
        target.Position = new Vector3(50, 0, 0);
        Assert.True(effect.Visibility(source, target, 1));
    }

    [Fact]
    public void ProximityEffect_UsesLowerMinRangeAndClampsWetDryOverride()
    {
        var source = new VoiceCraftEntity(1);
        var target = new VoiceCraftEntity(2);
        using var effect = new ProximityEffect();
        effect.MinRange = 8;
        effect.WetDry = 0.25f;
        
        source.SetProperty("ProximityEffect:MinRange", 6f);
        target.SetProperty("ProximityEffect:MinRange", 3f);
        source.SetProperty("ProximityEffect:WetDry", 2f);

        Assert.Equal(3f, effect.EvaluateMinRangeProperty(source, target));
        Assert.Equal(1f, effect.EvaluateWetDryProperty(source, target));

        source.ClearProperties();
        target.ClearProperties();
        Assert.Equal(8f, effect.EvaluateMinRangeProperty(source, target));
        Assert.Equal(0.25f, effect.EvaluateWetDryProperty(source, target));
    }

    [Fact]
    public void EchoEffect_ClampsPerEntityDelayAndFeedback()
    {
        var source = new VoiceCraftEntity(1);
        var target = new VoiceCraftEntity(2);
        using var effect = new EchoEffect();
        effect.Delay = 0.5f;
        effect.Feedback = 0.25f;

        Assert.Equal(0.5f, effect.EvaluateDelayProperty(source, target), 3);
        Assert.Equal(0.25f, effect.EvaluateFeedbackProperty(source, target));

        source.SetProperty("EchoEffect:Delay", 12f);
        target.SetProperty("EchoEffect:Feedback", 2f);
        Assert.Equal(10f, effect.EvaluateDelayProperty(source, target));
        Assert.Equal(1f, effect.EvaluateFeedbackProperty(source, target));
    }

    [Fact]
    public void ProximityMuffleEffect_UsesLargestPerEntityFactor()
    {
        var source = new VoiceCraftEntity(1);
        var target = new VoiceCraftEntity(2);
        using var effect = new ProximityMuffleEffect();
        effect.Factor = 0.2f;
        
        source.SetProperty("ProximityMuffleEffect:Factor", 0.4f);
        target.SetProperty("ProximityMuffleEffect:Factor", 0.8f);

        Assert.Equal(0.8f, effect.EvaluateFactorProperty(source, target));

        target.SetProperty("ProximityMuffleEffect:Factor", 2f);
        Assert.Equal(1f, effect.EvaluateFactorProperty(source, target));
    }
}
