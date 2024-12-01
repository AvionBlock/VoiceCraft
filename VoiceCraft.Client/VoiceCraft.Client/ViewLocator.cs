using Avalonia.Controls.Templates;
using Avalonia.Controls;
using System;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public Control? Build(object? data)
        {
            var name = data?.GetType()?.FullName?.Replace("ViewModel", "View");

            if (name != null)
            {
                var type = Type.GetType(name);
                if (type != null)
                {
                    return (Control?)Activator.CreateInstance(type);
                }
            }
            
            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
