using System;

namespace VoiceCraft.Core.Audio.Filters
{
	public class AudioFormat : IEquatable<AudioFormat>
	{
		public AudioFormat(
			int samplesPerSecond = AudioConstants.WidebandSamplesPerSecond,
			int millisecondsPerFrame = AudioConstants.MillisecondsPerFrame,
			int channels = AudioConstants.Channels,
			int bitsPerSample = AudioConstants.BitsPerSample)
		{
			BitsPerSample = bitsPerSample;
			Channels = channels;
			BytesPerSample = BitsPerSample / 8;
			SamplesPerSecond = samplesPerSecond;
			MillisecondsPerFrame = millisecondsPerFrame;
			SamplesPerFrame = (SamplesPerSecond * Channels / (1000 / MillisecondsPerFrame));
			SamplesPer10Ms = (SamplesPerSecond * Channels / (1000 / 10));
			BytesPerFrame = SamplesPerFrame * (AudioConstants.BitsPerSample / 8);
			FramesPerSecond = 1000 / MillisecondsPerFrame;
			MaxQueuedAudioFrames = 1000 / MillisecondsPerFrame; // i.e., 50 frames, or one second worth.
		}

		// Primary values
		public readonly int SamplesPerSecond;
		public readonly int MillisecondsPerFrame;

		// Derived values 
		public readonly int SamplesPerFrame;
		public readonly int SamplesPer10Ms;
		public readonly int BytesPerFrame;
		public readonly int FramesPerSecond;
		public readonly int MaxQueuedAudioFrames;

		// Const values (but reproduced here for the sake of consistency)
		public readonly int BitsPerSample;
		public readonly int Channels;
		public readonly int BytesPerSample;

		/// <summary>
		/// The audio format at which the playback system expects to be receiving the audio.
		/// </summary>
		public static AudioFormat Default
		{
			get { return defaultFormat ?? (defaultFormat = new AudioFormat()); }
		}
		private static AudioFormat defaultFormat;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			if (obj.GetType() != typeof(AudioFormat))
			{
				return false;
			}
			return Equals((AudioFormat)obj);
		}

		public bool Equals(AudioFormat other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			return other.SamplesPerSecond == SamplesPerSecond &&
				other.MillisecondsPerFrame == MillisecondsPerFrame &&
				other.SamplesPerFrame == SamplesPerFrame &&
				other.BytesPerFrame == BytesPerFrame &&
				other.FramesPerSecond == FramesPerSecond &&
				other.MaxQueuedAudioFrames == MaxQueuedAudioFrames &&
				other.BitsPerSample == BitsPerSample &&
				other.Channels == Channels &&
				other.BytesPerSample == BytesPerSample;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = SamplesPerSecond;
				result = (result * 397) ^ MillisecondsPerFrame;
				result = (result * 397) ^ SamplesPerFrame;
				result = (result * 397) ^ BytesPerFrame;
				result = (result * 397) ^ FramesPerSecond;
				result = (result * 397) ^ MaxQueuedAudioFrames;
				result = (result * 397) ^ BitsPerSample;
				result = (result * 397) ^ Channels;
				result = (result * 397) ^ BytesPerSample;
				return result;
			}
		}

		public static bool operator ==(AudioFormat left, AudioFormat right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(AudioFormat left, AudioFormat right)
		{
			return !Equals(left, right);
		}
	}
}
