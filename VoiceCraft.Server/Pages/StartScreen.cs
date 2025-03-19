using Spectre.Console;

namespace VoiceCraft.Server.Pages
{
    public class StartScreen : IPage
    {
        private readonly Rows _rows;

        public StartScreen()
        {
            _rows = new Rows(
                new FigletText("VoiceCraft").Color(Color.Green).Justify(Justify.Center),
#if DEBUG
                new Text($"[Server: {VoiceCraftServer.Version}][DEBUG]"),
#else
                new Text($"[Server: {VoiceCraftServer.Version}]================[RELEASE]\n"),
#endif
                new Text("Starting VoiceCraft server...")
                );
        }

        public void Render()
        {
            AnsiConsole.Write(_rows);
        }

        public void Dispose()
        {
            AnsiConsole.Clear();
        }
    }
}