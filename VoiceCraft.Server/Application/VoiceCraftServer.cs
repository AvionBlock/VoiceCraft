using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Server.Config;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server.Application
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static readonly Version Version = new(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;

        //Public Properties
        public VoiceCraftConfig Config { get; set; }
        public EventBasedNetListener Listener { get; }
        public VoiceCraftWorld World { get; } = new();
        public WorldSystem WorldSystem { get; }
        public NetworkSystem NetworkSystem { get; }
        public VisibilitySystem VisibilitySystem { get; }
        public EntityEventsSystem EntityEventsSystem { get; }

        private readonly NetManager _netManager;
        private bool _isDisposed;

        public VoiceCraftServer(VoiceCraftConfig? config = null)
        {
            Config = config ?? new VoiceCraftConfig();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true
            };

            WorldSystem = new WorldSystem(this);
            NetworkSystem = new NetworkSystem(this, _netManager);
            VisibilitySystem = new VisibilitySystem(this);
            EntityEventsSystem = new EntityEventsSystem(this);
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods
        public void Start()
        {
            #if DEBUG
            Thread.Sleep(1000); //Debug Simulation.
            #endif
            if (_netManager.IsRunning) return;
            _netManager.Start((int)Config.Port);
            OnStarted?.Invoke();
        }

        public void Update()
        {
            _netManager.PollEvents();
            VisibilitySystem.Update();
        }

        public void Stop()
        {
            if (!_netManager.IsRunning) return;
            _netManager.DisconnectAll();
            _netManager.Stop();
            OnStopped?.Invoke();
        }

        #endregion

        #region Dispose

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                _netManager.Stop();
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