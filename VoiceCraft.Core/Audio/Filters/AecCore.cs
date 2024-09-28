using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Audio.Filters
{
	public class HighPassFilter
	{
		private readonly short[] ba = { 4012, -8024, 4012, 8002, -3913 };
		private readonly short[] x = new short[2];
		private readonly short[] y = new short[4];

		public void Filter(short[] data)
		{
			for (int i = 0; i < data.Length; i++)
			{
				//  y[i] = b[0] * x[i] + b[1] * x[i-1] + b[2] * x[i-2]
				//         + -a[1] * y[i-1] + -a[2] * y[i-2];

				int tmp = y[1] * ba[3];
				tmp += y[3] * ba[4]; // -a[2] * y[i-2] (low part)
				tmp = (tmp >> 15);
				tmp += y[0] * ba[3]; // -a[1] * y[i-1] (high part)
				tmp += y[2] * ba[4]; // -a[2] * y[i-2] (high part)
				tmp = (tmp << 1);

				tmp += data[i] * ba[0]; // b[0]*x[0]
				tmp += x[0] * ba[1]; // b[1]*x[i-1]
				tmp += x[1] * ba[2]; // b[2]*x[i-2]

				// Update state (input part)
				x[1] = x[0];
				x[0] = data[i];

				// Update state (filtered part)
				y[2] = y[0];
				y[3] = y[1];
				y[0] = (short)(tmp >> 13);
				y[1] = (short)((tmp - WebRtcUtil.WEBRTC_SPL_LSHIFT_W32((y[0]), 13)) << 2);

				// Rounding in Q12, i.e. add 2^11
				tmp += 2048;

				// Saturate (to 2^27) so that the HP filtered signal does not overflow
				tmp = WebRtcUtil.WEBRTC_SPL_SAT((134217727), tmp, (-134217728));

				// Convert back to Q0 and use rounding
				data[i] = (short)WebRtcUtil.WEBRTC_SPL_RSHIFT_W32(tmp, 12);
			}
		}
	}

	public class RingBuffer
	{
		#region Wrap enum

		public enum Wrap
		{
			SameWrap,
			DiffWrap
		}

		#endregion

		private readonly short[] data;
		private int readPos;
		private Wrap rwWrap = Wrap.SameWrap;
		private int writePos;

		public RingBuffer(int size)
		{
			data = new short[size];
			readPos = 0;
			writePos = 0;
			rwWrap = Wrap.SameWrap;
		}

		private int size
		{
			get { return data.Length; }
		}

		public void Write(short[] input, int length)
		{
			int margin;
			int n = length;

			if (length < 0 || length > size)
			{
				throw new ArgumentException();
			}

			if (rwWrap == Wrap.SameWrap)
			{
				margin = size - writePos;
				if (n > margin)
				{
					rwWrap = Wrap.DiffWrap;
					// Array.Copy(data, 0, this.data, writePos, margin);
					Buffer.BlockCopy(input, 0, data, writePos * sizeof(short), margin * sizeof(short));

					writePos = 0;
					n = length - margin;
				}
				else
				{
					// Array.Copy(data, 0, this.data, writePos, n);
					Buffer.BlockCopy(input, 0, data, writePos * sizeof(short), n * sizeof(short));
					writePos += n;
					return;
				}
			}

			if (rwWrap == Wrap.DiffWrap)
			{
				margin = readPos - writePos;
				if (margin > n)
				{
					margin = n;
				}
				Buffer.BlockCopy(input, (length - n) * sizeof(short), data, writePos * sizeof(short), margin * sizeof(short));
				// Array.Copy(data, size - n, this.data, writePos, margin);
				writePos += margin;
			}
		}

		public int get_buffer_size()
		{
			if (rwWrap == Wrap.SameWrap)
			{
				return writePos - readPos;
			}
			return size - readPos + writePos;
		}

		public void Flush(int length)
		{
			int margin;

			if (length <= 0 || length > size)
			{
				throw new ArgumentException();
			}

			int n = length;
			if (rwWrap == Wrap.DiffWrap)
			{
				margin = size - readPos;
				if (n > margin)
				{
					rwWrap = Wrap.SameWrap;
					readPos = 0;
					n = length - margin;
				}
				else
				{
					readPos += n;
					return;
				}
			}

			if (rwWrap == Wrap.SameWrap)
			{
				margin = writePos - readPos;
				if (margin > n)
				{
					margin = n;
				}
				readPos += margin;
			}

			return;
		}

		public void Read(short[] output, int length)
		{
			int margin;

			if (length <= 0 || length > size)
			{
				return;
			}

			int n = length;
			if (rwWrap == Wrap.DiffWrap)
			{
				margin = size - readPos;
				if (n > margin)
				{
					rwWrap = Wrap.SameWrap;
					Buffer.BlockCopy(data, readPos * sizeof(short), output, 0, margin * sizeof(short));
					// Array.Copy(this.data, readPos, data, 0, margin);

					readPos = 0;
					n = length - margin;
				}
				else
				{
					Buffer.BlockCopy(data, readPos * sizeof(short), output, 0, n * sizeof(short));
					// Array.Copy(this.data, readPos, data, 0, n);

					readPos += n;
					return;
				}
			}

			if (rwWrap == Wrap.SameWrap)
			{
				margin = writePos - readPos;
				if (margin > n)
				{
					margin = n;
				}

				Buffer.BlockCopy(data, readPos * sizeof(short), output, (length - n) * sizeof(short), margin * sizeof(short));
				// Array.Copy(this.data, readPos, data, size - n, margin);

				readPos += margin;
			}

			return;
		}

		public void Stuff(int size)
		{
			int margin;

			if (size <= 0 || size > this.size)
			{
				return;
			}

			int n = size;
			if (rwWrap == Wrap.SameWrap)
			{
				margin = readPos;
				if (n > margin)
				{
					rwWrap = Wrap.DiffWrap;
					readPos = this.size - 1;
					n -= margin + 1;
				}
				else
				{
					readPos -= n;
					return;
				}
			}

			if (rwWrap == Wrap.DiffWrap)
			{
				margin = readPos - writePos;
				if (margin > n)
				{
					margin = n;
				}
				readPos -= margin;
			}

			return;
		}
	}

	public class PowerLevel
	{
		public float averagelevel;
		private float framelevel;
		public int frcounter;
		private float frsum;
		public float minlevel;
		public int sfrcounter;
		private float sfrsum;

		public PowerLevel()
		{
			const float bigFloat = 1E17f;
			averagelevel = 0;
			framelevel = 0;
			minlevel = bigFloat;
			frsum = 0;
			sfrsum = 0;
			frcounter = 0;
			sfrcounter = 0;
		}

		public void Update(short[] in_)
		{
			int k;

			for (k = 0; k < AecConfig.PART_LEN; k++)
			{
				sfrsum += in_[k] * in_[k];
			}
			sfrcounter++;

			if (sfrcounter > WebRtcConstants.SubCountLen)
			{
				framelevel = sfrsum / (WebRtcConstants.SubCountLen * AecConfig.PART_LEN);
				sfrsum = 0;
				sfrcounter = 0;

				if (framelevel > 0)
				{
					if (framelevel < minlevel)
					{
						minlevel = framelevel; // New minimum
					}
					else
					{
						minlevel *= (1 + 0.001f); // Small increase
					}
				}
				frcounter++;
				frsum += framelevel;

				if (frcounter > WebRtcConstants.CountLen)
				{
					averagelevel = frsum / WebRtcConstants.CountLen;
					frsum = 0;
					frcounter = 0;
				}
			}
		}
	}

	public class Stats
	{
		public float average;
		public int counter;
		public int hicounter;
		public float himean;
		public float hisum;
		public float instant;
		public float max;
		public float min;
		public float sum;

		public Stats()
		{
			const int offsetLevel = -100;
			instant = offsetLevel;
			average = offsetLevel;
			max = offsetLevel;
			min = offsetLevel * (-1);
			sum = 0;
			hisum = 0;
			himean = offsetLevel;
			counter = 0;
			hicounter = 0;
		}
	}

	public struct Complex
	{
		public float im;
		public float re;
	}

	public enum AecNlpMode
	{
		KAecNlpConservative = 0,
		KAecNlpModerate,
		KAecNlpAggressive
	}

	public class AecConfig
	{
		public AecConfig(int filterLengthInSamples, int samplesPerFrame, int samplesPerSecond)
		{
			NumPartitions = filterLengthInSamples / PART_LEN;
			FilterLength = NumPartitions * PART_LEN; // In case filterLengthInSamples isn't a multiple of PART_LEN.
			FilterLength2 = FilterLength * 2;
			FarBufferLength = (FilterLength2 * 2);
			SamplesPerFrame = samplesPerFrame;
			SamplesPerSecond = samplesPerSecond;
			BufSizeSamp = WebRtcConstants.BUF_SIZE_FRAMES * SamplesPerFrame;
		}

		public readonly int SamplesPerFrame;

		public readonly int SamplesPerSecond;

		public readonly int BufSizeSamp;

		public bool MetricsMode; // default kAecFalse
		public AecNlpMode NlpMode; // default kAecNlpModerate
		public bool SkewMode; // default kAecFalse

		/// <summary>
		/// Filter length in samples
		/// </summary>
		public readonly int FilterLength;

		/// <summary>
		/// Number of partitions
		/// </summary>
		public readonly int NumPartitions; // ks 9/27/11 - originally 12; now configurable.

		/// <summary>
		/// Double filter length
		/// </summary>
		public readonly int FilterLength2;

		public readonly int FarBufferLength;

		/// <summary>
		/// Length of partition
		/// </summary>
		public const int PART_LEN = 64;

		/// <summary>
		/// Unique fft coefficients
		/// </summary>
		public const int PART_LEN1 = (PART_LEN + 1);

		/// <summary>
		/// Length of partition * 2
		/// </summary>
		public const int PART_LEN2 = (PART_LEN * 2);

		public const int PREF_BAND_SIZE = 24;

		internal const int IP_LEN = PART_LEN; // this must be at least ceil(2 + sqrt(PART_LEN))
		internal const int W_LEN = PART_LEN;

		//float realSkew;
	}

	/// <summary>
	/// echo canceller core
	/// </summary>
	public class AecCore
	{

		#region Fields and Properties

		private readonly AecConfig aecConfig;
		private readonly Stats aNlp = new Stats();
		private readonly FFT fft = new FFT(AecConfig.PART_LEN1);
		private readonly float[] dBuf = new float[AecConfig.PART_LEN2]; // nearend

		private readonly float[] dBufH = new float[AecConfig.PART_LEN2]; // nearend

		private readonly float[] dInitMinPow = new float[AecConfig.PART_LEN1];
		private readonly float[] dMinPow = new float[AecConfig.PART_LEN1];
		private readonly float[] dPow = new float[AecConfig.PART_LEN1];
		private readonly float[] eBuf = new float[AecConfig.PART_LEN2]; // error
		private readonly Stats erl = new Stats();
		private readonly Stats erle = new Stats();
		private readonly float errThresh; // error threshold
		private readonly short[] farBuf;
		private readonly PowerLevel farlevel = new PowerLevel();
		private readonly PowerLevel linoutlevel = new PowerLevel();
		private readonly float mu; // stepsize
		public readonly short mult; // sampling frequency multiple
		private readonly PowerLevel nearlevel = new PowerLevel();
		private readonly PowerLevel nlpoutlevel = new PowerLevel();
		private readonly float[] outBuf = new float[AecConfig.PART_LEN];
		// private readonly int sampFreq;
		private readonly Complex[] sde = new Complex[AecConfig.PART_LEN1]; // cross-psd of nearend and error
		private readonly Complex[] sxd = new Complex[AecConfig.PART_LEN1]; // cross-psd of farend and nearend

		/// <summary>
		/// Filter FFT (real)
		/// </summary>
		private readonly float[] wfBuf0;

		/// <summary>
		/// Filter FFT (imaginary)
		/// </summary>
		private readonly float[] wfBuf1;

		/// <summary>
		/// Farend fft buffer (real)
		/// </summary>
		private readonly float[] xfBuf0;

		/// <summary>
		/// Farend fft buffer (imaginary)
		/// </summary>
		private readonly float[] xfBuf1;

		private readonly float[] xBuf = new float[AecConfig.PART_LEN2]; // farend
		private readonly float[] xPow = new float[AecConfig.PART_LEN1];
		private readonly Complex[] xfwBuf; // farend windowed fft buffer
		public float cn_scale_Hband; //scale for comfort noise in H band
		private int delayEstCtr;
		private int delayIdx;
		private short divergeState;
		private short echoState;
		private int farBufReadPos;
		private int farBufWritePos;
		private readonly RingBuffer farFrBuf;
		public int flag_Hband_cn; //for comfort noise
		public int freq_avg_ic; //initial bin for averaging nlp gain

		private float hNlFbLocalMin;
		private float hNlFbMin;
		private int hNlMinCtr;
		private int hNlNewMin;
		private float hNlXdAvgMin;
		public float[] hNs = new float[AecConfig.PART_LEN1];
		private int inSamples;
		private int knownDelay;
		public bool metricsMode;
		public float minOverDrive;
		private readonly RingBuffer nearFrBuf;
		private RingBuffer nearFrBufH;
		private int noiseEstCtr;
		private float[] noisePow; // points to another array
		private readonly RingBuffer outFrBuf;
		private RingBuffer outFrBufH;
		private int outSamples;
		private float overDrive, overDriveSm;
		public Stats rerl = new Stats();
		private readonly float[] sd = new float[AecConfig.PART_LEN1]; // far, near and error psd
		private readonly float[] se = new float[AecConfig.PART_LEN1]; // far, near and error psd
		private int seed;
		private short stNearState;

		private int stateCounter;
		private readonly float[] sx = new float[AecConfig.PART_LEN1]; // far, near and error psd
		public float targetSupp;
		private int xfBufBlockPos;

		#endregion

		#region Readonly Arrays

		private static readonly float[] sqrtHanning = new[]
		{
			0.00000000000000f, 0.02454122852291f, 0.04906767432742f,
			0.07356456359967f, 0.09801714032956f, 0.12241067519922f,
			0.14673047445536f, 0.17096188876030f, 0.19509032201613f,
			0.21910124015687f, 0.24298017990326f, 0.26671275747490f,
			0.29028467725446f, 0.31368174039889f, 0.33688985339222f,
			0.35989503653499f, 0.38268343236509f, 0.40524131400499f,
			0.42755509343028f, 0.44961132965461f, 0.47139673682600f,
			0.49289819222978f, 0.51410274419322f, 0.53499761988710f,
			0.55557023301960f, 0.57580819141785f, 0.59569930449243f,
			0.61523159058063f, 0.63439328416365f, 0.65317284295378f,
			0.67155895484702f, 0.68954054473707f, 0.70710678118655f,
			0.72424708295147f, 0.74095112535496f, 0.75720884650648f,
			0.77301045336274f, 0.78834642762661f, 0.80320753148064f,
			0.81758481315158f, 0.83146961230255f, 0.84485356524971f,
			0.85772861000027f, 0.87008699110871f, 0.88192126434835f,
			0.89322430119552f, 0.90398929312344f, 0.91420975570353f,
			0.92387953251129f, 0.93299279883474f, 0.94154406518302f,
			0.94952818059304f, 0.95694033573221f, 0.96377606579544f,
			0.97003125319454f, 0.97570213003853f, 0.98078528040323f,
			0.98527764238894f, 0.98917650996478f, 0.99247953459871f,
			0.99518472667220f, 0.99729045667869f, 0.99879545620517f,
			0.99969881869620f, 1.00000000000000f
		};

		/* Matlab code to produce table:
weightCurve = [0 ; 0.3 * sqrt(linspace(0,1,64))' + 0.1];
fprintf(1, '\t%.4f, %.4f, %.4f, %.4f, %.4f, %.4f,\n', weightCurve);
*/

		private static readonly float[] weightCurve = new[]
		{
			0.0000f, 0.1000f, 0.1378f, 0.1535f, 0.1655f, 0.1756f,
			0.1845f, 0.1926f, 0.2000f, 0.2069f, 0.2134f, 0.2195f,
			0.2254f, 0.2309f, 0.2363f, 0.2414f, 0.2464f, 0.2512f,
			0.2558f, 0.2604f, 0.2648f, 0.2690f, 0.2732f, 0.2773f,
			0.2813f, 0.2852f, 0.2890f, 0.2927f, 0.2964f, 0.3000f,
			0.3035f, 0.3070f, 0.3104f, 0.3138f, 0.3171f, 0.3204f,
			0.3236f, 0.3268f, 0.3299f, 0.3330f, 0.3360f, 0.3390f,
			0.3420f, 0.3449f, 0.3478f, 0.3507f, 0.3535f, 0.3563f,
			0.3591f, 0.3619f, 0.3646f, 0.3673f, 0.3699f, 0.3726f,
			0.3752f, 0.3777f, 0.3803f, 0.3828f, 0.3854f, 0.3878f,
			0.3903f, 0.3928f, 0.3952f, 0.3976f, 0.4000f
		};

		/* Matlab code to produce table:
		overDriveCurve = [sqrt(linspace(0,1,65))' + 1];
		fprintf(1, '\t%.4f, %.4f, %.4f, %.4f, %.4f, %.4f,\n', overDriveCurve);
		*/

		private static readonly float[] overDriveCurve = new[]
		{
			1.0000f, 1.1250f, 1.1768f, 1.2165f, 1.2500f, 1.2795f,
			1.3062f, 1.3307f, 1.3536f, 1.3750f, 1.3953f, 1.4146f,
			1.4330f, 1.4507f, 1.4677f, 1.4841f, 1.5000f, 1.5154f,
			1.5303f, 1.5449f, 1.5590f, 1.5728f, 1.5863f, 1.5995f,
			1.6124f, 1.6250f, 1.6374f, 1.6495f, 1.6614f, 1.6731f,
			1.6847f, 1.6960f, 1.7071f, 1.7181f, 1.7289f, 1.7395f,
			1.7500f, 1.7603f, 1.7706f, 1.7806f, 1.7906f, 1.8004f,
			1.8101f, 1.8197f, 1.8292f, 1.8385f, 1.8478f, 1.8570f,
			1.8660f, 1.8750f, 1.8839f, 1.8927f, 1.9014f, 1.9100f,
			1.9186f, 1.9270f, 1.9354f, 1.9437f, 1.9520f, 1.9601f,
			1.9682f, 1.9763f, 1.9843f, 1.9922f, 2.0000f
		};
		#endregion

		#region Constructors
		public AecCore(AecConfig aecConfig)
		{
			this.aecConfig = aecConfig;

			xfwBuf = new Complex[aecConfig.NumPartitions * AecConfig.PART_LEN1]; // farend windowed fft buffer
			farBuf = new short[aecConfig.FilterLength2 * 2];

			wfBuf0 = new float[aecConfig.NumPartitions * AecConfig.PART_LEN1];
			wfBuf1 = new float[aecConfig.NumPartitions * AecConfig.PART_LEN1];
			xfBuf0 = new float[aecConfig.NumPartitions * AecConfig.PART_LEN1];
			xfBuf1 = new float[aecConfig.NumPartitions * AecConfig.PART_LEN1];

			farFrBuf = new RingBuffer(aecConfig.SamplesPerFrame + AecConfig.PART_LEN);
			nearFrBuf = new RingBuffer(aecConfig.SamplesPerFrame + AecConfig.PART_LEN);
			outFrBuf = new RingBuffer(aecConfig.SamplesPerFrame + AecConfig.PART_LEN);
			nearFrBufH = new RingBuffer(aecConfig.SamplesPerFrame + AecConfig.PART_LEN);
			outFrBufH = new RingBuffer(aecConfig.SamplesPerFrame + AecConfig.PART_LEN);

			int i;
			if (aecConfig.SamplesPerSecond == 8000)
			{
				mu = 0.6f;
				errThresh = 2e-6f;
			}
			else
			{
				mu = 0.5f;
				errThresh = 1.5e-6f;
			}

			// Default target suppression level
			targetSupp = (float)-11.5;
			minOverDrive = (float)2.0;

			// Sampling frequency multiplier
			// SWB is processed as 160 frame size
			if (aecConfig.SamplesPerSecond == 32000)
			{
				mult = (short)(aecConfig.SamplesPerSecond / 16000);
			}
			else
			{
				// mult = 2;
				mult = (short)(aecConfig.SamplesPerSecond / 8000);
			}

			farBufWritePos = 0;
			farBufReadPos = 0;

			inSamples = 0;
			outSamples = 0;
			knownDelay = 0;

			noisePow = null;
			noiseEstCtr = 0;

			// Initial comfort noise power
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				dMinPow[i] = 1.0e6f;
			}

			// Holds the last block written to
			xfBufBlockPos = 0;


			// To prevent numerical instability in the first block.
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				sd[i] = 1;
			}
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				sx[i] = 1;
			}

			hNlFbMin = 1;
			hNlFbLocalMin = 1;
			hNlXdAvgMin = 1;
			hNlNewMin = 0;
			hNlMinCtr = 0;
			overDrive = 2;
			overDriveSm = 2;
			delayIdx = 0;
			stNearState = 0;
			echoState = 0;
			divergeState = 0;

			seed = 777;
			delayEstCtr = 0;

			// Metrics disabled by default
			metricsMode = false;
			InitMetrics();

			// Assembly optimization
			//WebRtcAec_FilterFar = FilterFar;
			//WebRtcAec_ScaleErrorSignal = ScaleErrorSignal;
			//WebRtcAec_FilterAdaptation = FilterAdaptation;
		}

		public void InitMetrics()
		{
			stateCounter = 0;
		}
		#endregion

		#region Methods

		/// <summary>
		/// Buffer the farend to account for knownDelay
		/// </summary>
		private void BufferFar(short[] farend, int farLen)
		{
			int writeLen = farLen, writePos = 0;

			// Check if the write position must be wrapped.
			while (farBufWritePos + writeLen > aecConfig.FarBufferLength)
			{
				// Write to remaining buffer space before wrapping.
				writeLen = aecConfig.FarBufferLength - farBufWritePos;
				Buffer.BlockCopy(farend, writePos * sizeof(short), farBuf, farBufWritePos * sizeof(short), writeLen * sizeof(short));
				// Array.Copy(farend, writePos, farBuf, farBufWritePos, writeLen);
				farBufWritePos = 0;
				writePos = writeLen;
				writeLen = farLen - writeLen;
			}

			Buffer.BlockCopy(farend, writePos * sizeof(short), farBuf, farBufWritePos * sizeof(short), writeLen * sizeof(short));
			// Array.Copy(farend, writePos, farBuf, farBufWritePos, writeLen);
			farBufWritePos += writeLen;
		}

		private void FetchFar(short[] farend, int farLen, int knownDelay)
		{
			int readLen = farLen, readPos = 0, delayChange = knownDelay - knownDelay;

			farBufReadPos -= delayChange;

			// Check if delay forces a read position wrap.
			while (farBufReadPos < 0)
			{
				farBufReadPos += aecConfig.FarBufferLength;
			}
			while (farBufReadPos > aecConfig.FarBufferLength - 1)
			{
				farBufReadPos -= aecConfig.FarBufferLength;
			}

			this.knownDelay = knownDelay;

			// Check if read position must be wrapped.
			while (farBufReadPos + readLen > aecConfig.FarBufferLength)
			{
				// Read from remaining buffer space before wrapping.
				readLen = aecConfig.FarBufferLength - farBufReadPos;
				Buffer.BlockCopy(farBuf, farBufReadPos * sizeof(short), farend, readPos * sizeof(short), readLen * sizeof(short));
				// Array.Copy(farBuf, farBufReadPos, farend, readPos, readLen);
				farBufReadPos = 0;
				readPos = readLen;
				readLen = farLen - readLen;
			}
			Buffer.BlockCopy(farBuf, farBufReadPos * sizeof(short), farend, readPos * sizeof(short), readLen * sizeof(short));
			// Array.Copy(farBuf, farBufReadPos, farend, readPos, readLen);

			farBufReadPos += readLen;
		}

		public void ProcessFrame(short[] nearend, short[] farend, short[] output, int knownDelay)
		{

			//TODO: I think it should be safe to move these to fields
			short[] farBlock = new short[AecConfig.PART_LEN], nearBlock = new short[AecConfig.PART_LEN], outBlock = new short[AecConfig.PART_LEN];
			// var farFr = new short[aecConfig.SamplesPerFrame];

			// For H band
			// short[] nearBlH = new short[PART_LEN], outBlH = new short[PART_LEN];

			// Buffer the current frame.
			// Fetch an older one corresponding to the delay.
			// BufferFar(farend, aecConfig.SamplesPerFrame);
			// FetchFar(farFr, aecConfig.SamplesPerFrame, knownDelay);

			// Buffer the synchronized far and near frames,
			// to pass the smaller blocks individually.
			farFrBuf.Write(farend, farend.Length);
			nearFrBuf.Write(nearend, nearend.Length);

			// Process as many blocks as possible.
			while (farFrBuf.get_buffer_size() >= AecConfig.PART_LEN)
			{
				farFrBuf.Read(farBlock, AecConfig.PART_LEN);
				nearFrBuf.Read(nearBlock, AecConfig.PART_LEN);

				ProcessBlock(farBlock, nearBlock, outBlock);

				outFrBuf.Write(outBlock, outBlock.Length);
			}

			// Stuff the out buffer if we have less than a frame to output.
			// This should only happen for the first frame.
			int size = outFrBuf.get_buffer_size();
			if (size < aecConfig.SamplesPerFrame)
			{
				outFrBuf.Stuff(aecConfig.SamplesPerFrame - size);
			}

			// Obtain an output frame.
			outFrBuf.Read(output, aecConfig.SamplesPerFrame);
		}

		private void ProcessBlock(short[] farend, short[] nearend, short[] output)
		{
			int i;
			float[] d = new float[AecConfig.PART_LEN], y = new float[AecConfig.PART_LEN], e = new float[AecConfig.PART_LEN]; //, dH = new float[PART_LEN];
			var eInt16 = new short[AecConfig.PART_LEN];

			var fftBuf = new float[AecConfig.PART_LEN2];

			// var xf = new float[2][];
			var xf0 = new float[AecConfig.PART_LEN1];
			var xf1 = new float[AecConfig.PART_LEN1];
			// var yf = new float[2][];
			var yf0 = new float[AecConfig.PART_LEN1];
			var yf1 = new float[AecConfig.PART_LEN1];
			// var ef = new float[2][];
			var ef0 = new float[AecConfig.PART_LEN1];
			var ef1 = new float[AecConfig.PART_LEN1];

			var df = new Complex[AecConfig.PART_LEN1];

			const float gPow0 = 0.9f;
			const float gPow1 = 0.1f;

			// Noise estimate constants.
			int noiseInitBlocks = 500 * mult;
			const float step = 0.1f;
			const float ramp = 1.0002f;
			var gInitNoise = new[] { 0.999f, 0.001f };


			// ---------- Ooura fft ----------
			// Concatenate old and new farend blocks.
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				xBuf[i + AecConfig.PART_LEN] = farend[i];
				d[i] = nearend[i];
			}

			Buffer.BlockCopy(xBuf, 0, fftBuf, 0, AecConfig.PART_LEN2 * sizeof(float));
			Buffer.BlockCopy(d, 0, dBuf, AecConfig.PART_LEN * sizeof(float), AecConfig.PART_LEN * sizeof(float));
			fft.Transform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 02               fft[18] = {0}", fftBuf[18]));

			// Far fft
			xf1[0] = 0;
			xf1[AecConfig.PART_LEN] = 0;
			xf0[0] = fftBuf[0];
			xf0[AecConfig.PART_LEN] = fftBuf[1];

			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				xf0[i] = fftBuf[2 * i];
				xf1[i] = fftBuf[2 * i + 1];
			}

			// Near fft
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 025               dBuf[13] = {0}", dBuf[13])); /////difference for 3rd AEC frame     ////

			Buffer.BlockCopy(dBuf, 0, fftBuf, 0, AecConfig.PART_LEN2 * sizeof(float));

			fft.Transform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);
			df[0].im = 0;
			df[AecConfig.PART_LEN].im = 0;
			df[0].re = fftBuf[0];
			df[AecConfig.PART_LEN].re = fftBuf[1];

			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				df[i].re = fftBuf[2 * i];
				df[i].im = fftBuf[2 * i + 1];
			}

			// Power smoothing
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				xPow[i] = gPow0 * xPow[i] + gPow1 * aecConfig.NumPartitions * (xf0[i] * xf0[i] + xf1[i] * xf1[i]);
				dPow[i] = gPow0 * dPow[i] + gPow1 * (df[i].re * df[i].re + df[i].im * df[i].im);
			}

			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 025               dPow[13] = {0}, xPow[13] = {1}", dPow[13], xPow[13])); /////difference for 3rd AEC frame

			// Estimate noise power. Wait until dPow is more stable.
			if (noiseEstCtr > 50)
			{
				for (i = 0; i < AecConfig.PART_LEN1; i++)
				{
					if (dPow[i] < dMinPow[i])
					{
						dMinPow[i] = (dPow[i] + step * (dMinPow[i] - dPow[i])) * ramp;
					}
					else
					{
						dMinPow[i] *= ramp;
					}
				}
			}

			// Smooth increasing noise power from zero at the start,
			// to avoid a sudden burst of comfort noise.
			if (noiseEstCtr < noiseInitBlocks)
			{
				noiseEstCtr++;
				for (i = 0; i < AecConfig.PART_LEN1; i++)
				{
					if (dMinPow[i] > dInitMinPow[i])
					{
						dInitMinPow[i] = gInitNoise[0] * dInitMinPow[i] + gInitNoise[1] * dMinPow[i];
					}
					else
					{
						dInitMinPow[i] = dMinPow[i];
					}
				}
				noisePow = dInitMinPow;
			}
			else
			{
				noisePow = dMinPow;
			}
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 02               noisePow[13] = {0}", noisePow[13])); /////difference for 3rd AEC frame

			// Update the xfBuf block position.
			xfBufBlockPos--;
			if (xfBufBlockPos == -1)
			{
				xfBufBlockPos = aecConfig.NumPartitions - 1;
			}

			// Buffer xf

			Buffer.BlockCopy(xf0, 0, xfBuf0, xfBufBlockPos * AecConfig.PART_LEN1 * sizeof(float), AecConfig.PART_LEN1 * sizeof(float));
			Buffer.BlockCopy(xf1, 0, xfBuf1, xfBufBlockPos * AecConfig.PART_LEN1 * sizeof(float), AecConfig.PART_LEN1 * sizeof(float));

			// Filter far			
			FilterFar(yf0, yf1);
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 03               yf[0, 30] = {0}", yf0[30]));

			// Inverse fft to obtain echo estimate and error.
			fftBuf[0] = yf0[0];
			fftBuf[1] = yf0[AecConfig.PART_LEN];
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[2 * i] = yf0[i];
				fftBuf[2 * i + 1] = yf1[i];
			}
			fft.ReverseTransform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

			float scale = 2.0f / AecConfig.PART_LEN2;
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				y[i] = fftBuf[AecConfig.PART_LEN + i] * scale; // fft scaling
			}

			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				e[i] = d[i] - y[i];
			}
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 04               e[13] = {0}", e[13])); // diff for aec frame #3

			// Error fft
			Buffer.BlockCopy(e, 0, eBuf, AecConfig.PART_LEN * sizeof(float), AecConfig.PART_LEN * sizeof(float));

			Array.Clear(fftBuf, 0, AecConfig.PART_LEN);
			Buffer.BlockCopy(e, 0, fftBuf, AecConfig.PART_LEN * sizeof(float), AecConfig.PART_LEN * sizeof(float));

			fft.Transform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

			ef1[0] = 0;
			ef1[AecConfig.PART_LEN] = 0;
			ef0[0] = fftBuf[0];
			ef0[AecConfig.PART_LEN] = fftBuf[1];
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				ef0[i] = fftBuf[2 * i];
				ef1[i] = fftBuf[2 * i + 1];
			}

			// Scale error signal inversely with far power.
			ScaleErrorSignal(ef0, ef1);
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 05              ef[0,13] = {0}", ef0[13]));

			// Filter adaptation
			FilterAdaptation(fftBuf, ef0, ef1);

			NonLinearProcessing(output);

			if (metricsMode)
			{
				for (i = 0; i < AecConfig.PART_LEN; i++)
				{
					eInt16[i] = (short)WebRtcUtil.WEBRTC_SPL_SAT(Int16.MaxValue, (int)e[i], Int16.MinValue);
				}

				// Update power levels and echo metrics
				farlevel.Update(farend);
				nearlevel.Update(nearend);
				linoutlevel.Update(eInt16);
				nlpoutlevel.Update(output);
				UpdateMetrics();
			}
		}

		private void ScaleErrorSignal(float[] ef0, float[] ef1)
		{
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 06              xPow[13] = {0}", xPow[13]));
			int i;
			for (i = 0; i < (AecConfig.PART_LEN1); i++)
			{
				ef0[i] /= (xPow[i] + 1e-10f);
				ef1[i] /= (xPow[i] + 1e-10f);
				var absEf = (float)Math.Sqrt(ef0[i] * ef0[i] + ef1[i] * ef1[i]);
				//WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC 07              absEf={0}   ef[0,i] = {1},    ef[1,i]={2}  i={3}   mu={4}", absEf, ef[0, i], ef[1, i], i, this.mu));

				if (absEf > errThresh)
				{
					absEf = errThresh / (absEf + 1e-10f);
					ef0[i] *= absEf;
					ef1[i] *= absEf;
				}

				// Stepsize factor
				ef0[i] *= mu;
				ef1[i] *= mu;
			}
		}

		private static float MulRe(float aRe, float aIm, float bRe, float bIm)
		{
			return aRe * bRe - aIm * bIm;
		}

		private static float MulIm(float aRe, float aIm, float bRe, float bIm)
		{
			return aRe * bIm + aIm * bRe;
		}

		private void FilterFar(float[] yf0, float[] yf1)
		{
			for (int i = 0; i < aecConfig.NumPartitions; i++)
			{
				int j;
				int xPos = (i + xfBufBlockPos) * AecConfig.PART_LEN1;
				int pos = i * AecConfig.PART_LEN1;
				// Check for wrap
				if (i + xfBufBlockPos >= aecConfig.NumPartitions)
				{
					xPos -= aecConfig.NumPartitions * (AecConfig.PART_LEN1);
				}

				for (j = 0; j < AecConfig.PART_LEN1; j++)
				{
					yf0[j] += MulRe(xfBuf0[xPos + j], xfBuf1[xPos + j], wfBuf0[pos + j], wfBuf1[pos + j]);
					yf1[j] += MulIm(xfBuf0[xPos + j], xfBuf1[xPos + j], wfBuf0[pos + j], wfBuf1[pos + j]);
				}
			}
		}

		private void FilterAdaptation(float[] fftBuf, float[] ef0, float[] ef1)
		{
			int i;
			for (i = 0; i < aecConfig.NumPartitions; i++)
			{
				int xPos = (i + xfBufBlockPos) * (AecConfig.PART_LEN1);
				// Check for wrap
				if (i + xfBufBlockPos >= aecConfig.NumPartitions)
				{
					xPos -= aecConfig.NumPartitions * AecConfig.PART_LEN1;
				}

				int pos = i * AecConfig.PART_LEN1;

#if UNCONSTR
	for (j = 0; j < WebRtcConstants.PART_LEN1; j++) {
	  this.wfBuf[pos + j][0] += MulRe(this.xfBuf[xPos + j][0],
									  -this.xfBuf[xPos + j][1],
									  ef[j][0], ef[j][1]);
	  this.wfBuf[pos + j][1] += MulIm(this.xfBuf[xPos + j][0],
									  -this.xfBuf[xPos + j][1],
									  ef[j][0], ef[j][1]);
	}
#else
				int j;
				for (j = 0; j < AecConfig.PART_LEN; j++)
				{
					fftBuf[2 * j] = MulRe(xfBuf0[xPos + j],
									 -xfBuf1[xPos + j],
									 ef0[j], ef1[j]);
					fftBuf[2 * j + 1] = MulIm(xfBuf0[xPos + j],
										 -xfBuf1[xPos + j],
										 ef0[j], ef1[j]);
				}
				fftBuf[1] = MulRe(xfBuf0[xPos + AecConfig.PART_LEN],
							   -xfBuf1[xPos + AecConfig.PART_LEN],
							   ef0[AecConfig.PART_LEN], ef1[AecConfig.PART_LEN]);

				fft.ReverseTransform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

				Array.Clear(fftBuf, AecConfig.PART_LEN, AecConfig.PART_LEN);

				// fft scaling
				{
					const float scale = 2.0f / AecConfig.PART_LEN2;
					for (j = 0; j < AecConfig.PART_LEN; j++)
					{
						fftBuf[j] *= scale;
					}
				}
				fft.Transform(fftBuf, AecConfig.PART_LEN2); // , ip, wfft);

				wfBuf0[pos] += fftBuf[0];
				wfBuf0[pos + AecConfig.PART_LEN] += fftBuf[1];

				for (j = 1; j < AecConfig.PART_LEN; j++)
				{
					wfBuf0[pos + j] += fftBuf[2 * j];
					//WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC FA              wfBuf[0, {0}] = {1}", pos + j, wfBuf[0, pos + j]));

					wfBuf1[pos + j] += fftBuf[2 * j + 1];
					//WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC FA              wfBuf[1, {0}] = {1}", pos + j, wfBuf[1, pos + j]));
				}
#endif
				// UNCONSTR
			}
		}

		private void UpdateMetrics()
		{
			const float actThresholdNoisy = 8.0f;
			const float actThresholdClean = 40.0f;
			const float safety = 0.99995f;
			const float noisyPower = 300000.0f;

			if (echoState != 0)
			{
				// Check if echo is likely present
				stateCounter++;
			}

			if (farlevel.frcounter == WebRtcConstants.CountLen)
			{
				float actThreshold = farlevel.minlevel < noisyPower ? actThresholdClean : actThresholdNoisy;

				if ((stateCounter > (0.5f * WebRtcConstants.CountLen * WebRtcConstants.SubCountLen))
					&& (farlevel.sfrcounter == 0)
					// Estimate in active far-end segments only
					&& (farlevel.averagelevel > (actThreshold * farlevel.minlevel))
					)
				{
					// Subtract noise power
					float echo = nearlevel.averagelevel - safety * nearlevel.minlevel;

					// ERL
					float dtmp = 10 * (float)Math.Log10(farlevel.averagelevel / nearlevel.averagelevel + 1e-10f);
					float dtmp2 = 10 * (float)Math.Log10(farlevel.averagelevel / echo + 1e-10f);

					erl.instant = dtmp;
					if (dtmp > erl.max)
					{
						erl.max = dtmp;
					}

					if (dtmp < erl.min)
					{
						erl.min = dtmp;
					}

					erl.counter++;
					erl.sum += dtmp;
					erl.average = erl.sum / erl.counter;

					// Upper mean
					if (dtmp > erl.average)
					{
						erl.hicounter++;
						erl.hisum += dtmp;
						erl.himean = erl.hisum / erl.hicounter;
					}

					// A_NLP
					dtmp = 10 * (float)Math.Log10(nearlevel.averagelevel / linoutlevel.averagelevel + 1e-10f);

					// subtract noise power
					float suppressedEcho = linoutlevel.averagelevel - safety * linoutlevel.minlevel;

					dtmp2 = 10 * (float)Math.Log10(echo / suppressedEcho + 1e-10f);

					aNlp.instant = dtmp2;
					if (dtmp > aNlp.max)
					{
						aNlp.max = dtmp;
					}

					if (dtmp < aNlp.min)
					{
						aNlp.min = dtmp;
					}

					aNlp.counter++;
					aNlp.sum += dtmp;
					aNlp.average = aNlp.sum / aNlp.counter;

					// Upper mean
					if (dtmp > aNlp.average)
					{
						aNlp.hicounter++;
						aNlp.hisum += dtmp;
						aNlp.himean = aNlp.hisum / aNlp.hicounter;
					}

					// ERLE

					// subtract noise power
					suppressedEcho = nlpoutlevel.averagelevel - safety * nlpoutlevel.minlevel;

					dtmp = 10 * (float)Math.Log10(nearlevel.averagelevel /
												 nlpoutlevel.averagelevel + 1e-10f);
					dtmp2 = 10 * (float)Math.Log10(echo / suppressedEcho + 1e-10f);

					dtmp = dtmp2;
					erle.instant = dtmp;
					if (dtmp > erle.max)
					{
						erle.max = dtmp;
					}

					if (dtmp < erle.min)
					{
						erle.min = dtmp;
					}

					erle.counter++;
					erle.sum += dtmp;
					erle.average = erle.sum / erle.counter;

					// Upper mean
					if (dtmp > erle.average)
					{
						erle.hicounter++;
						erle.hisum += dtmp;
						erle.himean = erle.hisum / erle.hicounter;
					}
				}

				stateCounter = 0;
			}
		}

		private void NonLinearProcessing(short[] output)
		{
			Complex[] dfw = new Complex[AecConfig.PART_LEN1], efw = new Complex[AecConfig.PART_LEN1], xfw = new Complex[AecConfig.PART_LEN1];
			var comfortNoiseHband = new Complex[AecConfig.PART_LEN1];
			var fftBuf = new float[AecConfig.PART_LEN2];
			float dtmp;
			float nlpGainHband;
			int i, j, pos;

			// Coherence and non-linear filter
			float[] cohde = new float[AecConfig.PART_LEN1], cohxd = new float[AecConfig.PART_LEN1];
			var hNl = new float[AecConfig.PART_LEN1];
			var hNlPref = new float[AecConfig.PREF_BAND_SIZE];
			float hNlFb = 0, hNlFbLow = 0;
			const float prefBandQuant = 0.75f, prefBandQuantLow = 0.5f;
			int prefBandSize = AecConfig.PREF_BAND_SIZE / mult;
			int minPrefBand = 4 / mult;

			// Near and error power sums
			float sdSum = 0, seSum = 0;

			// Power estimate smoothing coefficients
			var gCoh = new List<float[]> { new[] { 0.9f, 0.1f }, new[] { 0.93f, 0.07f } };
			var ptrGCoh = gCoh[mult - 1];

			// Filter energey
			int delayEstInterval = 10 * mult;

			delayEstCtr++;
			if (delayEstCtr == delayEstInterval)
			{
				delayEstCtr = 0;
			}

			// initialize comfort noise for H band
			//nlpGainHband = (float)0.0;
			//dtmp = (float)0.0;

			// Measure energy in each filter partition to determine delay.
			// TODO: Spread by computing one partition per block?
			if (delayEstCtr == 0)
			{
				float wfEnMax = 0;
				delayIdx = 0;
				for (i = 0; i < aecConfig.NumPartitions; i++)
				{
					pos = i * AecConfig.PART_LEN1;
					float wfEn = 0;
					for (j = 0; j < AecConfig.PART_LEN1; j++)
					{
						wfEn += wfBuf0[pos + j] * wfBuf0[pos + j] +
								wfBuf1[pos + j] * wfBuf1[pos + j];
					}

					if (wfEn > wfEnMax)
					{
						wfEnMax = wfEn;
						delayIdx = i;
					}
				}
			}

			// NLP
			// Windowed far fft
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[i] = xBuf[i] * sqrtHanning[i];
				fftBuf[AecConfig.PART_LEN + i] = xBuf[AecConfig.PART_LEN + i] * sqrtHanning[AecConfig.PART_LEN - i];
			}
			fft.Transform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

			xfw[0].im = 0;
			xfw[AecConfig.PART_LEN].im = 0;
			xfw[0].re = fftBuf[0];
			xfw[AecConfig.PART_LEN].re = fftBuf[1];
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				xfw[i].re = fftBuf[2 * i];
				xfw[i].im = fftBuf[2 * i + 1];
			}

			// Buffer far.
			Array.Copy(xfw, xfwBuf, xfw.Length);

			// Use delayed far.
			Array.Copy(xfwBuf, delayIdx * AecConfig.PART_LEN1, xfw, 0, xfw.Length);

			// Windowed near fft
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[i] = dBuf[i] * sqrtHanning[i];
				fftBuf[AecConfig.PART_LEN + i] = dBuf[AecConfig.PART_LEN + i] * sqrtHanning[AecConfig.PART_LEN - i];
			}
			fft.Transform(fftBuf, AecConfig.PART_LEN2); // , ip, wfft);

			dfw[0].im = 0;
			dfw[AecConfig.PART_LEN].im = 0;
			dfw[0].re = fftBuf[0];
			dfw[AecConfig.PART_LEN].re = fftBuf[1];
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				dfw[i].re = fftBuf[2 * i];
				dfw[i].im = fftBuf[2 * i + 1];
			}

			// Windowed error fft
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[i] = eBuf[i] * sqrtHanning[i];
				fftBuf[AecConfig.PART_LEN + i] = eBuf[AecConfig.PART_LEN + i] * sqrtHanning[AecConfig.PART_LEN - i];
			}
			fft.Transform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);
			efw[0].im = 0;
			efw[AecConfig.PART_LEN].im = 0;
			efw[0].re = fftBuf[0];
			efw[AecConfig.PART_LEN].re = fftBuf[1];
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				efw[i].re = fftBuf[2 * i];
				efw[i].im = fftBuf[2 * i + 1];
			}
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC NLP             efw[13].re = {0}", efw[13].re));

			// Smoothed PSD
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				sd[i] = ptrGCoh[0] * sd[i] + ptrGCoh[1] *
						(dfw[i].re * dfw[i].re + dfw[i].im * dfw[i].im);
				se[i] = ptrGCoh[0] * se[i] + ptrGCoh[1] *
						(efw[i].re * efw[i].re + efw[i].im * efw[i].im);
				// We threshold here to protect against the ill-effects of a zero farend.
				// The threshold is not arbitrarily chosen, but balances protection and
				// adverse interaction with the algorithm's tuning.
				// TODO: investigate further why this is so sensitive.
				sx[i] = ptrGCoh[0] * sx[i] + ptrGCoh[1] *
						WebRtcUtil.WEBRTC_SPL_MAX((xfw[i].re * xfw[i].re + xfw[i].im * xfw[i].im), 15);

				sde[i].re = ptrGCoh[0] * sde[i].re + ptrGCoh[1] *
							(dfw[i].re * efw[i].re + dfw[i].im * efw[i].im);
				sde[i].im = ptrGCoh[0] * sde[i].im + ptrGCoh[1] *
							(dfw[i].re * efw[i].im - dfw[i].im * efw[i].re);

				sxd[i].re = ptrGCoh[0] * sxd[i].re + ptrGCoh[1] *
							(dfw[i].re * xfw[i].re + dfw[i].im * xfw[i].im);
				sxd[i].im = ptrGCoh[0] * sxd[i].im + ptrGCoh[1] *
							(dfw[i].re * xfw[i].im - dfw[i].im * xfw[i].re);

				sdSum += sd[i];
				seSum += se[i];
			}
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC NLP            sxd[13].im = {0}, sxd[13].re = {1},  sde[13].im = {2}, sde[13].re = {3}", sxd[13].im, sxd[13].re, sde[13].im, sde[13].re));

			// Divergent filter safeguard.
			if (divergeState == 0)
			{
				if (seSum > sdSum)
				{
					divergeState = 1;
				}
			}
			else
			{
				if (seSum * 1.05f < sdSum)
				{
					divergeState = 0;
				}
			}

			if (divergeState == 1)
			{
				Array.Copy(dfw, efw, efw.Length);
			}

			// Reset if error is significantly larger than nearend (13 dB).
			if (seSum > (19.95f * sdSum))
			{
				// Testing shows that Array.Clear() is about 40% faster than a new array on float arrays of 768+ elements,
				// and about 5x as fast as clearing it in a loop.
				Array.Clear(wfBuf0, 0, wfBuf0.Length);
				Array.Clear(wfBuf1, 0, wfBuf1.Length);
			}

			// Subband coherence
			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				cohde[i] = (sde[i].re * sde[i].re + sde[i].im * sde[i].im) /
						   (sd[i] * se[i] + 1e-10f);
				cohxd[i] = (sxd[i].re * sxd[i].re + sxd[i].im * sxd[i].im) /
						   (sx[i] * sd[i] + 1e-10f); //todo:diff for 1st AEC frame   for sx[13]
			}
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC NLP            cohde[13] = {0}, cohxd[13] = {1}, sxd[13].im = {2}, sxd[13].re = {3}", cohde[13], cohxd[13], sxd[13].im, sxd[13].re)); //todo:diff for 1st AEC frame   for cohxd[13]

			float hNlXdAvg = 0;
			for (i = minPrefBand; i < prefBandSize + minPrefBand; i++)
			{
				hNlXdAvg += cohxd[i];
			}
			hNlXdAvg /= prefBandSize;
			hNlXdAvg = 1 - hNlXdAvg;

			WebRtcUtil.WriteDebugMessage(String.Format("(C#) AEC NLP 01             hNlXdAvg = {0}", hNlXdAvg)); //todo:diff for 1st AEC frame

			float hNlDeAvg = 0;
			for (i = minPrefBand; i < prefBandSize + minPrefBand; i++)
			{
				hNlDeAvg += cohde[i];
			}
			hNlDeAvg /= prefBandSize;

			if (hNlXdAvg < 0.75f && hNlXdAvg < hNlXdAvgMin)
			{
				hNlXdAvgMin = hNlXdAvg;
			}

			if (hNlDeAvg > 0.98f && hNlXdAvg > 0.9f)
			{
				stNearState = 1;
			}
			else if (hNlDeAvg < 0.95f || hNlXdAvg < 0.8f)
			{
				stNearState = 0;
			}

			if (hNlXdAvgMin == 1.0f)
			{
				echoState = 0;
				overDrive = minOverDrive;

				if (stNearState == 1)
				{
					Buffer.BlockCopy(cohde, 0, hNl, 0, hNl.Length * sizeof(float));
					hNlFb = hNlDeAvg;
					hNlFbLow = hNlDeAvg;
				}
				else
				{
					for (i = 0; i < AecConfig.PART_LEN1; i++)
					{
						hNl[i] = 1 - cohxd[i];
					}
					hNlFb = hNlXdAvg;
					hNlFbLow = hNlXdAvg;
				}
			}
			else
			{
				if (stNearState == 1)
				{
					echoState = 0;
					Buffer.BlockCopy(cohde, 0, hNl, 0, hNl.Length * sizeof(float));
					hNlFb = hNlDeAvg;
					hNlFbLow = hNlDeAvg;
				}
				else
				{
					echoState = 1;
					for (i = 0; i < AecConfig.PART_LEN1; i++)
					{
						hNl[i] = WebRtcUtil.WEBRTC_SPL_MIN(cohde[i], 1 - cohxd[i]);
					}

					// Select an order statistic from the preferred bands.
					// TODO: Using quicksort now, but a selection algorithm may be preferred.
					Buffer.BlockCopy(hNl, minPrefBand * sizeof(float), hNlPref, 0, prefBandSize * sizeof(float));
					Array.Sort(hNlPref, 0, prefBandSize);

					hNlFb = hNlPref[(int)Math.Floor(prefBandQuant * (prefBandSize - 1))];
					hNlFbLow = hNlPref[(int)Math.Floor(prefBandQuantLow * (prefBandSize - 1))];
				}
			}

			// Track the local filter minimum to determine suppression overdrive.
			if (hNlFbLow < 0.6f && hNlFbLow < hNlFbLocalMin)
			{
				hNlFbLocalMin = hNlFbLow;
				hNlFbMin = hNlFbLow;
				hNlNewMin = 1;
				hNlMinCtr = 0;
			}
			hNlFbLocalMin = WebRtcUtil.WEBRTC_SPL_MIN(hNlFbLocalMin + 0.0008f / mult, 1);
			hNlXdAvgMin = WebRtcUtil.WEBRTC_SPL_MIN(hNlXdAvgMin + 0.0006f / mult, 1);

			if (hNlNewMin == 1)
			{
				hNlMinCtr++;
			}
			if (hNlMinCtr == 2)
			{
				hNlNewMin = 0;
				hNlMinCtr = 0;
				overDrive = WebRtcUtil.WEBRTC_SPL_MAX(targetSupp / ((float)Math.Log(hNlFbMin + 1e-10f) + 1e-10f), minOverDrive);
			}

			// Smooth the overdrive.
			if (overDrive < overDriveSm)
			{
				overDriveSm = 0.99f * overDriveSm + 0.01f * overDrive;
			}
			else
			{
				overDriveSm = 0.9f * overDriveSm + 0.1f * overDrive;
			}

			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				// Weight subbands
				if (hNl[i] > hNlFb)
				{
					hNl[i] = weightCurve[i] * hNlFb + (1 - weightCurve[i]) * hNl[i];
				}

				hNl[i] = (float)Math.Pow(hNl[i], overDriveSm * overDriveCurve[i]);

				// Suppress error signal
				efw[i].re *= hNl[i];
				efw[i].im *= hNl[i];

				// Ooura fft returns incorrect sign on imaginary component.
				// It matters here because we are making an additive change with comfort noise.
				efw[i].im *= -1;
			}

			// Add comfort noise.
			ComfortNoise(efw, comfortNoiseHband, noisePow, hNl);

			// Inverse error fft.
			fftBuf[0] = efw[0].re;
			fftBuf[1] = efw[AecConfig.PART_LEN].re;
			for (i = 1; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[2 * i] = efw[i].re;
				// Sign change required by Ooura fft.
				fftBuf[2 * i + 1] = -efw[i].im;
			}
			fft.ReverseTransform(fftBuf, AecConfig.PART_LEN2); //, ip, wfft);

			// Overlap and add to obtain output.
			const float scale = 2.0f / AecConfig.PART_LEN2;
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				fftBuf[i] *= scale; // fft scaling
				fftBuf[i] = fftBuf[i] * sqrtHanning[i] + outBuf[i];

				// Saturation protection
				output[i] = (short)WebRtcUtil.WEBRTC_SPL_SAT(Int16.MaxValue, (int)fftBuf[i], Int16.MinValue);

				fftBuf[AecConfig.PART_LEN + i] *= scale; // fft scaling
				outBuf[i] = fftBuf[AecConfig.PART_LEN + i] * sqrtHanning[AecConfig.PART_LEN - i];
			}

			// For H band
			//if (this.sampFreq == 32000) {

			//    // H band gain
			//    // average nlp over low band: average over second half of freq spectrum
			//    // (4->8khz)
			//    GetHighbandGain(hNl, &nlpGainHband);

			//    // Inverse comfort_noise
			//    if (flagHbandCn == 1) {
			//        fft[0] = comfortNoiseHband[0][0];
			//        fft[1] = comfortNoiseHband[PART_LEN][0];
			//        for (i = 1; i < PART_LEN; i++) {
			//            fft[2*i] = comfortNoiseHband[i][0];
			//            fft[2*i + 1] = comfortNoiseHband[i][1];
			//        }
			//        rdft(WebRtcConstants.PART_LEN2, -1, fft, ip, wfft);
			//        scale = 2.0f / WebRtcConstants.PART_LEN2;
			//    }

			//    // compute gain factor
			//    for (i = 0; i < PART_LEN; i++) {
			//        dtmp = (float)this.dBufH[i];
			//        dtmp = (float)dtmp * nlpGainHband; // for variable gain

			//        // add some comfort noise where Hband is attenuated
			//        if (flagHbandCn == 1) {
			//            fft[i] *= scale; // fft scaling
			//            dtmp += cnScaleHband * fft[i];
			//        }

			//        // Saturation protection
			//        outputH[i] = (short)WebRtcUtil.WEBRTC_SPL_SAT(WEBRTC_SPL_WORD16_MAX, dtmp,
			//            WebRtcUtil.WEBRTC_SPL_WORD16_MIN);
			//     }
			//}

			// Copy the current block to the old position.
			Buffer.BlockCopy(xBuf, AecConfig.PART_LEN * sizeof(float), xBuf, 0, AecConfig.PART_LEN * sizeof(float));
			Buffer.BlockCopy(dBuf, AecConfig.PART_LEN * sizeof(float), dBuf, 0, AecConfig.PART_LEN * sizeof(float));
			Buffer.BlockCopy(eBuf, AecConfig.PART_LEN * sizeof(float), eBuf, 0, AecConfig.PART_LEN * sizeof(float));

			// Copy the current block to the old position for H band
			if (aecConfig.SamplesPerSecond == 32000)
			{
				Buffer.BlockCopy(dBufH, AecConfig.PART_LEN * sizeof(float), dBufH, 0, AecConfig.PART_LEN * sizeof(float));
			}

			Array.Copy(xfwBuf, 0, xfwBuf, AecConfig.PART_LEN1, xfwBuf.Length - AecConfig.PART_LEN1);
		}

		private static readonly Random rnd = new Random();
		private static void WebRtcSpl_RandUArray(short[] vector, short vectorLength)
		{
			for (int i = 0; i < vectorLength; i++)
			{
				vector[i] = (short)rnd.Next(Int16.MinValue, Int16.MaxValue);
			}
		}

		private static void ComfortNoise(Complex[] efw, Complex[] comfortNoiseHband, float[] noisePow, float[] lambda)
		{
			int i, num;
			var rand = new float[AecConfig.PART_LEN];
			float noiseAvg, tmp, tmpAvg;
			var randW16 = new short[AecConfig.PART_LEN];
			var u = new Complex[AecConfig.PART_LEN1];

			const float pi2 = 6.28318530717959f;

			// Generate a uniform random array on [0 1]
			WebRtcSpl_RandUArray(randW16, AecConfig.PART_LEN);
			for (i = 0; i < AecConfig.PART_LEN; i++)
			{
				rand[i] = ((float)randW16[i]) / 32768;
			}

			// Reject LF noise
			u[0].re = 0;
			u[0].im = 0;

			for (i = 1; i < AecConfig.PART_LEN1; i++)
			{
				tmp = pi2 * rand[i - 1];

				var noise = (float)Math.Sqrt(noisePow[i]);
				u[i].re = noise * (float)Math.Cos(tmp);
				u[i].im = -noise * (float)Math.Sin(tmp);
			}
			u[AecConfig.PART_LEN].im = 0;

			for (i = 0; i < AecConfig.PART_LEN1; i++)
			{
				// This is the proper weighting to match the background noise power
				tmp = (float)Math.Sqrt(WebRtcUtil.WEBRTC_SPL_MAX(1 - lambda[i] * lambda[i], 0));
				//tmp = 1 - lambda[i];
				efw[i].re += tmp * u[i].re;
				efw[i].im += tmp * u[i].im;
			}

			// For H band comfort noise
			// TODO: don't compute noise and "tmp" twice. Use the previous results.
			// noiseAvg = 0.0f;
			// tmpAvg = 0.0f;
			// num = 0;
			//if (aec->sampFreq == 32000 && flagHbandCn == 1) {

			//    // average noise scale
			//    // average over second half of freq spectrum (i.e., 4->8khz)
			//    // TODO: we shouldn't need num. We know how many elements we're summing.
			//    for (i = WebRtcConstants.PART_LEN1 >> 1; i < WebRtcConstants.PART_LEN1; i++) {
			//        num++;
			//        noiseAvg += sqrtf(noisePow[i]);
			//    }
			//    noiseAvg /= (float)num;

			//    // average nlp scale
			//    // average over second half of freq spectrum (i.e., 4->8khz)
			//    // TODO: we shouldn't need num. We know how many elements we're summing.
			//    num = 0;
			//    for (i = WebRtcConstants.PART_LEN1 >> 1; i < WebRtcConstants.PART_LEN1; i++) {
			//        num++;
			//        tmpAvg += sqrtf(WebRtcUtil.WEBRTC_SPL_MAX(1 - lambda[i] * lambda[i], 0));
			//    }
			//    tmpAvg /= (float)num;

			//    // Use average noise for H band
			//    // TODO: we should probably have a new random vector here.
			//    // Reject LF noise
			//    u[0][0] = 0;
			//    u[0][1] = 0;
			//    for (i = 1; i < WebRtcConstants.PART_LEN1; i++) {
			//        tmp = pi2 * rand[i - 1];

			//        // Use average noise for H band
			//        u[i][0] = noiseAvg * (float)cos(tmp);
			//        u[i][1] = -noiseAvg * (float)sin(tmp);
			//    }
			//    u[PART_LEN][1] = 0;

			//    for (i = 0; i < WebRtcConstants.PART_LEN1; i++) {
			//        // Use average NLP weight for H band
			//        comfortNoiseHband[i][0] = tmpAvg * u[i][0];
			//        comfortNoiseHband[i][1] = tmpAvg * u[i][1];
			//    }
			//}
		}

		#endregion
	}


}
