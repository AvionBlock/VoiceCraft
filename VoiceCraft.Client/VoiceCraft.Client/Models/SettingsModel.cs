using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.Models
{
    public partial class SettingsModel : ObservableObject
    {
        public const int NameLimit = 12;
        public const int IPLimit = 30;

        //public event EventHandler<ServerModel>? OnServerAdded;
        //public event EventHandler<ServerModel>? OnServerRemoved;


        //[ObservableProperty]
        //private ObservableCollection<ServerModel> _servers = new ObservableCollection<ServerModel>();

        ////Voice Settings
        //[ObservableProperty]
        //private int _inputDevice = -1;
        //[ObservableProperty]
        //private int _outputDevice = -1;
        //[ObservableProperty]
        //private float _softLimiterGain = 5.0f;
        //[ObservableProperty]
        //private float _microphoneSensitivity = 0.04f;
        //[ObservableProperty]
        //private bool _directionalHearing = false;
        //[ObservableProperty]
        //private bool _linearProximity = true;

        ////Behavior Settings
        //[ObservableProperty]
        //private string _selectedTheme = "Default";
        //[ObservableProperty]
        //private ushort _clientSidedPort = 8080;
        //[ObservableProperty]
        //private ushort _jitterBufferSize = 80;
        //[ObservableProperty]
        //private bool _hideServerAddresses = false;
        //[ObservableProperty]
        //private bool _clientSidedPositioning = false;
        //[ObservableProperty]
        //private bool _customClientProtocol = false;

        //public void AddServer(ServerModel server)
        //{
        //    if (string.IsNullOrWhiteSpace(server.Name))
        //        throw new Exception("Server name cannot be empty or whitespace!");
        //    if (string.IsNullOrWhiteSpace(server.Ip))
        //        throw new Exception("Server IP cannot be empty or whitespace!");
        //    if (server.Name.Length > NameLimit)
        //        throw new Exception($"Server name cannot be longer than {NameLimit} characters!");
        //    if (server.Ip.Length > IPLimit)
        //        throw new Exception($"Server name cannot be longer than {IPLimit} characters!");

        //    Servers.Insert(0, server);
        //    OnServerAdded?.Invoke(this, server);
        //}

        //public void RemoveServer(ServerModel server)
        //{
        //    Servers.Remove(server);
        //    OnServerRemoved?.Invoke(this, server);
        //}
    }
}
