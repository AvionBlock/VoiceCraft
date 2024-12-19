using Arch.Core;

namespace VoiceCraft.Server.Components
{
    public class ClientComponent
    {
        private readonly World _world;
        private readonly Entity _entity;
        private readonly VoiceCraftServerClient _client;

        public ClientComponent(World world, ref Entity entity, VoiceCraftServerClient client)
        {
            _world = world;
            _entity = entity;
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