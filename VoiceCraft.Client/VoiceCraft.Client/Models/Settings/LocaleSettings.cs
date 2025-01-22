using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class LocaleSettings : Setting<LocaleSettings>
    {
        public override event Action<LocaleSettings>? OnUpdated;

        public string Culture
        {
            get => _culture;
            set
            {
                _culture = value;
                OnUpdated?.Invoke(this);
            }
        }

        private string _culture = "en-US";

        public override object Clone()
        {
            var clone = (LocaleSettings)MemberwiseClone();
            clone.OnUpdated = null;
            return clone;
        }
    }
}