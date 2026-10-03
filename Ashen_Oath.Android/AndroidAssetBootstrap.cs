using Android.Content.Res;

namespace AshenOath;

/// <summary>
/// Bootstrap asset khusus Android (head project saja; gameplay tidak diubah).
/// Game membaca asset via path file relatif ("Assets/...", relatif dulu lalu
/// fallback BaseDirectory — lihat Player.ResolveAssetPath /
/// AudioManager.ResolveAsset). Di APK, asset tinggal di dalam paket sebagai
/// AndroidAsset sehingga path filesystem Linux tidak berlaku.
/// Solusi minimal: salin rekursif "Assets/**" dari AssetManager ke
/// &lt;filesDir&gt;/Assets lalu jadikan filesDir working directory.
/// Target: Assets/Player/player.png + semua .wav terbaca; SaveManager memakai
/// path data aplikasi (tidak diubah).
/// </summary>
internal static class AndroidAssetBootstrap
{
    public static string EnsureExtracted()
    {
        // global:: wajib: di dalam namespace AshenOath, nama "Android" akan
        // di-resolve ke AshenOath.Android (RootNamespace) bila tanpa kualifikasi.
        var context = global::Android.App.Application.Context
            ?? throw new InvalidOperationException("Application.Context null.");
        string filesDir = context.FilesDir?.AbsolutePath
            ?? throw new InvalidOperationException("FilesDir null.");
        string dstRoot = Path.Combine(filesDir, "Assets");
        int copied = CopyTree(context.Assets!, "Assets", dstRoot);
        Directory.SetCurrentDirectory(filesDir);
        Console.WriteLine($"[Assets] {copied} file siap di {dstRoot}.");
        return dstRoot;
    }

    private static int CopyTree(AssetManager assets, string assetPath, string dstPath)
    {
        string[] children;
        try
        {
            children = assets.List(assetPath) ?? [];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AssetManager.List gagal: {assetPath}", ex);
        }
        if (children.Length > 0)
        {
            Directory.CreateDirectory(dstPath);
            int total = 0;
            foreach (string child in children)
                total += CopyTree(assets, assetPath + "/" + child, Path.Combine(dstPath, child));
            return total;
        }
        // Daun: file bila bisa di-Open (dir kosong diabaikan).
        try
        {
            using var src = assets.Open(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
            bool same = false;
            if (File.Exists(dstPath))
            {
                try { same = new FileInfo(dstPath).Length == src.Length; }
                catch { same = false; }
            }
            if (!same)
            {
                using var dst = File.Create(dstPath);
                src.CopyTo(dst);
            }
            return 1;
        }
        catch
        {
            return 0;
        }
    }
}
