using System.Collections.ObjectModel;

namespace VoiceCraft.Core.Interfaces
{
    public interface IBaseLocalizer
    {
        string FallbackLanguage { get; }
        ObservableCollection<string> Languages { get; }
        string Get(string key);
        string Reload(string language);
    }
}