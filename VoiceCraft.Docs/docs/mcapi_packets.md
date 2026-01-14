# McApi Packet List

```
//Core
//Requests DO NOT CHANGE!
LoginRequest = 0,
LogoutRequest = 1,
PingRequest = 2,

//Responses DO NOT CHANGE!
AcceptResponse = 3,
DenyResponse = 4,
PingResponse = 5,

//Other/Changeable
//Requests
ResetRequest = 6,
SetEffectRequest = 7,
ClearEffectsRequest = 8,
CreateEntityRequest = 9,
DestroyEntityRequest = 10,
EntityAudioRequest = 11,
SetEntityTitleRequest = 12,
SetEntityDescriptionRequest = 13,
SetEntityWorldIdRequest = 14,
SetEntityNameRequest = 15,
SetEntityMuteRequest = 16,
SetEntityDeafenRequest = 17,
SetEntityTalkBitmaskRequest = 18,
SetEntityListenBitmaskRequest = 19,
SetEntityEffectBitmaskRequest = 20,
SetEntityPositionRequest = 21,
SetEntityRotationRequest = 22,
SetEntityCaveFactorRequest = 23,
SetEntityMuffleFactorRequest = 24,

//Responses
ResetResponse = 25,
CreateEntityResponse = 26,
DestroyEntityResponse = 27,

//Events
OnEffectUpdated = 28,
OnEntityCreated = 29,
OnNetworkEntityCreated = 30,
OnEntityDestroyed = 31,
OnEntityVisibilityUpdated = 32,
OnEntityWorldIdUpdated = 33,
OnEntityNameUpdated = 34,
OnEntityMuteUpdated = 35,
OnEntityDeafenUpdated = 36,
OnEntityServerMuteUpdated = 37,
OnEntityServerDeafenUpdated = 38,
OnEntityTalkBitmaskUpdated = 39,
OnEntityListenBitmaskUpdated = 40,
OnEntityEffectBitmaskUpdated = 41,
OnEntityPositionUpdated = 42,
OnEntityRotationUpdated = 43,
OnEntityCaveFactorUpdated = 44,
OnEntityMuffleFactorUpdated = 45,
OnEntityAudioReceived = 46
```