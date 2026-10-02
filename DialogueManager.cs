namespace AshenOath;

/// <summary>
/// Manager dialogue Tahap 18: Open -> lines -> Enter -> close.
/// Tanpa nested loop; Game memanggil Advance per input.
/// </summary>
public sealed class DialogueManager
{
    public Dialogue? Current { get; private set; }
    public bool IsOpen => Current is not null;

    public void Open(Dialogue dialogue)
    {
        dialogue.Reset();
        Current = dialogue;
    }

    public void Close() => Current = null;

    /// <summary>
    /// Enter/F: baris berikut, atau tutup (+ kembalikan aksi akhir).
    /// Escape menutup tanpa aksi.
    /// </summary>
    public DialogueEndAction Advance()
    {
        if (Current is null)
            return DialogueEndAction.None;
        if (!Current.IsLastLine)
        {
            Current.Advance();
            return DialogueEndAction.None;
        }
        var action = Current.EndAction;
        Close();
        return action;
    }

    public void Cancel() => Close();
}

/// <summary>Konten dialogue statis (cerita pendek, tanpa quest logic).</summary>
public static class DialogueContent
{
    public static Dialogue ElderRowan(QuestStatus questStatus) => questStatus switch
    {
        QuestStatus.NotStarted => new Dialogue("ELDER ROWAN", new[]
        {
            "Stranger, the ashes are spreading.",
            "The road beyond the ruins is no longer safe.",
            "Defeat the creatures near the old road.",
            "Return when you have proven yourself.",
        }, DialogueEndAction.AcceptQuest, questId: QuestManager.RoadCleansingId),
        QuestStatus.Active => new Dialogue("ELDER ROWAN", new[]
        {
            "The ashes still stir.",
            "Return when the road is cleansed.",
        }),
        QuestStatus.Completed => new Dialogue("ELDER ROWAN", new[]
        {
            "You have proven yourself.",
            "Take these, stranger.",
        }, DialogueEndAction.ClaimReward, questId: QuestManager.RoadCleansingId),
        _ => new Dialogue("ELDER ROWAN", new[]
        {
            "Walk safely, stranger.",
        }),
    };

    public static Dialogue ForgeKeeper(bool questDone, string? shopId) => questDone
        ? new Dialogue("FORGE KEEPER", new[]
        {
            "The forge answers those who survive.",
        }, DialogueEndAction.OpenShop, shopId: shopId)
        : new Dialogue("FORGE KEEPER", new[]
        {
            "The Hollow Forge still burns.",
            "Steel may help where courage fails.",
        }, DialogueEndAction.OpenShop, shopId: shopId);
}
