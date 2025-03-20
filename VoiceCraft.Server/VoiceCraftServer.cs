using LiteNetLib;
using VoiceCraft.Core;
using VoiceCraft.Server.Systems;

namespace VoiceCraft.Server
{
    public class VoiceCraftServer : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static readonly Version Version = new(1, 1, 0);

        public event Action? OnStarted;
        public event Action? OnStopped;

        //Public Properties
        public ServerProperties Properties { get; }
        public EventBasedNetListener Listener { get; }
        public VoiceCraftWorld World { get; } = new();
        public WorldSystem WorldSystem { get; }
        public NetworkSystem NetworkSystem { get; }
        public VisibilitySystem VisibilitySystem { get; }

        private readonly NetManager _netManager;
        private bool _isDisposed;

        public VoiceCraftServer(ServerProperties? properties = null)
        {
            Properties = properties ?? new ServerProperties();
            Listener = new EventBasedNetListener();
            _netManager = new NetManager(Listener)
            {
                AutoRecycle = true
            };

            WorldSystem = new WorldSystem(this);
            NetworkSystem = new NetworkSystem(this, _netManager);
            VisibilitySystem = new VisibilitySystem(this);
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        #region Public Methods
        public void Start(int port)
        {
            if (_netManager.IsRunning) return;
            _netManager.Start(port);
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