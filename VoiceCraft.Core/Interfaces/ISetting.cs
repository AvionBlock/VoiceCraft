using System;
using System.Text.Json.Serialization;

namespace VoiceCraft.Core.Interfaces
{
    public interface ISetting<T> where T : unmanaged
    {
        string Id { get; set; }

        [JsonIgnore]
        string Name { get; }

        [JsonIgnore]
        T Default { get; }

        [JsonIgnore]
        Action<T>? OnSave { get; }

        [JsonIgnore]
        Action<T>? OnLoad { get; }
    }
}