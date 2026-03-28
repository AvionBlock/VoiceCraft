using System;
using SoundFlow.Abstracts;
using SoundFlow.Structs;

namespace VoiceCraft.Client.Audio;

public class CallbackProvider(AudioEngine engine, AudioFormat format, Func<Span<float>, int> callback)
    : SoundComponent(engine,
        format)
{
    protected override void GenerateAudio(Span<float> buffer, int channels)
    {
        callback(buffer);
    }
}