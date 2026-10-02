using SDL2;

namespace AshenOath;

/// <summary>
/// Pemilik window/renderer dan game loop utama (satu loop, tanpa nesting).
/// Tahap 15: MainMenu -> New/Continue -> Stage -> Save/Load -> Select -> Pause.
/// Gameplay Tahap 1-14 dipertahankan.
/// </summary>
public sealed class Game : IDisposable
{
    private const string WindowTitle = "Ashen Oath";
    private const int WindowWidth = 960;
    private const int WindowHeight = 540;

    /// <summary>True untuk menggambar hitbox attack (debug saja).</summary>
    private bool _debugShowHitboxes = false;

    /// <summary>True untuk menggambar detection radius + state enemy (debug saja).</summary>
    private bool _debugShowEnemyAI = false;

    /// <summary>True untuk menggambar hazard bounds + moving platform path + falling platform state (debug saja).</summary>
    private bool _debugShowEnvironment = false;

    private IntPtr _window = IntPtr.Zero;
    private IntPtr _renderer = IntPtr.Zero;
    private bool _disposed;

    private readonly Input _input = new();
    private readonly AudioManager _audio = new();
    private readonly SaveManager _save;
    private GameProgress _progress;
    private readonly MainMenu _menu = new();
    private readonly StageSelect _stageSelect = new();

    private Stage _stage;
    private World _world;
    private Camera _camera;
    private readonly GameFeel _feel = new();
    private Player _player;
    private List<Enemy> _enemies;
    private List<Checkpoint> _checkpoints;
    private readonly List<Projectile> _projectiles = new();
    private List<Spike> _spikes;
    private List<HazardZone> _hazardZones;
    private List<MovingPlatform> _movingPlatforms;
    private List<FallingPlatform> _fallingPlatforms;
    private AshenWarden? _boss;
    private readonly List<BossProjectile> _bossProjectiles = new();
    private readonly List<LootDrop> _loot = new();
    private readonly InventoryMenu _invMenu = new();
    private readonly IRng _rng = new SystemRng();
    private bool _bossRewardGiven;
    private bool _bossXpGiven;
    private List<Npc> _npcs;
    private readonly QuestManager _questManager = new();
    private readonly DialogueManager _dialogue = new();
    private Npc? _dialogueNpc;
    private readonly Shop _forgeShop = Shop.CreateForgeShop();
    private readonly ShopMenu _shopMenu = new();
    private readonly QuestMenu _questMenu = new();
    private bool _bossMusicPlaying;
    private bool _goalBlockedLogged;
    private bool _savePathLogged;
    private float _respawnX;
    private float _respawnY;
    private GameState _state = GameState.MainMenu;
    private bool _running;

    public GameState State => _state;
    public GameProgress Progress => _progress;
    public string SavePath => _save.SavePath;
    public MainMenu Menu => _menu;
    public Difficulty CurrentDifficulty => _progress.Difficulty;
    public GameFeel Feel => _feel;
    public Camera Camera => _camera;
    public StageSelect StageSelector => _stageSelect;
    public IReadOnlyList<LootDrop> Loot => _loot;
    public InventoryMenu InventoryMenu => _invMenu;
    public IReadOnlyList<Npc> Npcs => _npcs;
    public QuestManager Quests => _questManager;
    public DialogueManager Dialogue => _dialogue;
    public ShopMenu ShopMenu => _shopMenu;

    public Game(string? savePath = null)
    {
        _save = new SaveManager(savePath);
        _progress = GameProgress.Default();
        _stage = Stage.CreateStage1();
        _world = new World(_stage);
        _camera = new Camera(WindowWidth, WindowHeight, _world.Width, _world.Height);
        _player = new Player(_world.SpawnX, _world.SpawnY);
        _enemies = new List<Enemy>(_stage.Enemies);
        _checkpoints = new List<Checkpoint>(_stage.Checkpoints);
        _spikes = new List<Spike>(_stage.Spikes);
        _hazardZones = new List<HazardZone>(_stage.HazardZones);
        _movingPlatforms = new List<MovingPlatform>(_stage.MovingPlatforms);
        _fallingPlatforms = new List<FallingPlatform>(_stage.FallingPlatforms);
        _npcs = new List<Npc>(_stage.Npcs);
        _respawnX = _world.SpawnX;
        _respawnY = _world.SpawnY;
        _camera.Follow(_player.X + Player.Width / 2f, _player.Y + Player.Height / 2f);
        BindBoss();
    }

    private void BindBoss()
    {
        _boss = null;
        foreach (var enemy in _enemies)
        {
            if (enemy is AshenWarden w)
            {
                _boss = w;
                break;
            }
        }
        if (_boss is not null && _stage.HasBossArena)
            _boss.ArenaBounds = _stage.BossArena;
    }

    public int Run()
    {
        if (!Init())
            return 1;

        Console.WriteLine($"Ashen Oath [{_stage.Name}] opened. Menu: Up/Down+Enter, Game: A/D move, Space jump, J attack, Esc pause.");
        LogSavePathOnce();

        // Load progress awal (fallback aman bila rusak).
        _progress = _save.Load();
        _menu.Refresh(_save.Exists());

        ulong frequency = SDL.SDL_GetPerformanceFrequency();
        ulong last = SDL.SDL_GetPerformanceCounter();
        _running = true;

        while (_running)
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
            {
                if (e.type == SDL.SDL_EventType.SDL_QUIT)
                {
                    _running = false;
                }
                else
                {
                    _input.HandleEvent(in e);
                }
            }

            ulong now = SDL.SDL_GetPerformanceCounter();
            float deltaTime = (float)((now - last) / (double)frequency);
            last = now;

            // Clamp agar tidak lompat jauh setelah hitch/breakpoint.
            if (deltaTime > 0.05f)
                deltaTime = 0.05f;
            if (deltaTime < 0f)
                deltaTime = 0f;

            Update(deltaTime);
            Render();

            // Yield CPU secukupnya; movement tetap berbasis deltaTime.
            SDL.SDL_Delay(1);
        }

        Cleanup();
        return 0;
    }

    private void LogSavePathOnce()
    {
        if (_savePathLogged)
            return;
        _savePathLogged = true;
        Console.WriteLine($"[Save] path: {_save.SavePath}");
    }

    private void Update(float deltaTime)
    {
        // Consume semua intent tiap frame agar tidak stale;
        // tiap state hanya memakai set miliknya (separasi input).
        var (dirX, _) = _input.GetDirection();
        bool jumpPressed = _input.ConsumeJumpPressed();
        bool attackPressed = _input.ConsumeAttackPressed();
        bool skillPressed = _input.ConsumeSkillPressed();
        bool bashPressed = _input.ConsumeBashPressed();
        bool firePressed = _input.ConsumeFirePressed();
        bool menuUp = _input.ConsumeMenuUp();
        bool menuDown = _input.ConsumeMenuDown();
        bool menuLeft = _input.ConsumeMenuLeft();
        bool menuRight = _input.ConsumeMenuRight();
        bool menuConfirm = _input.ConsumeMenuConfirm();
        bool menuBack = _input.ConsumeMenuBack();
        bool potionPressed = _input.ConsumePotionPressed();
        bool inventoryPressed = _input.ConsumeInventoryPressed();
        bool interactPressed = _input.ConsumeInteractPressed();
        bool questPressed = _input.ConsumeQuestPressed();
        bool shopPressed = _input.ConsumeShopPressed();

        if (_state == GameState.MainMenu)
        {
            UpdateMainMenu(menuUp, menuDown, menuLeft, menuRight, menuConfirm, menuBack);
        }
        else if (_state == GameState.StageSelect)
        {
            UpdateStageSelect(menuUp, menuDown, menuConfirm, menuBack);
        }
        else if (_state == GameState.Playing)
        {
            if (menuBack)
            {
                PauseGame();
                return;
            }
            if (inventoryPressed)
            {
                OpenInventory();
                return;
            }
            if (questPressed)
            {
                OpenQuestMenu();
                return;
            }
            if (shopPressed)
            {
                if (TryOpenShopDirect())
                    return;
            }
            if (interactPressed)
            {
                if (TryInteract())
                    return;
            }
            if (potionPressed)
                DrinkPotion();
            UpdateGameplay(deltaTime, dirX, jumpPressed, attackPressed, skillPressed, bashPressed, firePressed);
        }
        else if (_state == GameState.Paused)
        {
            if (menuBack || menuConfirm)
                ResumeGame();
            // Update gameplay diblokir total saat pause.
        }
        else if (_state == GameState.Inventory)
        {
            UpdateInventory(menuUp, menuDown, menuLeft, menuRight, menuConfirm, menuBack, inventoryPressed);
            // Update gameplay diblokir total saat inventory terbuka.
        }
        else if (_state == GameState.Dialogue)
        {
            UpdateDialogue(menuConfirm || interactPressed, menuBack);
            // Update gameplay diblokir total saat dialogue terbuka.
        }
        else if (_state == GameState.Shop)
        {
            UpdateShop(menuUp, menuDown, menuConfirm, menuBack);
            // Update gameplay diblokir total saat shop terbuka.
        }
        else if (_state == GameState.Quest)
        {
            if (menuBack || questPressed)
            {
                _state = GameState.Playing;
                Console.WriteLine("[Game] Quest menu closed.");
            }
            // Update gameplay diblokir total saat quest menu terbuka.
        }
        else if (_state == GameState.StageComplete)
        {
            if (menuConfirm || menuBack)
            {
                _stageSelect.Reset();
                _state = GameState.StageSelect;
                _menu.Refresh(_save.Exists());
            }
        }
        else if (_state == GameState.GameOver)
        {
            if (menuConfirm || menuBack)
                _state = GameState.MainMenu;
        }
    }

    // ---------- Menu ----------

    private void UpdateMainMenu(bool up, bool down, bool left, bool right, bool confirm, bool back)
    {
        _menu.Refresh(_save.Exists());
        if (up) _menu.MoveUp();
        if (down) _menu.MoveDown();
        if (left) _menu.MoveLeft();
        if (right) _menu.MoveRight();
        if (back) _menu.Cancel();
        if (!confirm)
            return;
        var action = _menu.Activate();
        switch (action)
        {
            case MainMenu.MenuAction.NewGame:
                _audio.PlaySfx("checkpoint");
                StartNewGame();
                break;
            case MainMenu.MenuAction.Continue:
                _audio.PlaySfx("checkpoint");
                ContinueSave();
                break;
            case MainMenu.MenuAction.StageSelect:
                _audio.PlaySfx("checkpoint");
                _stageSelect.Reset();
                _state = GameState.StageSelect;
                break;
            case MainMenu.MenuAction.ResetProgress:
                _audio.PlaySfx("checkpoint");
                ResetProgress();
                break;
            case MainMenu.MenuAction.Quit:
                _running = false;
                break;
        }
    }

    private void UpdateStageSelect(bool up, bool down, bool confirm, bool back)
    {
        if (back)
        {
            _stageSelect.DismissNotImplemented();
            _state = GameState.MainMenu;
            _menu.Refresh(_save.Exists());
            return;
        }
        if (up) _stageSelect.MoveUp();
        if (down) _stageSelect.MoveDown();
        if (!confirm)
            return;
        var action = _stageSelect.Activate(_progress);
        switch (action)
        {
            case StageSelect.SelectAction.PlayStage1:
                _audio.PlaySfx("checkpoint");
                TrySave();
                LoadStage(1, _progress.CurrentStage == 1 ? _progress.CheckpointId : string.Empty);
                _progress.CurrentStage = 1;
                _state = GameState.Playing;
                StartStageMusic(1);
                break;
            case StageSelect.SelectAction.PlayStage2:
                _audio.PlaySfx("checkpoint");
                TrySave();
                LoadStage(2, _progress.CurrentStage == 2 ? _progress.CheckpointId : string.Empty);
                _progress.CurrentStage = 2;
                _state = GameState.Playing;
                StartStageMusic(2);
                break;
            case StageSelect.SelectAction.Stage2NotImplemented:
                _audio.PlaySfx("checkpoint");
                Console.WriteLine("[Game] Stage 2 not implemented.");
                break;
        }
    }

    // ---------- Lifecycle ----------

    private void StartNewGame()
    {
        _progress.ResetToNewGame();
        _progress.Difficulty = DifficultyModifiers.IsDefined(_menu.SelectedDifficulty)
            ? _menu.SelectedDifficulty
            : Difficulty.Normal;
        _questManager.Reset();
        LoadStage(1, string.Empty);
        GiveStarterEquipment();
        TrySave();
        _state = GameState.Playing;
        StartStageMusic(1);
        Console.WriteLine($"[Game] New Game: Stage 1 (rusted_blade equipped, {_progress.Difficulty}).");
    }

    private void ContinueSave()
    {
        _progress = _save.Load();
        int stage = _progress.CurrentStage < 1 ? 1 : Math.Min(_progress.CurrentStage, 2);
        if (stage == 2 && !_progress.IsStageUnlocked(2))
            stage = 1;
        LoadStage(stage, _progress.CheckpointId);
        _progress.CurrentStage = stage;
        _state = GameState.Playing;
        StartStageMusic(stage);
        Console.WriteLine($"[Game] Continue: Stage {stage} ({_progress.Difficulty}).");
    }

    private void ResetProgress()
    {
        _save.Delete();
        _progress.ResetToNewGame();
        _menu.SetDifficulty(Difficulty.Normal);
        _questManager.Reset();
        _player.Inventory.Clear();
        _player.Equipment.Clear();
        _player.Progression.Reset();
        _player.ClampStatsToEffective();
        _feel.Clear();
        _camera.ClearShake();
        _menu.Refresh(false);
        Console.WriteLine("[Game] Progress reset.");
    }

    private void PauseGame()
    {
        _state = GameState.Paused;
        _audio.PauseMusic();
        Console.WriteLine("[Game] Paused.");
    }

    private void ResumeGame()
    {
        _state = GameState.Playing;
        _audio.ResumeMusic();
        Console.WriteLine("[Game] Resumed.");
    }

    // ---------- Inventory (Tahap 17) ----------

    private void OpenInventory()
    {
        _invMenu.Reset();
        _state = GameState.Inventory;
        Console.WriteLine("[Game] Inventory opened.");
    }

    private void CloseInventory()
    {
        _state = GameState.Playing;
        Console.WriteLine("[Game] Inventory closed.");
    }

    private void UpdateInventory(bool up, bool down, bool left, bool right, bool confirm, bool back, bool toggle)
    {
        if (back || toggle)
        {
            CloseInventory();
            return;
        }
        if (up) _invMenu.MoveUp();
        if (down) _invMenu.MoveDown();
        if (left) _invMenu.MoveLeft();
        if (right) _invMenu.MoveRight();
        if (!confirm)
            return;
        var action = _invMenu.Activate(_player);
        switch (action)
        {
            case InventoryMenu.InventoryAction.Equipped:
                _audio.PlaySfx("equip");
                break;
            case InventoryMenu.InventoryAction.UsedPotion:
                _audio.PlaySfx("potion");
                break;
        }
    }

    private void DrinkPotion()
    {
        if (_player.TryDrinkPotion())
        {
            _audio.PlaySfx("potion");
            Console.WriteLine("[Game] Potion used (+25 HP).");
        }
    }

    // ---------- NPC / Dialogue / Shop / Quest (Tahap 18) ----------

    private Npc? FindNearNpc()
    {
        foreach (var npc in _npcs)
        {
            if (npc.IsPlayerNear(_player))
                return npc;
        }
        return null;
    }

    /// <summary>F: buka dialogue bila dekat NPC. False bila tidak ada NPC dekat.</summary>
    private bool TryInteract()
    {
        var npc = FindNearNpc();
        if (npc is null)
            return false;
        OpenDialogueFor(npc);
        return true;
    }

    /// <summary>B: buka shop langsung bila dekat NPC penjual.</summary>
    private bool TryOpenShopDirect()
    {
        var npc = FindNearNpc();
        if (npc?.ShopId is null)
            return false;
        OpenShop(npc.ShopId);
        return true;
    }

    private void OpenDialogueFor(Npc npc)
    {
        _dialogueNpc = npc;
        Dialogue dlg;
        if (npc.Id == "elder_rowan")
        {
            dlg = DialogueContent.ElderRowan(_questManager.GetStatus(QuestManager.RoadCleansingId));
        }
        else if (npc.Id == "forge_keeper")
        {
            var qs = _questManager.GetStatus(QuestManager.RoadCleansingId);
            bool done = qs is QuestStatus.Completed or QuestStatus.Claimed;
            dlg = DialogueContent.ForgeKeeper(done, npc.ShopId);
        }
        else
        {
            dlg = new Dialogue(npc.Name.ToUpperInvariant(), new[] { "..." });
        }
        _dialogue.Open(dlg);
        _state = GameState.Dialogue;
        _audio.PlaySfx("npc_talk");
        Console.WriteLine($"[Game] Talking to {npc.Name}.");
    }

    private void UpdateDialogue(bool advance, bool cancel)
    {
        if (cancel)
        {
            _dialogue.Cancel();
            _dialogueNpc = null;
            _state = GameState.Playing;
            Console.WriteLine("[Game] Dialogue closed.");
            return;
        }
        if (!advance)
            return;
        var action = _dialogue.Advance();
        if (_dialogue.IsOpen)
            return;
        var npc = _dialogueNpc;
        _dialogueNpc = null;
        switch (action)
        {
            case DialogueEndAction.None:
                _state = GameState.Playing;
                break;
            case DialogueEndAction.AcceptQuest:
                if (_questManager.StartQuest(npc?.QuestId ?? QuestManager.RoadCleansingId))
                {
                    _audio.PlaySfx("quest_accept");
                    TrySave();
                    Console.WriteLine("[Quest] Road Cleansing accepted (0/5).");
                }
                _state = GameState.Playing;
                break;
            case DialogueEndAction.ClaimReward:
                if (_questManager.ClaimReward(npc?.QuestId ?? QuestManager.RoadCleansingId, _player))
                {
                    _audio.PlaySfx("quest_complete");
                    TrySave();
                    Console.WriteLine("[Quest] Reward claimed: +30 ash_shard, +1 small_potion.");
                    _state = GameState.Playing;
                }
                else if (npc is not null)
                {
                    Console.WriteLine("[Quest] Inventory penuh, reward ditahan. Kosongkan slot lalu bicara lagi.");
                    OpenDialogueFor(npc);
                }
                else
                {
                    _state = GameState.Playing;
                }
                break;
            case DialogueEndAction.OpenShop:
                OpenShop(npc?.ShopId);
                break;
        }
    }

    private void OpenShop(string? shopId)
    {
        if (shopId != Shop.ForgeShopId)
        {
            _state = GameState.Playing;
            return;
        }
        _shopMenu.Open(_forgeShop);
        _state = GameState.Shop;
        Console.WriteLine("[Game] Shop opened.");
    }

    private void UpdateShop(bool up, bool down, bool confirm, bool back)
    {
        if (back)
        {
            _shopMenu.Close();
            _state = GameState.Playing;
            Console.WriteLine("[Game] Shop closed.");
            return;
        }
        if (up) _shopMenu.MoveUp();
        if (down) _shopMenu.MoveDown();
        if (!confirm)
            return;
        if (_shopMenu.BuySelected(_player))
        {
            _audio.PlaySfx("shop_buy");
            Console.WriteLine("[Game] Item purchased.");
        }
        else
        {
            Console.WriteLine("[Game] Cannot buy: shard kurang atau inventory penuh.");
        }
    }

    private void OpenQuestMenu()
    {
        _state = GameState.Quest;
        Console.WriteLine("[Game] Quest menu opened.");
    }

    /// <summary>
    /// Buat drop satu kali per kematian enemy (event Died).
    /// Boss: guaranteed ash_blade + forge_armor, sekali per session;
    /// ditekan bila boss sudah defeated dari save.
    /// </summary>
    private void SpawnDropsFor(Enemy enemy)
    {
        if (enemy is AshenWarden)
        {
            if (_bossRewardGiven)
                return;
            _bossRewardGiven = true;
            foreach (var reward in DropTable.RollDrops(enemy, _rng))
                _loot.Add(new LootDrop(reward.ItemId, reward.Quantity, enemy.X, enemy.Y));
            Console.WriteLine("[Game] Boss reward dropped.");
            return;
        }
        foreach (var reward in DropTable.RollDrops(enemy, _rng))
            _loot.Add(new LootDrop(reward.ItemId, reward.Quantity, enemy.X, enemy.Y));
    }

    /// <summary>
    /// XP Tahap 19 dari event Died existing (satu death = satu reward).
    /// Boss 150 XP sekali saat pertama defeated; revive/respawn/save-defeated
    /// tidak memberi lagi. Quest tracking tetap lewat subscription sendiri.
    /// </summary>
    private void GrantXpFor(Enemy enemy)
    {
        if (enemy is AshenWarden)
        {
            if (_bossXpGiven)
                return;
            _bossXpGiven = true;
            AwardXp(XpRewards.BossXp);
            return;
        }
        AwardXp(XpRewards.ForEnemy(enemy));
    }

    private void AwardXp(int amount)
    {
        if (amount <= 0)
            return;
        int ups = _player.GainXp(amount);
        if (ups > 0)
        {
            _audio.PlaySfx("level_up");
            Console.WriteLine($"[Game] Level up! LV {_player.Progression.Level}.");
        }
    }

    private bool TrySave()
    {
        SyncProgressLoadout();
        _questManager.SyncProgress(_progress);
        bool ok = _save.Save(_progress);
        if (!ok)
            Console.Error.WriteLine("[Game] warning: autosave gagal, gameplay lanjut.");
        return ok;
    }

    /// <summary>
    /// Salin runtime inventory/equipment -> progress sebelum save.
    /// Hanya item valid yang tersimpan (invalid terdahulu ikut bersih).
    /// </summary>
    private void SyncProgressLoadout()
    {
        var entries = new List<InventoryEntry>();
        foreach (var slot in _player.Inventory.Slots)
        {
            if (!slot.IsEmpty && ItemDatabase.Exists(slot.ItemId))
                entries.Add(new InventoryEntry { Id = slot.ItemId, Quantity = slot.Count });
        }
        _progress.Inventory = entries;
        _progress.Equipment = new EquipmentData
        {
            Weapon = ValidEquipId(_player.Equipment.WeaponId),
            Armor = ValidEquipId(_player.Equipment.ArmorId),
            Accessory = ValidEquipId(_player.Equipment.AccessoryId),
        };
        _progress.Level = _player.Progression.Level;
        _progress.CurrentXp = _player.Progression.CurrentXP;
    }

    private static string? ValidEquipId(string? id)
        => string.IsNullOrEmpty(id) || !ItemDatabase.Exists(id) ? null : id;

    /// <summary>
    /// Bangun ulang runtime inventory/equipment dari progress (Continue).
    /// Item ID tak dikenal diabaikan + warning, tanpa crash.
    /// </summary>
    private void ApplyProgressLoadout()
    {
        _player.Inventory.Clear();
        _player.Equipment.Clear();
        foreach (var entry in _progress.Inventory)
        {
            if (!ItemDatabase.Exists(entry.Id))
            {
                Console.Error.WriteLine($"[Save] warning: item tak dikenal '{entry.Id}' diabaikan.");
                continue;
            }
            if (entry.Quantity > 0)
                _player.Inventory.AddItem(entry.Id, entry.Quantity);
        }
        EquipIfPresent(_progress.Equipment.Weapon);
        EquipIfPresent(_progress.Equipment.Armor);
        EquipIfPresent(_progress.Equipment.Accessory);
        _player.ClampStatsToEffective();
    }

    private void EquipIfPresent(string? itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;
        if (!ItemDatabase.Exists(itemId))
        {
            Console.Error.WriteLine($"[Save] warning: equipped item tak dikenal '{itemId}' diabaikan.");
            return;
        }
        _player.Equipment.Equip(_player.Inventory, itemId);
    }

    /// <summary>Starting equipment New Game via flow normal (inventory -> equip).</summary>
    private void GiveStarterEquipment()
    {
        if (_player.Inventory.AddItem("rusted_blade", 1))
            _player.Equipment.Equip(_player.Inventory, "rusted_blade");
        _player.ClampStatsToEffective();
    }

    /// <summary>Musik per stage, start once (tidak restart per frame).</summary>
    private void StartStageMusic(int stageNumber)
    {
        if (stageNumber == 2)
        {
            if (_audio.HasMusic(AudioManager.Stage2MusicName))
                _audio.StartMusic(AudioManager.Stage2MusicName);
            else
                _audio.StartMusic(AudioManager.StageMusicName);
        }
        else
        {
            _audio.StartMusic(AudioManager.StageMusicName);
        }
    }

    /// <summary>
    /// Buat ulang runtime stage dari progress (New/Continue/Select).
    /// Membersihkan entities stage sebelumnya: enemies, projectiles,
    /// checkpoints, camera, boss, dan music state.
    /// </summary>
    private void LoadStage(int stageNumber, string checkpointId)
    {
        if (stageNumber != 1 && stageNumber != 2)
        {
            Console.Error.WriteLine($"[Game] stage {stageNumber} not implemented.");
            return;
        }
        _player.Dispose();

        _stage = Stage.Create(stageNumber);
        _world = new World(_stage);
        _camera = new Camera(WindowWidth, WindowHeight, _world.Width, _world.Height);
        _enemies = new List<Enemy>(_stage.Enemies);
        _checkpoints = new List<Checkpoint>(_stage.Checkpoints);
        _npcs = new List<Npc>(_stage.Npcs);
        _spikes = new List<Spike>(_stage.Spikes);
        _hazardZones = new List<HazardZone>(_stage.HazardZones);
        _movingPlatforms = new List<MovingPlatform>(_stage.MovingPlatforms);
        _fallingPlatforms = new List<FallingPlatform>(_stage.FallingPlatforms);
        _projectiles.Clear();
        _bossProjectiles.Clear();
        _bossMusicPlaying = false;
        _goalBlockedLogged = false;

        // Tahap 20: terapkan difficulty terpusat sekali saat spawn.
        // Normal = nilai existing; Hard via DifficultyModifiers.
        // XP/loot/shop/quest tidak disentuh; player base stats tidak dikurangi.
        Difficulty stageDiff = DifficultyModifiers.IsDefined(_progress.Difficulty)
            ? _progress.Difficulty
            : Difficulty.Normal;
        _progress.Difficulty = stageDiff;
        foreach (var enemy in _enemies)
            enemy.ApplyDifficulty(stageDiff);
        foreach (var spike in _spikes)
            spike.ApplyDifficulty(stageDiff);
        foreach (var hz in _hazardZones)
            hz.ApplyDifficulty(stageDiff);

        // Transient game-feel tidak dibawa antar stage/Continue.
        _feel.Clear();
        _camera.ClearShake();

        // Loot runtime fresh dari stage (tidak menggandakan item antar stage).
        _loot.Clear();
        foreach (var spawn in _stage.StartingLoot)
            _loot.Add(spawn.Clone());
        _bossRewardGiven = _progress.BossDefeated;
        _bossXpGiven = _progress.BossDefeated;

        BindBoss();

        // Terapkan save boss defeated (Stage 1 saja) sebelum subscribe audio.
        if (_progress.BossDefeated && _stage.HasBossArena)
        {
            _stage.BossDefeated = true;
            _boss?.ApplySaveDefeated();
        }

        // Respawn awal dari checkpoint save bila cocok di stage ini.
        // ID checkpoint unik per stage sehingga checkpoint Stage 2
        // tidak pernah diterapkan ke Stage 1 (dan sebaliknya).
        float sx = _world.SpawnX;
        float sy = _world.SpawnY;
        if (!string.IsNullOrEmpty(checkpointId))
        {
            foreach (var cp in _checkpoints)
            {
                if (cp.Id == checkpointId)
                {
                    sx = cp.RespawnX;
                    sy = cp.RespawnY;
                    break;
                }
            }
        }
        _player = new Player(sx, sy);
        _respawnX = sx;
        _respawnY = sy;
        if (_renderer != IntPtr.Zero)
            _player.LoadContent(_renderer);
        _camera.Follow(_player.X + Player.Width / 2f, _player.Y + Player.Height / 2f);
        SubscribeAudioEvents();
        ApplyProgressLoadout();
        _player.Progression.SetLevelAndXp(_progress.Level, _progress.CurrentXp);
        _player.ClampStatsToEffective();
        _questManager.ApplySave(_progress);
    }

    // ---------- Gameplay (Tahap 1-14 dipertahankan) ----------

    private void UpdateGameplay(float deltaTime, float dirX, bool jumpPressed, bool attackPressed, bool skillPressed, bool bashPressed, bool firePressed)
    {
        // Tahap 20: hit stop — freeze simulasi gameplay, render tetap jalan,
        // input tidak rusak (intent frame ini dilepas), audio tetap sekali (event-based).
        // Tidak berjalan saat paused/menu/dialogue (hanya dipanggil dari Playing).
        if (_feel.IsHitStopped)
        {
            _feel.Update(deltaTime);
            _camera.Update(deltaTime);
            return;
        }

        // 2. Player state updates (attack, dash, bash, fire)
        if (attackPressed && _player.TryStartAttack())
            _audio.PlaySfx("attack");
        if (skillPressed && _player.TryDash())
            _audio.PlaySfx("dash");
        if (bashPressed && _player.TryShieldBash())
            _audio.PlaySfx("shield_bash");
        if (firePressed && _player.TryFireProjectile())
        {
            _audio.PlaySfx("projectile");
            float dir = _player.Facing == Facing.Left ? -1f : 1f;
            float px = dir > 0f ? _player.X + Player.Width : _player.X - Projectile.Width;
            float py = _player.Y + (Player.Height - Projectile.Height) / 2f;
            _projectiles.Add(new Projectile(px, py, dir));
        }

        // 3. Environment update (moving/falling platforms)
        foreach (var mp in _movingPlatforms)
            mp.Update(deltaTime, _world);
        foreach (var fp in _fallingPlatforms)
            fp.Update(deltaTime, _world, _player);

        // 4. Enemy update (boss tidak di-ResetToSpawn saat OOB agar HP tidak ke-reset)
        foreach (var enemy in _enemies)
        {
            if (enemy.Alive || enemy is Boss)
                enemy.Update(deltaTime, _world, _player);
            if (enemy is Boss)
                continue;
            if (_world.IsOutOfBounds(enemy.Y))
                enemy.ResetToSpawn();
        }

        // 4b. Boss projectile spawn drain (event-based audio sekali per volley)
        if (_boss is not null)
        {
            var spawned = _boss.DrainSpawnedProjectiles();
            if (spawned.Count > 0)
            {
                _bossProjectiles.AddRange(spawned);
            }
        }

        // 5. Player physics/collision
        if (_player.Alive)
        {
            _player.Update(dirX, jumpPressed, deltaTime, _world);
            ApplyBossArenaLock();
        }
        else
        {
            _projectiles.Clear();
            _bossProjectiles.Clear();
            if (_player.TickDeathDelay(deltaTime))
                RespawnAll();
        }

        // 6. Environment collision/hazard checks
        // Moving platform ride
        foreach (var mp in _movingPlatforms)
            mp.CheckPlayerRide(_player, deltaTime);
        // Falling platform ride (only when stable)
        foreach (var fp in _fallingPlatforms)
            fp.CheckPlayerRide(_player, deltaTime);
        // Spike check
        foreach (var spike in _spikes)
            spike.CheckPlayer(_player);
        // Hazard zone check (handled in HazardZone.Update via timer)
        foreach (var hz in _hazardZones)
            hz.Update(deltaTime, _world, _player);

        // 7. Projectiles update
        foreach (var projectile in _projectiles)
            projectile.Update(deltaTime, _world);
        if (_boss is not null)
        {
            foreach (var bp in _bossProjectiles)
                bp.Update(deltaTime, _world, _player);
        }

        // 8. Combat resolution (boss kompatibel via Enemy API;
        // Dash tidak memberi damage karena tidak masuk Combat sama sekali)
        Combat.ResolvePlayerAttack(_player, _enemies);
        Combat.ResolveShieldBash(_player, _enemies);
        Combat.ResolveProjectiles(_player, _projectiles, _enemies);
        Combat.ResolveContactDamage(_enemies, _player);
        _projectiles.RemoveAll(static p => !p.Alive);
        _bossProjectiles.RemoveAll(static p => !p.Alive);

        // 8c. Loot pickup (penuh -> loot tetap di dunia).
        if (_player.Alive)
        {
            foreach (var loot in _loot)
            {
                if (loot.TryPickup(_player))
                {
                    _audio.PlaySfx("pickup");
                    Console.WriteLine($"[Game] Picked up {loot.ItemId} x{loot.Quantity}.");
                }
            }
            _loot.RemoveAll(static l => !l.Active);
        }

        // 8b. Boss encounter trigger + defeat handling
        UpdateBossEncounter();

        // 9. Checkpoint, goal, death boundary
        if (_player.Alive)
        {
            foreach (var checkpoint in _checkpoints)
            {
                if (checkpoint.TryActivate(_player.Bounds))
                {
                    _respawnX = checkpoint.RespawnX;
                    _respawnY = checkpoint.RespawnY;
                    _progress.CurrentStage = _stage.StageNumber;
                    _progress.MarkCheckpoint(checkpoint.Id);
                    TrySave();
                    Console.WriteLine("[Game] Checkpoint activated.");
                    _audio.PlaySfx("checkpoint");
                }
            }

            if (_stage.IsGoalBlocked)
            {
                if (_stage.Goal.IsReached(_player.Bounds) && !_goalBlockedLogged)
                {
                    _goalBlockedLogged = true;
                    Console.WriteLine("[Game] Goal locked — defeat Ashen Warden first!");
                }
            }
            else if (_stage.CheckGoal(_player.Bounds))
            {
                _state = GameState.StageComplete;
                if (_stage.StageNumber == 2)
                    _progress.MarkStage2Complete();
                else
                    _progress.MarkStageComplete();
                TrySave();
                _projectiles.Clear();
                _bossProjectiles.Clear();
                Console.WriteLine($"[Game] STAGE {_stage.StageNumber} COMPLETE.");
                _audio.PlaySfx("goal");
            }

            if (_world.IsOutOfBounds(_player.Y))
                _player.ResetToSpawn(_world.SpawnX, _world.SpawnY);
        }

        // 10. Camera + Tahap 20 transient (hanya saat Playing; freeze saat pause/menu).
        _feel.Update(deltaTime);
        _camera.Update(deltaTime);
        _camera.Follow(
            _player.X + Player.Width / 2f,
            _player.Y + Player.Height / 2f);
    }

    private bool Init()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
        {
            Console.Error.WriteLine($"SDL_Init failed: {SDL.SDL_GetError()}");
            return false;
        }

        _window = SDL.SDL_CreateWindow(
            WindowTitle,
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            WindowWidth,
            WindowHeight,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

        if (_window == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateWindow failed: {SDL.SDL_GetError()}");
            SDL.SDL_Quit();
            return false;
        }

        _renderer = SDL.SDL_CreateRenderer(
            _window,
            -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        if (_renderer == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateRenderer (accelerated) failed: {SDL.SDL_GetError()}, trying software fallback.");
            _renderer = SDL.SDL_CreateRenderer(
                _window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
        }

        if (_renderer == IntPtr.Zero)
        {
            Console.Error.WriteLine($"SDL_CreateRenderer failed: {SDL.SDL_GetError()}");
            SDL.SDL_DestroyWindow(_window);
            _window = IntPtr.Zero;
            SDL.SDL_Quit();
            return false;
        }

        if (!SdlImage.InitPng())
            Console.Error.WriteLine("[Game] SDL2_image PNG init failed, sprite may fall back to rectangle.");

        _player.LoadContent(_renderer);

        _audio.Initialize();
        _audio.LoadStageAudio();
        _audio.LoadBossAudio();
        _audio.LoadStage2Audio();
        _audio.LoadItemAudio();
        _audio.LoadNpcAudio();
        _audio.LoadProgressionAudio();
        SubscribeAudioEvents();

        return true;
    }

    /// <summary>
    /// Wiring event gameplay -> SFX + loot + game-feel (sekali per event, bukan per frame).
    /// Tahap 20: enemy Damaged/Died -> hit stop + camera shake + flash;
    /// player Hurt -> hurt flash + shake; player Died -> bersihkan transient.
    /// </summary>
    private void SubscribeAudioEvents()
    {
        _player.Jumped += () => _audio.PlaySfx("jump");
        _player.Hurt += () => _audio.PlaySfx("hurt");
        _player.Hurt += OnPlayerHurtFeel;
        _player.Died += () => _audio.PlaySfx("death");
        _player.Died += OnPlayerDeathFeel;
        foreach (var enemy in _enemies)
        {
            var captured = enemy;
            bool isBoss = captured is AshenWarden;
            enemy.Damaged += () => _audio.PlaySfx(captured.HurtSfx);
            enemy.Damaged += () => OnEnemyHitFeel(isBoss);
            if (enemy is not Boss)
                enemy.Died += () => _audio.PlaySfx("death");
            enemy.Died += () => OnEnemyHitFeel(isBoss);
            enemy.Died += () => SpawnDropsFor(captured);
            enemy.Died += () => GrantXpFor(captured);
            enemy.Died += () =>
            {
                if (_questManager.NotifyKill(captured, _stage.StageNumber))
                {
                    _audio.PlaySfx("quest_complete");
                    TrySave();
                    Console.WriteLine("[Quest] Road Cleansing complete (5/5)! Return to Elder Rowan.");
                }
            };
        }
        if (_boss is not null)
        {
            _boss.EncounterStarted += () => _audio.PlaySfx("boss_intro");
            _boss.PhaseChanged += _ => _audio.PlaySfx("boss_phase2");
            _boss.Defeated += () => _audio.PlaySfx("boss_death");
            _boss.Died += () => _audio.PlaySfx("boss_death");
            _boss.MeleeStarted += () => _audio.PlaySfx("boss_melee");
            _boss.ProjectileFired += () => _audio.PlaySfx("boss_projectile");
            _boss.LeapStarted += () => _audio.PlaySfx("boss_melee");
        }
    }

    /// <summary>
    /// Tahap 20: feedback saat serangan mengenai enemy (event-based, sekali per hit).
    /// Normal: hit stop 0.04s + shake 3px; boss: 0.06s + 5px + flash putih.
    /// Tidak mengubah damage logic; tidak memakai Thread.Sleep.
    /// </summary>
    private void OnEnemyHitFeel(bool isBoss)
    {
        _feel.TriggerHitStop(isBoss);
        if (isBoss)
        {
            _feel.TriggerBossHitFlash();
            _camera.ShakeBossHit();
        }
        else
        {
            _camera.ShakeHit();
        }
    }

    /// <summary>Tahap 20: player hurt -> flash merah + shake kecil (event-based).</summary>
    private void OnPlayerHurtFeel()
    {
        _feel.TriggerHurtFlash();
        _camera.ShakeHit();
    }

    /// <summary>Tahap 20: player dead -> bersihkan semua transient.</summary>
    private void OnPlayerDeathFeel()
    {
        _feel.Clear();
        _camera.ClearShake();
    }

    /// <summary>
    /// Encounter trigger sekali + defeat handling + music switch + autosave boss.
    /// </summary>
    private void UpdateBossEncounter()
    {
        if (_boss is null || !_stage.HasBossArena)
            return;

        // Trigger: player masuk arena -> encounter mulai sekali.
        if (!_boss.EncounterActive && !_boss.BossDefeated && _boss.Alive && _player.Alive)
        {
            if (_boss.Alive && _stage.BossTrigger.Overlaps(_player.Bounds))
            {
                _boss.StartEncounter();
                Console.WriteLine("[Game] Boss encounter started: Ashen Warden!");
                if (_audio.HasMusic(AudioManager.BossMusicName))
                {
                    _audio.StartMusic(AudioManager.BossMusicName);
                    _bossMusicPlaying = true;
                }
            }
        }

        // Defeat: HP 0 + death delay selesai -> unlock arena + goal + musik kembali + save.
        if (_boss.BossDefeated && !_stage.BossDefeated)
        {
            _stage.BossDefeated = true;
            _bossProjectiles.Clear();
            _progress.MarkBossDefeated();
            TrySave();
            Console.WriteLine("[Game] Boss defeated! Arena unlocked.");
            if (_bossMusicPlaying)
            {
                _audio.StopMusic();
                _bossMusicPlaying = false;
                _audio.StartMusic(AudioManager.StageMusicName);
            }
        }
    }

    /// <summary>Arena lock sederhana: clamp X player di dalam arena saat encounter aktif.</summary>
    private void ApplyBossArenaLock()
    {
        if (_boss is null || !_stage.HasBossArena)
            return;
        if (!_boss.EncounterActive || _boss.BossDefeated || !_boss.Alive)
            return;
        if (!_player.Alive)
            return;
        float minX = _stage.ArenaMinX;
        float maxX = _stage.ArenaMaxX - Player.Width;
        if (_player.X < minX) { _player.X = minX; _player.VelocityX = Math.Max(0f, _player.VelocityX); }
        if (_player.X > maxX) { _player.X = maxX; _player.VelocityX = Math.Min(0f, _player.VelocityX); }
    }

    private void Render()
    {
        if (_state == GameState.MainMenu)
        {
            _menu.Render(_renderer, WindowWidth, WindowHeight, _progress);
            SDL.SDL_RenderPresent(_renderer);
            return;
        }
        if (_state == GameState.StageSelect)
        {
            _stageSelect.Render(_renderer, WindowWidth, WindowHeight, _progress);
            SDL.SDL_RenderPresent(_renderer);
            return;
        }
        if (_state == GameState.Inventory)
        {
            RenderGameplay();
            _invMenu.Render(_renderer, WindowWidth, WindowHeight, _player);
            SDL.SDL_RenderPresent(_renderer);
            return;
        }
        if (_state == GameState.Dialogue)
        {
            RenderGameplay();
            RenderDialogueBox();
            SDL.SDL_RenderPresent(_renderer);
            return;
        }
        if (_state == GameState.Shop)
        {
            RenderGameplay();
            _shopMenu.Render(_renderer, WindowWidth, WindowHeight, _player);
            SDL.SDL_RenderPresent(_renderer);
            return;
        }
        if (_state == GameState.Quest)
        {
            RenderGameplay();
            _questMenu.Render(_renderer, WindowWidth, WindowHeight, _questManager);
            SDL.SDL_RenderPresent(_renderer);
            return;
        }

        RenderGameplay();
        if (_state == GameState.StageComplete)
            RenderStageCompleteOverlay();
        if (_state == GameState.Paused)
            RenderPauseOverlay();
        SDL.SDL_RenderPresent(_renderer);
    }

    private void RenderGameplay()
    {
        SDL.SDL_SetRenderDrawColor(_renderer, 30, 30, 46, 255);
        SDL.SDL_RenderClear(_renderer);
        _world.Render(_renderer, _camera);

        // Spikes
        foreach (var spike in _spikes)
        {
            if (_camera.IsVisible(spike.Bounds))
                spike.Render(_renderer, _camera);
        }

        // Hazard Zones
        foreach (var hz in _hazardZones)
        {
            if (_camera.IsVisible(hz.Bounds))
                hz.Render(_renderer, _camera);
        }

        foreach (var checkpoint in _checkpoints)
        {
            if (_camera.IsVisible(checkpoint.Zone))
                checkpoint.Render(_renderer, _camera);
        }
        _stage.Goal.Render(_renderer, _camera);
        foreach (var loot in _loot)
        {
            if (loot.Active && _camera.IsVisible(loot.Bounds))
                loot.Render(_renderer, _camera);
        }
        foreach (var npc in _npcs)
        {
            if (npc.Active && _camera.IsVisible(npc.Bounds))
                npc.Render(_renderer, _camera);
        }
        // Prompt interaksi + quest tracker (screen-space).
        if (_state == GameState.Playing)
            RenderInteractPrompt();
        RenderQuestTracker();
        // Render order: boss -> enemy -> projectiles -> player -> HUD.
        if (_boss is not null && !_boss.BossDefeated)
        {
            if (_camera.IsVisible(_boss.Bounds))
                _boss.Render(_renderer, _camera);
        }
        foreach (var enemy in _enemies)
        {
            if (enemy is Boss)
                continue;
            if (enemy.Alive && _camera.IsVisible(enemy.Bounds))
                enemy.Render(_renderer, _camera);
        }
        if (_debugShowEnemyAI)
        {
            foreach (var enemy in _enemies)
            {
                if (enemy.Alive)
                    RenderEnemyDebug(enemy);
            }
        }
        foreach (var projectile in _projectiles)
        {
            if (_camera.IsVisible(projectile.Bounds))
                projectile.Render(_renderer, _camera);
        }
        foreach (var bp in _bossProjectiles)
        {
            if (_camera.IsVisible(bp.Bounds))
                bp.Render(_renderer, _camera);
        }
        _player.Render(_renderer, _camera);
        if (_debugShowHitboxes && _player.IsAttackActive)
        {
            Hitbox box = _player.AttackHitbox;
            SDL.SDL_Rect rect = new()
            {
                x = (int)(box.X - _camera.RenderX),
                y = (int)(box.Y - _camera.RenderY),
                w = (int)box.W,
                h = (int)box.H
            };
            SDL.SDL_SetRenderDrawColor(_renderer, 255, 220, 80, 255);
            SDL.SDL_RenderDrawRect(_renderer, ref rect);
        }

        // Debug Environment
        if (_debugShowEnvironment)
        {
            foreach (var spike in _spikes)
            {
                if (_camera.IsVisible(spike.Bounds))
                    spike.RenderDebug(_renderer, _camera);
            }
            foreach (var hz in _hazardZones)
            {
                if (_camera.IsVisible(hz.Bounds))
                    hz.RenderDebug(_renderer, _camera);
            }
            foreach (var mp in _movingPlatforms)
            {
                if (_camera.IsVisible(mp.Bounds))
                    mp.RenderDebug(_renderer, _camera);
            }
            foreach (var fp in _fallingPlatforms)
            {
                if (_camera.IsVisible(fp.Bounds))
                    fp.RenderDebug(_renderer, _camera);
            }
        }

        Hud.RenderPlayerHp(_renderer, _player.Hp, _player.EffectiveMaxHp);
        Hud.RenderPlayerEnergy(_renderer, _player.Energy, _player.EffectiveMaxEnergy);
        Hud.RenderSkills(_renderer, _player.Dash, _player.ShieldBash, _player.Projectile);
        Hud.RenderProgression(_renderer, _player.Progression.Level, _player.Progression.CurrentXP, _player.Progression.XPToNextLevel);
        foreach (var enemy in _enemies)
        {
            if (enemy is Boss)
                continue;
            if (_camera.IsVisible(enemy.Bounds))
                Hud.RenderEnemyHp(_renderer, enemy, _camera);
        }
        if (_boss is not null && _boss.EncounterActive && !_boss.BossDefeated && _boss.Alive)
            Hud.RenderBossBar(_renderer, _boss.Hp, _boss.MaxHp, _boss.Phase, WindowWidth);
        if (_player.ShowLevelUp && _player.Alive)
        {
            int sx = (int)(_player.X + Player.Width / 2f - _camera.RenderX);
            int sy = (int)(_player.Y - _camera.RenderY);
            BitmapFont.DrawTextCentered(_renderer, "LEVEL UP!", sx, sy - 30, 2, 240, 220, 130);
        }
        RenderFeelOverlay();
        // Tahap 20: tampilkan difficulty aktif (BitmapFont, screen-space).
        string diffLabel = _progress.Difficulty == Difficulty.Hard ? "HARD" : "NORMAL";
        BitmapFont.DrawText(_renderer, diffLabel, WindowWidth - BitmapFont.MeasureText(diffLabel) - 12, 64, 2, 150, 170, 200);
    }

    /// <summary>
    /// Tahap 20: flash overlay sederhana (alpha rectangle, tanpa post-processing).
    /// Player hurt: merah singkat; boss hit: putih singkat.
    /// Render-only; tidak menyentuh world/collision.
    /// </summary>
    private void RenderFeelOverlay()
    {
        bool hurt = _feel.HurtFlashTimeLeft > 0f;
        bool bossHit = _feel.BossHitFlashTimeLeft > 0f;
        if (!hurt && !bossHit)
            return;
        SDL.SDL_BlendMode prev;
        SDL.SDL_GetRenderDrawBlendMode(_renderer, out prev);
        SDL.SDL_SetRenderDrawBlendMode(_renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        if (hurt)
        {
            float a = Math.Clamp(_feel.HurtFlashTimeLeft / GameFeel.HurtFlashDuration, 0f, 1f);
            SDL.SDL_Rect full = new() { x = 0, y = 0, w = WindowWidth, h = WindowHeight };
            SDL.SDL_SetRenderDrawColor(_renderer, 220, 60, 60, (byte)(90 * a + 30));
            SDL.SDL_RenderFillRect(_renderer, ref full);
        }
        if (bossHit)
        {
            float a = Math.Clamp(_feel.BossHitFlashTimeLeft / GameFeel.BossHitFlashDuration, 0f, 1f);
            SDL.SDL_Rect full = new() { x = 0, y = 0, w = WindowWidth, h = WindowHeight };
            SDL.SDL_SetRenderDrawColor(_renderer, 240, 240, 245, (byte)(70 * a + 20));
            SDL.SDL_RenderFillRect(_renderer, ref full);
        }
        SDL.SDL_SetRenderDrawBlendMode(_renderer, prev);
    }

    /// <summary>
    /// Overlay STAGE COMPLETE rectangle-only (tanpa font): panel + bingkai emas.
    /// </summary>
    private void RenderStageCompleteOverlay()
    {
        SDL.SDL_Rect panel = new() { x = 280, y = 200, w = 400, h = 140 };
        SDL.SDL_SetRenderDrawColor(_renderer, 12, 12, 20, 255);
        SDL.SDL_RenderFillRect(_renderer, ref panel);
        SDL.SDL_SetRenderDrawColor(_renderer, 240, 200, 80, 255);
        SDL.SDL_RenderDrawRect(_renderer, ref panel);

        SDL.SDL_Rect bar = new() { x = 330, y = 260, w = 300, h = 20 };
        SDL.SDL_SetRenderDrawColor(_renderer, 240, 200, 80, 255);
        SDL.SDL_RenderFillRect(_renderer, ref bar);
    }

    private void RenderPauseOverlay()
    {
        SDL.SDL_Rect panel = new() { x = 330, y = 210, w = 300, h = 120 };
        SDL.SDL_SetRenderDrawColor(_renderer, 10, 10, 18, 255);
        SDL.SDL_RenderFillRect(_renderer, ref panel);
        SDL.SDL_SetRenderDrawColor(_renderer, 150, 180, 230, 255);
        SDL.SDL_RenderDrawRect(_renderer, ref panel);
        SDL.SDL_Rect bar = new() { x = 380, y = 260, w = 200, h = 14 };
        SDL.SDL_SetRenderDrawColor(_renderer, 150, 180, 230, 255);
        SDL.SDL_RenderFillRect(_renderer, ref bar);
    }

    /// <summary>Prompt "F" di atas NPC terdekat (screen-space).</summary>
    private void RenderInteractPrompt()
    {
        var npc = FindNearNpc();
        if (npc is null)
            return;
        int sx = (int)(npc.X + Npc.NpcWidth / 2f - _camera.RenderX);
        int sy = (int)(npc.Y - _camera.RenderY);
        BitmapFont.DrawTextCentered(_renderer, "F", sx, sy - 26, 2, 240, 220, 130);
    }

    /// <summary>Tracker quest aktif (screen-space, kanan atas).</summary>
    private void RenderQuestTracker()
    {
        var quest = _questManager.ActiveQuest();
        if (quest is null)
            return;
        string line = quest.Status == QuestStatus.Completed
            ? $"{quest.Name.ToUpperInvariant()} DONE"
            : $"{quest.Name.ToUpperInvariant()} {quest.Objective.CurrentAmount} / {quest.Objective.RequiredAmount}";
        int w = BitmapFont.MeasureText(line);
        BitmapFont.DrawText(_renderer, line, WindowWidth - w - 12, 44, 2, 240, 220, 130);
    }

    /// <summary>Dialogue box bawah (800x120) + speaker + line + prompt.</summary>
    private void RenderDialogueBox()
    {
        var dlg = _dialogue.Current;
        if (dlg is null)
            return;
        const int boxW = 800;
        const int boxH = 120;
        int bx = (WindowWidth - boxW) / 2;
        int by = WindowHeight - boxH - 16;
        SDL.SDL_Rect box = new() { x = bx, y = by, w = boxW, h = boxH };
        SDL.SDL_SetRenderDrawColor(_renderer, 10, 10, 18, 255);
        SDL.SDL_RenderFillRect(_renderer, ref box);
        SDL.SDL_SetRenderDrawColor(_renderer, 240, 200, 80, 255);
        SDL.SDL_RenderDrawRect(_renderer, ref box);

        BitmapFont.DrawText(_renderer, dlg.Speaker, bx + 20, by + 10, 2, 240, 200, 80);
        BitmapFont.DrawText(_renderer, dlg.CurrentLine.ToUpperInvariant(), bx + 20, by + 38, 2);
        string prompt = !dlg.IsLastLine
            ? "[ENTER] NEXT"
            : dlg.EndAction switch
            {
                DialogueEndAction.AcceptQuest => "[ENTER] ACCEPT QUEST",
                DialogueEndAction.ClaimReward => "[ENTER] CLAIM REWARD",
                DialogueEndAction.OpenShop => "[ENTER] OPEN SHOP",
                _ => "[ENTER] CLOSE",
            };
        BitmapFont.DrawText(_renderer, prompt, bx + 20, by + 88, 2, 150, 170, 200);
    }

    /// <summary>
    /// Debug AI: kotak detection radius + titik warna state (hijau normal,
    /// kuning chase, biru stun). Hanya bila DebugShowEnemyAI true.
    /// </summary>
    private void RenderEnemyDebug(Enemy enemy)
    {
        float radius = enemy switch
        {
            Skeleton => Skeleton.DetectionRadius,
            Bat => Bat.DetectionRadius,
            _ => 0f
        };
        if (radius > 0f)
        {
            float cx = enemy.X + enemy.Width / 2f - _camera.RenderX;
            float cy = enemy.Y + enemy.Height / 2f - _camera.RenderY;
            SDL.SDL_Rect zone = new()
            {
                x = (int)(cx - radius),
                y = (int)(cy - radius),
                w = (int)(radius * 2f),
                h = (int)(radius * 2f)
            };
            SDL.SDL_SetRenderDrawColor(_renderer, 120, 120, 130, 255);
            SDL.SDL_RenderDrawRect(_renderer, ref zone);
        }

        (byte r, byte g, byte b) = enemy.State switch
        {
            EnemyState.Chase => ((byte)230, (byte)180, (byte)60),
            EnemyState.Stunned => ((byte)90, (byte)200, (byte)230),
            EnemyState.Dead => ((byte)80, (byte)80, (byte)80),
            _ => ((byte)90, (byte)210, (byte)110),
        };
        SDL.SDL_Rect dot = new()
        {
            x = (int)(enemy.X - _camera.RenderX),
            y = (int)(enemy.Y - _camera.RenderY) - 16,
            w = 8,
            h = 8
        };
        SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
        SDL.SDL_RenderFillRect(_renderer, ref dot);
    }

    /// <summary>
    /// Respawn: player hidup penuh di checkpoint aktif.
    /// Boss TIDAK di-revive: HP dipertahankan, serangan di-reset aman.
    /// Enemy lain revive seperti biasa. Checkpoint aktif tidak direset.
    /// Save file tidak dibaca ulang saat respawn (runtime existing dipakai).
    /// </summary>
    private void RespawnAll()
    {
        _player.Respawn(_respawnX, _respawnY);
        foreach (var enemy in _enemies)
        {
            if (enemy is Boss boss)
            {
                if (boss.BossDefeated || !boss.Alive)
                    continue; // Boss mati tetap mati.
                boss.OnPlayerDeathReset(); // HP dipertahankan, state aman.
                continue;
            }
            enemy.Revive();
        }
        foreach (var mp in _movingPlatforms)
            mp.ResetToSpawn();
        foreach (var fp in _fallingPlatforms)
            fp.ResetToSpawn();
        _projectiles.Clear();
        _bossProjectiles.Clear();
        _feel.Clear();
        _camera.ClearShake();
        Console.WriteLine("[Game] Player respawned.");
    }

    private void Cleanup()
    {
        _player.Dispose();
        _audio.Shutdown();

        if (_renderer != IntPtr.Zero)
        {
            SDL.SDL_DestroyRenderer(_renderer);
            _renderer = IntPtr.Zero;
        }

        if (_window != IntPtr.Zero)
        {
            SDL.SDL_DestroyWindow(_window);
            _window = IntPtr.Zero;
        }

        SdlImage.Quit();
        SDL.SDL_Quit();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        Cleanup();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
