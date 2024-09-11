using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VoiceCraft.Client.Models;

namespace VoiceCraft.Client.ViewModels.HomeViews
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title { get => "Settings"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private bool _voiceSettingsExpanded = false;

        [ObservableProperty]
        private bool _behaviorSettingsExpanded = false;

        [ObservableProperty]
        private SettingsModel _settings;

        public SettingsViewModel(SettingsModel settings)
        {
            _settings = settings;

            Settings.PropertyChanged += (sender, ev) =>
            {
                _ = settings.SaveAsync(); //Inefficient but idc for now
            };
        }
    }
}
