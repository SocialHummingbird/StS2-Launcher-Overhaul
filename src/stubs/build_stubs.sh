#!/bin/bash
set -e

NDK_ROOT="${ANDROID_NDK_ROOT:-$HOME/Android/Sdk/ndk/28.1.13356709}"
TARGET_API="24"
TARGET_ABI="arm64-v8a"
TOOLCHAIN_HOST="linux-x86_64"

CC="$NDK_ROOT/toolchains/llvm/prebuilt/$TOOLCHAIN_HOST/bin/aarch64-linux-android$TARGET_API-clang"
OUT="out/$TARGET_ABI"

build_shared() {
    local output="$1"
    local soname="$2"
    shift 2

    "$CC" -shared -o "$output" "$@" -Wl,-soname,"$soname"
}

mkdir -p "$OUT"

build_shared "$OUT/libsteam_api.so" "libsteam_api.so" steam_stub.c steam_stub_auto.c
build_shared "$OUT/libsentry.so" "libsentry.so" sentry_stub.c

ls -lh "$OUT/"
