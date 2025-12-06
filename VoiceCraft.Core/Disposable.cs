/////////////////////////////////////////////////////////////////////////////////
// paint.net                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, and contributors.                  //
// All Rights Reserved.                                                        //
/////////////////////////////////////////////////////////////////////////////////

using System.Threading;

namespace VoiceCraft.Core;

/// <summary>
/// Provides a standard implementation of <see cref="IDisposable"/> with thread-safe dispose tracking.
/// </summary>
/// <remarks>
/// Derived classes should override <see cref="Dispose(bool)"/> to clean up resources.
/// The base implementation is thread-safe and tracks disposal state atomically.
/// </remarks>
/// <seealso href="https://gist.github.com/rickbrew/fc3e660c0930747f031e64ab7696c60d"/>
[Serializable]
public abstract class Disposable : IDisposable
{
    private int _isDisposed; // 0 for false, 1 for true

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    /// <value><c>true</c> if disposed; otherwise, <c>false</c>.</value>
    public bool IsDisposed => Volatile.Read(ref _isDisposed) != 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Disposable"/> class.
    /// </summary>
    protected Disposable()
    {
    }

    /// <summary>
    /// Finalizer - calls Dispose(false).
    /// </summary>
    ~Disposable()
    {
        int oldIsDisposed = Interlocked.Exchange(ref _isDisposed, 1);
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
        int oldIsDisposed = Interlocked.Exchange(ref _isDisposed, 1);
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
    /// <param name="disposing">
    /// <c>true</c> if called from <see cref="Dispose()"/>; 
    /// <c>false</c> if called from the finalizer.
    /// </param>
    /// <remarks>
    /// When <paramref name="disposing"/> is <c>true</c>, dispose managed resources.
    /// Always dispose unmanaged resources.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        // Base implementation does nothing.
        // Derived classes should override this to clean up resources.
    }
}
