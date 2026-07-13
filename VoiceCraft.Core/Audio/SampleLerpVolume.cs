using System;

namespace VoiceCraft.Core.Audio
{
    public class SampleLerpVolume
    {
        public float TargetVolume
        {
            get;
            set
            {
                if (Math.Abs(field - value) < Constants.FloatingPointTolerance)
                    return; //Return since it's the same target volume, and we don't want to reset the counter.
                _previousVolume = CurrentVolume;
                field = value;
                _fadeSamplesPosition = 0; //Reset position since we have a new target.
            }
        } = 1;

        public TimeSpan FadeDuration
        {
            get;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), "Fade duration cannot be negative.");

                var currentVolume = CurrentVolume;
                field = value;
                _fadeSamplesDuration = (int)(value.TotalMilliseconds * _sampleRate / 1000);
                _previousVolume = currentVolume;
                _fadeSamplesPosition = 0;
            }
        }

        private readonly int _sampleRate;
        private float _previousVolume;
        private int _fadeSamplesDuration;
        private int _fadeSamplesPosition;

        public SampleLerpVolume(int sampleRate, TimeSpan fadeDuration)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
            _sampleRate = sampleRate;
            _previousVolume = TargetVolume;
            FadeDuration = fadeDuration;
        }

        //Transforms values. (Can be multiple channels, interprets as 1 sample)
        public float Transform(float sample)
        {
            var value = CurrentVolume;
            sample = Math.Clamp(sample * value, -1, 1);
            return sample;
        }

        //Step forward 1 sample.
        public void Step()
        {
            if (_fadeSamplesPosition < _fadeSamplesDuration)
                _fadeSamplesPosition++;
        }

        private float CurrentVolume => _fadeSamplesDuration == 0
            ? TargetVolume
            : Lerp(_previousVolume, TargetVolume,
                Math.Clamp((float)_fadeSamplesPosition / _fadeSamplesDuration, 0, 1));

        private static float Lerp(float current, float target, float by)
        {
            return current * (1 - by) + target * by;
        }
    }
}
