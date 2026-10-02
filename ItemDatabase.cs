namespace AshenOath;

/// <summary>
/// Registry item statis Tahap 17. Lookup aman: ID tak dikenal -> null.
/// </summary>
public static class ItemDatabase
{
    private static readonly Dictionary<string, Item> Items = new()
    {
        ["rusted_blade"] = new Item("rusted_blade", "Rusted Blade", ItemType.Weapon, attackBonus: 5, sellValue: 5),
        ["ash_blade"] = new Item("ash_blade", "Ash Blade", ItemType.Weapon, attackBonus: 10, sellValue: 40),
        ["worn_armor"] = new Item("worn_armor", "Worn Armor", ItemType.Armor, defenseBonus: 3, sellValue: 8),
        ["forge_armor"] = new Item("forge_armor", "Forge Armor", ItemType.Armor, defenseBonus: 7, maxHpBonus: 10, sellValue: 60),
        ["ash_ring"] = new Item("ash_ring", "Ash Ring", ItemType.Accessory, maxEnergyBonus: 15, sellValue: 25),
        ["small_potion"] = new Item("small_potion", "Small Potion", ItemType.Potion, maxStack: 5, healAmount: 25, sellValue: 5, consumable: true),
        ["ash_shard"] = new Item("ash_shard", "Ash Shard", ItemType.Currency, maxStack: 99, sellValue: 1),
    };

    public static Item? Get(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return Items.TryGetValue(id, out var item) ? item : null;
    }

    public static bool Exists(string id) => Get(id) is not null;
}
