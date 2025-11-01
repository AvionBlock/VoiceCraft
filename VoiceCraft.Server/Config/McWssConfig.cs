namespace VoiceCraft.Server.Config;

public class McWssConfig
{
    public string LoginToken { get; set; } = string.Empty;
    public string Hostname { get; set; } = "ws://127.0.0.1:9050/";
    public uint MaxClients { get; set; } = 100;
    public string TunnelId { get; set; } = "vc:mcwss_api";
}