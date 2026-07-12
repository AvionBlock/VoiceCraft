namespace VoiceCraft.Network;

public enum VcConnectionState : byte
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}

public enum McApiConnectionState : byte
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}

public enum VcDeliveryMethod : byte
{
    Unreliable,
    Reliable
}

public enum PositioningType : byte
{
    Server,
    Client
}

public enum EffectType : byte
{
    None,
    Visibility,
    Proximity,
    Directional,
    ProximityEcho,
    Echo,
    ProximityMuffle,
    Muffle
}

public enum PropertyType : byte
{
    Null,
    Boolean,
    SByte,
    Byte,
    Short,
    UShort,
    Int,
    UInt,
    Long,
    ULong,
    Float,
    Double
}

public enum EventType : byte
{
    //Events
    None,
    OnEffectUpdated,
    OnEntityCreated,
    OnNetworkEntityCreated,
    OnEntityDestroyed,
    OnEntityVisibilityUpdated,
    OnEntityWorldIdUpdated,
    OnEntityNameUpdated,
    OnEntityMuteUpdated,
    OnEntityDeafenUpdated,
    OnEntityServerMuteUpdated,
    OnEntityServerDeafenUpdated,
    OnEntityTalkBitmaskUpdated,
    OnEntityListenBitmaskUpdated,
    OnEntityEffectBitmaskUpdated,
    OnEntityPositionUpdated,
    OnEntityRotationUpdated,
    OnEntityPropertyUpdated,
    OnEntityAudioReceived,
    OnEntityAudioDataReceived,
}

public enum VcPacketType : byte
{
    //Core
    //Requests DO NOT CHANGE!
    InfoRequest,
    LoginRequest,
    LogoutRequest,

    //Responses DO NOT CHANGE!
    InfoResponse,
    AcceptResponse,
    DenyResponse,

    //Other/Changeable
    //Requests
    EventRequest,
    SetNameRequest,
    AudioRequest,
    SetMuteRequest,
    SetDeafenRequest,
    SetServerMuteRequest,
    SetServerDeafenRequest,
    SetWorldIdRequest,
    SetTalkBitmaskRequest,
    SetListenBitmaskRequest,
    SetEffectBitmaskRequest,
    SetPositionRequest,
    SetRotationRequest,
    SetPropertyRequest,
    SetTitleRequest,
    SetDescriptionRequest,
    SetEntityVisibilityRequest,

    //Responses
}

public enum McApiPacketType : byte
{
    //Core
    //Requests DO NOT CHANGE!
    LoginRequest,
    LogoutRequest,
    PingRequest,

    //Responses DO NOT CHANGE!
    AcceptResponse,
    DenyResponse,
    PingResponse,

    //Other/Changeable
    //Requests
    EventRequest,
    ResetRequest,
    SetEffectRequest,
    ClearEffectsRequest,
    CreateEntityRequest,
    DestroyEntityRequest,
    EntityAudioRequest,
    SetEntityTitleRequest,
    SetEntityDescriptionRequest,
    SetEntityWorldIdRequest,
    SetEntityNameRequest,
    SetEntityMuteRequest,
    SetEntityDeafenRequest,
    SetEntityTalkBitmaskRequest,
    SetEntityListenBitmaskRequest,
    SetEntityEffectBitmaskRequest,
    SetEntityPositionRequest,
    SetEntityRotationRequest,
    SetEntityPropertyRequest,

    //Responses
    ResetResponse,
    CreateEntityResponse,
    DestroyEntityResponse,
}
