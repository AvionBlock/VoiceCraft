using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class HostServerViewModel : ViewModelBase, IDisposable
{
    private readonly NotificationService _notificationService;
    private readonly IBackgroundService _backgroundService;
    private readonly HostServerSettings _hostServerSettings;
    private readonly NavigationService _navigationService;

    private VoiceCraftServerService? _serverService;

    [ObservableProperty] public partial string ServerProperties { get; set; }

    [ObservableProperty] public partial bool IsRunning { get; set; }

    public HostServerViewModel(
        NotificationService notificationService,
        IBackgroundService backgroundService,
        SettingsService settingsService,
        NavigationService navigationService)
    {
        _notificationService = notificationService;
        _backgroundService = backgroundService;
        _hostServerSettings = settingsService.HostServerSettings;
        _navigationService = navigationService;

        ServerProperties = JsonSerializer.Serialize<Server.ServerPropertiesStructure>(
            _hostServerSettings.ServerProperties,
            Server.ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
    }

    partial void OnServerPropertiesChanging(string value)
    {
        var serverProperties = JsonSerializer.Deserialize<Server.ServerPropertiesStructure>(value,
            Server.ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
        if (serverProperties == null)
            throw new ArgumentException();

        _hostServerSettings.ServerProperties = serverProperties;
    }

    [RelayCommand]
    private void ResetProperties()
    {
        try
        {
            ServerProperties = JsonSerializer.Serialize<Server.ServerPropertiesStructure>(
                new Server.ServerPropertiesStructure(),
                Server.ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
            _notificationService.SendSuccessNotification(
                "HostServer.Notification.Badge",
                $"HostServer.Notification.ResetProperties");
        }
        catch (Exception ex)
        {
            _notificationService.SendErrorNotification(
                "VoiceCraft.Notification.Badge",
                ex.Message);
        }
    }

    [RelayCommand]
    private void ViewConsole()
    {
        _navigationService.NavigateTo<HostServerConsoleViewModel>();
    }

    [RelayCommand]
    private async Task ToggleServer()
    {
        var serverService = _backgroundService.GetService<VoiceCraftServerService>();
        if (serverService == null)
        {
            await StartServer();
        }
        else
        {
            await StopServer(serverService);
        }
    }

    public void Dispose()
    {
        ClearService();
        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        var service = _backgroundService.GetService<VoiceCraftServerService>();
        if (service == null) return;
        SetService(service);
    }

    private async Task StartServer()
    {
        try
        {
            await _backgroundService.StartServiceAsync<VoiceCraftServerService>((x, updateTitle, updateDescription) =>
            {
                SetService(x);
                var runtimeOptions = new Server.RuntimeOptions()
                {
                    DisableCommands = true,
                    DisableColor = true,
                    DisableAnsi = true,
                    FailFast = true,

                    ServerProperties = _hostServerSettings.ServerProperties
                };
                x.StartAsync(runtimeOptions).GetAwaiter().GetResult();
            });
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            _notificationService.SendErrorNotification(
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
            _notificationService.SendErrorNotification(
                "HostServer.Notification.Badge",
                ex.Message);
        }
    }

    private void SetService(VoiceCraftServerService voiceCraftServerService)
    {
        ClearService();
        _serverService = voiceCraftServerService;

        _serverService.OnStarted += OnStarted;
        _serverService.OnStopped += OnStopped;

        IsRunning = _serverService.IsRunning;
    }

    private void ClearService()
    {
        if (_serverService == null) return;
        var serverService = _serverService;
        _serverService = null;

        serverService.OnStarted -= OnStarted;
        serverService.OnStopped -= OnStopped;
    }

    private void OnStarted()
    {
        Dispatcher.UIThread.Invoke(() => IsRunning = true);
    }

    private void OnStopped(Exception? ex)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            IsRunning = false;
            if (ex == null) return;
            LogService.Log(ex);
            _notificationService.SendErrorNotification(
                "VoiceCraft.Notification.Badge",
                ex.Message);
        });
    }
}