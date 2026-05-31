#!/usr/bin/env python3
"""Generate a minimal Godot 4.5 PCK containing the standalone launcher scene.

This bootstrap PCK lets the engine initialize normally (project settings,
.NET/Mono, GodotSharp) so the STS2Mobile launcher can run without game files.
"""

import os
import hashlib
import struct
from dataclasses import dataclass

# PCK format constants
MAGIC = 0x43504447  # "GDPC"
FORMAT_VERSION = 3
GODOT_MAJOR = 4
GODOT_MINOR = 5
GODOT_PATCH = 1
PACK_REL_FILEBASE = 0x02
ALIGNMENT = 32
HEADER_SIZE = 4 + 4 + 4 + 4 + 4 + 4 + 8 + 8 + (16 * 4)  # 104 bytes

# Minimal project.godot that enables .NET and sets a dummy main scene
PROJECT_GODOT = """\
; Minimal bootstrap project for STS2Mobile launcher
config_version=5
_custom_features="dotnet"

[application]

config/name="sts2"
config/features=PackedStringArray("4.5", "Forward Plus", "C#")
run/main_scene="res://bootstrap.tscn"

[display]

window/size/viewport_width=1920
window/size/viewport_height=1080
window/stretch/mode="canvas_items"
window/stretch/aspect="expand"
window/handheld/orientation=4

[dotnet]

project/assembly_name="sts2"

[rendering]

renderer/rendering_method="gl_compatibility"
renderer/rendering_method.mobile="gl_compatibility"
"""

BOOTSTRAP_SCENE = """\
[gd_scene load_steps=2 format=3]

[node name="BootstrapScene" type="Node"]
"""

VARIANT_INT = 2
VARIANT_STRING = 4
VARIANT_PACKED_STRING_ARRAY = 34

PROJECT_SETTINGS = [
    ("_custom_features", ("string", "dotnet")),
    ("application/config/name", ("string", "sts2")),
    ("application/config/features", ("packed_string_array", ["4.5", "Forward Plus", "C#"])),
    ("application/run/main_scene", ("string", "res://bootstrap.tscn")),
    ("display/window/size/viewport_width", ("int", 1920)),
    ("display/window/size/viewport_height", ("int", 1080)),
    ("display/window/stretch/mode", ("string", "canvas_items")),
    ("display/window/stretch/aspect", ("string", "expand")),
    ("display/window/handheld/orientation", ("int", 4)),
    ("dotnet/project/assembly_name", ("string", "sts2")),
    ("rendering/renderer/rendering_method", ("string", "gl_compatibility")),
    ("rendering/renderer/rendering_method.mobile", ("string", "gl_compatibility")),
]


@dataclass(frozen=True)
class BootstrapFile:
    path: str
    data: bytes


def align(offset, alignment=ALIGNMENT):
    return (offset + alignment - 1) & ~(alignment - 1)


def pad_string_len(s):
    """String length padded to 4-byte boundary."""
    encoded = s.encode("utf-8")
    padded = len(encoded) + ((4 - len(encoded) % 4) % 4)
    return padded, encoded


def encode_padded_string(value):
    encoded = value.encode("utf-8")
    return struct.pack("<I", len(encoded)) + encoded + b"\x00" * ((4 - len(encoded) % 4) % 4)


def encode_variant(value):
    kind, payload = value
    if kind == "int":
        return struct.pack("<II", VARIANT_INT, int(payload))
    if kind == "string":
        return struct.pack("<I", VARIANT_STRING) + encode_padded_string(payload)
    if kind == "packed_string_array":
        result = bytearray()
        result += struct.pack("<II", VARIANT_PACKED_STRING_ARRAY, len(payload))
        for item in payload:
            encoded = item.encode("utf-8") + b"\x00"
            result += struct.pack("<I", len(encoded))
            result += encoded
            result += b"\x00" * ((4 - len(encoded) % 4) % 4)
        return bytes(result)
    raise ValueError(f"Unsupported bootstrap project setting kind: {kind}")


def make_project_binary():
    data = bytearray(b"ECFG")
    data += struct.pack("<I", len(PROJECT_SETTINGS))
    for key, value in PROJECT_SETTINGS:
        key_bytes = key.encode("utf-8")
        variant = encode_variant(value)
        data += struct.pack("<I", len(key_bytes))
        data += key_bytes
        data += struct.pack("<I", len(variant))
        data += variant
    return bytes(data)


def make_bootstrap_files():
    return [
        BootstrapFile("res://project.binary", make_project_binary()),
        BootstrapFile("res://project.godot", PROJECT_GODOT.encode("utf-8")),
        BootstrapFile("res://bootstrap.tscn", BOOTSTRAP_SCENE.encode("utf-8")),
    ]


def encode_directory_entry(file, absolute_offset, file_base):
    file_md5 = hashlib.md5(file.data).digest()
    padded_len, path_bytes = pad_string_len(file.path)
    entry = bytearray()
    entry += struct.pack("<I", padded_len)
    entry += path_bytes + b"\x00" * (padded_len - len(path_bytes))
    entry += struct.pack("<Q", absolute_offset - file_base)
    entry += struct.pack("<Q", len(file.data))
    entry += file_md5
    entry += struct.pack("<I", 0)
    return bytes(entry)


def build_header(file_base, dir_base):
    header = bytearray()
    header += struct.pack("<I", MAGIC)
    header += struct.pack("<I", FORMAT_VERSION)
    header += struct.pack("<I", GODOT_MAJOR)
    header += struct.pack("<I", GODOT_MINOR)
    header += struct.pack("<I", GODOT_PATCH)
    header += struct.pack("<I", PACK_REL_FILEBASE)
    header += struct.pack("<Q", file_base)
    header += struct.pack("<Q", dir_base)
    header += b"\x00" * (16 * 4)

    assert len(header) == HEADER_SIZE
    return bytes(header)


def write_pck(output, header, file_base, file_payload, dir_base, dir_section):
    os.makedirs(os.path.dirname(output), exist_ok=True)
    with open(output, "wb") as f:
        f.write(header)
        f.write(b"\x00" * (file_base - HEADER_SIZE))
        file_end = file_base + len(file_payload)
        f.write(file_payload)
        f.write(b"\x00" * (dir_base - file_end))
        f.write(dir_section)


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output = os.path.join(script_dir, "..", "android", "assets", "bootstrap.pck")

    files = make_bootstrap_files()

    file_base = align(HEADER_SIZE)  # file data starts after header, aligned
    file_offsets = []
    current_file_offset = file_base
    file_payload = bytearray()
    dir_entries = bytearray()

    for file in files:
        file_offsets.append((file, current_file_offset))
        current_file_offset += len(file.data)

    dir_base = align(current_file_offset)  # directory starts after file data, aligned
    entry_count = len(files)

    for file, absolute_offset in file_offsets:
        dir_entries += encode_directory_entry(file, absolute_offset, file_base)

    for file, _ in file_offsets:
        file_payload.extend(file.data)

    dir_section = struct.pack("<I", entry_count) + dir_entries
    header = build_header(file_base, dir_base)
    write_pck(output, header, file_base, file_payload, dir_base, dir_section)

    size = os.path.getsize(output)
    print(f"Created bootstrap PCK: {output} ({size} bytes)")


if __name__ == "__main__":
    main()
