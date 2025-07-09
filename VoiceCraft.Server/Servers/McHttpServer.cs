using System.Collections.Concurrent;
using LiteNetLib.Utils;
using Spectre.Console;
using VoiceCraft.Core.Network;
using VoiceCraft.Server.Config;
using WatsonWebserver.Lite;
using WatsonWebsocket;

namespace VoiceCraft.Server.Servers;

public class McHttpServer
{
    //Public Properties
    public McHttpConfig Config { get; private set; } = new();

    private readonly ConcurrentDictionary<ClientMetadata, McApiNetPeer> _mcApiPeers = [];
    private readonly NetDataReader _reader = new();
    private readonly NetDataWriter _writer = new();
    private WebserverLite? _httpServer;

    public McHttpServer(McHttpConfig? config = null)
    {
        if (config != null)
            Config = config;

        try
        {
            AnsiConsole.WriteLine(Locales.Locales.McWssServer_Starting);
            
            //Setup HTTP server here.
            AnsiConsole.WriteLine(Locales.Locales.McWssServer_Success);
        }
        catch
        {
            throw new Exception(Locales.Locales.McWssServer_Exceptions_Failed);
        }
    }
}