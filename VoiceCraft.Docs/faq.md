# FAQ

Commonly asked questions about VoiceCraft.

## How do I find the VoiceCraft server's IP address and port?

To find the IP address and port for your server.

# [Local Network](#tab/localNetwork)

You can type `ipconfig`(windows) or `ifconfig`(linux) in terminal and locate the IPV4 address. Your port is given to you
in VoiceCraft console.

# [Internet Network](#tab/internetNetwork)

You can find your IP address at https://whatismyipaddress.com/. You must visit this website from the device where your
server is hosted. Your port is given to you in VoiceCraft console.

---

## Does VoiceCraft work on mobile?

**Yes** it does, iOS (soon) and Android 6+ are supported.

## Do all players have to download the app for it to work?

**Yes**. Not the server though. Just the client app.

## Can you make it PocketMine compatible?

**No**. I will only be providing resources to vanilla BDS. You can make the PocketMine port yourself if you wanted to. Which
in that case you can look at the addon and take the protocols from there and port to a PocketMine plugin. However, there
is a separate PocketMine being worked on by another maintainer (Miniontoby) which will be available later.

## Will VoiceCraft work if I am using Apex Hosting, Aternos or some other minecraft hosting provider?

**Depends**. If your hosting provider allows communication from minecraft server to VoiceCraft server then yes. It will
work. However, you need to host the VoiceCraft server on a server with the MCComm port open and forwarded in order for
communication to happen. For Aternos, you will need to use client sided mode as Aternos blocks HTTP requests 
(from testing).

## Does VoiceCraft work on console?

**Not directly**. But yes it will work **with** console but not **on** console. It has been confirmed and tested to work
with console via server sided positioning but client sided **DOES NOT WORK**. There is currently some development to
port VoiceCraft to WebAssembly which is currently broken, but it does run which will work directly on consoles.

## Does VoiceCraft work on realms?

**Yes**. Only with client sided positioning enabled.

## Does VoiceCraft work with Java players on geyser servers?

**Not directly**, However there is a plugin being worked on that makes VoiceCraft, Simple Voice Chat, Plasmo Voice, 
etc... compatible with geyser/java servers. Otherwise, no.

## Can I host the server on mobile?

**Not yet**, it will be possible later through the in-app self-hosting **for android devices**.