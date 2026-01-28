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
using VoiceCraft.Network;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.ViewModels;

public partial class VoiceViewModel(
    NavigationService navigationService,
    SettingsService settingsService,
    IBackgroundService backgroundService)
    : ViewModelBase, IDisposable
{
    private VoiceCraftService? _service;
    [ObservableProperty] private ObservableCollection<EntityDataViewModel> _entityViewModels = [];
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isServerDeafened;
    [ObservableProperty] private bool _isServerMuted;
    [ObservableProperty] private bool _isSpeaking;
    [ObservableProperty] private EntityDataViewModel? _selectedEntity;
    [ObservableProperty] private bool _showModal;
    [ObservableProperty] private string _statusDescriptionText = string.Empty;

    [ObservableProperty] private string _statusTitleText = string.Empty;
    public override bool DisableBackButton { get; protected set; } = true;

    public void Dispose()
    {
        if (_service != null)
        {
            _service.OnDisconnected -= OnDisconnected;
            _service.OnUpdateTitle -= OnUpdateTitle;
            _service.OnUpdateMute -= OnUpdateMute;
            _service.OnUpdateDeafen -= OnUpdateDeafen;
            _service.OnUpdateServerMute -= OnUpdateServerMute;
            _service.OnUpdateServerDeafen -= OnUpdateServerDeafen;
            _service.OnUpdateSpeaking -= OnUpdateSpeaking;
            _service.OnEntityAdded -= OnEntityAdded;
            _service.OnEntityRemoved -= OnEntityRemoved;
        }

        GC.SuppressFinalize(this);
    }

    partial void OnIsMutedChanged(bool value)
    {
        if (_service != null)
            _service.Muted = value;
    }

    partial void OnIsDeafenedChanged(bool value)
    {
        if (_service != null)
            _service.Deafened = value;
    }

    partial void OnSelectedEntityChanged(EntityDataViewModel? value)
    {
        if (value == null)
        {
            ShowModal = false;
            return;
        }

        ShowModal = true;
    }

    [RelayCommand]
    private async Task Disconnect()
    {
        if (_service == null || _service.ConnectionState == VcConnectionState.Disconnected)
        {
            navigationService.Back(); //If disconnected. Return to previous page.
            return;
        }

        await _service.DisconnectAsync("VoiceCraft.DisconnectReason.Manual");
    }

    public override void OnAppearing(object? data = null)
    {
        switch (data)
        {
            case VoiceNavigationData navigationData:
                SetService(navigationData.VoiceCraftService);
                break;
            case VoiceStartNavigationData startNavigationData:
                backgroundService.StartServiceAsync<VoiceCraftService>((x, updateTitle, updateDescription) =>
                {
                    SetService(x);
                    try
                    {
                        x.OnUpdateTitle += updateTitle;
                        x.OnUpdateDescription += updateDescription;
                        x.ConnectAsync(startNavigationData.Ip, startNavigationData.Port).GetAwaiter().GetResult();
                        var sw = new SpinWait();
                        while (x.ConnectionState == VcConnectionState.Connected)
                        {
                            sw.SpinOnce();
                        }
                    }
                    finally
                    {
                        x.OnUpdateTitle -= updateTitle;
                        x.OnUpdateDescription -= updateDescription;
                    }
                });
                break;
        }
    }

    private void SetService(VoiceCraftService service)
    {
        _service = service;

        //Register events first.
        _service.OnDisconnected += OnDisconnected;
        _service.OnUpdateTitle += OnUpdateTitle;
        _service.OnUpdateDescription += OnUpdateDescription;
        _service.OnUpdateMute += OnUpdateMute;
        _service.OnUpdateDeafen += OnUpdateDeafen;
        _service.OnUpdateServerMute += OnUpdateServerMute;
        _service.OnUpdateServerDeafen += OnUpdateServerDeafen;
        _service.OnUpdateSpeaking += OnUpdateSpeaking;
        _service.OnEntityAdded += OnEntityAdded;
        _service.OnEntityRemoved += OnEntityRemoved;

        StatusTitleText = _service.Title;
        StatusDescriptionText = _service.Description;
        IsMuted = _service.Muted;
        IsDeafened = _service.Deafened;
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
            if (_service != null)
            {
                _service.OnDisconnected -= OnDisconnected;
                _service.OnUpdateTitle -= OnUpdateTitle;
                _service.OnUpdateDescription -= OnUpdateDescription;
                _service.OnUpdateMute -= OnUpdateMute;
                _service.OnUpdateDeafen -= OnUpdateDeafen;
                _service.OnUpdateServerMute -= OnUpdateServerMute;
                _service.OnUpdateServerDeafen -= OnUpdateServerDeafen;
                _service.OnUpdateSpeaking -= OnUpdateSpeaking;
                _service.OnEntityAdded -= OnEntityAdded;
                _service.OnEntityRemoved -= OnEntityRemoved;
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
        Dispatcher.UIThread.Invoke(() => { EntityViewModels.Add(new EntityDataViewModel(entity, settingsService)); });
    }

    private void OnEntityRemoved(VoiceCraftClientEntity entity)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var viewModel = EntityViewModels.FirstOrDefault(x => x.Entity == entity);
            if (viewModel == null) return;
            EntityViewModels.Remove(viewModel);
        });
    }
}