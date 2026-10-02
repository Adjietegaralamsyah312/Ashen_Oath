namespace AshenOath;

/// <summary>Abstraksi RNG agar drop test deterministik (tanpa global random flaky).</summary>
public interface IRng
{
    double NextDouble();
}

/// <summary>RNG gameplay (System.Random).</summary>
public sealed class SystemRng : IRng
{
    private readonly Random _random = new();
    public double NextDouble() => _random.NextDouble();
}

/// <summary>RNG deterministik untuk test (antrean nilai, default 1.0 = tidak drop).</summary>
public sealed class FixedRng : IRng
{
    private readonly Queue<double> _values;
    public FixedRng(IEnumerable<double>? values = null)
    {
        _values = new Queue<double>(values ?? Enumerable.Empty<double>());
    }
    public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : 1.0;
}

/// <summary>Satu reward drop: item ID + jumlah.</summary>
public readonly struct LootReward
{
    public string ItemId { get; }
    public int Quantity { get; }
    public LootReward(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}

/// <summary>
/// Tabel drop Tahap 17. Tiap entri roll independen:
/// Slime potion 20% + shard 40%; Skeleton potion 30% + shard 60%;
/// Bat ring 10% + shard 50%; Boss ash_blade + forge_armor guaranteed.
/// </summary>
public static class DropTable
{
    public static List<LootReward> RollDrops(Enemy enemy, IRng rng)
    {
        var out_ = new List<LootReward>();
        if (enemy is AshenWarden)
        {
            out_.Add(new LootReward("ash_blade", 1));
            out_.Add(new LootReward("forge_armor", 1));
            return out_;
        }
        if (enemy is Slime)
        {
            if (rng.NextDouble() < 0.20) out_.Add(new LootReward("small_potion", 1));
            if (rng.NextDouble() < 0.40) out_.Add(new LootReward("ash_shard", 1));
        }
        else if (enemy is Skeleton)
        {
            if (rng.NextDouble() < 0.30) out_.Add(new LootReward("small_potion", 1));
            if (rng.NextDouble() < 0.60) out_.Add(new LootReward("ash_shard", 1));
        }
        else if (enemy is Bat)
        {
            if (rng.NextDouble() < 0.10) out_.Add(new LootReward("ash_ring", 1));
            if (rng.NextDouble() < 0.50) out_.Add(new LootReward("ash_shard", 1));
        }
        return out_;
    }
}
