namespace VoiceCraft.Core.ECS
{
    public abstract class Component
    {
        public readonly Entity Entity;

        protected Component(Entity entity)
        {
            Entity = entity;
        }
    }
}