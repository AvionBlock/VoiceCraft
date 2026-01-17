using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.World;
using VoiceCraft.Network;

namespace VoiceCraft.Client.ViewModels;

public partial class VoiceViewModel(NavigationService navigationService, VoiceCraftService service)
    : ViewModelBase, IDisposable
{
    private readonly VoiceCraftService _service = service;
    [ObservableProperty] private ObservableCollection<EntityViewModel> _entityViewModels = [];
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isServerDeafened;
    [ObservableProperty] private bool _isServerMuted;
    [ObservableProperty] private bool _isSpeaking;
    [ObservableProperty] private EntityViewModel? _selectedEntity;
    [ObservableProperty] private bool _showModal;
    [ObservableProperty] private string _statusDescriptionText = string.Empty;
    [ObservableProperty] private string _statusTitleText = string.Empty;
    public override bool DisableBackButton { get; protected set; } = true;

    public void Dispose()
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

        GC.SuppressFinalize(this);
    }

    partial void OnIsMutedChanged(bool value)
    {
        _service.Muted = value;
    }

    partial void OnIsDeafenedChanged(bool value)
    {
        _service.Deafened = value;
    }

    partial void OnSelectedEntityChanged(EntityViewModel? value)
    {
        if (value == null)
        {
            ShowModal = false;
            return;
        }

        ShowModal = true;
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (_service.ConnectionState == VcConnectionState.Disconnected)
        {
            navigationService.Back(); //If disconnected. Return to previous page.
            return;
        }

        await _service.DisconnectAsync();
    }

    public override void OnAppearing(object? data = null)
    {
        if (_service.ConnectionState == VcConnectionState.Disconnected)
        {
            navigationService.Back();
            return;
        }

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

    private void OnEntityAdded(VoiceCraftEntity entity)
    {
        Dispatcher.UIThread.Invoke(() => { EntityViewModels.Add(entity); });
    }

    private void OnEntityRemoved(VoiceCraftEntity entity)
    {
        Dispatcher.UIThread.Invoke(() => { EntityViewModels.Remove(entity); });
    }
}