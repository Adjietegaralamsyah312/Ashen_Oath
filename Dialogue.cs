namespace AshenOath;

/// <summary>Aksi akhir dialogue (dipicu Enter pada baris terakhir).</summary>
public enum DialogueEndAction
{
    None,
    AcceptQuest,
    ClaimReward,
    OpenShop,
}

/// <summary>
/// Satu sesi dialogue: speaker + lines + aksi akhir opsional.
/// </summary>
public sealed class Dialogue
{
    public string Speaker { get; }
    public List<string> Lines { get; } = new();
    public int CurrentIndex { get; private set; }
    public DialogueEndAction EndAction { get; }
    public string? QuestId { get; }
    public string? ShopId { get; }

    public Dialogue(string speaker, IEnumerable<string> lines,
        DialogueEndAction endAction = DialogueEndAction.None,
        string? questId = null, string? shopId = null)
    {
        Speaker = speaker;
        Lines.AddRange(lines);
        if (Lines.Count == 0)
            Lines.Add("...");
        EndAction = endAction;
        QuestId = questId;
        ShopId = shopId;
    }

    public string CurrentLine => Lines[Math.Clamp(CurrentIndex, 0, Lines.Count - 1)];
    public bool IsLastLine => CurrentIndex >= Lines.Count - 1;

    public void Advance()
    {
        if (CurrentIndex < Lines.Count - 1)
            CurrentIndex++;
    }

    public void Reset() => CurrentIndex = 0;
}
