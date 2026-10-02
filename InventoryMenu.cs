using SDL2;

namespace AshenOath;

/// <summary>
/// Menu inventory Tahap 17: grid 20 slot + 3 equipment slot + stat bar.
/// Rectangle-only tanpa font. Navigasi Up/Down/Left/Right + Enter + Escape/I.
/// </summary>
public sealed class InventoryMenu
{
    public const int Columns = 5;

    public enum InventoryAction
    {
        None,
        Equipped,
        UsedPotion,
        Denied,
    }

    public int SelectedIndex { get; private set; }

    public void Reset() => SelectedIndex = 0;

    public void MoveLeft() => SelectedIndex = (SelectedIndex - 1 + Inventory.SlotCount) % Inventory.SlotCount;
    public void MoveRight() => SelectedIndex = (SelectedIndex + 1) % Inventory.SlotCount;
    public void MoveUp() => SelectedIndex = (SelectedIndex - Columns + Inventory.SlotCount) % Inventory.SlotCount;
    public void MoveDown() => SelectedIndex = (SelectedIndex + Columns) % Inventory.SlotCount;

    /// <summary>
    /// Enter pada slot: equipment -> Equip, potion -> Use, currency -> Denied.
    /// </summary>
    public InventoryAction Activate(Player player)
    {
        if (SelectedIndex < 0 || SelectedIndex >= Inventory.SlotCount)
            return InventoryAction.None;
        var slot = player.Inventory.Slots[SelectedIndex];
        if (slot.IsEmpty)
            return InventoryAction.None;
        var def = ItemDatabase.Get(slot.ItemId);
        if (def is null)
            return InventoryAction.None;
        if (def.IsEquipment)
        {
            bool ok = player.Equipment.Equip(player.Inventory, slot.ItemId);
            if (ok)
            {
                player.ClampStatsToEffective();
                Console.WriteLine($"[Inventory] equipped {def.Name}.");
                return InventoryAction.Equipped;
            }
            return InventoryAction.Denied;
        }
        if (def.Type == ItemType.Potion)
        {
            bool ok = player.TryDrinkPotion();
            if (ok)
            {
                Console.WriteLine($"[Inventory] used {def.Name}.");
                return InventoryAction.UsedPotion;
            }
            return InventoryAction.Denied;
        }
        return InventoryAction.Denied;
    }

    public void Render(IntPtr renderer, int windowWidth, int windowHeight, Player player)
    {
        SDL.SDL_Rect dim = new() { x = 0, y = 0, w = windowWidth, h = windowHeight };
        SDL.SDL_SetRenderDrawColor(renderer, 8, 8, 14, 220);
        SDL.SDL_RenderFillRect(renderer, ref dim);

        BitmapFont.DrawTextCentered(renderer, "INVENTORY", windowWidth / 2, 50);

        const int slotSize = 56;
        const int gap = 8;
        int gridW = Columns * slotSize + (Columns - 1) * gap;
        int startX = windowWidth / 2 - gridW / 2;
        int startY = 90;

        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            int x = startX + col * (slotSize + gap);
            int y = startY + row * (slotSize + gap);
            SDL.SDL_Rect slot = new() { x = x, y = y, w = slotSize, h = slotSize };
            var s = player.Inventory.Slots[i];
            var def = ItemDatabase.Get(s.ItemId);
            if (def is null)
            {
                SDL.SDL_SetRenderDrawColor(renderer, 30, 30, 42, 255);
            }
            else
            {
                var (sr, sg, sb) = SlotColor(def.Type);
                SDL.SDL_SetRenderDrawColor(renderer, sr, sg, sb, 255);
            }
            SDL.SDL_RenderFillRect(renderer, ref slot);

            if (def is not null)
            {
                // Icon dalam: kotak kecil tengah.
                SDL.SDL_Rect icon = new() { x = x + 14, y = y + 10, w = 28, h = 28 };
                SDL.SDL_SetRenderDrawColor(renderer, 18, 18, 26, 255);
                SDL.SDL_RenderFillRect(renderer, ref icon);
                // Count bar bawah: fill = count/maxStack.
                float ratio = Math.Clamp((float)s.Count / def.MaxStack, 0f, 1f);
                int fillW = (int)((slotSize - 8) * ratio);
                if (fillW > 0)
                {
                    SDL.SDL_Rect bar = new() { x = x + 4, y = y + slotSize - 10, w = fillW, h = 6 };
                    SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
                    SDL.SDL_RenderFillRect(renderer, ref bar);
                }
                // Equipped: bingkai hijau bila ID ini terpasang.
                if (IsEquipped(player, s.ItemId))
                {
                    SDL.SDL_SetRenderDrawColor(renderer, 90, 210, 110, 255);
                    SDL.SDL_RenderDrawRect(renderer, ref slot);
                }
            }
            if (i == SelectedIndex)
            {
                SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
                SDL.SDL_RenderDrawRect(renderer, ref slot);
            }
        }

        // Equipment row: Weapon | Armor | Accessory.
        int eqY = startY + 4 * (slotSize + gap) + 16;
        RenderEquipSlot(renderer, startX, eqY, slotSize, player.Equipment.WeaponId, SelectedIndex < 0);
        RenderEquipSlot(renderer, startX + (slotSize + gap), eqY, slotSize, player.Equipment.ArmorId, false);
        RenderEquipSlot(renderer, startX + 2 * (slotSize + gap), eqY, slotSize, player.Equipment.AccessoryId, false);

        // Stat bar: ATK / DEF / HP / EN (urutan tetap).
        int statX = startX + 3 * (slotSize + gap) + 16;
        string[] statLabels = { "ATK", "DEF", "HP", "EN" };
        int[] statValues = { player.EffectiveAttack, player.EffectiveDefense, player.EffectiveMaxHp, (int)player.EffectiveMaxEnergy };
        int[] statMax = { 20, 10, 150, 150 };
        for (int s = 0; s < 4; s++)
        {
            BitmapFont.DrawText(renderer, statLabels[s], statX - 44, eqY + s * 18 + 1, 2, 150, 150, 165);
            RenderStat(renderer, statX, eqY + s * 18, slotSize * 2, statValues[s], statMax[s]);
        }

        // Nama item terpilih.
        var sel = player.Inventory.Slots[SelectedIndex];
        var selDef = ItemDatabase.Get(sel.ItemId);
        string selName = selDef is null ? "EMPTY SLOT" : $"{selDef.Name.ToUpperInvariant()} X{sel.Count}";
        BitmapFont.DrawTextCentered(renderer, selName, windowWidth / 2, eqY + 78);
        BitmapFont.DrawTextCentered(renderer, $"SHARDS: {player.Inventory.GetCount("ash_shard")}", windowWidth / 2, eqY + 100, 2, 240, 200, 80);
    }

    private static (byte, byte, byte) SlotColor(ItemType type) => type switch
    {
        ItemType.Weapon => ((byte)70, (byte)90, (byte)130),
        ItemType.Armor => ((byte)60, (byte)120, (byte)80),
        ItemType.Accessory => ((byte)110, (byte)80, (byte)140),
        ItemType.Potion => ((byte)140, (byte)60, (byte)60),
        ItemType.Currency => ((byte)140, (byte)120, (byte)50),
        _ => ((byte)60, (byte)60, (byte)70),
    };

    private static bool IsEquipped(Player player, string itemId)
        => !string.IsNullOrEmpty(itemId)
            && (player.Equipment.WeaponId == itemId
                || player.Equipment.ArmorId == itemId
                || player.Equipment.AccessoryId == itemId);

    private static void RenderEquipSlot(IntPtr renderer, int x, int y, int size, string? itemId, bool selected)
    {
        SDL.SDL_Rect slot = new() { x = x, y = y, w = size, h = size };
        var def = itemId is null ? null : ItemDatabase.Get(itemId);
        if (def is null)
        {
            SDL.SDL_SetRenderDrawColor(renderer, 25, 25, 35, 255);
        }
        else
        {
            var (er, eg, eb) = SlotColor(def.Type);
            SDL.SDL_SetRenderDrawColor(renderer, er, eg, eb, 255);
        }
        SDL.SDL_RenderFillRect(renderer, ref slot);
        SDL.SDL_SetRenderDrawColor(renderer, 150, 150, 170, 255);
        SDL.SDL_RenderDrawRect(renderer, ref slot);
        if (selected)
        {
            SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
            SDL.SDL_RenderDrawRect(renderer, ref slot);
        }
    }

    private static void RenderStat(IntPtr renderer, int x, int y, int w, int value, int max)
    {
        SDL.SDL_Rect back = new() { x = x, y = y, w = w, h = 12 };
        SDL.SDL_SetRenderDrawColor(renderer, 20, 20, 30, 255);
        SDL.SDL_RenderFillRect(renderer, ref back);
        float ratio = max <= 0 ? 0f : Math.Clamp((float)value / max, 0f, 1f);
        int fillW = (int)(w * ratio);
        if (fillW > 0)
        {
            SDL.SDL_Rect fill = new() { x = x, y = y, w = fillW, h = 12 };
            SDL.SDL_SetRenderDrawColor(renderer, 120, 200, 140, 255);
            SDL.SDL_RenderFillRect(renderer, ref fill);
        }
    }
}
