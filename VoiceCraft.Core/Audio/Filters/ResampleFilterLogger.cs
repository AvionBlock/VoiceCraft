using System;
using System.Diagnostics;

namespace VoiceCraft.Core.Audio.Filters
{
    public class ResampleFilterLogger
    {
        public string InstanceName { get; set; }

        private DateTime firstFrameSubmittedAt = DateTime.MinValue;
        private DateTime lastResetAt = DateTime.MinValue;
        private long totalSamplesSubmitted;
        private long totalScaledLength;
        private long totalMaxSampleLength = long.MinValue;
        private long totalMinSamplelength = long.MaxValue;
        private long recentSamplesSubmitted;
        private long recentScaledLength;
        private long recentMaxScaledLength = long.MinValue;
        private long recentMinScaledLength = long.MaxValue;
        private long totalCorrectedLength;
        private long totalMinCorrectedLength = long.MaxValue;
        private long totalMaxCorrectedLength = long.MinValue;
        private long recentCorrectedLength;
        private long recentMinCorrectedLength = long.MaxValue;
        private long recentMaxCorrectedLength = long.MinValue;
        private long recentReadsTooSlow;
        private long totalReadsTooSlow;
        private long recentReadsTooFast;
        private long totalReadsTooFast;
        private double minCorrectionFactor = double.MaxValue;
        private double maxCorrectionFactor = double.MinValue;

        private DateTime firstFrameRetrievedAt = DateTime.MinValue;
        private long totalSamplesRetrieved;
        private long recentSamplesRetrieved;
        private long totalSampleLengthRetrieved;
        private long recentSampleLengthRetrieved;

        private const int reportingInterval = 1000;

        public double AverageSubmissionTimeTotal { get { return totalSamplesSubmitted > 0 ? (DateTime.Now - firstFrameSubmittedAt).TotalMilliseconds / totalSamplesSubmitted : 0; } }
        public double AverageSubmissionTimeRecent { get { return recentSamplesSubmitted > 0 ? (DateTime.Now - lastResetAt).TotalMilliseconds / recentSamplesSubmitted : 0; } }
        public double AverageScaledLengthRecent { get { return recentSamplesSubmitted > 0 ? recentScaledLength / recentSamplesSubmitted : 0; } }
        public double AverageScaledLengthTotal { get { return totalSamplesSubmitted > 0 ? totalScaledLength / totalSamplesSubmitted : 0; } }
        public double AverageCorrectedLengthRecent { get { return recentSamplesSubmitted > 0 ? recentCorrectedLength / recentSamplesSubmitted : 0; } }
        public double AverageCorrectedLengthTotal { get { return totalSamplesSubmitted > 0 ? totalCorrectedLength / totalSamplesSubmitted : 0; } }

        public double AverageRetrievalTimeTotal { get { return totalSamplesRetrieved > 0 ? (DateTime.Now - firstFrameSubmittedAt).TotalMilliseconds / totalSamplesSubmitted : 0; } }
        public double AverageRetrievalTimeRecent { get { return recentSamplesRetrieved > 0 ? (DateTime.Now - lastResetAt).TotalMilliseconds / recentSamplesSubmitted : 0; } }
        public double AverageSamplesLengthRetrievedTotal { get { return totalSamplesRetrieved > 0 ? totalSampleLengthRetrieved / totalSamplesRetrieved : 0; } }
        public double AverageSamplesLengthRetrievedRecent { get { return recentSamplesRetrieved > 0 ? recentSampleLengthRetrieved / recentSamplesRetrieved : 0; } }

        private void ResetRecentCounters(DateTime now)
        {
            lock (this)
            {
                lastResetAt = now;
                recentScaledLength = 0;
                recentSamplesSubmitted = 0;
                recentMinScaledLength = long.MaxValue;
                recentMaxScaledLength = long.MinValue;
                recentCorrectedLength = 0;
                recentMinCorrectedLength = long.MaxValue;
                recentMaxCorrectedLength = long.MinValue;
                recentReadsTooFast = 0;
                recentReadsTooSlow = 0;
                recentSamplesRetrieved = 0;
                recentSampleLengthRetrieved = 0;
            }
        }

        [Conditional("DEBUG")]
        public void LogSampleSubmitted(byte[] sampleData, int scaledLength, int correctedLength, double correctionFactor)
        {
            var now = DateTime.Now;
            totalSamplesSubmitted++;
            recentSamplesSubmitted++;
            if (firstFrameSubmittedAt == DateTime.MinValue)
            {
                firstFrameSubmittedAt = now;
                lastResetAt = now;
            }

            lock (this)
            {
                totalScaledLength += scaledLength;
                recentScaledLength += scaledLength;
                totalMaxSampleLength = Math.Max(totalMaxSampleLength, scaledLength);
                totalMinSamplelength = Math.Min(totalMinSamplelength, scaledLength);
                recentMaxScaledLength = Math.Max(recentMaxScaledLength, scaledLength);
                recentMinScaledLength = Math.Min(recentMinScaledLength, scaledLength);

                totalCorrectedLength += correctedLength;
                recentCorrectedLength += correctedLength;
                totalMaxCorrectedLength = Math.Max(totalMaxCorrectedLength, correctedLength);
                totalMinCorrectedLength = Math.Min(totalMinCorrectedLength, correctedLength);
                recentMaxCorrectedLength = Math.Max(recentMaxCorrectedLength, correctedLength);
                recentMinCorrectedLength = Math.Min(recentMinCorrectedLength, correctedLength);
            }

            if (totalSamplesSubmitted % reportingInterval == 0)
            {
                string stats = string.Format(
                    "Instance: {0}\r\n" +
                    "Total Submissions: {1}\r\n" +
                    "Total Retrievals: {2}\r\n" +
                    "Recent Submissions: {3}\r\n" +
                    "Recent Retrievals: {4}\r\n" +
                    "Total Avg Submission Time: {5}\r\n" +
                    "Total Avg Retrieval Time: {6}\r\n" +
                    "Recent Avg Submission Time: {7}\r\n" +
                    "Recent Avg Retrieval Time: {8}\r\n" +
                    "Total Avg Scaled Length: {9}\r\n" +
                    "Total Avg Retrieval Length: {10}\r\n" +
                    "Recent Avg Scaled Length: {11}\r\n" +
                    "Recent Avg Retrieval Length: {12}",
                    InstanceName, // 0
                    totalSamplesSubmitted, // 1
                    totalSamplesRetrieved, // 2
                    recentSamplesSubmitted, // 3
                    recentSamplesRetrieved, // 4
                    AverageSubmissionTimeTotal, // 5
                    AverageRetrievalTimeTotal, // 6
                    AverageSubmissionTimeRecent, // 7
                    AverageRetrievalTimeRecent, // 8 
                    AverageScaledLengthTotal, // 9
                    AverageSamplesLengthRetrievedTotal, // 10
                    AverageScaledLengthRecent, // 11
                    AverageSamplesLengthRetrievedRecent); // 12
                                                          //ClientLogger.Debug(stats);
                ResetRecentCounters(DateTime.Now);
            }
        }

        [Conditional("DEBUG")]
        public void LogSamplesRetrieved(int length)
        {
            var now = DateTime.Now;
            totalSamplesRetrieved++;
            recentSamplesRetrieved++;
            if (firstFrameRetrievedAt == DateTime.MinValue)
            {
                firstFrameRetrievedAt = now;
            }
            totalSampleLengthRetrieved += length;
            recentSampleLengthRetrieved += length;
        }

        [Conditional("DEBUG")]
        public void LogCorrectionFactorReset(double correctionFactor)
        {
            maxCorrectionFactor = Math.Max(maxCorrectionFactor, correctionFactor);
            minCorrectionFactor = Math.Min(minCorrectionFactor, correctionFactor);
            double range = maxCorrectionFactor - minCorrectionFactor;
            string stats = string.Format(
                "Instance: {0}\r\n" +
                "Total frames: {1}\r\n" +
                "Avg Arrival Time Total: {2}\r\n" +
                "Avg Arrival Time Recent: {3}\r\n" +
                "Avg Corrected Length Total: {4}\r\n" +
                "Avg Corrected Length Recent: {5}\r\n" +
                "Max Corrected Length Total: {6}\r\n" +
                "Min Corrected Length Total: {7}\r\n" +
                "Max Corrected Length Recent: {8}\r\n" +
                "Min Corrected Length Recent: {9}\r\n" +
                "Reads Too Fast Total: {10}\r\n" +
                "Reads Too Fast Recent: {11}\r\n" +
                "Reads Too Slow total: {12}\r\n" +
                "Reads Too Slow Recent: {13}\r\n" +
                "Current Correction Factor: {14}\r\n" +
                "Max Correction Factor: {15}\r\n" +
                "Min Correction Factor: {16}\r\n" +
                "Range Correction Factor: {17}",
                InstanceName,
                totalSamplesSubmitted,
                AverageSubmissionTimeTotal,
                AverageSubmissionTimeRecent,
                AverageCorrectedLengthTotal,
                AverageCorrectedLengthRecent,
                totalMaxCorrectedLength,
                totalMinCorrectedLength,
                recentMaxCorrectedLength,
                recentMinCorrectedLength,
                totalReadsTooFast,
                recentReadsTooFast,
                totalReadsTooSlow,
                recentReadsTooSlow,
                correctionFactor,
                maxCorrectionFactor,
                minCorrectionFactor,
                range);
            ResetRecentCounters(DateTime.Now);
            //ClientLogger.Debug(stats);
        }

        internal void LogReadingTooSlow(int unreadBytes)
        {
            recentReadsTooSlow++;
            totalReadsTooSlow++;
        }

        internal void LogReadingTooFast(int unreadBytes)
        {
            recentReadsTooFast++;
            totalReadsTooFast++;
        }
    }
}
