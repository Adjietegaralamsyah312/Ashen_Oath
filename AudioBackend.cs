using SDL2;

namespace AshenOath;

/// <summary>
/// Backend audio tipis agar lifecycle/state dapat diuji headless via stub.
/// Implementasi nyata mendelegasikan ke <see cref="SdlMixer"/>.
/// </summary>
public interface IAudioBackend
{
    int Init(int flags);
    void Quit();
    int OpenAudio(int frequency, ushort format, int channels, int chunkSize);
    void CloseAudio();
    IntPtr LoadChunk(string path);
    IntPtr LoadMusic(string path);
    int PlayChunk(IntPtr chunk, int loops);
    int HaltChannel(int channel);
    int ChunkVolume(IntPtr chunk, int volume);
    void FreeChunk(IntPtr handle);
    int PlayMusic(IntPtr music, int loops);
    int HaltMusic();
    int PlayingMusic();
    int PausedMusic();
    void PauseMusic();
    void ResumeMusic();
    int MusicVolume(int volume);
    void FreeMusic(IntPtr handle);
    int HasDecoder(string name);
    string LastError();
}

/// <summary>Backend nyata: P/Invoke SDL2_mixer Termux.</summary>
public sealed class SdlMixerBackend : IAudioBackend
{
    public int Init(int flags) => SdlMixer.Mix_Init(flags);
    public void Quit() => SdlMixer.Mix_Quit();
    public int OpenAudio(int frequency, ushort format, int channels, int chunkSize)
        => SdlMixer.Mix_OpenAudio(frequency, format, channels, chunkSize);
    public void CloseAudio() => SdlMixer.Mix_CloseAudio();
    public IntPtr LoadChunk(string path) => SdlMixer.Mix_LoadWAV(path);
    public IntPtr LoadMusic(string path) => SdlMixer.Mix_LoadMUS(path);
    public int PlayChunk(IntPtr chunk, int loops) => SdlMixer.Mix_PlayChannel(-1, chunk, loops);
    public int HaltChannel(int channel) => SdlMixer.Mix_HaltChannel(channel);
    public int ChunkVolume(IntPtr chunk, int volume) => SdlMixer.Mix_VolumeChunk(chunk, volume);
    public void FreeChunk(IntPtr handle) => SdlMixer.Mix_FreeChunk(handle);
    public int PlayMusic(IntPtr music, int loops) => SdlMixer.Mix_PlayMusic(music, loops);
    public int HaltMusic() => SdlMixer.Mix_HaltMusic();
    public int PlayingMusic() => SdlMixer.Mix_PlayingMusic();
    public int PausedMusic() => SdlMixer.Mix_PausedMusic();
    public void PauseMusic() => SdlMixer.Mix_PauseMusic();
    public void ResumeMusic() => SdlMixer.Mix_ResumeMusic();
    public int MusicVolume(int volume) => SdlMixer.Mix_VolumeMusic(volume);
    public void FreeMusic(IntPtr handle) => SdlMixer.Mix_FreeMusic(handle);
    public int HasDecoder(string name) => SdlMixer.Mix_HasMusicDecoder(name);
    public string LastError() => SDL.SDL_GetError();
}
