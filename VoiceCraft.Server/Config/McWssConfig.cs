namespace VoiceCraft.Server.Config;

public class McWssConfig
{
    public bool Enabled { get; set; }
    public string LoginToken { get; set; } = Guid.NewGuid().ToString();
    public string Hostname { get; set; } = "ws://127.0.0.1:9051/";
    public uint MaxClients { get; set; } = 1;
    public uint MaxTimeoutMs { get; set; } = 10000;
    public string SendTunnelCommand { get; set; } = "voicecraft:send_data_tunnel";
    public string ReceiveTunnelCommand { get; set; } = "voicecraft:receive_data_tunnel";
}