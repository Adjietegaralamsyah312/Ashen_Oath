namespace AshenOath;

/// <summary>Jenis objective quest (tanpa scripting kompleks).</summary>
public enum QuestObjectiveType
{
    KillEnemy,
    CollectItem,
    ReachLocation,
}

/// <summary>
/// Satu objective quest: tipe + target + progress.
/// StageScope membatasi kill quest stage-spesifik (mis. "stage1").
/// </summary>
public sealed class QuestObjective
{
    public QuestObjectiveType Type { get; }
    public int RequiredAmount { get; }
    public int CurrentAmount { get; private set; }
    public string? StageScope { get; }

    public QuestObjective(QuestObjectiveType type, int requiredAmount, string? stageScope = null)
    {
        Type = type;
        RequiredAmount = requiredAmount < 1 ? 1 : requiredAmount;
        StageScope = stageScope;
    }

    public bool IsComplete => CurrentAmount >= RequiredAmount;

    public void AddProgress(int amount = 1)
    {
        if (amount <= 0)
            return;
        CurrentAmount = Math.Min(RequiredAmount, CurrentAmount + amount);
    }

    public void SetProgress(int amount)
    {
        CurrentAmount = Math.Clamp(amount, 0, RequiredAmount);
    }

    public void Reset() => CurrentAmount = 0;
}
