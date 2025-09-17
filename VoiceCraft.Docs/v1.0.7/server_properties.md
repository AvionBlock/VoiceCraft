# Locating ServerProperties.json
Your ServerProperties.json file will get generated in the same directory as the VoiceCraft.Server.exe file when you start the VoiceCraft server for the first time. Once generated, it will look like this.
```json
{
  "VoiceCraftPortUDP": 9050,
  "MCCommPortTCP": 9050,
  "PermanentServerKey": "",
  "ConnectionType": 0,
  "ExternalServerTimeoutMS": 8000,
  "ClientTimeoutMS": 8000,
  "DefaultSettings": {
    "ProximityDistance": 30,
    "ProximityToggle": true,
    "VoiceEffects": true
  },
  "Channels": [
    {
      "Name": "Main",
      "Password": "",
      "Locked": true,
      "Hidden": true,
      "OverrideSettings": null
    }
  ],
  "ServerMOTD": "VoiceCraft Proximity Chat!",
  "Debugger": {
    "LogExceptions": false,
    "LogInboundPackets": false,
    "LogOutboundPackets": false,
    "LogInboundMCCommPackets": false,
    "LogOutboundMCCommPackets": false,
    "InboundPacketFilter": [],
    "OutboundPacketFilter": [],
    "InboundMCCommFilter": [],
    "OutboundMCCommFilter": []
  }
}
```

# Main Settings
Settings that the server will use to change it's behavior on runtime.

## VoicePortUDP
**VoicePortUDP** is the port that the voice socket will start on using the UDP protocol that allows people to hear each other. This setting will only accept values between `1025` to `65535`.

## SignallingPortTCP
**SignallingPortTCP** is the port that the signalling socket will start on using the TCP protocol that allows people to connect, bind and change states. This setting will only accept values between `1025` to `65535`.

## MCCommPortTCP
**MCCommPortTCP** is the port that the MCComm socket will start on using the TCP/HTTP protocol that allows the minecraft server to communicate and send data to the VoiceCraft server. This setting will only accept values between `1025` to `65535`.

## PermanentServerKey
**PermanentServerKey** is the key that is used for logging in the minecraft server to the VoiceCraft server. Leaving this blank, whitespace or null will generate a random UUID4 key to be used as the login key. This setting will only accept a `string` or `null` value.

## ConnectionType
**ConnectionType** is the type of login connection that the server will accept when a client connects. The numbers below correspond to the type of connection:
|Type|Description                                                                              |
|----|-----------------------------------------------------------------------------------------|
|0   |Server Sided: Will only allow clients to connect and bind by minecraft server.           |
|1   |Client Sided: Will only allow clients to connect and bind by using the MCWSS connection. |
|2   |Hybrid: Allows both of the above connections.                                            |

This setting will only accept values between `0` to `2`.

## ExternalServerTimeoutMS
**ExternalServerTimeoutMS** is the amount of time in milliseconds the server will use to kick/disconnect minecraft servers that exceed this value based on the last ping/packet. This setting will only accept an `integer` value.

## ClientTimeoutMS
**ClientTimeoutMS** is the amount of time in milliseconds the server will use to kick/disconnect clients that exceed this value based on the client's last ping. This setting will only accept an `integer` value.

## Channels
**Channels** is a list of available channels that the client can connect/switch. Each channel is defined as an `object` which contains the following format:
```json
{
  "Name": "ChannelName",
  "Password": "",
  "OverrideSettings": {
    "ProximityDistance": 30,
    "ProximityToggle": false,
    "VoiceEffects": false
  }
}
```
### Name
**Name** is the name of the channel that will be displayed on the client's UI. This setting will only accept a non `blank`, `whitespace` or `null` `string` value under 13 characters.

### Password
**Password** is the password that is required for the client to input before being able to join the channel. Leaving this blank will not require the client to input a password to join the channel. This setting will only accept a `blank` or `string` value under 13 characters.

### OverrideSettings
**OverrideSettings** are the settings that override the main settings (as described further in this page) when a client is in the channel. Leaving this empty or completely omitting the field will default to the main settings. This setting will only accept an `object` or `null` value.

## ProximityDistance
**ProximityDistance** is the setting that defines how far away players can hear each other (in blocks) when proximity chat is enabled. This setting will only accept values between `1` to `120`.

## ProximityToggle
**ProximityToggle** is the setting that defines whether proximity chat is enabled by default. This setting will only accept a `boolean` value.

## VoiceEffects
**VoiceEffects** is the setting the defines whether voice effect should be used when proximity chat is enabled. This setting will only accept a `boolean` value.

## ServerMOTD
**ServerMOTD** is the setting that defines what message clients will see when pinging before connecting to the server. This setting only accepts a `string` value with a maximum of 30 characters.

<br></br>

# Debugger
Used for debugging end to end connections. Especially useful for debugging communication between a custom client or a custom server connection.

## LogExceptions
**LogExceptions** is the setting that defines whether the server should log exception errors thrown by code. This setting will only accept a `boolean` value.

## LogInboundVoicePackets
**LogInboundVoicePackets** is the setting that defines whether the server should log incoming packets on the Voice socket. This setting will only accept a `boolean` value.

## LogOutboundVoicePackets
**LogOutboundVoicePackets** is the setting that defines whether the server should log outgoing packets on the Voice socket. This setting will only accept a `boolean` value.

## LogInboundSignallingPackets
**LogInboundSignallingPackets** is the setting that defines whether the server should log incoming packets on the Signalling socket. This setting will only accept a `boolean` value.

## LogOutboundSignallingPackets
**LogOutboundSignallingPackets** is the setting that defines whether the server should log outgoing packets on the Signalling socket. This setting will only accept a `boolean` value.

## LogInboundMCCommPackets
**LogInboundMCCommPackets** is the setting that defines whether the server should log incoming packets on the MCComm socket. This setting will only accept a `boolean` value.

## LogOutboundMCCommPackets
**LogOutboundMCCommPackets** is the setting that defines whether the server should log outgoing packets on the MCComm socket. This setting will only accept a `boolean` value.

## InboundVoiceFilter
**TODO**

## OutboundVoiceFilter
**TODO**

## InboundSignallingFilter
**TODO**

## OutboundSignallingFilter
**TODO**

## InboundMCCommFilter
**TODO**

## OutboundMCCommFilter
**TODO**