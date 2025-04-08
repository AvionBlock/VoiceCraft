using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using OpenTK.Audio.OpenAL.Native;
using OpenTK.Core;
using OpenTK.Core.Native;

using OpenTK.Audio.OpenAL;
// using VoiceCraft.Client.Browser.Audio.OpenAL;

namespace VoiceCraft.Client.Browser.Audio
{
    public class NativeAudioService : AudioService
    {
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
            var devices_js = EmbedInteropRecord.getDevices();
            var devices = new List<string>();

            for (int idx = 0; idx < devices_js.Length; idx++)
            {
                if (idx % 2 == 1) {
                    // Skip ids
                    continue;
                }

                var deviceName = $"{devices_js[idx].Truncate(8)}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }

            return devices;
        // var list = new List<string>();
        //
        // var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.CaptureDeviceSpecifier);
        // list.AddRange(devices);
        // 
        // return list;
        }

        public override List<string> GetOutputDevices()
        {
            var devices = new List<string>();
            devices.Add("im a output default");

            // var audioDevices = _audioManager.GetDevices(GetDevicesTargets.Outputs)?.Where(x => !DeniedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            // if (audioDevices == null) return devices;
            //
            // foreach (var audioDevice in audioDevices)
            // {
            //     var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
            //     if (!devices.Contains(deviceName))
            //         devices.Add(deviceName);
            // }
            return devices;
        }
    }

    // static partial class EmbedInterop
    // {
    //     [JSImport("constructStream", "audio_recorder.js")]
    //     public static partial string GetString([In] ALDevice device, AlcGetString param);
    //
    // }
}
