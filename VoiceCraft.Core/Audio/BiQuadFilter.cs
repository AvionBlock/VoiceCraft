using System;

namespace VoiceCraft.Core.Audio
{
    public class BiQuadFilter
    {
        //Coefficients
        private double _a0;
        private double _a1;
        private double _a2;
        private double _a3;
        private double _a4;

        //State
        private float _x1;
        private float _x2;
        private float _y1;
        private float _y2;
        
        public float Transform(float inSample)
        {
            //Compute the result
            var result = _a0 * inSample + _a1 * _x1 + _a2 * _x2 - _a3 * _y1 - _a4 * _y2;

            // Shift x values. 
            _x2 = _x1;
            _x1 = inSample;

            // Shift y values.
            _y2 = _y1;
            _y1 = (float)result;

            return _y1;
        }

        public void SetLowPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosW0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 - cosW0) / 2;
            var b1 = 1 - cosW0;
            var b2 = (1 - cosW0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosW0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        public void SetHighPassFilter(float sampleRate, float cutoffFrequency, float q)
        {
            var w0 = 2 * Math.PI * cutoffFrequency / sampleRate;
            var cosW0 = Math.Cos(w0);
            var alpha = Math.Sin(w0) / (2 * q);

            var b0 = (1 + cosW0) / 2;
            var b1 = -(1 + cosW0);
            var b2 = (1 + cosW0) / 2;
            var aa0 = 1 + alpha;
            var aa1 = -2 * cosW0;
            var aa2 = 1 - alpha;
            SetCoefficients(aa0, aa1, aa2, b0, b1, b2);
        }

        private void SetCoefficients(double aa0, double aa1, double aa2, double b0, double b1, double b2)
        {
            // precompute the coefficients
            _a0 = b0 / aa0;
            _a1 = b1 / aa0;
            _a2 = b2 / aa0;
            _a3 = aa1 / aa0;
            _a4 = aa2 / aa0;
        }
    }
}