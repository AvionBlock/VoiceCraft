using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class AddServerViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;

        public AddServerViewModel(IDatabaseService databaseService, INavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
        }

        [ObservableProperty]
        ServerModel server = new ServerModel();

        [RelayCommand]
        public async Task SaveServer()
        {
            try
            {
                await _databaseService.AddServer(Server);
                await _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
