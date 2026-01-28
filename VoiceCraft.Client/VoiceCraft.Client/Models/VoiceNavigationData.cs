using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models;

public record VoiceNavigationData(VoiceCraftService VoiceCraftService);

public record VoiceStartNavigationData(string Ip, int Port);