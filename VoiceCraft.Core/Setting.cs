using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace VoiceCraft.Core
{
    public abstract class Setting<T> : ObservableObject
    {
        [JsonIgnore]
        public virtual Action<T>? SettingSaved { get; }

        [JsonIgnore]
        public virtual Action<T>? SettingLoaded { get; }
    }
}