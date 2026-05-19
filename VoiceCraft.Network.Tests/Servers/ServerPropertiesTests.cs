using System.Text.Json;
using VoiceCraft.Server;
using Xunit;

namespace VoiceCraft.Network.Tests.Servers;

public class ServerPropertiesTests
{
    [Fact]
    public void ConfigJsonWriter_EmitsReadableJson()
    {
        var json = ServerPropertiesJson.Write(new ServerPropertiesStructure());

        Assert.False(json.Contains("/*", StringComparison.Ordinal), json);
        Assert.Contains("ConfigVersion", json, StringComparison.Ordinal);

        var parsed = JsonDocument.Parse(json);
        Assert.True(parsed.RootElement.TryGetProperty("WebRtcConfig", out _));
    }

    [Fact]
    public void Migrator_AddsMissingDefaults()
    {
        var json = """
                  {
                    "TelemetryEnabled": false,
                    "WebRtcConfig": {
                      "Enabled": true
                    }
                  }
                  """;

        var migrated = ServerPropertiesMigrator.Migrate(json, out var changed);
        var properties = JsonSerializer.Deserialize(
            migrated,
            ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);

        Assert.True(changed);
        Assert.NotNull(properties);
        Assert.Equal(ServerPropertiesMigrator.CurrentVersion, properties.ConfigVersion);
        Assert.False(properties.TelemetryEnabled);
        Assert.NotEmpty(properties.WebRtcConfig.IceServers);
        Assert.NotNull(properties.WebRtcConfig.Tls);
        Assert.NotNull(properties.WebRtcConfig.PortMapping);
    }

    [Fact]
    public void Migrator_AllowsNewerConfigVersionWithoutDowngrading()
    {
        var json = """
                  {
                    "ConfigVersion": "999.0.0",
                    "TelemetryEnabled": false
                  }
                  """;

        var migrated = ServerPropertiesMigrator.Migrate(json, out var changed);
        var document = JsonDocument.Parse(migrated);

        Assert.False(changed);
        Assert.Equal("999.0.0", document.RootElement.GetProperty("ConfigVersion").GetString());
    }

    [Fact]
    public void Migrator_RejectsNumericConfigVersion()
    {
        var json = """
                  {
                    "ConfigVersion": 1
                  }
                  """;

        Assert.Throws<InvalidOperationException>(() =>
            ServerPropertiesMigrator.Migrate(json, out _));
    }
}
