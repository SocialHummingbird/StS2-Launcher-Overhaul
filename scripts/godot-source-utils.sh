#!/usr/bin/env bash

apply_godot_patches() {
    local godot_dir="$1"
    local root="$2"
    local patch_dir="$root/patches/godot"

    if [ ! -d "$patch_dir" ]; then
        return 0
    fi

    local patch
    while IFS= read -r patch; do
        [ -n "$patch" ] || continue

        if (cd "$godot_dir" && git apply --check --ignore-whitespace "$patch" >/dev/null 2>&1); then
            echo "Applying Godot patch: $(basename "$patch")"
            (cd "$godot_dir" && git apply --ignore-whitespace "$patch")
            continue
        fi

        if (cd "$godot_dir" && git apply --reverse --check --ignore-whitespace "$patch" >/dev/null 2>&1); then
            echo "Godot patch already applied: $(basename "$patch")"
            continue
        fi

        echo "ERROR: Godot patch cannot be applied cleanly: $patch" >&2
        return 1
    done < <(find "$patch_dir" -maxdepth 1 -type f -name '*.patch' | sort)
}
