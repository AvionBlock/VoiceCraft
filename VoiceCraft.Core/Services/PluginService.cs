using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;

namespace VoiceCraft.Core.Services
{
    public class PluginService
    {
        private static string PluginsPath = $"{AppContext.BaseDirectory}/Plugins";
        public void LoadPlugins(ServiceCollection services)
        {
            if (!Directory.Exists(PluginsPath)) return;

            foreach(var file in Directory.GetFiles(PluginsPath, "*.dll"))
            {
                Debug.WriteLine(file);
                AssemblyLoadContext
            }
        }
    }
}
