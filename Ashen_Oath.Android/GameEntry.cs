using Android.Runtime;
using Android.Util;
using Java.Interop;

namespace AshenOath;

/// <summary>
/// Logika game loop managed Android (sumber kebenaran tunggal).
/// Dipanggil HANYA via <see cref="GameEntryBridge.RunGame"/> (delegasi C#),
/// yang dipanggil Java via reflection Class.forName("...GameEntryBridge").
/// Java TIDAK BOLEH mereferensikan class ini langsung (javac berjalan sebelum
/// ACW ada → "cannot find symbol").
/// Berjalan di thread "SDLThread" milik SDL SETELAH gerbang kesiapan
/// (surface + focus + resumed), sehingga SDL_Init() tidak pernah terlalu dini.
/// Tepat satu kali (Interlocked guard); Game.Run() tetap satu-satunya loop.
/// Desktop tidak tersentuh: Program.cs -&gt; Game.Run() tetap.
/// </summary>
[Register("com/ashenoath/game/GameEntry")]
public class GameEntry : Java.Lang.Object
{
    private const string LogTag = "AshenOath";
    private static int _started;

    [Export("runGame")]
    public static int RunGame()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            Log.Warn(LogTag, "GameEntry.runGame() dipanggil dua kali; panggilan kedua diabaikan.");
            return 0;
        }
        try
        {
            AndroidAssetBootstrap.EnsureExtracted();
            using var game = new Game();
            int exitCode = game.Run();
            Log.Info(LogTag, "Game.Run() selesai (exit=" + exitCode + ").");
            return exitCode;
        }
        catch (Exception ex)
        {
            Log.Error(LogTag, "Game crash: " + ex);
            return 99;
        }
    }
}
