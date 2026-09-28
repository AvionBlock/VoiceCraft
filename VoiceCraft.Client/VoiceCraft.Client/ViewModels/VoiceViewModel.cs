using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Client.ViewModels.Modals;
using VoiceCraft.Network;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.ViewModels;

public partial class VoiceViewModel(
    NotificationService notificationService,
    NavigationService navigationService,
    SettingsService settingsService,
    IBackgroundService backgroundService,
    ClipboardService clipboardService)
    : ViewModelBase, IDisposable
{
    private VoiceCraftClientService? _clientService;
    [ObservableProperty] public partial ObservableCollection<EntityDataViewModel> Entities { get; set; } = [];
    [ObservableProperty] public partial bool IsDeafened { get; set; }
    [ObservableProperty] public partial bool IsMuted { get; set; }
    [ObservableProperty] public partial bool IsServerDeafened { get; set; }
    [ObservableProperty] public partial bool IsServerMuted { get; set; }
    [ObservableProperty] public partial bool IsSpeaking { get; set; }
    [ObservableProperty] public partial string StatusTitleText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyDescriptionCommand))]
    public partial string StatusDescriptionText { get; set; } = string.Empty;

    public override bool DisableBackButton { get; protected set; } = true;

    private bool IsDescriptionCopyable()
    {
        return !string.IsNullOrWhiteSpace(StatusDescriptionText);
    }

    [RelayCommand]
    private void OpenEntity(EntityDataViewModel? entity)
    {
        if (entity == null) return;
        navigationService.PushModal<EntityDataSettingsViewModel>(new EntityDataSettingsNavigationData(entity));
    }

    [RelayCommand]
    private void ToggleMute()
    {
        if (_clientService == null) return;
        _clientService.Muted = !_clientService.Muted;
        IsMuted = _clientService.Muted;
    }

    [RelayCommand]
    private void ToggleDeafen()
    {
        if (_clientService == null) return;
        _clientService.Deafened = !_clientService.Deafened;
        IsDeafened = _clientService.Deafened;
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        if (_clientService == null || _clientService.ConnectionState == VcConnectionState.Disconnected)
        {
            navigationService.Back(); //If disconnected. Return to previous page.
            return;
        }

        await _clientService.DisconnectAsync("VoiceCraft.DisconnectReason.Manual");
    }

    [RelayCommand(CanExecute = nameof(IsDescriptionCopyable))]
    private async Task CopyDescription()
    {
        try
        {
            await clipboardService.SetTextAsync(StatusDescriptionText);
            notificationService.SendSuccessNotification("Voice.Notification.Badge", "Voice.Notification.Copied");
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification("Voice.Notification.Badge", ex.Message);
        }
    }
    
    public void Dispose()
    {
        if (_clientService != null)
        {
            _clientService.OnDisconnected -= OnDisconnected;
            _clientService.OnUpdateTitle -= OnUpdateTitle;
            _clientService.OnUpdateMute -= OnUpdateMute;
            _clientService.OnUpdateDeafen -= OnUpdateDeafen;
            _clientService.OnUpdateServerMute -= OnUpdateServerMute;
            _clientService.OnUpdateServerDeafen -= OnUpdateServerDeafen;
            _clientService.OnUpdateSpeaking -= OnUpdateSpeaking;
            _clientService.OnEntityAdded -= OnEntityAdded;
            _clientService.OnEntityRemoved -= OnEntityRemoved;
        }

        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        switch (data)
        {
            case VoiceNavigationData navigationData:
                SetService(navigationData.VoiceCraftClientService);
                break;
            case VoiceStartNavigationData startNavigationData:
                backgroundService.StartServiceAsync<VoiceCraftClientService>((x, updateTitle, updateDescription) =>
                {
                    using var disconnected = new ManualResetEventSlim(false);
                    try
                    {
                        SetService(x);
                        x.OnUpdateTitle += updateTitle;
                        x.OnUpdateDescription += updateDescription;
                        x.OnDisconnected += SignalDisconnected;
                        x.ConnectAsync(startNavigationData.Ip, startNavigationData.Port).GetAwaiter().GetResult();
                        while (x.ConnectionState == VcConnectionState.Connected)
                        {
                            disconnected.Wait(TimeSpan.FromSeconds(1));
                        }

                        SignalDisconnected();
                    }
                    finally
                    {
                        x.OnDisconnected -= SignalDisconnected;
                        x.OnUpdateTitle -= updateTitle;
                        x.OnUpdateDescription -= updateDescription;
                    }

                    return;

                    void SignalDisconnected()
                    {
                        disconnected.Set();
                    }
                }).ContinueWith(x =>
                {
                    if (x.Exception == null) return;
                    var flattened = x.Exception.Flatten();
                    var message = flattened.InnerException?.ToString() ?? flattened.ToString();
                    LogService.Log(flattened);
                    notificationService.SendErrorNotification(
                        "VoiceCraft.Notification.Badge",
                        message);
                    notificationService.SendNotification(
                        "VoiceCraft.Notification.Badge",
                        "VoiceCraft.Notification.Error");

                    OnDisconnected();
                });
                break;
        }
    }

    private void SetService(VoiceCraftClientService voiceCraftClientService)
    {
        _clientService = voiceCraftClientService;

        //Register events first.
        _clientService.OnDisconnected += OnDisconnected;
        _clientService.OnUpdateTitle += OnUpdateTitle;
        _clientService.OnUpdateDescription += OnUpdateDescription;
        _clientService.OnUpdateMute += OnUpdateMute;
        _clientService.OnUpdateDeafen += OnUpdateDeafen;
        _clientService.OnUpdateServerMute += OnUpdateServerMute;
        _clientService.OnUpdateServerDeafen += OnUpdateServerDeafen;
        _clientService.OnUpdateSpeaking += OnUpdateSpeaking;
        _clientService.OnEntityAdded += OnEntityAdded;
        _clientService.OnEntityRemoved += OnEntityRemoved;

        OnUpdateTitle(_clientService.Title);
        OnUpdateDescription(_clientService.Description);
        OnUpdateMute(_clientService.Muted);
        OnUpdateDeafen(_clientService.Deafened);
    }

    private void OnUpdateTitle(string title)
    {
        Dispatcher.UIThread.Invoke(() => { StatusTitleText = title; });
    }

    private void OnUpdateDescription(string description)
    {
        Dispatcher.UIThread.Invoke(() => { StatusDescriptionText = description; });
    }

    private void OnDisconnected()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_clientService != null)
            {
                _clientService.OnDisconnected -= OnDisconnected;
                _clientService.OnUpdateTitle -= OnUpdateTitle;
                _clientService.OnUpdateDescription -= OnUpdateDescription;
                _clientService.OnUpdateMute -= OnUpdateMute;
                _clientService.OnUpdateDeafen -= OnUpdateDeafen;
                _clientService.OnUpdateServerMute -= OnUpdateServerMute;
                _clientService.OnUpdateServerDeafen -= OnUpdateServerDeafen;
                _clientService.OnUpdateSpeaking -= OnUpdateSpeaking;
                _clientService.OnEntityAdded -= OnEntityAdded;
                _clientService.OnEntityRemoved -= OnEntityRemoved;
            }

            navigationService.Back();
        });
    }

    private void OnUpdateMute(bool muted)
    {
        Dispatcher.UIThread.Invoke(() => { IsMuted = muted; });
    }

    private void OnUpdateDeafen(bool deafened)
    {
        Dispatcher.UIThread.Invoke(() => { IsDeafened = deafened; });
    }

    private void OnUpdateServerMute(bool muted)
    {
        Dispatcher.UIThread.Invoke(() => { IsServerMuted = muted; });
    }

    private void OnUpdateServerDeafen(bool deafened)
    {
        Dispatcher.UIThread.Invoke(() => { IsServerDeafened = deafened; });
    }

    private void OnUpdateSpeaking(bool speaking)
    {
        Dispatcher.UIThread.Invoke(() => { IsSpeaking = speaking; });
    }

    private void OnEntityAdded(VoiceCraftClientEntity entity)
    {
        Dispatcher.UIThread.Invoke(() => { Entities.Add(new EntityDataViewModel(entity, settingsService)); });
    }

    private void OnEntityRemoved(VoiceCraftClientEntity entity)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var viewModel = Entities.FirstOrDefault(x => x.Entity == entity);
            if (viewModel == null) return;
            Entities.Remove(viewModel);
        });
    }
}