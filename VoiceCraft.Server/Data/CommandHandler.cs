namespace VoiceCraft.Server.Data;

/// <summary>
/// Handles registration and execution of console commands.
/// </summary>
public static class CommandHandler
{
    private static readonly Dictionary<string, Action<string[]>> _commands = new();

    /// <summary>
    /// Registers a command with the specified name and action.
    /// </summary>
    /// <param name="commandName">The command name (case-insensitive).</param>
    /// <param name="command">The action to execute when the command is invoked.</param>
    public static void RegisterCommand(string commandName, Action<string[]> command)
    {
        _commands.TryAdd(commandName.ToLowerInvariant(), command);
    }

    /// <summary>
    /// Parses and executes a command string.
    /// </summary>
    /// <param name="command">The command string to parse.</param>
    /// <exception cref="InvalidOperationException">Thrown when command is not found.</exception>
    public static void ParseCommand(string command)
    {
        string[] parts = command.Split(' ');
        string commandName = parts[0];
        string[] arguments = parts.Length > 1 
            ? parts[1..] 
            : [];

        if (_commands.TryGetValue(commandName, out var action))
        {
            action(arguments);
        }
        else
        {
            throw new InvalidOperationException("Invalid command. Type 'help' for a list of available commands.");
        }
    }
}

