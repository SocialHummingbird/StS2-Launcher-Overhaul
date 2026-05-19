#!/usr/bin/env python3
"""Generate a minimal Godot 4.5 PCK containing the standalone launcher scene.

This bootstrap PCK lets the engine initialize normally (project settings,
.NET/Mono, GodotSharp) so the STS2Mobile launcher can run without game files.
"""

import hashlib
import struct
import sys
import os

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


def align(offset, alignment=ALIGNMENT):
    return (offset + alignment - 1) & ~(alignment - 1)


def pad_string_len(s):
    """String length padded to 4-byte boundary."""
    encoded = s.encode("utf-8")
    padded = len(encoded) + ((4 - len(encoded) % 4) % 4)
    return padded, encoded


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output = os.path.join(script_dir, "..", "android", "assets", "bootstrap.pck")

    files = [
        ("res://project.godot", PROJECT_GODOT.encode("utf-8")),
        ("res://bootstrap.tscn", BOOTSTRAP_SCENE.encode("utf-8")),
    ]

    file_base = align(HEADER_SIZE)  # file data starts after header, aligned
    file_offsets = []
    current_file_offset = file_base
    file_payload = bytearray()
    dir_entries = bytearray()

    for file_path, file_data in files:
        file_offsets.append((file_path, file_data, current_file_offset))
        current_file_offset += len(file_data)

    dir_base = align(current_file_offset)  # directory starts after file data, aligned
    entry_count = len(files)

    for file_path, file_data, absolute_offset in file_offsets:
        file_md5 = hashlib.md5(file_data).digest()
        padded_len, path_bytes = pad_string_len(file_path)
        dir_entry = struct.pack("<I", padded_len)
        dir_entry += path_bytes + b"\x00" * (padded_len - len(path_bytes))
        dir_entry += struct.pack("<Q", absolute_offset - file_base)  # offset relative to file_base
        dir_entry += struct.pack("<Q", len(file_data))  # file size
        dir_entry += file_md5  # 16 bytes MD5
        dir_entry += struct.pack("<I", 0)  # flags (no encryption)
        dir_entries += dir_entry

    for _, file_data, _ in file_offsets:
        file_payload.extend(file_data)

    dir_section = struct.pack("<I", entry_count) + dir_entries

    # Build header
    # Build header
    header = struct.pack("<I", MAGIC)
    header += struct.pack("<I", FORMAT_VERSION)
    header += struct.pack("<I", GODOT_MAJOR)
    header += struct.pack("<I", GODOT_MINOR)
    header += struct.pack("<I", GODOT_PATCH)
    header += struct.pack("<I", PACK_REL_FILEBASE)
    header += struct.pack("<Q", file_base)  # file_base offset
    header += struct.pack("<Q", dir_base)   # dir_base offset
    header += b"\x00" * (16 * 4)  # 16 reserved uint32s

    assert len(header) == HEADER_SIZE

    # Write PCK
    os.makedirs(os.path.dirname(output), exist_ok=True)
    with open(output, "wb") as f:
        f.write(header)
        f.write(b"\x00" * (file_base - HEADER_SIZE))  # padding to alignment
        file_end = file_base + len(file_payload)
        f.write(file_payload)
        f.write(b"\x00" * (dir_base - file_end))  # padding to alignment
        f.write(dir_section)

    size = os.path.getsize(output)
    print(f"Created bootstrap PCK: {output} ({size} bytes)")


if __name__ == "__main__":
    main()
