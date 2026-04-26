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
}
