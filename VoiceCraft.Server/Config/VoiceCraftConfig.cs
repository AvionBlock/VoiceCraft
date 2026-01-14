using System.Text.Json.Serialization;
using VoiceCraft.Core;
using VoiceCraft.Core.JsonConverters;

namespace VoiceCraft.Server.Config;

public class VoiceCraftConfig
{
    public string Language { get; set; } = Constants.DefaultLanguage;
    public uint Port { get; set; } = 9050;
    public uint MaxClients { get; set; } = 100;
    public string Motd { get; set; } = "VoiceCraft Proximity Chat!";
    public PositioningType PositioningType { get; set; } = PositioningType.Server;
    [JsonConverter(typeof(JsonBooleanConverter))]
    public bool EnableVisibilityDisplay { get; set; } = true;
}