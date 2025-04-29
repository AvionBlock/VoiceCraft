using System;
using System.IO;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Windows
{
    public class NativeStorageService : StorageService
    {
        public override bool Exists(string directory) =>
            File.Exists(directory);
        public override byte[] Load(string directory) =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, directory));
        
        public override void Save(string directory, byte[] data) =>
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, directory), data);
        
        public override async Task<byte[]> LoadAsync(string directory) =>
            await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, directory));

        public override async Task SaveAsync(string directory, byte[] data) =>
            await File.WriteAllBytesAsync(Path.Combine(AppContext.BaseDirectory, directory), data);
    }
}