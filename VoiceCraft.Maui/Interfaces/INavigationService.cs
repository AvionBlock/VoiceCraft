namespace VoiceCraft.Maui.Interfaces
{
    public interface INavigationService
    {
        Task NavigateTo(string pageName, object? navigationData = null, string? queries = null);
        Task GoBack();
        T? GetNavigationData<T>();
    }
}
