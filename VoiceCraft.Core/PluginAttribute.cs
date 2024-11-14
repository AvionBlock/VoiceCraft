using System;
using System.Collections.Generic;
using System.Linq;

namespace VoiceCraft.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PluginAttribute : Attribute
    {
        public readonly Guid Id;
        public readonly string Name;
        public readonly string Description;
        public readonly int Priority;

        public readonly IEnumerable<Guid> ClientDependencies;
        public readonly IEnumerable<Guid> ServerDependencies;

        public PluginAttribute(string id, string name, string description, int priority, string[] clientDependencies, string[] serverDependencies)
        {
            Id = Guid.Parse(id);
            Name = name;
            Description = description;
            Priority = priority;

            ClientDependencies = clientDependencies.Select(x => Guid.Parse(x));
            ServerDependencies = serverDependencies.Select(x => Guid.Parse(x));
        }
    }
}
