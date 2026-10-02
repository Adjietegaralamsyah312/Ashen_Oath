using System.Runtime.InteropServices;

namespace AshenOath;

/// <summary>
/// Interop minimal ke native SDL2_mixer Termux (libSDL2_mixer.so).
/// ppy.SDL2-CS 1.0.82 hanya berisi binding SDL core.
/// Simbol + signature diverifikasi terhadap libSDL2_mixer.so (nm -D)
/// dan /usr/include/SDL2/SDL_mixer.h + SDL_audio.h. Hanya API yang dipakai.
/// </summary>
internal static class SdlMixer
{
    public const int INIT_OGG = 0x00000010;
    public const int FREQUENCY_44100 = 44100;
    public const ushort FORMAT_S16SYS = 0x8010;
    public const int CHANNELS_STEREO = 2;
    public const int CHUNK_SIZE = 2048;
    public const int MAX_VOLUME = 128;

    private const string NativeLib = "SDL2_mixer";

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_Init(int flags);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_Quit();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_OpenAudio(int frequency, ushort format, int channels, int chunksize);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_CloseAudio();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Mix_LoadWAV([MarshalAs(UnmanagedType.LPStr)] string file);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Mix_LoadMUS([MarshalAs(UnmanagedType.LPStr)] string file);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_PlayChannel(int channel, IntPtr chunk, int loops);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_HaltChannel(int channel);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_VolumeChunk(IntPtr chunk, int volume);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_FreeChunk(IntPtr chunk);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_PlayMusic(IntPtr music, int loops);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_HaltMusic();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_PlayingMusic();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_PausedMusic();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_PauseMusic();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_ResumeMusic();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_VolumeMusic(int volume);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Mix_FreeMusic(IntPtr music);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Mix_HasMusicDecoder([MarshalAs(UnmanagedType.LPStr)] string name);
}
