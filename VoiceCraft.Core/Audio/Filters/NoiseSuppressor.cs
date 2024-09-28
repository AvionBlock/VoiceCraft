using System;

namespace VoiceCraft.Core.Audio.Filters
{
	public class NoiseSuppressor
	{
		#region Constants

		private const int BLOCKL_MAX = 160; // max processing block length: 160
		private const int ANAL_BLOCKL_MAX = 256; // max analysis block length: 256
		private const int HALF_ANAL_BLOCKL = 129; // half max analysis block length + 1

		private const float QUANTILE = (float)0.25;

		private const int SIMULT = 3;
		private const int END_STARTUP_LONG = 200;
		private const int END_STARTUP_SHORT = 50;
		private const float FACTOR = (float)40.0;
		private const float WIDTH = (float)0.01;

		private const float SMOOTH = (float)0.75; // filter smoothing
		// Length of fft work arrays.
		private const int IP_LENGTH = (ANAL_BLOCKL_MAX >> 1); // must be at least ceil(2 + sqrt(ANAL_BLOCKL_MAX/2))
		private const int W_LENGTH = (ANAL_BLOCKL_MAX >> 1);

		//PARAMETERS FOR NEW METHOD
		private const float DD_PR_SNR = (float)0.98; // DD update of prior SNR
		private const float LRT_TAVG = (float)0.50; // tavg parameter for LRT (previously 0.90)
		private const float SPECT_FL_TAVG = (float)0.30; // tavg parameter for spectral flatness measure
		private const float SPECT_DIFF_TAVG = (float)0.30; // tavg parameter for spectral difference measure
		private const float PRIOR_UPDATE = (float)0.10; // update parameter of prior model
		private const float NOISE_UPDATE = (float)0.90; // update parameter for noise
		private const float SPEECH_UPDATE = (float)0.99; // update parameter when likely speech
		private const float WIDTH_PR_MAP = (float)4.0; // width parameter in sigmoid map for prior model
		private const float LRT_FEATURE_THR = (float)0.5; // default threshold for LRT feature
		private const float SF_FEATURE_THR = (float)0.5; // default threshold for Spectral Flatness feature
		private const float SD_FEATURE_THR = (float)0.5; // default threshold for Spectral Difference feature
		private const float PROB_RANGE = (float)0.20; // probability threshold for noise state in
		// speech/noise likelihood
		private const int HIST_PAR_EST = 1000; // histogram size for estimation of parameters
		private const float GAMMA_PAUSE = (float)0.05; // update for conservative noise estimate
		//
		private const float B_LIM = (float)0.5; // threshold in final energy gain factor calculation

		// hybrib Hanning & flat window
		private static readonly float[] kBlocks80w128 = {
		                                                	(float) 0.00000000, (float) 0.03271908, (float) 0.06540313, (float) 0.09801714, (float) 0.13052619,
		                                                	(float) 0.16289547, (float) 0.19509032, (float) 0.22707626, (float) 0.25881905, (float) 0.29028468,
		                                                	(float) 0.32143947, (float) 0.35225005, (float) 0.38268343, (float) 0.41270703, (float) 0.44228869,
		                                                	(float) 0.47139674, (float) 0.50000000, (float) 0.52806785, (float) 0.55557023, (float) 0.58247770,
		                                                	(float) 0.60876143, (float) 0.63439328, (float) 0.65934582, (float) 0.68359230, (float) 0.70710678,
		                                                	(float) 0.72986407, (float) 0.75183981, (float) 0.77301045, (float) 0.79335334, (float) 0.81284668,
		                                                	(float) 0.83146961, (float) 0.84920218, (float) 0.86602540, (float) 0.88192126, (float) 0.89687274,
		                                                	(float) 0.91086382, (float) 0.92387953, (float) 0.93590593, (float) 0.94693013, (float) 0.95694034,
		                                                	(float) 0.96592583, (float) 0.97387698, (float) 0.98078528, (float) 0.98664333, (float) 0.99144486,
		                                                	(float) 0.99518473, (float) 0.99785892, (float) 0.99946459, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
		                                                	(float) 1.00000000, (float) 0.99946459, (float) 0.99785892, (float) 0.99518473, (float) 0.99144486,
		                                                	(float) 0.98664333, (float) 0.98078528, (float) 0.97387698, (float) 0.96592583, (float) 0.95694034,
		                                                	(float) 0.94693013, (float) 0.93590593, (float) 0.92387953, (float) 0.91086382, (float) 0.89687274,
		                                                	(float) 0.88192126, (float) 0.86602540, (float) 0.84920218, (float) 0.83146961, (float) 0.81284668,
		                                                	(float) 0.79335334, (float) 0.77301045, (float) 0.75183981, (float) 0.72986407, (float) 0.70710678,
		                                                	(float) 0.68359230, (float) 0.65934582, (float) 0.63439328, (float) 0.60876143, (float) 0.58247770,
		                                                	(float) 0.55557023, (float) 0.52806785, (float) 0.50000000, (float) 0.47139674, (float) 0.44228869,
		                                                	(float) 0.41270703, (float) 0.38268343, (float) 0.35225005, (float) 0.32143947, (float) 0.29028468,
		                                                	(float) 0.25881905, (float) 0.22707626, (float) 0.19509032, (float) 0.16289547, (float) 0.13052619,
		                                                	(float) 0.09801714, (float) 0.06540313, (float) 0.03271908
		                                                };

		// hybrib Hanning & flat window
		private static readonly float[] kBlocks160w256 = new[]
		{
			(float) 0.00000000, (float) 0.01636173, (float) 0.03271908, (float) 0.04906767, (float) 0.06540313,
			(float) 0.08172107, (float) 0.09801714, (float) 0.11428696, (float) 0.13052619, (float) 0.14673047,
			(float) 0.16289547, (float) 0.17901686, (float) 0.19509032, (float) 0.21111155, (float) 0.22707626,
			(float) 0.24298018, (float) 0.25881905, (float) 0.27458862, (float) 0.29028468, (float) 0.30590302,
			(float) 0.32143947, (float) 0.33688985, (float) 0.35225005, (float) 0.36751594, (float) 0.38268343,
			(float) 0.39774847, (float) 0.41270703, (float) 0.42755509, (float) 0.44228869, (float) 0.45690388,
			(float) 0.47139674, (float) 0.48576339, (float) 0.50000000, (float) 0.51410274, (float) 0.52806785,
			(float) 0.54189158, (float) 0.55557023, (float) 0.56910015, (float) 0.58247770, (float) 0.59569930,
			(float) 0.60876143, (float) 0.62166057, (float) 0.63439328, (float) 0.64695615, (float) 0.65934582,
			(float) 0.67155895, (float) 0.68359230, (float) 0.69544264, (float) 0.70710678, (float) 0.71858162,
			(float) 0.72986407, (float) 0.74095113, (float) 0.75183981, (float) 0.76252720, (float) 0.77301045,
			(float) 0.78328675, (float) 0.79335334, (float) 0.80320753, (float) 0.81284668, (float) 0.82226822,
			(float) 0.83146961, (float) 0.84044840, (float) 0.84920218, (float) 0.85772861, (float) 0.86602540,
			(float) 0.87409034, (float) 0.88192126, (float) 0.88951608, (float) 0.89687274, (float) 0.90398929,
			(float) 0.91086382, (float) 0.91749450, (float) 0.92387953, (float) 0.93001722, (float) 0.93590593,
			(float) 0.94154407, (float) 0.94693013, (float) 0.95206268, (float) 0.95694034, (float) 0.96156180,
			(float) 0.96592583, (float) 0.97003125, (float) 0.97387698, (float) 0.97746197, (float) 0.98078528,
			(float) 0.98384601, (float) 0.98664333, (float) 0.98917651, (float) 0.99144486, (float) 0.99344778,
			(float) 0.99518473, (float) 0.99665524, (float) 0.99785892, (float) 0.99879546, (float) 0.99946459,
			(float) 0.99986614, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 0.99986614, (float) 0.99946459, (float) 0.99879546, (float) 0.99785892,
			(float) 0.99665524, (float) 0.99518473, (float) 0.99344778, (float) 0.99144486, (float) 0.98917651,
			(float) 0.98664333, (float) 0.98384601, (float) 0.98078528, (float) 0.97746197, (float) 0.97387698,
			(float) 0.97003125, (float) 0.96592583, (float) 0.96156180, (float) 0.95694034, (float) 0.95206268,
			(float) 0.94693013, (float) 0.94154407, (float) 0.93590593, (float) 0.93001722, (float) 0.92387953,
			(float) 0.91749450, (float) 0.91086382, (float) 0.90398929, (float) 0.89687274, (float) 0.88951608,
			(float) 0.88192126, (float) 0.87409034, (float) 0.86602540, (float) 0.85772861, (float) 0.84920218,
			(float) 0.84044840, (float) 0.83146961, (float) 0.82226822, (float) 0.81284668, (float) 0.80320753,
			(float) 0.79335334, (float) 0.78328675, (float) 0.77301045, (float) 0.76252720, (float) 0.75183981,
			(float) 0.74095113, (float) 0.72986407, (float) 0.71858162, (float) 0.70710678, (float) 0.69544264,
			(float) 0.68359230, (float) 0.67155895, (float) 0.65934582, (float) 0.64695615, (float) 0.63439328,
			(float) 0.62166057, (float) 0.60876143, (float) 0.59569930, (float) 0.58247770, (float) 0.56910015,
			(float) 0.55557023, (float) 0.54189158, (float) 0.52806785, (float) 0.51410274, (float) 0.50000000,
			(float) 0.48576339, (float) 0.47139674, (float) 0.45690388, (float) 0.44228869, (float) 0.42755509,
			(float) 0.41270703, (float) 0.39774847, (float) 0.38268343, (float) 0.36751594, (float) 0.35225005,
			(float) 0.33688985, (float) 0.32143947, (float) 0.30590302, (float) 0.29028468, (float) 0.27458862,
			(float) 0.25881905, (float) 0.24298018, (float) 0.22707626, (float) 0.21111155, (float) 0.19509032,
			(float) 0.17901686, (float) 0.16289547, (float) 0.14673047, (float) 0.13052619, (float) 0.11428696,
			(float) 0.09801714, (float) 0.08172107, (float) 0.06540313, (float) 0.04906767, (float) 0.03271908,
			(float) 0.01636173
		};

		// hybrib Hanning & flat window: for 20ms
		private static float[] kBlocks320w512 = new[]
		{
			(float) 0.00000000, (float) 0.00818114, (float) 0.01636173, (float) 0.02454123, (float) 0.03271908,
			(float) 0.04089475, (float) 0.04906767, (float) 0.05723732, (float) 0.06540313, (float) 0.07356456,
			(float) 0.08172107, (float) 0.08987211, (float) 0.09801714, (float) 0.10615561, (float) 0.11428696,
			(float) 0.12241068, (float) 0.13052619, (float) 0.13863297, (float) 0.14673047, (float) 0.15481816,
			(float) 0.16289547, (float) 0.17096189, (float) 0.17901686, (float) 0.18705985, (float) 0.19509032,
			(float) 0.20310773, (float) 0.21111155, (float) 0.21910124, (float) 0.22707626, (float) 0.23503609,
			(float) 0.24298018, (float) 0.25090801, (float) 0.25881905, (float) 0.26671276, (float) 0.27458862,
			(float) 0.28244610, (float) 0.29028468, (float) 0.29810383, (float) 0.30590302, (float) 0.31368174,
			(float) 0.32143947, (float) 0.32917568, (float) 0.33688985, (float) 0.34458148, (float) 0.35225005,
			(float) 0.35989504, (float) 0.36751594, (float) 0.37511224, (float) 0.38268343, (float) 0.39022901,
			(float) 0.39774847, (float) 0.40524131, (float) 0.41270703, (float) 0.42014512, (float) 0.42755509,
			(float) 0.43493645, (float) 0.44228869, (float) 0.44961133, (float) 0.45690388, (float) 0.46416584,
			(float) 0.47139674, (float) 0.47859608, (float) 0.48576339, (float) 0.49289819, (float) 0.50000000,
			(float) 0.50706834, (float) 0.51410274, (float) 0.52110274, (float) 0.52806785, (float) 0.53499762,
			(float) 0.54189158, (float) 0.54874927, (float) 0.55557023, (float) 0.56235401, (float) 0.56910015,
			(float) 0.57580819, (float) 0.58247770, (float) 0.58910822, (float) 0.59569930, (float) 0.60225052,
			(float) 0.60876143, (float) 0.61523159, (float) 0.62166057, (float) 0.62804795, (float) 0.63439328,
			(float) 0.64069616, (float) 0.64695615, (float) 0.65317284, (float) 0.65934582, (float) 0.66547466,
			(float) 0.67155895, (float) 0.67759830, (float) 0.68359230, (float) 0.68954054, (float) 0.69544264,
			(float) 0.70129818, (float) 0.70710678, (float) 0.71286806, (float) 0.71858162, (float) 0.72424708,
			(float) 0.72986407, (float) 0.73543221, (float) 0.74095113, (float) 0.74642045, (float) 0.75183981,
			(float) 0.75720885, (float) 0.76252720, (float) 0.76779452, (float) 0.77301045, (float) 0.77817464,
			(float) 0.78328675, (float) 0.78834643, (float) 0.79335334, (float) 0.79830715, (float) 0.80320753,
			(float) 0.80805415, (float) 0.81284668, (float) 0.81758481, (float) 0.82226822, (float) 0.82689659,
			(float) 0.83146961, (float) 0.83598698, (float) 0.84044840, (float) 0.84485357, (float) 0.84920218,
			(float) 0.85349396, (float) 0.85772861, (float) 0.86190585, (float) 0.86602540, (float) 0.87008699,
			(float) 0.87409034, (float) 0.87803519, (float) 0.88192126, (float) 0.88574831, (float) 0.88951608,
			(float) 0.89322430, (float) 0.89687274, (float) 0.90046115, (float) 0.90398929, (float) 0.90745693,
			(float) 0.91086382, (float) 0.91420976, (float) 0.91749450, (float) 0.92071783, (float) 0.92387953,
			(float) 0.92697940, (float) 0.93001722, (float) 0.93299280, (float) 0.93590593, (float) 0.93875641,
			(float) 0.94154407, (float) 0.94426870, (float) 0.94693013, (float) 0.94952818, (float) 0.95206268,
			(float) 0.95453345, (float) 0.95694034, (float) 0.95928317, (float) 0.96156180, (float) 0.96377607,
			(float) 0.96592583, (float) 0.96801094, (float) 0.97003125, (float) 0.97198664, (float) 0.97387698,
			(float) 0.97570213, (float) 0.97746197, (float) 0.97915640, (float) 0.98078528, (float) 0.98234852,
			(float) 0.98384601, (float) 0.98527764, (float) 0.98664333, (float) 0.98794298, (float) 0.98917651,
			(float) 0.99034383, (float) 0.99144486, (float) 0.99247953, (float) 0.99344778, (float) 0.99434953,
			(float) 0.99518473, (float) 0.99595331, (float) 0.99665524, (float) 0.99729046, (float) 0.99785892,
			(float) 0.99836060, (float) 0.99879546, (float) 0.99916346, (float) 0.99946459, (float) 0.99969882,
			(float) 0.99986614, (float) 0.99996653, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000, (float) 1.00000000,
			(float) 1.00000000, (float) 0.99996653, (float) 0.99986614, (float) 0.99969882, (float) 0.99946459,
			(float) 0.99916346, (float) 0.99879546, (float) 0.99836060, (float) 0.99785892, (float) 0.99729046,
			(float) 0.99665524, (float) 0.99595331, (float) 0.99518473, (float) 0.99434953, (float) 0.99344778,
			(float) 0.99247953, (float) 0.99144486, (float) 0.99034383, (float) 0.98917651, (float) 0.98794298,
			(float) 0.98664333, (float) 0.98527764, (float) 0.98384601, (float) 0.98234852, (float) 0.98078528,
			(float) 0.97915640, (float) 0.97746197, (float) 0.97570213, (float) 0.97387698, (float) 0.97198664,
			(float) 0.97003125, (float) 0.96801094, (float) 0.96592583, (float) 0.96377607, (float) 0.96156180,
			(float) 0.95928317, (float) 0.95694034, (float) 0.95453345, (float) 0.95206268, (float) 0.94952818,
			(float) 0.94693013, (float) 0.94426870, (float) 0.94154407, (float) 0.93875641, (float) 0.93590593,
			(float) 0.93299280, (float) 0.93001722, (float) 0.92697940, (float) 0.92387953, (float) 0.92071783,
			(float) 0.91749450, (float) 0.91420976, (float) 0.91086382, (float) 0.90745693, (float) 0.90398929,
			(float) 0.90046115, (float) 0.89687274, (float) 0.89322430, (float) 0.88951608, (float) 0.88574831,
			(float) 0.88192126, (float) 0.87803519, (float) 0.87409034, (float) 0.87008699, (float) 0.86602540,
			(float) 0.86190585, (float) 0.85772861, (float) 0.85349396, (float) 0.84920218, (float) 0.84485357,
			(float) 0.84044840, (float) 0.83598698, (float) 0.83146961, (float) 0.82689659, (float) 0.82226822,
			(float) 0.81758481, (float) 0.81284668, (float) 0.80805415, (float) 0.80320753, (float) 0.79830715,
			(float) 0.79335334, (float) 0.78834643, (float) 0.78328675, (float) 0.77817464, (float) 0.77301045,
			(float) 0.76779452, (float) 0.76252720, (float) 0.75720885, (float) 0.75183981, (float) 0.74642045,
			(float) 0.74095113, (float) 0.73543221, (float) 0.72986407, (float) 0.72424708, (float) 0.71858162,
			(float) 0.71286806, (float) 0.70710678, (float) 0.70129818, (float) 0.69544264, (float) 0.68954054,
			(float) 0.68359230, (float) 0.67759830, (float) 0.67155895, (float) 0.66547466, (float) 0.65934582,
			(float) 0.65317284, (float) 0.64695615, (float) 0.64069616, (float) 0.63439328, (float) 0.62804795,
			(float) 0.62166057, (float) 0.61523159, (float) 0.60876143, (float) 0.60225052, (float) 0.59569930,
			(float) 0.58910822, (float) 0.58247770, (float) 0.57580819, (float) 0.56910015, (float) 0.56235401,
			(float) 0.55557023, (float) 0.54874927, (float) 0.54189158, (float) 0.53499762, (float) 0.52806785,
			(float) 0.52110274, (float) 0.51410274, (float) 0.50706834, (float) 0.50000000, (float) 0.49289819,
			(float) 0.48576339, (float) 0.47859608, (float) 0.47139674, (float) 0.46416584, (float) 0.45690388,
			(float) 0.44961133, (float) 0.44228869, (float) 0.43493645, (float) 0.42755509, (float) 0.42014512,
			(float) 0.41270703, (float) 0.40524131, (float) 0.39774847, (float) 0.39022901, (float) 0.38268343,
			(float) 0.37511224, (float) 0.36751594, (float) 0.35989504, (float) 0.35225005, (float) 0.34458148,
			(float) 0.33688985, (float) 0.32917568, (float) 0.32143947, (float) 0.31368174, (float) 0.30590302,
			(float) 0.29810383, (float) 0.29028468, (float) 0.28244610, (float) 0.27458862, (float) 0.26671276,
			(float) 0.25881905, (float) 0.25090801, (float) 0.24298018, (float) 0.23503609, (float) 0.22707626,
			(float) 0.21910124, (float) 0.21111155, (float) 0.20310773, (float) 0.19509032, (float) 0.18705985,
			(float) 0.17901686, (float) 0.17096189, (float) 0.16289547, (float) 0.15481816, (float) 0.14673047,
			(float) 0.13863297, (float) 0.13052619, (float) 0.12241068, (float) 0.11428696, (float) 0.10615561,
			(float) 0.09801714, (float) 0.08987211, (float) 0.08172107, (float) 0.07356456, (float) 0.06540313,
			(float) 0.05723732, (float) 0.04906767, (float) 0.04089475, (float) 0.03271908, (float) 0.02454123,
			(float) 0.01636173, (float) 0.00818114
		};

		#endregion

		#region Fields and Properties

		private readonly FFT fft;
		private readonly int anaLen;
		private readonly int blockLen;
		private readonly int blockLen10Ms;
		private readonly int[] counter = new int[SIMULT];
		private readonly float[] dataBuf = new float[ANAL_BLOCKL_MAX];
		// parameters for quantile noise estimation
		private readonly float[] density = new float[SIMULT * HALF_ANAL_BLOCKL];
		private readonly float[] featureData = new float[7]; //data for features
		private readonly NsParaExtract featureExtractionParams = new NsParaExtract(); //parameters for feature extraction
		//histograms for parameter estimation
		private readonly int[] histLrt = new int[HIST_PAR_EST];
		private readonly int[] histSpecDiff = new int[HIST_PAR_EST];
		private readonly int[] histSpecFlat = new int[HIST_PAR_EST];
		private readonly int initFlag;
		private readonly float[] initMagnEst = new float[HALF_ANAL_BLOCKL]; //initial magnitude spectrum estimate
		private readonly float[] logLrtTimeAvg = new float[HALF_ANAL_BLOCKL]; //log lrt factor with time-smoothing
		private readonly float[] lquantile = new float[SIMULT * HALF_ANAL_BLOCKL];
		private readonly float[] magnAvgPause = new float[HALF_ANAL_BLOCKL]; //conservative noise spectrum estimate
		private readonly int magnLen;
		private readonly float[] magnPrev = new float[HALF_ANAL_BLOCKL]; //magnitude spectrum of previous frame
		private readonly int[] modelUpdatePars = new int[4]; //parameters for updating or estimating
		private readonly float[] noisePrev = new float[HALF_ANAL_BLOCKL]; //noise spectrum from previous frame
		private readonly float[] outBuf = new float[3 * BLOCKL_MAX];
		private readonly float[] priorModelPars = new float[7]; //parameters for prior model
		private readonly float[] quantile = new float[HALF_ANAL_BLOCKL];
		// parameters for Wiener filter
		private readonly float[] smooth = new float[HALF_ANAL_BLOCKL];
		private readonly float[] speechProbHb = new float[HALF_ANAL_BLOCKL]; //final speech/noise prob: prior + LRT
		private readonly float[] syntBuf = new float[ANAL_BLOCKL_MAX];
		private readonly int windShift;
		private readonly float[] window;
		private int aggrMode;

		// parameters for new method: some not needed, will reduce/cleanup later
		private int blockInd; //frame index counter
		private float[] dataBufHb = new float[ANAL_BLOCKL_MAX]; //buffering data for HB
		private float denoiseBound;
		private uint fs;
		private int gainmap;
		private int outLen;
		private float overdrive;
		private float pinkNoiseExp; //pink noise parameter: power of freq
		private float pinkNoiseNumerator; //pink noise parameter: numerator
		private float priorSpeechProb; //prior speech/noise probability
		private float signalEnergy; //energy of magn
		private float sumMagn; //sum of magn
		private int updates;
		private float whiteNoiseLevel; //initial noise estimate

		#endregion

		public NoiseSuppressor(AudioFormat audioFormat)
		{
			int i;
			//Important: we only support 10ms frames.

			// Initialization of struct
			if (audioFormat.SamplesPerSecond == 8000 || audioFormat.SamplesPerSecond == 16000 || audioFormat.SamplesPerSecond == 32000)
			{
				fs = (uint)audioFormat.SamplesPerSecond;
			}
			else
			{
				throw new ArgumentException("Unsupported audio format");
			}
			windShift = 0;
			switch (fs)
			{
				case 8000:
					blockLen = 80;
					blockLen10Ms = 80;
					anaLen = 128;
					window = kBlocks80w128;
					outLen = 0;
					break;
				case 16000:
					blockLen = 160;
					blockLen10Ms = 160;
					anaLen = 256;
					window = kBlocks160w256;
					outLen = 0;
					break;
				case 32000:
					blockLen = 160;
					blockLen10Ms = 160;
					anaLen = 256;
					window = kBlocks160w256;
					outLen = 0;
					break;
			}
			magnLen = anaLen / 2 + 1; // Number of frequency bins

			fft = new FFT(anaLen);

			// Initialize fft work arrays.
			// ip[0] = 0; // Setting this triggers initialization.

			fft.Transform(dataBuf, anaLen); //, ip, wfft);

			//for quantile noise estimation
			for (i = 0; i < SIMULT * HALF_ANAL_BLOCKL; i++)
			{
				lquantile[i] = (float)8.0;
				density[i] = (float)0.3;
			}

			for (i = 0; i < SIMULT; i++)
			{
				counter[i] = (int)Math.Floor((END_STARTUP_LONG * (i + 1)) / (float)SIMULT);
			}

			updates = 0;

			// Wiener filter initialization
			for (i = 0; i < HALF_ANAL_BLOCKL; i++)
			{
				smooth[i] = (float)1.0;
			}

			// Set the aggressiveness: default
			aggrMode = 0;

			//initialize variables for new method
			priorSpeechProb = (float)0.5; //prior prob for speech/noise
			for (i = 0; i < HALF_ANAL_BLOCKL; i++)
			{
				magnPrev[i] = (float)0.0; //previous mag spectrum
				noisePrev[i] = (float)0.0; //previous noise-spectrum
				logLrtTimeAvg[i] = LRT_FEATURE_THR; //smooth LR ratio (same as threshold)
				magnAvgPause[i] = (float)0.0; //conservative noise spectrum estimate
				speechProbHb[i] = (float)0.0; //for estimation of HB in second pass
				initMagnEst[i] = (float)0.0; //initial average mag spectrum
			}

			//feature quantities
			featureData[0] = SF_FEATURE_THR; //spectral flatness (start on threshold)
			featureData[1] = (float)0.0; //spectral entropy: not used in this version
			featureData[2] = (float)0.0; //spectral variance: not used in this version
			featureData[3] = LRT_FEATURE_THR; //average lrt factor (start on threshold)
			featureData[4] = SF_FEATURE_THR; //spectral template diff (start on threshold)
			featureData[5] = (float)0.0; //normalization for spectral-diff
			featureData[6] = (float)0.0; //window time-average of input magnitude spectrum

			//histogram quantities: used to estimate/update thresholds for features
			for (i = 0; i < HIST_PAR_EST; i++)
			{
				histLrt[i] = 0;
				histSpecFlat[i] = 0;
				histSpecDiff[i] = 0;
			}

			blockInd = -1; //frame counter
			priorModelPars[0] = LRT_FEATURE_THR; //default threshold for lrt feature
			priorModelPars[1] = (float)0.5; //threshold for spectral flatness:
			// determined on-line
			priorModelPars[2] = (float)1.0; //sgn_map par for spectral measure:
			// 1 for flatness measure
			priorModelPars[3] = (float)0.5; //threshold for template-difference feature:
			// determined on-line
			priorModelPars[4] = (float)1.0; //default weighting parameter for lrt feature
			priorModelPars[5] = (float)0.0; //default weighting parameter for
			// spectral flatness feature
			priorModelPars[6] = (float)0.0; //default weighting parameter for
			// spectral difference feature

			modelUpdatePars[0] = 2; //update flag for parameters:
			// 0 no update, 1=update once, 2=update every window
			modelUpdatePars[1] = 500; //window for update
			modelUpdatePars[2] = 0; //counter for update of conservative noise spectrum
			//counter if the feature thresholds are updated during the sequence
			modelUpdatePars[3] = modelUpdatePars[1];

			signalEnergy = 0.0f;
			sumMagn = 0.0f;
			whiteNoiseLevel = 0.0f;
			pinkNoiseNumerator = 0.0f;
			pinkNoiseExp = 0.0f;

			WebRtcNs_set_feature_extraction_parameters(); // Set feature configuration

			//default mode
			WebRtcNs_set_policy_core(0);

			initFlag = 1;
		}

		private void WebRtcNs_set_policy_core(int mode)
		{
			// allow for modes:0,1,2,3
			if (mode < 0 || mode > 3)
			{
				throw new ArgumentException("The mode is not supported");
			}

			aggrMode = mode;
			switch (mode)
			{
				case 0:
					overdrive = (float)1.0;
					denoiseBound = (float)0.5;
					gainmap = 0;
					break;
				case 1:
					overdrive = (float)1.0;
					denoiseBound = (float)0.25;
					gainmap = 1;
					break;
				case 2:
					overdrive = (float)1.1;
					denoiseBound = (float)0.125;
					gainmap = 1;
					break;
				case 3:
					overdrive = (float)1.25;
					denoiseBound = (float)0.09;
					gainmap = 1;
					break;
			}
		}

		// Set Feature Extraction Parameters
		private void WebRtcNs_set_feature_extraction_parameters()
		{
			//bin size of histogram
			featureExtractionParams.binSizeLrt = (float)0.1;
			featureExtractionParams.binSizeSpecFlat = (float)0.05;
			featureExtractionParams.binSizeSpecDiff = (float)0.1;

			//range of histogram over which lrt threshold is computed
			featureExtractionParams.rangeAvgHistLrt = (float)1.0;

			//scale parameters: multiply dominant peaks of the histograms by scale factor to obtain
			// thresholds for prior model
			featureExtractionParams.factor1ModelPars = (float)1.20; //for lrt and spectral diff
			featureExtractionParams.factor2ModelPars = (float)0.9; //for spectral_flatness:
			// used when noise is flatter than speech

			//peak limit for spectral flatness (varies between 0 and 1)
			featureExtractionParams.thresPosSpecFlat = (float)0.6;

			//limit on spacing of two highest peaks in histogram: spacing determined by bin size
			featureExtractionParams.limitPeakSpacingSpecFlat = 2 * featureExtractionParams.binSizeSpecFlat;
			featureExtractionParams.limitPeakSpacingSpecDiff = 2 * featureExtractionParams.binSizeSpecDiff;

			//limit on relevance of second peak:
			featureExtractionParams.limitPeakWeightsSpecFlat = (float)0.5;
			featureExtractionParams.limitPeakWeightsSpecDiff = (float)0.5;

			// fluctuation limit of lrt feature
			featureExtractionParams.thresFluctLrt = (float)0.05;

			//limit on the max and min values for the feature thresholds
			featureExtractionParams.maxLrt = (float)1.0;
			featureExtractionParams.minLrt = (float)0.20;

			featureExtractionParams.maxSpecFlat = (float)0.95;
			featureExtractionParams.minSpecFlat = (float)0.10;

			featureExtractionParams.maxSpecDiff = (float)1.0;
			featureExtractionParams.minSpecDiff = (float)0.16;

			//criteria of weight of histogram peak  to accept/reject feature
			featureExtractionParams.thresWeightSpecFlat = (int)(0.3 * (modelUpdatePars[1])); //for spectral flatness
			featureExtractionParams.thresWeightSpecDiff = (int)(0.3 * (modelUpdatePars[1])); //for spectral difference
		}


		private void WebRtcNs_ComputeSpectralFlatness(float[] magnIn)
		{
			int i;
			const int shiftLp = 1; //option to remove first bin(s) from spectral measures

			// comute spectral measures
			// for flatness
			float avgSpectralFlatnessNum = 0.0f;
			float avgSpectralFlatnessDen = sumMagn;
			for (i = 0; i < shiftLp; i++)
			{
				avgSpectralFlatnessDen -= magnIn[i];
			}
			// compute log of ratio of the geometric to arithmetic mean: check for log(0) case
			for (i = shiftLp; i < magnLen; i++)
			{
				if (magnIn[i] > 0.0)
				{
					avgSpectralFlatnessNum += (float)Math.Log(magnIn[i]);
				}
				else
				{
					featureData[0] -= SPECT_FL_TAVG * featureData[0];
					return;
				}
			}
			//normalize
			avgSpectralFlatnessDen = avgSpectralFlatnessDen / magnLen;
			avgSpectralFlatnessNum = avgSpectralFlatnessNum / magnLen;

			//ratio and inverse log: check for case of log(0)
			float spectralTmp = (float)Math.Exp(avgSpectralFlatnessNum) / avgSpectralFlatnessDen;

			//time-avg update of spectral flatness feature
			featureData[0] += SPECT_FL_TAVG * (spectralTmp - featureData[0]);
			// done with flatness feature
		}


		// Estimate noise
		private void WebRtcNs_NoiseEstimation(float[] magn, float[] noise)
		{
			int i, s, offset = 0;
			var lmagn = new float[HALF_ANAL_BLOCKL];

			if (updates < END_STARTUP_LONG)
			{
				updates++;
			}

			for (i = 0; i < magnLen; i++)
			{
				lmagn[i] = (float)Math.Log(magn[i]);
			}

			// loop over simultaneous estimates
			for (s = 0; s < SIMULT; s++)
			{
				offset = s * magnLen;

				// newquantest(...)
				for (i = 0; i < magnLen; i++)
				{
					// compute delta
					float delta;
					if (density[offset + i] > 1.0)
					{
						delta = FACTOR * (float)1.0 / density[offset + i];
					}
					else
					{
						delta = FACTOR;
					}

					// update log quantile estimate
					if (lmagn[i] > lquantile[offset + i])
					{
						lquantile[offset + i] += QUANTILE * delta
												 / (counter[s] + 1);
					}
					else
					{
						lquantile[offset + i] -= ((float)1.0 - QUANTILE) * delta
												 / (counter[s] + 1);
					}

					// update density estimate
					if (Math.Abs(lmagn[i] - lquantile[offset + i]) < WIDTH)
					{
						density[offset + i] = (counter[s] * density[offset
																  + i] + (float)1.0 / ((float)2.0 * WIDTH)) / (counter[s]
																										   + 1);
					}
				} // end loop over magnitude spectrum

				if (counter[s] >= END_STARTUP_LONG)
				{
					counter[s] = 0;
					if (updates >= END_STARTUP_LONG)
					{
						for (i = 0; i < magnLen; i++)
						{
							quantile[i] = (float)Math.Exp(lquantile[offset + i]);
						}
					}
				}

				counter[s]++;
			} // end loop over simultaneous estimates

			// Sequentially update the noise during startup
			if (updates < END_STARTUP_LONG)
			{
				// Use the last "s" to get noise during startup that differ from zero.
				for (i = 0; i < magnLen; i++)
				{
					quantile[i] = (float)Math.Exp(lquantile[offset + i]);
				}
			}

			Buffer.BlockCopy(quantile, 0, noise, 0, magnLen* sizeof(float));
		}


		private void WebRtcNs_ComputeSpectralDifference(float[] magnIn)
		{
			// avgDiffNormMagn = var(magnIn) - cov(magnIn, magnAvgPause)^2 / var(magnAvgPause)
			int i;

			float avgPause = 0.0f;
			float avgMagn = sumMagn;
			// compute average quantities
			for (i = 0; i < magnLen; i++)
			{
				//conservative smooth noise spectrum from pause frames
				avgPause += magnAvgPause[i];
			}
			avgPause = avgPause / (magnLen);
			avgMagn = avgMagn / (magnLen);

			float covMagnPause = 0.0f;
			float varPause = 0.0f;
			float varMagn = 0.0f;
			// compute variance and covariance quantities
			for (i = 0; i < magnLen; i++)
			{
				covMagnPause += (magnIn[i] - avgMagn) * (magnAvgPause[i] - avgPause);
				varPause += (magnAvgPause[i] - avgPause) * (magnAvgPause[i] - avgPause);
				varMagn += (magnIn[i] - avgMagn) * (magnIn[i] - avgMagn);
			}
			covMagnPause = covMagnPause / (magnLen);
			varPause = varPause / (magnLen);
			varMagn = varMagn / (magnLen);
			// update of average magnitude spectrum
			featureData[6] += signalEnergy;

			float avgDiffNormMagn = varMagn - (covMagnPause * covMagnPause) / (varPause + (float)0.0001);
			// normalize and compute time-avg update of difference feature
			avgDiffNormMagn = (avgDiffNormMagn / (featureData[5] + (float)0.0001));
			featureData[4] += SPECT_DIFF_TAVG * (avgDiffNormMagn - featureData[4]);
		}


		// Extract thresholds for feature parameters
		// histograms are computed over some window_size (given by inst->modelUpdatePars[1])
		// thresholds and weights are extracted every window
		// flag 0 means update histogram only, flag 1 means compute the thresholds/weights
		// threshold and weights are returned in: inst->priorModelPars
		private void WebRtcNs_FeatureParameterExtraction(int flag)
		{
			int i;

			//3 features: lrt, flatness, difference
			//lrt_feature = inst->featureData[3];
			//flat_feature = inst->featureData[0];
			//diff_feature = inst->featureData[4];

			//update histograms
			if (flag == 0)
			{
				// LRT
				if ((featureData[3] < HIST_PAR_EST * featureExtractionParams.binSizeLrt)
					&& (featureData[3] >= 0.0))
				{
					i = (int)(featureData[3] / featureExtractionParams.binSizeLrt);
					histLrt[i]++;
				}
				// Spectral flatness
				if ((featureData[0] < HIST_PAR_EST
					 * featureExtractionParams.binSizeSpecFlat)
					&& (featureData[0] >= 0.0))
				{
					i = (int)(featureData[0] / featureExtractionParams.binSizeSpecFlat);
					histSpecFlat[i]++;
				}
				// Spectral difference
				if ((featureData[4] < HIST_PAR_EST
					 * featureExtractionParams.binSizeSpecDiff)
					&& (featureData[4] >= 0.0))
				{
					i = (int)(featureData[4] / featureExtractionParams.binSizeSpecDiff);
					histSpecDiff[i]++;
				}
			}

			// extract parameters for speech/noise probability
			if (flag == 1)
			{
				//lrt feature: compute the average over this.featureExtractionParams.rangeAvgHistLrt
				float avgHistLrt = 0.0f;
				float avgHistLrtCompl = 0.0f;
				float avgSquareHistLrt = 0.0f;
				int numHistLrt = 0;
				float binMid;
				for (i = 0; i < HIST_PAR_EST; i++)
				{
					binMid = (i + (float)0.5) * featureExtractionParams.binSizeLrt;
					if (binMid <= featureExtractionParams.rangeAvgHistLrt)
					{
						avgHistLrt += histLrt[i] * binMid;
						numHistLrt += histLrt[i];
					}
					avgSquareHistLrt += histLrt[i] * binMid * binMid;
					avgHistLrtCompl += histLrt[i] * binMid;
				}
				if (numHistLrt > 0)
				{
					avgHistLrt = avgHistLrt / (numHistLrt);
				}
				avgHistLrtCompl = avgHistLrtCompl / (modelUpdatePars[1]);
				avgSquareHistLrt = avgSquareHistLrt / (modelUpdatePars[1]);
				float fluctLrt = avgSquareHistLrt - avgHistLrt * avgHistLrtCompl;
				// get threshold for lrt feature:
				if (fluctLrt < featureExtractionParams.thresFluctLrt)
				{
					//very low fluct, so likely noise
					priorModelPars[0] = featureExtractionParams.maxLrt;
				}
				else
				{
					priorModelPars[0] = featureExtractionParams.factor1ModelPars
										* avgHistLrt;
					// check if value is within min/max range
					if (priorModelPars[0] < featureExtractionParams.minLrt)
					{
						priorModelPars[0] = featureExtractionParams.minLrt;
					}
					if (priorModelPars[0] > featureExtractionParams.maxLrt)
					{
						priorModelPars[0] = featureExtractionParams.maxLrt;
					}
				}
				// done with lrt feature

				//
				// for spectral flatness and spectral difference: compute the main peaks of histogram
				int maxPeak1 = 0;
				int maxPeak2 = 0;
				float posPeak1SpecFlat = 0.0f;
				float posPeak2SpecFlat = 0.0f;
				int weightPeak1SpecFlat = 0;
				int weightPeak2SpecFlat = 0;

				// peaks for flatness
				for (i = 0; i < HIST_PAR_EST; i++)
				{
					binMid = (i + (float)0.5) * featureExtractionParams.binSizeSpecFlat;
					if (histSpecFlat[i] > maxPeak1)
					{
						// Found new "first" peak
						maxPeak2 = maxPeak1;
						weightPeak2SpecFlat = weightPeak1SpecFlat;
						posPeak2SpecFlat = posPeak1SpecFlat;

						maxPeak1 = histSpecFlat[i];
						weightPeak1SpecFlat = histSpecFlat[i];
						posPeak1SpecFlat = binMid;
					}
					else if (histSpecFlat[i] > maxPeak2)
					{
						// Found new "second" peak
						maxPeak2 = histSpecFlat[i];
						weightPeak2SpecFlat = histSpecFlat[i];
						posPeak2SpecFlat = binMid;
					}
				}

				//compute two peaks for spectral difference
				maxPeak1 = 0;
				maxPeak2 = 0;
				float posPeak1SpecDiff = 0.0f;
				float posPeak2SpecDiff = 0.0f;
				int weightPeak1SpecDiff = 0;
				int weightPeak2SpecDiff = 0;
				// peaks for spectral difference
				for (i = 0; i < HIST_PAR_EST; i++)
				{
					binMid = (i + (float)0.5) * featureExtractionParams.binSizeSpecDiff;
					if (histSpecDiff[i] > maxPeak1)
					{
						// Found new "first" peak
						maxPeak2 = maxPeak1;
						weightPeak2SpecDiff = weightPeak1SpecDiff;
						posPeak2SpecDiff = posPeak1SpecDiff;

						maxPeak1 = histSpecDiff[i];
						weightPeak1SpecDiff = histSpecDiff[i];
						posPeak1SpecDiff = binMid;
					}
					else if (histSpecDiff[i] > maxPeak2)
					{
						// Found new "second" peak
						maxPeak2 = histSpecDiff[i];
						weightPeak2SpecDiff = histSpecDiff[i];
						posPeak2SpecDiff = binMid;
					}
				}

				// for spectrum flatness feature
				int useFeatureSpecFlat = 1;
				// merge the two peaks if they are close
				if ((Math.Abs(posPeak2SpecFlat - posPeak1SpecFlat)
					 < featureExtractionParams.limitPeakSpacingSpecFlat)
					&& (weightPeak2SpecFlat
						> featureExtractionParams.limitPeakWeightsSpecFlat
						* weightPeak1SpecFlat))
				{
					weightPeak1SpecFlat += weightPeak2SpecFlat;
					posPeak1SpecFlat = (float)0.5 * (posPeak1SpecFlat + posPeak2SpecFlat);
				}
				//reject if weight of peaks is not large enough, or peak value too small
				if (weightPeak1SpecFlat < featureExtractionParams.thresWeightSpecFlat
					|| posPeak1SpecFlat < featureExtractionParams.thresPosSpecFlat)
				{
					useFeatureSpecFlat = 0;
				}
				// if selected, get the threshold
				if (useFeatureSpecFlat == 1)
				{
					// compute the threshold
					priorModelPars[1] = featureExtractionParams.factor2ModelPars
										* posPeak1SpecFlat;
					//check if value is within min/max range
					if (priorModelPars[1] < featureExtractionParams.minSpecFlat)
					{
						priorModelPars[1] = featureExtractionParams.minSpecFlat;
					}
					if (priorModelPars[1] > featureExtractionParams.maxSpecFlat)
					{
						priorModelPars[1] = featureExtractionParams.maxSpecFlat;
					}
				}
				// done with flatness feature

				// for template feature
				int useFeatureSpecDiff = 1;
				// merge the two peaks if they are close
				if ((Math.Abs(posPeak2SpecDiff - posPeak1SpecDiff)
					 < featureExtractionParams.limitPeakSpacingSpecDiff)
					&& (weightPeak2SpecDiff
						> featureExtractionParams.limitPeakWeightsSpecDiff
						* weightPeak1SpecDiff))
				{
					weightPeak1SpecDiff += weightPeak2SpecDiff;
					posPeak1SpecDiff = (float)0.5 * (posPeak1SpecDiff + posPeak2SpecDiff);
				}
				// get the threshold value
				priorModelPars[3] = featureExtractionParams.factor1ModelPars
									* posPeak1SpecDiff;
				//reject if weight of peaks is not large enough
				if (weightPeak1SpecDiff < featureExtractionParams.thresWeightSpecDiff)
				{
					useFeatureSpecDiff = 0;
				}
				//check if value is within min/max range
				if (priorModelPars[3] < featureExtractionParams.minSpecDiff)
				{
					priorModelPars[3] = featureExtractionParams.minSpecDiff;
				}
				if (priorModelPars[3] > featureExtractionParams.maxSpecDiff)
				{
					priorModelPars[3] = featureExtractionParams.maxSpecDiff;
				}
				// done with spectral difference feature

				// don't use template feature if fluctuation of lrt feature is very low:
				//  most likely just noise state
				if (fluctLrt < featureExtractionParams.thresFluctLrt)
				{
					useFeatureSpecDiff = 0;
				}

				// select the weights between the features
				// this.priorModelPars[4] is weight for lrt: always selected
				// this.priorModelPars[5] is weight for spectral flatness
				// this.priorModelPars[6] is weight for spectral difference
				float featureSum = (1 + useFeatureSpecFlat + useFeatureSpecDiff);
				priorModelPars[4] = (float)1.0 / featureSum;
				priorModelPars[5] = (useFeatureSpecFlat) / featureSum;
				priorModelPars[6] = (useFeatureSpecDiff) / featureSum;

				// set hists to zero for next update
				if (modelUpdatePars[0] >= 1)
				{
					// Testing shows that with arrays of 1000 elements, Array.Clear() is ~2x faster than new[].
					Array.Clear(histLrt, 0, HIST_PAR_EST);
					Array.Clear(histSpecFlat, 0, HIST_PAR_EST);
					Array.Clear(histSpecDiff, 0, HIST_PAR_EST);

					//histLrt = new int[HIST_PAR_EST];
					//histSpecFlat = new int[HIST_PAR_EST];
					//histSpecDiff = new int[HIST_PAR_EST];

					//for (i = 0; i < HIST_PAR_EST; i++)
					//{
					//    histLrt[i] = 0;
					//    histSpecFlat[i] = 0;
					//    histSpecDiff[i] = 0;
					//}
				}
			} // end of flag == 1
		}


		// Compute speech/noise probability
		// speech/noise probability is returned in: probSpeechFinal
		//magn is the input magnitude spectrum
		//noise is the noise spectrum
		//snrLocPrior is the prior snr for each freq.
		//snr loc_post is the post snr for each freq.
		private void WebRtcNs_SpeechNoiseProb(float[] probSpeechFinal, float[] snrLocPrior, float[] snrLocPost)
		{
			int i, sgnMap;
			float invLrt, gainPrior, indPrior;
			float logLrtTimeAvgKsum, besselTmp;
			float indicator0, indicator1, indicator2;
			float tmpFloat1, tmpFloat2;
			float weightIndPrior0, weightIndPrior1, weightIndPrior2;
			float threshPrior0, threshPrior1, threshPrior2;
			float widthPrior, widthPrior0, widthPrior1, widthPrior2;

			widthPrior0 = WIDTH_PR_MAP;
			widthPrior1 = (float)2.0 * WIDTH_PR_MAP; //width for pause region:
			// lower range, so increase width in tanh map
			widthPrior2 = (float)2.0 * WIDTH_PR_MAP; //for spectral-difference measure

			//threshold parameters for features
			threshPrior0 = priorModelPars[0];
			threshPrior1 = priorModelPars[1];
			threshPrior2 = priorModelPars[3];

			//sign for flatness feature
			sgnMap = (int)(priorModelPars[2]);

			//weight parameters for features
			weightIndPrior0 = priorModelPars[4];
			weightIndPrior1 = priorModelPars[5];
			weightIndPrior2 = priorModelPars[6];

			// compute feature based on average LR factor
			// this is the average over all frequencies of the smooth log lrt
			logLrtTimeAvgKsum = 0.0f;
			for (i = 0; i < magnLen; i++)
			{
				tmpFloat1 = (float)1.0 + (float)2.0 * snrLocPrior[i];
				tmpFloat2 = (float)2.0 * snrLocPrior[i] / (tmpFloat1 + (float)0.0001);
				besselTmp = (snrLocPost[i] + (float)1.0) * tmpFloat2;
				logLrtTimeAvg[i] += LRT_TAVG * (besselTmp - (float)Math.Log(tmpFloat1)
											  - logLrtTimeAvg[i]);
				logLrtTimeAvgKsum += logLrtTimeAvg[i];
			}
			logLrtTimeAvgKsum = logLrtTimeAvgKsum / (magnLen);
			featureData[3] = logLrtTimeAvgKsum;
			// done with computation of LR factor

			//
			//compute the indicator functions
			//

			// average lrt feature
			widthPrior = widthPrior0;
			//use larger width in tanh map for pause regions
			if (logLrtTimeAvgKsum < threshPrior0)
			{
				widthPrior = widthPrior1;
			}
			// compute indicator function: sigmoid map
			indicator0 = (float)0.5 * ((float)Math.Tanh(widthPrior * (logLrtTimeAvgKsum - threshPrior0))
									  + (float)1.0);

			//spectral flatness feature
			tmpFloat1 = featureData[0];
			widthPrior = widthPrior0;
			//use larger width in tanh map for pause regions
			if (sgnMap == 1 && (tmpFloat1 > threshPrior1))
			{
				widthPrior = widthPrior1;
			}
			if (sgnMap == -1 && (tmpFloat1 < threshPrior1))
			{
				widthPrior = widthPrior1;
			}
			// compute indicator function: sigmoid map
			indicator1 = (float)0.5 * ((float)Math.Tanh(
				sgnMap * widthPrior * (threshPrior1
								   - tmpFloat1)) + (float)1.0);

			//for template spectrum-difference
			tmpFloat1 = featureData[4];
			widthPrior = widthPrior0;
			//use larger width in tanh map for pause regions
			if (tmpFloat1 < threshPrior2)
			{
				widthPrior = widthPrior2;
			}
			// compute indicator function: sigmoid map
			indicator2 = (float)0.5 * ((float)Math.Tanh(widthPrior * (tmpFloat1 - threshPrior2))
									  + (float)1.0);

			//combine the indicator function with the feature weights
			indPrior = weightIndPrior0 * indicator0 + weightIndPrior1 * indicator1 + weightIndPrior2
					   * indicator2;
			// done with computing indicator function

			//compute the prior probability
			priorSpeechProb += PRIOR_UPDATE * (indPrior - priorSpeechProb);
			// make sure probabilities are within range: keep floor to 0.01
			if (priorSpeechProb > 1.0)
			{
				priorSpeechProb = (float)1.0;
			}
			if (priorSpeechProb < 0.01)
			{
				priorSpeechProb = (float)0.01;
			}

			//final speech probability: combine prior model with LR factor:
			gainPrior = ((float)1.0 - priorSpeechProb) / (priorSpeechProb + (float)0.0001);
			for (i = 0; i < magnLen; i++)
			{
				invLrt = (float)Math.Exp(-logLrtTimeAvg[i]);
				invLrt = gainPrior * invLrt;
				probSpeechFinal[i] = (float)1.0 / ((float)1.0 + invLrt);
			}
		}


		/// <summary>
		/// main routine for noise reduction
		/// </summary>
		public void ProcessFrame(short[] inFrame, int inOffset, short[] outFrame, int outOffset)
		{
			// main routine for noise reduction

			int flagHB = 0;
			int i, j;
			const int kStartBand = 5; // Skip first frequency bins during estimation.
			int updateParsFlag;

			float energy2, gain, factor, factor1, factor2;
			float signalEnergy, sumMagn;
			float snrPrior, currentEstimateStsa;
			float tmpFloat1, tmpFloat2, tmpFloat3, probSpeech, probNonSpeech;
			float gammaNoiseTmp, gammaNoiseOld;
			float noiseUpdateTmp, fTmp, dTmp;
			float[] fin = new float[BLOCKL_MAX], fout = new float[BLOCKL_MAX];
			var winData = new float[ANAL_BLOCKL_MAX];
			float[] magn = new float[HALF_ANAL_BLOCKL], noise = new float[HALF_ANAL_BLOCKL];
			float[] theFilter = new float[HALF_ANAL_BLOCKL], theFilterTmp = new float[HALF_ANAL_BLOCKL];
			float[] snrLocPost = new float[HALF_ANAL_BLOCKL], snrLocPrior = new float[HALF_ANAL_BLOCKL];
			float[] probSpeechFinal = new float[HALF_ANAL_BLOCKL], previousEstimateStsa = new float[HALF_ANAL_BLOCKL];
			float[] real = new float[ANAL_BLOCKL_MAX], imag = new float[HALF_ANAL_BLOCKL];
			// Variables during startup
			float sum_log_i = 0.0f;
			float sum_log_i_square = 0.0f;
			float sum_log_magn = 0.0f;
			float sum_log_i_log_magn = 0.0f;

			// SWB variables
			int deltaBweHB = 1;
			int deltaGainHB = 1;
			float decayBweHB = 1.0f;
			float gainMapParHB = 1.0f;
			float gainTimeDomainHB = 1.0f;
			float avgProbSpeechHB, avgProbSpeechHBTmp, avgFilterGainHB, gainModHB;

			// Check that initiation has been done
			if (initFlag != 1)
			{
				throw new InvalidOperationException("Class is not initialized");
			}

			//// Check for valid pointers based on sampling rate
			//if (this.fs == 32000)
			//{
			//    if (speechFrameHB == NULL)
			//    {
			//        return -1;
			//    }
			//    flagHB = 1;
			//    // range for averaging low band quantities for H band gain
			//    deltaBweHB = (int)this.magnLen / 4;
			//    deltaGainHB = deltaBweHB;
			//}
			//
			blockInd++;
			WebRtcUtil.WriteDebugMessage(String.Format("(C#) NS entry                 blockInd = {0} inFrame[143] = {1}", blockInd, inFrame[143]));
			//
			updateParsFlag = modelUpdatePars[0];
			//

			//for LB do all processing
			// convert to float
			for (i = 0; i < blockLen10Ms; i++)
			{
				fin[i] = inFrame[i + inOffset];
			}
			// update analysis buffer for L band
			Buffer.BlockCopy(dataBuf, blockLen10Ms * sizeof(float), dataBuf, 0, (anaLen - blockLen10Ms) * sizeof(float));
			Buffer.BlockCopy(fin, 0, dataBuf, (anaLen - blockLen10Ms) * sizeof(float), blockLen10Ms * sizeof(float));

			// check if processing needed
			if (outLen == 0)
			{
				// windowing
				float energy1 = 0.0f;
				for (i = 0; i < anaLen; i++)
				{
					winData[i] = window[i] * dataBuf[i];
					energy1 += winData[i] * winData[i];
				}
				// FFT
				fft.Transform(winData, anaLen); //, ip, wfft);

				imag[0] = 0;
				real[0] = winData[0];
				magn[0] = (Math.Abs(real[0]) + 1.0f);
				imag[magnLen - 1] = 0;
				real[magnLen - 1] = winData[1];
				magn[magnLen - 1] = (Math.Abs(real[magnLen - 1]) + 1.0f);
				signalEnergy = (real[0] * real[0]) + (real[magnLen - 1] * real[magnLen - 1]);
				sumMagn = magn[0] + magn[magnLen - 1];
				if (blockInd < END_STARTUP_SHORT)
				{
					initMagnEst[0] += magn[0];
					initMagnEst[magnLen - 1] += magn[magnLen - 1];
					tmpFloat2 = (float)Math.Log((magnLen - 1));
					sum_log_i = tmpFloat2;
					sum_log_i_square = tmpFloat2 * tmpFloat2;
					tmpFloat1 = (float)Math.Log(magn[magnLen - 1]);
					sum_log_magn = tmpFloat1;
					sum_log_i_log_magn = tmpFloat2 * tmpFloat1;
				}
				for (i = 1; i < magnLen - 1; i++)
				{
					real[i] = winData[2 * i];
					imag[i] = winData[2 * i + 1];
					// magnitude spectrum
					fTmp = real[i] * real[i];
					fTmp += imag[i] * imag[i];
					signalEnergy += fTmp;
					magn[i] = ((float)Math.Sqrt(fTmp)) + 1.0f;
					sumMagn += magn[i];
					if ((i >= kStartBand) && (blockInd < END_STARTUP_SHORT))
					{
						initMagnEst[i] += magn[i];
						tmpFloat2 = (float)Math.Log(i);
						sum_log_i += tmpFloat2;
						sum_log_i_square += tmpFloat2 * tmpFloat2;
						tmpFloat1 = (float)Math.Log(magn[i]);
						sum_log_magn += tmpFloat1;
						sum_log_i_log_magn += tmpFloat2 * tmpFloat1;
					}
				}
				signalEnergy = signalEnergy / (magnLen);
				this.signalEnergy = signalEnergy;
				this.sumMagn = sumMagn;

				//compute spectral flatness on input spectrum
				WebRtcNs_ComputeSpectralFlatness(magn);

				WebRtcUtil.WriteDebugMessage(String.Format("(C#) NS 01 blockInd = {0}    magn[2] = {1}", blockInd, magn[2]));

				// quantile noise estimate
				WebRtcNs_NoiseEstimation(magn, noise);
				//compute simplified noise model during startup
				if (blockInd < END_STARTUP_SHORT)
				{
					// Estimate White noise
					whiteNoiseLevel += sumMagn / (magnLen) * overdrive;
					// Estimate Pink noise parameters
					tmpFloat1 = sum_log_i_square * ((magnLen - kStartBand));
					tmpFloat1 -= (sum_log_i * sum_log_i);
					tmpFloat2 = (sum_log_i_square * sum_log_magn - sum_log_i * sum_log_i_log_magn);
					tmpFloat3 = tmpFloat2 / tmpFloat1;
					// Constraint the estimated spectrum to be positive
					if (tmpFloat3 < 0.0f)
					{
						tmpFloat3 = 0.0f;
					}
					pinkNoiseNumerator += tmpFloat3;
					tmpFloat2 = (sum_log_i * sum_log_magn);
					tmpFloat2 -= ((magnLen - kStartBand)) * sum_log_i_log_magn;
					tmpFloat3 = tmpFloat2 / tmpFloat1;
					// Constraint the pink noise power to be in the interval [0, 1];
					if (tmpFloat3 < 0.0f)
					{
						tmpFloat3 = 0.0f;
					}
					if (tmpFloat3 > 1.0f)
					{
						tmpFloat3 = 1.0f;
					}
					pinkNoiseExp += tmpFloat3;
					for (i = 0; i < magnLen; i++)
					{
						// Estimate the background noise using the white and pink noise parameters
						j = WebRtcUtil.WEBRTC_SPL_MAX(i, kStartBand);
						if (pinkNoiseExp == 0.0f)
						{
							// Use white noise estimate
							tmpFloat1 = whiteNoiseLevel;
						}
						else
						{
							// Use pink noise estimate
							tmpFloat1 = (float)Math.Exp(pinkNoiseNumerator / (blockInd + 1));
							tmpFloat1 *= (blockInd + 1);
							tmpFloat2 = pinkNoiseExp / (blockInd + 1);
							tmpFloat1 /= (float)Math.Pow(j, tmpFloat2);
						}
						theFilterTmp[i] = (initMagnEst[i] - overdrive * tmpFloat1);
						theFilterTmp[i] /= (initMagnEst[i] + (float)0.0001);
						// Weight quantile noise with modeled noise
						noise[i] *= (blockInd);
						tmpFloat2 = tmpFloat1 * (END_STARTUP_LONG - blockInd);
						noise[i] += (tmpFloat2 / (blockInd + 1));
						noise[i] /= END_STARTUP_LONG;
					}
				}
				//compute average signal during END_STARTUP_LONG time:
				// used to normalize spectral difference measure
				if (blockInd < END_STARTUP_LONG)
				{
					featureData[5] *= blockInd;
					featureData[5] += signalEnergy;
					featureData[5] /= (blockInd + 1);
				}

				//start processing at frames == converged+1
				//
				// STEP 1: compute  prior and post snr based on quantile noise est
				//

				// compute DD estimate of prior SNR: needed for new method
				for (i = 0; i < magnLen; i++)
				{
					// post snr
					snrLocPost[i] = (float)0.0;
					if (magn[i] > noise[i])
					{
						snrLocPost[i] = magn[i] / (noise[i] + (float)0.0001) - (float)1.0;
					}
					// previous post snr
					// previous estimate: based on previous frame with gain filter
					previousEstimateStsa[i] = magnPrev[i] / (noisePrev[i] + (float)0.0001)
											  * (smooth[i]);
					// DD estimate is sum of two terms: current estimate and previous estimate
					// directed decision update of snrPrior
					snrLocPrior[i] = DD_PR_SNR * previousEstimateStsa[i] + ((float)1.0 - DD_PR_SNR) * snrLocPost[i];
					// post and prior snr needed for step 2
				} // end of loop over freqs

				// done with step 1: dd computation of prior and post snr

				//
				//STEP 2: compute speech/noise likelihood
				//

				// compute difference of input spectrum with learned/estimated noise spectrum
				WebRtcNs_ComputeSpectralDifference(magn);
				// compute histograms for parameter decisions (thresholds and weights for features)
				// parameters are extracted once every window time (=this.modelUpdatePars[1])
				if (updateParsFlag >= 1)
				{
					// counter update
					modelUpdatePars[3]--;
					// update histogram
					if (modelUpdatePars[3] > 0)
					{
						WebRtcNs_FeatureParameterExtraction(0);
					}
					// compute model parameters
					if (modelUpdatePars[3] == 0)
					{
						WebRtcNs_FeatureParameterExtraction(1);
						modelUpdatePars[3] = modelUpdatePars[1];
						// if wish to update only once, set flag to zero
						if (updateParsFlag == 1)
						{
							modelUpdatePars[0] = 0;
						}
						else
						{
							// update every window:
							// get normalization for spectral difference for next window estimate
							featureData[6] = featureData[6] / (modelUpdatePars[1]);
							featureData[5] = (float)0.5 * (featureData[6] + featureData[5]);
							featureData[6] = (float)0.0;
						}
					}
				}
				// compute speech/noise probability
				WebRtcNs_SpeechNoiseProb(probSpeechFinal, snrLocPrior, snrLocPost);
				// time-avg parameter for noise update
				gammaNoiseTmp = NOISE_UPDATE;
				for (i = 0; i < magnLen; i++)
				{
					probSpeech = probSpeechFinal[i];
					probNonSpeech = (float)1.0 - probSpeech;
					// temporary noise update:
					// use it for speech frames if update value is less than previous
					noiseUpdateTmp = gammaNoiseTmp * noisePrev[i] + ((float)1.0 - gammaNoiseTmp)
									 * (probNonSpeech * magn[i] + probSpeech * noisePrev[i]);
					//
					// time-constant based on speech/noise state
					gammaNoiseOld = gammaNoiseTmp;
					gammaNoiseTmp = NOISE_UPDATE;
					// increase gamma (i.e., less noise update) for frame likely to be speech
					if (probSpeech > PROB_RANGE)
					{
						gammaNoiseTmp = SPEECH_UPDATE;
					}
					// conservative noise update
					if (probSpeech < PROB_RANGE)
					{
						magnAvgPause[i] += GAMMA_PAUSE * (magn[i] - magnAvgPause[i]);
					}
					// noise update
					if (gammaNoiseTmp == gammaNoiseOld)
					{
						noise[i] = noiseUpdateTmp;
					}
					else
					{
						noise[i] = gammaNoiseTmp * noisePrev[i] + ((float)1.0 - gammaNoiseTmp)
								   * (probNonSpeech * magn[i] + probSpeech * noisePrev[i]);
						// allow for noise update downwards:
						//  if noise update decreases the noise, it is safe, so allow it to happen
						if (noiseUpdateTmp < noise[i])
						{
							noise[i] = noiseUpdateTmp;
						}
					}
				} // end of freq loop
				// done with step 2: noise update

				//
				// STEP 3: compute dd update of prior snr and post snr based on new noise estimate
				//
				for (i = 0; i < magnLen; i++)
				{
					// post and prior snr
					currentEstimateStsa = (float)0.0;
					if (magn[i] > noise[i])
					{
						currentEstimateStsa = magn[i] / (noise[i] + (float)0.0001) - (float)1.0;
					}
					// DD estimate is sume of two terms: current estimate and previous estimate
					// directed decision update of snrPrior
					snrPrior = DD_PR_SNR * previousEstimateStsa[i] + ((float)1.0 - DD_PR_SNR) * currentEstimateStsa;
					// gain filter
					tmpFloat1 = overdrive + snrPrior;
					tmpFloat2 = snrPrior / tmpFloat1;
					theFilter[i] = tmpFloat2;
				} // end of loop over freqs
				// done with step3


				for (i = 0; i < magnLen; i++)
				{
					// flooring bottom
					if (theFilter[i] < denoiseBound)
					{
						theFilter[i] = denoiseBound;
					}
					// flooring top
					if (theFilter[i] > (float)1.0)
					{
						theFilter[i] = 1.0f;
					}
					if (blockInd < END_STARTUP_SHORT)
					{
						// flooring bottom
						if (theFilterTmp[i] < denoiseBound)
						{
							theFilterTmp[i] = denoiseBound;
						}
						// flooring top
						if (theFilterTmp[i] > (float)1.0)
						{
							theFilterTmp[i] = 1.0f;
						}
						// Weight the two suppression filters
						theFilter[i] *= (blockInd);
						theFilterTmp[i] *= (END_STARTUP_SHORT - blockInd);
						theFilter[i] += theFilterTmp[i];
						theFilter[i] /= (END_STARTUP_SHORT);
					}
					// smoothing

					smooth[i] = theFilter[i];
					real[i] *= smooth[i];
					imag[i] *= smooth[i];
				}
				// keep track of noise and magn spectrum for next frame
				for (i = 0; i < magnLen; i++)
				{
					noisePrev[i] = noise[i];
					magnPrev[i] = magn[i];
				}
				// back to time domain
				winData[0] = real[0];
				winData[1] = real[magnLen - 1];
				for (i = 1; i < magnLen - 1; i++)
				{
					winData[2 * i] = real[i];
					winData[2 * i + 1] = imag[i];
				}
				fft.ReverseTransform(winData, anaLen); //, ip, wfft);

				for (i = 0; i < anaLen; i++)
				{
					real[i] = 2.0f * winData[i] / anaLen; // fft scaling
				}

				//scale factor: only do it after END_STARTUP_LONG time
				factor = (float)1.0;
				if (gainmap == 1 && blockInd > END_STARTUP_LONG)
				{
					factor1 = (float)1.0;
					factor2 = (float)1.0;

					energy2 = 0.0f;
					for (i = 0; i < anaLen; i++)
					{
						energy2 += real[i] * real[i];
					}
					gain = (float)Math.Sqrt(energy2 / (energy1 + (float)1.0));


					// scaling for new version
					if (gain > B_LIM)
					{
						factor1 = (float)1.0 + (float)1.3 * (gain - B_LIM);
						if (gain * factor1 > (float)1.0)
						{
							factor1 = (float)1.0 / gain;
						}
					}
					if (gain < B_LIM)
					{
						//don't reduce scale too much for pause regions:
						// attenuation here should be controlled by flooring
						if (gain <= denoiseBound)
						{
							gain = denoiseBound;
						}
						factor2 = (float)1.0 - (float)0.3 * (B_LIM - gain);
					}
					//combine both scales with speech/noise prob:
					// note prior (priorSpeechProb) is not frequency dependent
					factor = priorSpeechProb * factor1 + ((float)1.0 - priorSpeechProb) * factor2;
				} // out of this.gainmap==1

				// synthesis
				for (i = 0; i < anaLen; i++)
				{
					syntBuf[i] += factor * window[i] * real[i];
				}
				// read out fully processed segment
				for (i = windShift; i < blockLen + windShift; i++)
				{
					fout[i - windShift] = syntBuf[i];
				}
				// update synthesis buffer
				Buffer.BlockCopy(syntBuf, blockLen * sizeof(float), syntBuf, 0, (anaLen - blockLen) * sizeof(float));
				Array.Clear(syntBuf, anaLen - blockLen, blockLen);

				// out buffer
				outLen = blockLen - blockLen10Ms;
				if (blockLen > blockLen10Ms)
				{
					Buffer.BlockCopy(fout, blockLen10Ms * sizeof(float), outBuf, 0, outLen * sizeof(float));
				}
			} // end of if out.len==0
			else
			{
				Buffer.BlockCopy(fout, 0, outBuf, 0, outLen * sizeof(float));
				Buffer.BlockCopy(outBuf, blockLen10Ms * sizeof(float), outBuf, 0, (outLen - blockLen10Ms) * sizeof(float));
				Array.Clear(outBuf, outLen - blockLen10Ms, blockLen10Ms);
				outLen -= blockLen10Ms;
			}

			// convert to short
			for (i = 0; i < blockLen10Ms; i++)
			{
				dTmp = fout[i];
				if (dTmp < short.MinValue)
				{
					dTmp = short.MinValue;
				}
				else if (dTmp > short.MaxValue)
				{
					dTmp = short.MaxValue;
				}
				outFrame[i + outOffset] = (short)dTmp;
			}

			//// for time-domain gain of HB
			//if (flagHB == 1)
			//{
			//    // convert to float
			//    for (i = 0; i < this.blockLen10ms; i++)
			//    {
			//        fin[i] = (float)speechFrameHB[i];
			//    }
			//    // update analysis buffer for H band
			//    memcpy(this.dataBufHB, this.dataBufHB + this.blockLen10ms,
			//           sizeof(float) * (this.anaLen - this.blockLen10ms));
			//    memcpy(this.dataBufHB + this.anaLen - this.blockLen10ms, fin,
			//           sizeof(float) * this.blockLen10ms);
			//    for (i = 0; i < this.magnLen; i++)
			//    {
			//        this.speechProbHB[i] = probSpeechFinal[i];
			//    }
			//    if (this.blockInd > END_STARTUP_LONG)
			//    {
			//        // average speech prob from low band
			//        // avg over second half (i.e., 4->8kHz) of freq. spectrum
			//        avgProbSpeechHB = 0.0;
			//        for (i = this.magnLen - deltaBweHB - 1; i < this.magnLen - 1; i++)
			//        {
			//            avgProbSpeechHB += this.speechProbHB[i];
			//        }
			//        avgProbSpeechHB = avgProbSpeechHB / ((float)deltaBweHB);
			//        // average filter gain from low band
			//        // average over second half (i.e., 4->8kHz) of freq. spectrum
			//        avgFilterGainHB = 0.0;
			//        for (i = this.magnLen - deltaGainHB - 1; i < this.magnLen - 1; i++)
			//        {
			//            avgFilterGainHB += this.smooth[i];
			//        }
			//        avgFilterGainHB = avgFilterGainHB / ((float)(deltaGainHB));
			//        avgProbSpeechHBTmp = (float)2.0 * avgProbSpeechHB - (float)1.0;
			//        // gain based on speech prob:
			//        gainModHB = (float)0.5 * ((float)1.0 + (float)tanh(gainMapParHB * avgProbSpeechHBTmp));
			//        //combine gain with low band gain
			//        gainTimeDomainHB = (float)0.5 * gainModHB + (float)0.5 * avgFilterGainHB;
			//        if (avgProbSpeechHB >= (float)0.5)
			//        {
			//            gainTimeDomainHB = (float)0.25 * gainModHB + (float)0.75 * avgFilterGainHB;
			//        }
			//        gainTimeDomainHB = gainTimeDomainHB * decayBweHB;
			//    } // end of converged
			//    //make sure gain is within flooring range
			//    // flooring bottom
			//    if (gainTimeDomainHB < this.denoiseBound)
			//    {
			//        gainTimeDomainHB = this.denoiseBound;
			//    }
			//    // flooring top
			//    if (gainTimeDomainHB > (float)1.0)
			//    {
			//        gainTimeDomainHB = 1.0;
			//    }
			//    //apply gain
			//    for (i = 0; i < this.blockLen10ms; i++)
			//    {
			//        dTmp = gainTimeDomainHB * this.dataBufHB[i];
			//        if (dTmp < WEBRTC_SPL_WORD16_MIN)
			//        {
			//            dTmp = WEBRTC_SPL_WORD16_MIN;
			//        }
			//        else if (dTmp > WEBRTC_SPL_WORD16_MAX)
			//        {
			//            dTmp = WEBRTC_SPL_WORD16_MAX;
			//        }
			//        outFrameHB[i] = (short)dTmp;
			//    }
			//}// end of H band gain computation
			////

			return;
		}

		#region Nested type: NSParaExtract

		private class NsParaExtract
		{
			//bin size of histogram
			public float binSizeLrt;
			public float binSizeSpecDiff;
			public float binSizeSpecFlat;
			//range of histogram over which lrt threshold is computed
			//scale parameters: multiply dominant peaks of the histograms by scale factor to obtain
			//thresholds for prior model
			public float factor1ModelPars; //for lrt and spectral difference
			public float factor2ModelPars; //for spectral_flatness: used when noise is flatter than speech
			//peak limit for spectral flatness (varies between 0 and 1)
			public float limitPeakSpacingSpecDiff;
			public float limitPeakSpacingSpecFlat;
			//limit on relevance of second peak:
			public float limitPeakWeightsSpecDiff;
			public float limitPeakWeightsSpecFlat;
			//limit on fluctuation of lrt feature
			//limit on the max and min values for the feature thresholds
			public float maxLrt;
			public float maxSpecDiff;
			public float maxSpecFlat;
			public float minLrt;
			public float minSpecDiff;
			public float minSpecFlat;
			public float rangeAvgHistLrt;
			public float thresFluctLrt;
			public float thresPosSpecFlat;
			//criteria of weight of histogram peak  to accept/reject feature
			public int thresWeightSpecDiff;
			public int thresWeightSpecFlat;
		}

		#endregion
	}
}