using OpusSharp.Core;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.Structures;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network
{
    public class VoiceCraftClientEntity(byte id) : VoiceCraftEntity(id)
    {
        private readonly SpeexDSPJitterBuffer _jitterBuffer = new(Constants.SamplesPerFrame);
        private readonly OpusDecoder _decoder = new(Constants.SampleRate, Constants.Channels);
        private readonly byte[] _decodeData = new byte[Constants.BytesPerFrame];

        public int Read(byte[] buffer)
        {
            if (buffer.Length < Constants.BytesPerFrame)
                return 0;

            try
            {
                var outPacket = new SpeexDSPJitterBufferPacket(_decodeData, (uint)_decodeData.Length);
                var startOffset = 0;
                return _jitterBuffer.Get(ref outPacket, Constants.SamplesPerFrame, ref startOffset) == JitterBufferState.JITTER_BUFFER_OK ? 
                    _decoder.Decode(_decodeData, (int)outPacket.len, buffer, Constants.SamplesPerFrame, false) :
                    _decoder.Decode(null, 0, buffer, Constants.SamplesPerFrame, false);
            }
            catch
            {
                return 0;
            }
            finally
            {
                _jitterBuffer.Tick();
            }
        }

        public override void ReceiveAudio(byte[] buffer, uint timestamp)
        {
            var inPacket = new SpeexDSPJitterBufferPacket(buffer, (uint)buffer.Length);
            _jitterBuffer.Put(ref inPacket);
        }
    }
}