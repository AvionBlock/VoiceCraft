using Avalonia.Notification;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using VoiceCraft.Core;

namespace VoiceCraft.Client.PDK.Services
{
    public class PluginsService
    {
        public static readonly string PluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
        private List<LoadedPlugin> _plugins = new List<LoadedPlugin>();
        private List<Exception> _pluginErrors = new List<Exception>();
        public IEnumerable<LoadedPlugin> Plugins { get => _plugins; }
        public IEnumerable<Exception> PluginErrors { get => _pluginErrors; }

        public void LoadPlugins(IEnumerable<string>? deletePlugins = null)
        {
            if (!Directory.Exists(PluginDirectory))
            {
                Directory.CreateDirectory(PluginDirectory);
            }

            var files = Directory.GetFiles(PluginDirectory, "*.dll")
                .ToArray();

            foreach (var file in files)
            {
                try
                {
                    if(deletePlugins?.Contains(file) ?? false)
                    {
                        File.Delete(file);
                        continue;
                    }

                    var plugin = LoadPlugin(file);
                    //Insert by priority
                    for (int i = -1; i < _plugins.Count; i++)
                    {
                        if (i == _plugins.Count - 1) //Reached the end, add to end.
                        {
                            _plugins.Add(plugin);
                            break;
                        }

                        var loadedPlugin = _plugins[i + 1];
                        if (loadedPlugin.LoadedInstance.Priority > plugin.LoadedInstance.Priority)
                        {
                            _plugins.Insert(i + 1, loadedPlugin);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _pluginErrors.Add(ex);
                }
            }

            for (int i = _plugins.Count - 1; i >= 0; i--)
            {
                var plugin = _plugins[i];
                //Plugin Dependency Checking Here.
                var dependencies = plugin.LoadedInstance.ClientDependencies;
                foreach (var dependency in dependencies)
                {
                    var matchedPlugin = _plugins.FirstOrDefault(x => x.LoadedInstance.Id == dependency.Id);
                    if (matchedPlugin != null && matchedPlugin.Version.Major == dependency.Version?.Major && matchedPlugin.Version.Minor == dependency.Version?.Minor) continue;

                    _plugins.Remove(plugin);
                    if (matchedPlugin == null)
                        _pluginErrors.Add(new Exception($"Failed to load plugin {plugin.Assembly.FullName}! Expected plugin with id {dependency.Id} with version {dependency.Version}!"));
                    else
                        _pluginErrors.Add(new Exception($"Failed to load plugin {plugin.Assembly.FullName}! Expected plugin {matchedPlugin.LoadedInstance.Name} with version {dependency.Version}!"));
                }
            }
        }

        public void SetupPlugins(ServiceCollection serviceCollection)
        {
            foreach (var plugin in _plugins)
            {
                try
                {
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
            var loadContext = new PluginLoadContext(filePath);
            var pluginAssembly = loadContext.LoadFromAssemblyPath(filePath);
            return new LoadedPlugin(pluginAssembly, loadContext);
        }
    }

    public class LoadedPlugin
    {
        public readonly IClientPlugin LoadedInstance;
        public readonly PluginLoadContext Context;
        public readonly Assembly Assembly;
        public readonly Version Version;

        public LoadedPlugin(Assembly assembly, PluginLoadContext loadContext)
        {
            Assembly = assembly;
            Context = loadContext;
            Version = Assembly.GetName().Version ?? new Version();

            var pluginEntryPoint = assembly.GetTypes().FirstOrDefault(x => typeof(IClientPlugin).IsAssignableFrom(x));
            if (pluginEntryPoint == null) throw new ArgumentException("Input assembly cannot be loaded as a plugin!", nameof(assembly));
            var loadedInstance = (IClientPlugin?)Activator.CreateInstance(pluginEntryPoint);
            if (loadedInstance == null) throw new Exception("Failed to load assembly as a plugin!");
            LoadedInstance = loadedInstance;
        }
    }
}
