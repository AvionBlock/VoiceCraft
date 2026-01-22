using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VoiceCraft.Core.JsonConverters;

namespace VoiceCraft.Network.Servers;

public class McWssMcApiServer
{
    public class McWssConfig
    {
        [JsonConverter(typeof(JsonBooleanConverter))]
        public bool Enabled { get; set; }

        public string LoginToken { get; set; } = Guid.NewGuid().ToString();
        public string Hostname { get; set; } = "ws://127.0.0.1:9051/";
        public uint MaxClients { get; set; } = 1;
        public uint MaxTimeoutMs { get; set; } = 10000;
        public string DataTunnelCommand { get; set; } = "voicecraft:data_tunnel";
        public uint CommandsPerTick { get; set; } = 5;
        public uint MaxStringLengthPerCommand { get; set; } = 1000;
        public HashSet<McApiPacketType> DisabledPacketTypes { get; set; } = [];
    }
}