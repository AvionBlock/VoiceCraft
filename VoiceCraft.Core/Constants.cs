namespace VoiceCraft.Core
{
    public static class Constants
    {
        public const int FrameSizeMs = 20;
        public const int SampleRate = 48000;
        public const int Channels = 1;
        public const int SamplesPerFrame = SampleRate / (1000 / FrameSizeMs) * Channels;
        public const int BytesPerFrame = (16 / 8) * Channels * SamplesPerFrame; //16-bit audio, this works out to 1920
        public const int MaximumEncodedBytes = 1000; //1000 bytes of allocation for encoding.
        
        public const int MaxStringLength = 100; //100 characters.
        public const int UpdateIntervalMS = 20;
    }
}