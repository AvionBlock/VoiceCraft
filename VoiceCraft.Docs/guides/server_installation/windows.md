# Getting Started

Go to the [Latest Release](https://github.com/AvionBlock/VoiceCraft/releases/latest) page and download the file named
`VoiceCraft.Server.Windows.<Architecture>.zip`.

The \<Architecture\> to choose and download needs to be the architecture of your CPU, the most common one is x64.

You will also need to download and install the [Dotnet9.0](https://dotnet.microsoft.com/en-us/download) drivers before
being able to run the application if you have not done so already.

# Installing & running the server

1. Go to where you have downloaded the `VoiceCraft.Server.Windows.<Architecture>.zip` file in your file manager and
   extract the file.
2. Open a terminal inside the extracted folder and run the command `./VoiceCraft.Server.exe`.

# HTTP Server Sided Setup (BDS Only)

Go to the [Latest Release](https://github.com/AvionBlock/VoiceCraft/releases/latest) page and download the file named
`VoiceCraft.Addon.Core.McHttp.zip`.

> [!IMPORTANT]
> This setup requires that you have a world with beta-api's enabled and targeted by the BDS software.

## Installing the addon

1. Once you have downloaded the addon above, extract the addon and place the `RP` folder in `<MCServer>/resource_packs/`
   and the `BP` folder in `<MCServer>/behavior_packs/`. You can rename the `RP` and `BP` folders to prevent any future
   conflicts.
2. Navigate into `<MCServer>/config/default/` and open the `permissions.json` file in the file editor of your choice.
3. Edit the contents of the file to match what is shown below and then save and close the file.

```json
{
  "allowed_modules": [
    "@minecraft/server-gametest",
    "@minecraft/server",
    "@minecraft/server-ui",
    "@minecraft/server-admin",
    "@minecraft/server-editor",
    "@minecraft/server-net"
  ]
}
```

4. Navigate into `<MCServer>/worlds/<YourWorld>/` and open or create the `world_behavior_packs.json` file in the file
   editor of your choice.
5. Edit the contents of the file by adding the following **inside the []**:

```json
{
  "pack_id": "71ebb3ba-e9db-4546-9520-05f20b17dcb6",
  "version": [
    1,
    2,
    0
  ]
}
```

If there are multiple addon's then you will need to edit the file like this:

```json
[
  {
    "pack_id": "71ebb3ba-e9db-4546-9520-05f20b17dcb6",
    "version": [
      1,
      2,
      0
    ]
  },
  ...
]
```

6. Do the same for `world_resource_packs.json` except only add the following **inside the []**:

```json
{
  "pack_id": "30b512be-77d1-4a61-bdb7-6c2f4062f889",
  "version": [
    1,
    2,
    0
  ]
}
```

7. Save and close both files, and then you should be able to start the Minecraft server. If you have done everything
   correctly, it should show this log:

```
[2025-12-19 18:40:41:761 INFO] Pack Stack - [01] VoiceCraft.Addon.Core.McHttp.BP (id: 71ebb3ba-e9db-4546-9520-05f20b17dcb6, version: 1.1.0) @ behavior_packs/VoiceCraft.Addon.Core.McHttp_bp
```

## Connecting

1. Connect to your server in Minecraft.
2. Enter the command `/vcconnect <hostname> <loginkey>`. The hostname is the IP and port of the VoiceCraft's
   McHttp server protocol, the login key is generated in the `ServerProperties.json` file located in
   `<VCServer>/config/`. An example command of the command would be
   `/vcconnect "http://127.0.0.1:9050" e4ad1f7e-4f90-4b21-bc15-6febe580bf1c`.

# MCWSS Server Sided Setup (Singleplayer worlds only)

Go to the [Latest Release](https://github.com/AvionBlock/VoiceCraft/releases/latest) page and download the file named
`VoiceCraft.Addon.Core.McWss.zip`.

> [!WARNING]
> The MCWSS version is unstable and may break more than often or crash your world! Do not use this for more than 2 or 3
> players!

## Installing the addon

1. Once you have downloaded the addon above, rename the file from `VoiceCraft.Addon.Core.McWss.zip` to
   `VoiceCraft.Addon.Core.McWss.mcaddon` then open it, Minecraft should auto-import the addon.
2. Start Minecraft, create or edit a world and add both the `VoiceCraft.Addon.Core.McWss.BP` and
   `VoiceCraft.Addon.Core.McWss.RP` to your world.

## Connecting

1. Open your Minecraft world that you have added the above addon's on.
2. Enter the command `/connect 127.0.0.1:9051`. This command assumes you are connecting to the VoiceCraft server that is
   hosted on the same device/computer, if not, you will need to get your public or local IP address of the target
   computer/device and enter that instead of `127.0.0.1`.
3. Enter the command `/vcconnect <loginkey>`. The login key is generated in the `ServerProperties.json` file located in
   `<VoiceCraftServer>/config/`. An example command of the command would be
   `/vcconnect e4ad1f7e-4f90-4b21-bc15-6febe580bf1c`.
