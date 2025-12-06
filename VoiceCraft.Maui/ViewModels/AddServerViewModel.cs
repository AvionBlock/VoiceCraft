using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.ViewModels;

/// <summary>
/// ViewModel for the add server page.
/// </summary>
public partial class AddServerViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ServerModel _server = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddServerViewModel"/> class.
    /// </summary>
    public AddServerViewModel(IDatabaseService databaseService, INavigationService navigationService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task SaveServer()
    {
        try
        {
            await _databaseService.AddServer(Server);
            await _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            var msg = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error occurred." : ex.Message;
            await Shell.Current.DisplayAlert("Error", msg, "OK");
        }
    }
}

