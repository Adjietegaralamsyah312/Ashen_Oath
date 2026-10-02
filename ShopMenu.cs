using SDL2;

namespace AshenOath;

/// <summary>
/// Menu shop Tahap 18: nama + harga + owned, stok unlimited.
/// Up/Down selection, Enter beli, Escape kembali. BitmapFont.
/// </summary>
public sealed class ShopMenu
{
    public Shop? Shop { get; private set; }
    public int SelectedIndex { get; private set; }

    public void Open(Shop shop)
    {
        Shop = shop;
        SelectedIndex = 0;
    }

    public void Close() => Shop = null;
    public bool IsOpen => Shop is not null;

    public void MoveUp()
    {
        if (Shop is null || Shop.Items.Count == 0)
            return;
        SelectedIndex = (SelectedIndex - 1 + Shop.Items.Count) % Shop.Items.Count;
    }

    public void MoveDown()
    {
        if (Shop is null || Shop.Items.Count == 0)
            return;
        SelectedIndex = (SelectedIndex + 1) % Shop.Items.Count;
    }

    public string? SelectedItemId()
    {
        if (Shop is null || Shop.Items.Count == 0)
            return null;
        return Shop.Items[Math.Clamp(SelectedIndex, 0, Shop.Items.Count - 1)].ItemId;
    }

    /// <summary>Enter: beli item terpilih. True bila transaksi berhasil.</summary>
    public bool BuySelected(Player player)
    {
        if (Shop is null)
            return false;
        string? id = SelectedItemId();
        if (string.IsNullOrEmpty(id))
            return false;
        return Shop.Buy(player, id);
    }

    public void Render(IntPtr renderer, int windowWidth, int windowHeight, Player player)
    {
        if (Shop is null)
            return;
        SDL.SDL_Rect dim = new() { x = 0, y = 0, w = windowWidth, h = windowHeight };
        SDL.SDL_SetRenderDrawColor(renderer, 8, 8, 14, 220);
        SDL.SDL_RenderFillRect(renderer, ref dim);

        SDL.SDL_Rect panel = new() { x = windowWidth / 2 - 320, y = 60, w = 640, h = 400 };
        SDL.SDL_SetRenderDrawColor(renderer, 14, 14, 24, 255);
        SDL.SDL_RenderFillRect(renderer, ref panel);
        SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
        SDL.SDL_RenderDrawRect(renderer, ref panel);

        BitmapFont.DrawTextCentered(renderer, "FORGE SHOP", windowWidth / 2, 80);
        BitmapFont.DrawText(renderer, $"SHARDS: {player.Inventory.GetCount(Shop.CurrencyId)}",
            windowWidth / 2 - 290, 110, 2, 240, 200, 80);

        for (int i = 0; i < Shop.Items.Count; i++)
        {
            var entry = Shop.Items[i];
            var def = ItemDatabase.Get(entry.ItemId);
            string name = def?.Name.ToUpperInvariant() ?? entry.ItemId.ToUpperInvariant();
            int y = 150 + i * 56;
            SDL.SDL_Rect row = new() { x = windowWidth / 2 - 290, y = y, w = 580, h = 48 };
            if (i == SelectedIndex)
                SDL.SDL_SetRenderDrawColor(renderer, 90, 140, 200, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 40, 40, 55, 255);
            SDL.SDL_RenderFillRect(renderer, ref row);
            if (i == SelectedIndex)
            {
                SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
                SDL.SDL_RenderDrawRect(renderer, ref row);
            }
            BitmapFont.DrawText(renderer, name, windowWidth / 2 - 275, y + 6);
            BitmapFont.DrawText(renderer, $"{entry.Price} SHARDS - OWNED {player.Inventory.GetCount(entry.ItemId)}",
                windowWidth / 2 - 275, y + 26, 2, 170, 180, 190);
        }

        BitmapFont.DrawTextCentered(renderer, "ENTER BUY - ESC BACK", windowWidth / 2, 400, 2, 150, 150, 165);
    }
}
