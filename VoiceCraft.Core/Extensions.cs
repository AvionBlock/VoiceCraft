namespace VoiceCraft.Core
{
    public static class Extensions
    {
        public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "…")
        {
            return value?.Length > maxLength
                ? value[..maxLength] + truncationSuffix
                : value;
        }
    }
}