using SDL2;

namespace AshenOath;

/// <summary>
/// Main Menu Tahap 15: New Game / Continue / Stage Select / Reset / Quit.
/// Rectangle-only tanpa font dependency. Navigasi Up/Down + Enter + Escape.
/// Reset memakai konfirmasi Yes/No satu langkah.
/// Tahap 20: difficulty selection Normal/Hard via Left/Right (tanpa state tree baru).
/// </summary>
public sealed class MainMenu
{
    public enum MenuAction
    {
        None,
        NewGame,
        Continue,
        StageSelect,
        ResetProgress,
        Quit,
    }

    private static readonly string[] ItemNames =
    {
        "New Game",
        "Continue",
        "Stage Select",
        "Reset Progress",
        "Quit",
    };

    public int SelectedIndex { get; private set; }
    public int ItemCount => ItemNames.Length;
    public bool IsConfirmingReset { get; private set; }
    public int ConfirmIndex { get; private set; } // 0 = Yes, 1 = No

    /// <summary>Continue aktif hanya bila save valid ada.</summary>
    public bool ContinueEnabled { get; private set; }

    /// <summary>Difficulty pilihan untuk New Game (Tahap 20). Default Normal.</summary>
    public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Normal;

    public void Refresh(bool hasSave)
    {
        ContinueEnabled = hasSave;
        if (SelectedIndex < 0) SelectedIndex = 0;
        if (SelectedIndex >= ItemCount) SelectedIndex = 0;
    }

    public void MoveUp()
    {
        if (IsConfirmingReset)
        {
            ConfirmIndex = (ConfirmIndex + 1) % 2;
            return;
        }
        SelectedIndex = (SelectedIndex - 1 + ItemCount) % ItemCount;
    }

    public void MoveDown()
    {
        if (IsConfirmingReset)
        {
            ConfirmIndex = (ConfirmIndex + 1) % 2;
            return;
        }
        SelectedIndex = (SelectedIndex + 1) % ItemCount;
    }

    /// <summary>
    /// Left/Right Tahap 20: toggle difficulty Normal/Hard.
    /// Saat konfirmasi reset, Left/Right menggeser Yes/No seperti Up/Down.
    /// </summary>
    public void MoveLeft()
    {
        if (IsConfirmingReset)
        {
            ConfirmIndex = (ConfirmIndex + 1) % 2;
            return;
        }
        ToggleDifficulty();
    }

    public void MoveRight()
    {
        if (IsConfirmingReset)
        {
            ConfirmIndex = (ConfirmIndex + 1) % 2;
            return;
        }
        ToggleDifficulty();
    }

    public void ToggleDifficulty()
    {
        SelectedDifficulty = SelectedDifficulty == Difficulty.Normal ? Difficulty.Hard : Difficulty.Normal;
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        SelectedDifficulty = DifficultyModifiers.IsDefined(difficulty) ? difficulty : Difficulty.Normal;
    }

    public MenuAction Activate()
    {
        if (IsConfirmingReset)
        {
            bool yes = ConfirmIndex == 0;
            IsConfirmingReset = false;
            ConfirmIndex = 1;
            return yes ? MenuAction.ResetProgress : MenuAction.None;
        }
        return SelectedIndex switch
        {
            0 => MenuAction.NewGame,
            1 => ContinueEnabled ? MenuAction.Continue : MenuAction.None,
            2 => MenuAction.StageSelect,
            3 => BeginConfirmReset(),
            4 => MenuAction.Quit,
            _ => MenuAction.None,
        };
    }

    private MenuAction BeginConfirmReset()
    {
        IsConfirmingReset = true;
        ConfirmIndex = 1; // default No agar aman
        return MenuAction.None;
    }

    public void Cancel()
    {
        if (IsConfirmingReset)
        {
            IsConfirmingReset = false;
            ConfirmIndex = 1;
        }
    }

    public bool IsSelectedContinueDisabled() => SelectedIndex == 1 && !ContinueEnabled;

    public void Render(IntPtr renderer, int windowWidth, int windowHeight, GameProgress progress)
    {
        SDL.SDL_SetRenderDrawColor(renderer, 18, 18, 28, 255);
        SDL.SDL_Rect bg = new() { x = 0, y = 0, w = windowWidth, h = windowHeight };
        SDL.SDL_RenderFillRect(renderer, ref bg);

        // Title area (rectangle emas).
        SDL.SDL_Rect title = new() { x = windowWidth / 2 - 200, y = 70, w = 400, h = 40 };
        SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref title);
        BitmapFont.DrawTextCentered(renderer, "ASHEN OATH", windowWidth / 2, 78, 3, 20, 20, 30);

        int startY = 160;
        const int btnW = 320;
        const int btnH = 40;
        const int gap = 12;
        for (int i = 0; i < ItemCount; i++)
        {
            int x = windowWidth / 2 - btnW / 2;
            int y = startY + i * (btnH + gap);
            SDL.SDL_Rect btn = new() { x = x, y = y, w = btnW, h = btnH };
            bool selected = i == SelectedIndex && !IsConfirmingReset;
            bool disabled = i == 1 && !ContinueEnabled;
            if (disabled)
                SDL.SDL_SetRenderDrawColor(renderer, 60, 60, 70, 255);
            else if (selected)
                SDL.SDL_SetRenderDrawColor(renderer, 90, 140, 200, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 45, 45, 60, 255);
            SDL.SDL_RenderFillRect(renderer, ref btn);

            if (selected)
            {
                SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
                SDL.SDL_RenderDrawRect(renderer, ref btn);
                // Cursor kotak di kiri.
                SDL.SDL_Rect cursor = new() { x = x - 24, y = y + 12, w = 14, h = 14 };
                SDL.SDL_RenderFillRect(renderer, ref cursor);
            }

            bool dim = disabled;
            byte tr = 235, tg = 235, tb = 240;
            if (dim) { tr = 110; tg = 110; tb = 120; }
            int tw = BitmapFont.MeasureText(ItemNames[i]);
            BitmapFont.DrawText(renderer, ItemNames[i].ToUpperInvariant(), x + (btnW - tw) / 2, y + 13, 2, tr, tg, tb);
        }

        BitmapFont.DrawTextCentered(renderer, "UP/DOWN ENTER - ESC BACK", windowWidth / 2, windowHeight - 30, 2, 140, 140, 155);

        // Tahap 20: difficulty selection via Left/Right (BitmapFont, tanpa state tree baru).
        string diffName = SelectedDifficulty == Difficulty.Hard ? "HARD" : "NORMAL";
        (byte dr, byte dg, byte db) = SelectedDifficulty == Difficulty.Hard
            ? ((byte)230, (byte)110, (byte)90)
            : ((byte)150, (byte)200, (byte)150);
        BitmapFont.DrawTextCentered(renderer, $"< DIFFICULTY: {diffName} >", windowWidth / 2, startY + ItemCount * (btnH + gap) - 4, 2, dr, dg, db);

        // Progress indicators sederhana (tanpa teks):
        // kotak hijau = stage1 completed, merah = boss defeated, biru = stage2 unlocked.
        int indY = startY + ItemCount * (btnH + gap) + 16;
        int indX = windowWidth / 2 - 60;
        SDL.SDL_Rect s1 = new() { x = indX, y = indY, w = 20, h = 20 };
        SDL.SDL_SetRenderDrawColor(renderer, progress.Stage1Completed ? (byte)80 : (byte)50, progress.Stage1Completed ? (byte)200 : (byte)50, progress.Stage1Completed ? (byte)110 : (byte)60, 255);
        SDL.SDL_RenderFillRect(renderer, ref s1);
        SDL.SDL_Rect bd = new() { x = indX + 30, y = indY, w = 20, h = 20 };
        SDL.SDL_SetRenderDrawColor(renderer, progress.BossDefeated ? (byte)210 : (byte)60, progress.BossDefeated ? (byte)70 : (byte)60, progress.BossDefeated ? (byte)70 : (byte)70, 255);
        SDL.SDL_RenderFillRect(renderer, ref bd);
        SDL.SDL_Rect s2 = new() { x = indX + 60, y = indY, w = 20, h = 20 };
        bool s2u = progress.UnlockedStage >= 2;
        SDL.SDL_SetRenderDrawColor(renderer, s2u ? (byte)90 : (byte)55, s2u ? (byte)170 : (byte)55, s2u ? (byte)220 : (byte)65, 255);
        SDL.SDL_RenderFillRect(renderer, ref s2);

        if (IsConfirmingReset)
        {
            SDL.SDL_Rect panel = new() { x = windowWidth / 2 - 180, y = 200, w = 360, h = 140 };
            SDL.SDL_SetRenderDrawColor(renderer, 12, 12, 20, 255);
            SDL.SDL_RenderFillRect(renderer, ref panel);
            SDL.SDL_SetRenderDrawColor(renderer, 240, 120, 90, 255);
            SDL.SDL_RenderDrawRect(renderer, ref panel);
            BitmapFont.DrawTextCentered(renderer, "RESET PROGRESS?", windowWidth / 2, 220);
            // Yes / No boxes.
            SDL.SDL_Rect yes = new() { x = windowWidth / 2 - 140, y = 280, w = 120, h = 36 };
            SDL.SDL_Rect no = new() { x = windowWidth / 2 + 20, y = 280, w = 120, h = 36 };
            SDL.SDL_SetRenderDrawColor(renderer, ConfirmIndex == 0 ? (byte)200 : (byte)70, ConfirmIndex == 0 ? (byte)90 : (byte)70, ConfirmIndex == 0 ? (byte)80 : (byte)75, 255);
            SDL.SDL_RenderFillRect(renderer, ref yes);
            SDL.SDL_SetRenderDrawColor(renderer, ConfirmIndex == 1 ? (byte)120 : (byte)70, ConfirmIndex == 1 ? (byte)190 : (byte)70, ConfirmIndex == 1 ? (byte)110 : (byte)75, 255);
            SDL.SDL_RenderFillRect(renderer, ref no);
            BitmapFont.DrawTextCentered(renderer, "YES", windowWidth / 2 - 80, 290);
            BitmapFont.DrawTextCentered(renderer, "NO", windowWidth / 2 + 80, 290);
        }
    }
}
