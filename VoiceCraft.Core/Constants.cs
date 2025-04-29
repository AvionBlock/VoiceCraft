using System;

namespace VoiceCraft.Core
{
    public static class Constants
    {
        //Tick
        public const int TickRate = 50;
        
        //Limits
        public const int FileWritingDelay = 2000;
        public const int MaxStringLength = 100; //100 characters.
        public const float FloatingPointTolerance = 0.001f;
        public const int MaximumEncodedBytes = 1000; //1000 bytes of allocation for encoding.
        
        //Audio
        public const AudioFormat Format = AudioFormat.Pcm16;
        public const int BitDepth = 16;
        public const int SampleRate = 48000;
        public const int Channels = 1;
        public const int FrameSizeMs = 20;
        public const int SilenceThresholdMs = 200; //200ms silence threshold.
        public const int DecodeBufferSizeThresholdMs = FrameSizeMs * 3; //Enough to hold 3 full packets.
        
        //Audio Calculations
        public const int SamplesPerFrame = SampleRate / 1000 * FrameSizeMs; //960 samples per frame.
        public const int BytesPerFrame = BitDepth / 8 * Channels * SamplesPerFrame; //16-bit byte audio. this works out to 1920
        public const int BlockAlign = Channels * (BitDepth / 8);
        
        //Storage
        public const string ApplicationDirectory = "voicecraft";
        public const string SettingsFile = "Settings.json";
        public const string CrashLogsFile = "CrashLogs.json";
        
        //Settings GUIDS.
        //Speex DSP
        public static readonly Guid SpeexDspEchoCancelerGuid = Guid.Parse("b4844eca-d5c0-497a-9819-7e4fa9ffa7ed");
        public static readonly Guid SpeexDspAutomaticGainControllerGuid = Guid.Parse("AE3F02FF-41A7-41FD-87A0-8EB0DA82B21C");
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