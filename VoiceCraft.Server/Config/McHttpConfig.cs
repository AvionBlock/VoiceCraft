namespace VoiceCraft.Server.Config;

public class McHttpConfig
{
    public string LoginToken { get; set; } = string.Empty;
    public string Hostname { get; set; } = "http://127.0.0.1:9051/";
    public uint MaxClients { get; set; } = 100;
}