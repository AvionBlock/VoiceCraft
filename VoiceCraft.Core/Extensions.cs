namespace VoiceCraft.Core
{
    public static class Extensions
    {
        public static string Truncate(this string value, int maxLength, string truncationSuffix = "…")
        {
            return value.Length > maxLength
                ? value[..maxLength] + truncationSuffix
                : value;
        }

        public static float GetFramePeak16(this byte[] data, int bytesRead)
        {
            float max = 0;
            // interpret as 16-bit audio
            for (var index = 0; index < bytesRead; index += 2)
            {
                var sample = (short)((data[index + 1] << 8) |
                                     data[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            return max;
        }
    }
}