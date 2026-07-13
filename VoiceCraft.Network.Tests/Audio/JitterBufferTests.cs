using Xunit;
using VoiceCraft.Network.Audio;

namespace VoiceCraft.Network.Tests.Audio;

public class JitterBufferTests
{
    [Fact]
    public void Get_WhenSequenceWraps_DoesNotThrow()
    {
        var buffer = new JitterBuffer(TimeSpan.Zero);
        var packet = new JitterPacket(ushort.MaxValue, [1, 2, 3]);

        buffer.Add(packet);

        Assert.True(buffer.Get(out var readPacket));
        Assert.Same(packet, readPacket);
    }

    [Fact]
    public void Get_ReordersPacketThatFillsExpectedGap()
    {
        var buffer = new JitterBuffer(TimeSpan.FromSeconds(1));
        buffer.Add(new JitterPacket(10, [10]));
        Assert.True(buffer.Get(out var first));
        Assert.Equal((ushort)10, first.SequenceId);

        buffer.Add(new JitterPacket(12, [12]));
        Assert.False(buffer.Get(out _));
        buffer.Add(new JitterPacket(11, [11]));

        Assert.True(buffer.Get(out var second));
        Assert.Equal((ushort)11, second.SequenceId);
        Assert.True(buffer.Get(out var third));
        Assert.Equal((ushort)12, third.SequenceId);
    }

    [Fact]
    public void Add_DropsPacketOlderThanExpectedSequence()
    {
        var buffer = new JitterBuffer(TimeSpan.Zero);
        buffer.Add(new JitterPacket(10, [10]));
        Assert.True(buffer.Get(out _));

        buffer.Add(new JitterPacket(9, [9]));

        Assert.False(buffer.Get(out _));
    }

    [Fact]
    public void Add_DropsDuplicateWithoutReplayingIt()
    {
        var buffer = new JitterBuffer(TimeSpan.Zero);
        buffer.Add(new JitterPacket(20, [20]));
        buffer.Add(new JitterPacket(20, [20]));

        Assert.True(buffer.Get(out var packet));
        Assert.Equal((ushort)20, packet.SequenceId);
        Assert.False(buffer.Get(out _));
    }

    [Fact]
    public void Get_ContinuesAcrossSequenceWraparound()
    {
        var buffer = new JitterBuffer(TimeSpan.Zero);
        buffer.Add(new JitterPacket(ushort.MaxValue, [1]));
        Assert.True(buffer.Get(out var beforeWrap));

        buffer.Add(new JitterPacket(0, [2]));

        Assert.Equal(ushort.MaxValue, beforeWrap.SequenceId);
        Assert.True(buffer.Get(out var afterWrap));
        Assert.Equal((ushort)0, afterWrap.SequenceId);
    }
}
