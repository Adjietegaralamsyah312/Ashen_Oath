using Android.Runtime;
using Android.Util;
using Java.Interop;

namespace AshenOath;

/// <summary>
/// Satu-satunya entry point managed Android (pengganti pola lama
/// MainActivity-Thread di file yang dihapus).
/// Dipanggil dari Java AshenOathSDLActivity.ManagedMain sebagai method Java
/// biasa <c>com.ashenoath.game.GameEntry.runGame()</c> — stub ACW resmi .NET
/// for Android dari atribut [Export] di bawah (tanpa JNI manual, tanpa tebakan
/// nama: [Register] eksplisit).
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
