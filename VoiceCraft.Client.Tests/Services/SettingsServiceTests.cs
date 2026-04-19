using System.Text;
using System.Text.Json;
using VoiceCraft.Client.Locales;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void Constructor_LoadsPersistedSettings()
    {
        InitializeLocalizer();
        var stored = new SettingsStructure
        {
            InputSettings = new InputSettings { InputDevice = "Mic-1", InputVolume = 1.2f },
            OutputSettings = new OutputSettings { OutputDevice = "Speaker-1", OutputVolume = 1.4f },
            NetworkSettings = new NetworkSettings { McWssListenIp = "10.0.0.5", McWssHostPort = 9000 }
        };
        var storage = new FakeStorageService
        {
            ExistsResult = true,
            StoredBytes = JsonSerializer.SerializeToUtf8Bytes(stored, SettingsStructureGenerationContext.Default.SettingsStructure)
        };

        var service = new SettingsService(storage);

        Assert.Equal("Mic-1", service.InputSettings.InputDevice);
        Assert.Equal(1.2f, service.InputSettings.InputVolume);
        Assert.Equal("Speaker-1", service.OutputSettings.OutputDevice);
        Assert.Equal("10.0.0.5", service.NetworkSettings.McWssListenIp);
        Assert.Equal((ushort)9000, service.NetworkSettings.McWssHostPort);
    }

    [Fact]
    public async Task SaveImmediate_WritesSerializedSettings()
    {
        InitializeLocalizer();
        var storage = new FakeStorageService();
        var service = new SettingsService(storage);

        service.InputSettings.InputDevice = "Mic-2";
        service.OutputSettings.OutputDevice = "Speaker-2";
        service.ThemeSettings.SelectedTheme = Constants.LightThemeGuid;

        await service.SaveImmediate();

        Assert.Equal(Constants.SettingsFile, storage.LastSavedPath);
        Assert.NotNull(storage.StoredBytes);

        var saved = JsonSerializer.Deserialize(storage.StoredBytes, SettingsStructureGenerationContext.Default.SettingsStructure);
        Assert.NotNull(saved);
        Assert.Equal("Mic-2", saved.InputSettings.InputDevice);
        Assert.Equal("Speaker-2", saved.OutputSettings.OutputDevice);
        Assert.Equal(Constants.LightThemeGuid, saved.ThemeSettings.SelectedTheme);
    }

    private static void InitializeLocalizer()
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales");
    }

    private sealed class FakeStorageService : StorageService
    {
        public bool ExistsResult { get; set; }
        public byte[] StoredBytes { get; set; } = Encoding.UTF8.GetBytes("{}");
        public string? LastSavedPath { get; private set; }

        public override bool Exists(string directory)
        {
            return ExistsResult;
        }

        public override void Save(string directory, byte[] data)
        {
            LastSavedPath = directory;
            StoredBytes = data;
        }

        public override byte[] Load(string directory)
        {
            return StoredBytes;
        }

        public override Task SaveAsync(string directory, byte[] data)
        {
            Save(directory, data);
            return Task.CompletedTask;
        }

        public override Task<byte[]> LoadAsync(string directory)
        {
            return Task.FromResult(StoredBytes);
        }
    }
}
