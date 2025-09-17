# Index

- [Login](#login--voicecraftpacket)
- [Logout](#logout--voicecraftpacket)
- [Accept](#accept--voicecraftpacket)
- [Deny](#deny--voicecraftpacket)
- [Ack](#ack--voicecraftpacket)
- [Ping](#ping--voicecraftpacket)
- [PingInfo](#pinginfo--voicecraftpacket)
- [Binded](#binded--voicecraftpacket)
- [Unbinded](#unbinded--voicecraftpacket)
- [ParticipantJoined](#participantjoined--voicecraftpacket)
- [ParticipantLeft](#participantleft--voicecraftpacket)
- [Mute](#mute--voicecraftpacket)
- [Unmute](#unmute--voicecraftpacket)
- [Deafen](#deafen--voicecraftpacket)
- [Undeafen](#undeafen--voicecraftpacket)
- [JoinChannel](#joinchannel--voicecraftpacket)
- [LeaveChannel](#leavechannel--voicecraftpacket)
- [AddChannel](#addchannel--voicecraftpacket)
- [RemoveChannel](#removechannel--voicecraftpacket)
- [UpdatePosition](#updateposition--voicecraftpacket)
- [FullUpdatePosition](#fullupdateposition--voicecraftpacket)
- [UpdateEnvironmentId](#updateenvironmentid--voicecraftpacket)
- [ClientAudio](#clientaudio--voicecraftpacket)
- [ServerAudio](#serveraudio--voicecraftpacket)

## VoiceCraftPacket

|Name    |Type  |Description                                                    |
|--------|------|---------------------------------------------------------------|
|PacketId|byte  |The id that defines the packet.                                |
|Sequence|uint? |The sequence of the packet if it's reliable.                   |
|Id      |long  |The private id to communicate with either the client or server.|

## Login : [VoiceCraftPacket](#voicecraftpacket)

|Name           |Type            |Description|
|---------------|----------------|-----------|
|Key            |short           ||
|PositioningType|PositioningTypes||
|Version        |string          ||

## Logout : [VoiceCraftPacket](#voicecraftpacket)

|Name  |Type  |Description|
|------|------|-----------|
|Reason|string||

## Accept : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## Deny : [VoiceCraftPacket](#voicecraftpacket)

|Name  |Type  |Description|
|------|------|-----------|
|Reason|string||

## Ack : [VoiceCraftPacket](#voicecraftpacket)

|Name          |Type|Description|
|--------------|----|-----------|
|PacketSequence|uint||

## Ping : [VoiceCraftPacket](#voicecraftpacket)

|Name  |Type  |Description|
|------|------|-----------|

## PingInfo : [VoiceCraftPacket](#voicecraftpacket)

|Name                 |Type            |Description|
|---------------------|----------------|-----------|
|PositioningType      |PositioningTypes||
|ConnectedParticipants|int             ||
|MOTD                 |string          ||

## Binded : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type  |Description|
|----|------|-----------|
|Name|string||

## Unbinded : [VoiceCraftPacket](#voicecraftpacket)

|Name  |Type  |Description|
|------|------|-----------|

## ParticipantJoined : [VoiceCraftPacket](#voicecraftpacket)

|Name      |Type  |Description|
|----------|------|-----------|
|Key       |short ||
|IsDeafened|bool  ||
|IsMuted   |bool  ||
|Name      |string||

## ParticipantLeft : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## Mute : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## Unmute : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## Deafen : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## Undeafen : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|
|Key |short||

## JoinChannel : [VoiceCraftPacket](#voicecraftpacket)

|Name     |Type  |Description|
|---------|------|-----------|
|ChannelId|byte  ||
|Password |string||

## LeaveChannel : [VoiceCraftPacket](#voicecraftpacket)

|Name|Type |Description|
|----|-----|-----------|

## AddChannel : [VoiceCraftPacket](#voicecraftpacket)

|Name            |Type  |Description|
|----------------|------|-----------|
|RequiresPassword|bool  ||
|ChannelId       |byte  ||
|Locked          |bool  ||
|Name            |string||

## RemoveChannel : [VoiceCraftPacket](#voicecraftpacket)

|Name     |Type|Description|
|---------|----|-----------|
|ChannelId|byte||

## UpdatePosition : [VoiceCraftPacket](#voicecraftpacket)

|Name    |Type   |Description|
|--------|-------|-----------|
|Position|Vector3||

## FullUpdatePosition : [VoiceCraftPacket](#voicecraftpacket)

|Name       |Type   |Description|
|-----------|-------|-----------|
|Position   |Vector3||
|Rotation   |float  ||
|CaveDensity|float  ||
|IsDead     |bool   ||
|InWater    |bool   ||

## UpdateEnvironmentId : [VoiceCraftPacket](#voicecraftpacket)

|Name         |Type  |Description|
|-------------|------|-----------|
|EnvironmentId|string||

## ClientAudio : [VoiceCraftPacket](#voicecraftpacket)

|Name       |Type  |Description|
|-----------|------|-----------|
|PacketCount|uint  ||
|Audio      |byte[]||

## ServerAudio : [VoiceCraftPacket](#voicecraftpacket)

|Name       |Type  |Description|
|-----------|------|-----------|
|Key        |short ||
|PacketCount|uint  ||
|Volume     |float ||
|EchoFactor |float ||
|Rotation   |float ||
|Muffled    |bool  ||
|Audio      |byte[]||