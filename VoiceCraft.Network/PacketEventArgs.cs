using System;
using System.Net;

namespace VoiceCraft.Network;

/// <summary>
/// Proives data for packet events.
/// </summary>
/// <typeparam name="T">The type of the packet.</typeparam>
public class PacketEventArgs<T> : EventArgs
{
    /// <summary>
    /// Gets the packet data.
    /// </summary>
    public T Packet { get; }

    /// <summary>
    /// Gets the source peer or endpoint.
    /// </summary>
    public object Source { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketEventArgs{T}"/> class.
    /// </summary>
    /// <param name="packet">The packet data.</param>
    /// <param name="source">The source of the packet.</param>
    public PacketEventArgs(T packet, object source)
    {
        Packet = packet;
        Source = source;
    }
}
