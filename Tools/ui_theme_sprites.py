"""
UI tema sprite'larini uretir (karanlik tas + altin cerceve, UI_STIL_REHBERI.md renkleri).
Cikti: Assets/Sprites/UI Theme/*.png  (9-slice border degerleri Assets/Editor/UIThemeTool.cs icinde)

  python Tools/ui_theme_sprites.py            -> sprite'lari yazar (ayni isimdekilerin ustune yazar, deterministik)
  python Tools/ui_theme_sprites.py --preview OUT.png   -> sadece onizleme sayfasi uretir

Sadece Pillow gerekir. Rastgelelik sabit tohumlu, yani her calistirmada ayni piksel cikar.
"""
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

S = 4  # supersample
OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "Sprites", "UI Theme")

GOLD = (212, 175, 55)
GOLD_LIGHT = (246, 222, 140)
GOLD_DARK = (122, 92, 24)
STONE = (24, 19, 32)        # ~#14101C biraz acik
STONE_DARK = (12, 9, 17)
IVORY = (237, 230, 214)


# ------------------------------------------------------------------ helpers
def noise(w, h, seed, scale=1, sigma=48):
    random.seed(seed)
    # Pillow'un effect_noise'u tohum almaz; kendi tohumlu gurultumuzu uretelim
    sw, sh = max(2, w // scale), max(2, h // scale)
    im = Image.new("L", (sw, sh))
    im.putdata([max(0, min(255, int(random.gauss(128, sigma)))) for _ in range(sw * sh)])
    if scale > 1:
        im = im.resize((w, h), Image.BICUBIC)
    return im


def grunge(w, h, seed):
    """Cok oktavli, tas dokusu gibi gri harita (128 civari)."""
    a = noise(w, h, seed, scale=max(1, w // 6), sigma=60)
    b = noise(w, h, seed + 1, scale=max(1, w // 24), sigma=50)
    c = noise(w, h, seed + 2, scale=S, sigma=40)
    m = ImageChops.add(ImageChops.multiply(a, Image.new("L", (w, h), 255)), b, scale=2.0)
    m = ImageChops.add(m, c, scale=2.0)
    return m.filter(ImageFilter.GaussianBlur(S * 0.6))


def tint(gray, dark, light):
    """Gri haritayi iki renk arasinda boyar."""
    lut = []
    for ch in range(3):
        lut += [int(dark[ch] + (light[ch] - dark[ch]) * (i / 255.0)) for i in range(256)]
    return Image.merge("RGB", [gray] * 3).point(lut)


def vgrad(w, h, top, bottom):
    g = Image.new("L", (1, h))
    g.putdata([int(top + (bottom - top) * y / max(1, h - 1)) for y in range(h)])
    return g.resize((w, h))


def rrect_mask(w, h, box, r, width=0):
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    if width:
        d.rounded_rectangle(box, r, outline=255, width=width)
    else:
        d.rounded_rectangle(box, r, fill=255)
    return m


def octagon(box, c):
    x0, y0, x1, y1 = box
    return [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c),
            (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]


def poly_mask(w, h, pts, width=0):
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    if width:
        d.line(pts + [pts[0]], fill=255, width=width, joint="curve")
    else:
        d.polygon(pts, fill=255)
    return m


def gold_layer(w, h, seed, bright=1.0):
    """Metalik altin: dikey isik + ince doku."""
    g = vgrad(w, h, 255, 60)
    n = noise(w, h, seed, scale=S * 2, sigma=30)
    g = ImageChops.add(g, n, scale=1.0, offset=-128)
    if bright != 1.0:
        g = g.point(lambda v: max(0, min(255, int(v * bright))))
    return tint(g, GOLD_DARK, GOLD_LIGHT)


def paste_masked(base, layer, mask, alpha=255):
    if alpha != 255:
        mask = mask.point(lambda v: v * alpha // 255)
    base.paste(layer, (0, 0), mask)


def finish(img, size):
    return img.resize(size, Image.LANCZOS)


def shadow(mask, blur, strength=200):
    sh = mask.filter(ImageFilter.GaussianBlur(blur)).point(lambda v: v * strength // 255)
    return Image.merge("RGBA", (Image.new("L", mask.size, 0),) * 3 + (sh,))


def diamond(cx, cy, r):
    return [(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)]


# ------------------------------------------------------------------ sprites
def panel_frame(size=256):
    """Ana panel: tas zemin, kalin altin cerceve, ic ince cizgi, koselerde elmas + kose kancalari."""
    W = H = size * S
    pad = 6 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = rrect_mask(W, H, (pad, pad, W - pad, H - pad), 10 * S)
    img.alpha_composite(shadow(body, 4 * S))

    stone = tint(grunge(W, H, 11), STONE_DARK, (44, 36, 56))
    # ic golge: kenarlara dogru karar
    inner = rrect_mask(W, H, (pad + 18 * S, pad + 18 * S, W - pad - 18 * S, H - pad - 18 * S), 30 * S)
    inner = inner.filter(ImageFilter.GaussianBlur(14 * S))
    stone = Image.composite(stone, tint(grunge(W, H, 12), (6, 4, 9), (20, 15, 26)), inner)
    paste_masked(img, stone, body)

    gold = gold_layer(W, H, 13)
    # kalin dis bant (koyu kontur + altin)
    paste_masked(img, Image.new("RGB", (W, H), (40, 28, 6)),
                 rrect_mask(W, H, (pad, pad, W - pad, H - pad), 10 * S, width=7 * S))
    paste_masked(img, gold, rrect_mask(W, H, (pad + S, pad + S, W - pad - S, H - pad - S), 9 * S, width=4 * S))
    # ince ic cizgi
    i = pad + 13 * S
    paste_masked(img, gold, rrect_mask(W, H, (i, i, W - i, H - i), 4 * S, width=int(1.5 * S)), 200)

    # kose kancalari + elmas
    d = ImageDraw.Draw(img)
    for sx, sy in ((0, 0), (1, 0), (0, 1), (1, 1)):
        cx = pad + 18 * S if sx == 0 else W - pad - 18 * S
        cy = pad + 18 * S if sy == 0 else H - pad - 18 * S
        dx = 1 if sx == 0 else -1
        dy = 1 if sy == 0 else -1
        # L kanca
        m = Image.new("L", (W, H), 0)
        md = ImageDraw.Draw(m)
        L = 34 * S
        md.line([(cx + dx * 4 * S, cy + dy * L), (cx + dx * 4 * S, cy + dy * 4 * S), (cx + dx * L, cy + dy * 4 * S)],
                fill=255, width=int(2.5 * S), joint="curve")
        md.ellipse((cx + dx * L - 3 * S, cy + dy * 4 * S - 3 * S, cx + dx * L + 3 * S, cy + dy * 4 * S + 3 * S), fill=255)
        md.ellipse((cx + dx * 4 * S - 3 * S, cy + dy * L - 3 * S, cx + dx * 4 * S + 3 * S, cy + dy * L + 3 * S), fill=255)
        paste_masked(img, gold, m)
        # elmas: koyu kontur, altin govde, kehribar tas
        ox = pad + 4 * S if sx == 0 else W - pad - 4 * S
        oy = pad + 4 * S if sy == 0 else H - pad - 4 * S
        d.polygon(diamond(ox, oy, 15 * S), fill=(40, 28, 6, 255))
        paste_masked(img, gold, poly_mask(W, H, diamond(ox, oy, 13 * S)))
        d.polygon(diamond(ox, oy, 7 * S), fill=(110, 40, 20, 255))
        d.polygon(diamond(ox - 1 * S, oy - 1 * S, 4 * S), fill=(236, 120, 50, 255))
        d.polygon(diamond(ox - 2 * S, oy - 3 * S, int(1.5 * S)), fill=(255, 235, 200, 255))
    return finish(img, (size, size))


def panel_plain(size=96):
    """Ic panel / liste ogesi: tas zemin, tek ince altin cizgi."""
    W = H = size * S
    pad = 3 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = rrect_mask(W, H, (pad, pad, W - pad, H - pad), 8 * S)
    img.alpha_composite(shadow(body, 2 * S, 160))
    stone = tint(grunge(W, H, 21), (14, 11, 20), (38, 31, 48))
    paste_masked(img, stone, body)
    paste_masked(img, gold_layer(W, H, 22), rrect_mask(W, H, (pad, pad, W - pad, H - pad), 8 * S, width=2 * S), 230)
    return finish(img, (size, size))


def backdrop(size=512):
    """Tam ekran arka plan: ortasi hafif acik, kenarlari koyu vinyet."""
    W = H = size
    g = grunge(W, H, 31)
    base = tint(g, (8, 6, 12), (30, 24, 40))
    v = Image.new("L", (W, H), 0)
    ImageDraw.Draw(v).ellipse((-W * 0.15, -H * 0.15, W * 1.15, H * 1.15), fill=255)
    v = v.filter(ImageFilter.GaussianBlur(W * 0.18))
    dark = Image.new("RGB", (W, H), (3, 2, 5))
    out = Image.composite(base, dark, v).convert("RGBA")
    return out


def button(state, w=240, h=80):
    """Sekizgen (pahli koseli) buton. state: normal / hover / pressed / disabled."""
    W, H = w * S, h * S
    pad = 6 * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shape = octagon((pad, pad, W - pad, H - pad), 12 * S)
    body = poly_mask(W, H, shape)

    if state == "hover":
        glow = body.filter(ImageFilter.GaussianBlur(5 * S)).point(lambda v: min(255, v * 2))
        img.alpha_composite(Image.merge("RGBA", (Image.new("L", (W, H), GOLD[0]), Image.new("L", (W, H), GOLD[1]),
                                                 Image.new("L", (W, H), GOLD[2]), glow.point(lambda v: v * 150 // 255))))
    else:
        img.alpha_composite(shadow(body, 3 * S, 180))

    top, bot = {"normal": ((52, 42, 64), (16, 12, 22)),
                "hover": ((74, 58, 88), (26, 20, 34)),
                "pressed": ((14, 10, 20), (36, 28, 46)),
                "disabled": ((40, 38, 44), (20, 19, 23))}[state]
    grad = vgrad(W, H, 255, 0)
    fill = tint(grad, bot, top)
    tex = tint(grunge(W, H, 41), (0, 0, 0), (40, 40, 40))
    fill = ImageChops.add(fill, tex, scale=1.0, offset=-20)
    paste_masked(img, fill, body)

    if state == "disabled":
        rim = tint(vgrad(W, H, 200, 80), (60, 58, 62), (150, 146, 150))
    else:
        rim = gold_layer(W, H, 42, bright=1.15 if state == "hover" else (0.8 if state == "pressed" else 1.0))
    paste_masked(img, Image.new("RGB", (W, H), (40, 28, 6)), poly_mask(W, H, shape, width=6 * S))
    paste_masked(img, rim, poly_mask(W, H, shape, width=3 * S))
    inset = octagon((pad + 6 * S, pad + 6 * S, W - pad - 6 * S, H - pad - 6 * S), 8 * S)
    paste_masked(img, rim, poly_mask(W, H, inset, width=S), 150)
    # ust parlama
    hl = poly_mask(W, H, inset).point(lambda v: v * 34 // 255)
    hl = ImageChops.multiply(hl, vgrad(W, H, 255, 0).point(lambda v: max(0, (v - 110) * 255 // 145)))
    paste_masked(img, Image.new("RGB", (W, H), (255, 245, 220)), hl)
    return finish(img, (w, h))


def pill(kind, size=40):
    """Slider/scrollbar parcalari. kind: track / fill / handle."""
    W = H = size * S
    pad = 2 * S
    r = (size // 2 - 2) * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    box = (pad, pad, W - pad, H - pad)
    body = rrect_mask(W, H, box, r)
    if kind == "track":
        groove = tint(grunge(W, H, 51), (4, 3, 6), (22, 17, 28))
        sh = vgrad(W, H, 0, 255)  # ustte ic golge
        groove = Image.composite(groove, Image.new("RGB", (W, H), (2, 1, 3)), sh)
        paste_masked(img, groove, body)
        paste_masked(img, gold_layer(W, H, 52, 0.9), rrect_mask(W, H, box, r, width=2 * S), 220)
    elif kind == "fill":
        # Gri tonlu: Image.color ile boyanir (altin, mana mavisi vs.)
        g = vgrad(W, H, 255, 150)
        paste_masked(img, Image.merge("RGB", [g] * 3), rrect_mask(W, H, (pad + 3 * S, pad + 3 * S, W - pad - 3 * S, H - pad - 3 * S), r - 3 * S))
    elif kind == "handle":
        paste_masked(img, Image.new("RGB", (W, H), (40, 28, 6)), body)
        inner = rrect_mask(W, H, (pad + S, pad + S, W - pad - S, H - pad - S), r - S)
        paste_masked(img, gold_layer(W, H, 53), inner)
    return finish(img, (size, size))


def knob(size=64):
    """Slider tutamagi: altin halka, koyu ic, ortada elmas."""
    W = H = size * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    c = W // 2
    outer = Image.new("L", (W, H), 0)
    ImageDraw.Draw(outer).ellipse((4 * S, 4 * S, W - 4 * S, H - 4 * S), fill=255)
    img.alpha_composite(shadow(outer, 2 * S, 200))
    d = ImageDraw.Draw(img)
    d.ellipse((4 * S, 4 * S, W - 4 * S, H - 4 * S), fill=(40, 28, 6, 255))
    ring = Image.new("L", (W, H), 0)
    ImageDraw.Draw(ring).ellipse((6 * S, 6 * S, W - 6 * S, H - 6 * S), fill=255)
    paste_masked(img, gold_layer(W, H, 61), ring)
    d.ellipse((15 * S, 15 * S, W - 15 * S, H - 15 * S), fill=(18, 13, 24, 255))
    paste_masked(img, gold_layer(W, H, 62, 1.1), poly_mask(W, H, diamond(c, c, 11 * S)))
    d.polygon(diamond(c - S, c - 2 * S, 3 * S), fill=(255, 245, 210, 255))
    return finish(img, (size, size))


def divider(w=256, h=24):
    """Baslik alti ayirici: ortada elmas, iki yana incelen altin cizgi."""
    W, H = w * S, h * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    m = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(m)
    cy = H // 2
    md.polygon([(4 * S, cy), (W // 2, cy - 2 * S), (W - 4 * S, cy), (W // 2, cy + 2 * S)], fill=255)
    md.polygon(diamond(W // 2, cy, 9 * S), fill=255)
    paste_masked(img, gold_layer(W, H, 71), m)
    ImageDraw.Draw(img).polygon(diamond(W // 2, cy, 4 * S), fill=(110, 40, 20, 255))
    return finish(img, (w, h))


SPRITES = {
    "UI_PanelFrame": panel_frame,
    "UI_PanelPlain": panel_plain,
    "UI_Backdrop": backdrop,
    "UI_Button": lambda: button("normal"),
    "UI_Button_Hover": lambda: button("hover"),
    "UI_Button_Pressed": lambda: button("pressed"),
    "UI_Button_Disabled": lambda: button("disabled"),
    "UI_BarTrack": lambda: pill("track"),
    "UI_BarFill": lambda: pill("fill"),
    "UI_ScrollHandle": lambda: pill("handle"),
    "UI_Knob": knob,
    "UI_Divider": divider,
}


def preview(images, out):
    """Koyu arka plan uzerinde, 9-slice gibi buyutulmus ornek ekran."""
    bg = backdrop(1024).resize((1280, 720))
    sheet = bg.copy()

    def slice9(im, w, h, b):
        k = min(1.0, w / (b[0] + b[2]), h / (b[1] + b[3]))  # Unity gibi: rect kucukse border'lari kucult
        if k < 1.0:
            im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
            b = tuple(int(v * k) for v in b)
        bl, bt, br, bb = b
        iw, ih = im.size
        out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        xs = [(0, bl, 0, bl), (bl, iw - br, bl, w - br), (iw - br, iw, w - br, w)]
        ys = [(0, bt, 0, bt), (bt, ih - bb, bt, h - bb), (ih - bb, ih, h - bb, h)]
        for sx0, sx1, dx0, dx1 in xs:
            for sy0, sy1, dy0, dy1 in ys:
                if dx1 > dx0 and dy1 > dy0:
                    out.alpha_composite(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0)), (dx0, dy0))
        return out

    sheet.alpha_composite(slice9(images["UI_PanelFrame"], 620, 560, (64,) * 4), (60, 80))
    sheet.alpha_composite(images["UI_Divider"].resize((360, 34)), (190, 160))
    for i, k in enumerate(["UI_Button", "UI_Button_Hover", "UI_Button_Pressed", "UI_Button_Disabled"]):
        sheet.alpha_composite(slice9(images[k], 300, 76, (36, 30, 36, 30)), (220, 230 + i * 90))
    sheet.alpha_composite(slice9(images["UI_PanelPlain"], 460, 300, (20,) * 4), (760, 80))
    for i in range(3):
        y = 120 + i * 80
        sheet.alpha_composite(slice9(images["UI_BarTrack"], 360, 26, (18,) * 4), (800, y))
        fill = slice9(images["UI_BarFill"], 60 + i * 120, 26, (18,) * 4)
        col = [GOLD, (30, 95, 200), (200, 50, 30)][i]
        fill = ImageChops.multiply(fill, Image.new("RGBA", fill.size, col + (255,)))
        sheet.alpha_composite(fill, (800, y))
        sheet.alpha_composite(images["UI_Knob"].resize((44, 44)), (800 + 60 + i * 120 - 22, y - 9))
    sheet.alpha_composite(slice9(images["UI_BarTrack"], 18, 240, (9,) * 4), (1180, 100))
    sheet.alpha_composite(slice9(images["UI_ScrollHandle"], 18, 80, (9,) * 4), (1180, 130))
    sheet.alpha_composite(slice9(images["UI_PanelFrame"], 460, 260, (64,) * 4), (760, 420))
    sheet.convert("RGB").save(out)
    print("preview ->", out)


def main():
    images = {k: f() for k, f in SPRITES.items()}
    if len(sys.argv) >= 3 and sys.argv[1] == "--preview":
        preview(images, sys.argv[2])
        return
    os.makedirs(OUT_DIR, exist_ok=True)
    for k, im in images.items():
        p = os.path.join(OUT_DIR, k + ".png")
        im.save(p, optimize=True)
        print("wrote", os.path.normpath(p), im.size)


if __name__ == "__main__":
    main()
