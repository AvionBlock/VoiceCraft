using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Application
{
    public class VoiceCraftServer : IDisposable
    {
        public static readonly Version Version = new(1, 1, 0);

        //Public Properties
        public VoiceCraftConfig Config { get; set; }
        public EventBasedNetListener Listener { get; }
        public VoiceCraftWorld World { get; } = new();
        public NetworkSystem NetworkSystem { get; }
        public AudioEffectSystem AudioEffectSystem { get; }
        private readonly WorldSystem _worldSystem;
        private readonly VisibilitySystem _visibilitySystem;
        private readonly EntityEventsSystem _entityEventsSystem;
        private readonly NetManager _netManager;
        private bool _isDisposed;

        public VoiceCraftServer(VoiceCraftConfig? config = null)
        {
            Config = config ?? new VoiceCraftConfig();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true
            };

            //Has to be initialized in this order otherwise shit falls apart.
            AudioEffectSystem = new AudioEffectSystem(this);
            NetworkSystem = new NetworkSystem(this, _netManager);
            _worldSystem = new WorldSystem(this);
            _visibilitySystem = new VisibilitySystem(this);
            _entityEventsSystem = new EntityEventsSystem(this);
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods
        public bool Start()
        {
            return _netManager.IsRunning || _netManager.Start((int)Config.Port);
        }

        public void Update()
        {
            _netManager.PollEvents();
            _visibilitySystem.Update();
            _entityEventsSystem.Update();
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.DisconnectAll();
            _netManager.Stop();
        }

        #endregion

        #region Dispose

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
                World.Dispose();
                _worldSystem.Dispose();
                NetworkSystem.Dispose();
                _entityEventsSystem.Dispose();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}