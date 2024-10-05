using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using VoiceCraft.Client.PDK.Services;

namespace VoiceCraft.Client.PDK
{
    public static class PluginLoader
    {
        private static List<IPlugin> _plugins = new List<IPlugin>();
        public static void LoadPlugins(string pluginDirectory, ServiceCollection serviceCollection)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            var assemblies = Directory.GetFiles(pluginDirectory, "*.dll")
                .Select(Assembly.LoadFrom)
                .ToArray();

            foreach (var assembly in assemblies)
            {
                AddPlugin(assembly);
            }

            //Order by priority.
            _plugins = _plugins.OrderBy(x => x.Priority).ToList();

            foreach (var plugin in _plugins)
            {
                LoadPlugin(plugin, serviceCollection);
            }
        }

        public static void InitializePlugins(IServiceProvider serviceProvider)
        {
            var notifications = serviceProvider.GetRequiredService<NotificationMessageManager>();
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.Initialize(serviceProvider);

                    notifications.CreateMessage()
                        .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                        .Animates(true)
                        .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                        .HasBadge("Plugin")
                        .HasMessage($"Loaded plugin: {plugin.Name}")
                        .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                        .Dismiss().WithButton("Dismiss", (button) => { })
                        .Queue();
                }
                catch (Exception ex)
                {
                    notifications.CreateMessage()
                        .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                        .Animates(true)
                        .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                        .HasBadge("Error")
                        .HasMessage(ex.Message)
                        .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                        .Dismiss().WithButton("Dismiss", (button) => { })
                        .Queue();
                }
            }
        }

        private static void AddPlugin(Assembly assembly)
        {
            var pluginInterfaceType = typeof(IPlugin);
            //Find and instantiate plugin.
            var assemblyType = assembly.GetTypes().FirstOrDefault(x => pluginInterfaceType.IsAssignableFrom(x) && x.IsClass);
            if (assemblyType == null) return;
            var plugin = (IPlugin?)Activator.CreateInstance(assemblyType);
            if (plugin == null) return;

            Debug.WriteLine($"Adding Plugin: {plugin.Name}");

            //If conflicted with another plugin, don't add it. This order can be completely random.
            if (_plugins.Exists(x => x.Id == plugin.Id)) return;
            _plugins.Add(plugin);
        }

        private static void LoadPlugin(IPlugin plugin, ServiceCollection serviceCollection)
        {
            foreach (var dependency in plugin.ClientDependencies)
            {
                //Cannot find dependency, don't load the plugin.
                if (_plugins.Exists(x => x.Id != dependency)) return;
            }

            plugin.Load(serviceCollection);
        }
    }
}
