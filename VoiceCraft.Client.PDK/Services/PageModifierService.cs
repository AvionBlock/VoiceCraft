using Avalonia.Controls;
using System.Collections.Concurrent;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.PDK.Services
{
    public class PageModifierService
    {
        private ConcurrentDictionary<Type, Func<Control, Control>> _registeredModifiers;

        public PageModifierService()
        {
            _registeredModifiers = new ConcurrentDictionary<Type, Func<Control, Control>>();
        }

        public void RegisterModifier(Type targetPage, Func<Control, Control> action)
        {
            if (!typeof(Control).IsAssignableFrom(targetPage)) throw new ArgumentException($"Target page must implement {nameof(Control)}.", nameof(targetPage));
            _registeredModifiers.AddOrUpdate(targetPage, action, (key, old) => old = action);
        }
        public void UnregisterModifier(Type targetPage)
        {
            if (!typeof(Control).IsAssignableFrom(targetPage)) throw new ArgumentException($"Target page must implement {nameof(Control)}.", nameof(targetPage));
            _registeredModifiers.TryRemove(targetPage, out _);
        }

        public Func<Control, Control>? Get(Type targetPage)
        {
            if (_registeredModifiers.TryGetValue(targetPage, out var action))
            {
                return action;
            }
            return null;
        }
    }
}
