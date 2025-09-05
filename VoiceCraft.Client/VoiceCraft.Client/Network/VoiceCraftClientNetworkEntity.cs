using System;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientNetworkEntity(int id, VoiceCraftWorld world, Guid userGuid) : VoiceCraftClientEntity(id, world)
{
    public Guid UserGuid { get; private set; } = userGuid;
}