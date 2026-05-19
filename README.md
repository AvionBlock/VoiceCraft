# VoiceCraft Proximity Chat

Proximity voice chat software for Minecraft Bedrock Edition supporting Windows, Android, iOS, Linux and MacOS.

<p align="center">
  <img style="margin: 10" width="300" height="300" src="./VoiceCraft.Client/VoiceCraft.Client/Assets/vc.png"/>
</p>

> [!WARNING]
> VOICECRAFT DOES NOT REQUIRE THE USE OF ANY THIRD PARTY SERVICE! VOICECRAFT IS ALSO NOT A MOD, PLUGIN OR STANDALONE
> ADDON/WORLD! It is a collection of both addons, servers and client software working together in order to simulate
> proximity chat.

> [!NOTE]
> VoiceCraft is also not a standard voice chat that comes with groups or channels. It is designed to be customized
> through the api allowing recreation of channels, proximity, effects and more through the session based Addon API. This
> is essentially up to the server owner to install or add-on developer to implement.

## Project Description

VoiceCraft is a cross-platform proximity voice chat solution for minecraft bedrock edition. VoiceCraft supports a wide
range of devices to increase its availability to players and can indirectly support any other devices such as consoles
through the standard binding system.

VoiceCraft is developed in C# for both the server and client application which uses the avalonia framework and uses an
addon developed in JavaScript to establish a connection to the vanilla minecraft server.
VoiceCraft also uses the opus codec for audio data compression and SpeexDSP for voice enhancements with optional support
for hardware related preprocessors on android devices.

There is also a comprehensive API system in place that addon developer's can use to customize VoiceCraft's behavior,
audio effects, audio simulations, authentication, and more!

<p align="center">
  <img width="800" src="./Images/MainPage.png">
</p>

## Packages, Guides And Resources

- ### [Wiki](https://voicecraft.avion.team/en)
- ### [Latest Release](https://github.com/AvionBlock/VoiceCraft/releases/latest)
- ### [Addon](https://github.com/AvionBlock/VoiceCraft-Addon)
- ### [GeyserVoice](https://github.com/AvionBlock/GeyserVoice)
- ### [PocketMine Plugin](https://github.com/AvionBlock/VoiceCraft-PocketMine)
- ### [Docker Package](https://github.com/AvionBlock/VoiceCraft-Docker/pkgs/container/voicecraft)
- ### [Docker Page](https://hub.docker.com/r/sinevector241/voicecraft/tags)

## Supported Devices

- ✅ Fully and natively supported.
- ❎ Can be supported but no reason to.
- ❗ Unknown status (limited support)
- ❌ Not planned, Not supported.

| Device      | x64 | x86 | arm32 | arm64 | Supported Audio Backends         |
|-------------|-----|-----|-------|-------|:---------------------------------|
| Linux       | ✅   | ❌   | ✅     | ✅     | PulseAudio, JACK, ALSA           |
| Android     | ❎   | ❎   | ❌     | ✅     | AAudio, OpenSL                   |
| Windows     | ✅   | ✅   | ❌     | ✅     | WinMM, WASAPI, DirectSound, JACK |
| iOS         | ❌   | ❌   | ✅     | ✅     | Core Audio                       |
| MacOS       | ✅   | ❌   | ❌     | ✅     | Core Audio, JACK                 |
| Web         | ❗   | ❗   | ❗     | ❗     | Web API                          |
| XBOX        | ❌   | ❌   | ❌     | ❌     | N.A.                             |
| PlayStation | ❌   | ❌   | ❌     | ❌     | N.A.                             |
| Switch      | ❌   | ❌   | ❌     | ❌     | N.A.                             |

## Hosts

- Atrioxhosting €0.63/m: https://atrioxhost.com/voicecraft

## Project Dependencies

> [!NOTE]
> All dotnet and microsoft extension packages aren't listed. If you wish to view all dependencies, You can look at
> the [Directory.Packages.props](./Directory.Packages.props) file.

### All Projects

- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)

### Client

- [SpeexDSPSharp](https://github.com/AvionBlock/SpeexDSPSharp)
- [OpusSharp](https://github.com/AvionBlock/OpusSharp)
- [Message.Avalonia](https://github.com/xiyaowong/Message.Avalonia)
- [SharpHook](https://github.com/TolikPylypchuk/SharpHook)
- [discord-rpc-csharp](https://github.com/Lachee/discord-rpc-csharp)
- [Soundflow](https://github.com/LSXPrime/SoundFlow/tree/master)

### Server

- [Certes](https://github.com/fszlin/certes)
- [Spectre.Console](https://github.com/spectreconsole/spectre.console)
- [OpenPort.Net](https://github.com/AvionBlock/OpenPort.Net)

## Web Client Transport

The browser client uses the WebRTC transport. The server WebRTC transport has two network surfaces:

- Signaling: `WebRtcConfig.SignalingUrl`, usually `ws://host:port/` or `wss://host:port/`.
- Data channel traffic: UDP ICE candidates constrained by `WebRtcConfig.PortRangeStart` and `WebRtcConfig.PortRangeEnd`.
- Optional ICE servers: `WebRtcConfig.IceServers` and browser `NetworkSettings.WebRtcIceServers`. These are empty by
  default include public STUN servers. TURN servers usually require provider credentials and can be added here.
- Optional NAT port mapping: `WebRtcConfig.PortMapping.Enabled` uses OpenPort.Net to open the signaling TCP port and
  UDP range through UPnP IGD, NAT-PMP or PCP, then adds the mapped public UDP candidates to WebRTC.

For public or containerized servers, open the signaling TCP port and the configured UDP range. When the signaling URL uses
`wss://`, VoiceCraft loads the PFX certificate from `WebRtcConfig.Tls.CertificatePath`.

By default, `WebRtcConfig.Tls.CertificateMode` is `lets-encrypt`. VoiceCraft requests the certificate through ACME
HTTP-01 using the public DNS name from `WebRtcConfig.Tls.Acme.Domains` or `WebRtcConfig.SignalingUrl`. This requires the
domain to resolve to the server and TCP/80 to be reachable while the certificate is issued. If
`Tls.Acme.AutoMapHttpChallengePort` is enabled, VoiceCraft also tries to open TCP/80 through OpenPort.Net temporarily.

For local/private deployments, set `CertificateMode` to `self-signed`. VoiceCraft will create a self-signed PFX when the
file is missing, but browsers only trust it after the certificate or CA is trusted by the user or operating system. To
provide your own PFX, set `CertificateMode` to `existing`.

Default STUN servers include Google's common public STUN endpoints and Cloudflare STUN. Public TURN is not hard-coded
because open TURN relays require credentials to avoid abuse. For TURN fallback, use your own coturn instance or a provider
such as Metered/Open Relay, Cloudflare Realtime TURN, or Twilio Network Traversal Service.
