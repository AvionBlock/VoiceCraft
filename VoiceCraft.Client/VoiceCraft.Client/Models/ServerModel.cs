using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.Models
{
    public partial class ServerModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
        [ObservableProperty]
        private string _ip = string.Empty;
        [ObservableProperty]
        private ushort _port = 9050;
        [ObservableProperty]
        private ushort _key = 0;

        public ServerModel(string name, string ip, ushort port, ushort key)
        {
            _name = name;
            _ip = ip;
            _port = port;
            _key = key;
        }
    }
}
