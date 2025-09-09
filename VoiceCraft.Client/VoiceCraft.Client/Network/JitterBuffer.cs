//////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2006–2025 Aaron Clauson All rights reserved.                                       //
// Source: https://github.com/sipsorcery-org/sipsorcery/blob/master/src/net/RTP/RTPReorderBuffer.cs //
//////////////////////////////////////////////////////////////////////////////////////////////////////

//Modified for use with VoiceCraft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace VoiceCraft.Client.Network
{
    public class JitterBuffer(TimeSpan maxDropOutTime)
    {
        private readonly LinkedList<JitterPacket> _data = [];
        private ushort? _currentSeqId;

        private JitterPacket? First => _data.First?.Value;
        private JitterPacket? Last => _data.Last?.Value;

        private static bool IsBeforeWrapAround(JitterPacket? packet)
        {
            return IsBeforeWrapAround(packet?.SequenceId ?? 0);
        }

        private static bool IsBeforeWrapAround(uint seq)
        {
            return seq > uint.MaxValue / 2 + uint.MaxValue / 4;
        }

        private static bool IsAfterWrapAround(JitterPacket? packet)
        {
            return packet?.SequenceId < uint.MaxValue / 4;
        }

        public bool Get([NotNullWhen(true)] out JitterPacket? packet)
        {
            packet = null;
            if (Last == null)
            {
                return false;
            }

            if (_currentSeqId.HasValue && _currentSeqId != Last.SequenceId)
            {
                if (DateTime.UtcNow - Last.ReceivedTime < maxDropOutTime)
                {
                    return false;
                }
            }

            packet = Last;
            _data.RemoveLast();
            _currentSeqId = (ushort)checked(packet.SequenceId + 1);
            return true;
        }

        public void Add(JitterPacket current)
        {
            if (_data.Count == 0)
            {
                _data.AddFirst(current);
                return;
            }
            
            // if seq number is greater or equal than we are waiting for then append to last position
            if (_currentSeqId.HasValue && _currentSeqId >= current.SequenceId)
            {
                if (Last?.SequenceId > _currentSeqId || IsAfterWrapAround(Last) && IsBeforeWrapAround(_currentSeqId.Value))
                {
                    _data.AddLast(current);
                    return;
                }
            }

            if (IsBeforeWrapAround(Last) && !IsAfterWrapAround(First) &&
                IsAfterWrapAround(current)) // first incoming packet after wraparound
            {
                _data.AddFirst(current);
                return;
            }

            var node = _data.First;
            while (node != null)
            {
                // if it is packet before wrap around skip all packets after wrap around and then insert the packet
                if (IsBeforeWrapAround(current) && IsBeforeWrapAround(Last) && IsAfterWrapAround(node.Value))
                {
                    node = node.Next;
                    continue;
                }

                if (IsBeforeWrapAround(node.Value) && IsAfterWrapAround(current) ||
                    current.SequenceId > node.Value.SequenceId)
                {
                    _data.AddBefore(node, current);
                    break;
                }

                if (current.SequenceId == node.Value.SequenceId)
                {
                    break;
                }

                node = node.Next;
            }
        }

        public void Reset()
        {
            _currentSeqId = null;
            _data.Clear();
        }
    }

    public class JitterPacket(uint sequenceId, byte[] data)
    {
        public readonly uint SequenceId = sequenceId;
        public readonly DateTime ReceivedTime = DateTime.UtcNow;
        public readonly byte[] Data = data;
    }
}