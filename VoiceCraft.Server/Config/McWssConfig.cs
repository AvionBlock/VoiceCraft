namespace VoiceCraft.Server.Config;

public class McWssConfig
{
    public string LoginToken { get; set; } = string.Empty;
    public string Hostname { get; set; } = "ws://127.0.0.1:9050/";
    public uint MaxClients { get; set; } = 100;
    public uint MaxTimeoutMs { get; set; } = 10000;
    public uint PingIntervalMs { get; set; } = 5000;
    public string TunnelCommand { get; set; } = "voicecraft:data_tunnel";
}