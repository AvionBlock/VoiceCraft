using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.PDK
{
    public class PluginLoader
    {
        public static readonly string PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
        private List<LoadedPlugin> _plugins = new List<LoadedPlugin>();
        private List<Exception> _pluginErrors = new List<Exception>();
        public IEnumerable<LoadedPlugin> Plugins { get => _plugins; }
        public IEnumerable<Exception> PluginErrors { get => _pluginErrors; }

        public void LoadPlugins()
        {
            if (!Directory.Exists(PluginDirectory))
            {
                Directory.CreateDirectory(PluginDirectory);
            }

            var files = 
                Directory.GetFiles(PluginDirectory, "*.dll")
                .ToArray();

            foreach (var file in files)
            {
                try
                {
                    var plugin = LoadPlugin(file);
                    //Insert by priority
                    for(int i = 0; i <= _plugins.Count; i++)
                    {
                        if(i >= _plugins.Count - 1) //Reached the end, add to end.
                        {
                            _plugins.Add(plugin);
                            break;
                        }

                        var loadedPlugin = _plugins[i];
                        if (loadedPlugin.LoadedInstance.Priority > plugin.LoadedInstance.Priority)
                        {
                            _plugins.Insert(i, loadedPlugin);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _pluginErrors.Add(ex);
                }
            }
        }

        public void SetupPlugins(ServiceCollection serviceCollection)
        {
            foreach (var plugin in _plugins)
            {
                try
                {
                    //Plugin Dependency Checking Here.
                    var dependencies = plugin.Assembly.GetReferencedAssemblies();
                    foreach (var dependency in dependencies)
                    {
                        Debug.WriteLine(dependency.FullName);
                    }

                    plugin.LoadedInstance.Initialize(serviceCollection);
                }
                catch (Exception ex)
                {
                    _pluginErrors.Add(ex);
                }
            }
        }

        public void ExecutePlugins(IServiceProvider serviceProvider)
        {
            var notifications = serviceProvider.GetRequiredService<NotificationMessageManager>();
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.LoadedInstance.Execute(serviceProvider);

                    notifications.CreateMessage()
                        .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                        .Animates(true)
                        .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                        .HasBadge("Plugin")
                        .HasMessage($"Loaded plugin: {plugin.LoadedInstance.Name}")
                        .Dismiss().WithDelay(TimeSpan.FromSeconds(2))
                        .Dismiss().WithButton("Dismiss", (button) => { })
                        .Queue();
                }
                catch (Exception ex)
                {
                    _pluginErrors.Add(ex);
                }
            }
        }

        public void FlushErrors(INotificationMessageManager manager)
        {
            foreach (var error in _pluginErrors)
            {
#if DEBUG
                Debug.WriteLine(error);
#endif
                manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentErrorBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundErrorBrush"))
                    .HasBadge("Error")
                    .HasMessage(error.Message)
                    .Dismiss().WithDelay(TimeSpan.FromSeconds(5))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
            }

            _pluginErrors.Clear();
        }

        private LoadedPlugin LoadPlugin(string filePath)
        {
            var pluginContext = new PluginLoadContext(filePath);
            var pluginAssembly = pluginContext.LoadFromAssemblyPath(filePath);
            return new LoadedPlugin(pluginAssembly);
        }
    }

    public class LoadedPlugin
    {
        public readonly IClientPlugin LoadedInstance;
        public readonly Assembly Assembly;
        public readonly Version Version;

        public LoadedPlugin(Assembly assembly)
        {
            Assembly = assembly;
            Version = Assembly.GetName().Version ?? new Version();

            var pluginEntryPoint = assembly.GetTypes().FirstOrDefault(x => typeof(IClientPlugin).IsAssignableFrom(x));
            if(pluginEntryPoint == null) throw new ArgumentException("Input assembly cannot be loaded as a plugin!", nameof(assembly));
            var loadedInstance = (IClientPlugin?)Activator.CreateInstance(pluginEntryPoint);
            if (loadedInstance == null) throw new Exception("Failed to load assembly as a plugin!");
            LoadedInstance = loadedInstance;
        }
    }
}
