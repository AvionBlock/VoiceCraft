# Server Properties

Server properties configuration.

```json
{
  "VoiceCraftConfig": {
    "Language": "en-US",
    "Port": 9050,
    "MaxClients": 100,
    "Motd": "VoiceCraft Proximity Chat!",
    "PositioningType": 0
  },
  "McWssConfig": {
    "Enabled": true,
    "LoginToken": "e4ad1f7e-4f90-4b21-bc15-6febe580bf1c",
    "Hostname": "ws://127.0.0.1:9051/",
    "MaxClients": 1,
    "MaxTimeoutMs": 10000,
    "DataTunnelCommand": "voicecraft:data_tunnel"
  },
  "McHttpConfig": {
    "Enabled": true,
    "LoginToken": "4cfbc55e-236f-43a2-bc9e-a83be6715d0f",
    "Hostname": "http://127.0.0.1:9050/",
    "MaxClients": 1,
    "MaxTimeoutMs": 10000
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
- Values: `0`-`4294967295`

## MaxClients

Defines how many clients are allowed to connect to the server.

- Type: `uint`
- Values: `0`-`4294967295`

## Motd

Defines the `Message Of The Day` to display when a client pings the server.

> [!NOTE]
> If the setting exceeds 100 characters, VoiceCraft automatically truncates the value before sending it over the
> network.

- Type: `string`
- Values: `0`-`100` characters.

## PositioningType

Defines the allowed positioning type that VoiceCraft will verify when a client connects.

- Type: `byte`
- Values: `0`-`1`

| Value | Positioning Type |
|-------|------------------|
| 0     | Server Sided     |
| 1     | Client Sided     |

# McWssConfig

McWss server configuration.

## Enabled

Defines whether the server is enabled or not. If disabled, the McWss server will not be available.

- Type: `boolean`
- Values: `true`, `false`

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
- Values: `0`-`4294967295`

## MaxTimeoutMs

Defines how long the server will wait for each ping packet before disconnecting if there is no request.

- Type: `uint`
- Values: `0`-`4294967295`

## DataTunnelCommand

Defines what Minecraft command the server will use in order to achieve packet data transfer.

- Type: `string`
- Values: `any`

## CommandsPerTick

Defines how many commands per server tick the server will send.

- Type: `uint`
- Values: `0`-`4294967295`

## MaxStringLengthPerCommand

Defines how many characters per command the server will send through each command.

- Type: `uint`
- Values: `0`-`4294967295`

# McHttpConfig

McHttp server configuration.

## Enabled

Defines whether the server is enabled or not. If disabled, the McHttp server will not be available.

- Type: `boolean`
- Values: `true`, `false`

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
- Values: `0`-`4294967295`

## MaxTimeoutMs

Defines how long the server will wait for each ping packet before disconnecting if there is no request.

- Type: `uint`
- Values: `0`-`4294967295`