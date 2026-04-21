using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Diagnostics;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CrashLogViewModel(
    NotificationService notificationService,
    ClipboardService clipboardService) : ViewModelBase
{
    [ObservableProperty] public partial ObservableCollection<KeyValuePair<DateTime, CrashLogRecord>> CrashLogs { get; set; } = [];

    private static async Task<string?> UploadCrashLog(DateTime timeStamp)
    {
        if (!LogService.TryGetCrashLog(timeStamp, out var crashLog)) return null;
        if (!string.IsNullOrWhiteSpace(crashLog.DumpUrl))
            return crashLog.DumpUrl;
        
        var dumpResponse = await ClientTelemetryService.ReportCrashAsync(crashLog.Message);
        var dumpUrl = dumpResponse?.ViewUrl ?? dumpResponse?.Url;
        if (string.IsNullOrWhiteSpace(dumpUrl))
            return null;

        crashLog.DumpUrl = dumpUrl;
        LogService.UpdateCrashLog(timeStamp, crashLog);
        return dumpUrl;
    }

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
                _ = text.AppendLine(crashLog.Value.Message);
                if (!string.IsNullOrWhiteSpace(crashLog.Value.DumpUrl))
                    _ = text.AppendLine($"Dump: {crashLog.Value.DumpUrl}");
                _ = text.AppendLine();
            }

            await clipboardService.SetTextAsync(text.ToString().Trim());
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
    private async Task CopyDumpLink(KeyValuePair<DateTime, CrashLogRecord>? crashLog)
    {
        try
        {
            if (crashLog is null)
            {
                notificationService.SendNotification(
                    "CrashLogs.Notification.Badge",
                    "CrashLogs.Notification.DumpUnavailable");
                return;
            }

            var dumpUrl = await UploadCrashLog(crashLog.Value.Key);
            if (string.IsNullOrWhiteSpace(dumpUrl))
            {
                notificationService.SendNotification(
                    "CrashLogs.Notification.Badge",
                    "CrashLogs.Notification.DumpUploadFailed");
                return;
            }

            await clipboardService.SetTextAsync(dumpUrl);
            notificationService.SendSuccessNotification(
                "CrashLogs.Notification.Badge",
                "CrashLogs.Notification.DumpCopied");
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
        CrashLogs = new ObservableCollection<KeyValuePair<DateTime, CrashLogRecord>>(LogService.CrashLogs);
    }
}
