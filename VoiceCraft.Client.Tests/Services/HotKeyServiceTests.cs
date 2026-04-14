using VoiceCraft.Client.Locales;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Tests.Services;

public class HotKeyServiceTests
{
    [Fact]
    public void SetBinding_ReplacesExistingConflictAndNormalizesCombo()
    {
        var service = CreateService();

        service.SetBinding("PushToTalk", "LeftControl\0LeftControl\0P");

        Assert.Equal("LeftControl\0P", service.GetBindings().Single(x => x.Action.Id == "PushToTalk").KeyCombo);
    }

    [Fact]
    public void SetBinding_RemovesConflictFromOtherAction()
    {
        var service = CreateService();

        service.SetBinding("PushToTalk", "LeftControl\0LeftShift\0M");

        var bindings = service.GetBindings();
        Assert.Equal("LeftControl\0LeftShift\0M", bindings.Single(x => x.Action.Id == "PushToTalk").KeyCombo);
        Assert.Equal("LeftControl\0LeftShift\0D", bindings.Single(x => x.Action.Id == "Deafen").KeyCombo);
    }

    [Fact]
    public void NormalizeKeyCombo_SortsInputsToMakeBindingsOrderIndependent()
    {
        var normalized = HotKeyService.NormalizeKeyCombo("MouseButton4\0LeftShift\0LeftControl");

        Assert.Equal("LeftControl\0LeftShift\0MouseButton4", normalized);
    }

    [Theory]
    [InlineData("Left", "MouseLeft")]
    [InlineData("XButton1", "MouseButton4")]
    [InlineData("Button5", "MouseButton5")]
    public void NormalizeMouseButton_ReturnsCanonicalButtonName(string input, string expected)
    {
        Assert.Equal(expected, HotKeyService.NormalizeMouseButton(input));
    }

    private static TestHotKeyService CreateService()
    {
        InitializeLocalizer();
        var storage = new FakeStorageService();
        var settings = new SettingsService(storage);
        return new TestHotKeyService(
            [new TestAction("Mute", "Mute", "LeftControl\0LeftShift\0M"),
             new TestAction("Deafen", "Deafen", "LeftControl\0LeftShift\0D"),
             new TestAction("PushToTalk", "PushToTalk", "LeftControl")],
            settings);
    }

    private static void InitializeLocalizer()
    {
        Localizer.BaseLocalizer = new EmbeddedJsonLocalizer("VoiceCraft.Client.Locales");
    }

    private sealed class TestHotKeyService(IEnumerable<HotKeyAction> actions, SettingsService settingsService)
        : HotKeyService(actions, settingsService)
    {
        protected override void InitializeCore()
        {
        }
    }

    private sealed class TestAction(string id, string title, string defaultKeyCombo) : HotKeyAction
    {
        public override string Id => id;
        public override string Title => title;
        public override string DefaultKeyCombo => defaultKeyCombo;
    }

    private sealed class FakeStorageService : StorageService
    {
        public override bool Exists(string directory) => false;
        public override void Save(string directory, byte[] data) { }
        public override byte[] Load(string directory) => [];
        public override Task SaveAsync(string directory, byte[] data) => Task.CompletedTask;
        public override Task<byte[]> LoadAsync(string directory) => Task.FromResult<byte[]>([]);
    }
}
