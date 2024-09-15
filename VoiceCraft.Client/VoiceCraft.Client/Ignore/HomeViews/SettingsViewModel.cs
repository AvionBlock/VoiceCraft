using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title { get => "Settings"; protected set => throw new NotSupportedException(); }
        //private AudioCapture capture = new AudioCapture() { WaveFormat = new NAudio.Wave.WaveFormat(48000, 1), BufferMilliseconds = 20 }; Deal with this later, OpenAL compilations for android absolutely destroy this.

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private bool _voiceSettingsExpanded = false;

        [ObservableProperty]
        private bool _behaviorSettingsExpanded = false;

        [ObservableProperty]
        private SettingsModel _settings;

        [ObservableProperty]
        private float _value = 0;

        [ObservableProperty]
        private ObservableCollection<string> _themes = new ObservableCollection<string>() { "Default", "Light", "Dark" };

        public SettingsViewModel(SettingsModel settings)
        {
            _settings = settings;

            Settings.PropertyChanged += (sender, ev) =>
            {
                _ = settings.SaveAsync(); //Inefficient but idc for now

                if (ev.PropertyName == nameof(Settings.SelectedTheme) && Application.Current != null)
                {
                    Application.Current.RequestedThemeVariant = new Avalonia.Styling.ThemeVariant(Settings.SelectedTheme, null);
                }

            };

            //capture.DataAvailable += Capture_DataAvailable;
        }

        /*
        private void Capture_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
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
                // is this the max value?
                if (sample32 > max) max = sample32;
            }

            Value = max;
        }
        */

        [RelayCommand]
        public void Test()
        {
            //capture.StartRecording();
        }
    }
}
