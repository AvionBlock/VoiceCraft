using Silk.NET.OpenAL;

namespace VoiceCraft.Client.Audio
{
    public unsafe class AudioSource : Core.Audio.AudioSource
    {
        public readonly Device* Device;

        private readonly ALContext _alContext;
        private readonly AL _al;
        private readonly Context* _context;

        public AudioSource(string device)
        {
            _alContext = ALContext.GetApi();
            _al = AL.GetApi();

            Device = _alContext.OpenDevice(device);
            if (Device == null)
            {
                throw new AudioDeviceException("Could not create device!");
            }

            _context = _alContext.CreateContext(Device, null);
            if (_context == null)
            {
                throw new AudioDeviceException($"Could not create device context! Error: {_al.GetError()}");
            }
            _alContext.MakeContextCurrent(_context);


        }
    }
}
