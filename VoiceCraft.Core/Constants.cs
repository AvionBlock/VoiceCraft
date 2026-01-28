using System;

namespace VoiceCraft.Core
{
    public static class Constants
    {
        public const int Major = 1; //These need to be the same on both client and server!
        public const int Minor = 4; //These need to be the same on both client and server!
        public const int Patch = 0; //This does not need to be the same on client and server.

        //Tick
        public const int TickRate = 50;

        //Limits
        public const int FileWritingDelay = 2000;
        public const int MaxStringLength = 100; //100 characters.
        public const int MaxDescriptionStringLength = 500; //500 characters.
        public const float FloatingPointTolerance = 0.001f;
        public const int MaximumEncodedBytes = 1000; //1000 bytes of allocation for encoding.
        public const int MaxPacketPoolSize = 100; //100 Packets Per type.

        //Audio
        public const AudioFormat Format = AudioFormat.Pcm16;
        public const int BitDepth = 16;
        public const int SampleRate = 48000;
        public const int Channels = 1;
        public const int FrameSizeMs = 20;
        public const int SilenceThresholdMs = 200; //200ms silence threshold.
        //Enough to hold 1 second of audio. BECAUSE CERTAIN PLATFORMS FAIL TO WORK WITH SMALL BUFFERS!
        public const int OutputBufferSizeMs = FrameSizeMs * 50;
        // Prefill buffer before we start releasing into the audio driver to prevent hitches.
        public const int PrefillBufferSizeMs = FrameSizeMs * 2;
        //Audio Calculations
        public const int SamplesPerFrame = SampleRate / 1000 * FrameSizeMs; //960 samples per frame.
        public const int OutputBufferSamples = SampleRate / 1000 * OutputBufferSizeMs;
        public const int PrefillBufferSamples = SampleRate / 1000 * PrefillBufferSizeMs;

        //Default Country Code & Fallback
        public const string DefaultLanguage = "en-US";

        //Storage
        public const string ApplicationDirectory = "voicecraft";
        public const string SettingsFile = "Settings.json";
        public const string ExceptionLogsFile = "ExceptionLogs.json";

        //RPC
        public const string ApplicationId = "1364434932968984669";
        public const string GithubButton = "VoiceCraft";
        public const string GithubButtonUrl = "https://github.com/AvionBlock/VoiceCraft";
        public const string LargeImageKey = "vc";
        public const string LargeImageText = "VoiceCraft";

        //Settings GUIDS.
        //Speex DSP
        public static readonly Guid SpeexDspEchoCancelerGuid = Guid.Parse("b4844eca-d5c0-497a-9819-7e4fa9ffa7ed");

        public static readonly Guid SpeexDspAutomaticGainControllerGuid =
            Guid.Parse("AE3F02FF-41A7-41FD-87A0-8EB0DA82B21C");

        public static readonly Guid SpeexDspDenoiserGuid = Guid.Parse("6E911874-5D10-4C8C-8E0A-6B30DF16EF78");

        //Background Images
        public static readonly Guid DockNightGuid = Guid.Parse("6b023e19-c9c5-4e06-84df-22833ccccd87");
        public static readonly Guid DockDayGuid = Guid.Parse("7c615c28-33b7-4d1d-b530-f8d988b00ea1");
        public static readonly Guid LethalCraftGuid = Guid.Parse("8d7616ce-cc2e-45af-a1c0-0456c09b998c");
        public static readonly Guid BlockSenseSpawnGuid = Guid.Parse("EDC317D4-687D-4607-ABE6-9C14C29054E9");
        public static readonly Guid SineSmpBaseGuid = Guid.Parse("3FAD5542-64F2-4A00-A4C2-534A517CCDE1");

        //Themes
        public static readonly Guid DarkThemeGuid = Guid.Parse("cf8e39fe-21cc-4210-91e6-d206e22ca52e");
        public static readonly Guid LightThemeGuid = Guid.Parse("3aeb95bc-a749-40f0-8f45-9f9070b76125");
        public static readonly Guid DarkPurpleThemeGuid = Guid.Parse("A59F5C67-043E-4052-A060-32D3DCBD43F7");
        public static readonly Guid DarkGreenThemeGuid = Guid.Parse("66BA4F00-C61C-4C04-A62B-CE4277679F14");
    }
}