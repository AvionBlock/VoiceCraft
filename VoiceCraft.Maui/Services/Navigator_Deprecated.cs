namespace VoiceCraft.Maui.Services
{
    public static class Navigator
    {
        private static object NavigationData = new object();
        public static async Task NavigateTo(string pageName, object? navigationData = null, string? queries = null)
        {
            try
            {
                if(navigationData != null)
                    NavigationData = navigationData;
                var query = !string.IsNullOrWhiteSpace(queries) ? $"?{queries}" : string.Empty;

                await Shell.Current.GoToAsync($"{pageName}{query}");
            }
            catch(Exception ex )
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public static async Task GoBack()
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

        public static T GetNavigationData<T>()
        {
            return (T)NavigationData;
        }

    }
}
