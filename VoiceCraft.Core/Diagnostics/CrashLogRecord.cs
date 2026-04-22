using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VoiceCraft.Core.Diagnostics;

public class CrashLogRecord : INotifyPropertyChanged
{
    public string Message
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string? DumpUrl
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
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
