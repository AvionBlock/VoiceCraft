using System;
using System.Numerics;
using VoiceCraft.Core.World;
using VoiceCraft.Network.NetPeers;

namespace VoiceCraft.Network.World
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        public VoiceCraftNetworkEntity(
            VoiceCraftNetPeer netPeer,
            int id) : base(id)
        {
            Name = "New Client";
            NetPeer = netPeer;
            UserGuid = netPeer.UserGuid;
            ServerUserGuid = netPeer.ServerUserGuid;
            Locale = netPeer.Locale;
            PositioningType = netPeer.PositioningType;
            ServerMuted = false;
            ServerDeafened = false;
        }

        public VoiceCraftNetPeer NetPeer { get; }
        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public PositioningType PositioningType { get; }

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
            CaveFactor = 0;
            MuffleFactor = 0;
            WorldId = string.Empty;
            Position = Vector3.Zero;
            Rotation = Vector2.Zero;
            EffectBitmask = ushort.MaxValue;
            TalkBitmask = ushort.MaxValue;
            ListenBitmask = ushort.MaxValue;
            ServerMuted = false;
            ServerDeafened = false;
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