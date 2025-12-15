# Getting Started
Go to the [Latest Release](https://github.com/AvionBlock/VoiceCraft/releases/latest) page and download the following files:
- `VoiceCraft.Addon.zip`
- `VoiceCraft.Server.zip` *Download available here https://github.com/AvionBlock/VoiceCraft/releases/tag/v1.0.7*

if you do not have a bedrock dedicated server already running or setup, you can download the appropriate file here https://www.minecraft.net/en-us/download/server/bedrock.

Once downloaded. Extract all the downloaded files.

## Installing The Addon
To install the addon you will need to put it onto a pre-existing world. There are 2 ways to go about this. First is to install the addon onto minecraft then add it onto a world then export that world or the second is to do it manually.

If you are doing it manually. Here are the steps below:

1. Navigate into the folder of your world folder and create or open `world_behavior_packs.json` file.
2. Inside your `world_behavior_packs.json`, add the following content:
```json
{
    "pack_id" : "87b49038-e613-4216-b265-6101b83376e2",
    "version" : [ 1, 0, 7 ]
}
```
your file should look like this in case you have just created the file for the first time:
```json
[
    {
        "pack_id" : "87b49038-e613-4216-b265-6101b83376e2",
        "version" : [ 1, 0, 7 ]
    }
]
```
Otherwise it should look similar to this if you have multiple addon's installed.
```json
[
    {
        "pack_id" : "...",
        "version" : [0, 0, 0]
    },
    {
        "pack_id" : "87b49038-e613-4216-b265-6101b83376e2",
        "version" : [ 1, 0, 7 ]
    }
]
```

3. Create a folder inside your world folder called `behavior_packs`.
4. Paste the addon folder inside of the `behavior_packs` folder. The directory to the addon folder must match exactly this directory: `yourworld/behavior_packs/VoiceCraft.Addon.BP/`. It must not look like `yourworld/behavior_packs/VoiceCraft.Addon.BP/VoiceCraft.Addon.BP/` etc...
5. In order for the addon to work, you must enable the `@minecraft/server-net` module. This module allows the addon to communicate from within the minecraft server to the external voicecraft server using HTTP. To do this, you will need to navigate to `YourServer/config/default/permissions.json` and replace the contents of the file with
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

## Starting The Server
Navigate inside your extracted `VoiceCraft.Server` folder and open a terminal inside that directory. Next, you will need to run the command `dotnet VoiceCraft.Server.dll`.

If the server does not start, this means a property in `ServerProperties.json` is invalid or the MCComm or UDP protocols failed to start due to not being able to bind to its port. You will need to close the application that is bound the ports or change the ports in `ServerProperties.json`.

If you have a firewall enabled then you will need to port forward any ports that require the VoiceCraft server to communicate to a separate computer/server. This is most common for the UDP port where people across the internet will need to connect to the VoiceCraft server. However if you are only using VoiceCraft on a local network and you do not wish anyone outside of your network to connect to the VoiceCraft server then you do not need to do anything.

## Linking The Servers
Once both servers are up and running. 

Join the minecraft server through minecraft and type the following command in chat. `/voicecraft:connect <yourserverIP> <MCCommPort> <ServerKey>` (Your server key is given to you in the VoiceCraft console when you start it).

If you do not know what is your server's IP, check this question in the [FAQ](../../faq.md#how-do-i-find-the-voicecraft-servers-ip-address-and-port)