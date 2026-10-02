namespace AshenOath;

/// <summary>
/// Data satu stage (prototype via kode, tanpa file eksternal).
/// Stage 2+ dapat ditambah sebagai factory method baru.
/// </summary>
public sealed class Stage
{
    public string Id { get; }
    public string Name { get; }
    public int StageNumber { get; }
    public int WorldWidth { get; }
    public int WorldHeight { get; }
    public float SpawnX { get; }
    public float SpawnY { get; }
    public List<Platform> Platforms { get; } = new();
    public List<Checkpoint> Checkpoints { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<Spike> Spikes { get; } = new();
    public List<HazardZone> HazardZones { get; } = new();
    public List<MovingPlatform> MovingPlatforms { get; } = new();
    public List<FallingPlatform> FallingPlatforms { get; } = new();
    public Goal Goal { get; private set; } = new(0f, 0f, 1f, 1f);
    public bool Completed { get; private set; }

    /// <summary>Loot awal terlihat di dunia (3 per stage, di-clone ke runtime).</summary>
    public List<LootDrop> StartingLoot { get; } = new();

    /// <summary>NPC statis per stage (quest giver / shop).</summary>
    public List<Npc> Npcs { get; } = new();

    // ---- Boss arena (Tahap 14) ----
    public bool HasBossArena { get; private set; }
    public bool BossDefeated { get; set; }
    public Aabb BossArena { get; private set; }
    public Aabb BossTrigger { get; private set; }
    public float BossSpawnX { get; private set; }
    public float BossSpawnY { get; private set; }
    public float ArenaMinX { get; private set; }
    public float ArenaMaxX { get; private set; }

    private Stage(string id, string name, int stageNumber, int worldWidth, int worldHeight, float spawnX, float spawnY)
    {
        Id = id;
        Name = name;
        StageNumber = stageNumber;
        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
        SpawnX = spawnX;
        SpawnY = spawnY;
    }

    /// <summary>
    /// Factory extensible ke Stage 3+: Stage.Create(1/2).
    /// </summary>
    public static Stage Create(int stageNumber) => stageNumber switch
    {
        1 => CreateStage1(),
        2 => CreateStage2(),
        _ => throw new ArgumentOutOfRangeException(nameof(stageNumber), $"Stage {stageNumber} not implemented."),
    };

    /// <summary>
    /// True bila sentuhan ini menyelesaikan stage.
    /// Bila stage punya boss arena, goal terkunci sampai BossDefeated.
    /// </summary>
    public bool CheckGoal(Aabb playerBounds)
    {
        if (Completed || !Goal.IsReached(playerBounds))
            return false;
        if (HasBossArena && !BossDefeated)
            return false;
        Completed = true;
        return true;
    }

    public bool IsGoalBlocked => HasBossArena && !BossDefeated;

    /// <summary>Stage 1 "Road of Ash": 3900x900, 7 area + boss arena.</summary>
    public static Stage CreateStage1()
    {
        var stage = new Stage("stage1", "Road of Ash", 1, 3900, 900, 120f, 700f);

        // Area 1: starting ground + platform + slime.
        stage.Platforms.Add(new Platform(0f, 800f, 620f, 100f, 76, 140, 96));
        stage.Platforms.Add(new Platform(180f, 710f, 160f, 24f, 96, 120, 180));
        stage.Platforms.Add(new Platform(400f, 620f, 160f, 24f, 96, 120, 180));
        stage.Enemies.Add(new Slime(300f, 772f));

        // Area 2: spikes + skeleton.
        stage.Platforms.Add(new Platform(700f, 800f, 600f, 100f, 76, 140, 96));
        stage.Platforms.Add(new Platform(760f, 710f, 170f, 24f, 96, 120, 180));
        stage.Platforms.Add(new Platform(980f, 620f, 170f, 24f, 96, 120, 180));
        stage.Platforms.Add(new Platform(1150f, 530f, 150f, 24f, 120, 150, 210));
        stage.Spikes.Add(new Spike(850f, 784f));
        stage.Spikes.Add(new Spike(900f, 784f));
        stage.Spikes.Add(new Spike(1050f, 506f));
        stage.Enemies.Add(new Skeleton(1150f, 752f));

        // Checkpoint 1: di atas platform Area 2 (pijakan Y=580).
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 1030f, respawnY: 580f,
            zoneX: 1020f, zoneY: 540f, zoneW: 70f, zoneH: 80f,
            poleX: 1050f, baseY: 620f, id: "checkpoint_area2"));

        // Area 3: moving platform + bat.
        stage.Platforms.Add(new Platform(1380f, 800f, 440f, 100f, 76, 140, 96));
        stage.MovingPlatforms.Add(new MovingPlatform(1450f, 1700f, 650f, 120));
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 1500f, respawnY: 760f,
            zoneX: 1490f, zoneY: 700f, zoneW: 70f, zoneH: 100f,
            poleX: 1520f, baseY: 800f, id: "checkpoint_area3"));
        stage.Enemies.Add(new Bat(1650f, 600f));

        // Area 4: falling platform + skeleton + bat (projectile/shield bash practice).
        stage.Platforms.Add(new Platform(1900f, 800f, 300f, 100f, 76, 140, 96));
        stage.FallingPlatforms.Add(new FallingPlatform(1950f, 700f, 100));
        stage.FallingPlatforms.Add(new FallingPlatform(2080f, 600f, 100));
        stage.Enemies.Add(new Skeleton(2000f, 752f));
        stage.Enemies.Add(new Bat(2100f, 550f));

        // Area 5: hazard zone + checkpoint.
        stage.Platforms.Add(new Platform(2250f, 800f, 400f, 100f, 76, 140, 96));
        stage.HazardZones.Add(new HazardZone(2300f, 720f, 200, 80));
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 2400f, respawnY: 760f,
            zoneX: 2390f, zoneY: 700f, zoneW: 70f, zoneH: 100f,
            poleX: 2420f, baseY: 800f, id: "checkpoint_area5"));

        // Area 6: combination gap + moving platform + enemies.
        stage.Platforms.Add(new Platform(2700f, 800f, 200f, 100f, 76, 140, 96));
        stage.MovingPlatforms.Add(new MovingPlatform(2750f, 2950f, 600f, 140));
        stage.Platforms.Add(new Platform(2950f, 710f, 150f, 24f, 96, 120, 180));
        stage.Platforms.Add(new Platform(3050f, 620f, 150f, 24f, 120, 150, 210));
        stage.Enemies.Add(new Slime(2750f, 772f));
        stage.Enemies.Add(new Bat(2900f, 550f));

        // Area 7: pre-boss ground + checkpoint sebelum arena.
        stage.Platforms.Add(new Platform(2850f, 800f, 350f, 100f, 76, 140, 96));
        stage.Enemies.Add(new Slime(3000f, 772f));
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 3100f, respawnY: 760f,
            zoneX: 3090f, zoneY: 700f, zoneW: 70f, zoneH: 100f,
            poleX: 3120f, baseY: 800f, id: "checkpoint_boss"));

        // Area 8: Boss arena "Ashen Warden" (~700x500, ground stabil,
        // tanpa moving/falling platform, tanpa hazard).
        // Ground 3200..3900 (700 lebar), top Y=800.
        stage.Platforms.Add(new Platform(3200f, 800f, 700f, 100f, 76, 140, 96));
        stage.HasBossArena = true;
        stage.BossArena = new Aabb(3200f, 300f, 700f, 500f);
        stage.BossTrigger = new Aabb(3210f, 600f, 100f, 200f);
        stage.ArenaMinX = 3200f;
        stage.ArenaMaxX = 3884f;
        stage.BossSpawnX = 3550f;
        stage.BossSpawnY = 720f;
        stage.Enemies.Add(new AshenWarden(stage.BossSpawnX, stage.BossSpawnY));

        // Wall kiri/kanan setinggi world.
        stage.Platforms.Add(new Platform(0f, 0f, 16f, 900f, 60, 60, 80));
        stage.Platforms.Add(new Platform(3884f, 0f, 16f, 900f, 60, 60, 80));

        // Goal di dalam arena, terkunci sampai boss mati.
        stage.Goal = new Goal(3800f, 704f, 64f, 96f);

        // Loot awal terlihat (3 pickup).
        stage.StartingLoot.Add(new LootDrop("small_potion", 1, 250f, 776f));
        stage.StartingLoot.Add(new LootDrop("ash_shard", 1, 1600f, 776f));
        stage.StartingLoot.Add(new LootDrop("worn_armor", 1, 2450f, 776f));

        // NPC: Elder Rowan dekat area awal (quest giver).
        stage.Npcs.Add(new Npc("elder_rowan", "Elder Rowan", 210f, 744f,
            dialogueId: "elder_rowan", questId: QuestManager.RoadCleansingId));
        return stage;
    }

    /// <summary>
    /// Stage 2 "The Hollow Forge": 4200x1200, 6 area, tanpa boss.
    /// Ground top Y=1050. Reuse Spike/HazardZone/Moving/Falling existing.
    /// Enemy budget: 3 Slime + 3 Skeleton + 3 Bat.
    /// </summary>
    public static Stage CreateStage2()
    {
        var stage = new Stage("stage2", "The Hollow Forge", 2, 4200, 1200, 120f, 970f);

        // AREA 1 — ENTRANCE: spawn + platform dasar + Slime + checkpoint awal.
        stage.Platforms.Add(new Platform(0f, 1050f, 700f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(200f, 950f, 160f, 24f, 150, 95, 60));
        stage.Enemies.Add(new Slime(350f, 1022f));
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 150f, respawnY: 1010f,
            zoneX: 140f, zoneY: 950f, zoneW: 70f, zoneH: 100f,
            poleX: 170f, baseY: 1050f, id: "checkpoint_start"));

        // AREA 2 — FURNACE: spike + hazard + Skeleton + moving platform + vertical.
        stage.Platforms.Add(new Platform(700f, 1050f, 700f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(800f, 900f, 170f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(1050f, 780f, 170f, 24f, 200, 120, 60));
        stage.MovingPlatforms.Add(new MovingPlatform(800f, 1050f, 850f, 120));
        stage.Spikes.Add(new Spike(900f, 1034f));
        stage.HazardZones.Add(new HazardZone(1100f, 970f, 200, 80));
        stage.Enemies.Add(new Skeleton(1200f, 1002f));

        // AREA 3 — VERTICAL SHAFT: tall platforms + falling + Bat/Skeleton + dash gap.
        stage.Platforms.Add(new Platform(1400f, 1050f, 350f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(1900f, 1050f, 200f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(1450f, 900f, 150f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(1650f, 750f, 150f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(1850f, 600f, 150f, 24f, 200, 120, 60));
        stage.FallingPlatforms.Add(new FallingPlatform(1550f, 800f, 100));
        stage.FallingPlatforms.Add(new FallingPlatform(1750f, 650f, 100));
        stage.Enemies.Add(new Skeleton(1900f, 1002f));
        stage.Enemies.Add(new Bat(1700f, 500f));

        // AREA 4 — FORGE BRIDGE: moving platform over gap + hazard + Slime/Bat + checkpoint.
        stage.Platforms.Add(new Platform(2100f, 1050f, 300f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(2600f, 1050f, 300f, 150f, 62, 52, 58));
        stage.MovingPlatforms.Add(new MovingPlatform(2350f, 2550f, 900f, 120));
        stage.HazardZones.Add(new HazardZone(2150f, 970f, 180, 80));
        stage.Enemies.Add(new Slime(2200f, 1022f));
        stage.Enemies.Add(new Bat(2500f, 800f));
        stage.Checkpoints.Add(new Checkpoint(
            respawnX: 2650f, respawnY: 1010f,
            zoneX: 2640f, zoneY: 950f, zoneW: 70f, zoneH: 100f,
            poleX: 2670f, baseY: 1050f, id: "checkpoint_forge"));

        // AREA 5 — INNER CHAMBER: susunan kompleks + ruang sempit (Shield Bash).
        stage.Platforms.Add(new Platform(2900f, 1050f, 700f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(3000f, 900f, 120f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(3200f, 900f, 120f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(3050f, 780f, 300f, 24f, 200, 120, 60));
        stage.Spikes.Add(new Spike(3150f, 1034f));
        stage.HazardZones.Add(new HazardZone(3300f, 970f, 180, 80));
        stage.Enemies.Add(new Slime(3100f, 1022f));
        stage.Enemies.Add(new Bat(3250f, 850f));

        // AREA 6 — FINAL GATE: traversal terakhir + Skeleton + goal.
        stage.Platforms.Add(new Platform(3600f, 1050f, 600f, 150f, 62, 52, 58));
        stage.Platforms.Add(new Platform(3700f, 920f, 150f, 24f, 150, 95, 60));
        stage.Platforms.Add(new Platform(3900f, 800f, 150f, 24f, 200, 120, 60));
        stage.Enemies.Add(new Skeleton(3950f, 1002f));

        // Wall kiri/kanan setinggi world.
        stage.Platforms.Add(new Platform(0f, 0f, 16f, 1200f, 60, 60, 80));
        stage.Platforms.Add(new Platform(4184f, 0f, 16f, 1200f, 60, 60, 80));

        // Goal Final Gate, tanpa boss requirement.
        stage.Goal = new Goal(4100f, 954f, 64f, 96f);

        // Loot awal terlihat (3 pickup).
        stage.StartingLoot.Add(new LootDrop("small_potion", 1, 400f, 1026f));
        stage.StartingLoot.Add(new LootDrop("ash_shard", 1, 2300f, 1026f));
        stage.StartingLoot.Add(new LootDrop("ash_ring", 1, 3350f, 850f));

        // NPC: Forge Keeper dekat Forge Bridge / checkpoint (quest + shop).
        stage.Npcs.Add(new Npc("forge_keeper", "Forge Keeper", 2700f, 994f,
            dialogueId: "forge_keeper", shopId: Shop.ForgeShopId));
        return stage;
    }
}
