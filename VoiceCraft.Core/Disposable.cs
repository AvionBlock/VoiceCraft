/////////////////////////////////////////////////////////////////////////////////
// paint.net                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, and contributors.                  //
// All Rights Reserved.                                                        //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace VoiceCraft.Core
{
    /// <summary>
    /// Provides a standard implementation of IDisposable and IIsDisposed.
    /// </summary>
    [Serializable]
    public abstract class Disposable : IDisposable
    {
        private int isDisposed; // 0 for false, 1 for true

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get => Volatile.Read(ref this.isDisposed) != 0;
        }

        protected Disposable()
        {
        }

        ~Disposable()
        {
            int oldIsDisposed = Interlocked.Exchange(ref this.isDisposed, 1);
            if (oldIsDisposed == 0)
            {
                Dispose(false);
            }
        }

        /// <summary>
        /// Disposes the object, releasing all managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            int oldIsDisposed = Interlocked.Exchange(ref this.isDisposed, 1);
            if (oldIsDisposed == 0)
            {
                try
                {
                    Dispose(true);
                }
                finally
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        /// <summary>
        /// Override this method to release managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Base implementation does nothing.
            // Derived classes should override this to clean up resources.
        }
    }
}

// https://gist.github.com/rickbrew/fc3e660c0930747f031e64ab7696c60d