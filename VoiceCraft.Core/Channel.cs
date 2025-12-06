namespace VoiceCraft.Core
{
    public class Channel
    {
        private bool _locked;

        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Locked { get => _locked || Hidden; set => _locked = value; }
        public bool Hidden { get; set; } = false;
        public ChannelOverride? OverrideSettings { get; set; }
    }

    public class ChannelOverride
    {
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = true;
        public bool VoiceEffects { get; set; } = true;
    }
}
