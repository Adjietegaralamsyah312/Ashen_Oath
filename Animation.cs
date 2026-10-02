using SDL2;

namespace AshenOath;

/// <summary>
/// Sequence frame reusable: daftar source rect, durasi per frame,
/// Update(deltaTime), looping, Reset, Current.
/// </summary>
public sealed class Animation
{
    public string Name { get; }
    public float FrameDuration { get; }
    public bool Loop { get; }
    public int FrameCount => _frames.Count;
    public int FrameIndex { get; private set; }
    public bool IsFinished => _finished;

    private readonly IReadOnlyList<SDL.SDL_Rect> _frames;
    private float _elapsed;
    private bool _finished;

    public Animation(string name, IReadOnlyList<SDL.SDL_Rect> frames, float frameDuration, bool loop = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Animation name required.", nameof(name));
        if (frames is null || frames.Count == 0)
            throw new ArgumentException("Animation needs at least one frame.", nameof(frames));
        if (frameDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(frameDuration), "Frame duration must be positive.");

        Name = name;
        _frames = frames;
        FrameDuration = frameDuration;
        Loop = loop;
    }

    public void Update(float deltaTime)
    {
        if (deltaTime < 0f)
            deltaTime = 0f;
        if (_frames.Count <= 1 || _finished)
            return;

        _elapsed += deltaTime;
        while (_elapsed >= FrameDuration)
        {
            _elapsed -= FrameDuration;
            FrameIndex++;
            if (FrameIndex >= _frames.Count)
            {
                if (Loop)
                {
                    FrameIndex = 0;
                }
                else
                {
                    FrameIndex = _frames.Count - 1;
                    _finished = true;
                    _elapsed = 0f;
                    break;
                }
            }
        }
    }

    public void Reset()
    {
        FrameIndex = 0;
        _elapsed = 0f;
        _finished = false;
    }

    public SDL.SDL_Rect Current => _frames[FrameIndex];
}
