using Xunit;
using VoiceCraft.Network;

namespace VoiceCraft.Network.Tests;

public class Z85Tests
{
    [Fact]
    public void GetBytes_WithInvalidCharacter_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Z85.GetBytes("0000 "));
        Assert.Throws<ArgumentException>(() => Z85.GetBytes("0000~"));
    }

    [Fact]
    public void GetString_WithUnpaddedLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Z85.GetString(new byte[] { 1, 2, 3 }));
    }
}
