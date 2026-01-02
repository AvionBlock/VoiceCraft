# Introduction

The bitmask system is a powerful feature in VoiceCraft, bitmasks can control which entity can talk to other entities,
which entities can listen to other entities and what effects are applied between each entity's interactions. The
bitmasks are 16bit unsigned integers allowing 16 individual channels/configurations.

# Different Bitmask Types

There are 4 different controllable bitmasks:

# [Entity.TalkBitmask](#tab/entityTalkBitmask)

This controls what bitmask the entity can talk on. This is compared with another entity's listen bitmask.
If both the talk and listen bitmasks do not intersect, then the listening entity cannot hear the talking entity.

# [Entity.ListenBitmask](#tab/entityListenBitmask)

This controls what bitmask the entity can listen on. This is compared with another entity's talk bitmask.
If both the talk and listen bitmasks do not intersect, then the listening entity cannot hear the talking entity.

# [Entity.EffectBitmask](#tab/entityEffectBitmask)

This controls what effects are enabled on each bitmask, this is compared against the intersections of the talk and
listen bitmask before being compared against each effect's bitmask.

# [Effect.Bitmask](#tab/effectBitmask)

This controls what bitmask the effect is enabled on. This is compared against all the bitmasks above to check whether it
is enabled or not. You can have up to 65535 different effect bitmasks, but you cannot have two effects with the same
bitmask value.

# [Technical Implementation](#tab/technicalImplementation)

`fromEntity.TalkBitmask` & `toEntity.ListenBitmask` & `fromEntity.EffectBitmask` & `toEntity.EffectBitmask` &
`effect.Bitmask`.

---

# Bitmask Visual

| From Entity.TalkBitmask | Comparison | To Entity.ListenBitmask | Value            |
|-------------------------|------------|-------------------------|------------------|
| 0000000000001011        | &          | 0000000100000001        | 0000000000000001 |
| 0110000000001010        | &          | 1001000010000101        | 0000000000000000 |