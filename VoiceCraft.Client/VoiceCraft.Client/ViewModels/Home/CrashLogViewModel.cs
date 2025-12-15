using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CrashLogViewModel(NotificationService notificationService) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<KeyValuePair<DateTime, string>> _crashLogs = [];

    [RelayCommand]
    private void ClearLogs()
    {
        try
        {
            LogService.ClearCrashLogs();
            CrashLogs.Clear();
            notificationService.SendSuccessNotification(Localizer.Get("Notification.CrashLogs.Cleared"),
                Localizer.Get("Notification.CrashLogs.Badge"));
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(ex.Message);
        }
    }

    public override void OnAppearing(object? data = null)
    {
        CrashLogs = new ObservableCollection<KeyValuePair<DateTime, string>>(LogService.CrashLogs);
    }
}