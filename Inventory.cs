namespace AshenOath;

/// <summary>
/// Satu slot inventory: item ID + jumlah.
/// </summary>
public sealed class InventorySlot
{
    public string ItemId { get; set; } = string.Empty;
    public int Count { get; set; }

    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
}

/// <summary>
/// Inventory 20 slot. Stackable (Potion/Currency) menumpuk sampai MaxStack;
/// equipment tidak stack (satu unit per slot). AddItem all-or-nothing:
/// gagal -> inventory tidak berubah, item tidak hilang.
/// </summary>
public sealed class Inventory
{
    public const int SlotCount = 20;

    private readonly List<InventorySlot> _slots = new();

    public Inventory()
    {
        for (int i = 0; i < SlotCount; i++)
            _slots.Add(new InventorySlot());
    }

    public IReadOnlyList<InventorySlot> Slots => _slots;
    public int UsedSlots => _slots.Count(s => !s.IsEmpty);

    public bool AddItem(string itemId, int quantity = 1)
    {
        var def = ItemDatabase.Get(itemId);
        if (def is null || quantity <= 0)
            return false;

        // Simulasi dulu agar all-or-nothing.
        int[] counts = _slots.Select(s => s.Count).ToArray();
        string[] ids = _slots.Select(s => s.ItemId).ToArray();
        int remaining = quantity;

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (ids[i] == itemId && counts[i] < def.MaxStack)
            {
                int room = def.MaxStack - counts[i];
                int add = Math.Min(room, remaining);
                counts[i] += add;
                remaining -= add;
            }
        }
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (string.IsNullOrEmpty(ids[i]))
            {
                int add = Math.Min(def.MaxStack, remaining);
                ids[i] = itemId;
                counts[i] = add;
                remaining -= add;
            }
        }
        if (remaining > 0)
        {
            Console.Error.WriteLine($"[Inventory] penuh, '{itemId}' x{quantity} ditolak.");
            return false;
        }
        for (int i = 0; i < SlotCount; i++)
        {
            _slots[i].ItemId = ids[i];
            _slots[i].Count = counts[i];
        }
        return true;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            return false;
        if (GetCount(itemId) < quantity)
            return false;
        int remaining = quantity;
        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_slots[i].ItemId == itemId)
            {
                int take = Math.Min(_slots[i].Count, remaining);
                _slots[i].Count -= take;
                remaining -= take;
                if (_slots[i].Count <= 0)
                    _slots[i].ItemId = string.Empty;
            }
        }
        return true;
    }

    public bool HasItem(string itemId, int quantity = 1)
        => !string.IsNullOrEmpty(itemId) && GetCount(itemId) >= quantity;

    public int GetCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;
        int total = 0;
        foreach (var slot in _slots)
        {
            if (slot.ItemId == itemId)
                total += slot.Count;
        }
        return total;
    }

    public void Clear()
    {
        foreach (var slot in _slots)
        {
            slot.ItemId = string.Empty;
            slot.Count = 0;
        }
    }
}
