using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class AudioSettings : Setting
    {
        public string InputDevice = "Default";
        public string OutputDevice = "Default";
        public Guid Preprocessor = Guid.Empty;
        public Guid EchoCanceler = Guid.Empty;
        public float MicrophoneSensitivity = 0.04f;
        public bool AEC = false;
        public bool AGC = false;
        public bool Denoiser = false;
        public bool VAD = false;

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}