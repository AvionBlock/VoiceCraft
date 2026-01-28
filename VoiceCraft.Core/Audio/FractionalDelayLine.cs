//MIT License
// 
// Copyright (c) 2017 Tim
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

// https://github.com/ar1st0crat/NWaves/blob/master/NWaves/Utils/FractionalDelayLine.cs#L8
namespace VoiceCraft.Core.Audio
{
    /// <summary>
    ///     Represents fractional delay line.
    /// </summary>
    public class FractionalDelayLine
    {
        /// <summary>
        ///     Delay line.
        /// </summary>
        private float[] _delayLine;

        /// <summary>
        ///     Current write position.
        /// </summary>
        private int _n;

        /// <summary>
        ///     Previously interpolated sample (used with InterpolationMode.Thiran).
        /// </summary>
        private float _prevInterpolated;

        /// <summary>
        ///     Constructs <see cref="FractionalDelayLine" /> and reserves given <paramref name="size" /> for its samples.
        /// </summary>
        /// <param name="size">Delay line size (number of samples)</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        public FractionalDelayLine(int size, InterpolationMode interpolationMode = InterpolationMode.Linear)
        {
            Size = Math.Max(4, size);
            _delayLine = new float[Size];
            _n = 0;

            InterpolationMode = interpolationMode;
        }

        /// <summary>
        ///     Constructs <see cref="FractionalDelayLine" /> and reserves the size
        ///     corresponding to <paramref name="maxDelay" /> seconds.
        /// </summary>
        /// <param name="samplingRate">Sampling rate</param>
        /// <param name="maxDelay">Max delay (in seconds)</param>
        /// <param name="interpolationMode">Interpolation mode</param>
        public FractionalDelayLine(int samplingRate,
            double maxDelay,
            InterpolationMode interpolationMode = InterpolationMode.Linear)
            : this((int)(samplingRate * maxDelay) + 1, interpolationMode)
        {
        }

        /// <summary>
        ///     Gets or sets interpolation mode.
        /// </summary>
        public InterpolationMode InterpolationMode { get; set; }

        /// <summary>
        ///     Gets the size of delay line (number of samples).
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        ///     Writes (puts) <paramref name="sample" /> to the delay line.
        /// </summary>
        public void Write(float sample)
        {
            _delayLine[_n] = sample;

            if (++_n >= Size) _n = 0;
        }

        /// <summary>
        ///     Reads (gets) sample from the delay line corresponding to given time <paramref name="delay" /> (in seconds).
        /// </summary>
        public float Read(double delay)
        {
            var precisePosition = (float)(_n - delay + Size) % Size;

            var intPosition = (int)precisePosition;

            var fraction = precisePosition - intPosition;

            switch (InterpolationMode)
            {
                case InterpolationMode.Nearest:
                {
                    return _delayLine[intPosition % Size];
                }

                case InterpolationMode.Cubic:
                {
                    var f2 = fraction * fraction;
                    var f3 = f2 * fraction;

                    var sample1 = _delayLine[(intPosition - 1 + Size) % Size];
                    var sample2 = _delayLine[intPosition];
                    var sample3 = _delayLine[(intPosition + 1) % Size];
                    var sample4 = _delayLine[(intPosition + 2) % Size];

                    var a0 = -0.5f * sample1 + 1.5f * sample2 - 1.5f * sample3 + 0.5f * sample4;
                    var a1 = sample1 - 2.5f * sample2 + 2.0f * sample3 - 0.5f * sample4;
                    var a2 = -0.5f * sample1 + 0.5f * sample3;
                    var a3 = sample2;

                    return a0 * f3 + a1 * f2 + a2 * fraction + a3;
                }

                case InterpolationMode.Thiran:
                {
                    var sample1 = _delayLine[intPosition];
                    var sample2 = _delayLine[(intPosition + 1) % Size];

                    // from DAFX book:
                    // var alpha = 1 - fraction;

                    // however, according to paper
                    // "A Lossless, Click-free, Pitchbend-able Delay Line Loop Interpolation Scheme"
                    // by Scott A. Van Duyne et al.:

                    if (fraction < 0.618) // keep fraction in range [0.618, 1.618] (golden ratio)
                        fraction++;

                    var alpha = (1 - fraction) / (1 + fraction);

                    var interpolated = sample2 + alpha * (sample1 - _prevInterpolated);

                    _prevInterpolated = interpolated;

                    // the processing scheme above is rather simple,
                    // so there may be audible artifacts in the output signal, anyway

                    return interpolated;
                }

                case InterpolationMode.Linear:
                default:
                {
                    var sample1 = _delayLine[intPosition];
                    var sample2 = _delayLine[(intPosition + 1) % Size];

                    return sample1 + fraction * (sample2 - sample1);
                }
            }
        }

        /// <summary>
        ///     Resets delay line.
        /// </summary>
        public void Reset()
        {
            Array.Clear(_delayLine, 0, Size);
            _n = 0;
            _prevInterpolated = 0;
        }

        /// <summary>
        ///     Resizes delay line to ensure new <paramref name="size" />.
        ///     If <paramref name="size" /> does not exceed current size of the delay line then nothing happens.
        /// </summary>
        public void Ensure(int size)
        {
            if (size <= Size) return;

            Array.Resize(ref _delayLine, size);

            Size = size;
        }

        /// <summary>
        ///     Resizes delay line to ensure new size corresponding to <paramref name="maxDelay" /> seconds.
        ///     If the new size does not exceed current size of the delay line then nothing happens.
        /// </summary>
        /// <param name="samplingRate">Sampling rate</param>
        /// <param name="maxDelay">Max delay (in seconds)</param>
        public void Ensure(int samplingRate, double maxDelay)
        {
            Ensure((int)(samplingRate * maxDelay) + 1);
        }
    }

    public enum InterpolationMode
    {
        Linear,
        Cubic,
        Thiran,
        Nearest
    }
}