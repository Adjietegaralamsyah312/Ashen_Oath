using SDL2;

namespace AshenOath;

/// <summary>
/// Moving Platform: horizontal platform that moves between two points.
/// Player rides on it when grounded.
/// </summary>
public sealed class MovingPlatform : IPhysicsBody
{
    public const float DefaultSpeed = 100f;
    public const int PlatformHeight = 24;

    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public bool Grounded { get; set; }
    public int Width { get; }
    public int Height => PlatformHeight;
    public int BodyWidth => Width;
    public int BodyHeight => Height;
    public Aabb Bounds => new(X, Y, Width, Height);

    private readonly float _startX;
    private readonly float _endX;
    private readonly float _speed;
    private int _direction = 1;
    private readonly float _spawnX;
    private readonly float _spawnY;

    public MovingPlatform(float startX, float endX, float y, int width, float speed = DefaultSpeed)
    {
        _startX = startX;
        _endX = endX;
        _speed = speed;
        Width = width;
        X = startX;
        Y = y;
        _spawnX = startX;
        _spawnY = y;
        VelocityX = _direction * _speed;
    }

    public void Update(float deltaTime, World world)
    {
        X += VelocityX * deltaTime;

        if (_direction > 0 && X >= _endX)
        {
            X = _endX;
            _direction = -1;
            VelocityX = -_speed;
        }
        else if (_direction < 0 && X <= _startX)
        {
            X = _startX;
            _direction = 1;
            VelocityX = _speed;
        }

        if (X < 0f) X = 0f;
        float maxX = world.Width - Width;
        if (X > maxX) X = maxX;
    }

    public void CheckPlayerRide(Player player, float deltaTime)
    {
        if (!player.Alive || !player.Grounded)
            return;

        if (player.Bounds.Overlaps(Bounds) &&
            player.Y + Player.Height <= Y + 4f &&
            player.VelocityY >= 0f)
        {
            player.X += VelocityX * deltaTime;
        }
    }

    public void Render(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);

        SDL.SDL_Rect baseRect = new() { x = sx, y = sy + Height - 6, w = Width, h = 6 };
        SDL.SDL_SetRenderDrawColor(renderer, 80, 70, 100, 255);
        SDL.SDL_RenderFillRect(renderer, ref baseRect);

        SDL.SDL_Rect top = new() { x = sx, y = sy, w = Width, h = Height - 6 };
        SDL.SDL_SetRenderDrawColor(renderer, 120, 110, 150, 255);
        SDL.SDL_RenderFillRect(renderer, ref top);

        SDL.SDL_Rect highlight = new() { x = sx + 4, y = sy + 4, w = Width - 8, h = 4 };
        SDL.SDL_SetRenderDrawColor(renderer, 160, 150, 190, 255);
        SDL.SDL_RenderFillRect(renderer, ref highlight);
    }

    public void RenderDebug(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        SDL.SDL_Rect debug = new() { x = sx, y = sy, w = Width, h = Height };
        SDL.SDL_SetRenderDrawColor(renderer, 80, 200, 255, 255);
        SDL.SDL_RenderDrawRect(renderer, ref debug);

        SDL.SDL_Rect start = new() { x = (int)(_startX - camera.RenderX), y = sy - 8, w = 16, h = 8 };
        SDL.SDL_SetRenderDrawColor(renderer, 80, 255, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref start);

        SDL.SDL_Rect end = new() { x = (int)(_endX - camera.RenderX), y = sy - 8, w = 16, h = 8 };
        SDL.SDL_SetRenderDrawColor(renderer, 255, 80, 80, 255);
        SDL.SDL_RenderFillRect(renderer, ref end);
    }

    public void ResetToSpawn()
    {
        X = _spawnX;
        Y = _spawnY;
        VelocityX = _direction > 0 ? _speed : -_speed;
        _direction = 1;
    }
}