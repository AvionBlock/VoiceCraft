using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using VoiceCraft.Core.Events;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Core.Components
{
    public class AudioListenerComponent : IAudioInput, ISerializableEntityComponent
    {
        private string _environmentId = string.Empty;
        private ulong _bitmask; //Will change to a default value later.
        private bool _isDisposed;
        private bool IsAlive => !_isDisposed && Entity.IsAlive();

        public ComponentType ComponentType => ComponentType.AudioListener;
        public Entity Entity { get; }

        public event Action? OnDestroyed;

        public string EnvironmentId
        {
            get => _environmentId;
            set
            {
                if (_environmentId == value || !IsAlive) return;
                _environmentId = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public ulong Bitmask
        {
            get => _bitmask;
            set
            {
                if (_bitmask == value || !IsAlive) return;
                _bitmask = value;
                WorldEventHandler.InvokeComponentUpdated(new ComponentUpdatedEvent(this));
            }
        }

        public AudioListenerComponent(Entity entity)
        {
            if (entity.Has<AudioListenerComponent>())
                throw new InvalidOperationException($"Entity already has the {GetType().Name}!");
            Entity = entity;
            Entity.Add(this);
            WorldEventHandler.InvokeComponentAdded(new ComponentAddedEvent(this));
        }

        public byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(_environmentId.Length));
            if (_environmentId.Length > 0)
                data.AddRange(Encoding.UTF8.GetBytes(_environmentId));

            data.AddRange(BitConverter.GetBytes(_bitmask));

            return data.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            var offset = 0;

            //Extract EnvironmentId
            var environmentIdLength = BitConverter.ToInt32(data, offset);
            offset += sizeof(int);
            if (environmentIdLength > 0)
            {
                _environmentId = Encoding.UTF8.GetString(data);
                offset += environmentIdLength;
            }

            //Extract Bitmask
            _bitmask = BitConverter.ToUInt64(data, offset);
        }

        public void GetVisibleComponents(World world, List<object> components)
        {
            if (components.Contains(this) || !IsAlive)
                return; //Already part of the list. don't need to recheck through or if the component/entity is dead. Also prevents stack overflows (I think).
            components.Add(this);
            var query = new QueryDescription()
                .WithAll<AudioSourceComponent>(); //Only search for this component. May make it generic later but for now, AudioSource is needed.
            //We don't include NetworkComponent because this will be checked in the system for network transfer for further custom behavior.
            
            var localComponents = Entity.GetAllComponents(); //Get all local components to loop through.
            var localComponentTypes = localComponents.Select(x => x?.GetType()).ToArray();
            world.Query(in query, (ref Entity entity, ref AudioSourceComponent component) =>
            {
                var combinedBitmask = _bitmask | component.Bitmask; //Get the combined bitmask of the AudioSource and AudioListener for effect checking.
                var otherComponents = entity.GetAllComponents(); //Get all the components on the other entity.

                //Loop through local components first.
                foreach (var localComponent in localComponents)
                {
                    if (!(localComponent is IVisibilityComponent visibilityComponent)) continue;
                    visibilityComponent.VisibleTo(entity, combinedBitmask);
                }
                
                //Loop through other entity components.
                foreach (var otherComponent in otherComponents)
                {
                    //Do not check against local component types.
                    if (!(otherComponent is IVisibilityComponent visibilityComponent) || localComponentTypes.Contains(otherComponent.GetType())) continue;
                    visibilityComponent.VisibleTo(entity, combinedBitmask);
                }
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Entity.Remove<AudioListenerComponent>();
            _isDisposed = true;
            OnDestroyed?.Invoke();
            WorldEventHandler.InvokeComponentRemoved(new ComponentRemovedEvent(this));
        }
    }
}