using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.McApiPackets.Request;

public class McApiLoginRequestPacket(string requestId, string token, Version version, EventType[] subscribeEvents)
    : IMcApiPacket, IMcApiRIdPacket
{
    public McApiLoginRequestPacket() : this(string.Empty, string.Empty, new Version(0, 0, 0), [])
    {
    }

    public string RequestId { get; private set; } = requestId;
    public string Token { get; private set; } = token;
    public Version Version { get; private set; } = version;
    public EventType[] SubscribeEvents { get; private set; } = subscribeEvents;

    public McApiPacketType PacketType => McApiPacketType.LoginRequest;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RequestId, Constants.MaxStringLength);
        writer.Put(Token, Constants.MaxStringLength);
        writer.Put(Version.Major);
        writer.Put(Version.Minor);
        writer.Put(Version.Build);
        writer.Put(SubscribeEvents.Length);
        foreach (var subscribeEvent in SubscribeEvents)
        {
            writer.Put((byte)subscribeEvent);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        RequestId = reader.GetString(Constants.MaxStringLength);
        Token = reader.GetString(Constants.MaxStringLength);
        Version = new Version(reader.GetInt(), reader.GetInt(), reader.GetInt());
        //Backwards Compatibility, We leave as empty array since the version will be invalid anyway.
        if (reader.EndOfData)
        {
            SubscribeEvents = [];
            return;
        }

        //Else, Do new subscribe method.
        var eventsLength = reader.GetInt();
        var events = new List<EventType>();
        for (var i = 0; i < eventsLength; i++)
        {
            var @event = (EventType)reader.GetByte();
            if (Enum.IsDefined(@event))
            {
                events.Add(@event);
            }
        }

        SubscribeEvents = events.ToArray();
    }
    
    public void Return()
    {
        PacketPool<McApiLoginRequestPacket>.Return(this);
    }

    public McApiLoginRequestPacket Set(string requestId = "", string token = "", Version? version = null,
        EventType[]? subscribeEvents = null)
    {
        RequestId = requestId;
        Token = token;
        Version = version ?? new Version(0, 0, 0);
        SubscribeEvents = subscribeEvents ?? [];
        return this;
    }
}