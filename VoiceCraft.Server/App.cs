using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public class App
    {
        private const int UpdateInterval = 20;
        private readonly VoiceCraftServer _server = new();
        private IPage? _currentPage;
        private bool _isRunning;
        private bool _shutdown;

        public IPage? CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage?.Dispose();
                _currentPage = value;
                _currentPage?.Render();
            }
        }

        public App()
        {
            _server.OnStarted += OnStarted;
        }

        public async Task Start()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Loading...";
            CurrentPage = new StartScreen();
            _server.Start(9050);

            var tick1 = Environment.TickCount;
            while (!_shutdown)
            {
                _server.Update();
                var dist = Environment.TickCount - tick1;
                var delay = UpdateInterval - dist;
                if(delay > 0)
                    await Task.Delay(delay);
                tick1 = Environment.TickCount;
            }
            
            _server.Stop();
            _isRunning = false;
        }

        public void Shutdown()
        {
            if (!_isRunning) return;
            _shutdown = true;
        }

        private void OnStarted()
        {
            _isRunning = true;
        }
    }
}