# VoiceCraft Proximity Chat

Proximity voice chat for Minecraft Bedrock Edition supporting Windows, Android, iOS, Linux and MacOS.

<p align="center">
  <img style="margin: 10" width="300" height="300" src="./VoiceCraft.Client/VoiceCraft.Client/Assets/vc.png"/>
</p>

> [!NOTE]
> VOICECRAFT DOES NOT REQUIRE THE USE OF ANY THIRD PARTY SERVICE!

## Project Description

VoiceCraft is a cross platform proximity voice chat solution for minecraft bedrock edition. VoiceCraft supports a wide
range of devices to increase it's availability to players and can indirectly support any other devices such as consoles
through the binding system.

VoiceCraft is coded in C# for both the server and applications using the Avalonia framework. VoiceCraft also uses the
Opus codec for voice data compression and SpeexDSP for voice enhancements.

There is also a comprehensive API system in place that addon developer's can use to customize VoiceCraft's behavior,
audio effects, audio simulations and more!

<p align="center">
  <img width="800" src="./Images/Screenshot from 2025-04-07 15-06-51.png">
</p>

## Supported Devices

- ✅ Fully and natively supported.
- ❎ Can be supported but no reason to.
- ❗ Unknown status (limited support)
- ❌ Not planned, Not supported.

| Device      | x64 | x86 | arm32 | arm64 |
|-------------|-----|-----|-------|-------|
| Linux       | ✅   | ❌   | ✅     | ✅     |
| Android     | ❎   | ❎   | ✅     | ✅     |
| Windows     | ✅   | ✅   | ❌     | ✅     |
| iOS         | ❌   | ❌   | ✅     | ✅     |
| MacOS       | ✅   | ❌   | ❌     | ✅     |
| Web         | ❗   | ❗   | ❗     | ❗     |
| XBOX        | ❌   | ❌   | ❌     | ❌     |
| PlayStation | ❌   | ❌   | ❌     | ❌     |
| Switch      | ❌   | ❌   | ❌     | ❌     |

## Hosts

- Atrioxhosting €0.44/m: https://atrioxhost.com/voicecraft

## Project Dependencies

> [!NOTE]
> Some dependencies are not used in full for example VoiceCraft only depends on NAudio.Core for all platforms but also
> depends on NAudio.WinForms and NAudio.WinMM for windows.

- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
- [SpeexDSPSharp](https://github.com/AvionBlock/SpeexDSPSharp)
- [OpusSharp](https://github.com/AvionBlock/OpusSharp)
- [Notification.Avalonia](https://github.com/AvaloniaCommunity/Notification.Avalonia)
- [LiteNetLib](https://github.com/RevenantX/LiteNetLib)
- [Jeek.Avalonia.Localization](https://github.com/tifish/Jeek.Avalonia.Localization)
- [NAudio](https://github.com/naudio/NAudio)
- [NWaves](https://github.com/ar1st0crat/NWaves)
- [OpenTK (OpenAL)](https://github.com/opentk/opentk)
