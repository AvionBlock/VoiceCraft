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
        private static List<LoadedPlugin> _plugins = new List<LoadedPlugin>();
        private static List<Exception> _pluginErrors = new List<Exception>();
        public static IEnumerable<LoadedPlugin> Plugins { get => _plugins; }
        public static IEnumerable<Exception> PluginErrors { get => _pluginErrors; }

        public static void LoadPlugins(string pluginDirectory, ServiceCollection serviceCollection)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            var assemblies = 
                Directory.GetFiles(pluginDirectory, "*.dll")
                .Select(x => new AssemblyInfo(x))
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

        public static bool DeletePlugin(Guid pluginId)
        {
            var plugin = _plugins.FirstOrDefault(x => x.PluginInformation.Id == pluginId);
            if (plugin != null && File.Exists(plugin.Assembly.FileLocation))
            {
                File.Delete(plugin.Assembly.FileLocation);
                return true;
            }

            return false;
        }

        private static void AddPlugin(AssemblyInfo assembly)
        {
            //Find plugin.
            var assemblyType = assembly.Assembly.GetTypes().FirstOrDefault(x => x.GetCustomAttribute<PluginAttribute>() != null); //Plugin attribute enforces to be a class.
            if (assemblyType == null || !typeof(IPlugin).IsAssignableFrom(assemblyType))
            {
                _pluginErrors.Add(new Exception($"Failed to load plugin with assembly {assembly.Assembly.FullName}."));
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

    public class AssemblyInfo(string fileLocation)
    {
        public readonly string FileLocation = fileLocation;
        public readonly Assembly Assembly = Assembly.Load(File.ReadAllBytes(fileLocation));
        public readonly FileVersionInfo Version = FileVersionInfo.GetVersionInfo(fileLocation);
    }

    public class LoadedPlugin(PluginAttribute pluginInformation, AssemblyInfo assemblyInfo, IPlugin loadedInstance)
    {
        public readonly PluginAttribute PluginInformation = pluginInformation;
        public readonly IPlugin LoadedInstance = loadedInstance;
        public readonly AssemblyInfo Assembly = assemblyInfo;
    }
}
