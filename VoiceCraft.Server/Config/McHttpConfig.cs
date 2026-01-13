using VoiceCraft.Core;

namespace VoiceCraft.Server.Config;

public class McHttpConfig
{
    public bool Enabled { get; set; } = true;
    public string LoginToken { get; set; } = Guid.NewGuid().ToString();
    public string Hostname { get; set; } = "http://127.0.0.1:9050/";
    public uint MaxClients { get; set; } = 1;
    public uint MaxTimeoutMs { get; set; } = 10000;
    public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
}