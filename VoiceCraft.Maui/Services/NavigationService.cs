using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui.Services;

/// <summary>
/// Service for handling navigation between pages in the application.
/// Implements spam-click prevention using a semaphore.
/// </summary>
public class NavigationService : INavigationService
{
    private object? _navigationData;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc/>
    public async Task NavigateTo(string pageName, object? navigationData = null, string? queries = null)
    {
        if (!await _semaphore.WaitAsync(200)) return;

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

    /// <inheritdoc/>
    public async Task GoBack()
    {
        if (!await _semaphore.WaitAsync(200)) return;

        try
        {
            await Shell.Current.GoToAsync("..");
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

    /// <inheritdoc/>
    public T? GetNavigationData<T>()
    {
        return _navigationData is T data ? data : default;
    }
}

