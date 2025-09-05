using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels;

public partial class VoiceViewModel(NavigationService navigationService) : ViewModelBase, IDisposable
{
    [ObservableProperty] private EntityViewModel? _selectedEntity;
    [ObservableProperty] private ObservableCollection<EntityViewModel> _entityViewModels = [];
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isSpeaking;
    [ObservableProperty] private bool _showModal;
    private VoipBackgroundProcess? _process;

    [ObservableProperty] private string _statusTitleText = string.Empty;
    [ObservableProperty] private string _statusDescriptionText = string.Empty;
    public override bool DisableBackButton { get; protected set; } = true;

    public void Dispose()
    {
        if (_process != null)
        {
            _process.OnDisconnected -= OnDisconnected;
            _process.OnUpdateTitle -= OnUpdateTitle;
            _process.OnUpdateMute -= OnUpdateMute;
            _process.OnUpdateDeafen -= OnUpdateDeafen;
            _process.OnUpdateSpeaking -= OnUpdateSpeaking;
            _process.OnEntityAdded -= OnEntityAdded;
            _process.OnEntityRemoved -= OnEntityRemoved;
        }

        GC.SuppressFinalize(this);
    }

    partial void OnIsMutedChanged(bool value)
    {
        _process?.ToggleMute(value);
    }

    partial void OnIsDeafenedChanged(bool value)
    {
        _process?.ToggleDeafen(value);
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
    private void Disconnect()
    {
        if (_process == null || _process.ConnectionState == ConnectionState.Disconnected)
        {
            navigationService.Back(); //If disconnected. Return to previous page.
            return;
        }

        _process?.Disconnect();
    }

    public override void OnAppearing(object? data = null)
    {
        if (data is VoiceNavigationData navigationData)
            _process = navigationData.Process;
        
        if (_process == null || _process.HasEnded)
        {
            navigationService.Back();
            return;
        }

        //Register events first.
        _process.OnDisconnected += OnDisconnected;
        _process.OnUpdateTitle += OnUpdateTitle;
        _process.OnUpdateDescription += OnUpdateDescription;
        _process.OnUpdateMute += OnUpdateMute;
        _process.OnUpdateDeafen += OnUpdateDeafen;
        _process.OnUpdateSpeaking += OnUpdateSpeaking;
        _process.OnEntityAdded += OnEntityAdded;
        _process.OnEntityRemoved += OnEntityRemoved;

        StatusTitleText = _process.Title;
        StatusDescriptionText = _process.Description;
        IsMuted = _process.Muted;
        IsDeafened = _process.Deafened;
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
            if (_process != null)
            {
                _process.OnDisconnected -= OnDisconnected;
                _process.OnUpdateTitle -= OnUpdateTitle;
                _process.OnUpdateDescription -= OnUpdateDescription;
                _process.OnUpdateMute -= OnUpdateMute;
                _process.OnUpdateDeafen -= OnUpdateDeafen;
                _process.OnUpdateSpeaking -= OnUpdateSpeaking;
                _process.OnEntityAdded -= OnEntityAdded;
                _process.OnEntityRemoved -= OnEntityRemoved;
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
    
    private void OnUpdateSpeaking(bool speaking)
    {
        Dispatcher.UIThread.Invoke(() => { IsSpeaking = speaking; });
    }

    private void OnEntityAdded(EntityViewModel entity)
    {
        Dispatcher.UIThread.Invoke(() => { EntityViewModels.Add(entity); });
    }

    private void OnEntityRemoved(EntityViewModel entity)
    {
        Dispatcher.UIThread.Invoke(() => { EntityViewModels.Remove(entity); });
    }
}