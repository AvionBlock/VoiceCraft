using Android.Media.Audiofx;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Android.Audio
{
    public class NativeAGC
    {
        private AutomaticGainControl? _gainController;
        private AudioRecorder? _attachedRecorder;

        public void Attach(IAudioRecorder audioRecorder)
        {
            if (audioRecorder is AudioRecorder recorder)
            {
                _attachedRecorder = recorder;
                _attachedRecorder.RecordingStarted += AttachedRecorderStarted;
                _attachedRecorder.RecordingStopped += AttachedRecorderStopped;
                OpenAutomaticGainControl();
            }
        }

        protected void OpenAutomaticGainControl()
        {
            CloseAutomaticGainControl();

            if (_attachedRecorder == null) return;
            if (_attachedRecorder.SessionId != null)
            {
                _gainController = AutomaticGainControl.Create((int)_attachedRecorder.SessionId);
                _gainController?.SetEnabled(true); //Force enable the AGC.
            }
        }

        protected void CloseAutomaticGainControl()
        {
            if (_gainController != null)
            {
                _gainController.Dispose();
                _gainController.Release();
                _gainController = null;
            }
        }

        private void AttachedRecorderStarted(object? sender, System.EventArgs e)
        {
            OpenAutomaticGainControl();
        }

        private void AttachedRecorderStopped(object? sender, NAudio.Wave.StoppedEventArgs e)
        {
            CloseAutomaticGainControl();
        }
    }
}
