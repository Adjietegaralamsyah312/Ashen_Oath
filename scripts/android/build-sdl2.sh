#!/usr/bin/env bash
#
# Bangun SDL2 untuk Android arm64-v8a dari source resmi upstream memakai NDK.
# Dijalankan dari root repository di GitHub Actions (ubuntu-latest).
# Hasil: Ashen_Oath.Android/native/android/arm64-v8a/libSDL2.so (workspace CI saja,
# JANGAN di-commit sebagai binary).
#
# Environment yang diterima:
#   ANDROID_NDK_HOME            (wajib)
#   ANDROID_SDK_ROOT | ANDROID_HOME (wajib, salah satu; validasi toolchain)
#   SDL_CACHE_DIR               (opsional; default .sdl-cache di repo root)
set -euo pipefail

SDL_VERSION="2.32.10"
SDL_URL="https://github.com/libsdl-org/SDL/releases/download/release-2.32.10/SDL2-2.32.10.tar.gz"
OUT="Ashen_Oath.Android/native/android/arm64-v8a/libSDL2.so"
CACHE_DIR="${SDL_CACHE_DIR:-.sdl-cache}"
TARBALL="$CACHE_DIR/SDL2-2.32.10.tar.gz"

log() { echo "[sdl2-android] $*"; }
fail() { echo "[sdl2-android] ERROR: $*" >&2; exit 1; }

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
command -v tar >/dev/null || fail "'tar' tidak ditemukan."
log "NDK: $ANDROID_NDK_HOME"
log "toolchain: $TOOLCHAIN"

# 2. Download SDL2 2.32.10 (pakai cache bila sudah ada dan tidak kosong).
mkdir -p "$CACHE_DIR"
if [ -s "$TARBALL" ]; then
    log "cache hit: $TARBALL ($(du -h "$TARBALL" | cut -f1))."
else
    log "download: $SDL_URL"
    if command -v curl >/dev/null; then
        curl -fsSL -o "$TARBALL" "$SDL_URL"
    elif command -v wget >/dev/null; then
        wget -O "$TARBALL" "$SDL_URL"
    else
        fail "butuh curl atau wget untuk download."
    fi
fi

# 3. Verifikasi download tidak kosong.
[ -s "$TARBALL" ] || fail "download kosong/rusak: $TARBALL"
log "tarball: $TARBALL ($(du -h "$TARBALL" | cut -f1))."

# 4. Extract source.
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
tar -xzf "$TARBALL" -C "$WORK"
SRC="$WORK/SDL2-2.32.10"
[ -d "$SRC" ] || fail "direktori source tidak ditemukan setelah extract: $SRC"
[ -f "$SRC/CMakeLists.txt" ] || fail "CMakeLists.txt tidak ditemukan di $SRC."
log "source: $SRC"

# 5. Configure dengan CMake Android toolchain (arm64-v8a, API 21, Release).
#    Opsi SDL yang masuk akal untuk mobile; dependency desktop Linux dimatikan.
#    Jangan memakai system SDL.
BUILD_DIR="$WORK/build"
cmake -S "$SRC" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_TOOLCHAIN_FILE="$TOOLCHAIN" \
    -DANDROID_ABI=arm64-v8a \
    -DANDROID_PLATFORM=android-21 \
    -DCMAKE_BUILD_TYPE=Release \
    -DSDL_SHARED=ON \
    -DSDL_STATIC=OFF \
    -DSDL_TEST=OFF \
    -DSDL_X11=OFF \
    -DSDL_WAYLAND=OFF \
    -DSDL_ALSA=OFF \
    -DSDL_PULSEAUDIO=OFF \
    -DSDL_JACK=OFF \
    -DSDL_PIPEWIRE=OFF

# 6. Build Release.
cmake --build "$BUILD_DIR"

# 7. Cari hasil libSDL2.so secara deterministik (jangan mengandalkan 1 path).
SO_CANDIDATE="$(find "$BUILD_DIR" -type f -name 'libSDL2.so' | sort | head -n 1 || true)"
if [ -z "$SO_CANDIDATE" ]; then
    SO_CANDIDATE="$(find "$BUILD_DIR" -type f -name 'libSDL2-*.so*' | sort | head -n 1 || true)"
fi
[ -n "$SO_CANDIDATE" ] || fail "libSDL2.so tidak ditemukan di $BUILD_DIR."
log "hasil build: $SO_CANDIDATE"

# 8. Validasi native library (otomatis; gagal -> exit 1).
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

log "validasi hasil build:"
validate_so "$SO_CANDIDATE" || fail "validasi libSDL2.so hasil build GAGAL."

# 9. Salin hanya hasil Android ARM64 ke lokasi target, lalu validasi ulang.
mkdir -p "$(dirname "$OUT")"
cp -L "$SO_CANDIDATE" "$OUT"
log "disalin ke: $OUT"
log "validasi ulang file target:"
validate_so "$OUT" || fail "validasi $OUT GAGAL."

# 10. Install header + config CMake SDL2 ke prefix build-time agar library
#     berikutnya (SDL2_image, ...) menemukan SDL2 Android via SDL2_DIR — bukan
#     dari /usr/lib, /usr/local/lib, Termux, atau host Ubuntu.
#     Prefix ini hanya ada di workspace CI (tidak di-commit).
PREFIX="Ashen_Oath.Android/native/android/sdl-prefix"
cmake --install "$BUILD_DIR" --prefix "$PREFIX"
[ -d "$PREFIX/include/SDL2" ] || fail "header SDL2 tidak terinstall di $PREFIX/include/SDL2."
log "SDL2 terinstall ke prefix: $PREFIX"
log "SELESAI: $OUT adalah ELF 64-bit AArch64 shared object (hasil NDK)."
