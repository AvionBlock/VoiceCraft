using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.ViewModels;

/// <summary>
/// ViewModel for the edit server page.
/// </summary>
public partial class EditServerViewModel : ObservableObject
{
    private readonly IDatabaseService _databaseService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ServerModel _unsavedServer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditServerViewModel"/> class.
    /// </summary>
    public EditServerViewModel(IDatabaseService databaseService, INavigationService navigationService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;

        var data = _navigationService.GetNavigationData<ServerModel>();
        _unsavedServer = data != null ? (ServerModel)data.Clone() : new ServerModel();
    }

    [RelayCommand]
    private async Task SaveServer()
    {
        try
        {
            await _databaseService.EditServer(UnsavedServer);
            await _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            var msg = string.IsNullOrWhiteSpace(ex.Message) ? "Unknown error occurred." : ex.Message;
            await Shell.Current.DisplayAlert("Error", msg, "OK");
        }
    }
}

