# Native SDL2 Android (arm64-v8a)

Direktori ini WAJIB berisi tiga library Android yang valid sebelum APK dapat dibangun:

- `libSDL2.so`
- `libSDL2_image.so`
- `libSDL2_mixer.so`

Gate MSBuild `CheckAndroidSdlNativeLibs` di `Ashen_Oath.Android.csproj`
menggagalkan build dengan error jelas bila file tersebut tidak ada.

## Aturan (tidak bisa ditawar)

- JANGAN menyalin `/data/data/com.termux/files/usr/lib/libSDL2*.so` ke sini.
  Itu library Linux/Termux (linker + libc Termux), bukan library Android.
- JANGAN memakai `.so` Linux desktop sebagai library Android.
- JANGAN membuat stub/fake `.so` agar build lolos.
- Binding C# tetap `ppy.SDL2-CS 1.0.82` (`DllImport("SDL2")`),
  interop `SDL2_image`/`SDL2_mixer` milik game (`SdlImage.cs`, `SdlMixer.cs`)
  me-resolve `libSDL2_image.so` / `libSDL2_mixer.so` dari `lib/arm64-v8a` di APK.

## Versi yang disarankan (tercatat, bukan "latest")

- SDL 2.32.x — segaris dengan lib Termux yang terbukti bekerja (`sdl2 2.32.10`),
  mis. tag upstream `release-2.32.10`.
- SDL_image 2.x dan SDL_mixer 2.x yang cocok dengan SDL tersebut.
- Toolchain: Android SDK 35+ dan NDK r28c+ (persyaratan SDL Android;
  dicatat dari spec proyek Tahap 21).

## Cara menyediakan (ringkas, dijalankan di mesin/CI Linux x64, bukan Termux)

1. Ambil source resmi SDL (+ SDL_image, SDL_mixer) pada tag di atas.
2. Bangun tiap library untuk ABI `arm64-v8a` memakai NDK, contoh sketsa:
   `cmake -DCMAKE_TOOLCHAIN_FILE="$NDK/build/cmake/android.toolchain.cmake"
   -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-35 ...`
   atau ikuti `android-project` / dokumen build resmi tiap repo SDL.
3. Taruh hasilnya tepat pada tiga path yang dicek gate, lalu jalankan ulang build.

## Setelah `.so` tersedia (pekerjaan lanjutan, sudah didokumentasikan)

APK juga membutuhkan Java glue `org.libsdl.app/SDLActivity` + bridge managed
agar `SDL_Init(SDL_INIT_VIDEO)` dan surface lifecycle bekerja — lihat
`docs/android-build.md`. Gate ini hanya menjaga validitas native library.
