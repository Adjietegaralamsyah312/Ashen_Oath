namespace AshenOath;

/// <summary>Satu baris dagangan: item ID + harga ash_shard.</summary>
public sealed class ShopItem
{
    public string ItemId { get; }
    public int Price { get; }

    public ShopItem(string itemId, int price)
    {
        ItemId = itemId;
        Price = price < 0 ? 0 : price;
    }
}

/// <summary>
/// Shop Tahap 18. Mata uang ash_shard. Stok unlimited (tanpa persistensi).
/// Buy all-or-nothing: dana/item tak berubah bila gagal.
/// </summary>
public sealed class Shop
{
    public const string ForgeShopId = "forge_shop";
    public const string CurrencyId = "ash_shard";

    public string Id { get; }
    public List<ShopItem> Items { get; } = new();

    public Shop(string id)
    {
        Id = id;
    }

    public static Shop CreateForgeShop()
    {
        var shop = new Shop(ForgeShopId);
        shop.Items.Add(new ShopItem("small_potion", 10));
        shop.Items.Add(new ShopItem("worn_armor", 30));
        shop.Items.Add(new ShopItem("ash_ring", 50));
        shop.Items.Add(new ShopItem("ash_blade", 100));
        return shop;
    }

    /// <summary>
    /// Beli 1 unit: cukup shard + muat inventory -> shard kurang, item masuk.
    /// Gagal -> tidak ada perubahan.
    /// </summary>
    public bool Buy(Player player, string itemId)
    {
        ShopItem? entry = null;
        foreach (var item in Items)
        {
            if (item.ItemId == itemId)
            {
                entry = item;
                break;
            }
        }
        if (entry is null || ItemDatabase.Get(itemId) is null)
            return false;
        if (!player.Inventory.HasItem(CurrencyId, entry.Price))
            return false;
        if (!player.Inventory.AddItem(itemId, 1))
            return false;
        player.Inventory.RemoveItem(CurrencyId, entry.Price);
        return true;
    }
}
