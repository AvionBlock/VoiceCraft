using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Tests.Models.Settings;

public class SettingsModelTests
{
    [Fact]
    public void InputSettings_ValidateRanges_AndRaiseUpdated()
    {
        var settings = new InputSettings();
        var updatedCount = 0;
        settings.OnUpdated += _ => updatedCount++;

        settings.InputVolume = 1.5f;
        settings.MicrophoneSensitivity = 0.5f;

        Assert.Equal(2, updatedCount);
        Assert.Throws<ArgumentException>(() => settings.InputVolume = 3f);
        Assert.Throws<ArgumentException>(() => settings.MicrophoneSensitivity = -0.1f);
    }

    [Fact]
    public void OutputSettings_ValidateRanges_AndDefaultClipper()
    {
        var settings = new OutputSettings();

        Assert.Equal(Constants.TanhSoftAudioClipperGuid, settings.AudioClipper);

        settings.OutputVolume = 1.75f;

        Assert.Equal(1.75f, settings.OutputVolume);
        Assert.Throws<ArgumentException>(() => settings.OutputVolume = -1f);
    }

    [Fact]
    public void ServersSettings_AddServer_ValidatesAndPreservesNewestFirst()
    {
        var settings = new ServersSettings();
        var first = new ServerSettings { Name = "Alpha", Ip = "127.0.0.1", Port = 9050 };
        var second = new ServerSettings { Name = "Beta", Ip = "127.0.0.2", Port = 9051 };

        settings.AddServer(first);
        settings.AddServer(second);

        Assert.Equal(["Beta", "Alpha"], settings.Servers.Select(x => x.Name));
        Assert.Throws<ArgumentException>(() => settings.AddServer(new ServerSettings { Name = "", Ip = "127.0.0.1", Port = 9050 }));
        Assert.Throws<ArgumentException>(() => settings.AddServer(new ServerSettings { Name = "Alpha", Ip = "", Port = 9050 }));
    }

    [Fact]
    public void UserSettings_Clone_DeepCopiesUsers()
    {
        var userId = Guid.NewGuid();
        var settings = new UserSettings
        {
            Users = new Dictionary<Guid, UserSetting>
            {
                [userId] = new() { UserMuted = true, Volume = 0.25f }
            }
        };

        var clone = (UserSettings)settings.Clone();
        clone.Users[userId].Volume = 0.75f;
        clone.Users[userId].UserMuted = false;

        Assert.Equal(0.25f, settings.Users[userId].Volume);
        Assert.True(settings.Users[userId].UserMuted);
    }

    [Fact]
    public void Server_ValidatePropertyLimits()
    {
        var server = new ServerSettings();

        server.Name = "Voice";
        server.Ip = "192.168.0.1";
        server.Port = 9050;

        Assert.Equal("Voice", server.Name);
        Assert.Throws<ArgumentException>(() => server.Name = new string('a', ServerSettings.NameLimit + 1));
        Assert.Throws<ArgumentException>(() => server.Ip = new string('b', ServerSettings.IpLimit + 1));
    }
}
