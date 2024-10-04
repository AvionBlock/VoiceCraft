using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Audio.Filters
{
    /// <summary>
    /// ks 9/26/11 - This class isn't used in our current AEC, as it solves problems that we've solved in a more generalizable fashion.
    /// </summary>
    public class AecPc
    {
        #region state variables

        private readonly AecCore aec;
        private readonly AecConfig aecConfig;
        public readonly RingBuffer farendBuf;
        private readonly List<short[]> farendOld;
        private readonly bool resample; // if the skew is small enough we don't resample
        private readonly int sampFreq;
        private readonly int scSampFreq;
        private readonly int splitSampFreq;
        private bool ECstartup;
        private short activity;
        private short autoOnOff;
        private short bufSizeStart;
        //short bufResetCtr;  // counts number of noncausal frames
        private short checkBufSizeCtr;

        // Variables used for delay shifts
        private bool checkBuffSize;
        private short counter;
        private int delayChange;
        private int delayCtr;
        private short filtDelay;
        private short firstVal;
        private int highSkewCtr;
        private short initFlag; // indicates if AEC has been initialized
        private int knownDelay;
        private short lastDelayDiff;

        private int lastError;
        private short msInSndCardBuf;
        private AecNlpMode nlpMode;
        private float sampFactor; // scSampRate / sampFreq
        private float skew;
        private int skewFrCtr;
        private bool skewMode;
        private short sum;
        private int timeForDelayChange;

        #endregion

        public AecPc(int filterLengthInSamples, int samplesPerFrame, int samplesPerSecond)
        {
            // Default settings.
            aecConfig = new AecConfig(filterLengthInSamples, samplesPerFrame, samplesPerSecond);
            aecConfig.NlpMode = AecNlpMode.KAecNlpModerate;
            aecConfig.SkewMode = false;
            aecConfig.MetricsMode = false;
            farendOld = new List<short[]> { new short[aecConfig.SamplesPerFrame], new short[aecConfig.SamplesPerFrame] };
            set_config(aecConfig);
            aec = new AecCore(aecConfig);
            farendBuf = new RingBuffer(aecConfig.BufSizeSamp);
            //if (WebRtcAec_CreateResampler(&aecpc->resampler) == -1) 

            sampFreq = 16000;
            scSampFreq = 48000;

            //WebRtcAec_InitResampler(aecpc->resampler, aecpc->scSampFreq)

            splitSampFreq = sampFreq;

            skewFrCtr = 0;
            activity = 0;

            delayChange = 1;
            delayCtr = 0;

            sum = 0;
            counter = 0;
            checkBuffSize = true;
            firstVal = 0;

            ECstartup = true;
            bufSizeStart = 0;
            checkBufSizeCtr = 0;
            filtDelay = 0;
            timeForDelayChange = 0;
            knownDelay = 0;
            lastDelayDiff = 0;

            skew = 0;
            resample = false;
            highSkewCtr = 0;
            sampFactor = (scSampFreq * 1.0f) / splitSampFreq;

        }

        private void set_config(AecConfig config)
        {
            if (config.SkewMode && config.SkewMode != true)
            {
                throw new ArgumentException();
            }
            skewMode = config.SkewMode;

            if (config.NlpMode != AecNlpMode.KAecNlpConservative &&
                config.NlpMode != AecNlpMode.KAecNlpModerate &&
                config.NlpMode != AecNlpMode.KAecNlpAggressive)
            {
                throw new ArgumentException();
            }
            nlpMode = config.NlpMode;
            aec.targetSupp = WebRtcConstants.targetSupp[(int)nlpMode];
            aec.minOverDrive = WebRtcConstants.minOverDrive[(int)nlpMode];

            if (config.MetricsMode && config.MetricsMode != true)
            {
                throw new ArgumentException();
            }
            aec.metricsMode = config.MetricsMode;
            if (aec.metricsMode)
            {
                aec.InitMetrics();
            }
        }

        public void WebRtcAec_Process(short[] nearend, short streamDelayMs, int skew)
        {
            int nrOfSamples = nearend.Length;
            short msInSndCardBuf = streamDelayMs;

            int retVal = 0;
            var farend = new short[aecConfig.SamplesPerFrame];
            short nmbrOfFilledBuffers;

            // Limit resampling to doubling/halving of signal
            const float minSkewEst = -0.5f;
            const float maxSkewEst = 1.0f;

            // number of samples == 160 for SWB input
            if (nrOfSamples != 80 && nrOfSamples != 160)
            {
                throw new ArgumentException();
            }

            // Check for valid pointers based on sampling rate
            if (sampFreq == 32000)
            {
                throw new ArgumentException();
            }

            if (msInSndCardBuf < 0)
            {
                msInSndCardBuf = 0;
                //throw new ArgumentException();//todo:warning
                retVal = -1;
            }
            else if (msInSndCardBuf > 500)
            {
                msInSndCardBuf = 500;
                //todo:warning
                retVal = -1;
            }
            msInSndCardBuf += 10;
            this.msInSndCardBuf = msInSndCardBuf;

            if (skewMode)
            {
                //if (aecpc->skewFrCtr < 25) {
                //    aecpc->skewFrCtr++;
                //}
                //else {
                //    retVal = WebRtcAec_GetSkew(aecpc->resampler, skew, &aecpc->skew);
                //    if (retVal == -1) {
                //        aecpc->skew = 0;
                //        aecpc->lastError = AEC_BAD_PARAMETER_WARNING;
                //    }

                //    aecpc->skew /= aecpc->sampFactor*nrOfSamples;

                //    if (aecpc->skew < 1.0e-3 && aecpc->skew > -1.0e-3) {
                //        aecpc->resample = kAecFalse;
                //    }
                //    else {
                //        aecpc->resample = kAecTrue;
                //    }

                //    if (aecpc->skew < minSkewEst) {
                //        aecpc->skew = minSkewEst;
                //    }
                //    else if (aecpc->skew > maxSkewEst) {
                //        aecpc->skew = maxSkewEst;
                //    }

                //}
            }

            var nFrames = (short)(nrOfSamples / aecConfig.SamplesPerFrame);
            var nBlocks10Ms = (short)(nFrames / aec.mult);

            WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 01               ECstartup = {0}", ECstartup));
            if (ECstartup)
            {
                nmbrOfFilledBuffers = (short)(farendBuf.get_buffer_size() / aecConfig.SamplesPerFrame);

                // The AEC is in the start up mode
                // AEC is disabled until the soundcard buffer and farend buffers are OK

                // Mechanism to ensure that the soundcard buffer is reasonably stable.
                if (checkBuffSize)
                {
                    checkBufSizeCtr++;
                    // Before we fill up the far end buffer we require the amount of data on the
                    // sound card to be stable (+/-8 ms) compared to the first value. This
                    // comparison is made during the following 4 consecutive frames. If it seems
                    // to be stable then we start to fill up the far end buffer.

                    if (counter == 0)
                    {
                        firstVal = this.msInSndCardBuf;
                        sum = 0;
                    }

                    if (Math.Abs(firstVal - this.msInSndCardBuf) <
                        WebRtcUtil.WEBRTC_SPL_MAX((int)(0.2 * this.msInSndCardBuf), WebRtcConstants.sampMsNb))
                    {
                        sum += this.msInSndCardBuf;
                        counter++;
                    }
                    else
                    {
                        counter = 0;
                    }

                    if (counter * nBlocks10Ms >= 6)
                    {
                        // The farend buffer size is determined in blocks of 80 samples
                        // Use 75% of the average value of the soundcard buffer
                        bufSizeStart = (short)WebRtcUtil.WEBRTC_SPL_MIN((int)(0.75 * (sum * aec.mult) / (counter * 10)), WebRtcConstants.BUF_SIZE_FRAMES);
                        // buffersize has now been determined
                        checkBuffSize = false;
                    }

                    if (checkBufSizeCtr * nBlocks10Ms > 50)
                    {
                        // for really bad sound cards, don't disable echocanceller for more than 0.5 sec
                        bufSizeStart = (short)WebRtcUtil.WEBRTC_SPL_MIN((int)(0.75 * (this.msInSndCardBuf * aec.mult) / 10), WebRtcConstants.BUF_SIZE_FRAMES);
                        checkBuffSize = false;
                    }
                }

                // if checkBuffSize changed in the if-statement above
                if (!checkBuffSize)
                {
                    // soundcard buffer is now reasonably stable
                    // When the far end buffer is filled with approximately the same amount of
                    // data as the amount on the sound card we end the start up phase and start
                    // to cancel echoes.

                    if (nmbrOfFilledBuffers == bufSizeStart)
                    {
                        ECstartup = false; // Enable the AEC
                    }
                    else if (nmbrOfFilledBuffers > bufSizeStart)
                    {
                        farendBuf.Flush(farendBuf.get_buffer_size() - bufSizeStart * aecConfig.SamplesPerFrame);
                        ECstartup = false;
                    }
                }
            }
            else
            {
                // AEC is enabled

                // Note only 1 block supported for nb and 2 blocks for wb
                for (int i = 0; i < nFrames; i++)
                {
                    nmbrOfFilledBuffers = (short)(farendBuf.get_buffer_size() / aecConfig.SamplesPerFrame);

                    // Check that there is data in the far end buffer
                    if (nmbrOfFilledBuffers > 0)
                    {
                        // Get the next 80 samples from the farend buffer
                        farendBuf.Read(farend, aecConfig.SamplesPerFrame);

                        // Always store the last frame for use when we run out of data
                        farendOld[i] = farend;
                    }
                    else
                    {
                        // We have no data so we use the last played frame
                        farend = farendOld[i];
                    }

                    // Call buffer delay estimator when all data is extracted,
                    // i.e. i = 0 for NB and i = 1 for WB or SWB
                    if ((i == 0 && splitSampFreq == 8000) ||
                        (i == 1 && (splitSampFreq == 16000)))
                    {
                        EstBufDelay(this.msInSndCardBuf);
                    }

                    // Call the AEC
                    var nearend80 = new short[aecConfig.SamplesPerFrame];
                    Buffer.BlockCopy(nearend, aecConfig.SamplesPerFrame * i * sizeof(short), nearend80, 0, aecConfig.SamplesPerFrame * sizeof(short));
                    aec.ProcessFrame(nearend80, farend, nearend80, knownDelay);
                    Buffer.BlockCopy(nearend80, 0, nearend, aecConfig.SamplesPerFrame * i * sizeof(short), aecConfig.SamplesPerFrame * sizeof(short));
                }
            }
        }

        private void EstBufDelay(short msInSndCardBuf)
        {
            short delayNew, nSampFar, nSampSndCard;
            short diff;

            nSampFar = (short)(farendBuf.get_buffer_size());
            nSampSndCard = (short)(msInSndCardBuf * WebRtcConstants.sampMsNb * aec.mult);

            delayNew = (short)(nSampSndCard - nSampFar);

            // Account for resampling frame delay
            if (skewMode && resample)
            {
                throw new NotImplementedException();
                //delayNew -= kResamplingDelay;
            }

            if (delayNew < aecConfig.SamplesPerFrame)
            {
                farendBuf.Flush(aecConfig.SamplesPerFrame);
                delayNew += (short)aecConfig.SamplesPerFrame;
            }

            filtDelay = WebRtcUtil.WEBRTC_SPL_MAX((short)0, (short)(0.8 * filtDelay + 0.2 * delayNew));

            diff = (short)(filtDelay - knownDelay);
            if (diff > 224)
            {
                if (lastDelayDiff < 96)
                {
                    timeForDelayChange = 0;
                }
                else
                {
                    timeForDelayChange++;
                }
            }
            else if (diff < 96 && knownDelay > 0)
            {
                if (lastDelayDiff > 224)
                {
                    timeForDelayChange = 0;
                }
                else
                {
                    timeForDelayChange++;
                }
            }
            else
            {
                timeForDelayChange = 0;
            }
            lastDelayDiff = diff;

            if (timeForDelayChange > 25)
            {
                knownDelay = WebRtcUtil.WEBRTC_SPL_MAX(filtDelay - 160, 0);
            }
            return;
        }
    }


}
