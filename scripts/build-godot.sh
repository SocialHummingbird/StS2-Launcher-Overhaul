#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT_DIR="${GODOT_DIR:-$ROOT/vendor/godot}"
ANDROID_LIBS="$ROOT/android/libs/release"
ARCHES="${ARCHES:-arm64 x86_64}"
EXPECTED_GODOT_VERSION="${EXPECTED_GODOT_VERSION:-4.5.1-stable}"

export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
if [ -z "${ANDROID_NDK_ROOT:-}" ]; then
    LATEST_NDK=$(ls -1 "$ANDROID_HOME/ndk" | sort -V | tail -1)
    export ANDROID_NDK_ROOT="$ANDROID_HOME/ndk/$LATEST_NDK"
fi

if [ ! -d "$GODOT_DIR" ]; then
    echo "ERROR: Expected Godot source checkout at $GODOT_DIR"
    echo "Run scripts/setup-godot-source.sh, or set GODOT_DIR to the custom patched Godot checkout."
    exit 1
fi

if [ ! -f "$ROOT/venv/bin/activate" ]; then
    echo "ERROR: Expected Python virtualenv at $ROOT/venv"
    exit 1
fi

source "$ROOT/venv/bin/activate"

jobs="$(nproc 2>/dev/null || echo 4)"

if [ -f "$GODOT_DIR/version.py" ]; then
    actual_version="$(
        cd "$GODOT_DIR"
        python3 - <<'PY'
scope = {}
with open("version.py", "r", encoding="utf-8") as f:
    exec(f.read(), scope)
print(f"{scope.get('major')}.{scope.get('minor')}.{scope.get('patch')}-{scope.get('status')}")
PY
    )"
    if [ "$actual_version" != "$EXPECTED_GODOT_VERSION" ]; then
        echo "ERROR: Godot checkout is $actual_version, expected $EXPECTED_GODOT_VERSION"
        echo "Set EXPECTED_GODOT_VERSION to override intentionally."
        exit 1
    fi
fi

for ARCH in $ARCHES; do
    case "$ARCH" in
        arm64)
            ABI="arm64-v8a"
            ;;
        x86_64)
            ABI="x86_64"
            ;;
        *)
            echo "ERROR: Unsupported Android Godot arch: $ARCH"
            exit 1
            ;;
    esac

    echo "Building Godot (android $ARCH template_release)..."
    cd "$GODOT_DIR"
    scons platform=android arch="$ARCH" target=template_release module_mono_enabled=yes -j"$jobs"

    BUILT_SO="$GODOT_DIR/platform/android/java/lib/libs/release/$ABI/libgodot_android.so"
    if [ ! -f "$BUILT_SO" ]; then
        echo "ERROR: Expected output not found at $BUILT_SO"
        exit 1
    fi

    echo "Updating libgodot_android.so for $ABI..."
    TMPDIR=$(mktemp -d)
    mkdir -p "$TMPDIR/jni/$ABI" "$ANDROID_LIBS/$ABI"
    cp "$BUILT_SO" "$TMPDIR/jni/$ABI/libgodot_android.so"
    (cd "$TMPDIR" && zip -u "$ANDROID_LIBS/godot-lib.template_release.aar" "jni/$ABI/libgodot_android.so")
    rm -rf "$TMPDIR"

    cp "$BUILT_SO" "$ANDROID_LIBS/$ABI/libgodot_android.so"
done

echo "Godot engine rebuild complete for: $ARCHES"
