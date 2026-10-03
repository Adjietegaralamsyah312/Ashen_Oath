package com.ashenoath.game;

import android.os.Bundle;
import android.util.Log;
import org.libsdl.app.SDLActivity;

/**
 * Launcher Activity Ashen Oath (satu-satunya entry path Android).
 *
 * Arsitektur (sesuai SDL 2.32.10, tanpa modifikasi glue resmi):
 *  - SDLActivity menyiapkan JNI, library loading, surface, input, lifecycle.
 *  - getLibraries() resmi di-override: SDL2, SDL2_image, SDL2_mixer
 *    (TANPA "main": tidak ada native main untuk arsitektur ini).
 *  - handleNativeState() SDL adalah static sehingga tidak dapat di-override.
 *    Sebagai gantinya, onCreate() mengisi mSDLThread (protected static) dengan
 *    thread managed SEBELUM transisi RESUMED pertama. Akibatnya alur resmi
 *    SDLMain -> nativeRunMain() (yang membutuhkan simbol native SDL_main dari
 *    libmain.so) TIDAK PERNAH berjalan; cabang PAUSED/RESUMED yang tersisa
 *    hanya memakai semafor + surface callback resmi (aman: semafor dibuat saat
 *    nativeSetupJNI di onCreate, sebelum transisi apa pun).
 *  - Thread managed menunggu gerbang kesiapan yang SAMA dengan SDL
 *    (surface ready + focus + resumed) sehingga Game.Run()/SDL_Init() tidak
 *    pernah berjalan terlalu dini, lalu memanggil SATU-SATUNYA entry managed
 *    via bridge ACW GameEntryBridge (tepat satu kali).
 *    Java TIDAK mengimpor class C# secara langsung: javac (AndroidJavaSource)
 *    berjalan sebelum ACW dibuat, sehingga referensi langsung menyebabkan
 *    "cannot find symbol". Pemanggilan memakai reflection Class.forName()
 *    saat runtime, ketika ACW sudah ada di classes.dex dan runtime .NET
 *    (aktif sejak Application.onCreate, jauh sebelum activity) sudah siap.
 *  - Game.Run() tetap satu-satunya game loop. Tidak ada loop kedua.
 */
public class AshenOathSDLActivity extends SDLActivity {
    private static final String TAG = "AshenOath";

    @Override
    protected String[] getLibraries() {
        return new String[] {
            "SDL2",
            "SDL2_image",
            "SDL2_mixer"
        };
    }

    @Override
    protected String[] getArguments() {
        return new String[0];
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Pre-set thread managed agar handleNativeState() statis tidak pernah
        // membuat SDLMain thread (yang akan memanggil nativeRunMain/libmain).
        if (mSDLThread == null) {
            mSDLThread = new Thread(new ManagedMain(), "SDLThread");
            mSDLThread.start();
        }
    }

    /**
     * Setara SDLMain.run() untuk arsitektur managed: menunggu kesiapan yang
     * sama (surface + focus + resumed), menjalankan entry managed tepat satu
     * kali, lalu meniru ekor SDLMain (bersihkan thread + finish activity).
     * CATATAN: member protected SDL diakses via AshenOathSDLActivity (aturan
     * protected Java untuk subclass beda package; kualifikasi SDLActivity.X
     * tidak legal di sini kecuali member public).
     */
    static class ManagedMain implements Runnable {
        @Override
        public void run() {
            try {
                android.os.Process.setThreadPriority(
                    android.os.Process.THREAD_PRIORITY_DISPLAY);
            } catch (Exception e) {
                Log.v(TAG, "modify thread properties failed " + e.toString());
            }

            Log.v(TAG, "Waiting for surface/focus/resume before managed entry");
            while (!isReady() && !isGone()) {
                try {
                    Thread.sleep(50);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                    return;
                }
            }

            if (!isGone()) {
                Log.v(TAG, "Running managed entry via GameEntryBridge (reflection)");
                int exitCode = callManagedEntry();
                Log.v(TAG, "Finished managed entry (exit=" + exitCode + ")");
            } else {
                Log.v(TAG, "Activity gone before managed entry; aborting");
            }

            if (AshenOathSDLActivity.mSingleton != null && !AshenOathSDLActivity.mSingleton.isFinishing()) {
                AshenOathSDLActivity.mSDLThread = null;
                AshenOathSDLActivity.mSingleton.finish();
            }
        }

        /**
         * Memanggil bridge ACW C# via reflection (BUKAN import langsung).
         * ACW com.ashenoath.game.GameEntryBridge dibuat oleh build .NET dari
         * [Register]+[Export]; javac tidak melihatnya, tetapi saat runtime
         * (paska-gerbang kesiapan, runtime .NET aktif) class tersedia di
         * classes.dex. Gagal → 99 (setara crash managed), tanpa loop kedua.
         */
        private static int callManagedEntry() {
            try {
                Class<?> bridge = Class.forName("com.ashenoath.game.GameEntryBridge");
                Object result = bridge.getMethod("runGame").invoke(null);
                return ((Integer) result).intValue();
            } catch (Exception e) {
                Log.e(TAG, "Managed bridge call failed: " + e);
                return 99;
            }
        }

        /** Gerbang kesiapan yang sama dengan cabang RESUMED SDL resmi. */
        private static boolean isReady() {
            return AshenOathSDLActivity.mSurface != null
                && AshenOathSDLActivity.mSurface.mIsSurfaceReady
                && SDLActivity.mHasFocus
                && SDLActivity.mIsResumedCalled;
        }

        private static boolean isGone() {
            return AshenOathSDLActivity.mSingleton == null
                || AshenOathSDLActivity.mSingleton.isFinishing();
        }
    }
}
