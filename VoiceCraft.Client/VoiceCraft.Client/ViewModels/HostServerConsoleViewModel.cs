using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client.Controls;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels;

public partial class HostServerConsoleViewModel(
    NavigationService navigationService,
    IBackgroundService backgroundService) : ViewModelBase, IDisposable
{
    [ObservableProperty] public partial string Output { get; set; } = string.Empty;

    private EventBufferedTextWriter? _eventBufferedTextWriter;

    [RelayCommand]
    private void Cancel()
    {
        navigationService.Back();
    }

    public void Dispose()
    {
        ClearService();
        GC.SuppressFinalize(this);
    }

    public override void OnAppearing(object? data = null)
    {
        var service = backgroundService.GetService<VoiceCraftServerService>();
        if (service == null) return;
        SetService(service);
    }

    private void SetService(VoiceCraftServerService voiceCraftServerService)
    {
        ClearService();

        _eventBufferedTextWriter = voiceCraftServerService.ConsoleEventOutput;
        _eventBufferedTextWriter.OnWrite += OnWrite;
        
        OnWrite(_eventBufferedTextWriter.ToString());
    }

    private void ClearService()
    {
        if (_eventBufferedTextWriter == null) return;
        var eventBufferedTextWriter = _eventBufferedTextWriter;
        _eventBufferedTextWriter = null;

        eventBufferedTextWriter.OnWrite -= OnWrite;
    }

    private void OnWrite(string value)
    {
        Dispatcher.UIThread.Invoke(() => Output = value);
    }
}