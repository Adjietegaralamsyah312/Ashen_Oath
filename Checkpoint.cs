using SDL2;

namespace AshenOath;

/// <summary>
/// Checkpoint prototype: zona sentuh + titik respawn aman + state aktif.
/// Belum perlu save ke disk.
/// </summary>
public sealed class Checkpoint
{
    public string Id { get; }
    public float RespawnX { get; }
    public float RespawnY { get; }
    public bool Active { get; private set; }
    public Aabb Zone => _zone;

    private readonly Aabb _zone;
    private readonly float _poleX;
    private readonly float _baseY;

    public Checkpoint(float respawnX, float respawnY, float zoneX, float zoneY, float zoneW, float zoneH, float poleX, float baseY, string id = "")
    {
        Id = id ?? string.Empty;
        RespawnX = respawnX;
        RespawnY = respawnY;
        _zone = new Aabb(zoneX, zoneY, zoneW, zoneH);
        _poleX = poleX;
        _baseY = baseY;
    }

    /// <summary>True bila aktivasi baru terjadi frame ini.</summary>
    public bool TryActivate(Aabb playerBounds)
    {
        if (Active || !_zone.Overlaps(playerBounds))
            return false;
        Active = true;
        return true;
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        int baseX = (int)(_poleX - camera.RenderX);
        int baseY = (int)(_baseY - camera.RenderY);

        // Basis + tiang.
        SDL.SDL_Rect bottom = new() { x = baseX - 4, y = baseY - 6, w = 14, h = 6 };
        SDL.SDL_SetRenderDrawColor(renderer, 90, 90, 100, 255);
        SDL.SDL_RenderFillRect(renderer, ref bottom);

        SDL.SDL_Rect pole = new() { x = baseX, y = baseY - 80, w = 6, h = 80 };
        SDL.SDL_SetRenderDrawColor(renderer, 150, 150, 160, 255);
        SDL.SDL_RenderFillRect(renderer, ref pole);

        // Bendera: emas bila aktif, abu bila belum.
        SDL.SDL_Rect flag = new() { x = baseX + 6, y = baseY - 80, w = 26, h = 16 };
        if (Active)
            SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
        else
            SDL.SDL_SetRenderDrawColor(renderer, 100, 100, 110, 255);
        SDL.SDL_RenderFillRect(renderer, ref flag);
    }
}
