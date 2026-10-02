using System.Collections.ObjectModel;

namespace AshenOath;

/// <summary>
/// Body yang bisa diintegrasikan Physics (Player, Enemy).
/// </summary>
public interface IPhysicsBody
{
    float X { get; set; }
    float Y { get; set; }
    float VelocityX { get; set; }
    float VelocityY { get; set; }
    bool Grounded { get; set; }
    int BodyWidth { get; }
    int BodyHeight { get; }
    Aabb Bounds { get; }
}

/// <summary>
/// Integrasi + resolusi tabrakan axis-separated.
/// Body mengatur intent (velocity), Physics mengatur posisi vs solids.
/// </summary>
public static class Physics
{
    public const float Gravity = 1200f;
    public const float MaxFallSpeed = 900f;

    public static float ApplyGravity(float velocityY, float deltaTime)
    {
        velocityY += Gravity * deltaTime;
        if (velocityY > MaxFallSpeed)
            velocityY = MaxFallSpeed;
        return velocityY;
    }

    public static void MoveX(IPhysicsBody body, ReadOnlyCollection<Aabb> solids, float deltaTime)
    {
        body.X += body.VelocityX * deltaTime;
        if (body.VelocityX == 0f)
            return;

        var box = body.Bounds;
        foreach (var solid in solids)
        {
            if (!box.Overlaps(solid))
                continue;
            if (body.VelocityX > 0f)
                body.X = solid.Left - body.BodyWidth;
            else
                body.X = solid.Right;
            body.VelocityX = 0f;
            box = body.Bounds;
        }
    }

    public static void MoveY(IPhysicsBody body, ReadOnlyCollection<Aabb> solids, float deltaTime)
    {
        body.Grounded = false;
        body.Y += body.VelocityY * deltaTime;

        var box = body.Bounds;
        foreach (var solid in solids)
        {
            if (!box.Overlaps(solid))
                continue;
            if (body.VelocityY > 0f)
            {
                body.Y = solid.Top - body.BodyHeight;
                body.VelocityY = 0f;
                body.Grounded = true;
            }
            else if (body.VelocityY < 0f)
            {
                body.Y = solid.Bottom;
                body.VelocityY = 0f;
            }
            box = body.Bounds;
        }
    }
}
