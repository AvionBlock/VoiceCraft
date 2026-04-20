using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoiceCraft.Core.Diagnostics;

public class CrashLogRecord : INotifyPropertyChanged
{
    private string _message = string.Empty;
    private string? _dumpUrl;

    public string Message
    {
        get => _message;
        set
        {
            if (_message == value)
                return;

            _message = value;
            OnPropertyChanged();
        }
    }

    public string? DumpUrl
    {
        get => _dumpUrl;
        set
        {
            if (_dumpUrl == value)
                return;

            _dumpUrl = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDumpUrl));
        }
    }

    public bool HasDumpUrl => !string.IsNullOrWhiteSpace(DumpUrl);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
