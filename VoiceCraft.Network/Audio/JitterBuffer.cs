//////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2006–2025 Aaron Clauson All rights reserved.                                       //
// Source: https://github.com/sipsorcery-org/sipsorcery/blob/master/src/net/RTP/RTPReorderBuffer.cs //
//////////////////////////////////////////////////////////////////////////////////////////////////////

//Modified for use with VoiceCraft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace VoiceCraft.Network.Audio;

public class JitterBuffer(TimeSpan maxDropOutTime)
{
    private readonly Lock _lock = new();
    private readonly LinkedList<JitterPacket> _data = [];
    private ushort? _currentSeqId;

    public bool Get([NotNullWhen(true)] out JitterPacket? packet)
    {
        lock (_lock)
        {
            packet = null;
            var next = _data.First?.Value;
            if (next == null) return false;

            if (_currentSeqId.HasValue && _currentSeqId != next.SequenceId)
                if (DateTime.UtcNow - next.ReceivedTime < maxDropOutTime)
                    return false;

            packet = next;
            _data.RemoveFirst();
            _currentSeqId = (ushort)(packet.SequenceId + 1);
            return true;
        }
    }

    public void Add(JitterPacket current)
    {
        lock (_lock)
        {
            _currentSeqId ??= current.SequenceId;
            var distance = GetSequenceDistance(_currentSeqId.Value, current.SequenceId);
            if (distance < 0) return; // The packet has already been played or skipped.

            var node = _data.First;
            while (node != null)
            {
                var nodeDistance = GetSequenceDistance(_currentSeqId.Value, node.Value.SequenceId);
                if (distance == nodeDistance) return; // Duplicate.
                if (distance < nodeDistance)
                {
                    _data.AddBefore(node, current);
                    return;
                }

                node = node.Next;
            }

            _data.AddLast(current);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _currentSeqId = null;
            _data.Clear();
        }
    }

    private static int GetSequenceDistance(ushort expected, ushort sequenceId)
    {
        return unchecked((short)(sequenceId - expected));
    }
}

public class JitterPacket(ushort sequenceId, byte[] data)
{
    public readonly byte[] Data = data;
    public readonly DateTime ReceivedTime = DateTime.UtcNow;
    public readonly ushort SequenceId = sequenceId;
}
