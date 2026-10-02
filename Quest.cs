namespace AshenOath;

/// <summary>Status quest: NotStarted -> Active -> Completed -> Claimed.</summary>
public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    Claimed,
}

/// <summary>
/// Quest Tahap 18: data + objective + reward. Tanpa scripting kompleks.
/// </summary>
public sealed class Quest
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public QuestObjective Objective { get; }
    public QuestStatus Status { get; private set; } = QuestStatus.NotStarted;
    public List<LootReward> RewardItems { get; } = new();
    public int RewardCurrency { get; private set; }

    public Quest(string id, string name, string description, QuestObjective objective)
    {
        Id = id;
        Name = name;
        Description = description;
        Objective = objective;
    }

    public void AddReward(string itemId, int quantity)
    {
        if (!string.IsNullOrEmpty(itemId) && quantity > 0)
            RewardItems.Add(new LootReward(itemId, quantity));
    }

    public void SetRewardCurrency(int amount)
    {
        RewardCurrency = amount < 0 ? 0 : amount;
    }

    public bool Start()
    {
        if (Status != QuestStatus.NotStarted)
            return false;
        Status = QuestStatus.Active;
        return true;
    }

    public void MarkCompleted()
    {
        if (Status == QuestStatus.Active)
            Status = QuestStatus.Completed;
    }

    /// <summary>
    /// Klaim reward via NPC, all-or-nothing: gagal -> quest tetap Completed,
    /// inventory/currency tidak berubah.
    /// </summary>
    public bool Claim(Player player)
    {
        if (Status != QuestStatus.Completed)
            return false;
        int shards = player.Inventory.GetCount("ash_shard");
        if (RewardCurrency > 0 && !player.Inventory.AddItem("ash_shard", RewardCurrency))
            return false;
        bool itemsOk = true;
        var added = new List<LootReward>();
        foreach (var reward in RewardItems)
        {
            if (player.Inventory.AddItem(reward.ItemId, reward.Quantity))
                added.Add(reward);
            else
            {
                itemsOk = false;
                break;
            }
        }
        if (!itemsOk)
        {
            // Rollback: kembalikan state inventory seperti semula.
            player.Inventory.RemoveItem("ash_shard", player.Inventory.GetCount("ash_shard") - shards);
            foreach (var reward in added)
                player.Inventory.RemoveItem(reward.ItemId, reward.Quantity);
            return false;
        }
        Status = QuestStatus.Claimed;
        return true;
    }

    public void SetStatusForLoad(QuestStatus status)
    {
        Status = status;
    }
}
