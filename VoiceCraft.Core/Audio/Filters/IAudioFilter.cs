using System;
namespace VoiceCraft.Core.Audio.Filters
{
    /// <summary>
    /// A generic interface for audio filters.
    /// </summary>
    public interface IAudioFilter
    {
        /// <summary>
        /// Submits samples to the filter for processing.
        /// </summary>
        /// <param name="sampleData">A byte[] array of audio samples which need to be processed.</param>
        void Write(byte[] sampleData);

        /// <summary>
        /// Retrieves the next processed frame from the filter.
        /// </summary>
        /// <param name="frame">The buffer onto which the processed frame should be written.</param>
        /// <param name="moreFrames">Whether there are more processed frames available for retrieval.</param>
        /// <returns>True if a processed frame was retrieved and written onto the outBuffer, false if not.</returns>
        bool Read(Array frame, out bool moreFrames);

        /// <summary>
        /// (Optional) Unique name for the filter instance. Helpful in debugging, so we can control which instance we're breaking into.
        /// </summary>
        string InstanceName { get; set; }
    }

    public interface IAudioTwoWayFilter : IAudioFilter
    {
        void RegisterFramePlayed(byte[] speakerSample);
    }

    /// <summary>
    /// An audio filter which filters the data in-place. Used to avoid memcopies.
    /// </summary>
    public interface IAudioInplaceFilter
    {
        void Filter(short[] sampleData);
        string InstanceName { get; set; }
    }
}
