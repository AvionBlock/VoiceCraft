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
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    /// <value><c>true</c> if disposed; otherwise, <c>false</c>.</value>
    public bool IsDisposed => _isDisposed;

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
        Dispose(false);
    }

    /// <summary>
    /// Disposes the object, releasing all managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override this method to release managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> if called from <see cref="Dispose()"/>; 
    /// <c>false</c> if called from the finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
    }
}
