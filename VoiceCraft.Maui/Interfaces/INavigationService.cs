namespace VoiceCraft.Maui.Interfaces;

/// <summary>
/// Interface for navigation operations within the application.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to a page by name.</summary>
    Task NavigateTo(string pageName, object? navigationData = null, string? queries = null);

    /// <summary>Navigates back to the previous page.</summary>
    Task GoBack();

    /// <summary>Gets the navigation data passed to the current page.</summary>
    T? GetNavigationData<T>();
}

