namespace AshenOath;

/// <summary>
/// Pemilik subsystem audio: init sekali, load SFX/musik, play, volume, cleanup sekali.
/// Audio bersifat OPSIONAL: device gagal / file hilang hanya log, gameplay lanjut.
/// </summary>
public sealed class AudioManager : IDisposable
{
    public static readonly string[] SfxNames =
        { "jump", "attack", "hit", "hurt", "death", "checkpoint", "goal", "dash", "shield_bash", "projectile", "skeleton_hurt", "bat_hurt", "spike_hit", "hazard_hit", "platform_move", "platform_fall" };
    public static readonly string[] ItemSfxNames =
        { "pickup", "equip", "potion" };
    public static readonly string[] NpcSfxNames =
        { "npc_talk", "quest_accept", "quest_complete", "shop_buy" };
    public static readonly string[] ProgressionSfxNames =
        { "level_up" };
    public static readonly string[] BossSfxNames =
        { "boss_intro", "boss_melee", "boss_projectile", "boss_phase2", "boss_death", "boss_hit" };
    public const string StageMusicName = "road_of_ash";
    public const string BossMusicName = "boss_theme";
    public const string Stage2MusicName = "hollow_forge";

    public bool Initialized { get; private set; }
    public bool AudioEnabled { get; private set; }
    public int MusicVolume { get; private set; } = 80;
    public int SfxVolume { get; private set; } = 80;
    public string? CurrentMusic { get; private set; }
    public int LoadedSfxCount => _sfx.Count;
    public int LoadedMusicCount => _music.Count;

    private readonly IAudioBackend _backend;
    private readonly Dictionary<string, SfxChunk> _sfx = new();
    private readonly Dictionary<string, MusicTrack> _music = new();
    private readonly Dictionary<string, int> _sfxPlayCounts = new();
    private bool _disposed;

    public AudioManager(IAudioBackend? backend = null)
    {
        _backend = backend ?? new SdlMixerBackend();
    }

    /// <summary>Init sekali; false bila device tak tersedia (gameplay tetap jalan).</summary>
    public bool Initialize()
    {
        if (Initialized)
            return AudioEnabled;
        Initialized = true;

        try
        {
            int inited = _backend.Init(SdlMixer.INIT_OGG);
            if ((inited & SdlMixer.INIT_OGG) == 0)
                Console.Error.WriteLine("[Audio] warning: OGG decoder unavailable, music falls back to WAV.");
            if (_backend.OpenAudio(SdlMixer.FREQUENCY_44100, SdlMixer.FORMAT_S16SYS,
                    SdlMixer.CHANNELS_STEREO, SdlMixer.CHUNK_SIZE) != 0)
            {
                Console.Error.WriteLine($"[Audio] warning: audio device unavailable ({_backend.LastError()}). Continuing silent.");
                return false;
            }
            AudioEnabled = true;
            ApplyMusicVolume();
            Console.WriteLine("[Audio] initialized (44100Hz stereo).");
            return true;
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine($"[Audio] warning: SDL2_mixer native library not found ({ex.Message}). Continuing silent.");
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            Console.Error.WriteLine($"[Audio] warning: mixer entry point missing ({ex.Message}). Continuing silent.");
            return false;
        }
    }

    /// <summary>Load 7 SFX + musik stage; file hilang hanya log.</summary>
    public void LoadStageAudio()
    {
        foreach (string name in SfxNames)
            LoadSfx(name, ResolveAsset($"Assets/Audio/SFX/{name}.wav"));
        string ogg = ResolveAsset($"Assets/Audio/Music/{StageMusicName}.ogg") ?? string.Empty;
        string wav = ResolveAsset($"Assets/Audio/Music/{StageMusicName}.wav") ?? string.Empty;
        if (ogg.Length > 0)
            LoadMusic(StageMusicName, ogg);
        else if (wav.Length > 0)
            LoadMusic(StageMusicName, wav);
        else
            Console.Error.WriteLine($"[Audio] asset not found: Assets/Audio/Music/{StageMusicName}.ogg|wav");
    }

    /// <summary>
    /// Load audio boss opsional (Tahap 14): event-based, missing asset -&gt; silent fallback.
    /// </summary>
    public void LoadBossAudio()
    {
        foreach (string name in BossSfxNames)
            LoadSfx(name, ResolveAsset($"Assets/Audio/SFX/{name}.wav"));
        string ogg = ResolveAsset($"Assets/Audio/Music/{BossMusicName}.ogg") ?? string.Empty;
        string wav = ResolveAsset($"Assets/Audio/Music/{BossMusicName}.wav") ?? string.Empty;
        if (ogg.Length > 0)
            LoadMusic(BossMusicName, ogg);
        else if (wav.Length > 0)
            LoadMusic(BossMusicName, wav);
        else
            Console.Error.WriteLine($"[Audio] asset not found: Assets/Audio/Music/{BossMusicName}.ogg|wav (silent fallback)");
    }

    /// <summary>
    /// Load SFX item opsional (Tahap 17): missing asset -&gt; silent fallback.
    /// </summary>
    public void LoadItemAudio()
    {
        foreach (string name in ItemSfxNames)
            LoadSfx(name, ResolveAsset($"Assets/Audio/SFX/{name}.wav"));
    }

    /// <summary>
    /// Load SFX NPC/quest/shop opsional (Tahap 18): missing asset -&gt; silent fallback.
    /// </summary>
    public void LoadNpcAudio()
    {
        foreach (string name in NpcSfxNames)
            LoadSfx(name, ResolveAsset($"Assets/Audio/SFX/{name}.wav"));
    }

    /// <summary>
    /// Load SFX progression opsional (Tahap 19): missing asset -&gt; silent fallback.
    /// </summary>
    public void LoadProgressionAudio()
    {
        foreach (string name in ProgressionSfxNames)
            LoadSfx(name, ResolveAsset($"Assets/Audio/SFX/{name}.wav"));
    }

    /// <summary>
    /// Load musik Stage 2 opsional (Tahap 16): missing asset -&gt; silent fallback.
    /// </summary>
    public void LoadStage2Audio()
    {
        string ogg = ResolveAsset($"Assets/Audio/Music/{Stage2MusicName}.ogg") ?? string.Empty;
        string wav = ResolveAsset($"Assets/Audio/Music/{Stage2MusicName}.wav") ?? string.Empty;
        if (ogg.Length > 0)
            LoadMusic(Stage2MusicName, ogg);
        else if (wav.Length > 0)
            LoadMusic(Stage2MusicName, wav);
        else
            Console.Error.WriteLine($"[Audio] asset not found: Assets/Audio/Music/{Stage2MusicName}.ogg|wav (silent fallback)");
    }

    public bool HasMusic(string name) => _music.ContainsKey(name);

    public bool LoadSfx(string name, string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Console.Error.WriteLine($"[Audio] asset not found: {name}");
            return false;
        }
        if (!EnsureEnabled())
            return false;
        IntPtr handle;
        try { handle = _backend.LoadChunk(path); }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"[Audio] playback failed: {ex.Message}");
            return false;
        }
        if (handle == IntPtr.Zero)
        {
            Console.Error.WriteLine($"[Audio] load failed '{name}': {_backend.LastError()}");
            return false;
        }
        if (_sfx.TryGetValue(name, out var existing))
            existing.Dispose();
        _sfx[name] = new SfxChunk(_backend, name, handle);
        Console.WriteLine($"[Audio] loaded SFX: {name}");
        return true;
    }

    public bool LoadMusic(string name, string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Console.Error.WriteLine($"[Audio] asset not found: {name}");
            return false;
        }
        if (!EnsureEnabled())
            return false;
        IntPtr handle;
        try { handle = _backend.LoadMusic(path); }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"[Audio] playback failed: {ex.Message}");
            return false;
        }
        if (handle == IntPtr.Zero)
        {
            Console.Error.WriteLine($"[Audio] load failed '{name}': {_backend.LastError()}");
            return false;
        }
        if (_music.TryGetValue(name, out var existing))
            existing.Dispose();
        _music[name] = new MusicTrack(_backend, name, handle);
        Console.WriteLine($"[Audio] loaded music: {name}");
        return true;
    }

    public bool PlaySfx(string name)
    {
        if (!EnsureEnabled() || !_sfx.TryGetValue(name, out var chunk) || !chunk.IsValid)
            return false;
        _backend.ChunkVolume(chunk.Handle, MapVolume(SfxVolume));
        if (_backend.PlayChunk(chunk.Handle, 0) < 0)
        {
            Console.Error.WriteLine($"[Audio] playback failed: {name} ({_backend.LastError()})");
            return false;
        }
        _sfxPlayCounts[name] = _sfxPlayCounts.TryGetValue(name, out int n) ? n + 1 : 1;
        return true;
    }

    public int SfxPlayCount(string name)
        => _sfxPlayCounts.TryGetValue(name, out int n) ? n : 0;

    /// <summary>Mulai musik sekali + loop; panggilan ulang track sama tidak restart.</summary>
    public bool StartMusic(string name)
    {
        if (!EnsureEnabled() || !_music.TryGetValue(name, out var track) || !track.IsValid)
            return false;
        if (CurrentMusic == name && _backend.PlayingMusic() != 0)
            return true;
        if (_backend.PlayMusic(track.Handle, -1) != 0)
        {
            Console.Error.WriteLine($"[Audio] playback failed: {name} ({_backend.LastError()})");
            return false;
        }
        CurrentMusic = name;
        return true;
    }

    public void StopMusic()
    {
        if (!AudioEnabled)
            return;
        _backend.HaltMusic();
        CurrentMusic = null;
    }

    public void PauseMusic()
    {
        if (AudioEnabled)
            _backend.PauseMusic();
    }

    public void ResumeMusic()
    {
        if (AudioEnabled)
            _backend.ResumeMusic();
    }

    public void SetMusicVolume(int volume)
    {
        MusicVolume = Math.Clamp(volume, 0, 100);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(int volume)
    {
        SfxVolume = Math.Clamp(volume, 0, 100);
    }

    public void Shutdown()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var chunk in _sfx.Values)
            chunk.Dispose();
        _sfx.Clear();
        foreach (var track in _music.Values)
            track.Dispose();
        _music.Clear();
        CurrentMusic = null;
        if (Initialized && AudioEnabled)
        {
            try
            {
                _backend.CloseAudio();
                _backend.Quit();
            }
            catch { /* aman diabaikan saat shutdown */ }
        }
        AudioEnabled = false;
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    private bool EnsureEnabled()
    {
        if (!Initialized || !AudioEnabled)
            return false;
        return true;
    }

    private void ApplyMusicVolume()
    {
        if (AudioEnabled)
            _backend.MusicVolume(MapVolume(MusicVolume));
    }

    private static int MapVolume(int volume) => Math.Clamp(volume, 0, 100) * SdlMixer.MAX_VOLUME / 100;

    private static string? ResolveAsset(string relative)
    {
        string[] candidates =
        {
            relative,
            Path.Combine(AppContext.BaseDirectory, relative),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", relative)),
        };
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
