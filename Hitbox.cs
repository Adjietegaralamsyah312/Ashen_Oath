namespace AshenOath;

/// <summary>
/// Hitbox serangan: data rect + konversi ke AABB untuk overlap test.
/// </summary>
public readonly struct Hitbox
{
    public float X { get; }
    public float Y { get; }
    public float W { get; }
    public float H { get; }

    public Hitbox(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public Aabb ToAabb() => new(X, Y, W, H);
}
