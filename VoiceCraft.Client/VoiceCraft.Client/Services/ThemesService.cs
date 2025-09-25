using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace VoiceCraft.Client.Services;

public class ThemesService
{
    private readonly ConcurrentDictionary<Guid, RegisteredBackgroundImage> _registeredBackgroundImages = new();

    private readonly ConcurrentDictionary<Guid, RegisteredTheme> _registeredThemes = new();
    private RegisteredBackgroundImage? _currentBackgroundImage;
    private RegisteredTheme? _currentTheme;

    public ThemesService(IEnumerable<RegisteredTheme> registeredThemes,
        IEnumerable<RegisteredBackgroundImage> registeredBackgroundImages)
    {
        _registeredThemes.TryAdd(Guid.Empty, new RegisteredTheme(Guid.Empty, "Default", ThemeVariant.Default, [], []));
        _registeredBackgroundImages.TryAdd(Guid.Empty, new RegisteredBackgroundImage(Guid.Empty, "None", string.Empty));

        foreach (var registeredTheme in registeredThemes) _registeredThemes.TryAdd(registeredTheme.Id, registeredTheme);

        foreach (var registeredBackgroundImage in registeredBackgroundImages)
            _registeredBackgroundImages.TryAdd(registeredBackgroundImage.Id, registeredBackgroundImage);
    }

    public IEnumerable<RegisteredTheme> RegisteredThemes => _registeredThemes.Values.ToArray();

    public IEnumerable<RegisteredBackgroundImage> RegisteredBackgroundImages =>
        _registeredBackgroundImages.Values.ToArray();

    public event Action<RegisteredTheme?>? OnThemeChanged;
    public event Action<RegisteredBackgroundImage?>? OnBackgroundImageChanged;

    public void SwitchTheme(Guid id)
    {
        if (!_registeredThemes.TryGetValue(id, out var theme) || _currentTheme == theme ||
            Application.Current == null) return;
        Application.Current.Resources.MergedDictionaries.Clear();
        if (id == Guid.Empty)
        {
            OnThemeChanged?.Invoke(null);
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            if (_currentTheme == null) return;
            foreach (var themeStyle in _currentTheme.ThemeStyles) Application.Current.Styles.Remove(themeStyle);
            _currentTheme = null;
            return;
        }

        if (_currentTheme != null)
            foreach (var themeStyle in _currentTheme.ThemeStyles)
                Application.Current.Styles.Remove(themeStyle);

        _currentTheme = theme;
        Application.Current.RequestedThemeVariant = theme.Variant;
        foreach (var themeStyle in theme.ThemeStyles) Application.Current.Styles.Add(themeStyle);
        foreach (var resource in _currentTheme.Resources)
            Application.Current.Resources.MergedDictionaries.Add(resource);

        OnThemeChanged?.Invoke(_currentTheme);
    }

    public void SwitchBackgroundImage(Guid id)
    {
        if (!_registeredBackgroundImages.TryGetValue(id, out var backgroundImage) ||
            _currentBackgroundImage == backgroundImage) return;
        if (id == Guid.Empty)
        {
            OnBackgroundImageChanged?.Invoke(null);
            _currentBackgroundImage = null;
            return;
        }

        backgroundImage.LoadBitmap();
        _currentBackgroundImage?.UnloadBitmap();
        _currentBackgroundImage = backgroundImage;
        OnBackgroundImageChanged?.Invoke(_currentBackgroundImage);
    }

    /// <summary>
    ///     Get Brush from resource <paramref name="key" />. Returns <paramref name="fallback" /> if key has not been found OR
    ///     returns a default color if <paramref name="fallback" /> has not been defined.
    /// </summary>
    /// <param name="key">Key for TryGetResource</param>
    /// <param name="fallback">Fallback for when the resource cannot be found. Can be null</param>
    /// <returns>An IBrush with the value of <paramref name="key" /> or <paramref name="fallback" /> or the default color.</returns>
    public static IBrush GetBrushResource(string key, IBrush? fallback = null)
    {
        return Application.Current is not null &&
               Application.Current.TryGetResource(key, Application.Current.ActualThemeVariant, out var val) &&
               val is not null
            ? (IBrush)val
            : fallback ?? new SolidColorBrush(new Color());
    }
}

public class RegisteredTheme(
    Guid id,
    string name,
    ThemeVariant variant,
    IStyle[] themeStyles,
    IResourceDictionary[] resourceDictionaries)
{
    public string Name { get; } = name;
    public Guid Id { get; } = id;
    public ThemeVariant Variant { get; } = variant;
    public IStyle[] ThemeStyles { get; } = themeStyles;
    public IResourceDictionary[] Resources { get; } = resourceDictionaries;
}

public class RegisteredBackgroundImage(Guid id, string name, string path)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string Path { get; } = path;
    public Bitmap? BackgroundImageBitmap { get; private set; }

    public Bitmap LoadBitmap()
    {
        UnloadBitmap();
        if (File.Exists(Path))
        {
            using (var fileStream = File.OpenRead(Path))
            {
                BackgroundImageBitmap = new Bitmap(fileStream);
            }

            return BackgroundImageBitmap;
        }

        if (AssetLoader.Exists(new Uri(Path)))
            return BackgroundImageBitmap = new Bitmap(AssetLoader.Open(new Uri(Path)));

        throw new FileNotFoundException("Could not find image file.", Path);
    }

    public void UnloadBitmap()
    {
        BackgroundImageBitmap?.Dispose();
        BackgroundImageBitmap = null;
    }
}