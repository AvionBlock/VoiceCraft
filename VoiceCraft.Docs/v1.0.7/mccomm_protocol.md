# Index
- [Login](#login--mccommpacket)
- [Logout](#logout--mccommpacket)
- [Accept](#accept--mccommpacket)
- [Deny](#deny--mccommpacket)
- [Bind](#bind--mccommpacket)
- [Update](#update--mccommpacket)
- [AckUpdate](#ackupdate--mccommpacket)
- [GetChannels](#getchannels--mccommpacket)
- [GetChannelSettings](#getchannelsettings--mccommpacket)
- [SetChannelSettings](#setchannelsettings--mccommpacket)
- [GetDefaultSettings](#getdefaultsettings--mccommpacket)
- [SetDefaultSettings](#setdefaultsettings--mccommpacket)
- [GetParticipants](#getparticipants--mccommpacket)
- [DisconnectParticipant](#disconnectparticipant--mccommpacket)
- [GetParticipantBitmask](#getparticipantbitmask--mccommpacket)
- [SetParticipantBitmask](#setparticipantbitmask--mccommpacket)
- [MuteParticipant](#muteparticipant--mccommpacket)
- [UnmuteParticipant](#unmuteparticipant--mccommpacket)
- [DeafenParticipant](#deafenparticipant--mccommpacket)
- [UndeafenParticipant](#undeafenparticipant--mccommpacket)
- [ANDModParticipantBitmask](#andmodparticipantbitmask--mccommpacket)
- [ORModParticipantBitmask](#ormodparticipantbitmask--mccommpacket)
- [XORModParticipantBitmask](#xormodparticipantbitmask--mccommpacket)
- [ChannelMove](#channelmove--mccommpacket)

## MCCommPacket
|Name    |Type  |Description                                 |
|--------|------|--------------------------------------------|
|PacketId|byte  |The id that defines the packet.             |
|Token   |string|The session token used to verify the sender.|

## Login : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                    |
|--------|------|-----------------------------------------------|
|LoginKey|string|The key from the server to get a session token.|

## Logout : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                    |
|--------|------|-----------------------------------------------|

## Accept : [MCCommPacket](#mccommpacket)

|Name|Type|Description|
|----|----|-----------|

## Deny : [MCCommPacket](#mccommpacket)

|Name  |Type  |Description               |
|------|------|--------------------------|
|Reason|string|The reason for the denial.|

## Bind : [MCCommPacket](#mccommpacket)

|Name     |Type  |Description                                           |
|---------|------|------------------------------------------------------|
|PlayerId |string|The id of the ingame player.                          |
|PlayerKey|short |The key to bind to displayed on the VoiceCraft client.|
|Gamertag |string|The gamertag of the ingame player.                    |

## Update : [MCCommPacket](#mccommpacket)

|Name   |Type               |Description                                            |
|-------|-------------------|-------------------------------------------------------|
|Players|[Player](#player)[]|An array of players to update in the VoiceCraft server.|

## AckUpdate : [MCCommPacket](#mccommpacket)

|Name           |Type    |Description                                    |
|---------------|--------|-----------------------------------------------|
|SpeakingPlayers|string[]|Contains a list of speaking ingame player id's.|

## GetChannels : [MCCommPacket](#mccommpacket)

|Name    |Type                                |Description                                                           |
|--------|------------------------------------|----------------------------------------------------------------------|
|Channels|Dicionary<byte, [Channel](#channel)>|Contains a dictionary of channels registered in the VoiceCraft server.|

## GetChannelSettings : [MCCommPacket](#mccommpacket)

|Name             |Type|Description                                        |
|-----------------|----|---------------------------------------------------|
|ChannelId        |byte|The channel id to get the settings from.           |
|ProximityDistance|int |The proximity distance that players can hear other.|
|ProximityToggle  |bool|Defines whether proximity is enabled or not.       |
|VoiceEffects     |bool|Defines whether voice effects is enabled or not.   |

## SetChannelSettings : [MCCommPacket](#mccommpacket)

|Name             |Type|Description                                            |
|-----------------|----|-------------------------------------------------------|
|ChannelId        |byte|The channel id to get the settings from.               |
|ProximityDistance|int |The proximity distance that players can hear other.    |
|ProximityToggle  |bool|Defines whether proximity is enabled or not.           |
|VoiceEffects     |bool|Defines whether voice effects is enabled or not.       |
|ClearSettings    |bool|Defines whether the channel settings should be cleared.|

## GetDefaultSettings : [MCCommPacket](#mccommpacket)

|Name             |Type|Description                                        |
|-----------------|----|---------------------------------------------------|
|ProximityDistance|int |The proximity distance that players can hear other.|
|ProximityToggle  |bool|Defines whether proximity is enabled or not.       |
|VoiceEffects     |bool|Defines whether voice effects is enabled or not.   |

## SetDefaultSettings : [MCCommPacket](#mccommpacket)

|Name             |Type|Description                                        |
|-----------------|----|---------------------------------------------------|
|ProximityDistance|int |The proximity distance that players can hear other.|
|ProximityToggle  |bool|Defines whether proximity is enabled or not.       |
|VoiceEffects     |bool|Defines whether voice effects is enabled or not.   |

## GetParticipants : [MCCommPacket](#mccommpacket)

|Name   |Type    |Description                                                |
|-------|--------|-----------------------------------------------------------|
|Players|string[]|Contains a list of connected and binded ingame player id's.|

## DisconnectParticipant : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                               |
|--------|------|------------------------------------------|
|PlayerId|string|The player id that you want to disconnect.|

## GetParticipantBitmask : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                        |
|--------|------|---------------------------------------------------|
|PlayerId|string|The player id that you want to get the bitmask for.|
|Bitmask |uint  |The player's bitmask.                              |

## SetParticipantBitmask : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                        |
|--------|------|---------------------------------------------------|
|PlayerId|string|The player id that you want to set the bitmask for.|
|Bitmask |uint  |The bitmask to set.                                |

## MuteParticipant : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                           |
|--------|------|--------------------------------------|
|PlayerId|string|The player id you want to server mute.|

## UnmuteParticipant : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                             |
|--------|------|----------------------------------------|
|PlayerId|string|The player id you want to server unmute.|

## DeafenParticipant : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                             |
|--------|------|----------------------------------------|
|PlayerId|string|The player id you want to server deafen.|

## UndeafenParticipant : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                               |
|--------|------|------------------------------------------|
|PlayerId|string|The player id you want to server undeafen.|

## ANDModParticipantBitmask : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                               |
|--------|------|----------------------------------------------------------|
|PlayerId|string|The player id that you want to AND modify the bitmask for.|
|Bitmask |uint  |The bitmask to apply.                                     |

## ORModParticipantBitmask : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                              |
|--------|------|---------------------------------------------------------|
|PlayerId|string|The player id that you want to OR modify the bitmask for.|
|Bitmask |uint  |The bitmask to apply.                                    |

## XORModParticipantBitmask : [MCCommPacket](#mccommpacket)

|Name    |Type  |Description                                               |
|--------|------|----------------------------------------------------------|
|PlayerId|string|The player id that you want to XOR modify the bitmask for.|
|Bitmask |uint  |The bitmask to apply.                                     |

## ChannelMove : [MCCommPacket](#mccommpacket)

|Name     |Type  |Description                          |
|---------|------|-------------------------------------|
|PlayerId |string|The player id that you want to move. |
|ChannelId|byte  |The channel id to move the player to.|

# Classes

## Player
|Name       |Type   |Description                                                                 |
|-----------|-------|----------------------------------------------------------------------------|
|PlayerId   |string |The player Id to update.                                                    |
|DimensionId|string |The dimension/environment that the player is in.                            |
|Location   |Vector3|The location of the player.                                                 |
|Rotation   |float  |The rotation of the player (head rotation).                                 |
|CaveDensity|float  |Used as a factor for calculating how much echo minus by per player distance.|
|IsDead     |bool   |If the player is dead.                                                      |
|InWater    |bool   |If the player is in water, used to produce muffle sounds.                   |


## Channel

|Name            |Type                                |Description                                                       |
|----------------|------------------------------------|------------------------------------------------------------------|
|Name            |string                              |The name of the channel.                                          |
|Password        |string                              |The password to join the channel.                                 |
|Locked          |bool                                |If the channel is locked and cannot be joined by a client request.|
|Hidden          |bool                                |If the channel is hidden from clients (also locks the channel).   |
|OverrideSettings|[ChannelOverride](#channeloverride)?|The settings on the channel that override the default settings    |

## ChannelOverride
|Name             |Type|Description                                        |
|-----------------|----|---------------------------------------------------|
|ProximityDistance|int |The proximity distance that players can hear other.|
|ProximityToggle  |bool|Defines whether proximity is enabled or not.       |
|VoiceEffects     |bool|Defines whether voice effects is enabled or not.   |