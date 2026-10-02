using SDL2;

namespace AshenOath;

/// <summary>
/// Stage Select Tahap 16: Stage 1 Road of Ash + Stage 2 The Hollow Forge
/// + Stage 3 Locked (belum diimplementasikan, tidak bisa dipilih).
/// </summary>
public sealed class StageSelect
{
    public enum SelectAction
    {
        None,
        Back,
        PlayStage1,
        PlayStage2,
        Stage2NotImplemented,
    }

    public int SelectedIndex { get; private set; } // 0 = Stage1, 1 = Stage2, 2 = Stage3
    public bool ShowNotImplemented { get; private set; }

    public int ItemCount => 3;

    public void Reset()
    {
        SelectedIndex = 0;
        ShowNotImplemented = false;
    }

    public void MoveUp() => Move(-1);
    public void MoveDown() => Move(1);

    private void Move(int dir)
    {
        ShowNotImplemented = false;
        SelectedIndex = (SelectedIndex + dir + ItemCount) % ItemCount;
    }

    public bool IsUnlocked(int index, GameProgress progress)
    {
        return index switch
        {
            0 => true,
            1 => progress.IsStageUnlocked(2),
            _ => false, // Stage 3 selalu locked.
        };
    }

    public SelectAction Activate(GameProgress progress)
    {
        if (SelectedIndex == 0)
        {
            ShowNotImplemented = false;
            return SelectAction.PlayStage1;
        }
        if (SelectedIndex == 1)
        {
            if (!IsUnlocked(1, progress))
            {
                ShowNotImplemented = false;
                return SelectAction.None; // locked: tidak boleh dipilih
            }
            ShowNotImplemented = false;
            return SelectAction.PlayStage2;
        }
        ShowNotImplemented = false;
        return SelectAction.None; // Stage 3 locked.
    }

    public void DismissNotImplemented() => ShowNotImplemented = false;

    public void Render(IntPtr renderer, int windowWidth, int windowHeight, GameProgress progress)
    {
        SDL.SDL_SetRenderDrawColor(renderer, 20, 20, 32, 255);
        SDL.SDL_Rect bg = new() { x = 0, y = 0, w = windowWidth, h = windowHeight };
        SDL.SDL_RenderFillRect(renderer, ref bg);

        SDL.SDL_Rect title = new() { x = windowWidth / 2 - 180, y = 60, w = 360, h = 32 };
        SDL.SDL_SetRenderDrawColor(renderer, 150, 170, 220, 255);
        SDL.SDL_RenderFillRect(renderer, ref title);
        BitmapFont.DrawTextCentered(renderer, "STAGE SELECT", windowWidth / 2, 66, 2, 20, 20, 30);

        string[] labels = { "1 ROAD OF ASH", "2 THE HOLLOW FORGE", "3 LOCKED" };
        for (int i = 0; i < ItemCount; i++)
        {
            int x = windowWidth / 2 - 200;
            int y = 130 + i * 64;
            SDL.SDL_Rect row = new() { x = x, y = y, w = 400, h = 48 };
            bool selected = i == SelectedIndex;
            bool unlocked = IsUnlocked(i, progress);
            if (!unlocked)
                SDL.SDL_SetRenderDrawColor(renderer, 55, 55, 65, 255);
            else if (selected)
                SDL.SDL_SetRenderDrawColor(renderer, 90, 140, 200, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 45, 45, 60, 255);
            SDL.SDL_RenderFillRect(renderer, ref row);
            if (selected)
            {
                SDL.SDL_SetRenderDrawColor(renderer, 240, 220, 130, 255);
                SDL.SDL_RenderDrawRect(renderer, ref row);
            }
            // Status pip kanan: hijau unlocked, abu locked.
            SDL.SDL_Rect pip = new() { x = x + 400 + 12, y = y + 14, w = 20, h = 20 };
            if (unlocked)
                SDL.SDL_SetRenderDrawColor(renderer, 80, 200, 110, 255);
            else
                SDL.SDL_SetRenderDrawColor(renderer, 90, 90, 100, 255);
            SDL.SDL_RenderFillRect(renderer, ref pip);

            byte lr = 235, lg = 235, lb = 240;
            if (!unlocked) { lr = 120; lg = 120; lb = 130; }
            BitmapFont.DrawText(renderer, labels[i], x + 16, y + 17, 2, lr, lg, lb);
        }

        if (ShowNotImplemented)
        {
            SDL.SDL_Rect panel = new() { x = windowWidth / 2 - 180, y = 360, w = 360, h = 60 };
            SDL.SDL_SetRenderDrawColor(renderer, 12, 12, 20, 255);
            SDL.SDL_RenderFillRect(renderer, ref panel);
            SDL.SDL_SetRenderDrawColor(renderer, 240, 200, 80, 255);
            SDL.SDL_RenderDrawRect(renderer, ref panel);
            SDL.SDL_Rect bar = new() { x = windowWidth / 2 - 120, y = 385, w = 240, h = 12 };
            SDL.SDL_SetRenderDrawColor(renderer, 200, 120, 80, 255);
            SDL.SDL_RenderFillRect(renderer, ref bar);
        }
    }
}
