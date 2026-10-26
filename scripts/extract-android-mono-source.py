"""Extract the pinned native Mono build inputs without archive symlinks."""
import pathlib
import sys
import tarfile

archive, destination, prefix = sys.argv[1:]
target = pathlib.Path(destination).resolve()
target.mkdir(parents=True, exist_ok=False)
directories = ("eng/native/", "src/mono/", "src/native/", "src/coreclr/scripts/")
files = {
    ".gitignore", "LICENSE.TXT", "PATENTS.TXT",
    "src/coreclr/vm/ClrEtwAll.man", "src/coreclr/vm/ClrEtwAllMeta.lst",
}
with tarfile.open(archive, "r:gz") as source:
    for member in source:
        if member.name == prefix and member.isdir():
            continue
        if not member.name.startswith(prefix + "/"):
            raise ValueError("Unexpected runtime archive root: " + member.name)
        relative = member.name[len(prefix) + 1:]
        if relative not in files and not relative.startswith(directories):
            continue
        # Native sources contain documentation symlinks. They are not compiler
        # inputs, and creating them requires unnecessary privileges on Windows.
        if not member.isfile():
            continue
        path = target.joinpath(*pathlib.PurePosixPath(relative).parts).resolve()
        if not path.is_relative_to(target):
            raise ValueError("Archive path escapes native source root: " + relative)
        path.parent.mkdir(parents=True, exist_ok=True)
        with source.extractfile(member) as incoming, path.open("wb") as output:
            output.write(incoming.read())
