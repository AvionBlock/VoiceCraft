namespace VoiceCraft.Server
{
    public class VoiceCraftServerClient
    {
        public event Action<byte[]> OnAudioReceived;
    }
}