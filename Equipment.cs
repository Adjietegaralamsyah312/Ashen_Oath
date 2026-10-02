namespace AshenOath;

/// <summary>
/// Equipment 3 slot (Weapon/Armor/Accessory), satu item per slot.
/// Equip memindah item inventory -> slot; item lama kembali ke inventory.
/// Jika item lama tidak muat, equip dibatalkan tanpa kehilangan item.
/// </summary>
public sealed class Equipment
{
    public string? WeaponId { get; private set; }
    public string? ArmorId { get; private set; }
    public string? AccessoryId { get; private set; }

    public string? GetEquipped(ItemType type) => type switch
    {
        ItemType.Weapon => WeaponId,
        ItemType.Armor => ArmorId,
        ItemType.Accessory => AccessoryId,
        _ => null,
    };

    public bool Equip(Inventory inventory, string itemId)
    {
        var def = ItemDatabase.Get(itemId);
        if (def is null || !def.IsEquipment)
            return false;
        if (!inventory.HasItem(itemId))
            return false;

        string? current = GetEquipped(def.Type);
        if (current == itemId && inventory.GetCount(itemId) == 1 && IsOnlyCopy(inventory, itemId))
        {
            // Sudah terpasang satu-satunya copy: nothing to do.
            return true;
        }

        // Keluarkan dari inventory dulu.
        if (!inventory.RemoveItem(itemId))
            return false;

        // Kembalikan item lama; bila penuh, batalkan (kembalikan item baru).
        if (!string.IsNullOrEmpty(current))
        {
            if (!inventory.AddItem(current!, 1))
            {
                inventory.AddItem(itemId, 1);
                Console.Error.WriteLine("[Equipment] inventory penuh, equip dibatalkan.");
                return false;
            }
        }

        SetSlot(def.Type, itemId);
        return true;
    }

    public bool Unequip(Inventory inventory, ItemType type)
    {
        string? current = GetEquipped(type);
        if (string.IsNullOrEmpty(current))
            return false;
        if (!inventory.AddItem(current!, 1))
        {
            Console.Error.WriteLine("[Equipment] inventory penuh, unequip dibatalkan.");
            return false;
        }
        SetSlot(type, null);
        return true;
    }

    public void Clear()
    {
        WeaponId = null;
        ArmorId = null;
        AccessoryId = null;
    }

    private void SetSlot(ItemType type, string? itemId)
    {
        switch (type)
        {
            case ItemType.Weapon: WeaponId = itemId; break;
            case ItemType.Armor: ArmorId = itemId; break;
            case ItemType.Accessory: AccessoryId = itemId; break;
        }
    }

    private static bool IsOnlyCopy(Inventory inventory, string itemId)
        => inventory.GetCount(itemId) == 0;
}
