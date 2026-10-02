# Android Build — Ashen Oath

Jalur CI untuk membangun APK Android (`arm64-v8a`, Release) dari project C# SDL2
tanpa mengubah game desktop. Project desktop `Ashen_Oath.csproj` (`net9.0`)
tidak diubah; target Android tinggal di `Ashen_Oath.Android/` (`net9.0-android`).

Status saat ini: **pipeline APK lengkap** — SDL2/SDL2_image/SDL2_mixer
dibangun dari upstream di CI, glue SDLActivity resmi + entry managed,
asset di-bootstrap, APK Release `arm64-v8a` di-publish dan diupload sebagai
artifact. Runtime di perangkat **belum terverifikasi** (lihat "Status runtime").

## Cara menjalankan workflow

1. Commit + push file-file CI ini ke GitHub (dilakukan manual, bukan oleh agen).
2. Buka tab **Actions** → pilih **Android APK (arm64-v8a)** → **Run workflow**.
3. Tunggu job `build-apk` selesai (runner `ubuntu-latest` menyiapkan .NET 9,
   workload Android, JDK 17, Android SDK 35 + NDK r28c).

## Lokasi artifact APK

- Jika build sukses: artifact bernama **`Ashen-Oath-Android-APK`** berisi
  satu file `*-Signed.apk` dari
  `Ashen_Oath.Android/bin/Release/net9.0-android/publish/`
  (ditandatangani debug keystore — tanpa keystore pribadi pada tahap ini).

## Cara download APK dari Actions

1. Buka halaman run workflow yang sukses → bagian **Artifacts**.
2. Download `Ashen-Oath-Android-APK` (file zip) → ekstrak → dapatkan `.apk`.

## Cara install APK ke HP via ADB

```sh
adb install -r Ashen-Oath-Android-APK.apk
adb shell am start -n com.ashenoath.game/com.ashenoath.game.AshenOathSDLActivity
adb logcat -s AshenOath SDL AndroidRuntime
```

Log ber-tag `AshenOath` mencatat kesiapan surface, pemanggilan entry managed,
ekstraksi asset, dan exit code game. Tag `SDL` mencatat native SDL Android.
Jangan mengklaim runtime sukses tanpa pengujian nyata di perangkat.

## Perbedaan build desktop Termux vs Android CI

| Aspek | Desktop Termux | Android CI |
|---|---|---|
| Project | `Ashen_Oath.csproj` (`net9.0`) | `Ashen_Oath.Android/` (`net9.0-android`, head terpisah) |
| Runner | Perangkat (Termux+X11) | `ubuntu-latest` GitHub Actions |
| .NET | SDK 9.0.121 Termux (tanpa workload Android) | .NET 9 (`9.0.x`) + `dotnet workload install android` |
| JDK/SDK | Tidak ada | Bawaan runner (tanpa secret/path Termux/ADB pribadi) |
| Native SDL | `libSDL2*.so` sistem Termux (`sdl2 2.32.10`) | `libSDL2{,_image,_mixer}.so` arm64-v8a dari source resmi (SDL 2.32.10, image 2.8.12, mixer 2.8.2) |
| ABI | `aarch64` Linux | `arm64-v8a` saja |
| Signing | n/a | Debug keystore (tahap awal, tanpa secret) |

`dotnet build` di root project hanya membangun desktop dan tidak tersentuh
oleh file CI (tidak ada `Directory.Build.props`, tidak ada `.sln` baru,
`Ashen_Oath.csproj` tidak diubah).

## Dependency native SDL2 Android yang digunakan

- Binding C#: `ppy.SDL2-CS 1.0.82` (sama seperti desktop; `DllImport("SDL2")`).
- Interop milik game: `SdlImage.cs` (`DllImport("SDL2_image")`) dan
  `SdlMixer.cs` (`DllImport("SDL2_mixer")`) — di APK keduanya harus resolve ke
  `lib/arm64-v8a` di dalam APK, bukan ke library Termux.
- Yang wajib disediakan (lihat `Ashen_Oath.Android/native/android/arm64-v8a/README.md`):
  `libSDL2.so`, `libSDL2_image.so`, `libSDL2_mixer.so`, dibangun dari source
  resmi SDL line 2.32 (mis. tag `release-2.32.10`) + SDL_image/SDL_mixer 2.x
  dengan NDK (persyaratan SDL Android: SDK 35+, NDK r28c+).

## libSDL2.so — dibangun di CI dari upstream (tahap berjalan)

- `libSDL2.so` dibuat di CI dari SDL 2.32.10 upstream
  (`SDL2-2.32.10.tar.gz`, tag `release-2.32.10`), bukan dari Termux.
- Target ABI `arm64-v8a`, API 21, konfigurasi Release, via script
  `scripts/android/build-sdl2.sh` (CMake + `android.toolchain.cmake` NDK).
- Toolchain CI: NDK **r28c**, SDK Platform 35, Java Temurin 17
  (lihat `.github/workflows/android-apk.yml`).
- Binary hasil build **tidak disimpan ke repository** — hanya ada di workspace
  CI dan divalidasi (`file` = ELF 64-bit AArch64 shared object,
  `readelf -h` Machine = AArch64).

## libSDL2_image.so — dibangun di CI dari upstream (tahap berjalan)

- `libSDL2_image.so` dibuat di CI dari SDL_image 2.8.12 upstream
  (`SDL2_image-2.8.12.tar.gz`, tag `release-2.8.12`; bukan SDL3_image,
  bukan Termux, bukan stub), via `scripts/android/build-sdl2-image.sh`.
- Dibangun terhadap SDL2 Android tahap sebelumnya
  (`Ashen_Oath.Android/native/android/arm64-v8a/libSDL2.so` + header/config
  dari `sdl-prefix`, ditemukan via `SDL2_LIBRARY` + `SDL2_INCLUDE_DIR` (modul find privat 2.8.x, bukan `SDL2_DIR`) — tidak pernah dari
  `/usr/lib`, `/usr/local/lib`, Termux, atau host Ubuntu).
- Target ABI `arm64-v8a`, API 21, Release, shared.
- Codec minimal sesuai inspeksi game (`SdlImage.cs` hanya memakai
  `IMG_Init(INIT_PNG)` + `IMG_Load` + `IMG_Quit`; satu-satunya asset gambar
  adalah `Assets/Player/player.png`): **PNG saja** (dependency portable via
  mekanisme vendored upstream; JPEG/codec lain OFF).
- Validasi otomatis: `file` (ELF 64-bit AArch64 shared object), `readelf -h`
  (Machine AArch64), simbol `IMG_Init`/`IMG_Load`/`IMG_Quit` ada,
  `readelf -d` NEEDED wajib referensi `libSDL2` Android + hanya system libs
  Android (kontrak single-`.so`); gagal → exit 1.
- Binary **tidak disimpan ke repository** — hanya di workspace CI.
  `SDL2_mixer` menyusul tahap berikutnya.

## libSDL2_mixer.so — dibangun di CI dari upstream (tahap berjalan)

- `libSDL2_mixer.so` dibuat di CI dari SDL_mixer 2.8.2 upstream
  (`SDL2_mixer-2.8.2.tar.gz`, tag `release-2.8.2`; bukan SDL3_mixer,
  bukan Termux, bukan binary pihak ketiga, bukan stub),
  via `scripts/android/build-sdl2-mixer.sh`.
- Dibangun terhadap SDL2 Android tahap sebelumnya
  (`Ashen_Oath.Android/native/android/arm64-v8a/libSDL2.so` + header/config
  dari `sdl-prefix`, ditemukan via `SDL2_LIBRARY` + `SDL2_INCLUDE_DIR` (modul find privat 2.8.x, bukan `SDL2_DIR`) — tidak pernah dari
  `/usr/lib`, `/usr/local/lib`, host Ubuntu, atau Termux).
- Target ABI `arm64-v8a`, API 21, NDK r28c, Release, shared.
- Codec minimal sesuai inspeksi game (`SdlMixer.cs` memakai `Mix_Init(INIT_OGG)`
  + `Mix_LoadWAV`/`Mix_LoadMUS` + kontrol channel/music; 11/11 file audio
  adalah `.wav`, tanpa OGG/MP3/FLAC/MOD/MIDI): **WAV (built-in) + OGG Vorbis
  via STB in-tree** (tanpa lib eksternal; MP3/FLAC/MOD/MIDI/OPUS/WAVPACK OFF;
  dependency portable via vendored upstream).
- Validasi otomatis: `file` (ELF 64-bit AArch64 shared object), `readelf -h`
  (Machine AArch64), simbol `Mix_Init`/`Mix_OpenAudio`/`Mix_LoadWAV`/
  `Mix_LoadMUS`/`Mix_PlayChannel`/`Mix_PlayMusic`/`Mix_HasMusicDecoder` ada,
  `readelf -d` NEEDED wajib referensi `libSDL2` Android + hanya system libs
  Android (kontrak single-`.so`); gagal → exit 1.
- Binary **tidak disimpan ke repository** — hanya di workspace CI.

## SDLActivity resmi + managed entry point

- Glue Java = SDL 2.32.10 resmi verbatim
  (`Ashen_Oath.Android/src/main/java/org/libsdl/app/`, 9 file: `SDL`,
  `SDLActivity`, `SDLAudioManager`, `SDLControllerManager`, `SDLSurface`,
  `HIDDevice*` ×4) sebagai `AndroidJavaSource` — tanpa modifikasi.
- Launcher: `com.ashenoath.game.AshenOathSDLActivity extends SDLActivity`
  (landscape, fullscreen, lifecycle SDL penuh).
  `getLibraries()` → `SDL2`, `SDL2_image`, `SDL2_mixer` (tanpa `main`).
- `handleNativeState()` SDL bersifat static sehingga alur
  `SDLMain → nativeRunMain()` (yang butuh simbol native `SDL_main` dari
  `libmain.so`) tidak dipakai. Sebagai gantinya `onCreate()` mengisi
  `mSDLThread` (protected) dengan thread managed sebelum transisi RESUMED;
  thread menunggu gerbang kesiapan yang sama (surface + focus + resumed) lalu
  memanggil `GameEntry.runGame()` tepat satu kali. `Game.Run()` tetap
  satu-satunya game loop; desktop `Program.cs → Game.Run()` utuh.
- `libmain.so`: **tidak diperlukan dan tidak dibuat** — tidak ada stub;
  bridge native tidak mungkin tanpa Mono embedding API untuk runtime yang
  sudah berjalan.
- Entry managed: `GameEntry` (`[Register("com/ashenoath/game/GameEntry")`,
  `[Export("runGame")]`) — stub ACW resmi, tanpa JNI manual.

## Asset handling Android

Game membaca asset via path relatif (`Assets/...`, relatif-first —
`Player.ResolveAssetPath`, `AudioManager.ResolveAsset`). Di APK asset adalah
`AndroidAsset`, sehingga `AndroidAssetBootstrap` (head project saja)
menyalin rekursif `Assets/**` ke `<filesDir>/Assets` lalu menjadikan filesDir
working directory. Target: `Assets/Player/player.png` + semua `.wav` terbaca.
Format asset dan gameplay tidak diubah.

## Status runtime (jujur, tanpa workaround palsu)

1. **Eksekusi CI tertunda**: pipeline lengkap dan tervalidasi statis, tetapi
   run GitHub Actions + uji perangkat menunggu dispatch manual + HP nyata.
   Status jujur sampai ada artefak: **APK BUILD belum terkonfirmasi;
   DEVICE RUNTIME NOT YET VERIFIED**.
2. DllImport (`SDL2`, `SDL2_image`, `SDL2_mixer`) resolve dari
   `lib/arm64-v8a/` di APK; tidak ada absolute path Termux; desktop utuh.
