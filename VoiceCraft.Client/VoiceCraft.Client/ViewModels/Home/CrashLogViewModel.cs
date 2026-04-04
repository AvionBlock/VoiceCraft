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
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CrashLogViewModel(NotificationService notificationService) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<KeyValuePair<DateTime, string>> _crashLogs = [];

    [RelayCommand]
    private async Task CopyLogs()
    {
        try
        {
            if (CrashLogs.Count <= 0)
            {
                notificationService.SendNotification(
                    Localizer.Get("Notification.CrashLogs.Empty"),
                    Localizer.Get("Notification.CrashLogs.Badge"));
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
                Localizer.Get("Notification.CrashLogs.Copied"),
                Localizer.Get("Notification.CrashLogs.Badge"));
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(ex.Message);
        }
    }

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
