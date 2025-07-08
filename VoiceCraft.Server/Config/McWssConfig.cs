namespace VoiceCraft.Server.Config;

public class McWssConfig
{
    public string LoginToken { get; set; } = string.Empty;
    public uint Port { get; set; } = 9050;
    public uint MaxClients { get; set; } = 100;
}