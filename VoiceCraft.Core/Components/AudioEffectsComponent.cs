using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace VoiceCraft.Core.Components
{
    public struct AudioEffectsComponent : IComponent
    {
        public List<IAudioEffect> Effects;
    }
}