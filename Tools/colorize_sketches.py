"""Colorizes the pencil sketches in the element's color.

Usage (from the repo root):
  python Tools/colorize_sketches.py --preview OUT.png   # preview sheet, changes no files
  python Tools/colorize_sketches.py --apply BACKUP_DIR  # copies originals to BACKUP_DIR, then writes in place

The file is overwritten in place, so the .meta/GUID and sprite slices stay the same and
references do not break. The originals stay in git history (commit 579bc9f and earlier).
"""
import os
import re
import shutil
import sys

from PIL import Image, ImageChops, ImageFilter, ImageOps, ImageStat

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# element -> (dark edge, light center, glow) — matched to the card frame colors
PALETTE = {
    "Fire":  ((40, 8, 6),    (255, 196, 140), (255, 110, 40)),
    "Water": ((6, 16, 40),   (170, 210, 255), (70, 150, 255)),
    "Earth": ((36, 20, 6),   (250, 205, 130), (255, 150, 40)),
    "Air":   ((6, 30, 24),   (180, 245, 215), (60, 255, 170)),
    "None":  ((22, 20, 26),  (225, 220, 210), (255, 220, 150)),
}
ELEMENT_ENUM = ["None", "Fire", "Water", "Earth", "Air", "Fire", "Water", "Water", "Earth"]  # fusions -> nearest main color
FRAME_TO_ELEMENT = {"Fire": "Fire", "Water": "Water", "Earth": "Earth", "Air": "Air", "None": "None"}

# Assets that use a sketch (from docs/GORSEL_DURUMU.md). The element comes from the frame (cardbander/enemybander);
# items with no frame use the value here.
TARGETS = [
    "CardData/Breeze.asset", "CardData/Healing Water.asset", "CardData/Magma Punch.asset",
    "CardData/Pebble.asset", "CardData/Quake.asset", "CardData/Spark.asset", "CardData/Wind Slash.asset",
    "CardData/Support Data/Gambling.asset", "CardData/Support Data/Greed.asset",
    "CardData/Companion Card Data/Mother Nature.asset", "CardData/Companion Card Data/Strong Magic.asset",
    "CardData/Companion Card Data/Will.asset",
    "EnemyData/Coal Imp.asset", "EnemyData/Ice Witch.asset", "EnemyData/White Wolf.asset",
    "ItemData/Extra Demage.asset", "ItemData/Max health.asset",
]
ART_FIELD = r"(?:cardArt|enemyArt|icon)"
FRAME_FIELD = r"(?:cardbander|enemybander)"


def guid_map():
    out = {}
    for dp, _, files in os.walk("Assets"):
        for f in files:
            if f.endswith(".meta"):
                with open(os.path.join(dp, f), encoding="utf-8", errors="ignore") as fh:
                    m = re.search(r"guid: ([0-9a-f]{32})", fh.read())
                if m:
                    out[m.group(1)] = os.path.join(dp, f[:-5])
    return out


def resolve(g2p):
    jobs = []
    for rel in TARGETS:
        with open(os.path.join("Assets", rel), encoding="utf-8", errors="ignore") as fh:
            t = fh.read()
        art = re.search(ART_FIELD + r": \{fileID: -?\d+, guid: (\w+)", t)
        frame = re.search(FRAME_FIELD + r": \{fileID: -?\d+, guid: (\w+)", t)
        elem = None
        if frame and frame.group(1) in g2p:
            name = os.path.basename(g2p[frame.group(1)]).split()[0]
            elem = FRAME_TO_ELEMENT.get(name)
        if elem is None:
            m = re.search(r"^  element: (\d+)", t, re.M)
            elem = ELEMENT_ENUM[int(m.group(1))] if m else "None"
        jobs.append((rel, g2p[art.group(1)], elem))
    return jobs


def corner_brightness(src):
    """Average brightness of the four corners: gray paper on a sketch, dark vignette once colorized."""
    im = Image.open(src).convert("L")
    w, h = im.size
    k = max(4, w // 40)
    boxes = [(0, 0, k, k), (w - k, 0, w, k), (0, h - k, k, h), (w - k, h - k, w, h)]
    return sum(ImageStat.Stat(im.crop(b)).mean[0] for b in boxes) / 4


def colorize(src, elem):
    dark, light, glow = PALETTE[elem]
    im = Image.open(src)
    alpha = im.getchannel("A") if im.mode == "RGBA" else None
    rgb = im.convert("RGB")
    w, h = rgb.size

    # The paper color (the most common tone) is pulled to white; lines and colored strokes are kept.
    gray = ImageOps.grayscale(rgb)
    paper = max(1, int(ImageStat.Stat(gray.resize((64, 64))).median[0]))
    # paper -> white, lines darkened slightly (gamma 1.6) so they are not lost on the colored background
    lut = [int(255 * min(1.0, v / paper) ** 1.6) for v in range(256)] * 3
    ink = rgb.point(lut)

    # Background: light element color in the center, dark toward the edges
    mask = Image.radial_gradient("L").resize((w, h))        # 0 center, 255 edges
    bg = Image.composite(Image.new("RGB", (w, h), dark), Image.new("RGB", (w, h), light), mask)

    # Glow: a soft glow in the element color around the lines — added to the background, under the lines
    lines = ImageOps.invert(ImageOps.grayscale(ink)).filter(ImageFilter.GaussianBlur(max(3, w // 90)))
    glow_layer = ImageChops.multiply(Image.new("RGB", (w, h), glow), Image.merge("RGB", (lines,) * 3))
    bg = ImageChops.screen(bg, glow_layer)

    # The lines go on last, so they stay dark and sharp
    out = ImageChops.multiply(bg, ink)

    if alpha is not None:
        out = out.convert("RGBA")
        out.putalpha(alpha)
    return out


def main():
    os.chdir(ROOT)
    sys.stdout.reconfigure(encoding="utf-8")
    jobs = resolve(guid_map())
    if "--preview" in sys.argv:
        out_path = sys.argv[sys.argv.index("--preview") + 1]
        H = 160
        tiles = []
        for rel, src, elem in jobs:
            for im in (Image.open(src).convert("RGBA"), colorize(src, elem).convert("RGBA")):
                im.thumbnail((H, H))
                tiles.append(im)
        per = 12
        rows = (len(tiles) + per - 1) // per
        sheet = Image.new("RGBA", (per * (H + 6), rows * (H + 6)), (30, 30, 30, 255))
        for i, t in enumerate(tiles):
            sheet.paste(t, ((i % per) * (H + 6), (i // per) * (H + 6)), t)
        sheet.save(out_path)
        for rel, src, elem in jobs:
            print(f"{elem:6} {rel}  <-  {src}")
    elif "--apply" in sys.argv:
        backup = sys.argv[sys.argv.index("--apply") + 1]
        os.makedirs(backup, exist_ok=True)
        for rel, src, elem in jobs:
            if corner_brightness(src) < 70:  # sketch paper ~110-155; a colorized one has a dark vignette (~20-35)
                print(f"skipped (already colorized): {src}")
                continue
            shutil.copy2(src, os.path.join(backup, os.path.basename(src)))
            colorize(src, elem).save(src)
            print(f"written: {src} ({elem})")
    else:
        print(__doc__)


if __name__ == "__main__":
    main()
