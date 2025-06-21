using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.Models;

public class Contributor
{
    private readonly string[] _roles;
    
    public string Name { get; }
    public ObservableCollection<string> Roles { get; }
    public Bitmap? ImageIcon { get; }

    public Contributor(string name, string[] roles, Bitmap? imageIcon = null)
    {
        Name = name;
        _roles = roles;
        ImageIcon = imageIcon;
        Roles = new ObservableCollection<string>(roles);
        Localizer.LanguageChanged += (_, _) => UpdateLocalizations();
        UpdateLocalizations();
    }

    private void UpdateLocalizations()
    {
        Roles.Clear();
        foreach (var role in _roles)
        {
            Roles.Add(Localizer.Get(role));
        }
    }
}