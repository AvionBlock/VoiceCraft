using System;
// using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Browser
{
    public class NativeStorageService : StorageService
    {

        public override bool Exists(string directory) =>
            EmbedInteropItem.Get(directory) != null;

        public override byte[] Load(string directory)
        {
            var data = EmbedInteropItem.Get(directory);
            if (data == null) {
                throw new Exception($"'{data}' is null!");
            }
            return Convert.FromBase64String(data);
        }

        public override void Save(string directory, byte[] data)
        {
            EmbedInteropItem.Set(directory, Convert.ToBase64String(data));
        }

        private async Task<byte[]> LoadAsyncInternal(string directory)
        {
            var data = await EmbedInteropItem.GetAsync(directory);
            if (data == null) {
                throw new Exception($"'{data}' is null!");
            }
            return Convert.FromBase64String(data);
        }

        public override async Task<byte[]> LoadAsync(string directory)
        {
            return await LoadAsyncInternal(directory);
        }

        public override async Task SaveAsync(string directory, byte[] data)
        {
            EmbedInteropItem.SetAsync(directory, Convert.ToBase64String(data));
        }
    }

    internal static partial class EmbedInteropItem {
        [JSImport("globalThis.localStorage.setItem")]
        public static partial void Set(string key, string val);

        [JSImport("setItemAsync", "storage.js")]
        public static partial Task SetAsync(string key, string val);

        [JSImport("globalThis.localStorage.getItem")]
        public static partial string? Get(string key);

        [JSImport("getItemAsync", "storage.js")]
        public static partial Task<string?> GetAsync(string key);
    }
}
