using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Audio.Filters
{
    /// <summary>
    /// A workable base class for a wide range of echo cancellation algorithms.
    /// Includes a queue for buffering frames to account for extra latency incurred when playing audio.
    /// </summary>
    /// <remarks>Assumes a 16-bit (short) sample. Also, for performance reasons, 
    /// the individual methods of the class are NOT threadsafe. 
    /// Only call a given method on a given instance from one thread at a time.</remarks>
    public abstract class EchoCancelFilter : IAudioTwoWayFilter
    {
        #region Constructors

        /// <summary>
        /// Initializes the echo canceller.
        /// </summary>
        /// <param name="systemLatency">The amount of latency that the operating environment adds (in milliseconds). 
        /// Determines how long a played frame is held before being submitted to the echo canceller.
        /// For Silverlight v4, this is typically ~150ms.</param>
        /// <param name="filterLength">The length of the echo cancellation filter in milliseconds (typically ~150).</param>
        /// <param name="recordedAudioFormat">The format of the recorded audio</param>
        /// <param name="playedAudioFormat">The format of the played audio</param>
        /// <param name="playedResampler">An instance of an IAudioFilter&lt;short&gt; which can be used to resample or synchronize played frames.</param>
        /// <param name="recordedResampler">An instance of an IAudioFilter&lt;short&gt; which can be used to resample or synchronize played frames.</param>
        protected EchoCancelFilter(int systemLatency, int filterLength, AudioFormat recordedAudioFormat, AudioFormat playedAudioFormat, IAudioFilter playedResampler = null, IAudioFilter recordedResampler = null)
        {
            this.recordedAudioFormat = recordedAudioFormat;
            this.playedAudioFormat = playedAudioFormat;

            if (playedResampler == null)
            {
                // playedResampler = new NullAudioFilter(recordedAudioFormat.SamplesPerFrame * sizeof(short)) { InstanceName = "EchoCanceller_played" };
                playedResampler = new ResampleFilter(playedAudioFormat, recordedAudioFormat);
                // var pr = new ResampleFilter<short>(sizeof(short), samplesPerSecond, channels, sizeof(short), samplesPerSecond, channels, samplesPerFrame * sizeof(short));
            }

            if (recordedResampler == null)
            {
                recordedResampler = new NullAudioFilter(recordedAudioFormat.SamplesPerFrame * sizeof(short));
                // var rr = new ResampleFilter(sizeof(short), samplesPerSecond, channels, sizeof(short), samplesPerSecond, channels, samplesPerFrame * sizeof(short));
                // recordedResampler = new ResampleFilter(playedAudioFormat, recordedAudioFormat);
                recordedResampler.InstanceName = "EchoCanceller_recorded";
            }

            logger = new EchoCancelFilterLogger();
            SystemLatency = systemLatency;
            FilterLength = filterLength * (recordedAudioFormat.SamplesPerSecond / 1000);
            SamplesPerFrame = recordedAudioFormat.SamplesPerFrame;
            SamplesPerSecond = recordedAudioFormat.SamplesPerSecond;
            recorded = new short[SamplesPerFrame];

            // Configure the latency queue.
            QueueSize = Math.Max(systemLatency / recordedAudioFormat.MillisecondsPerFrame, 1);
            maxQueueSize = QueueSize + 1;
            playedQueue = new Queue<short[]>();

            this.playedResampler = playedResampler;
            this.recordedResampler = recordedResampler;

        }
        #endregion

        #region Fields and Properties
        private readonly Queue<short[]> playedQueue;
        private bool queueTargetReached;
        private readonly IAudioFilter playedResampler;
        private readonly IAudioFilter recordedResampler;
        protected EchoCancelFilterLogger logger;
        private readonly short[] recorded;
        protected readonly AudioFormat recordedAudioFormat;
        protected readonly AudioFormat playedAudioFormat;

        public int SystemLatency { get; private set; }
        public int FilterLength { get; private set; }
        public int SamplesPerFrame { get; private set; }
        public int SamplesPerSecond { get; private set; }
        public int QueueSize { get; private set; }
        private readonly int maxQueueSize;

        /// <summary>
        /// Helps in debugging.
        /// </summary>
        public string InstanceName { get; set; }
        #endregion

        #region Methods

        /// <summary>
        /// Record that a frame was submitted to the speakers.
        /// </summary>
        /// <param name="speakerSample">The ByteStream containing the data submitted to the speakers.</param>
        public void RegisterFramePlayed(byte[] speakerSample)
        {
            lock (playedQueue)
            {
                // If we have too many frames in the queue, discard the oldest.
                while (playedQueue.Count > maxQueueSize)
                {
                    logger.LogQueueFull();
                    playedQueue.Dequeue();
                }

                // Resample the frame in case the frames have been coming in at the wrong rate.
                playedResampler.Write(speakerSample);
                var frame = new short[SamplesPerFrame];
                bool moreFrames;
                do
                {
                    if (playedResampler.Read(frame, out moreFrames))
                    {
                        playedQueue.Enqueue(frame);
                        logger.LogFramePlayed(frame);
                        if (!queueTargetReached && playedQueue.Count >= QueueSize)
                        {
                            logger.LogQueueTargetReached(QueueSize, playedQueue.Count);
                            queueTargetReached = true;
                        }
                    }
                } while (moreFrames);
            }
        }

        /// <summary>
        /// Perform echo cancellation on a frame recorded from the local microphone.
        /// </summary>
        /// <param name="recordedData">The byte array containing the data recorded from the local microphone.</param>
        public virtual void Write(byte[] recordedData)
        {
            // Resample the incoming microphone sample onto a new buffer (to adjust for slight differences in timing on different sound cards).
            recordedResampler.Write(recordedData);
        }

        public virtual bool Read(Array outBuffer, out bool moreFrames)
        {
            // FIXME: We're moving the data around twice, which is unnecessary. When I get it working,
            // I need to come back and get rid of one of the Buffer.BlockCopy() moves.

            // Dequeue the audio submitted to the speakers ~12 frames back.
            if (recordedResampler.Read(recorded, out moreFrames))
            {
                // If we successfully retrieved a buffered recorded frame, then try to get one of the buffered played frames.
                short[] played;
                lock (playedQueue)
                {
                    // Don't cancel anything if we haven't buffered enough packets yet.
                    if (!queueTargetReached || playedQueue.Count == 0)
                    {
                        queueTargetReached = false;
                        Buffer.BlockCopy(recorded, 0, outBuffer, 0, recorded.Length * sizeof(short));
                        logger.LogQueueTooSmall();
                        return true;
                    }
                    played = playedQueue.Dequeue();
                }

                // If we have both a recorded and a played frame, let's echo cancel those babies.
                PerformEchoCancellation(recorded, played, (short[])outBuffer);
                logger.LogFrameCancelled(recorded, (short[])outBuffer);
            }
            else
            {
                return false;
            }
            return true;
        }

        protected abstract void PerformEchoCancellation(short[] recorded, short[] played, short[] outFrame);
        #endregion

    }
}
