using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class EditServerViewModel : ObservableObject
    {
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        ServerModel unsavedServer;

        public EditServerViewModel(IDatabaseService databaseService, INavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            
            var data = _navigationService.GetNavigationData<ServerModel>();
            if (data != null)
            {
                UnsavedServer = (ServerModel)data.Clone();
            }
            else
            {
                UnsavedServer = new ServerModel(); // Fallback
            }
        }

        [RelayCommand]
        public async Task SaveServer()
        {
            try
            {
                await _databaseService.EditServer(UnsavedServer);
                await _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
