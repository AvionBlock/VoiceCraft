using System;

namespace VoiceCraft.Network.NetPeers;

public abstract class McApiNetPeer(Version version, string token)
{
    public abstract McApiConnectionState ConnectionState { get; }
    public Version Version { get; } = version;
    public string Token { get; } = token;
    public object? Tag { get; set; }

    public abstract void Accept();
    public abstract void Reject();
    public abstract void Reject(Span<byte> data);
    public abstract void Send<T>(Span<byte> data);
    public abstract void Disconnect(Span<byte> data);
    public abstract void Disconnect();
}