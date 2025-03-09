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
            while (true)
            {
                if(UpdateInterval > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(UpdateInterval));
                _server.Update();
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