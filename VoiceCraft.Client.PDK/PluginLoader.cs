using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.PDK
{
    public static class PluginLoader
    {
        private const string DeletePlugins = "deletePlugins";

        private static List<LoadedPlugin> _plugins = new List<LoadedPlugin>();
        private static List<Exception> _pluginErrors = new List<Exception>();
        public static IEnumerable<LoadedPlugin> Plugins { get => _plugins; }
        public static IEnumerable<Exception> PluginErrors { get => _pluginErrors; }

        public static void LoadPlugins(string pluginDirectory, ServiceCollection serviceCollection)
        {
            GlobalSettings.RegisterSetting<List<string>>("deletePlugins");
            GlobalSettings.Load();

            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            var files = Directory.GetFiles(pluginDirectory, "*.dll").ToList();

            var deletePlugins = GlobalSettings.Get<List<string>>(DeletePlugins);
            foreach(var deletePlugin in deletePlugins)
            {
                if(files.Contains(deletePlugin))
                {
                    files.Remove(deletePlugin);
                    File.Delete(deletePlugin);
                }
            }

            foreach (var file in files)
            {
                Debug.WriteLine(file);
            }

            var assemblies = files
                .Select(Assembly.LoadFrom)
                .ToArray();

            foreach (var assembly in assemblies)
            {
                AddPlugin(assembly);
            }

            //Order by priority.
            _plugins = _plugins.OrderBy(x => x.PluginInformation.Priority).ToList();

            foreach (var plugin in _plugins)
            {
                LoadPlugin(plugin, serviceCollection);
            }

            GlobalSettings.Load(); //Load again for plugins
            GlobalSettings.Set(DeletePlugins, new List<string>());
            _ = GlobalSettings.SaveAsync();
        }

        public static void InitializePlugins(IServiceProvider serviceProvider)
        {
            var notifications = serviceProvider.GetRequiredService<NotificationMessageManager>();
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.LoadedInstance.Initialize(serviceProvider);

                    notifications.CreateMessage()
                        .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                        .Animates(true)
                        .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                        .HasBadge("Plugin")
                        .HasMessage($"Loaded plugin: {plugin.PluginInformation.Name}")
                        .Dismiss().WithDelay(TimeSpan.FromSeconds(2))
                        .Dismiss().WithButton("Dismiss", (button) => { })
                        .Queue();
                }
                catch (Exception ex)
                {
                    _pluginErrors.Add(ex);
                }
            }

            foreach (var error in _pluginErrors)
            {
#if DEBUG
                Debug.WriteLine(error);
#endif
                notifications.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(error.Message)
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }
        }

        public static void DeletePlugin(Guid pluginId)
        {
            var plugin = _plugins.FirstOrDefault(x => x.PluginInformation.Id == pluginId);
            var deletePlugins = GlobalSettings.Get<List<string>>(DeletePlugins);
            if (plugin != null && !deletePlugins.Contains(plugin.Assembly.Location))
            {
                deletePlugins.Add(plugin.Assembly.Location);
                _ = GlobalSettings.SaveAsync();
            }
        }

        public static void CancelPluginDeletion(Guid pluginId)
        {
            var plugin = _plugins.FirstOrDefault(x => x.PluginInformation.Id == pluginId);
            if (plugin != null)
            {
                GlobalSettings.Get<List<string>>(DeletePlugins).Remove(plugin.Assembly.Location);
                _ = GlobalSettings.SaveAsync();
            }
        }

        private static void AddPlugin(Assembly assembly)
        {
            //Find plugin.
            var assemblyType = assembly.GetTypes().FirstOrDefault(x => x.GetCustomAttribute<PluginAttribute>() != null); //Plugin attribute enforces to be a class.
            if (assemblyType == null || !typeof(IPlugin).IsAssignableFrom(assemblyType))
            {
                _pluginErrors.Add(new Exception($"Failed to load plugin with assembly {assembly.FullName}."));
                return;
            }

            var pluginAttribute = assemblyType.GetCustomAttribute<PluginAttribute>()!;
            var plugin = (IPlugin)Activator.CreateInstance(assemblyType)!;

            //If conflicted with another plugin, don't add it. This order can be completely random.
            if (_plugins.Exists(x => x.PluginInformation.Id == pluginAttribute.Id)) return;
            _plugins.Add(new LoadedPlugin(pluginAttribute, assembly, plugin));
        }

        private static void LoadPlugin(LoadedPlugin loadedPlugin, ServiceCollection serviceCollection)
        {
            foreach (var dependency in loadedPlugin.PluginInformation.ClientDependencies)
            {
                //Cannot find dependency, don't load the plugin.
                if (_plugins.Exists(x => x.PluginInformation.Id != dependency))
                {
                    _pluginErrors.Add(new Exception($"Failed to load plugin {loadedPlugin.PluginInformation.Name}, Missing client dependencies."));
                    return;
                }
            }

            loadedPlugin.LoadedInstance.Load(serviceCollection);
        }
    }

    public class LoadedPlugin(PluginAttribute pluginInformation, Assembly assembly, IPlugin loadedInstance)
    {
        public readonly PluginAttribute PluginInformation = pluginInformation;
        public readonly IPlugin LoadedInstance = loadedInstance;
        public readonly Assembly Assembly = assembly;
    }
}
