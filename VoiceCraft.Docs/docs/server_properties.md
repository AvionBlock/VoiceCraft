# Server Properties

Server properties configuration.

```json
{
  "VoiceCraftConfig": {
    "Language": "en-US",
    "Port": 9050,
    "MaxClients": 100,
    "Motd": "VoiceCraft Proximity Chat!",
    "PositioningType": 0,
    "EnableVisibilityDisplay": true
  },
  "McWssConfig": {
    "Enabled": false,
    "LoginToken": "e9409f08-429d-4c5d-a995-6f7138d2ac8d",
    "Hostname": "ws://127.0.0.1:9051/",
    "MaxClients": 1,
    "MaxTimeoutMs": 10000,
    "DataTunnelCommand": "voicecraft:data_tunnel",
    "CommandsPerTick": 5,
    "MaxStringLengthPerCommand": 1000,
    "DisabledPacketTypes": []
  },
  "McHttpConfig": {
    "Enabled": true,
    "LoginToken": "f6e88ebe-9562-447a-a386-aa18715ee272",
    "Hostname": "http://127.0.0.1:9050/",
    "MaxClients": 1,
    "MaxTimeoutMs": 10000,
    "DisabledPacketTypes": []
  },
  "DefaultAudioEffectsConfig": {
    "1": {
      "EffectType": 1
    },
    "2": {
      "WetDry": 1,
      "MinRange": 0,
      "MaxRange": 30,
      "EffectType": 2
    },
    "4": {
      "WetDry": 1,
      "Delay": 0.5,
      "Range": 30,
      "EffectType": 4
    },
    "8": {
      "WetDry": 1,
      "EffectType": 6
    }
  }
}
```

# VoiceCraftConfig

VoiceCraft server configuration.

## Language

Defines the language the server will use when printing to console.

- Type: `string`
- Values: `"en-US"`, `"nl-NL"`

## Port

Defines the UDP port the server will use when starting the server.

- Type: `uint`
- Values: `0` - `4294967295`

> [!NOTE]
> Ports on OS level configurations may not allow values lower than 1024 or higher than 65535.

## MaxClients

Defines how many clients are allowed to connect to the server.

- Type: `uint`
- Values: `0` - `4294967295`

## Motd

Defines the `Message Of The Day` to display when a client pings the server.

> [!NOTE]
> If the setting exceeds 100 characters, VoiceCraft automatically truncates the value before sending it over the
> network.

- Type: `string`
- Values: `0` - `100` characters.

## PositioningType

Defines the allowed positioning type that VoiceCraft will verify when a client connects.

- Type: `byte`
- Values: `0` - `1`

| Value | Positioning Type |
|-------|------------------|
| 0     | Server Sided     |
| 1     | Client Sided     |

## EnableVisibilityDisplay

Defines whether the server sends visibility notifiers to the client. This affects the highlighting of player names
within the client but does not affect any other behavioral conditions.

- Type: `boolean`, `string`
- Values: `true`, `false`, `"true"`, `"false"`

# McWssConfig

McWss server configuration.

## Enabled

Defines whether the server is enabled or not. If disabled, the McWss server will not be available.

- Type: `boolean`, `string`
- Values: `true`, `false`, `"true"`, `"false"`

## LoginToken

Defines the login token required when a server client tries to establish a connection.

- Type: `string`
- Values: `any`

## HostName

Defines the hostname (IP and Port) that the server will host and listen to.

- Type: `string`
- Values: `ws://<IP>:<Port>/`

## MaxClients

Defines how many server clients are allowed to connect to the server.

- Type: `uint`
- Values: `0` - `4294967295`

## MaxTimeoutMs

Defines how long the server will wait for each ping packet before disconnecting if there is no request.

- Type: `uint`
- Values: `0` - `4294967295`

## DataTunnelCommand

Defines what Minecraft command the server will use in order to achieve packet data transfer.

- Type: `string`
- Values: `any`

## CommandsPerTick

Defines how many commands per server tick the server will send.

- Type: `uint`
- Values: `0` - `4294967295`

## MaxStringLengthPerCommand

Defines how many characters per command the server will send through each command.

- Type: `uint`
- Values: `0` - `4294967295`

## DisabledPacketTypes

> [!WARNING]
> This property is most likely to break every major or minor update!

Disables the server from sending or receiving any packets defined here (including core packets `0` - `5`). Only use this 
if you know what you are doing. Check [McApi Packets](./mcapi_packets.md) for the list of packet ID's.

- Type: `byte[]`
- Values: `0` - `255` per value.
- Example: `[ 6, 8 ]`

# McHttpConfig

McHttp server configuration.

## Enabled

Defines whether the server is enabled or not. If disabled, the McHttp server will not be available.

- Type: `boolean`, `string`
- Values: `true`, `false`, `"true"`, `"false"`

## LoginToken

Defines the login token required when a server client tries to establish a connection.

- Type: `string`
- Values: `any`

## HostName

Defines the hostname (IP and Port) that the server will host and listen to.

- Type: `string`
- Values: `http://<IP>:<Port>/`

## MaxClients

Defines how many server clients are allowed to connect to the server.

- Type: `uint`
- Values: `0` - `4294967295`

## MaxTimeoutMs

Defines how long the server will wait for each ping packet before disconnecting if there is no request.

- Type: `uint`
- Values: `0` - `4294967295`

## DisabledPacketTypes

> [!WARNING]
> This property is most likely to break every major or minor update!

Disables the server from sending or receiving any packets defined here (including core packets `0` - `5`). Only use this
if you know what you are doing. Check [McApi Packets](./mcapi_packets.md) for the list of packet ID's.

- Type: `byte[]`
- Values: `0` - `255` per value.
- Example: `[ 6, 8 ]`

# DefaultAudioEffectsConfig

Configuration for the default audio effects to apply when the server starts or is reset.

## Structure

| Bitmask | Effect |
|---------|--------|
| Ushort  | Object |

## Bitmask

Defines what bitmask the effect is enabled on. The bitmask can overlap other effects but cannot have the same bitmask
value, e.g. `2` and `3` are valid overlapping bitmasks but two effects using `2` and `2` is not valid.

- Type: `ushort`
- Values: `1` - `65535`

## Effects

### Visibility Effect

Controls whether entities are able to talk to each other if they have the same `WorldId` that is not null or empty.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `1`

### Proximity Effect

Controls the proximity volume of each entity based on the entity's `Location`.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `2`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0.0` - `1.0`

#### MinRange

The minimum range of the effect before the proximity volume takes effect beyond this range.

- Type: `int`
- Values: `-2147483648` - `2147483647`

#### MaxRange

The maximum audible range of the effect.

- Type: `int`
- Values: `-2147483648` - `2147483647`

### Directional Effect

Controls the directional audio of each entity based on the entity's `Location` and `Rotation`.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `3`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0.0` - `1.0`

### Proximity Echo Effect

Controls the echo audio of each entity based on the entity's `Location` and `CaveFactor`. The closer the entities are,
the less echo, the further away the entities are, the more echo.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `4`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0.0` - `1.0`

#### Delay

How much delay in seconds the echo will apply before outputting into the audio stream.

- Type: `float`
- Values: `0.0` - `10.0`

#### Range

The max range at which the echo will be fully applied. For example, if two entities distance is higher than 30, and the
range is 30, the echo effect will be fully applied.

- Type: `float`
- Values: `0.0` - `3.40282347E+38`

### Echo Effect

Controls the echo effect of each entity.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `5`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0` - `1`

#### Delay

How much delay in seconds the echo will apply before outputting into the audio stream.

- Type: `float`
- Values: `0.0` - `10.0`

#### Feedback

How much echo audio is applied to the audio stream.

- Type: `float`
- Values: `0.0` - `1.0`

### Proximity Muffle Effect

Controls the muffle effect of each entity based on the entity's `MuffleFactor`. This effect is useful for underwater or
suffocation simulations.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `6`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0.0` - `1.0`

### Muffle Effect

Controls the muffle effect of each entity.

#### EffectType

The effect type of the effect.

- Type: `byte`
- Values: `7`

#### WetDry

The Wet/Dry control of the effect. This controls how much of the original audio and effect modified audio is applied.

- Type: `float`
- Values: `0.0` - `1.0`