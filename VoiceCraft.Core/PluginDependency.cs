using System;

namespace VoiceCraft.Core
{
    public class PluginDependency
    {
        public readonly Guid Id;
        public readonly Version Version;

        public PluginDependency(Guid id, Version version)
        {
            Id = id;
            Version = version;
        }
    }
}
