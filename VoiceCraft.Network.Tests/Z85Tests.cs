using Xunit;

namespace VoiceCraft.Network.Tests;

public class Z85Tests
{
    [Fact]
    public void GetString_WithUnpaddedLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Z85.GetString([1, 2, 3]));
    }

    [Fact]
    public void Z85EncodeDecoder_Is_Same()
    {
        byte[] bytes = [50, 205, 25, 56];
        var encoded = Z85.GetString(bytes);
        var decoded = Z85.GetBytes(encoded);
        Assert.Equal(bytes, decoded);
    }
    
    [Fact]
    public void Z85EncodeDecoder_With_Padding_Is_Same()
    {
        byte[] bytes = [50, 205, 25, 56, 7];
        var encoded = Z85.GetStringWithPadding(bytes);
        var decoded = Z85.GetBytesWithPadding(encoded);
        Assert.Equal(bytes, decoded);
    }
}
