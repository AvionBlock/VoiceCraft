using System;
using System.Numerics;
using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Core.World;

namespace VoiceCraft.Network.World
{
    public class VoiceCraftNetworkEntity : VoiceCraftEntity
    {
        private bool _serverDeafened;
        private bool _serverMuted;

        public VoiceCraftNetworkEntity(
            NetPeer netPeer,
            int id,
            Guid userGuid,
            Guid serverUserGuid,
            string locale,
            PositioningType positioningType,
            bool serverMuted,
            bool serverDeafened,
            VoiceCraftWorld world) : base(id, world)
        {
            Name = "New Client";
            NetPeer = netPeer;
            UserGuid = userGuid;
            ServerUserGuid = serverUserGuid;
            Locale = locale;
            PositioningType = positioningType;
            ServerMuted = serverMuted;
            ServerDeafened = serverDeafened;
        }

        public NetPeer NetPeer { get; }
        public Guid UserGuid { get; private set; }
        public Guid ServerUserGuid { get; private set; }
        public string Locale { get; private set; }
        public PositioningType PositioningType { get; }

        public bool ServerMuted
        {
            get => _serverMuted;
            set
            {
                if (_serverMuted == value) return;
                _serverMuted = value;
                OnServerMuteUpdated?.Invoke(_serverMuted, this);
            }
        }

        public bool ServerDeafened
        {
            get => _serverDeafened;
            set
            {
                if (_serverDeafened == value) return;
                _serverDeafened = value;
                OnServerDeafenUpdated?.Invoke(_serverDeafened, this);
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