using System.Threading.Tasks;

namespace VoiceCraft.Client.Services
{
    public abstract class StorageService
    {
        public abstract bool Exists(string directory);
        
        public abstract void Save(string directory, byte[] data);

        public abstract byte[] Load(string directory);
        
        public abstract Task SaveAsync(string directory, byte[] data);

        public abstract Task<byte[]> LoadAsync(string directory);
    }
}