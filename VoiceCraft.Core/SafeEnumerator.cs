using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace VoiceCraft.Core
{
    public class SafeEnumerator<T>: IEnumerator<T>
    {
        // this is the (thread-unsafe)
        // enumerator of the underlying collection
        private readonly IEnumerator<T> _mInner;
        // this is the object we shall lock on. 
        private readonly object _mLock;

        public SafeEnumerator(IEnumerator<T> inner, object @lock)
        {
            _mInner = inner;
            _mLock = @lock;
            // entering lock in constructor
            Monitor.Enter(_mLock);
        }

        public void Dispose()
        {
            // .. and exiting lock on Dispose()
            // This will be called when foreach loop finishes
            Monitor.Exit(_mLock);
        }

        // we just delegate actual implementation
        // to the inner enumerator, that actually iterates
        // over some collection
    
        public bool MoveNext()
        {
            return _mInner.MoveNext();
        }

        public void Reset()
        {
            _mInner.Reset();
        }

        public T Current => _mInner.Current;

        object? IEnumerator.Current => Current;
    }
}