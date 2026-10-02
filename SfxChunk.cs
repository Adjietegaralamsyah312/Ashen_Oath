namespace AshenOath;

/// <summary>Pemilik satu Mix_Chunk (SFX). Dispose tepat sekali.</summary>
public sealed class SfxChunk : IDisposable
{
    public string Name { get; }
    public IntPtr Handle { get; private set; }
    public bool IsValid => Handle != IntPtr.Zero;

    private readonly IAudioBackend _backend;
    private bool _disposed;

    internal SfxChunk(IAudioBackend backend, string name, IntPtr handle)
    {
        _backend = backend;
        Name = name;
        Handle = handle;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (Handle != IntPtr.Zero)
        {
            _backend.FreeChunk(Handle);
            Handle = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
