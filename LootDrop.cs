using SDL2;

namespace AshenOath;

/// <summary>
/// Pickup world object Tahap 17: item ID + jumlah + posisi + bounds + active.
/// Overlap player -> coba AddItem; penuh -> loot tetap di dunia.
/// Render rectangle primitive (warna per tipe).
/// </summary>
public sealed class LootDrop
{
    public const int DropWidth = 24;
    public const int DropHeight = 24;

    public string ItemId { get; }
    public int Quantity { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool Active { get; private set; } = true;
    public Aabb Bounds => new(X, Y, DropWidth, DropHeight);

    public LootDrop(string itemId, int quantity, float x, float y)
    {
        ItemId = itemId;
        Quantity = quantity < 1 ? 1 : quantity;
        X = x;
        Y = y;
    }

    public LootDrop Clone() => new(ItemId, Quantity, X, Y);

    /// <summary>
    /// Coba pickup bila overlap. True bila diambil (jadi inactive).
    /// Inventory penuh -> tetap active.
    /// </summary>
    public bool TryPickup(Player player)
    {
        if (!Active || !player.Alive)
            return false;
        if (!Bounds.Overlaps(player.Bounds))
            return false;
        if (!player.Inventory.AddItem(ItemId, Quantity))
            return false;
        Active = false;
        return true;
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        if (!Active)
            return;
        var def = ItemDatabase.Get(ItemId);
        (byte r, byte g, byte b) = def?.Type switch
        {
            ItemType.Weapon => ((byte)140, (byte)170, (byte)220),
            ItemType.Armor => ((byte)90, (byte)190, (byte)120),
            ItemType.Accessory => ((byte)180, (byte)130, (byte)220),
            ItemType.Potion => ((byte)220, (byte)90, (byte)90),
            ItemType.Currency => ((byte)240, (byte)200, (byte)80),
            _ => ((byte)150, (byte)150, (byte)150),
        };
        SDL.SDL_Rect body = new()
        {
            x = (int)(X - camera.RenderX),
            y = (int)(Y - camera.RenderY),
            w = DropWidth,
            h = DropHeight
        };
        SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
        SDL.SDL_RenderFillRect(renderer, ref body);
        SDL.SDL_Rect inner = new()
        {
            x = (int)(X - camera.RenderX) + 5,
            y = (int)(Y - camera.RenderY) + 5,
            w = DropWidth - 10,
            h = DropHeight - 10
        };
        SDL.SDL_SetRenderDrawColor(renderer, 20, 20, 28, 255);
        SDL.SDL_RenderFillRect(renderer, ref inner);
    }
}
