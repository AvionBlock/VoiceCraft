using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Browser.Audio;

public class NativeAudioService(
    PermissionsService permissionsService,
    IEnumerable<RegisteredAutomaticGainController> registeredAutomaticGainControllers,
    IEnumerable<RegisteredEchoCanceler> registeredEchoCancelers,
    IEnumerable<RegisteredDenoiser> registeredDenoisers,
    IEnumerable<RegisteredAudioClipper> registeredClippers)
    : AudioService(
        registeredAutomaticGainControllers,
        registeredEchoCancelers,
        registeredDenoisers,
        registeredClippers)
{
    public override IAudioRecorder CreateAudioRecorder(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioRecorder(sampleRate, channels, format);
    }

    public override IAudioPlayer CreateAudioPlayer(int sampleRate, int channels, AudioFormat format)
    {
        return new AudioPlayer(sampleRate, channels, format);
    }

    public override async Task<List<string>> GetInputDevicesAsync()
    {
        await permissionsService
            .CheckAndRequestPermission<Microsoft.Maui.ApplicationModel.Permissions.Microphone>(); //I HATE WEB
        var list = new List<string>();
        var devices = JsonSerializer.Deserialize(await JsNativeAudio.GetInputDevicesAsync(),
            MediaDevicesSerializationContext.Default.ListJsMediaDeviceInfo);
        if (devices == null) return list;
        list.AddRange(devices.Select(device => device.label));
        return list;
    }

    public override async Task<List<string>> GetOutputDevicesAsync()
    {
        await permissionsService
            .CheckAndRequestPermission<Microsoft.Maui.ApplicationModel.Permissions.Microphone>(); //I HATE WEB
        var list = new List<string>();
        var devices = JsonSerializer.Deserialize(await JsNativeAudio.GetOutputDevicesAsync(),
            MediaDevicesSerializationContext.Default.ListJsMediaDeviceInfo);
        if (devices == null) return list;
        list.AddRange(devices.Select(device => device.label));
        return list;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<JsMediaDeviceInfo>), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class MediaDevicesSerializationContext : JsonSerializerContext;

public class JsMediaDeviceInfo
{
    // ReSharper disable InconsistentNaming
    public string deviceId { get; set; } = string.Empty;
    public string groupId { get; set; } = string.Empty;
    public string kind { get; set; } = string.Empty;
    public string label { get; set; } = string.Empty;
}

internal static partial class JsNativeAudio
{
    [JSImport("getInputDevicesAsync", "audio.js")]
    public static partial Task<string> GetInputDevicesAsync();

    [JSImport("getOutputDevicesAsync", "audio.js")]
    public static partial Task<string> GetOutputDevicesAsync();
}