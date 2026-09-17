using System;
using System.IO;
using System.Text;

namespace VoiceCraft.Client.Controls;

public class EventBufferedTextWriter : TextWriter
{
    private const int MaxCapacity = 5000;
    private readonly StringBuilder _buffer = new(MaxCapacity);

    public Action<string>? OnWrite;

    public void Clear()
    {
        _buffer.Clear();
    }

    public override void Write(char value)
    {
        if (_buffer.Length >= MaxCapacity)
        {
            _buffer.Remove(0, 1);
        }
        
        _buffer.Append(value);
        OnWrite?.Invoke(_buffer.ToString());
    }

    public override void Write(string? value)
    {
        if (value != null && _buffer.Length >= MaxCapacity)
        {
            _buffer.Remove(0, value.Length);
        }
        
        _buffer.Append(value);
        OnWrite?.Invoke(_buffer.ToString());
    }

    public override void WriteLine(string? value)
    {
        if (value != null && _buffer.Length >= MaxCapacity)
        {
            _buffer.Remove(0, value.Length + 1);
        }
        
        _buffer.AppendLine(value);
        OnWrite?.Invoke(_buffer.ToString());
    }
    
    public override Encoding Encoding => Encoding.UTF8;

    public override string ToString()
    {
        return _buffer.ToString();
    }
}