using Android.Media.Audiofx;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    //Should be disposable.
    public class NativeAEC
    {
        private AcousticEchoCanceler? _echoCanceler;
        private AudioRecorder? _attachedRecorder;

        public void Attach(IAudioRecorder audioRecorder, IAudioPlayer audioPlayer)
        {
            if (audioRecorder is AudioRecorder recorder)
            {
                _attachedRecorder = recorder;
                _attachedRecorder.RecordingStarted += AttachedRecorderStarted;
                _attachedRecorder.RecordingStopped += AttachedRecorderStopped;
                OpenEchoCanceler();
            }
        }

        protected void CloseEchoCanceler()
        {
            if (_echoCanceler != null)
            {
                _echoCanceler.Dispose();
                _echoCanceler.Release();
                _echoCanceler = null;
            }
        }

        protected void OpenEchoCanceler()
        {
            CloseEchoCanceler();

            if (_attachedRecorder == null) return;
            if (_attachedRecorder.SessionId != null)
            {
                _echoCanceler = AcousticEchoCanceler.Create((int)_attachedRecorder.SessionId);
                _echoCanceler?.SetEnabled(true); //Force enable the AEC.
            }
        }

        private void AttachedRecorderStarted(object? sender, System.EventArgs e)
        {
            OpenEchoCanceler();
        }

        private void AttachedRecorderStopped(object? sender, NAudio.Wave.StoppedEventArgs e)
        {
            CloseEchoCanceler();
        }
    }
}
