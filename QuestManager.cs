namespace AshenOath;

/// <summary>
/// Manager quest Tahap 18: active/available, start, tracking kill stage-aware,
/// claim, reset. Logic di sini, bukan Game.cs.
/// Kill dihitung sekali per instance enemy (guard duplikat/revive);
/// boss tidak pernah dihitung.
/// </summary>
public sealed class QuestManager
{
    public const string RoadCleansingId = "road_cleansing";

    private readonly Dictionary<string, Quest> _quests = new();
    private readonly HashSet<Enemy> _countedKills = new();

    public QuestManager()
    {
        var cleansing = new Quest(
            RoadCleansingId,
            "Road Cleansing",
            "Defeat enemies near the old road.",
            new QuestObjective(QuestObjectiveType.KillEnemy, 5, stageScope: "stage1"));
        cleansing.AddReward("small_potion", 1);
        cleansing.SetRewardCurrency(30);
        _quests[cleansing.Id] = cleansing;
    }

    public Quest? GetQuest(string id)
        => _quests.TryGetValue(id, out var quest) ? quest : null;

    public Quest? ActiveQuest()
    {
        foreach (var quest in _quests.Values)
        {
            if (quest.Status is QuestStatus.Active or QuestStatus.Completed)
                return quest;
        }
        return null;
    }

    public QuestStatus GetStatus(string id)
        => GetQuest(id)?.Status ?? QuestStatus.NotStarted;

    public bool StartQuest(string id)
    {
        var quest = GetQuest(id);
        if (quest is null)
            return false;
        return quest.Start();
    }

    /// <summary>
    /// Kill tracking dari event Died existing. Hanya Stage 1 Slime/Skeleton/Bat,
    /// sekali per instance. True bila quest baru saja Completed.
    /// </summary>
    public bool NotifyKill(Enemy enemy, int stageNumber)
    {
        if (enemy is null || enemy is Boss)
            return false;
        if (enemy is not (Slime or Skeleton or Bat))
            return false;
        var quest = ActiveQuest();
        if (quest is null || quest.Status != QuestStatus.Active)
            return false;
        if (quest.Objective.Type != QuestObjectiveType.KillEnemy)
            return false;
        string scope = quest.Objective.StageScope ?? string.Empty;
        if (scope == "stage1" && stageNumber != 1)
            return false;
        if (!_countedKills.Add(enemy))
            return false; // duplikat / revive: sudah dihitung.
        quest.Objective.AddProgress(1);
        if (quest.Objective.IsComplete)
        {
            quest.MarkCompleted();
            return true;
        }
        return false;
    }

    public bool ClaimReward(string id, Player player)
    {
        var quest = GetQuest(id);
        if (quest is null)
            return false;
        return quest.Claim(player);
    }

    public bool IsClaimed(string id) => GetQuest(id)?.Status == QuestStatus.Claimed;

    public void Reset()
    {
        foreach (var quest in _quests.Values)
        {
            quest.Objective.Reset();
            quest.SetStatusForLoad(QuestStatus.NotStarted);
        }
        _countedKills.Clear();
    }

    /// <summary>Terapkan save (unknown quest ID diabaikan).</summary>
    public void ApplySave(GameProgress progress)
    {
        Reset();
        if (progress is null)
            return;
        string? activeId = progress.ActiveQuestId;
        if (string.IsNullOrEmpty(activeId) || GetQuest(activeId) is null)
        {
            if (!string.IsNullOrEmpty(activeId))
                Console.Error.WriteLine($"[Quest] warning: quest tak dikenal '{activeId}' diabaikan.");
            ApplyClaimed(progress.ClaimedQuestIds);
            return;
        }
        var quest = GetQuest(activeId)!;
        quest.SetStatusForLoad(ParseStatus(progress.QuestStatus));
        quest.Objective.SetProgress(progress.ObjectiveProgress);
        // Konsistensi: progress penuh + Active -> Completed.
        if (quest.Status == QuestStatus.Active && quest.Objective.IsComplete)
            quest.MarkCompleted();
        ApplyClaimed(progress.ClaimedQuestIds);
    }

    private void ApplyClaimed(List<string>? claimedIds)
    {
        if (claimedIds is null)
            return;
        foreach (string id in claimedIds)
        {
            var quest = GetQuest(id);
            if (quest is null)
            {
                Console.Error.WriteLine($"[Quest] warning: claimed quest tak dikenal '{id}' diabaikan.");
                continue;
            }
            quest.Objective.SetProgress(quest.Objective.RequiredAmount);
            quest.SetStatusForLoad(QuestStatus.Claimed);
        }
    }

    /// <summary>Tulis state manager -> progress (sebelum save).</summary>
    public void SyncProgress(GameProgress progress)
    {
        var active = ActiveQuest();
        // Claimed quest tidak lagi active; catat via claimed list.
        var claimed = new List<string>();
        foreach (var quest in _quests.Values)
        {
            if (quest.Status == QuestStatus.Claimed)
                claimed.Add(quest.Id);
        }
        if (active is not null)
        {
            progress.ActiveQuestId = active.Id;
            progress.QuestStatus = active.Status.ToString();
            progress.ObjectiveProgress = active.Objective.CurrentAmount;
        }
        else
        {
            progress.ActiveQuestId = null;
            progress.QuestStatus = QuestStatus.NotStarted.ToString();
            progress.ObjectiveProgress = 0;
        }
        progress.ClaimedQuestIds = claimed;
    }

    private static QuestStatus ParseStatus(string? status) => status switch
    {
        "Active" => QuestStatus.Active,
        "Completed" => QuestStatus.Completed,
        "Claimed" => QuestStatus.Claimed,
        _ => QuestStatus.NotStarted,
    };
}
