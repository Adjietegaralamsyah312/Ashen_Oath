using System.Text.Json.Serialization;

namespace AshenOath;

/// <summary>
/// Progression Tahap 20 (schema v6): v5 + difficulty.
/// Property names stabil (lowerCamel) agar save bisa dimigrasi.
/// Derived stats TIDAK disimpan; dihitung ulang dari level + equipment.
/// Transient (hit stop / shake / flash) TIDAK disimpan.
/// </summary>
public sealed class GameProgress
{
    public const int CurrentVersion = 6;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("unlockedStage")]
    public int UnlockedStage { get; set; } = 1;

    [JsonPropertyName("currentStage")]
    public int CurrentStage { get; set; } = 1;

    [JsonPropertyName("checkpointId")]
    public string CheckpointId { get; set; } = string.Empty;

    [JsonPropertyName("stage1Completed")]
    public bool Stage1Completed { get; set; }

    [JsonPropertyName("bossDefeated")]
    public bool BossDefeated { get; set; }

    [JsonPropertyName("stage2Completed")]
    public bool Stage2Completed { get; set; }

    [JsonPropertyName("inventory")]
    public List<InventoryEntry> Inventory { get; set; } = new();

    [JsonPropertyName("equipment")]
    public EquipmentData Equipment { get; set; } = new();

    [JsonPropertyName("activeQuestId")]
    public string? ActiveQuestId { get; set; }

    [JsonPropertyName("questStatus")]
    public string QuestStatus { get; set; } = "NotStarted";

    [JsonPropertyName("objectiveProgress")]
    public int ObjectiveProgress { get; set; }

    [JsonPropertyName("claimedQuestIds")]
    public List<string> ClaimedQuestIds { get; set; } = new();

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("currentXp")]
    public int CurrentXp { get; set; }

    [JsonPropertyName("difficulty")]
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    public static GameProgress Default() => new()
    {
        Version = CurrentVersion,
        UnlockedStage = 1,
        CurrentStage = 1,
        CheckpointId = string.Empty,
        Stage1Completed = false,
        BossDefeated = false,
        Stage2Completed = false,
        Inventory = new List<InventoryEntry>(),
        Equipment = new EquipmentData(),
        ActiveQuestId = null,
        QuestStatus = "NotStarted",
        ObjectiveProgress = 0,
        ClaimedQuestIds = new List<string>(),
        Level = 1,
        CurrentXp = 0,
        Difficulty = Difficulty.Normal,
    };

    public void ResetToNewGame()
    {
        Version = CurrentVersion;
        UnlockedStage = 1;
        CurrentStage = 1;
        CheckpointId = string.Empty;
        Stage1Completed = false;
        BossDefeated = false;
        Stage2Completed = false;
        Inventory = new List<InventoryEntry>();
        Equipment = new EquipmentData();
        ActiveQuestId = null;
        QuestStatus = "NotStarted";
        ObjectiveProgress = 0;
        ClaimedQuestIds = new List<string>();
        Level = 1;
        CurrentXp = 0;
        Difficulty = Difficulty.Normal;
    }

    public void MarkCheckpoint(string checkpointId)
    {
        CheckpointId = checkpointId ?? string.Empty;
    }

    public void MarkBossDefeated()
    {
        BossDefeated = true;
    }

    public void MarkStageComplete()
    {
        Stage1Completed = true;
        if (UnlockedStage < 2)
            UnlockedStage = 2;
    }

    public void MarkStage2Complete()
    {
        Stage2Completed = true;
        if (UnlockedStage < 3)
            UnlockedStage = 3;
    }

    public bool IsStageUnlocked(int stage) => stage <= UnlockedStage;
}

/// <summary>Satu entri inventory tersimpan: item ID + jumlah.</summary>
public sealed class InventoryEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

/// <summary>Equipment tersimpan: item ID per slot (null = kosong).</summary>
public sealed class EquipmentData
{
    [JsonPropertyName("weapon")]
    public string? Weapon { get; set; }

    [JsonPropertyName("armor")]
    public string? Armor { get; set; }

    [JsonPropertyName("accessory")]
    public string? Accessory { get; set; }
}
