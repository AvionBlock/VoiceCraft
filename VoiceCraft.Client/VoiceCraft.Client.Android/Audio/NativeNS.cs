using Android.Media.Audiofx;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeNS
    {
        private NoiseSuppressor? _noiseSupressor;
        private AudioRecorder? _attachedRecorder;

        public void Attach(IAudioRecorder audioRecorder)
        {
            if (audioRecorder is AudioRecorder recorder)
            {
                _attachedRecorder = recorder;
                _attachedRecorder.RecordingStarted += AttachedRecorderStarted;
                _attachedRecorder.RecordingStopped += AttachedRecorderStopped;
                OpenNoiseSupressor();
            }
        }

        protected void OpenNoiseSupressor()
        {
            CloseNoiseSupressor();

            if (_attachedRecorder == null) return;
            if (_attachedRecorder.SessionId != null)
            {
                _noiseSupressor = NoiseSuppressor.Create((int)_attachedRecorder.SessionId);
                _noiseSupressor?.SetEnabled(true); //Force enable the NS.
            }
        }

        protected void CloseNoiseSupressor()
        {
            if (_noiseSupressor != null)
            {
                _noiseSupressor.Dispose();
                _noiseSupressor.Release();
                _noiseSupressor = null;
            }
        }

        private void AttachedRecorderStarted(object? sender, System.EventArgs e)
        {
            OpenNoiseSupressor();
        }

        private void AttachedRecorderStopped(object? sender, NAudio.Wave.StoppedEventArgs e)
        {
            CloseNoiseSupressor();
        }
    }
}
