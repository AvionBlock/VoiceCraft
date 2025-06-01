using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels;

public partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorMessage = string.Empty;
}