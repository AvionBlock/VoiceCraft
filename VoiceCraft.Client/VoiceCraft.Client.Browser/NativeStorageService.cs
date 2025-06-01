using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser;

public class NativeStorageService : StorageService
{
    public override bool Exists(string directory)
    {
        return JsNativeStorage.Exists(directory);
    }

    public override byte[] Load(string directory)
    {
        return JsNativeStorage.Load(directory);
    }

    public override void Save(string directory, byte[] data)
    {
        JsNativeStorage.Save(directory, data);
    }

    public override Task<byte[]> LoadAsync(string directory)
    {
        return Task.FromResult(JsNativeStorage.Load(directory));
    }

    public override Task SaveAsync(string directory, byte[] data)
    {
        JsNativeStorage.Save(directory, data);
        return Task.CompletedTask;
    }
}

internal static partial class JsNativeStorage
{
    [JSImport("exists", "storage.js")]
    public static partial bool Exists(string directory);

    [JSImport("load", "storage.js")]
    public static partial byte[] Load(string directory);

    [JSImport("save", "storage.js")]
    public static partial void Save(string directory, byte[] data);
}