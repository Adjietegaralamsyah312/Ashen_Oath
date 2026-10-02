using System.Collections.ObjectModel;

namespace AshenOath;

/// <summary>
/// World runtime dari data <see cref="Stage"/>: dimensi, spawn, solids.
/// Koordinat gameplay selalu world coordinates.
/// </summary>
public sealed class World
{
    public const float FallResetMargin = 80f;

    public int Width { get; }
    public int Height { get; }
    public float SpawnX { get; }
    public float SpawnY { get; }

    public IReadOnlyList<Platform> Platforms { get; }
    public IReadOnlyList<MovingPlatform> MovingPlatforms { get; }
    public IReadOnlyList<FallingPlatform> FallingPlatforms { get; }
    public ReadOnlyCollection<Aabb> Solids { get; }

    public World(Stage stage)
    {
        Width = stage.WorldWidth;
        Height = stage.WorldHeight;
        SpawnX = stage.SpawnX;
        SpawnY = stage.SpawnY;
        Platforms = stage.Platforms;
        MovingPlatforms = stage.MovingPlatforms;
        FallingPlatforms = stage.FallingPlatforms;

        var solids = new List<Aabb>();
        foreach (var p in stage.Platforms)
            solids.Add(p.Bounds);
        foreach (var mp in stage.MovingPlatforms)
            solids.Add(mp.Bounds);
        foreach (var fp in stage.FallingPlatforms)
            solids.Add(fp.Bounds);
        Solids = solids.AsReadOnly();
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        foreach (var platform in Platforms)
        {
            if (camera.IsVisible(platform.Bounds))
                platform.Render(renderer, camera);
        }
        foreach (var mp in MovingPlatforms)
        {
            if (camera.IsVisible(mp.Bounds))
                mp.Render(renderer, camera);
        }
        foreach (var fp in FallingPlatforms)
        {
            if (camera.IsVisible(fp.Bounds))
                fp.Render(renderer, camera);
        }
    }

    public bool IsOutOfBounds(float y) => y > Height + FallResetMargin;
}
