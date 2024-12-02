using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Linux.Audio;

public class NativeAudioService : AudioService
{
    public override string GetDefaultInputDevice()
    {
        return "Default";
    }

    public override string GetDefaultOutputDevice()
    {
        return "Default";
    }

    public override List<string> GetInputDevices()
    {
        var list = new List<string>() { GetDefaultInputDevice() };
        
        Process process = new();
        process.StartInfo.FileName = "arecord";
        process.StartInfo.Arguments = $"-L";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;

        TaskCompletionSource<string> tcs = new();
        process.Exited += (sender, e) =>
        {
            var output = process.StandardOutput.ReadToEnd();
            tcs.SetResult(output);
            process.Dispose();
        };

        process.Start();
        tcs.Task.Wait(5000); //5 second timeout.
        var regex = new Regex(@"^hw:?(.*)\n?(.*)", RegexOptions.Multiline);
        var matches = regex.Matches(tcs.Task.Result);
        list.AddRange(matches.Select(m => m.Groups[2].Value.Replace("    ", "")));
        return list;
    }

    public override List<string> GetOutputDevices()
    {
        var list = new List<string>() { GetDefaultInputDevice() };
        
        Process process = new();
        process.StartInfo.FileName = "aplay";
        process.StartInfo.Arguments = $"-L";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;

        TaskCompletionSource<string> tcs = new();
        process.Exited += (sender, e) =>
        {
            var output = process.StandardOutput.ReadToEnd();
            tcs.SetResult(output);
            process.Dispose();
        };

        process.Start();
        tcs.Task.Wait(5000); //5 second timeout.
        var regex = new Regex(@"^hw:?(.*)\n?(.*)", RegexOptions.Multiline);
        var matches = regex.Matches(tcs.Task.Result);
        list.AddRange(matches.Select(m => m.Groups[2].Value.Replace("    ", "")));
        return list;
    }

    public override List<string> GetPreprocessors()
    {
        throw new System.NotImplementedException();
    }

    public override List<string> GetEchoCancelers()
    {
        throw new System.NotImplementedException();
    }

    public override IAudioRecorder CreateAudioRecorder()
    {
        throw new System.NotImplementedException();
    }

    public override IAudioPlayer CreateAudioPlayer()
    {
        throw new System.NotImplementedException();
    }
}