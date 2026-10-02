using System.Text.Json;

namespace AshenOath;

/// <summary>
/// Persistence JSON sederhana (System.Text.Json, tanpa NuGet baru).
/// Aman: missing/empty/rusak -> fallback default, tidak pernah crash startup.
/// Tulis via temporary file + replace agar tidak setengah tertulis.
/// </summary>
public sealed class SaveManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string SavePath { get; }
    public string SaveDirectory => Path.GetDirectoryName(SavePath) ?? ".";

    public SaveManager(string? savePath = null)
    {
        SavePath = savePath ?? GetDefaultSavePath();
    }

    public static string GetDefaultSavePath()
    {
        string dir;
        string? xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        string? home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(xdg))
        {
            dir = Path.Combine(xdg, "AshenOath");
        }
        else if (!string.IsNullOrEmpty(home))
        {
            dir = Path.Combine(home, ".local", "share", "AshenOath");
        }
        else
        {
            try
            {
                dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AshenOath");
            }
            catch
            {
                dir = AppContext.BaseDirectory;
            }
        }
        return Path.Combine(dir, "save.json");
    }

    public bool Exists() => File.Exists(SavePath);

    public bool Delete()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Save] warning: delete failed ({ex.Message})");
            return false;
        }
    }

    public bool Save(GameProgress progress)
    {
        try
        {
            Directory.CreateDirectory(SaveDirectory);
            string json = JsonSerializer.Serialize(progress, Options);
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, SavePath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Save] warning: save failed ({ex.Message}). Gameplay tetap berjalan.");
            try
            {
                string tmp = SavePath + ".tmp";
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
            catch { /* abaikan cleanup */ }
            return false;
        }
    }

    /// <summary>
    /// Load aman: file tidak ada / kosong / rusak / field hilang /
    /// version tidak dikenal -> default, tidak pernah throw.
    /// </summary>
    public GameProgress Load()
    {
        try
        {
            if (!File.Exists(SavePath))
                return GameProgress.Default();
            string text = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(text))
                return GameProgress.Default();
            var loaded = JsonSerializer.Deserialize<GameProgress>(text, Options);
            if (loaded is null)
                return GameProgress.Default();
            return Normalize(loaded);
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("[Save] warning: save rusak, fallback New Game.");
            return GameProgress.Default();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Save] warning: load failed ({ex.Message}), fallback New Game.");
            return GameProgress.Default();
        }
    }

    internal static GameProgress Normalize(GameProgress p)
    {
        // Migrasi berantai: v1 -> v2 (existing), lalu default inventory/equipment/quest.
        if (p.Version == 1)
        {
            var migrated = GameProgress.Default();
            migrated.Version = GameProgress.CurrentVersion;
            migrated.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 2);
            migrated.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
            migrated.CheckpointId = p.CheckpointId ?? string.Empty;
            migrated.Stage1Completed = p.Stage1Completed;
            migrated.BossDefeated = p.BossDefeated;
            migrated.Stage2Completed = false;
            migrated.Inventory = new List<InventoryEntry>();
            migrated.Equipment = new EquipmentData();
            migrated.ActiveQuestId = null;
            migrated.QuestStatus = "NotStarted";
            migrated.ObjectiveProgress = 0;
            migrated.ClaimedQuestIds = new List<string>();
            migrated.Level = 1;
            migrated.CurrentXp = 0;
            migrated.Difficulty = Difficulty.Normal;
            if (migrated.Stage1Completed && migrated.UnlockedStage < 2)
                migrated.UnlockedStage = 2;
            return migrated;
        }
        // Migrasi v2 -> v3 -> v4 -> v5 -> v6: inventory/equipment/quest/progression kosong.
        if (p.Version == 2)
        {
            var migrated = GameProgress.Default();
            migrated.Version = GameProgress.CurrentVersion;
            migrated.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 2);
            migrated.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
            migrated.CheckpointId = p.CheckpointId ?? string.Empty;
            migrated.Stage1Completed = p.Stage1Completed;
            migrated.BossDefeated = p.BossDefeated;
            migrated.Stage2Completed = p.Stage2Completed;
            migrated.Inventory = new List<InventoryEntry>();
            migrated.Equipment = new EquipmentData();
            migrated.ActiveQuestId = null;
            migrated.QuestStatus = "NotStarted";
            migrated.ObjectiveProgress = 0;
            migrated.ClaimedQuestIds = new List<string>();
            migrated.Level = 1;
            migrated.CurrentXp = 0;
            migrated.Difficulty = Difficulty.Normal;
            if (migrated.Stage1Completed && migrated.UnlockedStage < 2)
                migrated.UnlockedStage = 2;
            if (migrated.Stage2Completed && migrated.UnlockedStage < 3)
                migrated.UnlockedStage = 3;
            return migrated;
        }
        // Migrasi v3 -> v4 -> v5 -> v6: quest/progression kosong, inventory/equipment dipertahankan.
        if (p.Version == 3)
        {
            var migrated = GameProgress.Default();
            migrated.Version = GameProgress.CurrentVersion;
            migrated.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 3);
            migrated.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
            migrated.CheckpointId = p.CheckpointId ?? string.Empty;
            migrated.Stage1Completed = p.Stage1Completed;
            migrated.BossDefeated = p.BossDefeated;
            migrated.Stage2Completed = p.Stage2Completed;
            migrated.Inventory = p.Inventory ?? new List<InventoryEntry>();
            migrated.Equipment = p.Equipment ?? new EquipmentData();
            migrated.ActiveQuestId = null;
            migrated.QuestStatus = "NotStarted";
            migrated.ObjectiveProgress = 0;
            migrated.ClaimedQuestIds = new List<string>();
            migrated.Level = 1;
            migrated.CurrentXp = 0;
            migrated.Difficulty = Difficulty.Normal;
            if (migrated.Stage1Completed && migrated.UnlockedStage < 2)
                migrated.UnlockedStage = 2;
            if (migrated.Stage2Completed && migrated.UnlockedStage < 3)
                migrated.UnlockedStage = 3;
            return migrated;
        }
        // Migrasi v4 -> v5 -> v6: progression default, sisanya dipertahankan.
        if (p.Version == 4)
        {
            var migrated = GameProgress.Default();
            migrated.Version = GameProgress.CurrentVersion;
            migrated.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 3);
            migrated.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
            migrated.CheckpointId = p.CheckpointId ?? string.Empty;
            migrated.Stage1Completed = p.Stage1Completed;
            migrated.BossDefeated = p.BossDefeated;
            migrated.Stage2Completed = p.Stage2Completed;
            migrated.Inventory = p.Inventory ?? new List<InventoryEntry>();
            migrated.Equipment = p.Equipment ?? new EquipmentData();
            migrated.ActiveQuestId = p.ActiveQuestId;
            migrated.QuestStatus = p.QuestStatus ?? "NotStarted";
            migrated.ObjectiveProgress = Math.Max(0, p.ObjectiveProgress);
            migrated.ClaimedQuestIds = p.ClaimedQuestIds ?? new List<string>();
            migrated.Level = 1;
            migrated.CurrentXp = 0;
            migrated.Difficulty = Difficulty.Normal;
            if (migrated.Stage1Completed && migrated.UnlockedStage < 2)
                migrated.UnlockedStage = 2;
            if (migrated.Stage2Completed && migrated.UnlockedStage < 3)
                migrated.UnlockedStage = 3;
            return migrated;
        }
        // Migrasi v5 -> v6 (Tahap 20): semua v5 dipertahankan, difficulty default Normal.
        if (p.Version == 5)
        {
            var migrated = GameProgress.Default();
            migrated.Version = GameProgress.CurrentVersion;
            migrated.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 3);
            migrated.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
            migrated.CheckpointId = p.CheckpointId ?? string.Empty;
            migrated.Stage1Completed = p.Stage1Completed;
            migrated.BossDefeated = p.BossDefeated;
            migrated.Stage2Completed = p.Stage2Completed;
            migrated.Inventory = p.Inventory ?? new List<InventoryEntry>();
            migrated.Equipment = p.Equipment ?? new EquipmentData();
            migrated.ActiveQuestId = p.ActiveQuestId;
            migrated.QuestStatus = p.QuestStatus ?? "NotStarted";
            migrated.ObjectiveProgress = Math.Max(0, p.ObjectiveProgress);
            migrated.ClaimedQuestIds = p.ClaimedQuestIds ?? new List<string>();
            var prog = new PlayerProgression();
            prog.SetLevelAndXp(p.Level, p.CurrentXp);
            migrated.Level = prog.Level;
            migrated.CurrentXp = prog.CurrentXP;
            migrated.Difficulty = Difficulty.Normal;
            if (migrated.Stage1Completed && migrated.UnlockedStage < 2)
                migrated.UnlockedStage = 2;
            if (migrated.Stage2Completed && migrated.UnlockedStage < 3)
                migrated.UnlockedStage = 3;
            return migrated;
        }
        // Version tidak dikenal -> jangan crash, fallback aman.
        if (p.Version != GameProgress.CurrentVersion)
        {
            Console.Error.WriteLine($"[Save] warning: version {p.Version} tidak dikenal, fallback default.");
            return GameProgress.Default();
        }
        var clean = GameProgress.Default();
        clean.UnlockedStage = p.UnlockedStage < 1 ? 1 : Math.Min(p.UnlockedStage, 3);
        clean.CurrentStage = p.CurrentStage < 1 ? 1 : Math.Min(p.CurrentStage, 2);
        clean.CheckpointId = p.CheckpointId ?? string.Empty;
        clean.Stage1Completed = p.Stage1Completed;
        clean.BossDefeated = p.BossDefeated;
        clean.Stage2Completed = p.Stage2Completed;
        clean.Inventory = p.Inventory ?? new List<InventoryEntry>();
        clean.Equipment = p.Equipment ?? new EquipmentData();
        clean.ActiveQuestId = p.ActiveQuestId;
        clean.QuestStatus = p.QuestStatus ?? "NotStarted";
        clean.ObjectiveProgress = Math.Max(0, p.ObjectiveProgress);
        clean.ClaimedQuestIds = p.ClaimedQuestIds ?? new List<string>();
        // Progression: clamp + normalisasi carry-over via logic yang sama.
        var progClean = new PlayerProgression();
        progClean.SetLevelAndXp(p.Level, p.CurrentXp);
        clean.Level = progClean.Level;
        clean.CurrentXp = progClean.CurrentXP;
        // Difficulty: unknown -> Normal (Tahap 20).
        clean.Difficulty = DifficultyModifiers.IsDefined(p.Difficulty) ? p.Difficulty : Difficulty.Normal;
        // Konsistensi: stage complete => minimal unlock berikutnya.
        if (clean.Stage1Completed && clean.UnlockedStage < 2)
            clean.UnlockedStage = 2;
        if (clean.Stage2Completed && clean.UnlockedStage < 3)
            clean.UnlockedStage = 3;
        return clean;
    }
}
