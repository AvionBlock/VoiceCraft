using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.Services
{
    public class NavigationService : INavigationService
    {
        private object? _navigationData;

        public async Task NavigateTo(string pageName, object? navigationData = null, string? queries = null)
        {
            try
            {
                if (navigationData != null)
                {
                    _navigationData = navigationData;
                }

                var query = !string.IsNullOrWhiteSpace(queries) ? $"?{queries}" : string.Empty;
                await Shell.Current.GoToAsync($"{pageName}{query}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public async Task GoBack()
        {
            try
            {
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public T? GetNavigationData<T>()
        {
            if (_navigationData is T data)
            {
                return data;
            }
            return default;
        }
    }
}
