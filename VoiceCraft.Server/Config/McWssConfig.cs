namespace VoiceCraft.Server.Config;

public class McWssConfig
{
    public string LoginToken { get; set; } = Guid.NewGuid().ToString();
    public string Hostname { get; set; } = "ws://127.0.0.1:9050/";
    public uint MaxClients { get; set; } = 1;
    public uint MaxTimeoutMs { get; set; } = 10000;
    public string TunnelCommand { get; set; } = "voicecraft:data_tunnel";
}