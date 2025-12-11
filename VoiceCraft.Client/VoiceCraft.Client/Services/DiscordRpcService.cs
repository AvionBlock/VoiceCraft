using System;
using System.Diagnostics;
using DiscordRPC;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services;

public class DiscordRpcService : IDisposable
{
    private readonly RichPresence _richPresence = new()
    {
        Buttons =
        [
            new Button
            {
                Label = Constants.GithubButton,
                Url = Constants.GithubButtonUrl
            }
        ],
        Assets = new Assets
        {
            LargeImageKey = Constants.LargeImageKey,
            LargeImageText = Constants.LargeImageText
        }
    };

    private readonly DiscordRpcClient? _rpcClient;

    public DiscordRpcService()
    {
        if (OperatingSystem.IsBrowser()) return;

        _rpcClient = new DiscordRpcClient(Constants.ApplicationId);

#if DEBUG
        _rpcClient.OnConnectionEstablished += (_, msg) => { Debug.WriteLine($"RPC Connected: {msg.Type}"); };

        _rpcClient.OnConnectionFailed += (_, msg) => { Debug.WriteLine($"RPC Failed: {msg.Type}"); };

        _rpcClient.OnReady += (_, msg) => { Debug.WriteLine($"RPC Ready: {msg.User.Username}"); };

        _rpcClient.OnPresenceUpdate += (_, _) => { Debug.WriteLine("RPC Update: Presence updated."); };

        _rpcClient.OnClose += (_, msg) => { Debug.WriteLine($"RPC Closed: {msg.Type}"); };

        _rpcClient.OnError += (_, msg) => { Debug.WriteLine($"RPC Error: {msg.Type}"); };
#endif

        _rpcClient?.Initialize();
        _rpcClient?.SetPresence(_richPresence);
    }

    public void Dispose()
    {
        _rpcClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void SetState(string state)
    {
        _richPresence.State = state;
        _rpcClient?.SetPresence(_richPresence);
    }
}