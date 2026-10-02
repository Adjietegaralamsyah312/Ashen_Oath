namespace AshenOath;

/// <summary>
/// Data statis item (value object). Tidak menyimpan reference ke Player.
/// </summary>
public sealed class Item
{
    public string Id { get; }
    public string Name { get; }
    public ItemType Type { get; }
    public int MaxStack { get; }
    public int AttackBonus { get; }
    public int DefenseBonus { get; }
    public int MaxHpBonus { get; }
    public int MaxEnergyBonus { get; }
    public int HealAmount { get; }
    public int SellValue { get; }
    public bool Consumable { get; }

    public Item(string id, string name, ItemType type, int maxStack = 1,
        int attackBonus = 0, int defenseBonus = 0, int maxHpBonus = 0,
        int maxEnergyBonus = 0, int healAmount = 0, int sellValue = 0,
        bool consumable = false)
    {
        Id = id;
        Name = name;
        Type = type;
        MaxStack = maxStack < 1 ? 1 : maxStack;
        AttackBonus = attackBonus;
        DefenseBonus = defenseBonus;
        MaxHpBonus = maxHpBonus;
        MaxEnergyBonus = maxEnergyBonus;
        HealAmount = healAmount;
        SellValue = sellValue;
        Consumable = consumable;
    }

    public bool IsEquipment => Type is ItemType.Weapon or ItemType.Armor or ItemType.Accessory;
}
