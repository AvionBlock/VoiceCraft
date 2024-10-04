using NAudio.Wave;

namespace VoiceCraft.Client.PDK.Audio
{
    //I don't know if this should be changed to LerpedVolumeSampleProvider.
    public class FadingVolumeSampleProvider : ISampleProvider
    {
        private ISampleProvider _source;
        private float _fadeSamplePosition;
        private int _fadeSamples;
        private float _fadeDurationMS;
        private float _targetVolume;
        private float _previousVolume;

        public WaveFormat WaveFormat => _source.WaveFormat;
        public float TargetVolume
        {
            get
            {
                return _targetVolume;
            }
            set
            {
                if (_targetVolume == value) return; //Return since it's the same target volume, and we don't want to reset the counter.
                _previousVolume = _targetVolume;
                _targetVolume = value;
                _fadeSamplePosition = 0.0f; //Reset position since we have a new target.
            }
        }
        public float FadeDurationMS
        {
            get
            {
                return _fadeDurationMS;
            }
            set
            {
                _fadeDurationMS = value;
                var newSamples = (int)(FadeDurationMS * _source.WaveFormat.SampleRate / 1000);
                if (newSamples < _fadeSamplePosition) //Make sure we don't overshoot the target when lerping.
                {
                    _fadeSamplePosition = newSamples;
                }
                _fadeSamples = newSamples;
            }
        }

        public FadingVolumeSampleProvider(ISampleProvider source, float fadeDurationMS = 20)
        {
            this._source = source;
            _fadeSamplePosition = 0.0f;
            FadeDurationMS = fadeDurationMS;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = _source.Read(buffer, offset, count);
            int sample = 0;
            while (sample < read)
            {
                var value = Lerp(_previousVolume, TargetVolume, _fadeSamplePosition / _fadeSamples);
                for (int ch = 0; ch < _source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= value;
                }

                //Make sure we set all others ahead to the same volume value and not overshoot the target...
                if (_fadeSamplePosition <= _fadeSamples)
                {
                    _fadeSamplePosition++;
                }
            }
            return read;
        }

        private float Lerp(float current, float target, float by)
        {
            return current * (1 - by) + target * by;
        }
    }
}