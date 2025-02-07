using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class CrashLogViewModel : ViewModelBase
    {
        [ObservableProperty] private ObservableCollection<KeyValuePair<DateTime, string>> _crashLogs = [];
        
        public override void OnAppearing()
        {
            CrashLogs = new ObservableCollection<KeyValuePair<DateTime, string>>(CrashLogService.CrashLogs.OrderByDescending(x => x.Key));
        }
    }
}