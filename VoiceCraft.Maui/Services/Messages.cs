using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceCraft.Core;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.VoiceCraft;

namespace VoiceCraft.Maui.Services;

/// <summary>Message for status updates.</summary>
public class StatusUpdatedMSG(string value) : ValueChangedMessage<string>(value);

/// <summary>Message when client starts speaking.</summary>
public class StartedSpeakingMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when client stops speaking.</summary>
public class StoppedSpeakingMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when client is muted.</summary>
public class MutedMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when client is unmuted.</summary>
public class UnmutedMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when client is deafened.</summary>
public class DeafenedMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when client is undeafened.</summary>
public class UndeafenedMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Message when a participant joins.</summary>
public class ParticipantJoinedMSG(VoiceCraftParticipant value) : ValueChangedMessage<VoiceCraftParticipant>(value);

/// <summary>Message when a participant leaves.</summary>
public class ParticipantLeftMSG(VoiceCraftParticipant value) : ValueChangedMessage<VoiceCraftParticipant>(value);

/// <summary>Message when a participant is updated.</summary>
public class ParticipantUpdatedMSG(VoiceCraftParticipant value) : ValueChangedMessage<VoiceCraftParticipant>(value);

/// <summary>Message when a participant starts speaking.</summary>
public class ParticipantStartedSpeakingMSG(VoiceCraftParticipant value) : ValueChangedMessage<VoiceCraftParticipant>(value);

/// <summary>Message when a participant stops speaking.</summary>
public class ParticipantStoppedSpeakingMSG(VoiceCraftParticipant value) : ValueChangedMessage<VoiceCraftParticipant>(value);

/// <summary>Message when a channel is added.</summary>
public class ChannelAddedMSG(Channel value) : ValueChangedMessage<Channel>(value);

/// <summary>Message when a channel is removed.</summary>
public class ChannelRemovedMSG(Channel value) : ValueChangedMessage<Channel>(value);

/// <summary>Message when joining a channel.</summary>
public class ChannelJoinedMSG(Channel value) : ValueChangedMessage<Channel>(value);

/// <summary>Message when leaving a channel.</summary>
public class ChannelLeftMSG(Channel value) : ValueChangedMessage<Channel>(value);

/// <summary>Message when disconnected.</summary>
public class DisconnectedMSG(string value) : ValueChangedMessage<string>(value);

/// <summary>Message when connection is denied.</summary>
public class DenyMSG(string value) : ValueChangedMessage<string>(value);

/// <summary>Request data message.</summary>
public class RequestDataMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Response data message.</summary>
public class ResponseDataMSG(ResponseData value) : ValueChangedMessage<ResponseData>(value);

/// <summary>Mute request message.</summary>
public class MuteMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Unmute request message.</summary>
public class UnmuteMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Deafen request message.</summary>
public class DeafenMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Undeafen request message.</summary>
public class UndeafenMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Join channel request message.</summary>
public class JoinChannelMSG(JoinChannel value) : ValueChangedMessage<JoinChannel>(value);

/// <summary>Leave channel request message.</summary>
public class LeaveChannelMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Disconnect request message.</summary>
public class DisconnectMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Start service message.</summary>
public class StartServiceMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>Stop service message.</summary>
public class StopServiceMSG(string? value = null) : ValueChangedMessage<string?>(value);

/// <summary>
/// Response data containing current voice state.
/// </summary>
public class ResponseData(
    List<ParticipantModel> participants,
    List<ChannelModel> channels,
    bool isSpeaking,
    bool isMuted,
    bool isDeafened,
    string statusMessage)
{
    /// <summary>Gets the list of participants.</summary>
    public List<ParticipantModel> Participants { get; set; } = participants;

    /// <summary>Gets the list of channels.</summary>
    public List<ChannelModel> Channels { get; set; } = channels;

    /// <summary>Gets whether the client is speaking.</summary>
    public bool IsSpeaking { get; set; } = isSpeaking;

    /// <summary>Gets whether the client is muted.</summary>
    public bool IsMuted { get; set; } = isMuted;

    /// <summary>Gets whether the client is deafened.</summary>
    public bool IsDeafened { get; set; } = isDeafened;

    /// <summary>Gets the status message.</summary>
    public string StatusMessage { get; set; } = statusMessage;
}

/// <summary>
/// Data for joining a channel.
/// </summary>
public class JoinChannel(Channel channel)
{
    /// <summary>Gets or sets the channel to join.</summary>
    public Channel Channel { get; set; } = channel;

    /// <summary>Gets or sets the channel password.</summary>
    public string Password { get; set; } = string.Empty;
}
