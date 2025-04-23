using System;
using System.Diagnostics;
using DiscordRPC;

namespace VoiceCraft.Client.Services
{
    public class DiscordRpcService : IDisposable
    {
        private const string ApplicationId = "1364434932968984669";
        private const string GithubButton = "VoiceCraft";
        private const string GithubButtonUrl = "https://github.com/AvionBlock/VoiceCraft";
        private const string LargeImageKey = "vc";
        private const string LargeImageText = "VoiceCraft";
        private readonly DiscordRpcClient _rpcClient = new(ApplicationId);
        private readonly RichPresence _richPresence = new()
        {
            Buttons = [ new Button()
            {
                Label = GithubButton,
                Url = GithubButtonUrl
            }],
            Assets = new Assets()
            {
                LargeImageKey = LargeImageKey,
                LargeImageText = LargeImageText,
            }
        };

        public DiscordRpcService()
        {
            _rpcClient.OnReady += (_, _) =>
            {
                Debug.WriteLine("RPC Ready");
            };

            _rpcClient.OnClose += (_, msg) =>
            {
                Debug.WriteLine($"RPC Closed: {msg.Type}");
            };
            
            _rpcClient.OnError += (_, msg) =>
            {
                Debug.WriteLine($"RPC Error: {msg.Type}");
            };
            
            _rpcClient.OnConnectionEstablished += (_, msg) =>
            {
                Debug.WriteLine($"RPC Connection Established: {msg.Type}");
            };

            _rpcClient.OnConnectionFailed += (_, msg) =>
            {
                Debug.WriteLine($"RPC Connection Failed: {msg.Type}");
            };
            
            // == Subscribe to some events
            _rpcClient.OnReady += (_, msg) =>
            {
                //Create some events so we know things are happening
                Debug.WriteLine("Connected to discord with user {0}", msg.User.Username);
            };

            _rpcClient.OnPresenceUpdate += (_, _) =>
            {
                //The presence has updated
                Debug.WriteLine("Presence has been updated!");
            };
        }

        public void SetState(string state)
        {
            _richPresence.State = state;
            _rpcClient.SetPresence(_richPresence);
        }

        public void Initialize()
        {
            _rpcClient.Initialize();
            _rpcClient.SetPresence(_richPresence);
        }

        public void Dispose()
        {
            _rpcClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}