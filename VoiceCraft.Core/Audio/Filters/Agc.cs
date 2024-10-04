using System;

namespace VoiceCraft.Core.Audio.Filters
{
    /// <summary>
    /// contains AGC state variables
    /// original source: webrtc\trunk\modules\audio_processing\agc\main\source\analog_agc.h
    /// </summary>
    class Agc
    {
        public enum AgcMode
        {
            AgcModeUnchanged = 0,
            AgcModeAdaptiveAnalog,
            AgcModeAdaptiveDigital,
            AgcModeFixedDigital
        }

        enum AgcBoolean
        {
            AgcFalse = 0,
            AgcTrue = 1
        }

        class WebRtcAgcConfig
        {
            public short targetLevelDbfs;   // default 3 (-3 dBOv)
            public short compressionGaindB; // default 9 dB
            public AgcBoolean limiterEnable;     // default kAgcTrue (on)
        }

        class AgcVad
        {
            readonly int[] downState = new int[8];
            short HPstate;
            public short counter;
            public short logRatio; // log( P(active) / P(inactive) ) (Q10)
            short meanLongTerm; // Q10
            int varianceLongTerm; // Q8
            public short stdLongTerm; // Q10
            short meanShortTerm; // Q10
            int varianceShortTerm; // Q8
            public short stdShortTerm; // Q10

            public void WebRtcAgc_InitVad()
            {
                short k;

                HPstate = 0; // state of high pass filter
                logRatio = 0; // log( P(active) / P(inactive) )
                              // average input level (Q10)
                meanLongTerm = WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(15, 10);

                // variance of input level (Q8)
                varianceLongTerm = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(500, 8);

                stdLongTerm = 0; // standard deviation of input level in dB
                                 // short-term average input level (Q10)
                meanShortTerm = WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(15, 10);

                // short-term variance of input level (Q8)
                varianceShortTerm = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(500, 8);

                stdShortTerm = 0; // short-term standard deviation of input level in dB
                counter = 3; // counts updates
                for (k = 0; k < 8; k++)
                {
                    // downsampling filter
                    downState[k] = 0;
                }
            }

            public short WebRtcAgc_ProcessVad(
                            short[] inArray, // (i) Speech signal
                            int inArrayPtr,
                            short nrSamples) // (i) number of samples
            {
                int out_, nrg, tmp32, tmp32b;
                ushort tmpU16;
                short k, subfr, tmp16;
                var buf1 = new short[8];
                var buf2 = new short[4];
                short HPstate;
                short zeros, dB;
                int buf1_ptr;


                // process inArray 10 sub frames of 1 ms (to save on memory)

                //WriteDebugMessage(String.Format("(C#) AGC VAD 0004     inArray[143] = {0}", inArray[inArrayPtr + 143]));
                nrg = 0;
                buf1_ptr = 0;
                HPstate = this.HPstate;
                for (subfr = 0; subfr < 10; subfr++)
                {
                    // downsample to 4 kHz
                    if (nrSamples == 160)
                    {
                        for (k = 0; k < 8; k++)
                        {
                            tmp32 = inArray[inArrayPtr + 2 * k] + inArray[inArrayPtr + 2 * k + 1];
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 1);
                            buf1[k] = (short)tmp32;
                        }
                        inArrayPtr += 16;

                        WebRtcSpl_DownsampleBy2(buf1, 0, 8, buf2, downState);
                    }
                    else
                    {
                        WebRtcSpl_DownsampleBy2(inArray, inArrayPtr, 8, buf2, downState);
                        inArrayPtr += 8;
                    }

                    // high pass filter and compute energy
                    for (k = 0; k < 4; k++)
                    {
                        out_ = buf2[k] + HPstate;
                        tmp32 = WEBRTC_SPL_MUL(600, out_);
                        HPstate = (short)(WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 10) - buf2[k]);
                        tmp32 = WEBRTC_SPL_MUL(out_, out_);
                        nrg += WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 6);
                    }
                }
                this.HPstate = HPstate;

                //WriteDebugMessage(String.Format("(C#) AGC VAD 0003     nrg = {0}    this.HPstate = {1}   this.downState[5] = {2}", nrg, this.HPstate,
                //	this.downState[5])); // todo: difference here for 2nd run

                // find number of leading zeros
                if ((0xFFFF0000 & nrg) == 0)
                {
                    zeros = 16;
                }
                else
                {
                    zeros = 0;
                }
                if ((0xFF000000 & (nrg << zeros)) == 0)
                {
                    zeros += 8;
                }
                if ((0xF0000000 & (nrg << zeros)) == 0)
                {
                    zeros += 4;
                }
                if ((0xC0000000 & (nrg << zeros)) == 0)
                {
                    zeros += 2;
                }
                if ((0x80000000 & (nrg << zeros)) == 0)
                {
                    zeros += 1;
                }

                // energy level (range {-32..30}) (Q10)
                dB = WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(15 - zeros, 11);
                //WriteDebugMessage(String.Format("(C#) AGC VAD 0002     dB = {0}, zeros = {1}", dB, zeros)); // todo: difference here for 2nd run

                // Update statistics
                if (counter < kAvgDecayTime)
                {
                    // decay time = AvgDecTime * 10 ms
                    counter++;
                }

                // update short-term estimate of mean energy level (Q10)
                //WriteDebugMessage(String.Format("(C#) AGC VAD 0001     meanShortTerm = {0}", meanShortTerm)); // todo: difference here for 2nd run
                tmp32 = (WebRtcUtil.WEBRTC_SPL_MUL_16_16(meanShortTerm, 15) + dB);
                meanShortTerm = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 4);
                //WriteDebugMessage(String.Format("(C#) AGC VAD 01     meanShortTerm = {0}, tmp32 = {1}, dB = {2}", meanShortTerm, tmp32, dB)); // todo: difference here for 2nd run

                // update short-term estimate of variance inArray energy level (Q8)
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(WebRtcUtil.WEBRTC_SPL_MUL_16_16(dB, dB), 12);
                tmp32 += WEBRTC_SPL_MUL(varianceShortTerm, 15);
                varianceShortTerm = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 4);

                // update short-term estimate of standard deviation inArray energy level (Q10)
                tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(meanShortTerm, meanShortTerm);
                //WriteDebugMessage(String.Format("(C#) AGC VAD 001     tmp32 = {0}, meanShortTerm = {1}", tmp32, meanShortTerm));
                tmp32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(varianceShortTerm, 12) - tmp32;
                //WriteDebugMessage(String.Format("(C#) AGC VAD 002     tmp32 = {0}", tmp32));
                stdShortTerm = (short)WebRtcSpl_Sqrt(tmp32);

                // update long-term estimate of mean energy level (Q10)
                tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(meanLongTerm, counter) + dB;
                meanLongTerm = WebRtcSpl_DivW32W16ResW16(tmp32, WEBRTC_SPL_ADD_SAT_W16(counter, 1));

                // update long-term estimate of variance inArray energy level (Q8)
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(WebRtcUtil.WEBRTC_SPL_MUL_16_16(dB, dB), 12);
                tmp32 += WEBRTC_SPL_MUL(varianceLongTerm, counter);
                varianceLongTerm = WebRtcUtil.WebRtcSpl_DivW32W16(tmp32, WEBRTC_SPL_ADD_SAT_W16(counter, 1));

                // update long-term estimate of standard deviation inArray energy level (Q10)
                tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(meanLongTerm, meanLongTerm);
                tmp32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(varianceLongTerm, 12) - tmp32;
                stdLongTerm = (short)WebRtcSpl_Sqrt(tmp32);

                //WriteDebugMessage(String.Format("(C#) AGC VAD      meanShortTerm = {0}, varianceShortTerm = {1}, stdShortTerm = {2}, meanLongTerm = {3}, varianceLongTerm = {4}, stdLongTerm = {5}", 
                //	meanShortTerm, varianceShortTerm, stdShortTerm, meanLongTerm, varianceLongTerm, stdLongTerm));

                // update voice activity measure (Q10)
                tmp16 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(3, 12);
                tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(tmp16, (dB - meanLongTerm));
                tmp32 = WebRtcUtil.WebRtcSpl_DivW32W16(tmp32, stdLongTerm);
                tmpU16 = WEBRTC_SPL_LSHIFT_U16(13, 12);
                tmp32b = WEBRTC_SPL_MUL_16_U16(logRatio, tmpU16);
                tmp32 += WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32b, 10);

                logRatio = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 6);

                // limit
                if (logRatio > 2048)
                {
                    logRatio = 2048;
                }
                if (logRatio < -2048)
                {
                    logRatio = -2048;
                }

                //WriteDebugMessage(String.Format("(C#) AGC VAD      logRatio = {0}", logRatio));
                // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC VAD logRatio", (double)logRatio, 2);

                return logRatio; // Q10
            }

        }

        class DigitalAgc
        {
            int capacitorSlow;
            int capacitorFast;
            int gain;
            public readonly int[] gainTable = new int[32];
            short gatePrevious;
            AgcMode agcMode;
            readonly AgcVad vadNearend = new AgcVad();
            readonly AgcVad vadFarend = new AgcVad();
#if AGC_DEBUG
				FILE*         logFile;
				int           frameCounter;
#endif

            public int WebRtcAgc_InitDigital(AgcMode agcMode)
            {

                if (agcMode == AgcMode.AgcModeFixedDigital)
                {
                    // start at minimum to find correct gain faster
                    capacitorSlow = 0;
                }
                else
                {
                    // start outArray with 0 dB gain
                    capacitorSlow = 134217728; // (int)(0.125f * 32768.0f * 32768.0f);
                }
                capacitorFast = 0;
                gain = 65536;
                gatePrevious = 0;
                this.agcMode = agcMode;
#if AGC_DEBUG
				this.frameCounter = 0;
#endif

                // initialize VADs
                vadNearend.WebRtcAgc_InitVad();
                vadFarend.WebRtcAgc_InitVad();

                return 0;
            }

            public int WebRtcAgc_ProcessDigital(short[] in_near_arr, int in_near_ptr,
                                                   short[] in_near_H_arr, int in_near_H_ptr, short[] out_arr, int out_ptr,
                                                   short[] out_H_arr, int out_H_ptr, uint FS,
                                                   short lowlevelSignal)
            {
                // array for gains (one value per ms, incl start & end)
                var gains = new int[11];

                int out_tmp, tmp32;
                var env = new int[10];
                int nrg, max_nrg;
                int cur_level;
                int gain32, delta;
                short logratio;
                short lower_thr, upper_thr;
                short zeros = 0, zeros_fast, frac = 0;
                short decay;
                short gate, gain_adj;
                short k, n;
                short L, L2; // samples/subframe

                // determine number of samples per ms
                if (FS == 8000)
                {
                    L = 8;
                    L2 = 3;
                }
                else if (FS == 16000)
                {
                    L = 16;
                    L2 = 4;
                }
                else if (FS == 32000)
                {
                    L = 16;
                    L2 = 4;
                }
                else
                {
                    return -1;
                }

                Buffer.BlockCopy(in_near_arr, in_near_ptr * sizeof(short), out_arr, out_ptr * sizeof(short), 10 * L * sizeof(short));
                // Array.Copy(in_near_arr, in_near_ptr, out_arr, out_ptr, 10 * L);
                //WriteDebugMessage(String.Format("(C#) AGC 040     in_near[102] = {0} ", in_near_arr[in_near_ptr + 102]));
                // memcpy(out, in_near, 10 * L * sizeof(short));
                if (FS == 32000)
                {
                    Buffer.BlockCopy(in_near_H_arr, in_near_H_ptr * sizeof(short), out_H_arr, out_H_ptr * sizeof(short), 10 * L * sizeof(short));
                    // Array.Copy(in_near_H_arr, in_near_H_ptr, out_H_arr, out_H_ptr, 10 * L);
                    // memcpy(out_H, in_near_H, 10 * L * sizeof(short));
                }
                // VAD for near end
                logratio = vadNearend.WebRtcAgc_ProcessVad(out_arr, out_ptr, (short)(L * 10));

                //WriteDebugMessage(String.Format("(C#) AGC 04     logratio = {0}", logratio)); // no difference here

                // Account for far end VAD
                if (vadFarend.counter > 10)
                {
                    tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(3, logratio);
                    logratio = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32 - vadFarend.logRatio, 2);
                }

                // Determine decay factor depending on VAD
                //  upper_thr = 1.0f;
                //  lower_thr = 0.25f;
                upper_thr = 1024; // Q10
                lower_thr = 0; // Q10
                if (logratio > upper_thr)
                {
                    // decay = -2^17 / DecayTime;  ->  -65
                    decay = -65;
                }
                else if (logratio < lower_thr)
                {
                    decay = 0;
                }
                else
                {
                    // decay = (short)(((lower_thr - logratio)
                    //       * (2^27/(DecayTime*(upper_thr-lower_thr)))) >> 10);
                    // SUBSTITUTED: 2^27/(DecayTime*(upper_thr-lower_thr))  ->  65
                    tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16((lower_thr - logratio), 65);
                    decay = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 10);
                }

                // adjust decay factor for long silence (detected as low standard deviation)
                // This is only done in the adaptive modes
                if (agcMode != AgcMode.AgcModeFixedDigital)
                {
                    if (vadNearend.stdLongTerm < 4000)
                    {
                        decay = 0;
                    }
                    else if (vadNearend.stdLongTerm < 8096)
                    {
                        // decay = (short)(((this.vadNearend.stdLongTerm - 4000) * decay) >> 12);
                        tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16((vadNearend.stdLongTerm - 4000), decay);
                        decay = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 12);
                    }

                    if (lowlevelSignal != 0)
                    {
                        decay = 0;
                    }
                }
#if AGC_DEBUG
	this.frameCounter++;
	fprintf(this.logFile, "%5.2f\t%d\t%d\t%d\t", (float)(this.frameCounter) / 100, logratio, decay, this.vadNearend.stdLongTerm);
#endif
                //WriteDebugMessage(String.Format("(C#) AGC 050     out_arr[102] = {0} ", out_arr[102]));

                // Find max amplitude per sub frame
                // iterate over sub frames
                for (k = 0; k < 10; k++)
                {
                    // iterate over samples
                    max_nrg = 0;
                    for (n = 0; n < L; n++)
                    {
                        nrg = WebRtcUtil.WEBRTC_SPL_MUL_16_16(out_arr[out_ptr + k * L + n], out_arr[out_ptr + k * L + n]);
                        if (nrg > max_nrg)
                        {
                            max_nrg = nrg;
                        }
                    }
                    env[k] = max_nrg;

                    //WriteDebugMessage(String.Format("(C#) AGC 05     env[{0}] = {1}, max_nrg = {2}  ", k, env[k], max_nrg));
                }


                // Calculate gain per sub frame
                gains[0] = gain;
                for (k = 0; k < 10; k++)
                {
                    //WriteDebugMessage(String.Format("(C#) AGC 058     k = {0} capacitorFast = {1}  env[k] = {2}", k, capacitorFast, env[k]));// todo: difference here
                    // Fast envelope follower
                    //  decay time = -131000 / -1000 = 131 (ms)
                    capacitorFast = AGC_SCALEDIFF32(-1000, capacitorFast, capacitorFast);
                    if (env[k] > capacitorFast)
                    {
                        capacitorFast = env[k];
                    }

                    //WriteDebugMessage(String.Format("(C#) AGC 059     k = {0} capacitorFast = {1}", k, capacitorFast));// todo: difference here


                    // Slow envelope follower
                    if (env[k] > capacitorSlow)
                    {
                        // increase capacitorSlow
                        capacitorSlow = AGC_SCALEDIFF32(500, (env[k] - capacitorSlow), capacitorSlow);
                    }
                    else
                    {
                        // decrease capacitorSlow
                        capacitorSlow = AGC_SCALEDIFF32(decay, capacitorSlow, capacitorSlow);
                    }

                    // use maximum of both capacitors as current level
                    if (capacitorFast > capacitorSlow)
                    {
                        cur_level = capacitorFast;
                    }
                    else
                    {
                        cur_level = capacitorSlow;
                    }
                    // Translate signal level into gain, using a piecewise linear approximation
                    // find number of leading zeros
                    zeros = (short)WebRtcSpl_NormU32((uint)cur_level);

                    if (cur_level == 0)
                    {
                        zeros = 31;
                    }
                    tmp32 = (WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(cur_level, zeros) & 0x7FFFFFFF);
                    frac = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 19); // Q12
                    tmp32 = WEBRTC_SPL_MUL((gainTable[zeros - 1] - gainTable[zeros]), frac);
                    gains[k + 1] = gainTable[zeros] + WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 12);
#if AGC_DEBUG
		if (k == 0)
		{
			fprintf(this.logFile, "%d\t%d\t%d\t%d\t%d\n", env[0], cur_level, this.capacitorFast, this.capacitorSlow, zeros);
		}
#endif
                }

                // Gate processing (lower gain during absence of speech)
                zeros = (short)(WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(zeros, 9) - WebRtcUtil.WEBRTC_SPL_RSHIFT_W16(frac, 3));
                // find number of leading zeros
                zeros_fast = (short)WebRtcSpl_NormU32((uint)capacitorFast);
                if (capacitorFast == 0)
                {
                    zeros_fast = 31;
                }

                //WriteDebugMessage(String.Format("(C#) AGC 060     zeros_fast = {0}", zeros_fast));// todo: difference here
                //WriteDebugMessage(String.Format("(C#) AGC 060     capacitorFast = {0}", capacitorFast));// this is correct
                //WriteDebugMessage(String.Format("(C#) AGC 060     zeros = {0}", zeros));// todo: difference here

                tmp32 = (WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(capacitorFast, zeros_fast) & 0x7FFFFFFF);
                zeros_fast = WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(zeros_fast, 9);
                zeros_fast -= (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 22);

                gate = (short)(1000 + zeros_fast - zeros - vadNearend.stdShortTerm);
                //WriteDebugMessage(String.Format(
                //    "(C#) AGC 07     gate = {0},  zeros_fast = {1}, zeros = {2}, vadNearend.stdShortTerm = {3}", gate, zeros_fast, zeros, this.vadNearend.stdShortTerm));// todo difference here

                if (gate < 0)
                {
                    gatePrevious = 0;
                }
                else
                {
                    tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(gatePrevious, 7);
                    gate = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gate + tmp32, 3);
                    gatePrevious = gate;
                }

                //WriteDebugMessage(String.Format("(C#) AGC 06     gate = {0}", gate));// todo difference here
                // gate < 0     -> no gate
                // gate > 2500  -> max gate
                if (gate > 0)
                {
                    if (gate < 2500)
                    {
                        gain_adj = WebRtcUtil.WEBRTC_SPL_RSHIFT_W16(2500 - gate, 5);
                    }
                    else
                    {
                        gain_adj = 0;
                    }
                    for (k = 0; k < 10; k++)
                    {
                        if ((gains[k + 1] - gainTable[0]) > 8388608)
                        {
                            // To prevent wraparound
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32((gains[k + 1] - gainTable[0]), 8);
                            tmp32 = WEBRTC_SPL_MUL(tmp32, (178 + gain_adj));
                        }
                        else
                        {
                            tmp32 = WEBRTC_SPL_MUL((gains[k + 1] - gainTable[0]), (178 + gain_adj));
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 8);
                        }
                        gains[k + 1] = gainTable[0] + tmp32;
                    }
                }

                // Limit gain to avoid overload distortion
                for (k = 0; k < 10; k++)
                {
                    // To prevent wrap around
                    zeros = 10;
                    if (gains[k + 1] > 47453132)
                    {
                        zeros = (short)(16 - WebRtcSpl_NormW32(gains[k + 1]));
                    }
                    gain32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gains[k + 1], zeros) + 1;
                    gain32 = WEBRTC_SPL_MUL(gain32, gain32);
                    // check for overflow
                    while (AGC_MUL32(WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(env[k], 12) + 1, gain32)
                            > WebRtcUtil.WEBRTC_SPL_SHIFT_W32(32767, 2 * (1 - zeros + 10)))
                    {
                        // multiply by 253/256 ==> -0.1 dB
                        if (gains[k + 1] > 8388607)
                        {
                            // Prevent wrap around
                            gains[k + 1] = WEBRTC_SPL_MUL(WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gains[k + 1], 8), 253);
                        }
                        else
                        {
                            gains[k + 1] = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(WEBRTC_SPL_MUL(gains[k + 1], 253), 8);
                        }
                        gain32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gains[k + 1], zeros) + 1;
                        gain32 = WEBRTC_SPL_MUL(gain32, gain32);
                    }
                }
                // gain reductions should be done 1 ms earlier than gain increases
                for (k = 1; k < 10; k++)
                {
                    if (gains[k] > gains[k + 1])
                    {
                        gains[k] = gains[k + 1];
                    }
                }
                // save start gain for next frame
                gain = gains[10];

                // Apply gain
                // handle first sub frame separately
                delta = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(gains[1] - gains[0], (4 - L2));
                gain32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(gains[0], 4);
                // iterate over samples
                for (n = 0; n < L; n++)
                {
                    // For lower band
                    tmp32 = WEBRTC_SPL_MUL(out_arr[out_ptr + n], WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32 + 127, 7));
                    out_tmp = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                    if (out_tmp > 4095)
                    {
                        out_arr[out_ptr + n] = 32767;
                    }
                    else if (out_tmp < -4096)
                    {
                        out_arr[out_ptr + n] = -32768;
                    }
                    else
                    {
                        tmp32 = WEBRTC_SPL_MUL(out_arr[out_ptr + n], WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32, 4));
                        out_arr[out_ptr + n] = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                    }
                    // For higher band
                    if (FS == 32000)
                    {
                        tmp32 = WEBRTC_SPL_MUL(out_H_arr[out_H_ptr + n],
                                               WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32 + 127, 7));
                        out_tmp = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                        if (out_tmp > 4095)
                        {
                            out_H_arr[out_H_ptr + n] = 32767;
                        }
                        else if (out_tmp < -4096)
                        {
                            out_H_arr[out_H_ptr + n] = -32768;
                        }
                        else
                        {
                            tmp32 = WEBRTC_SPL_MUL(out_H_arr[out_H_ptr + n], WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32, 4));
                            out_H_arr[out_H_ptr + n] = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                        }
                    }
                    //

                    gain32 += delta;
                }
                // iterate over subframes
                for (k = 1; k < 10; k++)
                {
                    delta = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(gains[k + 1] - gains[k], (4 - L2));
                    gain32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(gains[k], 4);
                    // iterate over samples
                    for (n = 0; n < L; n++)
                    {
                        // For lower band
                        tmp32 = WEBRTC_SPL_MUL(out_arr[out_ptr + k * L + n], WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32, 4));
                        out_arr[out_ptr + k * L + n] = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                        // For higher band
                        if (FS == 32000)
                        {
                            tmp32 = WEBRTC_SPL_MUL(out_H_arr[out_H_ptr + k * L + n], WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(gain32, 4));
                            out_H_arr[out_H_ptr + k * L + n] = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 16);
                        }
                        gain32 += delta;
                    }
                }

                return 0;
            }

            public int WebRtcAgc_AddFarendToDigital(short[] in_far_arr, int in_far_ptr, short nrSamples)
            {
                // Check for valid pointer
                if (vadFarend == null)
                {
                    return -1;
                }

                // VAD for far end
                vadFarend.WebRtcAgc_ProcessVad(in_far_arr, in_far_ptr, nrSamples);

                return 0;
            }

        }

        #region variables
        // Configurable parameters/variables
        readonly uint fs;                 // Sampling frequency
        short compressionGaindB;  // Fixed gain level in dB
        short targetLevelDbfs;    // Target level in -dBfs of envelope (default -3)
        readonly AgcMode agcMode;            // Hard coded mode (adaptAna/adaptDig/fixedDig)
        AgcBoolean limiterEnable;      // Enabling limiter (on/off (default off))
        readonly WebRtcAgcConfig defaultConfig = new WebRtcAgcConfig();
        readonly WebRtcAgcConfig usedConfig = new WebRtcAgcConfig();

        // General variables
        readonly short initFlag;

        // Target level parameters
        // Based on the above: analogTargetLevel = round((32767*10^(-22/20))^2*16/2^7)
        int analogTargetLevel;  // = RXX_BUFFER_LEN * 846805;       -22 dBfs
        int startUpperLimit;    // = RXX_BUFFER_LEN * 1066064;      -21 dBfs
        int startLowerLimit;    // = RXX_BUFFER_LEN * 672641;       -23 dBfs
        int upperPrimaryLimit;  // = RXX_BUFFER_LEN * 1342095;      -20 dBfs
        int lowerPrimaryLimit;  // = RXX_BUFFER_LEN * 534298;       -24 dBfs
        int upperSecondaryLimit;// = RXX_BUFFER_LEN * 2677832;      -17 dBfs
        int lowerSecondaryLimit;// = RXX_BUFFER_LEN * 267783;       -27 dBfs
        ushort targetIdx;          // Table index for corresponding target level
#if MIC_LEVEL_FEEDBACK
			ushort      targetIdxOffset;    // Table index offset for level compensation
#endif
        short analogTarget;       // Digital reference level in ENV scale

        // Analog AGC specific variables
        readonly int[] filterState = new int[8];     // For downsampling wb to nb
        int upperLimit;         // Upper limit for mic energy
        int lowerLimit;         // Lower limit for mic energy
        int Rxx160w32;          // Average energy for one frame
        int Rxx16_LPw32;        // Low pass filtered subframe energies
        int Rxx160_LPw32;       // Low pass filtered frame energies
        int Rxx16_LPw32Max;     // Keeps track of largest energy subframe
        readonly int[] Rxx16_vectorw32 = new int[RXX_BUFFER_LEN];// Array with subframe energies
        private readonly int[][] Rxx16w32_array; // Energy values of microphone signal
        private readonly int[][] env;          // Envelope values of subframes

        short Rxx16pos;           // Current position in the Rxx16_vectorw32
        short envSum;             // Filtered scaled envelope in subframes
        short vadThreshold;       // Threshold for VAD decision
        short inActive;           // Inactive time in milliseconds
        short msTooLow;           // Milliseconds of speech at a too low level
        short msTooHigh;          // Milliseconds of speech at a too high level
        short changeToSlowMode;   // Change to slow mode after some time at target
        short firstCall;          // First call to the process-function
        short msZero;             // Milliseconds of zero input
        short msecSpeechOuterChange;// Min ms of speech between volume changes
        short msecSpeechInnerChange;// Min ms of speech between volume changes
        short activeSpeech;       // Milliseconds of active speech
        short muteGuardMs;        // Counter to prevent mute action
        short inQueue;            // 10 ms batch indicator

        // Microphone level variables
        int micRef;             // Remember ref. mic level for virtual mic
        ushort gainTableIdx;       // Current position in virtual gain table
        int micGainIdx;         // Gain index of mic level to increase slowly
        int micVol;             // Remember volume between frames
        int maxLevel;           // Max possible vol level, incl dig gain
        readonly int maxAnalog;          // Maximum possible analog volume level
        readonly int maxInit;            // Initial value of "max"
        readonly int minLevel;           // Minimum possible volume level
        readonly int minOutput;          // Minimum output volume level
        int zeroCtrlMax;        // Remember max gain => don't amp low input

        readonly short scale;              // Scale factor for internal volume levels
#if MIC_LEVEL_FEEDBACK
			short       numBlocksMicLvlSat;
			byte micLvlSat;
#endif
        // Structs for VAD and digital_agc
        readonly AgcVad vadMic = new AgcVad();
        readonly DigitalAgc digitalAgc = new DigitalAgc();

#if AGC_DEBUG
			FILE*               fpt;
			FILE*               agcLog;
			int       fcount;
#endif

        short lowLevelSignal;
        #endregion
        #region constants

        /* The slope of in Q13*/
        readonly short[] kSlope1 = new short[] { 21793, 12517, 7189, 4129, 2372, 1362, 472, 78 };

        /* The offset in Q14 */
        readonly short[] kOffset1 = new short[] { 25395, 23911, 22206, 20737, 19612, 18805, 17951, 17367 };

        /* The slope of in Q13*/
        readonly short[] kSlope2 = new short[] { 2063, 1731, 1452, 1218, 1021, 857, 597, 337 };

        /* The offset in Q14 */
        readonly short[] kOffset2 = new short[] { 18432, 18379, 18290, 18177, 18052, 17920, 17670, 17286 };

        const short kMuteGuardTimeMs = 8000;
        const short kInitCheck = 42;

        /* Default settings if config is not used */
        const int AGC_DEFAULT_TARGET_LEVEL = 3;
        const int AGC_DEFAULT_COMP_GAIN = 9;
        /* This is the target level for the analog part in ENV scale. To convert to RMS scale you
		 * have to add OFFSET_ENV_TO_RMS.
		 */
        const int ANALOG_TARGET_LEVEL = 11;
        const int ANALOG_TARGET_LEVEL_2 = 5; // ANALOG_TARGET_LEVEL / 2
        /* Offset between RMS scale (analog part) and ENV scale (digital part). This value actually
		 * varies with the FIXED_ANALOG_TARGET_LEVEL, hence we should in the future replace it with
		 * a table.
		 */
        const int OFFSET_ENV_TO_RMS = 9;
        /* The reference input level at which the digital part gives an output of targetLevelDbfs
		 * (desired level) if we have no compression gain. This level should be set high enough not
		 * to compress the peaks due to the dynamics.
		 */
        const int DIGITAL_REF_AT_0_COMP_GAIN = 4;
        /* Speed of reference level decrease.
		 */
        const int DIFF_REF_TO_ANALOG = 5;

        const int RXX_BUFFER_LEN = 10;

        const short kMsecSpeechInner = 520;
        const short kMsecSpeechOuter = 340;

        const short kNormalVadThreshold = 400;

        const short kAlphaShortTerm = 6; // 1 >> 6 = 0.0156
        const short kAlphaLongTerm = 10; // 1 >> 10 = 0.000977


        // To generate the gaintable, copy&paste the following lines to a Matlab window:
        // MaxGain = 6; MinGain = 0; CompRatio = 3; Knee = 1;
        // zeros = 0:31; lvl = 2.^(1-zeros);
        // A = -10*log10(lvl) * (CompRatio - 1) / CompRatio;
        // B = MaxGain - MinGain;
        // gains = round(2^16*10.^(0.05 * (MinGain + B * ( log(exp(-Knee*A)+exp(-Knee*B)) - log(1+exp(-Knee*B)) ) / log(1/(1+exp(Knee*B))))));
        // fprintf(1, '\t%i, %i, %i, %i,\n', gains);
        // % Matlab code for plotting the gain and input/output level characteristic (copy/paste the following 3 lines):
        // in = 10*log10(lvl); out = 20*log10(gains/65536);
        // subplot(121); plot(in, out); axis([-30, 0, -5, 20]); grid on; xlabel('Input (dB)'); ylabel('Gain (dB)');
        // subplot(122); plot(in, in+out); axis([-30, 0, -30, 5]); grid on; xlabel('Input (dB)'); ylabel('Output (dB)');
        // zoom on;

        // Generator table for y=log2(1+e^x) in Q8.
        static readonly ushort[] kGenFuncTable = new ushort[]{
                      256,   485,   786,  1126,  1484,  1849,  2217,  2586,
                     2955,  3324,  3693,  4063,  4432,  4801,  5171,  5540,
                     5909,  6279,  6648,  7017,  7387,  7756,  8125,  8495,
                     8864,  9233,  9603,  9972, 10341, 10711, 11080, 11449,
                    11819, 12188, 12557, 12927, 13296, 13665, 14035, 14404,
                    14773, 15143, 15512, 15881, 16251, 16620, 16989, 17359,
                    17728, 18097, 18466, 18836, 19205, 19574, 19944, 20313,
                    20682, 21052, 21421, 21790, 22160, 22529, 22898, 23268,
                    23637, 24006, 24376, 24745, 25114, 25484, 25853, 26222,
                    26592, 26961, 27330, 27700, 28069, 28438, 28808, 29177,
                    29546, 29916, 30285, 30654, 31024, 31393, 31762, 32132,
                    32501, 32870, 33240, 33609, 33978, 34348, 34717, 35086,
                    35456, 35825, 36194, 36564, 36933, 37302, 37672, 38041,
                    38410, 38780, 39149, 39518, 39888, 40257, 40626, 40996,
                    41365, 41734, 42104, 42473, 42842, 43212, 43581, 43950,
                    44320, 44689, 45058, 45428, 45797, 46166, 46536, 46905
            };

        const short kAvgDecayTime = 250; // frames; < 3000

        /// <summary>
        /// Size of analog gain table
        /// </summary>
        const int GAIN_TBL_LEN = 32;

        /// <summary>
        /// Matlab code:
        /// fprintf(1, '\t%i, %i, %i, %i,\n', round(10.^(linspace(0,10,32)/20) * 2^12));
        /// Q12
        /// </summary>
        readonly ushort[] kGainTableAnalog = new ushort[]{4096, 4251, 4412, 4579, 4752,
                4932, 5118, 5312, 5513, 5722, 5938, 6163, 6396, 6638, 6889, 7150, 7420, 7701, 7992,
                8295, 8609, 8934, 9273, 9623, 9987, 10365, 10758, 11165, 11587, 12025, 12480, 12953};

        /// <summary>
        /// Gain table for virtual Mic (in Q10)
        /// </summary>
        readonly ushort[] kGainTableVirtualMic = new ushort[] {1052, 1081, 1110, 1141, 1172, 1204,
                    1237, 1271, 1305, 1341, 1378, 1416, 1454, 1494, 1535, 1577, 1620, 1664, 1710, 1757,
                    1805, 1854, 1905, 1957, 2010, 2065, 2122, 2180, 2239, 2301, 2364, 2428, 2495, 2563,
                    2633, 2705, 2779, 2855, 2933, 3013, 3096, 3180, 3267, 3357, 3449, 3543, 3640, 3739,
                    3842, 3947, 4055, 4166, 4280, 4397, 4517, 4640, 4767, 4898, 5032, 5169, 5311, 5456,
                    5605, 5758, 5916, 6078, 6244, 6415, 6590, 6770, 6956, 7146, 7341, 7542, 7748, 7960,
                    8178, 8402, 8631, 8867, 9110, 9359, 9615, 9878, 10148, 10426, 10711, 11004, 11305,
                    11614, 11932, 12258, 12593, 12938, 13292, 13655, 14029, 14412, 14807, 15212, 15628,
                    16055, 16494, 16945, 17409, 17885, 18374, 18877, 19393, 19923, 20468, 21028, 21603,
                    22194, 22801, 23425, 24065, 24724, 25400, 26095, 26808, 27541, 28295, 29069, 29864,
                    30681, 31520, 32382};

        /// <summary>
        /// Suppression table for virtual Mic (in Q10)
        /// </summary>
        readonly short[] kSuppressionTableVirtualMic = new short[] {1024, 1006, 988, 970, 952,
                    935, 918, 902, 886, 870, 854, 839, 824, 809, 794, 780, 766, 752, 739, 726, 713, 700,
                    687, 675, 663, 651, 639, 628, 616, 605, 594, 584, 573, 563, 553, 543, 533, 524, 514,
                    505, 496, 487, 478, 470, 461, 453, 445, 437, 429, 421, 414, 406, 399, 392, 385, 378,
                    371, 364, 358, 351, 345, 339, 333, 327, 321, 315, 309, 304, 298, 293, 288, 283, 278,
                    273, 268, 263, 258, 254, 249, 244, 240, 236, 232, 227, 223, 219, 215, 211, 208, 204,
                    200, 197, 193, 190, 186, 183, 180, 176, 173, 170, 167, 164, 161, 158, 155, 153, 150,
                    147, 145, 142, 139, 137, 134, 132, 130, 127, 125, 123, 121, 118, 116, 114, 112, 110,
                    108, 106, 104, 102};


        /// <summary>
        /// Table for target energy levels. Values in Q(-7)
        /// Matlab code
        /// targetLevelTable = fprintf('%d,\t%d,\t%d,\t%d,\n', round((32767*10.^(-(0:63)'/20)).^2*16/2^7)
        /// </summary>
        readonly int[] kTargetLevelTable = new[] {134209536, 106606424, 84680493, 67264106,
                    53429779, 42440782, 33711911, 26778323, 21270778, 16895980, 13420954, 10660642,
                    8468049, 6726411, 5342978, 4244078, 3371191, 2677832, 2127078, 1689598, 1342095,
                    1066064, 846805, 672641, 534298, 424408, 337119, 267783, 212708, 168960, 134210,
                    106606, 84680, 67264, 53430, 42441, 33712, 26778, 21271, 16896, 13421, 10661, 8468,
                    6726, 5343, 4244, 3371, 2678, 2127, 1690, 1342, 1066, 847, 673, 534, 424, 337, 268,
                    213, 169, 134, 107, 85, 67};

        #endregion
        #region common procedures
        static int WebRtcSpl_NormU32(uint value)
        {
            int zeros = 0;

            if (value == 0)
                return 0;

            if ((0xFFFF0000 & value) == 0)
                zeros = 16;
            if ((0xFF000000 & (value << zeros)) == 0)
                zeros += 8;
            if ((0xF0000000 & (value << zeros)) == 0)
                zeros += 4;
            if ((0xC0000000 & (value << zeros)) == 0)
                zeros += 2;
            if ((0x80000000 & (value << zeros)) == 0)
                zeros += 1;

            return zeros;
        }
        static void WebRtcSpl_MemSetW32(int[] ptr, int set_value, int length)
        {
            int j;
            for (j = 0; j < length; j++)
            {
                ptr[j] = set_value;
            }
        }
        static short WebRtcSpl_DivW32W16ResW16(int num, short den)
        {
            // Guard against division with 0
            if (den != 0)
            {
                return (short)(num / den);
            }
            else
            {
                return 0x7FFF;
            }
        }
        static short WEBRTC_SPL_RSHIFT_U16(int x, int c)
        {
            return (short)((ushort)(x) >> (c));
        }
        static int WEBRTC_SPL_MUL_16_U16(int a, int b)
        {

            return ((short)(a) * (ushort)(b));
        }
        static int WEBRTC_SPL_ABS_W32(int a)
        {
            return ((a >= 0) ? (a) : -(a));
        }
        static uint WEBRTC_SPL_RSHIFT_U32(uint x, int c)
        {
            return ((x) >> (c));
        }
        static uint WEBRTC_SPL_UMUL_16_16(ushort a, ushort b)
        {
            return (uint)(a) * (b);
        }
        static uint WEBRTC_SPL_LSHIFT_U32(uint x, int c)
        {
            return ((x) << (c));
        }
        static uint WEBRTC_SPL_UMUL_32_16(uint a, ushort b)
        {
            return (((a) * (b)));
        }
        static int WEBRTC_SPL_MUL_32_16(int a, int b)
        {
            return ((a) * (b));
        }
        static int WebRtcSpl_NormW32(int value)
        {
            int zeros = 0;

            if (value <= 0)
                value ^= (-1);

            // Fast binary search to determine the number of left shifts required to 32-bit normalize
            // the value
            if ((0xFFFF8000 & value) == 0)
                zeros = 16;
            if ((0xFF800000 & (value << zeros)) == 0)
                zeros += 8;
            if ((0xF8000000 & (value << zeros)) == 0)
                zeros += 4;
            if ((0xE0000000 & (value << zeros)) == 0)
                zeros += 2;
            if ((0xC0000000 & (value << zeros)) == 0)
                zeros += 1;

            return zeros;
        }
        static int WEBRTC_SPL_DIV(int a, int b)
        {
            return (((a) / (b)));
        }
        static int WEBRTC_SPL_MUL(int a, int b)
        {
            return (((a) * (b)));
        }
        static ushort WEBRTC_SPL_LSHIFT_U16(int x, int c)
        {
            return (ushort)((ushort)(x) << (c));
        }

        // C + the 32 most significant bits of A * B
        static int WEBRTC_SPL_SCALEDIFF32(int A, int B, int C)
        {
            return (C + (B >> 16) * A + (int)(((uint)(0x0000FFFF & B) * A) >> 16));
        }

        // the 32 most significant bits of A(19) * B(26) >> 13
        static int AGC_MUL32(int A, int B)
        {
            return (((B) >> 13) * (A) + (((0x00001FFF & (B)) * (A)) >> 13));
        }
        static uint WEBRTC_SPL_UMUL(uint a, uint b)
        {
            return (((a) * (b)));
        }
        static int WebRtcSpl_DotProductWithScale(short[] vector1, short[] vector2, int length, int scaling)
        {
            int sum;
            int i;
            sum = 0;


            for (i = 0; i < length; i++)
            {
                sum += WebRtcUtil.WEBRTC_SPL_MUL_16_16_RSFT(vector1[i], vector2[i], scaling);
            }


            return sum;
        }
        static short WEBRTC_SPL_ADD_SAT_W16(short var1, short var2)
        {
            int s_sum = var1 + var2;

            if (s_sum > short.MaxValue)
                s_sum = short.MaxValue;
            else if (s_sum < short.MinValue)
                s_sum = short.MinValue;

            return (short)s_sum;
        }
        static int AGC_SCALEDIFF32(int A, int B, int C)
        {
            return ((C) + ((B) >> 16) * (A) + (((0x0000FFFF & (B)) * (A)) >> 16));
        }


        static int WebRtcSpl_SqrtLocal(int in_)
        {

            short x_half, t16;
            int A, B, x2;

            /* The following block performs:
			 y=in/2
			 x=y-2^30
			 x_half=x/2^31
			 t = 1 + (x_half) - 0.5*((x_half)^2) + 0.5*((x_half)^3) - 0.625*((x_half)^4)
				 + 0.875*((x_half)^5)
			 */

            B = in_;

            B = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(B, 1); // B = in/2
            B = B - (0x40000000); // B = in/2 - 1/2
            x_half = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(B, 16);// x_half = x/2 = (in-1)/2
            B = B + (0x40000000); // B = 1 + x/2
            B = B + (0x40000000); // Add 0.5 twice (since 1.0 does not exist in Q31)

            x2 = (x_half) * (x_half) * 2; // A = (x/2)^2
            A = -x2; // A = -(x/2)^2
            B = B + (A >> 1); // B = 1 + x/2 - 0.5*(x/2)^2

            A = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16);
            A = A * A * 2; // A = (x/2)^4
            t16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16);
            B = B + WebRtcUtil.WEBRTC_SPL_MUL_16_16(-20480, t16) * 2; // B = B - 0.625*A
                                                                      // After this, B = 1 + x/2 - 0.5*(x/2)^2 - 0.625*(x/2)^4

            t16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16);
            A = WebRtcUtil.WEBRTC_SPL_MUL_16_16(x_half, t16) * 2; // A = (x/2)^5
            t16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16);
            B = B + WebRtcUtil.WEBRTC_SPL_MUL_16_16(28672, t16) * 2; // B = B + 0.875*A
                                                                     // After this, B = 1 + x/2 - 0.5*(x/2)^2 - 0.625*(x/2)^4 + 0.875*(x/2)^5

            t16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(x2, 16);
            A = WebRtcUtil.WEBRTC_SPL_MUL_16_16(x_half, t16) * 2; // A = x/2^3

            B = B + (A >> 1); // B = B + 0.5*A
                              // After this, B = 1 + x/2 - 0.5*(x/2)^2 + 0.5*(x/2)^3 - 0.625*(x/2)^4 + 0.875*(x/2)^5

            B = B + (32768); // Round off bit

            return B;
        }

        static int WebRtcSpl_Sqrt(int value)
        {
            /*
			 Algorithm:

			 Six term Taylor Series is used here to compute the square root of a number
			 y^0.5 = (1+x)^0.5 where x = y-1
			 = 1+(x/2)-0.5*((x/2)^2+0.5*((x/2)^3-0.625*((x/2)^4+0.875*((x/2)^5)
			 0.5 <= x < 1

			 Example of how the algorithm works, with ut=sqrt(in), and
			 with in=73632 and ut=271 (even shift value case):

			 in=73632
			 y= in/131072
			 x=y-1
			 t = 1 + (x/2) - 0.5*((x/2)^2) + 0.5*((x/2)^3) - 0.625*((x/2)^4) + 0.875*((x/2)^5)
			 ut=t*(1/sqrt(2))*512

			 or:

			 in=73632
			 in2=73632*2^14
			 y= in2/2^31
			 x=y-1
			 t = 1 + (x/2) - 0.5*((x/2)^2) + 0.5*((x/2)^3) - 0.625*((x/2)^4) + 0.875*((x/2)^5)
			 ut=t*(1/sqrt(2))
			 ut2=ut*2^9

			 which gives:

			 in  = 73632
			 in2 = 1206386688
			 y   = 0.56176757812500
			 x   = -0.43823242187500
			 t   = 0.74973506527313
			 ut  = 0.53014274874797
			 ut2 = 2.714330873589594e+002

			 or:

			 in=73632
			 in2=73632*2^14
			 y=in2/2
			 x=y-2^30
			 x_half=x/2^31
			 t = 1 + (x_half) - 0.5*((x_half)^2) + 0.5*((x_half)^3) - 0.625*((x_half)^4)
				 + 0.875*((x_half)^5)
			 ut=t*(1/sqrt(2))
			 ut2=ut*2^9

			 which gives:

			 in  = 73632
			 in2 = 1206386688
			 y   = 603193344
			 x   = -470548480
			 x_half =  -0.21911621093750
			 t   = 0.74973506527313
			 ut  = 0.53014274874797
			 ut2 = 2.714330873589594e+002

			 */

            short x_norm, nshift, t16, sh;
            int A;

            short k_sqrt_2 = 23170; // 1/sqrt2 (==5a82)

            A = value;

            if (A == 0)
                return 0; // sqrt(0) = 0

            sh = (short)WebRtcSpl_NormW32(A); // # shifts to normalize A
            A = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(A, sh); // Normalize A
            if (A < (Int32.MaxValue - 32767))
            {
                A = A + (32768); // Round off bit
            }
            else
            {
                A = Int32.MaxValue;
            }

            x_norm = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16); // x_norm = AH

            nshift = WebRtcUtil.WEBRTC_SPL_RSHIFT_W16(sh, 1); // nshift = sh>>1
            nshift = (short)-nshift; // Negate the power for later de-normalization

            A = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(x_norm, 16);
            A = WEBRTC_SPL_ABS_W32(A); // A = abs(x_norm<<16)
            A = WebRtcSpl_SqrtLocal(A); // A = sqrt(A)

            if ((-2 * nshift) == sh)
            { // Even shift value case

                t16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16); // t16 = AH

                A = WebRtcUtil.WEBRTC_SPL_MUL_16_16(k_sqrt_2, t16) * 2; // A = 1/sqrt(2)*t16
                A = A + (32768); // Round off
                A = A & (0x7fff0000); // Round off

                A = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 15); // A = A>>16

            }
            else
            {
                A = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(A, 16); // A = A>>16
            }

            A = A & (0x0000ffff);
            A = WebRtcUtil.WEBRTC_SPL_SHIFT_W32(A, nshift); // De-normalize the result

            return A;
        }
        #endregion

        public Agc(int minLevel, int maxLevel, AgcMode agcMode, uint fs)
        {
            int max_add, tmp32;
            short i;
            int tmpNorm;

            if (digitalAgc.WebRtcAgc_InitDigital(agcMode) != 0)
            {
                throw new Exception("AGC_UNINITIALIZED_ERROR");
            }

            Rxx16w32_array = JaggedArrayHelper.Create2DJaggedArray<int>(2, 10); // new int[2, 10];
            env = JaggedArrayHelper.Create2DJaggedArray<int>(2, 20);

            /* Analog AGC variables */
            envSum = 0;

            /* mode     = 0 - Only saturation protection
			 *            1 - Analog Automatic Gain Control [-targetLevelDbfs (default -3 dBOv)]
			 *            2 - Digital Automatic Gain Control [-targetLevelDbfs (default -3 dBOv)]
			 *            3 - Fixed Digital Gain [compressionGaindB (default 8 dB)]
			 */
#if AGC_DEBUG//test log
	this.fcount = 0;
	fprintf(this.fpt, "AGC->Init\n");
#endif
            if (agcMode < AgcMode.AgcModeUnchanged || agcMode > AgcMode.AgcModeFixedDigital)
            {
                throw new Exception("AGC->Init: error, incorrect mode");
            }
            this.agcMode = agcMode;
            this.fs = fs;

            /* initialize input VAD */
            vadMic.WebRtcAgc_InitVad();

            /* If the volume range is smaller than 0-256 then
			 * the levels are shifted up to Q8-domain */
            tmpNorm = WebRtcSpl_NormU32((uint)maxLevel);
            scale = (short)(tmpNorm - 23);
            if (scale < 0)
            {
                scale = 0;
            }
            // TODO(bjornv): Investigate if we really need to scale up a small range now when we have
            // a guard against zero-increments. For now, we do not support scale up (scale = 0).
            scale = 0;
            maxLevel = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(maxLevel, scale);
            minLevel = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(minLevel, scale);

            /* Make minLevel and maxLevel static in AdaptiveDigital */
            if (this.agcMode == AgcMode.AgcModeAdaptiveDigital)
            {
                minLevel = 0;
                maxLevel = 255;
                scale = 0;
            }
            /* The maximum supplemental volume range is based on a vague idea
			 * of how much lower the gain will be than the real analog gain. */
            max_add = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(maxLevel - minLevel, 2);

            /* Minimum/maximum volume level that can be set */
            this.minLevel = minLevel;
            maxAnalog = maxLevel;
            this.maxLevel = maxLevel + max_add;
            maxInit = this.maxLevel;

            zeroCtrlMax = maxAnalog;

            /* Initialize micVol parameter */
            micVol = maxAnalog;
            if (this.agcMode == AgcMode.AgcModeAdaptiveDigital)
            {
                micVol = 127; /* Mid-point of mic level */
            }
            micRef = micVol;
            micGainIdx = 127;
#if MIC_LEVEL_FEEDBACK
	this.numBlocksMicLvlSat = 0;
	this.micLvlSat = 0;
#endif
#if AGC_DEBUG//test log
	fprintf(this.fpt,
			"AGC->Init: minLevel = %d, maxAnalog = %d, maxLevel = %d\n",
			this.minLevel, this.maxAnalog, this.maxLevel);
#endif

            /* Minimum output volume is 4% higher than the available lowest volume level */
            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32((this.maxLevel - this.minLevel) * 10, 8);
            minOutput = (this.minLevel + tmp32);

            msTooLow = 0;
            msTooHigh = 0;
            changeToSlowMode = 0;
            firstCall = 0;
            msZero = 0;
            muteGuardMs = 0;
            gainTableIdx = 0;

            msecSpeechInnerChange = kMsecSpeechInner;
            msecSpeechOuterChange = kMsecSpeechOuter;

            activeSpeech = 0;
            Rxx16_LPw32Max = 0;

            vadThreshold = kNormalVadThreshold;
            inActive = 0;

            for (i = 0; i < RXX_BUFFER_LEN; i++)
            {
                Rxx16_vectorw32[i] = 1000; /* -54dBm0 */
            }
            Rxx160w32 = 125 * RXX_BUFFER_LEN; /* (this.Rxx16_vectorw32[0]>>3) = 125 */

            Rxx16pos = 0;
            Rxx16_LPw32 = 16284; /* Q(-4) */

            // ks 11/3/11 - Not necessary
            //for (i = 0; i < 5; i++)
            //{
            //    Rxx16w32_array[0][i] = 0;
            //}
            //for (i = 0; i < 10; i++)
            //    for (int k = 0; k < 2; k++)
            //    {
            //        env[k][i] = 0;
            //    }

            inQueue = 0;

#if MIC_LEVEL_FEEDBACK
	this.targetIdxOffset = 0;
#endif

            WebRtcSpl_MemSetW32(filterState, 0, 8);

            initFlag = kInitCheck;
            // Default config settings.
            defaultConfig.limiterEnable = AgcBoolean.AgcTrue;
            defaultConfig.targetLevelDbfs = AGC_DEFAULT_TARGET_LEVEL;
            defaultConfig.compressionGaindB = AGC_DEFAULT_COMP_GAIN;

            if (WebRtcAgc_set_config(defaultConfig) == -1)
            {
                throw new Exception("AGC_UNSPECIFIED_ERROR");
            }
            Rxx160_LPw32 = analogTargetLevel; // Initialize rms value

            lowLevelSignal = 0;

            /* Only positive values are allowed that are not too large */
            if ((minLevel >= maxLevel) || ((maxLevel & 0xFC000000) != 0))
            {
#if AGC_DEBUG//test log
		fprintf(this.fpt, "\n\n");
#endif
                throw new ArgumentException("minLevel, maxLevel value(s) are invalid");
            }
            else
            {
#if AGC_DEBUG//test log
		fprintf(this.fpt, "\n");
#endif
            }
        }

        int WebRtcAgc_set_config(WebRtcAgcConfig agcConfig)
        {
            if (initFlag != kInitCheck)
            {
                throw new Exception("AGC_UNINITIALIZED_ERROR");
            }

            if (agcConfig.limiterEnable != AgcBoolean.AgcFalse && agcConfig.limiterEnable != AgcBoolean.AgcTrue)
            {
                throw new ArgumentException();
            }
            limiterEnable = agcConfig.limiterEnable;
            compressionGaindB = agcConfig.compressionGaindB;
            if ((agcConfig.targetLevelDbfs < 0) || (agcConfig.targetLevelDbfs > 31))
            {
                throw new ArgumentException();
                //this.lastError = AGC_BAD_PARAMETER_ERROR;
            }
            targetLevelDbfs = agcConfig.targetLevelDbfs;

            if (agcMode == AgcMode.AgcModeFixedDigital)
            {
                /* Adjust for different parameter interpretation in FixedDigital mode */
                compressionGaindB += agcConfig.targetLevelDbfs;
            }

            /* Update threshold levels for analog adaptation */
            WebRtcAgc_UpdateAgcThresholds();

            /* Recalculate gain table */
            if (WebRtcAgc_CalculateGainTable(digitalAgc.gainTable, compressionGaindB,
                                   targetLevelDbfs, limiterEnable, analogTarget) == -1)
            {
#if AGC_DEBUG//test log
		fprintf(this.fpt, "AGC->set_config, frame %d: Error from calcGainTable\n\n", this.fcount);
#endif
                return -1;
            }
            /* Store the config in a WebRtcAgcConfig */
            usedConfig.compressionGaindB = agcConfig.compressionGaindB;
            usedConfig.limiterEnable = agcConfig.limiterEnable;
            usedConfig.targetLevelDbfs = agcConfig.targetLevelDbfs;

            return 0;
        }

        void WebRtcAgc_UpdateAgcThresholds()
        {
#if MIC_LEVEL_FEEDBACK
	int zeros;

	if (this.micLvlSat)
	{
		/* Lower the analog target level since we have reached its maximum */
		zeros = WebRtcSpl_NormW32(this.Rxx160_LPw32);
		this.targetIdxOffset = WEBRTC_SPL_RSHIFT_W16((3 * zeros) - this.targetIdx - 2, 2);
	}
#endif

            /* Set analog target level in envelope dBOv scale */
            var tmp16 = (short)((DIFF_REF_TO_ANALOG * compressionGaindB) + ANALOG_TARGET_LEVEL_2);
            tmp16 = WebRtcSpl_DivW32W16ResW16(tmp16, ANALOG_TARGET_LEVEL);
            analogTarget = (short)(DIGITAL_REF_AT_0_COMP_GAIN + tmp16);
            if (analogTarget < DIGITAL_REF_AT_0_COMP_GAIN)
            {
                analogTarget = DIGITAL_REF_AT_0_COMP_GAIN;
            }
            if (agcMode == AgcMode.AgcModeFixedDigital)
            {
                /* Adjust for different parameter interpretation in FixedDigital mode */
                analogTarget = compressionGaindB;
            }
#if MIC_LEVEL_FEEDBACK
	this.analogTarget += this.targetIdxOffset;
#endif
            /* Since the offset between RMS and ENV is not constant, we should make this into a
	 * table, but for now, we'll stick with a constant, tuned for the chosen analog
	 * target level.
	 */
            targetIdx = ANALOG_TARGET_LEVEL + OFFSET_ENV_TO_RMS;
#if MIC_LEVEL_FEEDBACK
	this.targetIdx += this.targetIdxOffset;
#endif
            /* Analog adaptation limits */
            /* analogTargetLevel = round((32767*10^(-targetIdx/20))^2*16/2^7) */
            analogTargetLevel = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx]; /* ex. -20 dBov */
            startUpperLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx - 1];/* -19 dBov */
            startLowerLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx + 1];/* -21 dBov */
            upperPrimaryLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx - 2];/* -18 dBov */
            lowerPrimaryLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx + 2];/* -22 dBov */
            upperSecondaryLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx - 5];/* -15 dBov */
            lowerSecondaryLimit = RXX_BUFFER_LEN * kTargetLevelTable[targetIdx + 5];/* -25 dBov */
            upperLimit = startUpperLimit;
            lowerLimit = startLowerLimit;
        }

        static int WebRtcAgc_CalculateGainTable(int[] gainTable, // Q16
                                       short digCompGaindB, // Q0
                                       short targetLevelDbfs,// Q0
                                       AgcBoolean limiterEnable,
                                       short analogTarget) // Q0
        {
            // This function generates the compressor gain table used in the fixed digital part.
            uint tmpU32no1, tmpU32no2, absInLevel, logApprox;
            int inLevel, limiterLvl;
            int tmp32, tmp32no1, tmp32no2, numFIX, den, y32;
            const ushort kLog10 = 54426; // log2(10)     in Q14
            const ushort kLog10_2 = 49321; // 10*log10(2)  in Q14
            const ushort kLogE_1 = 23637; // log2(e)      in Q14
            ushort constMaxGain;
            ushort tmpU16, intPart, fracPart;
            const short kCompRatio = 3;
            const short kSoftLimiterLeft = 1;
            short limiterOffset = 0; // Limiter offset
            short limiterIdx, limiterLvlX;
            short constLinApprox, zeroGainLvl, maxGain, diffGain;
            short i, tmp16, tmp16no1;
            int zeros, zerosScale;

            // Constants
            //    kLogE_1 = 23637; // log2(e)      in Q14
            //    kLog10 = 54426; // log2(10)     in Q14
            //    kLog10_2 = 49321; // 10*log10(2)  in Q14

            // Calculate maximum digital gain and zero gain level
            tmp32no1 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(digCompGaindB - analogTarget, kCompRatio - 1);
            tmp16no1 = (short)(analogTarget - targetLevelDbfs);
            tmp16no1 += WebRtcSpl_DivW32W16ResW16(tmp32no1 + (kCompRatio >> 1), kCompRatio);
            maxGain = WebRtcUtil.WEBRTC_SPL_MAX(tmp16no1, (short)(analogTarget - targetLevelDbfs));
            tmp32no1 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(maxGain, kCompRatio);
            zeroGainLvl = digCompGaindB;
            zeroGainLvl -= WebRtcSpl_DivW32W16ResW16(tmp32no1 + ((kCompRatio - 1) >> 1),
                                                     kCompRatio - 1);
            if ((digCompGaindB <= analogTarget) && (limiterEnable == AgcBoolean.AgcTrue))
            {
                zeroGainLvl += (short)(analogTarget - digCompGaindB + kSoftLimiterLeft);
                limiterOffset = 0;
            }

            // Calculate the difference between maximum gain and gain at 0dB0v:
            //  diffGain = maxGain + (compRatio-1)*zeroGainLvl/compRatio
            //           = (compRatio-1)*digCompGaindB/compRatio
            tmp32no1 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(digCompGaindB, kCompRatio - 1);
            diffGain = WebRtcSpl_DivW32W16ResW16(tmp32no1 + (kCompRatio >> 1), kCompRatio);
            if (diffGain < 0)
            {
                return -1;
            }

            // Calculate the limiter level and index:
            //  limiterLvlX = analogTarget - limiterOffset
            //  limiterLvl  = targetLevelDbfs + limiterOffset/compRatio
            limiterLvlX = (short)(analogTarget - limiterOffset);
            limiterIdx = (short)(2
                    + WebRtcSpl_DivW32W16ResW16(WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(limiterLvlX, 13),
                                                WEBRTC_SPL_RSHIFT_U16(kLog10_2, 1)));
            tmp16no1 = WebRtcSpl_DivW32W16ResW16(limiterOffset + (kCompRatio >> 1), kCompRatio);
            limiterLvl = targetLevelDbfs + tmp16no1;

            // Calculate (through table lookup):
            //  constMaxGain = log2(1+2^(log2(e)*diffGain)); (in Q8)
            constMaxGain = kGenFuncTable[diffGain]; // in Q8

            // Calculate a parameter used to approximate the fractional part of 2^x with a
            // piecewise linear function in Q14:
            //  constLinApprox = round(3/2*(4*(3-2*sqrt(2))/(log(2)^2)-0.5)*2^14);
            constLinApprox = 22817; // in Q14

            // Calculate a denominator used in the exponential part to convert from dB to linear scale:
            //  den = 20*constMaxGain (in Q8)
            den = WEBRTC_SPL_MUL_16_U16(20, constMaxGain); // in Q8

            for (i = 0; i < 32; i++)
            {
                // Calculate scaled input level (compressor):
                //  inLevel = fix((-constLog10_2*(compRatio-1)*(1-i)+fix(compRatio/2))/compRatio)
                tmp16 = (short)WebRtcUtil.WEBRTC_SPL_MUL_16_16(kCompRatio - 1, i - 1); // Q0
                tmp32 = WEBRTC_SPL_MUL_16_U16(tmp16, kLog10_2) + 1; // Q14
                inLevel = WebRtcUtil.WebRtcSpl_DivW32W16(tmp32, kCompRatio); // Q14

                // Calculate diffGain-inLevel, to map using the genFuncTable
                inLevel = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(diffGain, 14) - inLevel; // Q14

                // Make calculations on abs(inLevel) and compensate for the sign afterwards.
                absInLevel = (uint)WEBRTC_SPL_ABS_W32(inLevel); // Q14

                // LUT with interpolation
                intPart = (ushort)WEBRTC_SPL_RSHIFT_U32(absInLevel, 14);
                fracPart = (ushort)(absInLevel & 0x00003FFF); // extract the fractional part
                tmpU16 = (ushort)(kGenFuncTable[intPart + 1] - kGenFuncTable[intPart]); // Q8
                tmpU32no1 = WEBRTC_SPL_UMUL_16_16(tmpU16, fracPart); // Q22
                tmpU32no1 += WEBRTC_SPL_LSHIFT_U32(kGenFuncTable[intPart], 14); // Q22
                logApprox = WEBRTC_SPL_RSHIFT_U32(tmpU32no1, 8); // Q14
                                                                 // Compensate for negative exponent using the relation:
                                                                 //  log2(1 + 2^-x) = log2(1 + 2^x) - x
                if (inLevel < 0)
                {
                    zeros = WebRtcSpl_NormU32(absInLevel);
                    zerosScale = 0;
                    if (zeros < 15)
                    {
                        // Not enough space for multiplication
                        tmpU32no2 = WEBRTC_SPL_RSHIFT_U32(absInLevel, 15 - zeros); // Q(zeros-1)
                        tmpU32no2 = WEBRTC_SPL_UMUL_32_16(tmpU32no2, kLogE_1); // Q(zeros+13)
                        if (zeros < 9)
                        {
                            tmpU32no1 = WEBRTC_SPL_RSHIFT_U32(tmpU32no1, 9 - zeros); // Q(zeros+13)
                            zerosScale = 9 - zeros;
                        }
                        else
                        {
                            tmpU32no2 = WEBRTC_SPL_RSHIFT_U32(tmpU32no2, zeros - 9); // Q22
                        }
                    }
                    else
                    {
                        tmpU32no2 = WEBRTC_SPL_UMUL_32_16(absInLevel, kLogE_1); // Q28
                        tmpU32no2 = WEBRTC_SPL_RSHIFT_U32(tmpU32no2, 6); // Q22
                    }
                    logApprox = 0;
                    if (tmpU32no2 < tmpU32no1)
                    {
                        logApprox = WEBRTC_SPL_RSHIFT_U32(tmpU32no1 - tmpU32no2, 8 - zerosScale); //Q14
                    }
                }
                numFIX = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(WEBRTC_SPL_MUL_16_U16(maxGain, constMaxGain), 6); // Q14
                numFIX -= WEBRTC_SPL_MUL_32_16((int)logApprox, diffGain); // Q14

                // Calculate ratio
                // Shift numFIX as much as possible
                zeros = WebRtcSpl_NormW32(numFIX);
                numFIX = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(numFIX, zeros); // Q(14+zeros)

                // Shift den so we end up in Qy1
                tmp32no1 = WebRtcUtil.WEBRTC_SPL_SHIFT_W32(den, zeros - 8); // Q(zeros)
                if (numFIX < 0)
                {
                    numFIX -= WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32no1, 1);
                }
                else
                {
                    numFIX += WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32no1, 1);
                }
                y32 = WEBRTC_SPL_DIV(numFIX, tmp32no1); // in Q14
                if (limiterEnable == AgcBoolean.AgcTrue && (i < limiterIdx))
                {
                    tmp32 = WEBRTC_SPL_MUL_16_U16(i - 1, kLog10_2); // Q14
                    tmp32 -= WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(limiterLvl, 14); // Q14
                    y32 = WebRtcUtil.WebRtcSpl_DivW32W16(tmp32 + 10, 20);
                }
                if (y32 > 39000)
                {
                    tmp32 = WEBRTC_SPL_MUL(y32 >> 1, kLog10) + 4096; // in Q27
                    tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 13); // in Q14
                }
                else
                {
                    tmp32 = WEBRTC_SPL_MUL(y32, kLog10) + 8192; // in Q28
                    tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 14); // in Q14
                }
                tmp32 += WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(16, 14); // in Q14 (Make sure final output is in Q16)

                // Calculate power
                if (tmp32 > 0)
                {
                    intPart = (ushort)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 14);
                    fracPart = (ushort)(tmp32 & 0x00003FFF); // in Q14
                    if (WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(fracPart, 13) != 0)
                    {
                        tmp16 = (short)(WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(2, 14) - constLinApprox);
                        tmp32no2 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(1, 14) - fracPart;
                        tmp32no2 = WEBRTC_SPL_MUL_32_16(tmp32no2, tmp16);
                        tmp32no2 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32no2, 13);
                        tmp32no2 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(1, 14) - tmp32no2;
                    }
                    else
                    {
                        tmp16 = (short)(constLinApprox - WebRtcUtil.WEBRTC_SPL_LSHIFT_W16(1, 14));
                        tmp32no2 = WEBRTC_SPL_MUL_32_16(fracPart, tmp16);
                        tmp32no2 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32no2, 13);
                    }
                    fracPart = (ushort)tmp32no2;
                    gainTable[i] = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(1, intPart)
                            + WebRtcUtil.WEBRTC_SPL_SHIFT_W32(fracPart, intPart - 14);
                }
                else
                {
                    gainTable[i] = 0;
                }
            }

            return 0;
        }
        /*
		 * This function replaces the analog microphone with a virtual one.
		 * It is a digital gain applied to the input signal and is used in the
		 * agcAdaptiveDigital mode where no microphone level is adjustable.
		 * Microphone speech length can be either 10ms or 20ms. The length of the
		 * input speech vector must be given in samples (80/160 when FS=8000, and
		 * 160/320 when FS=16000 or FS=32000).
		 *
		 * Input:
		 *      - agcInst           : AGC instance.
		 *      - inMic             : Microphone input speech vector for (10 or 20 ms)
		 *                            L band
		 *      - inMic_H           : Microphone input speech vector for (10 or 20 ms)
		 *                            H band
		 *      - samples           : Number of samples in input vector
		 *      - micLevelIn        : Input level of microphone (static)
		 *
		 * Output:
		 *      - inMic             : Microphone output after processing (L band)
		 *      - inMic_H           : Microphone output after processing (H band)
		 *      - micLevelOut       : Adjusted microphone level after processing
		 *
		 * Return value:
		 *                          :  0 - Normal operation.
		 *                          : -1 - Error
		 */
        public int WebRtcAgc_VirtualMic(
                                 short[] in_near, short[] in_near_H,
                                 short samples,
                                 int micLevelIn,
                                 out int micLevelOut)
        {
            int tmpFlt, micLevelTmp, gainIdx;
            ushort gain;
            short ii;

            uint nrg;
            short sampleCntr;
            uint frameNrg = 0;
            uint frameNrgLimit = 5500;
            short numZeroCrossing = 0;
            const short kZeroCrossingLowLim = 15;
            const short kZeroCrossingHighLim = 20;


            /*
			 *  Before applying gain decide if this is a low-level signal.
			 *  The idea is that digital AGC will not adapt to low-level
			 *  signals.
			 */
            if (fs != 8000)
            {
                frameNrgLimit = frameNrgLimit << 1;
            }

            frameNrg = (uint)WebRtcUtil.WEBRTC_SPL_MUL_16_16(in_near[0], in_near[0]);
            for (sampleCntr = 1; sampleCntr < samples; sampleCntr++)
            {

                // increment frame energy if it is less than the limit
                // the correct value of the energy is not important
                if (frameNrg < frameNrgLimit)
                {
                    nrg = (uint)WebRtcUtil.WEBRTC_SPL_MUL_16_16(in_near[sampleCntr], in_near[sampleCntr]);
                    frameNrg += nrg;
                }

                // Count the zero crossings
                numZeroCrossing += (short)(((in_near[sampleCntr] ^ in_near[sampleCntr - 1]) < 0) ? 1 : 0);
            }

            if ((frameNrg < 500) || (numZeroCrossing <= 5))
            {
                lowLevelSignal = 1;
            }
            else if (numZeroCrossing <= kZeroCrossingLowLim)
            {
                lowLevelSignal = 0;
            }
            else if (frameNrg <= frameNrgLimit)
            {
                lowLevelSignal = 1;
            }
            else if (numZeroCrossing >= kZeroCrossingHighLim)
            {
                lowLevelSignal = 1;
            }
            else
            {
                lowLevelSignal = 0;
            }

            //WriteDebugMessage(String.Format("(C#) AGC 502   micLevelIn = {0}", micLevelIn));
            micLevelTmp = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(micLevelIn, scale);
            /* Set desired level */
            gainIdx = micVol;
            //WriteDebugMessage(String.Format("(C#) AGC 501   micVol = {0}, micLevelTmp = {1}, this.micRef = {2}", this.micVol, micLevelTmp, this.micRef));
            if (micVol > maxAnalog)
            {
                gainIdx = maxAnalog;
            }
            if (micLevelTmp != micRef)
            {
                /* Something has happened with the physical level, restart. */
                micRef = micLevelTmp;
                micVol = 127;
                micLevelOut = 127;
                micGainIdx = 127;
                gainIdx = 127;
            }
            /* Pre-process the signal to emulate the microphone level. */
            /* Take one step at a time in the gain table. */
            //WriteDebugMessage(String.Format("(C#) AGC 500    gainIdx = {0} micVol = {1}", gainIdx, this.micVol));
            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("micVol", (double)micVol, 2);

            if (gainIdx > 127)
            {
                gain = kGainTableVirtualMic[gainIdx - 128];
            }
            else
            {
                gain = (ushort)kSuppressionTableVirtualMic[127 - gainIdx];
            }
            for (ii = 0; ii < samples; ii++)
            {
                tmpFlt = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(WEBRTC_SPL_MUL_16_U16(in_near[ii], gain), 10);
                if (tmpFlt > 32767)
                {
                    tmpFlt = 32767;
                    gainIdx--;
                    if (gainIdx >= 127)
                    {
                        gain = kGainTableVirtualMic[gainIdx - 127];
                    }
                    else
                    {
                        gain = (ushort)kSuppressionTableVirtualMic[127 - gainIdx];
                    }
                }
                if (tmpFlt < -32768)
                {
                    tmpFlt = -32768;
                    gainIdx--;
                    if (gainIdx >= 127)
                    {
                        gain = kGainTableVirtualMic[gainIdx - 127];
                    }
                    else
                    {
                        gain = (ushort)kSuppressionTableVirtualMic[127 - gainIdx];
                    }
                }
                in_near[ii] = (short)tmpFlt;
                if (fs == 32000)
                {
                    tmpFlt = WEBRTC_SPL_MUL_16_U16(in_near_H[ii], gain);
                    tmpFlt = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmpFlt, 10);
                    if (tmpFlt > 32767)
                    {
                        tmpFlt = 32767;
                    }
                    if (tmpFlt < -32768)
                    {
                        tmpFlt = -32768;
                    }
                    in_near_H[ii] = (short)tmpFlt;
                }
            }
            /* Set the level we (finally) used */
            micGainIdx = gainIdx;
            //    *micLevelOut = this.micGainIdx;
            micLevelOut = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(micGainIdx, scale);
            /* Add to Mic as if it was the output from a true microphone */

            //WriteDebugMessage(String.Format("(C#) AGC 02001     in_near[143] = {0}", in_near[143]));//todo
            if (WebRtcAgc_AddMic(in_near, in_near_H, samples) != 0)
            {
                return -1;
            }
            return 0;
        }


        /* This function processes a 10/20ms frame of microphone speech to determine
* if there is active speech. Microphone speech length can be either 10ms or
* 20ms. The length of the input speech vector must be given in samples
* (80/160 when FS=8000, and 160/320 when FS=16000 or FS=32000). For very low
* input levels, the input signal is increased in level by multiplying and
* overwriting the samples in inMic[].
*
* This function should be called before any further processing of the
* near-end microphone signal.
*
* Input:
*      - agcInst           : AGC instance.
*      - inMic             : Microphone input speech vector (10 or 20 ms) for
*                            L band
*      - inMic_H           : Microphone input speech vector (10 or 20 ms) for
*                            H band
*      - samples           : Number of samples in input vector
*
* Return value:
*                          :  0 - Normal operation.
*                          : -1 - Error
*/
        int WebRtcAgc_AddMic(short[] in_mic, short[] in_mic_H, short samples)
        {
            int nrg, max_nrg, sample, tmp32;
            int ptrIndex;
            ushort targetGainIdx, gain;
            short i, n, L, M, subFrames, tmp16;
            var tmp_speech = new short[16];

            //default/initial values corresponding to 10ms for wb and swb
            M = 10;
            L = 16;
            subFrames = 160;

            if (fs == 8000)
            {
                if (samples == 80)
                {
                    subFrames = 80;
                    M = 10;
                    L = 8;
                }
                else if (samples == 160)
                {
                    subFrames = 80;
                    M = 20;
                    L = 8;
                }
                else
                {
                    return -1;
                }
            }
            else if (fs == 16000)
            {
                if (samples == 160)
                {
                    subFrames = 160;
                    M = 10;
                    L = 16;
                }
                else if (samples == 320)
                {
                    subFrames = 160;
                    M = 20;
                    L = 16;
                }
                else
                {
                    return -1;
                }
            }
            else if (fs == 32000)
            {
                /* SWB is processed as 160 sample for L and H bands */
                if (samples == 160)
                {
                    subFrames = 160;
                    M = 10;
                    L = 16;
                }
                else
                {
                    return -1;
                }
            }

            /* Check for valid pointers based on sampling rate */
            if ((fs == 32000) && (in_mic_H == null))
            {
                return -1;
            }
            /* Check for valid pointer for low band */
            if (in_mic == null)
            {
                return -1;
            }

            /* apply slowly varying digital gain */
            if (micVol > maxAnalog)
            {
                /* Q1 */
                tmp16 = (short)(micVol - maxAnalog);
                tmp32 = WebRtcUtil.WEBRTC_SPL_MUL_16_16(GAIN_TBL_LEN - 1, tmp16);
                tmp16 = (short)(maxLevel - maxAnalog);
                targetGainIdx = (ushort)WEBRTC_SPL_DIV(tmp32, tmp16);
                if ((targetGainIdx < GAIN_TBL_LEN) == false) throw new Exception();

                /* Increment through the table towards the target gain.
				 * If micVol drops below maxAnalog, we allow the gain
				 * to be dropped immediately. */
                if (gainTableIdx < targetGainIdx)
                {
                    gainTableIdx++;
                }
                else if (gainTableIdx > targetGainIdx)
                {
                    gainTableIdx--;
                }

                /* Q12 */
                gain = kGainTableAnalog[gainTableIdx];

                for (i = 0; i < samples; i++)
                {
                    // For lower band
                    tmp32 = WEBRTC_SPL_MUL_16_U16(in_mic[i], gain);
                    sample = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 12);
                    if (sample > 32767)
                    {
                        in_mic[i] = 32767;
                    }
                    else if (sample < -32768)
                    {
                        in_mic[i] = -32768;
                    }
                    else
                    {
                        in_mic[i] = (short)sample;
                    }

                    // For higher band
                    if (fs == 32000)
                    {
                        tmp32 = WEBRTC_SPL_MUL_16_U16(in_mic_H[i], gain);
                        sample = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 12);
                        if (sample > 32767)
                        {
                            in_mic_H[i] = 32767;
                        }
                        else if (sample < -32768)
                        {
                            in_mic_H[i] = -32768;
                        }
                        else
                        {
                            in_mic_H[i] = (short)sample;
                        }
                    }
                }
            }
            else
            {
                gainTableIdx = 0;
            }

            /* compute envelope */
            if ((M == 10) && (inQueue > 0))
            {
                ptrIndex = 1;
            }
            else
            {
                ptrIndex = 0;
            }

            for (i = 0; i < M; i++)
            {
                /* iterate over samples */
                max_nrg = 0;
                for (n = 0; n < L; n++)
                {
                    nrg = WebRtcUtil.WEBRTC_SPL_MUL_16_16(in_mic[i * L + n], in_mic[i * L + n]);
                    if (nrg > max_nrg)
                    {
                        max_nrg = nrg;
                    }
                }

                env[ptrIndex][i] = max_nrg;
            }

            /* compute energy */
            if ((M == 10) && (inQueue > 0))
            {
                ptrIndex = 1;
            }
            else
            {
                ptrIndex = 0;
            }

            for (i = 0; i < WebRtcUtil.WEBRTC_SPL_RSHIFT_W16(M, 1); i++)
            {
                if (fs == 16000)
                {
                    WebRtcSpl_DownsampleBy2(in_mic, i * 32, 32, tmp_speech, filterState);
                }
                else
                {
                    Buffer.BlockCopy(in_mic, i * 16 * sizeof(short), tmp_speech, 0, 16 * sizeof(short));
                    // Array.Copy(in_mic, i * 16, tmp_speech, 0, 16);
                    //memcpy(tmp_speech, &in_mic[i * 16], 16 * sizeof(short));
                }
                /* Compute energy in blocks of 16 samples */

                Rxx16w32_array[ptrIndex][i] = WebRtcSpl_DotProductWithScale(tmp_speech, tmp_speech, 16, 4);
            }

            /* update queue information */
            if ((inQueue == 0) && (M == 10))
            {
                inQueue = 1;
            }
            else
            {
                inQueue = 2;
            }

            //WriteDebugMessage(String.Format("(C#) AGC 01001     in_mic[143] = {0}", in_mic[143]));//todo:difference

            /* call VAD (use low band only) */
            for (i = 0; i < samples; i += subFrames)
            {
                vadMic.WebRtcAgc_ProcessVad(in_mic, i, subFrames);

                // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC 01002 vadMic.logRatio", (double)vadMic.logRatio, 2);
                //WriteDebugMessage(String.Format("(C#) AGC 01002     vadMic.logRatio = {0}", vadMic.logRatio));
            }

            return 0;
        }

        #region DownsampleBy2
        static readonly ushort[] kResampleAllpass1 = new ushort[] { 3284, 24441, 49528 };
        static readonly ushort[] kResampleAllpass2 = new ushort[] { 12199, 37471, 60255 };
        static void WebRtcSpl_DownsampleBy2(short[] inArray, int inArrayPtr, short len,
                                     short[] outArray, int[] filtState)
        {
            int outptr;
            int statePtr;
            int tmp1, tmp2, diff, in32, out32;
            short i;

            // local versions of poinArrayters to inArrayput and output arrays

            outptr = 0; // output array (of length len/2)
            statePtr = 0; // filter state array; length = 8

            for (i = (short)(len >> 1); i > 0; i--)
            {
                // lower allpass filter
                in32 = (inArray[inArrayPtr]) << 10; inArrayPtr++;
                diff = in32 - filtState[statePtr + 1];
                tmp1 = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass2[0], diff, filtState[statePtr + 0]);
                filtState[statePtr + 0] = in32;
                diff = tmp1 - filtState[statePtr + 2];
                tmp2 = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass2[1], diff, filtState[statePtr + 1]);
                filtState[statePtr + 1] = tmp1;
                diff = tmp2 - filtState[statePtr + 3];
                filtState[statePtr + 3] = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass2[2], diff, filtState[statePtr + 2]);
                filtState[statePtr + 2] = tmp2;

                // upper allpass filter
                in32 = (inArray[inArrayPtr]) << 10; inArrayPtr++;
                diff = in32 - filtState[statePtr + 5];
                tmp1 = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass1[0], diff, filtState[statePtr + 4]);
                filtState[statePtr + 4] = in32;
                diff = tmp1 - filtState[statePtr + 6];
                tmp2 = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass1[1], diff, filtState[statePtr + 5]);
                filtState[statePtr + 5] = tmp1;
                diff = tmp2 - filtState[statePtr + 7];
                filtState[statePtr + 7] = WEBRTC_SPL_SCALEDIFF32(kResampleAllpass1[2], diff, filtState[statePtr + 6]);
                filtState[statePtr + 6] = tmp2;

                // add two allpass outArrayputs, divide by two and round
                out32 = (filtState[statePtr + 3] + filtState[statePtr + 7] + 1024) >> 11;

                // limit amplitude to prevent wrap-around, and write to output array
                if (out32 > 32767)
                    outArray[outptr] = 32767;
                else if (out32 < -32768)
                    outArray[outptr] = -32768;
                else
                    outArray[outptr] = (short)out32;

                outptr++;
            }
        }
        #endregion

        /*
 * This function processes a 10/20ms frame and adjusts (normalizes) the gain
 * both analog and digitally. The gain adjustments are done only during
 * active periods of speech. The input speech length can be either 10ms or
 * 20ms and the output is of the same length. The length of the speech
 * vectors must be given in samples (80/160 when FS=8000, and 160/320 when
 * FS=16000 or FS=32000). The echo parameter can be used to ensure the AGC will
 * not adjust upward in the presence of echo.
 *
 * This function should be called after processing the near-end microphone
 * signal, in any case after any echo cancellation.
 *
 * Input:
 *      - agcInst           : AGC instance
 *      - inNear            : Near-end input speech vector (10 or 20 ms) for
 *                            L band
 *      - inNear_H          : Near-end input speech vector (10 or 20 ms) for
 *                            H band
 *      - samples           : Number of samples in input/output vector
 *      - inMicLevel        : Current microphone volume level
 *      - echo              : Set to 0 if the signal passed to add_mic is
 *                            almost certainly free of echo; otherwise set
 *                            to 1. If you have no information regarding echo
 *                            set to 0.
 *
 * Output:
 *      - outMicLevel       : Adjusted microphone volume level
 *      - out               : Gain-adjusted near-end speech vector (L band)
 *                          : May be the same vector as the input.
 *      - out_H             : Gain-adjusted near-end speech vector (H band)
 *      - saturationWarning : A returned value of 1 indicates a saturation event
 *                            has occurred and the volume cannot be further
 *                            reduced. Otherwise will be set to 0.
 *
 * Return value:
 *                          :  0 - Normal operation.
 *                          : -1 - Error
 */
        public int WebRtcAgc_Process(short[] in_near, short[] in_near_H, short samples,
                  short[] outArray, short[] out_H, int inMicLevel,
                  out int outMicLevel, short echo,
                  out bool saturationWarning)
        {
            short subFrames, i;
            bool satWarningTmp = false;

            outMicLevel = 0;
            saturationWarning = false;
            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC WebRtcAgc_Process inMicLevel 7", (double)inMicLevel, 2);


            if (fs == 8000)
            {
                if ((samples != 80) && (samples != 160))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->Process, frame %d: Invalid number of samples\n\n", this.fcount);
#endif
                    return -1;
                }
                subFrames = 80;
            }
            else if (fs == 16000)
            {
                if ((samples != 160) && (samples != 320))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->Process, frame %d: Invalid number of samples\n\n", this.fcount);
#endif
                    return -1;
                }
                subFrames = 160;
            }
            else if (fs == 32000)
            {
                if ((samples != 160) && (samples != 320))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->Process, frame %d: Invalid number of samples\n\n", this.fcount);
#endif
                    return -1;
                }
                subFrames = 160;
            }
            else
            {
#if AGC_DEBUG// test log
		fprintf(this.fpt,
				"AGC->Process, frame %d: Invalid sample rate\n\n", this.fcount);
#endif
                return -1;
            }

            /* Check for valid pointers based on sampling rate */
            if (fs == 32000 && in_near_H == null)
            {
                return -1;
            }
            /* Check for valid pointers for low band */
            if (in_near == null)
            {
                return -1;
            }

            saturationWarning = false;
            //TODO: PUT IN RANGE CHECKING FOR INPUT LEVELS
            outMicLevel = inMicLevel;
            int inMicLevelTmp = inMicLevel;

            //WriteDebugMessage(String.Format("(C#) AGC 0123     inMicLevel = {0}  in_near[103] = {1}", inMicLevel, in_near[103]));

            Buffer.BlockCopy(in_near, 0, outArray, 0, samples * sizeof(short));
            // Array.Copy(in_near, 0, outArray, 0, samples);
            //memcpy(outArray, in_near, samples * sizeof(short));
            if (fs == 32000)
            {
                Buffer.BlockCopy(in_near_H, 0, out_H, 0, samples * sizeof(short));
                // Array.Copy(in_near_H, 0, out_H, 0, samples);
                // memcpy(out_H, in_near_H, samples * sizeof(short));
            }

#if AGC_DEBUG//test log
	this.fcount++;
#endif

            for (i = 0; i < samples; i += subFrames)
            {
                if (digitalAgc.WebRtcAgc_ProcessDigital(in_near, i, in_near_H, i, outArray, i, out_H, i,
                                   fs, lowLevelSignal) == -1)
                {
#if AGC_DEBUG//test log
			fprintf(this.fpt, "AGC->Process, frame %d: Error from DigAGC\n\n", this.fcount);
#endif
                    return -1;
                }

                //WriteDebugMessage(String.Format("(C#) AGC 01     outArray[127] = {0}", outArray[127]));
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC outArray[127]", (double)outArray[127], 2);
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC outMicLevel 7", (double)outMicLevel, 2);

                if ((agcMode < AgcMode.AgcModeFixedDigital) && ((lowLevelSignal == 0)
                        || (agcMode != AgcMode.AgcModeAdaptiveDigital)))
                {
                    if (WebRtcAgc_ProcessAnalog(inMicLevelTmp, ref outMicLevel,
                                                  vadMic.logRatio, echo, ref saturationWarning) == -1)
                    {
                        return -1;
                    }
                }

                //WriteDebugMessage(String.Format("(C#) AGC 01     vadMic.logRatio = {0}", vadMic.logRatio));
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC logRatio 1", (double)this.vadMic.logRatio, 2);
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC outMicLevel 1", (double)outMicLevel, 2);
                // deviation at frame 3213 for 'AGC outMicLevel 1': values 126 and 130, difference = 3,17460317460317%

#if AGC_DEBUG//test log
		fprintf(this.agcLog, "%5d\t%d\t%d\t%d\n", this.fcount, inMicLevelTmp, *outMicLevel, this.maxLevel, this.micVol);
#endif

                /* update queue */
                if (inQueue > 1)
                {
                    for (int k = 0; k < 10; k++)
                        env[0][k] = env[1][k];

                    //memcpy(this.env[0], this.env[1], 10 * sizeof(int));
                    for (int k = 0; k < 5; k++)
                        Rxx16w32_array[0][k] = Rxx16w32_array[1][k];
                }

                if (inQueue > 0)
                {
                    inQueue--;
                }

                /* If 20ms frames are used the input mic level must be updated so that
				 * the analog AGC does not think that there has been a manual volume
				 * change. */
                inMicLevelTmp = outMicLevel;

                /* Store a positive saturation warning. */
                if (saturationWarning)
                {
                    satWarningTmp = true;
                }
            }

            /* Trigger the saturation warning if displayed by any of the frames. */
            saturationWarning = satWarningTmp;

            //WriteDebugMessage(String.Format("(C#) AGC 700   micVol = {0}", this.micVol));

            return 0;
        }
        int WebRtcAgc_ProcessAnalog(int inMicLevel,
                                    ref int outMicLevel,
                                    short vadLogRatio,
                                    short echo, ref bool saturationWarning)
        {
            uint tmpU32;
            int Rxx16w32, tmp32;
            int inMicLevelTmp, lastMicVol;
            short i;
            bool saturated = false;


            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC outMicLevel 100", (double)outMicLevel, 2);

            inMicLevelTmp = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(inMicLevel, scale);

            if (inMicLevelTmp > maxAnalog)
            {
#if AGC_DEBUG //test log
		fprintf(this.fpt, "\tAGC->ProcessAnalog, frame %d: micLvl > maxAnalog\n", this.fcount);
#endif
                return -1;
            }
            if (inMicLevelTmp < minLevel)
            {
#if AGC_DEBUG //test log
		fprintf(this.fpt, "\tAGC->ProcessAnalog, frame %d: micLvl < minLevel\n", this.fcount);
#endif
                return -1;
            }

            if (firstCall == 0)
            {
                firstCall = 1;
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32((maxLevel - minLevel) * 51, 9);
                int tmpVol = (minLevel + tmp32);

                /* If the mic level is very low at start, increase it! */
                if ((inMicLevelTmp < tmpVol) && (agcMode == AgcMode.AgcModeAdaptiveAnalog))
                {
                    inMicLevelTmp = tmpVol;
                }
                micVol = inMicLevelTmp;
            }

            /* Set the mic level to the previous output value if there is digital input gain */
            if ((inMicLevelTmp == maxAnalog) && (micVol > maxAnalog))
            {
                inMicLevelTmp = micVol;
            }

            /* If the mic level was manually changed to a very low value raise it! */
            if ((inMicLevelTmp != micVol) && (inMicLevelTmp < minOutput))
            {
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32((maxLevel - minLevel) * 51, 9);
                inMicLevelTmp = (minLevel + tmp32);
                micVol = inMicLevelTmp;
#if MIC_LEVEL_FEEDBACK
		//this.numBlocksMicLvlSat = 0;
#endif
#if AGC_DEBUG //test log
		fprintf(this.fpt,
				"\tAGC->ProcessAnalog, frame %d: micLvl < minLevel by manual decrease, raise vol\n",
				this.fcount);
#endif
            }

            if (inMicLevelTmp != micVol)
            {
                // Incoming level mismatch; update our level.
                // This could be the case if the volume is changed manually, or if the
                // sound device has a low volume resolution.
                micVol = inMicLevelTmp;
            }

            if (inMicLevelTmp > maxLevel)
            {
                // Always allow the user to raise the volume above the maxLevel.
                maxLevel = inMicLevelTmp;
            }

            //WriteDebugMessage(String.Format("(C#) AGC 600  micVol = {0}", this.micVol));

            // Store last value here, after we've taken care of manual updates etc.
            lastMicVol = micVol;

            /* Checks if the signal is saturated. Also a check if individual samples
			 * are larger than 12000 is done. If they are the counter for increasing
			 * the volume level is set to -100ms
			 */
            WebRtcAgc_SaturationCtrl(ref saturated, env);

            /* The AGC is always allowed to lower the level if the signal is saturated */
            if (saturated)
            {
                /* Lower the recording level
				 * Rxx160_LP is adjusted down because it is so slow it could
				 * cause the AGC to make wrong decisions. */
                /* this.Rxx160_LPw32 *= 0.875; */
                Rxx160_LPw32 = WEBRTC_SPL_MUL(WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160_LPw32, 3), 7);

                zeroCtrlMax = micVol;

                /* this.micVol *= 0.903; */
                tmp32 = inMicLevelTmp - minLevel;
                tmpU32 = WEBRTC_SPL_UMUL(29591, (uint)(tmp32));
                micVol = (int)WEBRTC_SPL_RSHIFT_U32(tmpU32, 15) + minLevel;
                if (micVol > lastMicVol - 2)
                {
                    micVol = lastMicVol - 2;
                }
                inMicLevelTmp = micVol;

#if AGC_DEBUG //test log
		fprintf(this.fpt,
				"\tAGC->ProcessAnalog, frame %d: saturated, micVol = %d\n",
				this.fcount, this.micVol);
#endif

                if (micVol < minOutput)
                {
                    saturationWarning = true;
                }

                /* Reset counter for decrease of volume level to avoid
				 * decreasing too much. The saturation control can still
				 * lower the level if needed. */
                msTooHigh = -100;

                /* Enable the control mechanism to ensure that our measure,
				 * Rxx160_LP, is in the correct range. This must be done since
				 * the measure is very slow. */
                activeSpeech = 0;
                Rxx16_LPw32Max = 0;

                /* Reset to initial values */
                msecSpeechInnerChange = kMsecSpeechInner;
                msecSpeechOuterChange = kMsecSpeechOuter;
                changeToSlowMode = 0;

                muteGuardMs = 0;

                upperLimit = startUpperLimit;
                lowerLimit = startLowerLimit;
#if MIC_LEVEL_FEEDBACK
		//this.numBlocksMicLvlSat = 0;
#endif
            }

            /* Check if the input speech is zero. If so the mic volume
			 * is increased. On some computers the input is zero up as high
			 * level as 17% */
            WebRtcAgc_ZeroCtrl(ref inMicLevelTmp, env);

            /* Check if the near end speaker is inactive.
			 * If that is the case the VAD threshold is
			 * increased since the VAD speech model gets
			 * more sensitive to any sound after a long
			 * silence.
			 */
            WebRtcAgc_SpeakerInactiveCtrl();

            for (i = 0; i < 5; i++)
            {
                /* Computed on blocks of 16 samples */

                Rxx16w32 = Rxx16w32_array[0][i];
                //WriteDebugMessage(String.Format("(C#) AGC 0001  i = {0}, Rxx16pos = {1}", i, Rxx16pos));

                /* Rxx160w32 in Q(-7) */
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx16w32 - Rxx16_vectorw32[Rxx16pos], 3);

                // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC Rxx16_vectorw32[stt->Rxx16pos] 1", (double)(this.Rxx16_vectorw32[this.Rxx16pos]), 1);
                // deviation at frame 1 for 'AGC Rxx16_vectorw32[stt->Rxx16pos] 1': values 1000 and 6007591, difference = 600659,1%

                Rxx160w32 = Rxx160w32 + tmp32;
                Rxx16_vectorw32[Rxx16pos] = Rxx16w32;

                /* Circular buffer */
                Rxx16pos++;
                if (Rxx16pos == RXX_BUFFER_LEN)
                {
                    Rxx16pos = 0;
                }

                /* Rxx16_LPw32 in Q(-4) */
                tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx16w32 - Rxx16_LPw32, kAlphaShortTerm);
                Rxx16_LPw32 = (Rxx16_LPw32) + tmp32;

                //WriteDebugMessage(String.Format("(C#) AGC 10     i = {0}, vadLogRatio = {1}, vadThreshold = {2}", i, vadLogRatio, vadThreshold));


                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC Rxx16_LPw32 2", (double)this.Rxx16_LPw32, 1);
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC vadLogRatio 1", (double)vadLogRatio, 1);
                //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC vadThreshold 1", (double)this.vadThreshold, 1);
                if (vadLogRatio > vadThreshold)
                {
                    /* Speech detected! */

                    /* Check if Rxx160_LP is in the correct range. If
					 * it is too high/low then we set it to the maximum of
					 * Rxx16_LPw32 during the first 200ms of speech.
					 */
                    if (activeSpeech < 250)
                    {
                        activeSpeech += 2;

                        if (Rxx16_LPw32 > Rxx16_LPw32Max)
                        {
                            Rxx16_LPw32Max = Rxx16_LPw32;
                        }
                    }
                    else if (activeSpeech == 250)
                    {
                        activeSpeech += 2;
                        tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx16_LPw32Max, 3);
                        Rxx160_LPw32 = WEBRTC_SPL_MUL(tmp32, RXX_BUFFER_LEN);
                    }

                    // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC Rxx160w32 1", (double)this.Rxx160w32, 1);
                    tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160w32 - Rxx160_LPw32, kAlphaLongTerm);
                    // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC Rxx160_LPw32 tmp32 1", (double)tmp32, 1);
                    Rxx160_LPw32 = Rxx160_LPw32 + tmp32;

                    // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC Rxx160_LPw32 1", (double)this.Rxx160_LPw32, 1);
                    if (Rxx160_LPw32 > upperSecondaryLimit)
                    {
                        msTooHigh += 2;
                        msTooLow = 0;
                        changeToSlowMode = 0;

                        if (msTooHigh > msecSpeechOuterChange)
                        {
                            msTooHigh = 0;

                            /* Lower the recording level */
                            /* Multiply by 0.828125 which corresponds to decreasing ~0.8dB */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160_LPw32, 6);
                            Rxx160_LPw32 = WEBRTC_SPL_MUL(tmp32, 53);

                            /* Reduce the max gain to avoid excessive oscillation
							 * (but never drop below the maximum analog level).
							 * this.maxLevel = (15 * this.maxLevel + this.micVol) / 16;
							 */
                            tmp32 = (15 * maxLevel) + micVol;
                            maxLevel = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 4);
                            maxLevel = WebRtcUtil.WEBRTC_SPL_MAX(maxLevel, maxAnalog);

                            zeroCtrlMax = micVol;

                            /* 0.95 in Q15 */
                            tmp32 = inMicLevelTmp - minLevel;
                            tmpU32 = WEBRTC_SPL_UMUL(31130, (uint)(tmp32));
                            micVol = (int)WEBRTC_SPL_RSHIFT_U32(tmpU32, 15) + minLevel;
                            if (micVol > lastMicVol - 1)
                            {
                                micVol = lastMicVol - 1;
                            }
                            inMicLevelTmp = micVol;

                            /* Enable the control mechanism to ensure that our measure,
							 * Rxx160_LP, is in the correct range.
							 */
                            activeSpeech = 0;
                            Rxx16_LPw32Max = 0;
#if MIC_LEVEL_FEEDBACK
					//this.numBlocksMicLvlSat = 0;
#endif
#if AGC_DEBUG //test log
					fprintf(this.fpt,
							"\tAGC->ProcessAnalog, frame %d: measure > 2ndUpperLim, micVol = %d, maxLevel = %d\n",
							this.fcount, this.micVol, this.maxLevel);
#endif
                        }
                    }
                    else if (Rxx160_LPw32 > upperLimit)
                    {
                        msTooHigh += 2;
                        msTooLow = 0;
                        changeToSlowMode = 0;

                        if (msTooHigh > msecSpeechInnerChange)
                        {
                            /* Lower the recording level */
                            msTooHigh = 0;
                            /* Multiply by 0.828125 which corresponds to decreasing ~0.8dB */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160_LPw32, 6);
                            Rxx160_LPw32 = WEBRTC_SPL_MUL(tmp32, 53);

                            /* Reduce the max gain to avoid excessive oscillation
							 * (but never drop below the maximum analog level).
							 * this.maxLevel = (15 * this.maxLevel + this.micVol) / 16;
							 */
                            tmp32 = (15 * maxLevel) + micVol;
                            maxLevel = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 4);
                            maxLevel = WebRtcUtil.WEBRTC_SPL_MAX(maxLevel, maxAnalog);

                            zeroCtrlMax = micVol;

                            /* 0.965 in Q15 */
                            tmp32 = inMicLevelTmp - minLevel;
                            tmpU32 = WEBRTC_SPL_UMUL(31621, (uint)(inMicLevelTmp - minLevel));
                            micVol = (int)WEBRTC_SPL_RSHIFT_U32(tmpU32, 15) + minLevel;
                            if (micVol > lastMicVol - 1)
                            {
                                micVol = lastMicVol - 1;
                            }
                            inMicLevelTmp = micVol;

#if MIC_LEVEL_FEEDBACK
					//this.numBlocksMicLvlSat = 0;
#endif
#if AGC_DEBUG //test log
					fprintf(this.fpt,
							"\tAGC->ProcessAnalog, frame %d: measure > UpperLim, micVol = %d, maxLevel = %d\n",
							this.fcount, this.micVol, this.maxLevel);
#endif
                        }
                    }
                    else if (Rxx160_LPw32 < lowerSecondaryLimit)
                    {
                        msTooHigh = 0;
                        changeToSlowMode = 0;
                        msTooLow += 2;

                        if (msTooLow > msecSpeechOuterChange)
                        {
                            /* Raise the recording level */
                            short index, weightFIX;
                            short volNormFIX = 16384; // =1 in Q14.

                            msTooLow = 0;

                            /* Normalize the volume level */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(inMicLevelTmp - minLevel, 14);
                            if (maxInit != minLevel)
                            {
                                volNormFIX = (short)WEBRTC_SPL_DIV(tmp32, (maxInit - minLevel));
                            }

                            /* Find correct curve */
                            WebRtcAgc_ExpCurve(volNormFIX, out index);

                            /* Compute weighting factor for the volume increase, 32^(-2*X)/2+1.05 */
                            weightFIX = (short)(kOffset1[index]
                                      - (short)WebRtcUtil.WEBRTC_SPL_MUL_16_16_RSFT(kSlope1[index], volNormFIX, 13));

                            /* this.Rxx160_LPw32 *= 1.047 [~0.2 dB]; */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160_LPw32, 6);
                            Rxx160_LPw32 = WEBRTC_SPL_MUL(tmp32, 67);

                            tmp32 = inMicLevelTmp - minLevel;
                            tmpU32 = ((uint)weightFIX * (uint)(inMicLevelTmp - minLevel));
                            micVol = (int)WEBRTC_SPL_RSHIFT_U32(tmpU32, 14) + minLevel;
                            if (micVol < lastMicVol + 2)
                            {
                                micVol = lastMicVol + 2;
                            }

                            inMicLevelTmp = micVol;

#if MIC_LEVEL_FEEDBACK
					/* Count ms in level saturation */
					//if (this.micVol > this.maxAnalog) {
					if (this.micVol > 150)
					{
						/* mic level is saturated */
						this.numBlocksMicLvlSat++;
						fprintf(stderr, "Sat mic Level: %d\n", this.numBlocksMicLvlSat);
					}
#endif
#if AGC_DEBUG //test log
					fprintf(this.fpt,
							"\tAGC->ProcessAnalog, frame %d: measure < 2ndLowerLim, micVol = %d\n",
							this.fcount, this.micVol);
#endif
                        }
                    }
                    else if (Rxx160_LPw32 < lowerLimit)
                    {
                        msTooHigh = 0;
                        changeToSlowMode = 0;
                        msTooLow += 2;

                        if (msTooLow > msecSpeechInnerChange)
                        {
                            /* Raise the recording level */
                            short index;
                            short volNormFIX = 16384; // =1 in Q14.

                            msTooLow = 0;

                            /* Normalize the volume level */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_LSHIFT_W32(inMicLevelTmp - minLevel, 14);
                            if (maxInit != minLevel)
                            {
                                volNormFIX = (short)WEBRTC_SPL_DIV(tmp32,
                                                                      (maxInit - minLevel));
                            }

                            /* Find correct curve */
                            WebRtcAgc_ExpCurve(volNormFIX, out index);

                            /* Compute weighting factor for the volume increase, (3.^(-2.*X))/8+1 */
                            var weightFIX = (short)(kOffset2[index]
                                          - (short)WebRtcUtil.WEBRTC_SPL_MUL_16_16_RSFT(kSlope2[index], volNormFIX, 13));

                            /* this.Rxx160_LPw32 *= 1.047 [~0.2 dB]; */
                            tmp32 = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(Rxx160_LPw32, 6);
                            Rxx160_LPw32 = WEBRTC_SPL_MUL(tmp32, 67);

                            tmp32 = inMicLevelTmp - minLevel;
                            tmpU32 = ((uint)weightFIX * (uint)(inMicLevelTmp - minLevel));
                            micVol = (int)WEBRTC_SPL_RSHIFT_U32(tmpU32, 14) + minLevel;
                            if (micVol < lastMicVol + 1)
                            {
                                micVol = lastMicVol + 1;
                            }

                            inMicLevelTmp = micVol;

#if MIC_LEVEL_FEEDBACK
					/* Count ms in level saturation */
					//if (this.micVol > this.maxAnalog) {
					if (this.micVol > 150)
					{
						/* mic level is saturated */
						this.numBlocksMicLvlSat++;
						fprintf(stderr, "Sat mic Level: %d\n", this.numBlocksMicLvlSat);
					}
#endif
#if AGC_DEBUG //test log
					fprintf(this.fpt,
							"\tAGC->ProcessAnalog, frame %d: measure < LowerLim, micVol = %d\n",
							this.fcount, this.micVol);
#endif

                        }
                    }
                    else
                    {
                        /* The signal is inside the desired range which is:
						 * lowerLimit < Rxx160_LP/640 < upperLimit
						 */
                        if (changeToSlowMode > 4000)
                        {
                            msecSpeechInnerChange = 1000;
                            msecSpeechOuterChange = 500;
                            upperLimit = upperPrimaryLimit;
                            lowerLimit = lowerPrimaryLimit;
                        }
                        else
                        {
                            changeToSlowMode += 2; // in milliseconds
                        }
                        msTooLow = 0;
                        msTooHigh = 0;

                        //Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC inMicLevelTmp 1", (double)inMicLevelTmp, 1);
                        micVol = inMicLevelTmp;

                    }
#if MIC_LEVEL_FEEDBACK
			if (this.numBlocksMicLvlSat > NUM_BLOCKS_IN_SAT_BEFORE_CHANGE_TARGET)
			{
				this.micLvlSat = 1;
				fprintf(stderr, "target before = %d (%d)\n", this.analogTargetLevel, this.targetIdx);
				WebRtcAgc_UpdateAgcThresholds(stt);
				WebRtcAgc_CalculateGainTable(&(this.digitalAgc.gainTable[0]),
						this.compressionGaindB, this.targetLevelDbfs, this.limiterEnable,
						this.analogTarget);
				this.numBlocksMicLvlSat = 0;
				this.micLvlSat = 0;
				fprintf(stderr, "target offset = %d\n", this.targetIdxOffset);
				fprintf(stderr, "target after  = %d (%d)\n", this.analogTargetLevel, this.targetIdx);
			}
#endif
                }
            }

            /* Ensure gain is not increased in presence of echo or after a mute event
			 * (but allow the zeroCtrl() increase on the frame of a mute detection).
			 */
            if (echo == 1 || (muteGuardMs > 0 && muteGuardMs < kMuteGuardTimeMs))
            {
                if (micVol > lastMicVol)
                {
                    micVol = lastMicVol;
                }
            }

            /* limit the gain */
            if (micVol > maxLevel)
            {
                micVol = maxLevel;
            }
            else if (micVol < minOutput)
            {
                micVol = minOutput;
            }

            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC stt->micVol 1", (double)this.micVol, 1);
            // todo: deviation at frame 1654 for 'AGC stt->micVol 1': values 117 and 123, difference = 5,12820512820513%
            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC stt->scale 1", (double)this.scale, 1);

            outMicLevel = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(micVol, scale);
            if (outMicLevel > WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(maxAnalog, scale))
            {
                outMicLevel = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(maxAnalog, scale);
            }

            //WriteDebugMessage(String.Format("(C#) AGC ProcessAnalog 001 outMicLevel = {0}", outMicLevel));

            // Alanta.CodeComparison.ComparisonMaker.CompareVariableInCodeCs("AGC outMicLevel", (double)outMicLevel, 1);

            return 0;
        }
        void WebRtcAgc_SaturationCtrl(ref bool saturated, int[][] env)
        {
            short i;


            /* Check if the signal is saturated */
            for (i = 0; i < 10; i++)
            {
                var tmpW16 = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(env[0][i], 20);
                if (tmpW16 > 875)
                {
                    envSum += tmpW16;
                }
            }

            if (envSum > 25000)
            {
                saturated = true;
                envSum = 0;
            }

            /* this.envSum *= 0.99; */
            envSum = (short)WebRtcUtil.WEBRTC_SPL_MUL_16_16_RSFT(envSum, 32440, 15);
        }
        void WebRtcAgc_ZeroCtrl(ref int inMicLevel, int[][] env)
        {
            short i;
            int tmp32 = 0;
            int midVal;

            /* Is the input signal zero? */
            for (i = 0; i < 10; i++)
            {
                tmp32 += env[0][i];
            }

            /* Each block is allowed to have a few non-zero
			 * samples.
			 */
            if (tmp32 < 500)
            {
                msZero += 10;
            }
            else
            {
                msZero = 0;
            }

            if (muteGuardMs > 0)
            {
                muteGuardMs -= 10;
            }

            if (msZero > 500)
            {
                msZero = 0;

                /* Increase microphone level only if it's less than 50% */
                midVal = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(maxAnalog + minLevel + 1, 1);
                if (inMicLevel < midVal)
                {
                    /* *inMicLevel *= 1.1; */
                    tmp32 = WEBRTC_SPL_MUL(1126, inMicLevel);
                    inMicLevel = WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 10);
                    /* Reduces risk of a muted mic repeatedly triggering excessive levels due
					 * to zero signal detection. */
                    inMicLevel = WebRtcUtil.WEBRTC_SPL_MIN(inMicLevel, zeroCtrlMax);
                    micVol = inMicLevel;
                }

#if AGC_DEBUG //test log
		fprintf(this.fpt,
				"\t\tAGC->zeroCntrl, frame %d: 500 ms under threshold, micVol:\n",
				this.fcount, this.micVol);
#endif

                activeSpeech = 0;
                Rxx16_LPw32Max = 0;

                /* The AGC has a tendency (due to problems with the VAD parameters), to
				 * vastly increase the volume after a muting event. This timer prevents
				 * upwards adaptation for a short period. */
                muteGuardMs = kMuteGuardTimeMs;
            }
        }
        void WebRtcAgc_SpeakerInactiveCtrl()
        {
            /* Check if the near end speaker is inactive.
			 * If that is the case the VAD threshold is
			 * increased since the VAD speech model gets
			 * more sensitive to any sound after a long
			 * silence.
			 */

            if (vadMic.stdLongTerm < 2500)
            {
                vadThreshold = 1500;
            }
            else
            {
                short vadThresh = kNormalVadThreshold;
                if (vadMic.stdLongTerm < 4500)
                {
                    /* Scale between min and max threshold */
                    vadThresh += WebRtcUtil.WEBRTC_SPL_RSHIFT_W16(4500 - vadMic.stdLongTerm, 1);
                }

                /* this.vadThreshold = (31 * this.vadThreshold + vadThresh) / 32; */
                int tmp32 = vadThresh;
                tmp32 += WebRtcUtil.WEBRTC_SPL_MUL_16_16(31, vadThreshold);
                vadThreshold = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp32, 5);
            }
        }
        void WebRtcAgc_ExpCurve(short volume, out short index)
        {
            // volume in Q14
            // index in [0-7]
            /* 8 different curves */
            if (volume > 5243)
            {
                if (volume > 7864)
                {
                    if (volume > 12124)
                    {
                        index = 7;
                    }
                    else
                    {
                        index = 6;
                    }
                }
                else
                {
                    if (volume > 6554)
                    {
                        index = 5;
                    }
                    else
                    {
                        index = 4;
                    }
                }
            }
            else
            {
                if (volume > 2621)
                {
                    if (volume > 3932)
                    {
                        index = 3;
                    }
                    else
                    {
                        index = 2;
                    }
                }
                else
                {
                    if (volume > 1311)
                    {
                        index = 1;
                    }
                    else
                    {
                        index = 0;
                    }
                }
            }
        }
        public int WebRtcAgc_AddFarend(short[] in_far, short samples)
        {
            int errHandle = 0;
            short i, subFrames;

            if (fs == 8000)
            {
                if ((samples != 80) && (samples != 160))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->add_far_end, frame %d: Invalid number of samples\n\n",
					this.fcount);
#endif
                    return -1;
                }
                subFrames = 80;
            }
            else if (fs == 16000)
            {
                if ((samples != 160) && (samples != 320))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->add_far_end, frame %d: Invalid number of samples\n\n",
					this.fcount);
#endif
                    return -1;
                }
                subFrames = 160;
            }
            else if (fs == 32000)
            {
                if ((samples != 160) && (samples != 320))
                {
#if AGC_DEBUG //test log
			fprintf(this.fpt,
					"AGC->add_far_end, frame %d: Invalid number of samples\n\n",
					this.fcount);
#endif
                    return -1;
                }
                subFrames = 160;
            }
            else
            {
#if AGC_DEBUG //test log
		fprintf(this.fpt,
				"AGC->add_far_end, frame %d: Invalid sample rate\n\n",
				this.fcount + 1);
#endif
                return -1;
            }

            for (i = 0; i < samples; i += subFrames)
            {
                errHandle += digitalAgc.WebRtcAgc_AddFarendToDigital(in_far, i, subFrames);
            }

            return errHandle;
        }
    }

}
