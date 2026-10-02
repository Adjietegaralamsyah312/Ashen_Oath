using SDL2;

namespace AshenOath;

/// <summary>
/// Falling Platform: solid initially, falls after player lands + delay.
/// Resets to spawn position after reset delay.
/// </summary>
public sealed class FallingPlatform : IPhysicsBody
{
    public const float TriggerDelay = 0.5f;
    public const float ResetDelay = 2.0f;
    public const float FallSpeed = 300f;
    public const int PlatformHeight = 24;

    public enum FallingState
    {
        Stable,
        Triggered,
        Falling,
        Resetting
    }

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

    public FallingState State { get; private set; } = FallingState.Stable;

    private readonly float _spawnX;
    private readonly float _spawnY;
    private float _stateTimer;
    private bool _playerWasOnTop;

    public FallingPlatform(float x, float y, int width)
    {
        _spawnX = x;
        _spawnY = y;
        Width = width;
        X = x;
        Y = y;
    }

    public void Update(float deltaTime, World world, Player player)
    {
        switch (State)
        {
            case FallingState.Stable:
                CheckPlayerTrigger(player);
                break;

            case FallingState.Triggered:
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f)
                {
                    State = FallingState.Falling;
                    VelocityY = FallSpeed;
                }
                break;

            case FallingState.Falling:
                Y += VelocityY * deltaTime;
                if (Y > world.Height + Height)
                {
                    State = FallingState.Resetting;
                    _stateTimer = ResetDelay;
                }
                break;

            case FallingState.Resetting:
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f)
                {
                    ResetToSpawn();
                }
                break;
        }
    }

    private void CheckPlayerTrigger(Player player)
    {
        if (!player.Alive || !player.Grounded)
        {
            _playerWasOnTop = false;
            return;
        }

        bool playerOnTop = player.Bounds.Overlaps(Bounds) &&
                           player.Y + Player.Height <= Y + 4f &&
                           player.VelocityY >= 0f;

        if (playerOnTop && !_playerWasOnTop)
        {
            State = FallingState.Triggered;
            _stateTimer = TriggerDelay;
        }
        _playerWasOnTop = playerOnTop;
    }

    public void CheckPlayerRide(Player player, float deltaTime)
    {
        if (!player.Alive || !player.Grounded || State != FallingState.Stable)
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

        byte r, g, b;
        switch (State)
        {
            case FallingState.Triggered:
                r = 255; g = 180; b = 60;
                break;
            case FallingState.Falling:
                r = 220; g = 80; b = 60;
                break;
            case FallingState.Resetting:
                r = 100; g = 100; b = 140;
                break;
            default:
                r = 100; g = 140; b = 100;
                break;
        }

        SDL.SDL_Rect baseRect = new() { x = sx, y = sy + Height - 6, w = Width, h = 6 };
        SDL.SDL_SetRenderDrawColor(renderer, (byte)(r * 0.6), (byte)(g * 0.6), (byte)(b * 0.6), 255);
        SDL.SDL_RenderFillRect(renderer, ref baseRect);

        SDL.SDL_Rect top = new() { x = sx, y = sy, w = Width, h = Height - 6 };
        SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
        SDL.SDL_RenderFillRect(renderer, ref top);

        if (State == FallingState.Triggered)
        {
            float pulse = (TriggerDelay - _stateTimer) / TriggerDelay;
            SDL.SDL_Rect warn = new() { x = sx, y = sy - 4, w = Width, h = 4 };
            SDL.SDL_SetRenderDrawColor(renderer, 255, 180, 60, (byte)(128 + pulse * 127));
            SDL.SDL_RenderFillRect(renderer, ref warn);
        }
    }

    public void RenderDebug(IntPtr renderer, Camera camera)
    {
        int sx = (int)(X - camera.RenderX);
        int sy = (int)(Y - camera.RenderY);
        SDL.SDL_Rect debug = new() { x = sx, y = sy, w = Width, h = Height };
        byte r, g, b;
        switch (State)
        {
            case FallingState.Triggered: r = 255; g = 180; b = 60; break;
            case FallingState.Falling: r = 220; g = 80; b = 60; break;
            case FallingState.Resetting: r = 100; g = 100; b = 140; break;
            default: r = 100; g = 200; b = 100; break;
        }
        SDL.SDL_SetRenderDrawColor(renderer, r, g, b, 255);
        SDL.SDL_RenderDrawRect(renderer, ref debug);
    }

    public void ResetToSpawn()
    {
        X = _spawnX;
        Y = _spawnY;
        VelocityX = 0f;
        VelocityY = 0f;
        Grounded = false;
        State = FallingState.Stable;
        _stateTimer = 0f;
        _playerWasOnTop = false;
    }
}