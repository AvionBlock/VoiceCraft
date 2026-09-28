using System;
using System.Linq;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Runtime;

namespace VoiceCraft.Tests.Server.Runtime;

[Collection("SharedLocalizer")]
public class ServerPropertiesTests
{
    [Fact]
    public void Load_AppliesHostPortAndSharedKeyToEveryTransport()
    {
        var properties = Load(new RuntimeOptions
        {
            ServerProperties = new ServerPropertiesStructure(),
            ServerKey = "shared-key",
            TransportHost = "0.0.0.0",
            TransportPort = 9273,
            VoicePort = 9052
        });

        Assert.Equal("shared-key", properties.McHttpConfig.LoginToken);
        Assert.Equal("shared-key", properties.McTcpConfig.LoginToken);
        Assert.Equal("shared-key", properties.McWssConfig.LoginToken);
        Assert.Equal("0.0.0.0", properties.McTcpConfig.Hostname);
        Assert.Equal(9273, properties.McTcpConfig.Port);
        Assert.Equal("http://0.0.0.0:9273/", properties.McHttpConfig.Hostname);
        Assert.Equal("ws://0.0.0.0:9273/", properties.McWssConfig.Hostname);
        Assert.Equal((uint)9052, properties.VoiceCraftConfig.Port);
    }

    [Theory]
    [InlineData("http", true, false, false)]
    [InlineData("tcp-socket,wss", false, true, true)]
    [InlineData(" local-socket , websocket ", false, true, true)]
    public void Load_SelectsRequestedTransports(string modes, bool http, bool tcp, bool wss)
    {
        var properties = Load(new RuntimeOptions
        {
            ServerProperties = new ServerPropertiesStructure(),
            TransportMode = [modes]
        });

        Assert.Equal(http, properties.McHttpConfig.Enabled);
        Assert.Equal(tcp, properties.McTcpConfig.Enabled);
        Assert.Equal(wss, properties.McWssConfig.Enabled);
    }

    [Fact]
    public void Load_RejectsUnknownTransportMode()
    {
        var options = new RuntimeOptions
        {
            ServerProperties = new ServerPropertiesStructure(),
            TransportMode = ["quic"]
        };

        var exception = Assert.Throws<ArgumentException>(() => Load(options));
        Assert.Contains("quic", exception.Message);
    }

    [Fact]
    public void Load_IgnoresOutOfRangePortOverrides()
    {
        var config = new ServerPropertiesStructure();
        var originalTcpPort = config.McTcpConfig.Port;
        var originalVoicePort = config.VoiceCraftConfig.Port;
        var properties = Load(new RuntimeOptions
        {
            ServerProperties = config,
            TransportPort = 0,
            VoicePort = 65536
        });

        Assert.Equal(originalTcpPort, properties.McTcpConfig.Port);
        Assert.Equal(originalVoicePort, properties.VoiceCraftConfig.Port);
    }

    [Fact]
    public void Load_AssignsBitmasksToDefaultAudioEffects()
    {
        var properties = Load(new RuntimeOptions
        {
            ServerProperties = new ServerPropertiesStructure()
        });

        Assert.Equal(new ushort[] { 1, 2, 4, 8 }, properties.DefaultAudioEffects.Keys.ToArray());
        foreach (var effect in properties.DefaultAudioEffects)
            Assert.Equal(effect.Key, effect.Value.Bitmask);
    }

    private static ServerProperties Load(RuntimeOptions options)
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Server");
        var properties = new ServerProperties();
        properties.Load(options);
        return properties;
    }
}
