using NAudio.Wave;
using NWaves.Filters.Base;

namespace VoiceCraft.Client.PDK.Audio
{
    public class FilterChainSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public List<IOnlineFilter> Filters { get; }
        public WaveFormat WaveFormat => _source.WaveFormat;

        public FilterChainSampleProvider(ISampleProvider source)
        {
            _source = source;
            Filters = new List<IOnlineFilter>();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            int sample = 0;
            lock (Filters)
            {
                while (sample < read)
                {
                    for (int ch = 0; ch < _source.WaveFormat.Channels; ch++)
                    {
                        foreach (var filter in Filters)
                        {
                            buffer[offset + sample] = filter.Process(buffer[offset + sample]);
                        }
                        sample++;
                    }
                }
            }
            return read;
        }
    }
}
