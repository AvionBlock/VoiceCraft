using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace VoiceCraft.Client.PDK
{
    public static class PluginLoader
    {
        private static List<IPlugin> _plugins = new List<IPlugin>();
        public static void LoadPlugins(string pluginDirectory, ServiceCollection serviceCollection)
        {
            var assemblies = Directory.GetFiles(pluginDirectory, "*.dll")
                .Select(Assembly.LoadFrom)
                .ToArray();

            var pluginInterfaceType = typeof(IPlugin);
            foreach (var assembly in assemblies)
            {
                var assemblyType = assembly.GetTypes().FirstOrDefault(x => pluginInterfaceType.IsAssignableFrom(x) && x.IsClass);
                if (assemblyType != null)
                {
                    var plugin = (IPlugin?)Activator.CreateInstance(assemblyType);
                    if (plugin != null)
                    {
                        Debug.WriteLine($"Loading Plugin: {plugin.Name}");
                        _plugins.Add(plugin);
                        plugin?.Load(serviceCollection);
                        break;
                    }
                }
            }
        }

        public static void InitializePlugins(IServiceProvider serviceProvider)
        {
            foreach (var plugin in _plugins)
            {
                plugin.Initialize(serviceProvider);
            }
        }
    }
}
