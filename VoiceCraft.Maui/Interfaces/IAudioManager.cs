using NAudio.Wave;

namespace VoiceCraft.Maui.Interfaces;

/// <summary>
/// Interface for managing audio input/output devices.
/// </summary>
public interface IAudioManager
{
    /// <summary>
    /// Creates a recorder on the native device.
    /// </summary>
    /// <param name="audioFormat">The audio format for recording.</param>
    /// <param name="bufferMS">Buffer size in milliseconds.</param>
    /// <returns>A wave input device.</returns>
    IWaveIn CreateRecorder(WaveFormat audioFormat, int bufferMS);

    /// <summary>
    /// Creates a player on the native device.
    /// </summary>
    /// <param name="audioFormat">The sample provider for playback.</param>
    /// <returns>A wave player device.</returns>
    IWavePlayer CreatePlayer(ISampleProvider audioFormat);

    /// <summary>
    /// Gets a list of input devices.
    /// </summary>
    /// <returns>The list of device names.</returns>
    string[] GetInputDevices();

    /// <summary>
    /// Gets a list of output devices.
    /// </summary>
    /// <returns>The list of device names.</returns>
    string[] GetOutputDevices();

    /// <summary>
    /// Gets the number of available input audio devices.
    /// </summary>
    /// <returns>The number of available audio devices.</returns>
    int GetInputDeviceCount();

    /// <summary>
    /// Gets the number of available output audio devices.
    /// </summary>
    /// <returns>The number of available audio devices.</returns>
    int GetOutputDeviceCount();

    /// <summary>
    /// Requests permissions to record audio.
    /// </summary>
    /// <returns>True if permission was granted.</returns>
    Task<bool> RequestInputPermissions();
}

