using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.Services
{
    public class NavigationService : INavigationService
    {
        private object? _navigationData;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task NavigateTo(string pageName, object? navigationData = null, string? queries = null)
        {
            if (!await _semaphore.WaitAsync(200)) return; // Prevent spam clicking

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
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task GoBack()
        {
            if (!await _semaphore.WaitAsync(200)) return;

            try
            {
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                _semaphore.Release();
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
