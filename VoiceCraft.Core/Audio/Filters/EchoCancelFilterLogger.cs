using System;
using System.Diagnostics;

namespace VoiceCraft.Core.Audio.Filters
{
    public class EchoCancelFilterLogger
    {
        public EchoCancelFilterLogger()
        {
            startTime = DateTime.MinValue;
            lastResetTime = DateTime.MinValue;
        }

        private const int reportingInterval = 2000;

        private DateTime startTime;
        private DateTime lastResetTime;

        volatile int framesPlayedTotal;
        volatile int framesPlayedRecent;
        volatile int framesCancelledTotal;
        volatile int framesCancelledRecent;
        volatile int queueFullTotal;
        volatile int queueFullRecent;
        volatile int queueTooSmallTotal;
        volatile int queueTooSmallRecent;
        private volatile int queueTargetReachedTotal;
        private volatile int queueTargetReachedRecent;

        private double recentStdDevInputVolumeSum;
        private double totalStdDevInputVolumeSum;
        private double recentStdDevCancelledVolumeSum;
        private double totalStdDevCancelledVolumeSum;

        [Conditional("DEBUG")]
        public void LogFramePlayed(short[] played)
        {
            if (startTime == DateTime.MinValue)
            {
                startTime = DateTime.Now;
                lastResetTime = DateTime.Now;
            }
            framesPlayedTotal++;
            if (++framesPlayedRecent >= reportingInterval)
            {
                var intervalSinceStart = DateTime.Now - startTime;
                var intervalSinceLastReset = DateTime.Now - lastResetTime;

                var totalMsBetweenCancelledFrames = intervalSinceStart.TotalMilliseconds / framesCancelledTotal;
                var recentMsBetweenCancelledFrames = intervalSinceLastReset.TotalMilliseconds / framesCancelledRecent;
                var totalMsBetweenPlayedFrames = intervalSinceStart.TotalMilliseconds / framesPlayedTotal;
                var recentMsBetweenPlayedFrames = intervalSinceLastReset.TotalMilliseconds / framesPlayedRecent;
                //ClientLogger.Debug("(AEC) Played: {0}/{1}; Cancelled: {2}/{3}; msBetweenPlayed:{4:0.00}/{5:0.00}; msBetweenCancelled:{6:0.00}/{7:0.00};",
                //    framesPlayedRecent, framesPlayedTotal,
                //    framesCancelledRecent, framesCancelledTotal,
                //    recentMsBetweenCancelledFrames, totalMsBetweenCancelledFrames,
                //    recentMsBetweenPlayedFrames, totalMsBetweenPlayedFrames);

                //ClientLogger.LogDebugMessage("(AEC) Played: {0}/{1}; Cancelled: {2}/{3}; Full: {4}/{5}; TooSmall: {6}/{7}; TargetReached: {8}/{9}",
                //    framesPlayedRecent, framesPlayedTotal,
                //    framesCancelledRecent, framesCancelledTotal,
                //    queueFullRecent, queueFullTotal,
                //    queueTooSmallRecent, queueTooSmallTotal,
                //    queueTargetReachedRecent, queueTargetReachedTotal);

                //double totalInputAverage = totalStdDevInputVolumeSum / framesPlayedTotal;
                //double recentInputAverage = recentStdDevInputVolumeSum / framesPlayedRecent;
                //double totalCancelledAverage = totalStdDevCancelledVolumeSum / framesPlayedTotal;
                //double recentCancelledAverage = recentStdDevCancelledVolumeSum / framesPlayedRecent;
                //ClientLogger.LogDebugMessage("(AEC) Recent input {0:f}, cancelled {1:f}, diff {2:f}, % {3:f}; total input {4:f}, cancelled {5:f}, diff {6:f}, % {7:f}",
                //    recentInputAverage,
                //    recentCancelledAverage,
                //    recentInputAverage - recentCancelledAverage,
                //    (recentInputAverage - recentCancelledAverage) / recentInputAverage * 100,
                //    totalInputAverage,
                //    totalCancelledAverage,
                //    totalInputAverage - totalCancelledAverage,
                //    (totalInputAverage - totalCancelledAverage) / totalInputAverage * 100);

                framesCancelledRecent = 0;
                framesPlayedRecent = 0;
                queueTooSmallRecent = 0;
                queueFullRecent = 0;
                recentStdDevCancelledVolumeSum = 0;
                recentStdDevInputVolumeSum = 0;
                queueTargetReachedRecent = 0;
                lastResetTime = DateTime.Now;
            }
        }

        [Conditional("DEBUG")]
        public void LogFrameCancelled(short[] played, short[] cancelled)
        {
            framesCancelledTotal++;
            framesCancelledRecent++;

            double inputStdDev = DspHelper.GetStandardDeviation(played);
            double cancelledStdDev = DspHelper.GetStandardDeviation(cancelled);

            totalStdDevInputVolumeSum += inputStdDev;
            recentStdDevInputVolumeSum += inputStdDev;
            totalStdDevCancelledVolumeSum += cancelledStdDev;
            recentStdDevCancelledVolumeSum += cancelledStdDev;
        }

        [Conditional("DEBUG")]
        public void LogQueueFull()
        {
            queueFullTotal++;
            queueFullRecent++;
        }

        [Conditional("DEBUG")]
        public void LogQueueTooSmall()
        {
            queueTooSmallRecent++;
            queueTooSmallTotal++;
        }

        //[Conditional("DEBUG")]
        //internal void LogSff(float Sff)
        //{
        //    sffTotal += Sff;
        //    if (++sffCount % 500 == 0)
        //    {
        //        ClientLogger.LogDebugMessage("(AEC) Average Sff = {0}", sffTotal / sffCount);
        //        sffTotal = 0;
        //        sffCount = 0;
        //    }
        //}

        [Conditional("DEBUG")]
        internal void LogQueueTargetReached(int targetQueueSize, int actualQueueSize)
        {
            // ClientLogger.LogDebugMessage("(AEC) The queue target size of {0} was reached with an actual queue size of {1}", targetQueueSize, actualQueueSize);
            queueTargetReachedRecent++;
            queueTargetReachedTotal++;
        }
    }
}
