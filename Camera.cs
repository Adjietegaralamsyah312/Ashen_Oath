namespace AshenOath;

/// <summary>
/// Kamera direct-follow (tanpa smoothing): center mengikuti target,
/// di-clamp agar tidak keluar dari world bounds.
/// Semua gameplay memakai world coordinates; render mengurangkan posisi kamera.
/// </summary>
public sealed class Camera
{
    public float X { get; private set; }
    public float Y { get; private set; }
    public int ViewWidth { get; }
    public int ViewHeight { get; }
    public int WorldWidth { get; }
    public int WorldHeight { get; }

    /// <summary>Durasi shake normal enemy hit / player hurt (detik).</summary>
    public const float ShakeDurationHit = 0.12f;

    /// <summary>Durasi shake boss hit (detik).</summary>
    public const float ShakeDurationBoss = 0.20f;

    /// <summary>Magnitudo shake normal enemy hit / player hurt (px).</summary>
    public const float ShakeMagnitudeHit = 3f;

    /// <summary>Magnitudo shake boss hit (px).</summary>
    public const float ShakeMagnitudeBoss = 5f;

    private float _shakeTime;
    private float _shakeDuration = 1f;
    private float _shakeMagnitude;
    private readonly Random _shakeRng = new();

    /// <summary>Offset render X (0 bila tidak shake). World coords tak tersentuh.</summary>
    public float RenderX => X + ShakeOffsetX;

    /// <summary>Offset render Y (0 bila tidak shake). World coords tak tersentuh.</summary>
    public float RenderY => Y + ShakeOffsetY;

    public bool IsShaking => _shakeTime > 0f;

    public Camera(int viewWidth, int viewHeight, int worldWidth, int worldHeight)
    {
        ViewWidth = viewWidth;
        ViewHeight = viewHeight;
        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
    }

    public void Follow(float targetCenterX, float targetCenterY)
    {
        float maxX = Math.Max(0, WorldWidth - ViewWidth);
        float maxY = Math.Max(0, WorldHeight - ViewHeight);
        X = Math.Clamp(targetCenterX - ViewWidth / 2f, 0f, maxX);
        Y = Math.Clamp(targetCenterY - ViewHeight / 2f, 0f, maxY);
    }

    /// <summary>
    /// Picu shake render-only (tidak mengubah world coords/collision/clamp).
    /// Durasi*magnitudo terbesar menang; tidak stacking.
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f)
            return;
        if (duration * magnitude < _shakeTime * _shakeMagnitude)
            return;
        _shakeDuration = duration;
        _shakeTime = duration;
        _shakeMagnitude = magnitude;
    }

    public void ShakeHit() => Shake(ShakeDurationHit, ShakeMagnitudeHit);
    public void ShakeBossHit() => Shake(ShakeDurationBoss, ShakeMagnitudeBoss);

    public void Update(float deltaTime)
    {
        if (_shakeTime > 0f)
        {
            _shakeTime -= deltaTime;
            if (_shakeTime < 0f)
                _shakeTime = 0f;
        }
    }

    /// <summary>Bersihkan shake (mati/Continue/pindah state).</summary>
    public void ClearShake()
    {
        _shakeTime = 0f;
        _shakeMagnitude = 0f;
    }

    private float ShakeOffsetX
    {
        get
        {
            if (_shakeTime <= 0f)
                return 0f;
            float fade = _shakeTime / _shakeDuration;
            return (float)(_shakeRng.NextDouble() * 2.0 - 1.0) * _shakeMagnitude * fade;
        }
    }

    private float ShakeOffsetY
    {
        get
        {
            if (_shakeTime <= 0f)
                return 0f;
            float fade = _shakeTime / _shakeDuration;
            return (float)(_shakeRng.NextDouble() * 2.0 - 1.0) * _shakeMagnitude * fade;
        }
    }

    public float ToScreenX(float worldX) => worldX - X;
    public float ToScreenY(float worldY) => worldY - Y;

    public bool IsVisible(Aabb box)
    {
        return box.Right > X && box.Left < X + ViewWidth
            && box.Bottom > Y && box.Top < Y + ViewHeight;
    }
}
