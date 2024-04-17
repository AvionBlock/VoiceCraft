﻿using Fleck;
using System.Diagnostics;
using System.Numerics;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Network.Sockets
{
    public class MCWSS : IDisposable
    {
        //Variables
        private WebSocketServer Socket;
        private IWebSocketConnection? ConnectedSocket;
        private readonly string[] Dimensions;
        private readonly int Port;
        private readonly MCPacketRegistry MCPacketReg;

        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }

        //Events
        public delegate void Connect(string Username);
        public delegate void PlayerTravelled(Vector3 position, string Dimension);
        public delegate void Disconnect();

        public event Connect? OnConnect;
        public event PlayerTravelled? OnPlayerTravelled;
        public event Disconnect? OnDisconnect;

        public MCWSS(int Port)
        {
            MCPacketReg = new MCPacketRegistry();
            MCPacketReg.RegisterPacket(new Header() { messagePurpose = "event", eventName = nameof(Core.Packets.MCWSS.PlayerTravelled) }, typeof(MCWSSPacket<Core.Packets.MCWSS.PlayerTravelled>));
            MCPacketReg.RegisterPacket(new Header() { messagePurpose = "commandResponse" }, typeof(MCWSSPacket<LocalPlayerName>));
            this.Port = Port;
            Socket = new WebSocketServer($"ws://0.0.0.0:{Port}");
            Dimensions = ["minecraft:overworld", "minecraft:nether", "minecraft:end"];
        }

        public void Start()
        {
            Socket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    if (ConnectedSocket == null)
                    {
                        //https://gist.github.com/jocopa3/5f718f4198f1ea91a37e3a9da468675c
                        socket.Send(new MCWSSPacket<Command>() { header = { messagePurpose = "commandRequest", requestId = Guid.NewGuid().ToString() }, body = { commandLine = "/getlocalplayername" } }.SerializePacket());
                        socket.Send(new MCWSSPacket<Event> { header = { requestId = Guid.NewGuid().ToString(), messagePurpose = "subscribe" }, body = { eventName = "PlayerTravelled" } }.SerializePacket());
                        ConnectedSocket = socket;
                        IsConnected = true;
                    }
                    else
                        socket.Close();
                };

                socket.OnClose = () =>
                {
                    if (socket == ConnectedSocket)
                    {
                        ConnectedSocket = null;
                        IsConnected = false;
                        OnDisconnect?.Invoke();
                    }
                };

                socket.OnMessage = message =>
                {
                    var packet = MCPacketReg.GetPacketFromJsonString(message);

                    if (packet is MCWSSPacket<LocalPlayerName>)
                    {
                        var data = (MCWSSPacket<LocalPlayerName>)packet;
                        var name = data.body.localplayername;
                        OnConnect?.Invoke(name);
                    }

                    else if (packet is MCWSSPacket<Core.Packets.MCWSS.PlayerTravelled> data)
                    {
                        var x = data.body.player.position.x;
                        var y = data.body.player.position.y;
                        var z = data.body.player.position.z;
                        var dimensionInt = data.body.player.dimension;

                        OnPlayerTravelled?.Invoke(new Vector3(x, y, z), Dimensions[dimensionInt]);
#if DEBUG
                        Debug.WriteLine($"PlayerTravelled: {x}, {y}, {z}, {Dimensions[dimensionInt]}");
#endif
                    }
                };
            });
        }

        public void Stop()
        {
            ConnectedSocket?.Close();
            ConnectedSocket = null;
            Socket.Dispose();
            Socket = new WebSocketServer($"ws://0.0.0.0:{Port}");
        }

        //Dispose Handlers
        ~MCWSS()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Socket.Dispose();
                    IsConnected = false;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
