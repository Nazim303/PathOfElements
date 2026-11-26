"""Path of Elements denge simülasyonu (Monte Carlo).

Oyunun savaş ve harita kurallarını (CombatManager, TurnManager, EnergyManager,
DeckManager, MapGenerator, bosslar) sade bir modelle taklit eder ve binlerce koşu
oynatır. Açgözlü bir yapay oyuncu kullanır; insan oyuncu biraz daha iyi oynar.

Kullanım:
  python Tools/balance_sim.py                # mevcut ve önerilen değerleri karşılaştırır
  python Tools/balance_sim.py --runs 5000
  python Tools/balance_sim.py --preset onerilen --deck guclu

Kart ve düşman sayıları elle girilmiştir (asset'lerden okunmaz). Asset değerleri
değişirse buradaki tablolar da güncellenmelidir. Varsayımlar docs/DENGELEME_PLANI.md'de.
"""
import argparse
import copy
import itertools
import random
import statistics
from dataclasses import dataclass, field

F, W, E, A, N = "Fire", "Water", "Earth", "Air", "None"
STRONG = {(F, E), (E, A), (A, W), (W, F)}
WEAK = {(F, W), (W, A), (A, E), (E, F)}


MOD_MULT = 1.5  # güçlendiricinin aynı elementteki karta çarpanı (CombatManager)


def mult(att, dfn):
    if att == N or dfn == N or att == dfn:
        return 1.0
    if (att, dfn) in STRONG:
        return 2.0
    if (att, dfn) in WEAK:
        return 0.5
    return 1.0


@dataclass(frozen=True)
class Card:
    name: str
    kind: str  # atk, heal, mod, sup, comp
    el: str
    cost: int
    dmg: int = 0
    heal: int = 0
    sup: str = ""
    val: int = 0


@dataclass
class Enemy:
    name: str
    hp: int
    el: str
    dmg: int
    gold: int
    boss: str = ""


# ---------------------------------------------------------------- veri setleri
def cards_mevcut():
    return {
        "Spark": Card("Spark", "atk", F, 1, 5),
        "Fire Ball": Card("Fire Ball", "atk", F, 2, 12),
        "Magma Punch": Card("Magma Punch", "atk", F, 2, 15),
        "Fire Supportive": Card("Fire Supportive", "mod", F, 0, 3),
        "Droplet": Card("Droplet", "atk", W, 1, 5),
        "Water Jet": Card("Water Jet", "atk", W, 2, 11),
        "Ice Spear": Card("Ice Spear", "atk", W, 2, 14),
        "Pebble": Card("Pebble", "atk", E, 1, 7),
        "Rock Throw": Card("Rock Throw", "atk", E, 2, 13),
        "Quake": Card("Quake", "atk", E, 2, 16),
        "Breeze": Card("Breeze", "atk", A, 1, 6),
        "Wind Slash": Card("Wind Slash", "atk", A, 2, 12),
        "Tornado": Card("Tornado", "atk", A, 3, 16),
        "Heal Potion": Card("Heal Potion", "heal", N, 1, heal=8),
        "Healing Water": Card("Healing Water", "heal", W, 2, heal=15),
        "Greed": Card("Greed", "sup", N, 0, sup="draw", val=2),
        "Gambling": Card("Gambling", "sup", N, 0, sup="redraw", val=4),
        "Mana Transfer": Card("Mana Transfer", "sup", N, 0, sup="energy", val=2),
        "McDuck": Card("McDuck", "sup", N, 0, sup="gold", val=5),
        "Blood Transfer": Card("Blood Transfer", "sup", N, 0, sup="heal", val=15),
        "Shield": Card("Shield", "sup", N, 0, sup="shield", val=8),
        # yoldaşlar
        "Pablo": Card("Pablo", "comp", F, 3, 15),
        "Tsunami": Card("Tsunami", "atk", W, 4, 22),
        "Mother of Nature": Card("Mother of Nature", "heal", E, 3, heal=20),
    }


def enemies_mevcut():
    return [
        Enemy("Watcher", 20, A, 7, 5), Enemy("Fire Arrows", 22, F, 9, 7),
        Enemy("Drowned", 25, W, 6, 5), Enemy("Shining Worm", 30, A, 8, 8),
        Enemy("Gao", 35, E, 9, 12), Enemy("Magma Worm", 15, F, 7, 4),
        Enemy("Mushroom", 30, E, 8, 9), Enemy("Tree", 25, E, 9, 8),
        Enemy("Boiling Desire", 30, W, 10, 9), Enemy("Marauders", 25, E, 6, 3),
        Enemy("Bloom Sprout", 20, E, 4, 2),
    ]


ITEMS_MEVCUT = [
    {"name": "Bandage", "cost": 30, "type": "fullheal", "val": 0},
    {"name": "Camp Fire", "cost": 30, "type": "maxhp", "val": 15},
    {"name": "Gambler's Hand", "cost": 25, "type": "hand", "val": 2},
]

MEVCUT = dict(
    name="mevcut",
    cards=cards_mevcut(),
    enemies=enemies_mevcut(),
    items=ITEMS_MEVCUT,
    elementalist=Enemy("Elementalist", 100, F, 15, 100, boss="elem"),
    protector=Enemy("Protector", 150, E, 20, 25, boss="prot"),
    minion=Enemy("Bloom Sprout", 20, E, 4, 2),  # defaultMinion sahnede; varsayım
    protector_heal=20, protector_heal_every=3,
    elite_hp=1.5, elite_dmg=1.3, elite_gold=2.0, elite_protector_chance=0.5,
    max_hp=100, max_energy=11, energy_per_turn=2, refill_on_battle=False,
    hand_start=4, draw_per_turn=2, max_hand=7, exchange_hp=10,
    victory_heal=0, start_gold=0,
    map_len=10,
    chances=dict(combat=40, elite=10, shop=10, diviner=10, companion=15, mystery=15),
    elite_min_layer=1, shop_min_layer=1,
    early_enemies=(1, 2), late_enemies=(2, 3), elite_count=(2, 3),
    companions=["Pablo", "Tsunami", "Mother of Nature"],
    early_pool=None, late_pool=None,
)


def onerilen():
    c = cards_mevcut()
    upd = {
        # Saldırı eğrisi: 1 enerji = 6, 2 enerji = 13, 3 enerji (güç 3) = 20
        "Spark": Card("Spark", "atk", F, 1, 6),
        "Droplet": Card("Droplet", "atk", W, 1, 6),
        "Pebble": Card("Pebble", "atk", E, 1, 6),
        "Breeze": Card("Breeze", "atk", A, 1, 6),
        "Fire Ball": Card("Fire Ball", "atk", F, 2, 13),
        "Water Jet": Card("Water Jet", "atk", W, 2, 13),
        "Rock Throw": Card("Rock Throw", "atk", E, 2, 13),
        "Wind Slash": Card("Wind Slash", "atk", A, 2, 13),
        "Ice Spear": Card("Ice Spear", "atk", W, 3, 20),  # su güç-3 rolü
        "Magma Punch": Card("Magma Punch", "atk", F, 3, 20),
        "Quake": Card("Quake", "atk", E, 3, 20),
        "Tornado": Card("Tornado", "atk", A, 3, 20),
        "Fire Supportive": Card("Fire Supportive", "mod", F, 1, 5),
        "Heal Potion": Card("Heal Potion", "heal", N, 1, heal=8),
        "Healing Water": Card("Healing Water", "heal", W, 2, heal=16),
        "Water Supportive": Card("Water Supportive", "mod", W, 1, 5),
        "Earth Supportive": Card("Earth Supportive", "mod", E, 1, 5),
        "Air Supportive": Card("Air Supportive", "mod", A, 1, 5),
        # destekler: bedava oldukları için küçük
        "Mana Transfer": Card("Mana Transfer", "sup", N, 0, sup="energy", val=1),
        "Blood Transfer": Card("Blood Transfer", "sup", N, 0, sup="heal", val=8),
        "McDuck": Card("McDuck", "sup", N, 0, sup="gold", val=10),
        "Shield": Card("Shield", "sup", N, 0, sup="shield", val=8),
        "Greed": Card("Greed", "sup", N, 0, sup="draw", val=2),
        "Gambling": Card("Gambling", "sup", N, 0, sup="redraw", val=4),
        # yoldaşlar: Pablo tek kullanımlık, diğerleri kalıcı kart
        "Pablo": Card("Pablo", "comp", F, 2, 20),
        "Tsunami": Card("Tsunami", "atk", W, 3, 24),
        "Mother of Nature": Card("Mother of Nature", "heal", E, 2, heal=20),
    }
    c.update(upd)
    en = [
        # erken (zayıf) havuz: ilk yarıda tek bu havuzdan çıkar
        Enemy("Bloom Sprout", 22, E, 6, 4), Enemy("Magma Worm", 20, F, 8, 4),
        Enemy("Watcher", 22, A, 7, 4), Enemy("Drowned", 26, W, 7, 5),
        Enemy("Marauders", 30, A, 8, 5),   # Earth -> Air (element dengesi)
        # geç (güçlü) havuz
        Enemy("Fire Arrows", 28, F, 10, 7), Enemy("Tree", 36, E, 9, 7),
        Enemy("Shining Worm", 34, A, 9, 7), Enemy("Mushroom", 34, W, 9, 7),  # Earth -> Water
        Enemy("Boiling Desire", 34, W, 11, 8), Enemy("Gao", 44, E, 12, 10),
    ]
    items = [
        {"name": "Bandage", "cost": 40, "type": "fullheal", "val": 0},
        {"name": "Camp Fire", "cost": 30, "type": "maxhp", "val": 12},
        {"name": "Gambler's Hand", "cost": 40, "type": "draw", "val": 1},
    ]
    d = copy.deepcopy(MEVCUT)
    d.update(
        name="onerilen", cards=c, enemies=en, items=items,
        early_pool=["Bloom Sprout", "Magma Worm", "Watcher", "Drowned", "Marauders"],
        late_pool=None,
        elementalist=Enemy("Elementalist", 150, F, 14, 0, boss="elem"),
        protector=Enemy("Protector", 70, E, 10, 30, boss="prot"),
        minion=Enemy("Bloom Sprout", 22, E, 6, 0),
        protector_heal=10, protector_heal_every=3,
        elite_hp=1.3, elite_dmg=1.2, elite_gold=2.0, elite_protector_chance=0.5,
        max_hp=80, max_energy=6, energy_per_turn=2, refill_on_battle=True,
        hand_start=5, draw_per_turn=2, max_hand=8,
        victory_heal=0, start_gold=0,
        chances=dict(combat=45, elite=12, shop=12, diviner=8, companion=10, mystery=13),
        elite_min_layer=3, shop_min_layer=2,
        early_enemies=(1, 2), late_enemies=(2, 3), elite_count=(2, 2),
    )
    return d


PRESETS = {"mevcut": lambda: copy.deepcopy(MEVCUT), "onerilen": onerilen}

DECKS = {
    # tipik oyuncu: her elementten bir zayıf + bir orta, iyileşme, iki destek
    "tipik": ["Spark", "Fire Ball", "Droplet", "Water Jet", "Pebble", "Rock Throw",
              "Breeze", "Wind Slash", "Heal Potion", "Healing Water", "Greed", "Shield"],
    # güçlü kartları seçen oyuncu
    "guclu": ["Magma Punch", "Quake", "Ice Spear", "Fire Ball", "Rock Throw", "Wind Slash",
              "Water Jet", "Tornado", "Healing Water", "Mana Transfer", "Blood Transfer", "Greed"],
    # tek element (hava) + yeni güçlendirici (yalnızca önerilen preset'te var)
    "hava": ["Breeze", "Wind Slash", "Tornado", "Air Supportive", "Breeze", "Wind Slash",
             "Water Jet", "Ice Spear", "Healing Water", "Heal Potion", "Mana Transfer", "Shield"],
    # iki element + iki güçlendirici
    "ikili": ["Droplet", "Water Jet", "Ice Spear", "Water Supportive", "Pebble", "Rock Throw",
              "Quake", "Earth Supportive", "Healing Water", "Heal Potion", "Greed", "Shield"],
    # tek element (ateş) + destek
    "ates": ["Spark", "Fire Ball", "Magma Punch", "Fire Supportive", "Spark", "Fire Ball",
             "Rock Throw", "Quake", "Healing Water", "Heal Potion", "Mana Transfer", "Shield"],
}


# ---------------------------------------------------------------- savaş
class Player:
    def __init__(self, cfg, deck):
        self.cfg = cfg
        self.max_hp = cfg["max_hp"]
        self.hp = self.max_hp
        self.gold = cfg["start_gold"]
        self.energy = cfg["max_energy"]
        self.max_energy = cfg["max_energy"]
        self.max_hand = cfg["max_hand"]
        self.draw_per_turn = cfg["draw_per_turn"]
        self.deck = [cfg["cards"][n] for n in deck]
        self.hand = []
        self.bonus = 0


def best_play(p, hand, energy, front_el, hp_missing):
    """Eldeki ≤4 kartlık en değerli kombinasyonu seçer (sıralı slotlar)."""
    playable = [c for c in hand if c.kind in ("atk", "heal", "mod", "comp")]
    best, best_v = [], 0.0
    idx = list(range(len(playable)))
    for r in range(1, min(4, len(playable)) + 1):
        for combo in itertools.combinations(idx, r):
            cs = [playable[i] for i in combo]
            cost = sum(c.cost for c in cs)
            if cost > energy:
                continue
            v = 0.0
            heal_left = hp_missing
            attacks = [c for c in cs if c.kind in ("atk", "comp")]
            mods = [c for c in cs if c.kind == "mod"]
            used_mod = 0
            for c in attacks:
                m = 1.0
                if used_mod < len(mods) and any(mo.el == c.el for mo in mods[used_mod:]):
                    m = MOD_MULT
                    used_mod += 1
                v += (c.dmg + p.bonus) * mult(c.el, front_el) * m
            for mo in mods[used_mod:]:
                v += mo.dmg * mult(mo.el, front_el)
            for c in cs:
                if c.kind == "heal":
                    h = min(c.heal, heal_left)
                    heal_left -= h
                    v += h * (1.2 if hp_missing > p.max_hp * 0.4 else 0.6)
            v -= cost * 0.3  # enerjiyi boşa harcamama eğilimi
            if r == 4 and len({c.el for c in cs}) == 1:
                v += 10  # tam kombo AOE bonusu (kaba)
            if v > best_v:
                best_v, best = v, cs
    return best


def arrange(cards):
    """Güçlendiriciyi aynı elementteki saldırının sağına koyar."""
    atks = [c for c in cards if c.kind != "mod"]
    mods = [c for c in cards if c.kind == "mod"]
    out = []
    for c in atks:
        out.append(c)
        for mo in mods:
            if mo.el == c.el:
                out.append(mo)
                mods.remove(mo)
                break
    return out + mods


def battle(p, enemies, rng, stats):
    cfg = p.cfg
    draw, discard = [c for c in p.deck], []
    for c in p.hand:
        if c in draw:
            draw.remove(c)
    rng.shuffle(draw)
    comps = [c for c in draw if c.kind == "comp"]
    if comps:
        draw.remove(comps[0])
        draw.insert(0, comps[0])
    if cfg["refill_on_battle"]:
        p.energy = p.max_energy

    def draw_n(n):
        n = min(n, p.max_hand - len(p.hand))
        for _ in range(max(0, n)):
            if not draw:
                if not discard:
                    return
                draw.extend(discard)
                discard.clear()
                rng.shuffle(draw)
            p.hand.append(draw.pop(0))

    foes = [copy.copy(e) for e in enemies]
    prot_turn = 0
    elem_cycle = [F, W, E, A]
    elem_idx = 0
    if any(e.boss == "prot" for e in foes):
        foes = [copy.copy(cfg["minion"]), copy.copy(cfg["minion"])] + foes
    draw_n(cfg["hand_start"])
    rounds = 0
    hp_start = p.hp
    while True:
        rounds += 1
        if rounds > 40:
            p.hp = 0
            stats["timeout"] += 1
            return False
        # PlayerTurn: enerji +x, kalkan sıfır
        if rounds > 1 or not cfg["refill_on_battle"]:
            p.energy = min(p.max_energy, p.energy + cfg["energy_per_turn"])
        shield = 0
        # destekleri Exchange'e at
        for c in list(p.hand):
            if c.kind != "sup":
                continue
            use = True
            if c.sup == "heal" and p.max_hp - p.hp < c.val * 0.6:
                use = False
            if c.sup == "redraw" and len(p.hand) > 3:
                use = False
            if not use:
                continue
            p.hand.remove(c)
            discard.append(c)
            if c.sup == "draw":
                draw_n(c.val)
            elif c.sup == "redraw":
                discard.extend(x for x in p.hand if x.kind != "comp")
                p.hand = [x for x in p.hand if x.kind == "comp"]
                draw_n(c.val)
            elif c.sup == "energy":
                p.energy = min(p.max_energy, p.energy + c.val)
            elif c.sup == "gold":
                p.gold += c.val
            elif c.sup == "heal":
                p.hp = min(p.max_hp, p.hp + c.val)
            elif c.sup == "shield":
                shield += c.val
        front = foes[0]
        play = arrange(best_play(p, p.hand, p.energy, front.el, p.max_hp - p.hp))
        for c in play:
            p.hand.remove(c)
        full_combo = len(play) == 4 and len({c.el for c in play}) == 1
        i = 0
        while i < len(play):
            c = play[i]
            if c.cost > p.energy:
                discard.append(c)
                i += 1
                continue
            p.energy -= c.cost
            m = 1.0
            if c.kind != "mod" and i + 1 < len(play) and play[i + 1].kind == "mod" and play[i + 1].el == c.el \
                    and play[i + 1].cost <= p.energy:
                p.energy -= play[i + 1].cost
                m = MOD_MULT
                discard.append(play[i + 1])
                skip = True
            else:
                skip = False
            if c.kind == "heal":
                p.hp = min(p.max_hp, p.hp + c.heal)
            elif foes:
                if c.kind == "comp":
                    targets = list(foes)
                elif full_combo:
                    targets = list(foes)
                else:
                    targets = [foes[0]]
                for t in targets:
                    if t.boss == "prot" and len(foes) > 1:
                        continue
                    t.hp -= round((c.dmg + p.bonus) * mult(c.el, t.el) * m)
                for t in list(foes):
                    if t.hp <= 0:
                        foes.remove(t)
                        p.gold += t.gold
            if c.kind == "comp":
                p.deck.remove(c)
            else:
                discard.append(c)
            i += 2 if skip else 1
        if not foes:
            stats["rounds"].append(rounds)
            stats["hp_loss"].append(hp_start - p.hp)
            p.hp = min(p.max_hp, p.hp + cfg["victory_heal"])
            return True
        # Düşman turu
        for e in list(foes):
            dmg = e.dmg
            if e.boss == "prot":
                prot_turn += 1
                if prot_turn % cfg["protector_heal_every"] == 0:
                    for f in foes:
                        base = cfg["minion"].hp if f.boss == "" else cfg["protector"].hp
                        f.hp = min(base, f.hp + cfg["protector_heal"])
                    continue
            absorbed = min(shield, dmg)
            shield -= absorbed
            p.hp -= dmg - absorbed
            if e.boss == "elem":
                elem_idx = (elem_idx + 1) % 4
                e.el = elem_cycle[elem_idx]
        if p.hp <= 0:
            return False
        draw_n(p.draw_per_turn)


# ---------------------------------------------------------------- harita
def roll_type(cfg, rng, layer, exclude, force_combat, no_mystery=False):
    ch = dict(cfg["chances"])
    if no_mystery or force_combat:
        ch["mystery"] = 0
    if force_combat:
        ch.update(shop=0, diviner=0, companion=0, mystery=0)
    else:
        for t in ("shop", "diviner", "companion", "mystery"):
            if t in exclude:
                ch[t] = 0
    if layer <= 1 or layer >= cfg["map_len"] - 2:
        ch["diviner"] = 0
    if layer < cfg["elite_min_layer"]:
        ch["elite"] = 0
    if layer < cfg["shop_min_layer"]:
        ch["shop"] = 0
    tot = sum(ch.values())
    r = rng.randrange(tot)
    for k, v in ch.items():
        if r < v:
            return k
        r -= v
    return "combat"


def pick_enemies(cfg, rng, layer, kind):
    en = cfg["enemies"]
    by = {e.name: e for e in en}
    mid = cfg["map_len"] // 2
    if kind == "elite":
        if rng.random() < cfg["elite_protector_chance"]:
            return [cfg["protector"]]
        n = rng.randint(*cfg["elite_count"])
        pool = [e for e in en if not cfg["early_pool"] or e.name not in cfg["early_pool"]] or en
        out = []
        for _ in range(n):
            b = rng.choice(pool)
            out.append(Enemy(b.name, round(b.hp * cfg["elite_hp"]), b.el, round(b.dmg * cfg["elite_dmg"]),
                             round(b.gold * cfg["elite_gold"])))
        return out
    if layer < mid:
        n = rng.randint(*cfg["early_enemies"])
        pool = [by[x] for x in cfg["early_pool"]] if cfg["early_pool"] else en
    else:
        n = rng.randint(*cfg["late_enemies"])
        pool = [e for e in en if not cfg["early_pool"] or e.name not in cfg["early_pool"]] if cfg["early_pool"] else en
        if cfg["early_pool"] and n == 3:
            # 3'lü grupta biri zayıf havuzdan
            return [rng.choice(pool), rng.choice(pool), by[rng.choice(cfg["early_pool"])]]
    return [rng.choice(pool) for _ in range(n)]


def shop(p, cfg, rng):
    offer = rng.sample(cfg["items"], min(3, len(cfg["items"])))
    order = sorted(offer, key=lambda it: 0 if it["type"] in ("fullheal", "heal") and p.hp < p.max_hp * 0.6 else 1)
    for it in order:
        if p.gold < it["cost"]:
            continue
        t = it["type"]
        if t in ("fullheal", "heal") and p.hp > p.max_hp * 0.7:
            continue
        if t == "hand" and p.max_hand >= 9:
            continue
        p.gold -= it["cost"]
        if t == "fullheal":
            p.hp = p.max_hp
        elif t == "heal":
            p.hp = min(p.max_hp, p.hp + it["val"])
        elif t == "maxhp":
            p.max_hp += it["val"]
            p.hp += it["val"]
        elif t == "hand":
            p.max_hand += it["val"]
        elif t == "draw":
            p.draw_per_turn += it["val"]
        elif t == "dmg":
            p.bonus += it["val"]


def run(cfg, deck, rng, stats):
    p = Player(cfg, deck)
    L = cfg["map_len"]
    streak = 0
    prev = set()
    for layer in range(L):
        if layer == 0:
            kind = "combat"
        elif layer == L - 1:
            kind = "boss"
        else:
            opts = []
            for _ in range(2):
                t = roll_type(cfg, rng, layer, prev, streak >= 2)
                if t == "mystery":
                    t = roll_type(cfg, rng, layer, prev, streak >= 2, no_mystery=True)
                opts.append(t)
            low = p.hp < p.max_hp * 0.45
            pref = (["shop", "companion", "diviner", "combat", "elite"] if low
                    else ["companion", "combat", "shop", "elite", "diviner"])
            if p.gold < 25:
                pref.remove("shop")
                pref.append("shop")
            kind = min(opts, key=pref.index)
        stats["nodes"][kind] += 1
        prev = {kind}
        if kind in ("combat", "elite", "boss"):
            streak = 0
            if kind == "boss":
                foes = [cfg["elementalist"]]
            else:
                foes = pick_enemies(cfg, rng, layer, kind)
            st = stats["by"][kind]
            ok = battle(p, foes, rng, st)
            if not ok:
                stats["death"][f"{kind}@{layer}"] += 1
                return False, p
        else:
            streak += 1
            if kind == "shop":
                shop(p, cfg, rng)
            elif kind == "companion":
                p.deck.append(cfg["cards"][rng.choice(cfg["companions"])])
        stats["hp_at"][layer].append(p.hp / p.max_hp)
    return True, p


def simulate(cfg, deck_name, runs, seed=1):
    from collections import Counter, defaultdict
    rng = random.Random(seed)
    stats = {
        "by": {k: {"rounds": [], "hp_loss": [], "timeout": 0} for k in ("combat", "elite", "boss")},
        "death": Counter(), "nodes": Counter(), "hp_at": defaultdict(list),
    }
    wins, golds = 0, []
    for _ in range(runs):
        ok, p = run(cfg, DECKS[deck_name], rng, stats)
        wins += ok
        golds.append(p.gold)
    return wins / runs, stats, golds


def report(cfg, deck, runs):
    wr, st, golds = simulate(cfg, deck, runs)
    print(f"\n=== {cfg['name']} / deste: {deck} / {runs} koşu ===")
    print(f"Kazanma oranı: %{wr * 100:.0f}")
    for k in ("combat", "elite", "boss"):
        b = st["by"][k]
        if b["rounds"]:
            print(f"  {k:7s}: ort. {statistics.mean(b['rounds']):.1f} tur, "
                  f"ort. can kaybı {statistics.mean(b['hp_loss']):.1f}  (kazanılan {len(b['rounds'])})")
    deaths = sorted(st["death"].items(), key=lambda kv: -kv[1])[:8]
    tot = sum(st["death"].values()) or 1
    print("  Ölüm yerleri:", ", ".join(f"{k} %{v * 100 / tot:.0f}" for k, v in deaths))
    hp_line = " ".join(f"L{l}:{statistics.mean(v) * 100:.0f}" for l, v in sorted(st["hp_at"].items()))
    print("  Katman sonu ort. can %:", hp_line)
    print(f"  Koşu sonu ort. altın: {statistics.mean(golds):.0f}")
    return wr


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--runs", type=int, default=3000)
    ap.add_argument("--preset", choices=list(PRESETS), default=None)
    ap.add_argument("--deck", choices=list(DECKS), default=None)
    a = ap.parse_args()
    presets = [a.preset] if a.preset else list(PRESETS)
    for pr in presets:
        cards = PRESETS[pr]()["cards"]
        decks = [a.deck] if a.deck else [d for d in DECKS if all(n in cards for n in DECKS[d])]
        for d in decks:
            report(PRESETS[pr](), d, a.runs)
