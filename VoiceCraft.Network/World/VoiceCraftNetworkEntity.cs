using System;
using System.Numerics;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;

namespace VoiceCraft.Network.World
{
    public class VoiceCraftNetworkEntity(VoiceCraftNetPeer netPeer, int id) : VoiceCraftEntity(id)
    {
        public VoiceCraftNetPeer NetPeer { get; } = netPeer;
        public Guid UserGuid { get; private set; } = netPeer.UserGuid;
        public Guid ServerUserGuid { get; private set; } = netPeer.ServerUserGuid;
        public string Locale { get; private set; } = netPeer.Locale;
        public PositioningType PositioningType { get; } = netPeer.PositioningType;

        public bool ServerMuted
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnServerMuteUpdated?.Invoke(field, this);
            }
        }

        public bool ServerDeafened
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                OnServerDeafenUpdated?.Invoke(field, this);
            }
        }

        //Events
        public event Action<string, VoiceCraftNetworkEntity>? OnSetTitle;
        public event Action<string, VoiceCraftNetworkEntity>? OnSetDescription;
        public event Action<bool, VoiceCraftNetworkEntity>? OnServerMuteUpdated;
        public event Action<bool, VoiceCraftNetworkEntity>? OnServerDeafenUpdated;

        public void SetTitle(string title)
        {
            OnSetTitle?.Invoke(title, this);
        }

        public void SetDescription(string description)
        {
            OnSetDescription?.Invoke(description, this);
        }

        public override void Reset()
        {
            //Doesn't remove the entity from the world.
            Name = "New Client";
            WorldId = string.Empty;
            Position = Vector3.Zero;
            Rotation = Vector2.Zero;
            EffectBitmask = ushort.MaxValue;
            TalkBitmask = ushort.MaxValue;
            ListenBitmask = ushort.MaxValue;
            ServerMuted = false;
            ServerDeafened = false;
            //Only clear out properties. Visible entities are handled by the visibility system.
            ClearProperties();
        }

        public override void Destroy()
        {
            base.Destroy();
            OnSetTitle = null;
            OnSetDescription = null;
            OnServerMuteUpdated = null;
            OnServerDeafenUpdated = null;
        }
    }
}