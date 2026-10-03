using Android.Runtime;
using Java.Interop;

namespace AshenOath;

/// <summary>
/// Bridge Java-callable khusus Android (Android Callable Wrapper resmi).
/// PENTING: Java (AshenOathSDLActivity.java) TIDAK BOLEH mengimpor atau
/// mereferensikan class ini secara langsung — javac (AndroidJavaSource)
/// berjalan SEBELUM ACW dibuat sehingga simbolnya belum ada dan menyebabkan
/// "cannot find symbol". Java memanggil bridge ini via reflection
/// Class.forName("com.ashenoath.game.GameEntryBridge") saat runtime, ketika
/// ACW sudah ada di classes.dex dan runtime .NET sudah siap.
/// ACW dihasilkan dari [Register] + [Export] di bawah (tanpa JNI manual).
/// Method mendelegasikan ke GameEntry.RunGame(): sekali-panggil (Interlocked
/// guard di sana), game loop, exit code, dan exception handling tetap satu
/// sumber kebenaran di GameEntry.
/// </summary>
[Register("com/ashenoath/game/GameEntryBridge")]
public class GameEntryBridge : Java.Lang.Object
{
    [Export("runGame")]
    public static int RunGame() => GameEntry.RunGame();
}
