using Avalonia.Controls.Templates;
using Avalonia.Controls;
using VoiceCraft.Client.PDK.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;
        private readonly IServiceProvider _services;

        public ViewLocator(IServiceProvider serviceProvider)
        {
            _services = serviceProvider;
        }

        public Control Build(object? data)
        {
            var name = data?.GetType()?.FullName?.Replace("ViewModel", "View");
            var view = _services.GetKeyedService<Control>(name);

            if (view != null)
            {
                return _services.GetService<PageModifierService>()?.Get(view.GetType())?.Invoke(view) ?? view;
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
