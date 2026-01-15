using System;
using System.Text.Json.Serialization;

namespace VoiceCraft.Network.Packets.McWssPackets
{
    public class McWssCommandRequest : McWssPacket<McWssCommandRequest.McWssCommandRequestBody>
    {
        public McWssCommandRequest(string command = "")
        {
            Command = command;
            header.requestId = Guid.NewGuid().ToString();
            header.messagePurpose = "commandRequest";
        }

        public override McWssCommandRequestBody body { get; set; } = new();

        [JsonIgnore]
        public string Command
        {
            get => body.commandLine;
            set => body.commandLine = value;
        }

        //Resharper disable All
        public class McWssCommandRequestBody
        {
            public int version { get; set; } = 1;
            public string commandLine { get; set; } = string.Empty;
        }
        //Resharper enable All
    }
}