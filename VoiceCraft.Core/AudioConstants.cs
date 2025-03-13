namespace VoiceCraft.Core
{
    public static class AudioConstants
    {
        public const int FrameSizeMs = 20;
        public const int SampleRate = 48000;
        public const int Channels = 1;
        public const int SamplesPerFrame = SampleRate / (1000 / FrameSizeMs) * Channels;
    }
}