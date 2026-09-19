using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace VoiceCraft.Server.Runtime;

public class SimpleConsole : IAnsiConsole
{
    public Profile Profile { get; }
    public IAnsiConsoleCursor Cursor { get; }
    public IAnsiConsoleInput Input { get; }
    public IExclusivityMode ExclusivityMode { get; }
    public RenderPipeline Pipeline { get; }

    private RenderOptions _options;

    public SimpleConsole(TextWriter writer, int width = 80, int height = 25)
    {
        var output = new SimpleOutput(writer, () => width, () => height);
        var capabilities = new Capabilities()
        {
            ColorSystem = ColorSystem.TrueColor,
            Unicode = true,
            Ansi = false,
            Links = false,
            Interactive = false,
            AlternateBuffer = false
        };

        Profile = new Profile(output, capabilities, writer.Encoding);
        Cursor = new NoopConsoleCursor();
        Input = new NoopConsoleInput();
        ExclusivityMode = new ExclusivityMode();
        Pipeline = new RenderPipeline();
        
        _options = new RenderOptions(capabilities, new Size(width, height));
    }

    public void Clear(bool home)
    {
        
    }

    public void Write(IRenderable renderable)
    {
        foreach (var segment in renderable.Render(_options, _options.ConsoleSize.Width))
        {
            Profile.Out.Writer.Write(segment.Text);
        }
    }

    public void WriteAnsi(Action<AnsiWriter> action)
    {
        
    }
}

public sealed class SimpleOutput(TextWriter writer, Func<int> width, Func<int> height) : IAnsiConsoleOutput
{
    private readonly Func<int> _width = width ?? throw new ArgumentNullException(nameof(width));
    private readonly Func<int> _height = height ?? throw new ArgumentNullException(nameof(height));

    public TextWriter Writer => writer;
    public bool IsTerminal => false;
    public int Width => _width();
    public int Height => _height();

    public void SetEncoding(Encoding encoding)
    {
    }
}

internal sealed class NoopConsoleCursor : IAnsiConsoleCursor
{
    public void Show(bool show)
    {
    }

    public void SetPosition(int column, int line)
    {
    }

    public void Move(CursorDirection direction, int steps)
    {
    }
}

internal sealed class NoopConsoleInput : IAnsiConsoleInput
{
    public bool IsKeyAvailable()
    {
        return false;
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        return null;
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        return Task.FromResult<ConsoleKeyInfo?>(null);
    }
}

internal sealed class ExclusivityMode : IExclusivityMode
{
    public T Run<T>(Func<T> func)
    {
        return func();
    }

    public Task<T> RunAsync<T>(Func<Task<T>> func)
    {
        return func();
    }
}