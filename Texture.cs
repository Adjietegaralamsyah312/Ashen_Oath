using SDL2;

namespace AshenOath;

/// <summary>
/// Pemilik satu SDL_Texture di GPU. Load via SDL2_image, surface selalu
/// dibebaskan setelah texture dibuat. Dispose menghancurkan texture.
/// Tidak valid (Handle == Zero) bila asset gagal dimuat — bukan error fatal.
/// </summary>
public sealed class Texture : IDisposable
{
    public IntPtr Handle { get; private set; } = IntPtr.Zero;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsValid => Handle != IntPtr.Zero;

    private bool _disposed;

    private Texture() { }

    public static Texture Load(IntPtr renderer, string path)
    {
        var texture = new Texture();
        IntPtr surface;
        try
        {
            surface = SdlImage.Load(path);
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine($"[Texture] SDL2_image native library not found: {ex.Message}");
            return texture;
        }
        catch (EntryPointNotFoundException ex)
        {
            Console.Error.WriteLine($"[Texture] IMG_Load entry point missing: {ex.Message}");
            return texture;
        }

        if (surface == IntPtr.Zero)
        {
            Console.Error.WriteLine($"[Texture] IMG_Load failed for '{path}': {SDL.SDL_GetError()}");
            return texture;
        }

        try
        {
            IntPtr handle = SDL.SDL_CreateTextureFromSurface(renderer, surface);
            if (handle == IntPtr.Zero)
            {
                Console.Error.WriteLine($"[Texture] SDL_CreateTextureFromSurface failed: {SDL.SDL_GetError()}");
                return texture;
            }

            texture.Handle = handle;
            if (SDL.SDL_QueryTexture(handle, out _, out _, out int w, out int h) == 0)
            {
                texture.Width = w;
                texture.Height = h;
            }
            Console.WriteLine($"[Texture] Loaded '{path}' ({texture.Width}x{texture.Height}).");
        }
        finally
        {
            SDL.SDL_FreeSurface(surface);
        }

        return texture;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (Handle != IntPtr.Zero)
        {
            SDL.SDL_DestroyTexture(Handle);
            Handle = IntPtr.Zero;
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
