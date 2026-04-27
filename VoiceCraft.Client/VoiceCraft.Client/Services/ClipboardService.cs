using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;

namespace VoiceCraft.Client.Services;

public class ClipboardService
{
    private IClipboard? _clipboard;

    public void RegisterTopLevel(TopLevel topLevel)
    {
        _clipboard = topLevel.Clipboard;
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
