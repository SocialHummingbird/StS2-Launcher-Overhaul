#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT_DIR="${GODOT_DIR:-$ROOT/vendor/godot}"
GODOT_REPO="${GODOT_REPO:-https://github.com/godotengine/godot.git}"
GODOT_REF="${GODOT_REF:-4.5.1-stable}"
EXPECTED_GODOT_COMMIT="f62fdbde15035c5576dad93e586201f4d41ef0cb"

source "$ROOT/scripts/godot-source-utils.sh"

mkdir -p "$(dirname "$GODOT_DIR")"

if [ ! -d "$GODOT_DIR/.git" ]; then
    echo "Cloning Godot source: $GODOT_REPO#$GODOT_REF"
    git clone --depth 1 --branch "$GODOT_REF" "$GODOT_REPO" "$GODOT_DIR"
else
    echo "Godot source already exists at $GODOT_DIR"
fi

ACTUAL_GODOT_COMMIT="$(git -C "$GODOT_DIR" rev-parse HEAD)"
if [ "$ACTUAL_GODOT_COMMIT" != "$EXPECTED_GODOT_COMMIT" ]; then
    echo "ERROR: Godot checkout is $ACTUAL_GODOT_COMMIT, expected pinned 4.5.1-stable commit $EXPECTED_GODOT_COMMIT"
    exit 1
fi

apply_godot_patches "$GODOT_DIR" "$ROOT"

if [ ! -d "$ROOT/venv" ]; then
    python3 -m venv "$ROOT/venv"
fi

source "$ROOT/venv/bin/activate"
python3 -m pip install --require-hashes -r "$ROOT/scripts/requirements-godot-build.txt"

cat <<EOF

Godot source is ready.

For the emulator crash, the important requirement is that arm64 and x86_64
libgodot_android.so are built from the same engine checkout.

Then build both Android templates from the pinned checkout:

  scripts/build-godot.sh

EOF
