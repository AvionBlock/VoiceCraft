using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels;

public partial class ErrorViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ErrorMessage { get; set; } = string.Empty;
}