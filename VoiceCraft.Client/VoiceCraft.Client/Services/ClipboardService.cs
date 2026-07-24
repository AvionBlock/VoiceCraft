using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace VoiceCraft.Client.Services;

public class ClipboardService
{
    private static IClipboard? _clipboard;

    public static void SetClipboardManager(IClipboard clipboard)
    {
        _clipboard = clipboard;
    }

    public async Task SetTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Clipboard text cannot be empty.", nameof(text));

        if (_clipboard is null)
            throw new InvalidOperationException("Clipboard is not available yet.");

        await _clipboard.SetTextAsync(text);
    }
}
