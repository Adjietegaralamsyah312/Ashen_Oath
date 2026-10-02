using SDL2;

namespace AshenOath;

/// <summary>
/// Menu quest Tahap 18 (tombol L): nama + deskripsi + progress + reward.
/// BitmapFont. Gameplay pause saat terbuka.
/// </summary>
public sealed class QuestMenu
{
    public void Render(IntPtr renderer, int windowWidth, int windowHeight, QuestManager quests)
    {
        SDL.SDL_Rect dim = new() { x = 0, y = 0, w = windowWidth, h = windowHeight };
        SDL.SDL_SetRenderDrawColor(renderer, 8, 8, 14, 220);
        SDL.SDL_RenderFillRect(renderer, ref dim);

        SDL.SDL_Rect panel = new() { x = windowWidth / 2 - 320, y = 100, w = 640, h = 300 };
        SDL.SDL_SetRenderDrawColor(renderer, 14, 14, 24, 255);
        SDL.SDL_RenderFillRect(renderer, ref panel);
        SDL.SDL_SetRenderDrawColor(renderer, 150, 170, 220, 255);
        SDL.SDL_RenderDrawRect(renderer, ref panel);

        BitmapFont.DrawTextCentered(renderer, "QUESTS", windowWidth / 2, 120);

        var quest = quests.ActiveQuest() ?? quests.GetQuest(QuestManager.RoadCleansingId);
        if (quest is null)
        {
            BitmapFont.DrawTextCentered(renderer, "NO QUESTS", windowWidth / 2, 200);
            return;
        }

        int x = windowWidth / 2 - 280;
        BitmapFont.DrawText(renderer, quest.Name.ToUpperInvariant(), x, 160);
        BitmapFont.DrawText(renderer, quest.Description.ToUpperInvariant(), x, 190, 2, 170, 180, 190);

        string progress = quest.Status switch
        {
            QuestStatus.NotStarted => "NOT STARTED",
            QuestStatus.Claimed => "CLAIMED",
            _ => $"{quest.Objective.CurrentAmount} / {quest.Objective.RequiredAmount}",
        };
        BitmapFont.DrawText(renderer, progress, x, 225, 2, 240, 220, 130);

        if (quest.Status == QuestStatus.Completed)
            BitmapFont.DrawText(renderer, "RETURN TO ELDER ROWAN", x, 250, 2, 140, 220, 140);

        BitmapFont.DrawText(renderer, "REWARD: 30 ASH SHARDS", x, 285, 2, 170, 180, 190);
        BitmapFont.DrawText(renderer, "SMALL POTION X1", x + 420, 285, 2, 170, 180, 190);
        BitmapFont.DrawTextCentered(renderer, "ESC CLOSE", windowWidth / 2, 360, 2, 150, 150, 165);
    }
}
