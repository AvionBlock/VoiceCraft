using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace VoiceCraft.Client.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public override string Title { get => "Settings"; protected set => throw new NotSupportedException(); }
    }
}
