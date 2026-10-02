namespace AshenOath;

/// <summary>
/// State global Tahap 15. Completed dipertahankan sebagai alias
/// StageComplete agar Stage Complete existing tetap bekerja.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Inventory,
    Dialogue,
    Shop,
    Quest,
    StageSelect,
    StageComplete,
    Completed = StageComplete,
    GameOver,
}
