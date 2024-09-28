
namespace VoiceCraft.Core.Audio.Filters
{
	public class AudioConstants
	{
		public const int SpeexMode = 1;
		public const int SpeexQuality = 8;
		public const int SpeexComplexity = 1;
		public const int BitsPerSample = 16;
		public const int Channels = 1;
		public const int BytesPerSample = BitsPerSample / 8;
		public const int NarrowbandSamplesPerSecond = 8000;
		public const int WidebandSamplesPerSecond = 16000;
		public const int UltrawidebandSamplesPerSecond = 32000;
		public const int MillisecondsPerFrame = 20; // In milliseconds
		public const int FrameBufferSizeInShorts = 640;
	}
}
