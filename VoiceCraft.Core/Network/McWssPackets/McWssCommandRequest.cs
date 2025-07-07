using System;
using System.Text.Json.Serialization;

namespace VoiceCraft.Core.Network.McWssPackets
{
    public class McWssCommandRequest: McWssPacket<McWssCommandRequest.McWssCommandRequestBody>
    {
        public override McWssCommandRequestBody body { get; set; } = new McWssCommandRequestBody();

        [JsonIgnore]
        public string Command
        {
            get => body.commandLine;
            set => body.commandLine = value;
        }

        public McWssCommandRequest(string command = "")
        {
            Command = command;
            header.requestId = Guid.NewGuid().ToString();
            header.messagePurpose = "command";
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