"""Lists the visual assignments of the cards, enemies and items.

Usage (from the repo root):  python Tools/artscan.py [--missing]

--missing: show only the rows with a missing (YOK) or broken (KOPUK) visual.
"""
import collections
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLASSES = ("CardData", "EnemyData", "ItemData")
ART_FIELDS = r"cardArt|cardbander|enemyArt|enemybander|icon"


def main():
    os.chdir(ROOT)
    sys.stdout.reconfigure(encoding="utf-8")
    only_missing = "--missing" in sys.argv
    guid_path = {}
    for dp, _, files in os.walk("Assets"):
        for f in files:
            if f.endswith(".meta"):
                p = os.path.join(dp, f)
                with open(p, encoding="utf-8", errors="ignore") as fh:
                    m = re.search(r"guid: ([0-9a-f]{32})", fh.read())
                if m:
                    guid_path[m.group(1)] = p[:-5]

    rows = collections.defaultdict(list)
    for dp, _, files in os.walk("Assets"):
        for f in files:
            if not f.endswith(".asset"):
                continue
            p = os.path.join(dp, f).replace(os.sep, "/")
            with open(p, encoding="utf-8", errors="ignore") as fh:
                text = fh.read()
            sm = re.search(r"m_Script: \{fileID: 11500000, guid: (\w+)", text)
            if not sm:
                continue
            cls = os.path.basename(guid_path.get(sm.group(1), "")).removesuffix(".cs")
            if cls not in CLASSES:
                continue
            info = []
            for field, fid, g in re.findall(
                    rf"^  ({ART_FIELDS}): \{{fileID: (-?\d+)(?:, guid: (\w+))?", text, re.M):
                if fid == "0":
                    val = "YOK"
                else:
                    val = os.path.basename(guid_path[g]) if g in guid_path else "KOPUK"
                info.append(f"{field}={val}")
            if only_missing and not any(("YOK" in i or "KOPUK" in i) for i in info):
                continue
            rows[cls].append((p[len("Assets/"):], info))

    for cls in CLASSES:
        print(f"### {cls}: {len(rows[cls])}")
        for p, info in sorted(rows[cls]):
            print(f"   {p} | {' ; '.join(info)}")


if __name__ == "__main__":
    main()
