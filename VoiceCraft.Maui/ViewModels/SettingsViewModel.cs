using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Interfaces;
using NAudio.Wave;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAudioManager _audioManager;

        [ObservableProperty]
        SettingsModel settings;

        [ObservableProperty]
        ObservableCollection<string> inputDevices = ["Default"];
        
        [ObservableProperty]
        ObservableCollection<string> outputDevices = ["Default"];

        [ObservableProperty]
        float microphoneDetection;

        [ObservableProperty]
        bool isRecording = false;

        private IWaveIn? Microphone;
        private readonly WaveFormat AudioFormat = new(48000, 1);

        public SettingsViewModel(IDatabaseService databaseService, IAudioManager audioManager)
        {
            _databaseService = databaseService;
            _audioManager = audioManager;
            
            Settings = _databaseService.Settings;

            foreach(var device in _audioManager.GetInputDevices())
                InputDevices.Add(device);

            foreach (var device in _audioManager.GetOutputDevices())
                OutputDevices.Add(device);

            if (Settings.InputDevice > _audioManager.GetInputDeviceCount())
            {
                Settings.InputDevice = 0;
            }
            if (Settings.OutputDevice > _audioManager.GetOutputDeviceCount())
            {
                Settings.OutputDevice = 0;
            }
        }

        [RelayCommand]
        public void SaveSettings()
        {
            if(Microphone != null)
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
            _ = _databaseService.SaveSettings();
        }

        [RelayCommand]
        public async Task OpenCloseMicrophone()
        {
            if (Microphone == null)
            {
                if (!await _audioManager.RequestInputPermissions()) return;
                Microphone = _audioManager.CreateRecorder(AudioFormat, 20);
                Microphone.DataAvailable += Microphone_DataAvailable;
                Microphone.StartRecording();
                IsRecording = true;
            }
            else
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
        }

        private void Microphone_DataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            MicrophoneDetection = max;
        }
    }
}
