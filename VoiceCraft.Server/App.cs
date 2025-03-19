using VoiceCraft.Core.Effects;
using VoiceCraft.Server.Pages;

namespace VoiceCraft.Server
{
    public class App
    {
        private const int UpdateInterval = 20;
        private readonly VoiceCraftServer _server;
        private IPage? _currentPage;

        public IPage? CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                _currentPage?.Render();
            }
        }

        public App()
        {
            _server = new VoiceCraftServer();
            _server.OnStarted += OnStarted;
            _server.OnStopped += OnStopped;
        }

        public async Task Start()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Loading...";
            CurrentPage = new StartScreen();
            _server.Start(9050);

            for (var i = 0; i < 1000; i++)
            {
                var entity = _server.World.CreateEntity();
                entity.WorldId = "test";
                entity.TalkBitmask = ulong.MaxValue;
                entity.ListenBitmask = ulong.MaxValue;
                entity.AddEffect(new ProximityEffect() { Bitmask = ulong.MaxValue });
            }

            var tick1 = Environment.TickCount;
            while (true)
            {
                _server.Update();
                var dist = Environment.TickCount - tick1;
                var delay = UpdateInterval - dist;
                if(delay > 0)
                    await Task.Delay(delay);
                tick1 = Environment.TickCount;
            }
        }

        private static void OnStarted()
        {
            Console.WriteLine("Server started!");
        }

        private static void OnStopped()
        {
            Console.WriteLine("Server stopped!");
        }
    }
}