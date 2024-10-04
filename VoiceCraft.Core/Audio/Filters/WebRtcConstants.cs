
namespace VoiceCraft.Core.Audio.Filters
{
    internal static class WebRtcConstants
    {
        /// <summary>
        /// The length of the frames to process.
        /// </summary>
        // internal const int SamplesPerFrame = AudioFormat.SamplesPerFrame;

        /// <summary>
        /// Buffer size (in frames)
        /// </summary>
        internal const int BUF_SIZE_FRAMES = 50;

        /// <summary>
        /// Buffer size (in samples)
        /// </summary>
        // internal const int BufSizeSamp = BUF_SIZE_FRAMES * SamplesPerFrame;

        /// <summary>
        /// Samples per ms in nb
        /// </summary>
        internal const int sampMsNb = 8;

        // Target suppression levels for nlp modes
        // log{0.001, 0.00001, 0.00000001}
        internal static readonly float[] targetSupp = new[] { -6.9f, -11.5f, -18.4f };
        internal static readonly float[] minOverDrive = new[] { 1.0f, 2.0f, 5.0f };

        // Metrics
        internal const int SubCountLen = 4;
        internal const int CountLen = 50;
    }
}
