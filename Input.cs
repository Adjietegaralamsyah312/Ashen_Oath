using SDL2;

namespace AshenOath;

/// <summary>
/// Keyboard state berbasis event SDL_KEYDOWN / SDL_KEYUP.
/// WASD + Arrow keys. Release ditangani via SDL_KEYUP agar tidak stuck.
/// Space / W / Up dimaknai sebagai intent lompat (edge-trigger, repeat diabaikan);
/// Player hanya melompat bila grounded sehingga tidak ada infinite/double jump.
/// J / Z dimaknai sebagai intent attack (edge-trigger seperti jump).
/// K / C dimaknai sebagai intent skill Dash (edge-trigger).
/// Q dimaknai sebagai intent Shield Bash, E sebagai intent Projectile (edge-trigger).
/// H = potion, I = inventory, F = interact, L = quest menu, B = shop.
/// Menu: arrows/WASD + Enter + Escape.
/// </summary>
public sealed class Input
{
    private readonly HashSet<SDL.SDL_Keycode> _pressed = new();
    private bool _jumpPressed;
    private bool _attackPressed;
    private bool _skillPressed;
    private bool _bashPressed;
    private bool _firePressed;
    private bool _menuUp;
    private bool _menuDown;
    private bool _menuLeft;
    private bool _menuRight;
    private bool _menuConfirm;
    private bool _menuBack;
    private bool _potionPressed;
    private bool _inventoryPressed;
    private bool _interactPressed;
    private bool _questPressed;
    private bool _shopPressed;

    public void HandleEvent(in SDL.SDL_Event e)
    {
        if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
        {
            // Abaikan key repeat agar HashSet stabil.
            if (e.key.repeat == 0)
            {
                _pressed.Add(e.key.keysym.sym);
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_SPACE
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_w
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_UP)
                {
                    _jumpPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_j
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_z)
                {
                    _attackPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_k
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_c)
                {
                    _skillPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_q)
                {
                    _bashPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_e)
                {
                    _firePressed = true;
                }
                // Menu navigation (edge-trigger, repeat diabaikan).
                // Dipisah dari gameplay: Game hanya consume satu set per state.
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_UP
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_w)
                {
                    _menuUp = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_s)
                {
                    _menuDown = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_a)
                {
                    _menuLeft = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_d)
                {
                    _menuRight = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_RETURN
                    || e.key.keysym.sym == SDL.SDL_Keycode.SDLK_KP_ENTER)
                {
                    _menuConfirm = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
                {
                    _menuBack = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_h)
                {
                    _potionPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_i)
                {
                    _inventoryPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_f)
                {
                    _interactPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_l)
                {
                    _questPressed = true;
                }
                if (e.key.keysym.sym == SDL.SDL_Keycode.SDLK_b)
                {
                    _shopPressed = true;
                }
            }
        }
        else if (e.type == SDL.SDL_EventType.SDL_KEYUP)
        {
            _pressed.Remove(e.key.keysym.sym);
        }
    }

    public bool IsDown(SDL.SDL_Keycode key) => _pressed.Contains(key);

    /// <summary>
    /// Ambil intent lompat sekali, lalu kosongkan (edge-trigger per frame).
    /// </summary>
    public bool ConsumeJumpPressed()
    {
        bool pressed = _jumpPressed;
        _jumpPressed = false;
        return pressed;
    }

    /// <summary>
    /// Ambil intent attack sekali, lalu kosongkan (edge-trigger per frame).
    /// </summary>
    public bool ConsumeAttackPressed()
    {
        bool pressed = _attackPressed;
        _attackPressed = false;
        return pressed;
    }

    /// <summary>
    /// Ambil intent skill sekali, lalu kosongkan (edge-trigger per frame).
    /// </summary>
    public bool ConsumeSkillPressed()
    {
        bool pressed = _skillPressed;
        _skillPressed = false;
        return pressed;
    }

    /// <summary>
    /// Ambil intent Shield Bash sekali, lalu kosongkan (edge-trigger per frame).
    /// </summary>
    public bool ConsumeBashPressed()
    {
        bool pressed = _bashPressed;
        _bashPressed = false;
        return pressed;
    }

    /// <summary>
    /// Ambil intent Projectile sekali, lalu kosongkan (edge-trigger per frame).
    /// </summary>
    public bool ConsumeFirePressed()
    {
        bool pressed = _firePressed;
        _firePressed = false;
        return pressed;
    }

    public bool ConsumeMenuUp()
    {
        bool pressed = _menuUp;
        _menuUp = false;
        return pressed;
    }

    public bool ConsumeMenuDown()
    {
        bool pressed = _menuDown;
        _menuDown = false;
        return pressed;
    }

    public bool ConsumeMenuConfirm()
    {
        bool pressed = _menuConfirm;
        _menuConfirm = false;
        return pressed;
    }

    public bool ConsumeMenuBack()
    {
        bool pressed = _menuBack;
        _menuBack = false;
        return pressed;
    }

    public bool ConsumeMenuLeft()
    {
        bool pressed = _menuLeft;
        _menuLeft = false;
        return pressed;
    }

    public bool ConsumeMenuRight()
    {
        bool pressed = _menuRight;
        _menuRight = false;
        return pressed;
    }

    public bool ConsumePotionPressed()
    {
        bool pressed = _potionPressed;
        _potionPressed = false;
        return pressed;
    }

    public bool ConsumeInventoryPressed()
    {
        bool pressed = _inventoryPressed;
        _inventoryPressed = false;
        return pressed;
    }

    public bool ConsumeInteractPressed()
    {
        bool pressed = _interactPressed;
        _interactPressed = false;
        return pressed;
    }

    public bool ConsumeQuestPressed()
    {
        bool pressed = _questPressed;
        _questPressed = false;
        return pressed;
    }

    public bool ConsumeShopPressed()
    {
        bool pressed = _shopPressed;
        _shopPressed = false;
        return pressed;
    }

    public (float X, float Y) GetDirection()
    {
        float x = 0f;
        float y = 0f;

        if (IsDown(SDL.SDL_Keycode.SDLK_a) || IsDown(SDL.SDL_Keycode.SDLK_LEFT))
            x -= 1f;
        if (IsDown(SDL.SDL_Keycode.SDLK_d) || IsDown(SDL.SDL_Keycode.SDLK_RIGHT))
            x += 1f;
        if (IsDown(SDL.SDL_Keycode.SDLK_w) || IsDown(SDL.SDL_Keycode.SDLK_UP))
            y -= 1f;
        if (IsDown(SDL.SDL_Keycode.SDLK_s) || IsDown(SDL.SDL_Keycode.SDLK_DOWN))
            y += 1f;

        return (x, y);
    }

    public void Clear()
    {
        _pressed.Clear();
        _jumpPressed = false;
        _attackPressed = false;
        _skillPressed = false;
        _bashPressed = false;
        _firePressed = false;
        _menuUp = false;
        _menuDown = false;
        _menuLeft = false;
        _menuRight = false;
        _menuConfirm = false;
        _menuBack = false;
        _potionPressed = false;
        _inventoryPressed = false;
        _interactPressed = false;
        _questPressed = false;
        _shopPressed = false;
    }
}
