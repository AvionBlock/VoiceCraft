using System;

namespace VoiceCraft.Core.Audio
{
    public class LerpSampleVolume
    {
        public float TargetVolume
        {
            get => _targetVolume;
            set
            {
                if (Math.Abs(_targetVolume - value) < Constants.FloatingPointTolerance)
                    return; //Return since it's the same target volume, and we don't want to reset the counter.
                _previousVolume = _targetVolume;
                _targetVolume = value;
                _fadeSamplesPosition = 0; //Reset position since we have a new target.
            }
        }

        public TimeSpan FadeDuration
        {
            get => _fadeDuration;
            set
            {
                _fadeDuration = value;
                var newSamples = (int)(value.TotalMilliseconds * _sampleRate / 1000);
                if (newSamples < _fadeSamplesPosition) //Make sure we don't overshoot the target when lerping.
                {
                    _fadeSamplesPosition = newSamples;
                }

                _fadeSamplesDuration = newSamples;
            }
        }

        private int _sampleRate;
        private TimeSpan _fadeDuration;
        private float _targetVolume = 1;
        private float _previousVolume;
        private float _fadeSamplesDuration;
        private float _fadeSamplesPosition;

        public LerpSampleVolume(int sampleRate, TimeSpan fadeDuration)
        {
            _sampleRate = sampleRate;
            FadeDuration = fadeDuration;
        }

        //Transforms values. (Can be multiple channels, interprets as 1 sample)
        public float Transform(float sample)
        {
            var value = Lerp(_previousVolume, TargetVolume, _fadeSamplesPosition / _fadeSamplesDuration); 
            sample = Math.Clamp(sample * value, -1, 1);
            return sample;
        }

        //Step forward 1 sample.
        public void Step()
        {
            if(_fadeSamplesPosition > _fadeSamplesDuration) return;
            _fadeSamplesPosition++;
        }

        private static float Lerp(float current, float target, float by)
        {
            return current * (1 - by) + target * by;
        }
    }
}