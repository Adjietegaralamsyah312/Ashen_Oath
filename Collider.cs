namespace AshenOath;

/// <summary>
/// AABB float untuk physics. Overlap test sisi-inklusif tepi (touching = tidak overlap).
/// </summary>
public readonly struct Aabb
{
    public float X { get; }
    public float Y { get; }
    public float W { get; }
    public float H { get; }

    public float Left => X;
    public float Right => X + W;
    public float Top => Y;
    public float Bottom => Y + H;

    public Aabb(float x, float y, float w, float h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public bool Overlaps(Aabb other)
    {
        return Left < other.Right && Right > other.Left
            && Top < other.Bottom && Bottom > other.Top;
    }
}
