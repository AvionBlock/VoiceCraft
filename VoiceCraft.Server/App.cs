namespace VoiceCraft.Server
{
    public class App
    {
        private const int UpdateInterval = 20;
        private readonly VoiceCraftServer _server;

        public App()
        {
            _server = new VoiceCraftServer();
            _server.OnStarted += OnStarted;
            _server.OnStopped += OnStopped;
        }

        public async Task Start()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Loading...";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"__     __    _           ____            __ _");
            Console.WriteLine(@"\ \   / /__ (_) ___ ___ / ___|_ __ __ _ / _| |_");
            Console.WriteLine(@" \ \ / / _ \| |/ __/ _ \ |   | '__/ _` | |_| __|");
            Console.WriteLine(@"  \ V / (_) | | (_|  __/ |___| | | (_| |  _| |_");
            Console.WriteLine(@"   \_/ \___/|_|\___\___|\____|_|  \__,_|_|  \__|");
#if DEBUG
            Console.WriteLine($"[Server: {VoiceCraftServer.Version}]===============[DEBUG]\n");
#else
            Console.WriteLine($"[Server: {VoiceCraftServer.Version}]================[RELEASE]\n");
#endif
            Console.WriteLine("Starting VoiceCraft server...");
            _server.Start(9050);

            for (var i = 0; i < 1000; i++)
            {
                _server.World.CreateEntity();
            }

            var tick1 = Environment.TickCount;
            while (true)
            {
                _server.Update();
                Console.WriteLine(Environment.TickCount - tick1);
                tick1 = Environment.TickCount;
                await Task.Delay(UpdateInterval);
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