using System.Runtime.InteropServices;

namespace AshenOath;

/// <summary>
/// Interop minimal ke native SDL2_image Termux (libSDL2_image.so).
/// ppy.SDL2-CS 1.0.82 hanya berisi binding SDL core, jadi 3 fungsi ini
/// dideklarasikan sendiri. Bukan engine baru, bukan package NuGet.
/// Nama simbol diverifikasi terhadap libSDL2_image.so (IMG_Init/IMG_Load/IMG_Quit).
/// </summary>
internal static class SdlImage
{
    public const int INIT_JPG = 0x00000001;
    public const int INIT_PNG = 0x00000002;
    public const int INIT_TIF = 0x00000004;

    private const string NativeLib = "SDL2_image";

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern int IMG_Init(int flags);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr IMG_Load([MarshalAs(UnmanagedType.LPStr)] string file);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void IMG_Quit();

    public static bool InitPng()
    {
        try
        {
            int initialized = IMG_Init(INIT_PNG);
            if ((initialized & INIT_PNG) == 0)
            {
                Console.Error.WriteLine("[SdlImage] IMG_Init PNG support not available.");
                return false;
            }
            return true;
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine($"[SdlImage] native SDL2_image not found: {ex.Message}");
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            Console.Error.WriteLine($"[SdlImage] IMG_Init entry point missing: {ex.Message}");
            return false;
        }
    }

    public static IntPtr Load(string path) => IMG_Load(path);

    public static void Quit()
    {
        try { IMG_Quit(); }
        catch { /* aman diabaikan saat cleanup */ }
    }
}
