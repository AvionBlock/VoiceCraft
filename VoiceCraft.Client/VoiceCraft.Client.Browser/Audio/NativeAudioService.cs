using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using OpenTK.Audio.OpenAL.Native;
using OpenTK.Core;
using OpenTK.Core.Native;

using OpenTK.Audio.OpenAL;
using OpusSharp.Core;

using System.Diagnostics.CodeAnalysis;

namespace VoiceCraft.Client.Browser.Audio
{
    // [DynamicDependency("alcGetString")]
    public class NativeAudioService : AudioService
    {
        internal const CallingConvention AlcCallingConv = CallingConvention.Cdecl;
        [DllImport("openal", EntryPoint = "alcGetString", ExactSpelling = true, CallingConvention = AlcCallingConv, CharSet = CharSet.Ansi)]
        private static unsafe extern byte* a([In] ALDevice device, AlcGetString param);

        public override IAudioRecorder CreateAudioRecorder()
        {
            return new AudioRecorder();
        }

        public override IAudioPlayer CreateAudioPlayer()
        {
            return new AudioPlayer();
        }

        public override List<string> GetInputDevices()
        {
            var list = new List<string>();

            var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.CaptureDeviceSpecifier);
            list.AddRange(devices);

            return list;
        }

        public override List<string> GetOutputDevices()
        {
            var list = new List<string>();

            var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.AllDevicesSpecifier);
            list.AddRange(devices);

            return list;
        }
    }
}
