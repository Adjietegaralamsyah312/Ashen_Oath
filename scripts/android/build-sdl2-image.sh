#!/usr/bin/env bash
#
# Bangun SDL2_image 2.8.12 untuk Android arm64-v8a dari source resmi upstream,
# di-link terhadap SDL2 Android yang sudah dibangun tahap sebelumnya.
# Dijalankan dari root repository di GitHub Actions (ubuntu-latest), SETELAH
# scripts/android/build-sdl2.sh (yang menyediakan libSDL2.so + sdl-prefix).
# Hasil: Ashen_Oath.Android/native/android/arm64-v8a/libSDL2_image.so
# (workspace CI saja, JANGAN di-commit sebagai binary).
#
# Codec: PNG saja — sesuai inspeksi source game (SdlImage.cs hanya memakai
# IMG_Init(INIT_PNG) + IMG_Load + IMG_Quit; satu-satunya asset gambar adalah
# Assets/Player/player.png). JPEG/codec lain TIDAK diaktifkan.
# Dependency portable memakai mekanisme vendored upstream SDL_image
# (bukan system libraries Linux, bukan Termux).
#
# Environment yang diterima:
#   ANDROID_NDK_HOME            (wajib)
#   ANDROID_SDK_ROOT | ANDROID_HOME (wajib, salah satu; validasi toolchain)
#   SDLIMG_CACHE_DIR            (opsional; default .sdl-cache di repo root)
set -euo pipefail

SDLIMAGE_VERSION="2.8.12"
SDLIMAGE_URL="https://github.com/libsdl-org/SDL_image/releases/download/release-2.8.12/SDL2_image-2.8.12.tar.gz"
OUT="Ashen_Oath.Android/native/android/arm64-v8a/libSDL2_image.so"
SDL2_SO="Ashen_Oath.Android/native/android/arm64-v8a/libSDL2.so"
SDL2_PREFIX="Ashen_Oath.Android/native/android/sdl-prefix"
CACHE_DIR="${SDLIMG_CACHE_DIR:-.sdl-cache}"
TARBALL="$CACHE_DIR/SDL2_image-2.8.12.tar.gz"

log() { echo "[sdl2image-android] $*"; }
fail() { echo "[sdl2image-android] ERROR: $*" >&2; exit 1; }

# 0. Harus dijalankan dari root repository.
[ -d "Ashen_Oath.Android" ] || fail "jalankan dari root repository (Ashen_Oath.Android tidak ditemukan)."

# 1. Validasi environment + tooling.
: "${ANDROID_NDK_HOME:?ANDROID_NDK_HOME harus di-set (direktori NDK r28c).}"
if [ -z "${ANDROID_SDK_ROOT:-}" ] && [ -z "${ANDROID_HOME:-}" ]; then
    fail "ANDROID_SDK_ROOT atau ANDROID_HOME harus di-set."
fi
TOOLCHAIN="$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake"
[ -f "$TOOLCHAIN" ] || fail "toolchain NDK tidak ditemukan: $TOOLCHAIN"
command -v cmake >/dev/null || fail "cmake tidak ditemukan."
command -v ninja >/dev/null || fail "ninja tidak ditemukan."
command -v file >/dev/null || fail "'file' tidak ditemukan."
command -v readelf >/dev/null || fail "'readelf' tidak ditemukan."
command -v nm >/dev/null || fail "'nm' tidak ditemukan."
command -v tar >/dev/null || fail "'tar' tidak ditemukan."

# 2. SDL2 Android prasyarat harus sudah ada (dibangun tahap sebelumnya).
[ -s "$SDL2_SO" ] || fail "libSDL2.so Android tidak ditemukan di $SDL2_SO. Jalankan scripts/android/build-sdl2.sh terlebih dahulu."
[ -d "$SDL2_PREFIX/include/SDL2" ] || fail "header SDL2 tidak ditemukan di $SDL2_PREFIX/include/SDL2. Jalankan scripts/android/build-sdl2.sh terlebih dahulu."
SDL2_CMAKE_CONFIG="$(find "$SDL2_PREFIX" -name 'SDL2Config.cmake' | sort | head -n 1 || true)"
[ -n "$SDL2_CMAKE_CONFIG" ] || fail "SDL2Config.cmake tidak ditemukan di $SDL2_PREFIX. Jalankan scripts/android/build-sdl2.sh terlebih dahulu."
SDL2_DIR="$(dirname "$SDL2_CMAKE_CONFIG")"
SDL2_LIB="$SDL2_PREFIX/lib/libSDL2.so"
SDL2_INC="$SDL2_PREFIX/include/SDL2"
[ -f "$SDL2_LIB" ] || fail "libSDL2.so tidak ditemukan di $SDL2_LIB. Jalankan scripts/android/build-sdl2.sh terlebih dahulu."
log "SDL2 Android: $SDL2_SO"
log "SDL2_DIR: $SDL2_DIR"
log "SDL2_LIBRARY: $SDL2_LIB"
log "SDL2_INCLUDE_DIR: $SDL2_INC"

# 3. Download SDL2_image 2.8.12 (pakai cache bila sudah ada dan tidak kosong).
mkdir -p "$CACHE_DIR"
if [ -s "$TARBALL" ]; then
    log "cache hit: $TARBALL ($(du -h "$TARBALL" | cut -f1))."
else
    log "download: $SDLIMAGE_URL"
    if command -v curl >/dev/null; then
        curl -fsSL -o "$TARBALL" "$SDLIMAGE_URL"
    elif command -v wget >/dev/null; then
        wget -O "$TARBALL" "$SDLIMAGE_URL"
    else
        fail "butuh curl atau wget untuk download."
    fi
fi

# 4. Verifikasi archive tidak kosong.
[ -s "$TARBALL" ] || fail "download kosong/rusak: $TARBALL"
log "tarball: $TARBALL ($(du -h "$TARBALL" | cut -f1))."

# 5. Extract source (reproducible: verifikasi top-level + CMakeLists).
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
tar -xzf "$TARBALL" -C "$WORK"
SRC="$WORK/SDL2_image-2.8.12"
[ -d "$SRC" ] || fail "direktori source tidak ditemukan setelah extract: $SRC"
[ -f "$SRC/CMakeLists.txt" ] || fail "CMakeLists.txt SDL_image tidak ditemukan di $SRC (bukan source 2.8.x?)."
log "source: $SRC"

# 6. Configure dengan CMake Android toolchain.
#    SDL_image 2.8.12 memakai modul find privat (cmake/FindPrivateSDL2.cmake)
#    yang mencari SDL2_LIBRARY + SDL2_INCLUDE_DIR (BUKAN SDL2_DIR) — keduanya
#    diarahkan eksplisit ke prefix Android. Jangan sampai CMake mengambil SDL2
#    dari /usr/lib, /usr/local/lib, Termux, atau host Ubuntu.
#    Codec minimal: PNG saja (kebutuhan game). Dependency portable via VENDORED
#    upstream. Variabel -D yang tidak dikenal versi ini hanya warning CMake.
BUILD_DIR="$WORK/build"
cmake -S "$SRC" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_TOOLCHAIN_FILE="$TOOLCHAIN" \
    -DANDROID_ABI=arm64-v8a \
    -DANDROID_PLATFORM=android-21 \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_PREFIX_PATH="$SDL2_PREFIX" \
    -DSDL2_DIR="$SDL2_DIR" \
    -DSDL2_LIBRARY="$SDL2_LIB" \
    -DSDL2_INCLUDE_DIR="$SDL2_INC" \
    -DBUILD_SHARED_LIBS=ON \
    -DSDL2IMAGE_PNG=ON \
    -DSDL2IMAGE_JPG=OFF \
    -DSDL2IMAGE_TIF=OFF \
    -DSDL2IMAGE_WEBP=OFF \
    -DSDL2IMAGE_AVIF=OFF \
    -DSDL2IMAGE_JXL=OFF \
    -DSDL2IMAGE_QOI=OFF \
    -DSDL2IMAGE_VENDORED=ON \
    -DSDL2IMAGE_DEPS_SHARED=OFF \
    -DSDL2IMAGE_SAMPLES=OFF \
    -DSDL2IMAGE_TESTS=OFF
log "SDL2 yang dipakai CMake (wajib dari prefix Android):"
SDL2_LIB_USED="$(grep -E '^SDL2_LIBRARY:' "$BUILD_DIR/CMakeCache.txt" | cut -d= -f2- || true)"
SDL2_INC_USED="$(grep -E '^SDL2_INCLUDE_DIR:' "$BUILD_DIR/CMakeCache.txt" | cut -d= -f2- || true)"
log "SDL2_LIBRARY=$SDL2_LIB_USED"
log "SDL2_INCLUDE_DIR=$SDL2_INC_USED"
[ -n "$SDL2_LIB_USED" ] || fail "SDL2_LIBRARY tidak tercatat di CMakeCache."
[ -n "$SDL2_INC_USED" ] || fail "SDL2_INCLUDE_DIR tidak tercatat di CMakeCache."
case "$SDL2_LIB_USED" in
    *sdl-prefix*) ;;
    *) fail "SDL2_LIBRARY bukan dari prefix Android: $SDL2_LIB_USED" ;;
esac
case "$SDL2_INC_USED" in
    *sdl-prefix*) ;;
    *) fail "SDL2_INCLUDE_DIR bukan dari prefix Android: $SDL2_INC_USED" ;;
esac

# 7. Build Release.
cmake --build "$BUILD_DIR"

# 8. Cari hasil libSDL2_image.so secara deterministik.
SO_CANDIDATE="$(find "$BUILD_DIR" -type f -name 'libSDL2_image.so' | sort | head -n 1 || true)"
[ -n "$SO_CANDIDATE" ] || fail "libSDL2_image.so tidak ditemukan di $BUILD_DIR."
log "hasil build: $SO_CANDIDATE"

# 9. Validasi otomatis: ELF 64-bit AArch64 shared object (gagal -> exit 1).
validate_so() {
    local f="$1"
    [ -s "$f" ] || { echo "file kosong: $f" >&2; return 1; }
    local info
    info="$(file -b "$f")"
    echo "$info"
    case "$info" in
        *ELF\ 64-bit*aarch64*) ;;
        *) echo "bukan ELF 64-bit AArch64: $f" >&2; return 1 ;;
    esac
    case "$info" in
        *shared\ object*) ;;
        *) echo "bukan shared object: $f" >&2; return 1 ;;
    esac
    case "$info" in
        *x86-64* | *x86_64* | *80386* | *32-bit* | *ARM\ EABI*)
            echo "ABI salah (bukan arm64-v8a): $f" >&2; return 1 ;;
    esac
    local machine
    machine="$(readelf -h "$f" | awk -F: '/Machine:/{print $2}')"
    echo "Machine:$machine"
    case "$machine" in
        *AArch64*) ;;
        *) echo "readelf Machine bukan AArch64: $f" >&2; return 1 ;;
    esac
    return 0
}

log "validasi hasil build (file/readelf -h):"
validate_so "$SO_CANDIDATE" || fail "validasi libSDL2_image.so hasil build GAGAL."

# 10. Simbol API yang dipakai game harus ada (IMG_Init/IMG_Load/IMG_Quit).
for sym in IMG_Init IMG_Load IMG_Quit; do
    nm -D "$SO_CANDIDATE" | grep -q " $sym$" \
        || fail "simbol $sym tidak ditemukan di $SO_CANDIDATE."
    log "simbol OK: $sym"
done

# 11. Dependency check via readelf -d: wajib referensi libSDL2 Android yang
#     akan dipackage; hanya system libs Android (+ C++ NDK bila ada) yang boleh
#     menemani single-.so ini. Library host/desktop = gagal.
log "dependency (readelf -d):"
readelf -d "$SO_CANDIDATE"
NEEDED="$(readelf -d "$SO_CANDIDATE" | awk -F'[][]' '/NEEDED/{print $2}')"
log "NEEDED: $NEEDED"
echo "$NEEDED" | grep -q 'libSDL2' \
    || fail "NEEDED tidak mereferensikan libSDL2 Android."
echo "$NEEDED" | grep -qE '/usr/lib|/usr/local|termux|/data/data' \
    && fail "NEEDED menunjuk library host/Termux (bukan Android target)."
while IFS= read -r lib; do
    [ -z "$lib" ] && continue
    case "$lib" in
        libSDL2.so | \
        libc.so | libm.so | libdl.so | liblog.so | libandroid.so | \
        libEGL.so | libGLESv1_CM.so | libGLESv2.so | libGLESv3.so | \
        libOpenSLES.so | libz.so | libjnigraphics.so | libmediandk.so | \
        libcamera2ndk.so | libnativewindow.so | libsync.so | libvulkan.so | \
        libaaudio.so | libamidi.so | libc++_shared.so) ;;
        *) fail "dependency tak dikenal/bukan system Android: $lib (kontrak single-.so dilanggar)." ;;
    esac
done <<< "$NEEDED"
log "dependency OK: hanya libSDL2 + system libs Android."

# 12. Salin hasil valid ke lokasi target, lalu validasi ulang.
mkdir -p "$(dirname "$OUT")"
cp -L "$SO_CANDIDATE" "$OUT"
log "disalin ke: $OUT"
log "validasi ulang file target:"
validate_so "$OUT" || fail "validasi $OUT GAGAL."
log "SELESAI: $OUT adalah ELF 64-bit AArch64 shared object (hasil NDK, PNG, link SDL2 Android)."
