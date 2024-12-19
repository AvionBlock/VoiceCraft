using System;

namespace VoiceCraft.Core.Interfaces
{
    public interface IUpdateable<T> where T : class
    {
        public event Action<T> OnUpdate;
    }
}