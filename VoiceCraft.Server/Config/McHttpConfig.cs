namespace VoiceCraft.Server.Config;

public class McHttpConfig
{
    public string LoginToken { get; set; } = string.Empty;
    public uint Port { get; set; } = 9051;
    public uint MaxClients { get; set; } = 100;
}