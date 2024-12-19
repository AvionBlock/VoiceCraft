namespace VoiceCraft.Server.Components
{
    public class ClientComponent
    {
        private readonly VoiceCraftServerClient _client;

        public ClientComponent(VoiceCraftServerClient client)
        {
            _client = client;
            _client.OnAudioReceived += ClientAudioReceived;
        }

        private void ClientAudioReceived(byte[] audioData)
        {
            //Find all other clients that can receive the audio data and send it.
            throw new NotImplementedException();
        }
    }
}