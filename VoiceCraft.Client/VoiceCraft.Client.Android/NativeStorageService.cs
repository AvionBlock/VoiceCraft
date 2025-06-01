using System;
using System.IO;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android;

public class NativeStorageService : StorageService
{
    private static readonly string ApplicationDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.ApplicationDirectory);

    public override bool Exists(string directory)
    {
        return File.Exists(Path.Combine(ApplicationDirectory, directory));
    }

    public override byte[] Load(string directory)
    {
        return File.ReadAllBytes(Path.Combine(ApplicationDirectory, directory));
    }

    public override void Save(string directory, byte[] data)
    {
        CreateDirectoryIfNotExists();
        File.WriteAllBytes(Path.Combine(ApplicationDirectory, directory), data);
    }

    public override async Task<byte[]> LoadAsync(string directory)
    {
        return await File.ReadAllBytesAsync(Path.Combine(ApplicationDirectory, directory));
    }

    public override async Task SaveAsync(string directory, byte[] data)
    {
        CreateDirectoryIfNotExists();
        await File.WriteAllBytesAsync(Path.Combine(ApplicationDirectory, directory), data);
    }

    private static void CreateDirectoryIfNotExists()
    {
        if (!Directory.Exists(ApplicationDirectory))
            Directory.CreateDirectory(ApplicationDirectory);
    }
}