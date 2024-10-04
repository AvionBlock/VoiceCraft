using System;

namespace VoiceCraft.Core.Audio.Filters
{
    public class WebRtcFilter : EchoCancelFilter
    {

        private readonly AecCore aec;
        private readonly NoiseSuppressor ns;
        private readonly HighPassFilter highPassFilter = new HighPassFilter();
        private readonly bool enableAec;
        private readonly bool enableDenoise;
        private readonly bool enableAgc;
        private readonly Agc agc;

        public WebRtcFilter(int expectedAudioLatency, int filterLength,
            AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat,
            bool enableAec, bool enableDenoise, bool enableAgc,
            IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null) :
            base(expectedAudioLatency, filterLength, recordedAudioFormat, playedAudioFormat, playedResampler, recordedResampler)
        {
            // Default settings.
            var aecConfig = new AecConfig(FilterLength, recordedAudioFormat.SamplesPerFrame, recordedAudioFormat.SamplesPerSecond)
            {
                NlpMode = AecNlpMode.KAecNlpModerate,
                SkewMode = false,
                MetricsMode = false
            };
            ns = new NoiseSuppressor(recordedAudioFormat);
            aec = new AecCore(aecConfig);

            if (aecConfig.NlpMode != AecNlpMode.KAecNlpConservative &&
                aecConfig.NlpMode != AecNlpMode.KAecNlpModerate &&
                aecConfig.NlpMode != AecNlpMode.KAecNlpAggressive)
            {
                throw new ArgumentException();
            }

            aec.targetSupp = WebRtcConstants.targetSupp[(int)aecConfig.NlpMode];
            aec.minOverDrive = WebRtcConstants.minOverDrive[(int)aecConfig.NlpMode];

            if (aecConfig.MetricsMode && aecConfig.MetricsMode != true)
            {
                throw new ArgumentException();
            }
            aec.metricsMode = aecConfig.MetricsMode;
            if (aec.metricsMode)
            {
                aec.InitMetrics();
            }
            this.enableAec = enableAec;
            this.enableDenoise = enableDenoise;
            this.enableAgc = enableAgc;

            agc = new Agc(0, 255, Agc.AgcMode.AgcModeAdaptiveDigital, (uint)recordedAudioFormat.SamplesPerSecond);
        }

        protected override void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame)
        {

            // ks 11/2/11 - This seems to be more-or-less the order in which things are processed in the WebRtc audio_processing_impl.cc file.
            highPassFilter.Filter(recorded);

            if (enableAgc)
            {
                agc.WebRtcAgc_AddFarend(played, (short)played.Length);
                gain_control_AnalyzeCaptureAudio(recorded);
            }

            if (enableAec)
            {
                aec.ProcessFrame(recorded, played, outFrame, 0);
            }
            else
            {
                Buffer.BlockCopy(recorded, 0, outFrame, 0, SamplesPerFrame * sizeof(short));
            }

            if (enableDenoise)
            {
                // ks 11/14/11 - The noise suppressor only supports 10 ms blocks. I might be able to fix that,
                // but this is easier for now.
                ns.ProcessFrame(outFrame, 0, outFrame, 0);
                ns.ProcessFrame(outFrame, recordedAudioFormat.SamplesPer10Ms, outFrame, recordedAudioFormat.SamplesPer10Ms);
            }

            if (enableAgc)
            {
                gain_control_ProcessCaptureAudio(outFrame);
            }
        }

        void gain_control_AnalyzeCaptureAudio(short[] data)
        {
            const int analogCaptureLevel = 127;
            //WriteDebugMessage(String.Format("(C#) AGC 503   analog_capture_level_ = {0}", analog_capture_level_));
            int v;
            //WriteDebugMessage(String.Format("(C#) AGC 03001     data[143] = {0}", data[143]));//todo
            agc.WebRtcAgc_VirtualMic(data, null, (short)data.Length, analogCaptureLevel, out v);
            captureLevels0 = v;
            //micLevelIn = v;
        }

        int captureLevels0 = 127;
        void gain_control_ProcessCaptureAudio(short[] data)
        {
            bool saturationWarning;
            int captureLevelOut;
            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC capture_levels_0", (double)capture_levels_0, 1);
            agc.WebRtcAgc_Process(data, null, (short)data.Length, data, null, captureLevels0, out captureLevelOut, 0, out saturationWarning);

            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC capture_level_out", (double)capture_level_out, 1);
            captureLevels0 = captureLevelOut;
        }
    }
}