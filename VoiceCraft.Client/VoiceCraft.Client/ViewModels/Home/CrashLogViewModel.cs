using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CrashLogViewModel(NotificationService notificationService) : ViewModelBase
{
    [ObservableProperty] public partial ObservableCollection<KeyValuePair<DateTime, string>> CrashLogs { get; set; } = [];

    [RelayCommand]
    private async Task CopyLogs()
    {
        try
        {
            if (CrashLogs.Count <= 0)
            {
                notificationService.SendNotification(
                    "CrashLogs.Notification.Badge",
                    "CrashLogs.Notification.Empty");
                return;
            }

            var text = new StringBuilder();
            foreach (var crashLog in CrashLogs.OrderByDescending(x => x.Key))
            {
                _ = text.AppendLine($"[{crashLog.Key:O}]");
                _ = text.AppendLine(crashLog.Value);
                _ = text.AppendLine();
            }

            await Clipboard.Default.SetTextAsync(text.ToString().Trim());
            notificationService.SendSuccessNotification(
                "CrashLogs.Notification.Badge",
                "CrashLogs.Notification.Copied");
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                "CrashLogs.Notification.Badge",
                ex.Message);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        try
        {
            LogService.ClearCrashLogs();
            CrashLogs.Clear();
            notificationService.SendSuccessNotification(
                "CrashLogs.Notification.Badge",
                "CrashLogs.Notification.Cleared");
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                "CrashLogs.Notification.Badge",
                ex.Message);
        }
    }

    public override void OnAppearing(object? data = null)
    {
        CrashLogs = new ObservableCollection<KeyValuePair<DateTime, string>>(LogService.CrashLogs);
    }
}
