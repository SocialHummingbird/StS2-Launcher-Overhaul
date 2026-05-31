#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT_DIR="${GODOT_DIR:-$ROOT/vendor/godot}"
GODOT_REPO="${GODOT_REPO:-https://github.com/godotengine/godot.git}"
GODOT_REF="${GODOT_REF:-4.5.1-stable}"

source "$ROOT/scripts/godot-source-utils.sh"

mkdir -p "$(dirname "$GODOT_DIR")"

if [ ! -d "$GODOT_DIR/.git" ]; then
    echo "Cloning Godot source: $GODOT_REPO#$GODOT_REF"
    git clone --depth 1 --branch "$GODOT_REF" "$GODOT_REPO" "$GODOT_DIR"
else
    echo "Godot source already exists at $GODOT_DIR"
fi

apply_godot_patches "$GODOT_DIR" "$ROOT"

if [ ! -d "$ROOT/venv" ]; then
    python3 -m venv "$ROOT/venv"
fi

source "$ROOT/venv/bin/activate"
python3 -m pip install --upgrade pip
python3 -m pip install scons

cat <<EOF

Godot source is ready.

For the emulator crash, the important requirement is that arm64 and x86_64
libgodot_android.so are built from the same engine checkout.

If you have the original patched engine fork, use it instead:

  GODOT_REPO=<custom-fork-url> GODOT_REF=<custom-ref> scripts/setup-godot-source.sh

Then build both Android templates:

  scripts/build-godot.sh

EOF
