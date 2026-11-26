"""Finds broken asset references in a Unity project.

Usage (from the repo root):  python Tools/guidscan.py

Reports:
  - tracked assets whose .meta file is not in git
  - duplicate GUIDs
  - GUID references in scenes/prefabs/assets that match no file under Assets/
    or Library/PackageCache (file, line, field)
"""
import collections
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")
REF_EXT = (".unity", ".prefab", ".asset", ".mat", ".controller", ".anim",
           ".overrideController", ".playable", ".renderTexture")


def meta_guids(base):
    """guid -> path for every .meta under base."""
    out = {}
    for dp, _, files in os.walk(base):
        for f in files:
            if f.endswith(".meta"):
                p = os.path.join(dp, f)
                with open(p, encoding="utf-8", errors="ignore") as fh:
                    m = GUID_RE.search(fh.read())
                if m:
                    out.setdefault(m.group(1), []).append(p)
    return out


def main():
    os.chdir(ROOT)
    sys.stdout.reconfigure(encoding="utf-8")
    tracked = set(subprocess.run(
        ["git", "-c", "core.quotepath=off", "ls-files", "-z"],
        capture_output=True).stdout.decode("utf-8").split("\0"))
    no_meta = sorted(f for f in tracked if f.startswith("Assets/")
                     and not f.endswith(".meta") and f + ".meta" not in tracked)
    print(f"Assets tracked without a .meta in git: {len(no_meta)}")
    for f in no_meta[:20]:
        print("   ", f)

    assets = meta_guids("Assets")
    dups = {g: v for g, v in assets.items() if len(v) > 1}
    print(f"Duplicate GUIDs: {len(dups)}")
    for g, v in dups.items():
        print("   ", g, v)

    known = set(assets) | set(meta_guids(os.path.join("Library", "PackageCache")))
    if not os.path.isdir(os.path.join("Library", "PackageCache")):
        print("WARNING: no Library/PackageCache - open the project in Unity once; package references will show as broken.")

    broken = []
    for dp, _, files in os.walk("Assets"):
        for f in files:
            if not f.endswith(REF_EXT):
                continue
            p = os.path.join(dp, f).replace(os.sep, "/")
            with open(p, encoding="utf-8", errors="ignore") as fh:
                for n, line in enumerate(fh, 1):
                    for g in GUID_RE.findall(line):
                        if g not in known and not g.startswith("0000000000"):
                            broken.append((p, n, line.strip()[:100]))
    print(f"Broken GUID references: {len(broken)}")
    for p, n, l in broken:
        print(f"    {p}:{n}  {l}")
    return 1 if broken else 0


if __name__ == "__main__":
    sys.exit(main())
