using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;

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
            notificationService.SendSuccessNotification(Locales.Locales.Notification_Badges_CrashLogs, "Successfully cleared all logs.");
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