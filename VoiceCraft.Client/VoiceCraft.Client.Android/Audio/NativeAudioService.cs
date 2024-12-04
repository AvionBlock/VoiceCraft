using Android.Media;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAudioService(AudioManager audioManager) : AudioService
    {
        //This may or may not include bugged devices that can crash the application.
        private static readonly AudioDeviceType[] AllowedDeviceTypes = [
            AudioDeviceType.AuxLine,
            AudioDeviceType.BluetoothA2dp,
            AudioDeviceType.BluetoothSco,
            AudioDeviceType.BuiltinMic,
            AudioDeviceType.BuiltinEarpiece,
            AudioDeviceType.BuiltinSpeaker,
            AudioDeviceType.Dock,
            AudioDeviceType.Hdmi,
            AudioDeviceType.HdmiArc,
            AudioDeviceType.LineAnalog,
            AudioDeviceType.LineDigital,
            AudioDeviceType.UsbAccessory,
            AudioDeviceType.UsbDevice,
            AudioDeviceType.WiredHeadphones,
            AudioDeviceType.WiredHeadset
            ];

        public override IAudioRecorder CreateAudioRecorder()
        {
            return new AudioRecorder(audioManager);
        }

        public override IAudioPlayer CreateAudioPlayer()
        {
            return new AudioPlayer(audioManager);
        }

        public override string GetDefaultInputDevice()
        {
            return "Default";
        }

        public override string GetDefaultOutputDevice()
        {
            return "Default";
        }

        public override List<string> GetInputDevices()
        {
            var devices = new List<string>() { GetDefaultInputDevice() };

            var audioDevices = audioManager.GetDevices(GetDevicesTargets.Inputs)?.Where(x => AllowedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            if (audioDevices == null) return devices;

            foreach (var audioDevice in audioDevices)
            {
                var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }
            return devices;
        }

        public override List<string> GetOutputDevices()
        {
            var devices = new List<string>() { GetDefaultOutputDevice() };

            var audioDevices = audioManager.GetDevices(GetDevicesTargets.Outputs)?.Where(x => AllowedDeviceTypes.Contains(x.Type)); //Don't ask. this is the only way to stop users from selecting a device that completely annihilates the app.
            if (audioDevices == null) return devices;

            foreach (var audioDevice in audioDevices)
            {
                var deviceName = $"{audioDevice.ProductName.Truncate(8)} - {audioDevice.Type}";
                if (!devices.Contains(deviceName))
                    devices.Add(deviceName);
            }
            return devices;
        }
    }
}