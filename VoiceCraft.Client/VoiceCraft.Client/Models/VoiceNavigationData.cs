using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models;

public record VoiceNavigationData(VoiceCraftClientService VoiceCraftClientService);

public record VoiceStartNavigationData(string Ip, int Port);