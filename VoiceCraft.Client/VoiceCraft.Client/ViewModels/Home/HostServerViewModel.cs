using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Server;
using LogService = VoiceCraft.Client.Services.LogService;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class HostServerViewModel(
    NotificationService notificationService,
    IBackgroundService backgroundService) : ViewModelBase
{
    private VoiceCraftServerService? _serverService;

    [ObservableProperty]
    public partial string ServerProperties { get; set; } = JsonSerializer.Serialize<ServerPropertiesStructure>(
        new ServerPropertiesStructure(), ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);

    [ObservableProperty] public partial bool IsHosting { get; set; }

    [RelayCommand]
    private async Task ToggleServer()
    {
        var serverService = backgroundService.GetService<VoiceCraftServerService>();
        if (serverService == null)
        {
            await StartServer();
        }
        else
        {
            await StopServer(serverService);
        }
    }

    private async Task StartServer()
    {
        try
        {
            await backgroundService.StartServiceAsync<VoiceCraftServerService>((x, updateTitle, updateDescription) =>
            {
                SetService(x);
                var runtimeOptions = new Server.RuntimeOptions()
                {
                    DisableCommands = true,
                    DisableColor = true,
                    DisableAnsi = true,
                    FailFast = true
                };
                x.StartAsync(runtimeOptions).GetAwaiter().GetResult();
            });
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            notificationService.SendErrorNotification(
                "HostServer.Notification.Badge",
                ex.Message);
        }
    }

    private async Task StopServer(VoiceCraftServerService serverService)
    {
        try
        {
            await serverService.StopAsync();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            notificationService.SendErrorNotification(
                "HostServer.Notification.Badge",
                ex.Message);
        }
    }

    private void SetService(VoiceCraftServerService voiceCraftServerService)
    {
        _serverService = voiceCraftServerService;

        //Register events first.
        _serverService.OnStarted += OnStarted;
        _serverService.OnStopped += OnStopped;
    }

    private void OnStarted()
    {
        Dispatcher.UIThread.Invoke(() => IsHosting = true);
    }

    private void OnStopped(Exception? ex)
    {
        Dispatcher.UIThread.Invoke(() => IsHosting = false);
        if (ex == null) return;
        LogService.Log(ex);
        notificationService.SendErrorNotification(
            "VoiceCraft.Notification.Badge",
            ex.Message);
    }
}