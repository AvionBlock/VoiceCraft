using System;

namespace VoiceCraft.Core.Packets.MCWSS
{
    public class Header
    {
        public string requestId { get; set; } = string.Empty;
        public string messagePurpose { get; set; } = string.Empty;
        public int version { get; set; } = 1;
        public string messageType { get; set; } = string.Empty;
        public string eventName { get; set; } = string.Empty;

        public override bool Equals(object obj)
        {
            if (obj is Header other)
            {
                return string.Equals(messagePurpose, other.messagePurpose, StringComparison.Ordinal) &&
                       string.Equals(messageType, other.messageType, StringComparison.Ordinal) &&
                       string.Equals(eventName, other.eventName, StringComparison.Ordinal);
            }
            return false;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(messagePurpose);
            hash.Add(messageType);
            hash.Add(eventName);

            return hash.ToHashCode();
        }
    }
}
