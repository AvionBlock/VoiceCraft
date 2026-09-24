using System.IO;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Tests.Core.Locales;

public class LocalizationTests
{
    [Fact]
    public void Reload_LoadsEmbeddedClientTranslations()
    {
        var localizer = new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Client");

        Assert.Equal("en-US", localizer.Reload("en-US"));
        Assert.Contains("en-US", localizer.Languages);
        Assert.Contains("ru-RU", localizer.Languages);
        Assert.Equal("Language", localizer.Get("Settings.General.Language"));
        Assert.Equal("Alice has been removed.", localizer.Get("Servers.Notification.Removed:Alice"));
    }

    [Fact]
    public void Reload_UnknownLanguage_UsesFallback()
    {
        var localizer = new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Client");

        Assert.Equal("en-US", localizer.Reload("unknown"));
        Assert.Equal("Servers", localizer.Get("Servers.Title"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKey()
    {
        var localizer = new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Client");
        localizer.Reload("en-US");

        Assert.Equal("Missing.Key", localizer.Get("Missing.Key"));
    }

    [Fact]
    public void Reload_UnknownResourceDirectory_Throws()
    {
        var localizer = new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Missing");

        Assert.Throws<FileNotFoundException>(() => localizer.Reload("en-US"));
    }

    [Fact]
    public void CombinedLocaliser_UsesServerTranslationsForKeysMissingFromClient()
    {
        var localizer = new CombinedLocaliser(
            new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Client"),
            new EmbeddedJsonLocalizer("VoiceCraft.Core.Locales.Server"));

        Assert.Equal("en-US", localizer.Reload("en-US"));
        Assert.Equal("Settings", localizer.Get("Settings.Title"));
        Assert.Equal("Server starting...", localizer.Get("Startup.Starting"));
        Assert.Equal("Missing.Key", localizer.Get("Missing.Key"));
    }
}
