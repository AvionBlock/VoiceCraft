# VoiceCraft Proximity Chat

Proximity voice chat for Minecraft Bedrock Edition supporting Windows, Android, iOS, Linux and MacOS.

<p align="center">
  <img style="margin: 10" width="300" height="300" src="./VoiceCraft.Client/VoiceCraft.Client/Assets/vc.png"/>
</p>

> [!WARNING]
> VOICECRAFT DOES NOT REQUIRE THE USE OF ANY THIRD PARTY SERVICE! VOICECRAFT IS ALSO NOT A MOD, PLUGIN OR STANDALONE ADDON/WORLD!

> [!NOTE]
> VoiceCraft is also not a standard voice chat that comes with groups or channels. It is designed to be customized through the api allowing recreation of channels, proximity, effects and more through the session based Addon API. This is essentially up to the server owner to install or add-on developer to implement.

## Project Description

VoiceCraft is a cross platform proximity voice chat solution for minecraft bedrock edition. VoiceCraft supports a wide
range of devices to increase it's availability to players and can indirectly support any other devices such as consoles
through the binding system.

VoiceCraft is coded in C# for both the server and applications using the Avalonia framework. VoiceCraft also uses the
Opus codec for voice data compression and SpeexDSP for voice enhancements with optional support for hardware related preprocessors on android devices.

There is also a comprehensive API system in place that addon developer's can use to customize VoiceCraft's behavior,
audio effects, audio simulations and more!

<p align="center">
  <img width="800" src="./Images/MainPage.png">
</p>

## Supported Devices

- ✅ Fully and natively supported.
- ❎ Can be supported but no reason to.
- ❗ Unknown status (limited support)
- ❌ Not planned, Not supported.

| Device      | x64 | x86 | arm32 | arm64 | Audio Backend |
|-------------|-----|-----|-------|-------|
| Linux       | ✅   | ❌   | ✅     | ✅     |OpenAL|
| Android     | ❎   | ❎   | ✅     | ✅     |Android API|
| Windows     | ✅   | ✅   | ❌     | ✅     |WinMM|
| iOS         | ❌   | ❌   | ✅     | ✅     | N.A.|
| MacOS       | ✅   | ❌   | ❌     | ✅     |N.A.|
| Web         | ❗   | ❗   | ❗     | ❗     |Web API|
| XBOX        | ❌   | ❌   | ❌     | ❌     |N.A.|
| PlayStation | ❌   | ❌   | ❌     | ❌     |N.A.|
| Switch      | ❌   | ❌   | ❌     | ❌     |N.A.|

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
- [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet)
- [discord-rpc-csharp](https://github.com/Lachee/discord-rpc-csharp)
- [Microsoft.Maui.Essentials](https://github.com/dotnet/maui)
- [Microsoft.Extensions.DependencyInjection](https://github.com/dotnet/runtime)
