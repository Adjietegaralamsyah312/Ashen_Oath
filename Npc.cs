using SDL2;

namespace AshenOath;

/// <summary>
/// NPC statis Tahap 18: quest giver / shop. Tidak bergerak, tidak menyerang.
/// Render placeholder berbeda dari enemy (jubah + kepala + tongkat).
/// </summary>
public sealed class Npc
{
    public const float InteractRadius = 80f;
    public const int NpcWidth = 36;
    public const int NpcHeight = 56;

    public string Id { get; }
    public string Name { get; }
    public float X { get; }
    public float Y { get; }
    public string DialogueId { get; }
    public string? ShopId { get; }
    public string? QuestId { get; }
    public bool Active { get; set; } = true;
    public int Width => NpcWidth;
    public int Height => NpcHeight;
    public Aabb Bounds => new(X, Y, NpcWidth, NpcHeight);

    public Npc(string id, string name, float x, float y, string dialogueId,
        string? shopId = null, string? questId = null)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        DialogueId = dialogueId;
        ShopId = shopId;
        QuestId = questId;
    }

    /// <summary>True bila player cukup dekat untuk interaksi (sekitar 80 px).</summary>
    public bool IsPlayerNear(Player player)
    {
        if (!Active || !player.Alive)
            return false;
        float cx = X + NpcWidth / 2f;
        float cy = Y + NpcHeight / 2f;
        float px = player.X + Player.Width / 2f;
        float py = player.Y + Player.Height / 2f;
        float dx = px - cx;
        float dy = py - cy;
        return MathF.Sqrt(dx * dx + dy * dy) <= InteractRadius;
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        if (!Active)
            return;
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        bool keeper = ShopId is not null;

        // Jubah (badan): ungu tua elder, oranye keeper.
        SDL.SDL_Rect robe = new() { x = sx + 4, y = sy + 16, w = NpcWidth - 8, h = NpcHeight - 16 };
        if (keeper)
            SDL.SDL_SetRenderDrawColor(renderer, 190, 110, 50, 255);
        else
            SDL.SDL_SetRenderDrawColor(renderer, 120, 90, 170, 255);
        SDL.SDL_RenderFillRect(renderer, ref robe);

        // Kepala.
        SDL.SDL_Rect head = new() { x = sx + 9, y = sy, w = NpcWidth - 18, h = 16 };
        SDL.SDL_SetRenderDrawColor(renderer, 225, 190, 150, 255);
        SDL.SDL_RenderFillRect(renderer, ref head);

        // Tongkat di sisi kanan.
        SDL.SDL_Rect staff = new() { x = sx + NpcWidth - 4, y = sy + 6, w = 4, h = NpcHeight - 6 };
        SDL.SDL_SetRenderDrawColor(renderer, 150, 110, 70, 255);
        SDL.SDL_RenderFillRect(renderer, ref staff);
    }
}
