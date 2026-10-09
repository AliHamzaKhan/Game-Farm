#!/usr/bin/env python3
"""
Farm Quest — Phase 0 pacing / economy simulation.

Validates the v1 tuning numbers from game-design-document.md against the
stated pacing targets:
  - L10 in ~2 weeks of casual play
  - L30 in ~3 months
  - L50 in 6–9 months
  - "Profit per plot-hour rises gently with level — progression you can feel,
     never a cliff."

Model: discrete-event sim. Player does 2 short sessions/day (morning + evening,
12h apart, 15 min each). Each session: harvest ripe, water due, replant empty
plots with the best unlocked crop for an attentive-but-casual player.

Crop choice heuristic: maximize profit per growth-hour * player availability.
A crop only gets planted if it will be harvested within a day (kids don't want
dead plots; ripe crops wait patiently, so longer crops are fine as long as the
player checks twice a day).

Quality: watering on-time rate models player attentiveness.
  - engaged kid: 90% on-time -> mostly ⭐⭐
  - casual kid:   60% on-time -> mix of ⭐/⭐⭐
Skipping compost caps at ⭐⭐ (kid rule).

XP sources (from GDD §25): plant 3 · water 2 · harvest 8 (+2/star) ·
sell 1 per 10 coins · mission 30 (5/day from L5-ish) · order/unlock/animal
ignored for early pacing (conservative).

Run: python3 phase0-pacing-sim.py
"""

import math, random

random.seed(42)

# ---------------------------------------------------------------- crops ----
# (name, unlock_lv, seed, growth_min, waterings, yield_avg, sell, perennial?)
CROPS = [
    ("carrot",     2, 10,   3, 1, 5.0,   6),
    ("lettuce",    4, 12,   4, 1, 5.0,   7),
    ("tomato",     3, 15,   5, 2, 6.5,  12),
    ("onion",      5, 14,   6, 2, 6.0,   8),
    ("peas",       6, 16,   6, 2, 6.5,   9),
    ("potato",     7, 12,   8, 2, 7.5,   8),
    ("cucumber",   8, 18,   8, 2, 6.5,  10),
    ("spinach",   10, 20,  10, 2, 7.5,  11),
    ("pumpkin",    9, 25,  15, 2, 4.0,  22),
    ("corn",      13, 30,  20, 3, 8.0,  14),
    ("chili",     16, 35,  25, 3, 6.5,  18),
    ("strawberry",12, 45,  30, 3, 8.0,  16),
    ("bellpepper",18, 40,  30, 3, 6.5,  20),
    ("watermelon",14, 60,  45, 4, 4.0,  45),
    ("sunflower", 21, 70,  40, 2, 5.0,  30),
    ("wheat",     24, 50,  45, 2, 12.5,  8),
    ("rice",      27, 90,  90, 5, 12.5, 12),
    ("cotton",    29,120, 120, 3, 8.0,  28),
    ("cocoa",     32,200, 240, 4, 6.5,  60),
    ("coffee",    35,220, 240, 4, 6.5,  65),
]

def xp_for_next_level(n):
    return round(100 * n ** 1.5)

def pick_crop(level, day_hours, session_gap_h=12.0):
    """Best unlocked crop the player can fully tend: harvestable between
    sessions (growth <= session gap) so plots never sit idle waiting."""
    cands = [c for c in CROPS if c[1] <= level and c[3] / 60.0 <= session_gap_h]
    def score(c):
        profit = c[5] * c[6] - c[2]          # avg yield * sell - seed (⭐⭐ mults cancel in ranking)
        per_hour = profit / (c[3] / 60.0)    # profit per plot-hour
        per_cycle = profit                    # attention-light crops score on bulk
        return per_hour * 0.5 + per_cycle * 0.02
    return max(cands, key=score)

def simulate(ontime_rate, days=300, sessions_per_day=2, mission_xp_from_day=3):
    plots = [{"crop": None, "ready_at": None, "watered": 0, "crop_def": None}
             for _ in range(4)]
    coins = 500.0
    xp = 0.0
    level = 1
    free_carrot_seeds = 5   # tutorial starter inventory (L1, before carrot unlock)
    t = 0.0  # hours
    milestones = {}
    land2_bought = False
    hist = []

    def add_xp(v):
        nonlocal xp, level
        xp += v
        while xp >= xp_for_next_level(level):
            xp -= xp_for_next_level(level)
            level += 1

    def note(name):
        if name not in milestones:
            milestones[name] = t / 24.0

    note("start")
    while t < days * 24:
        for s in range(sessions_per_day):
            # harvest + replant + water
            for p in plots:
                if p["crop"] is not None and t >= p["ready_at"]:
                    name, lv, seed, growth, water, yld, sell = p["crop_def"]
                    # quality: all waterings on time -> ⭐⭐ (yield*1.2, price*1.3)
                    all_on_time = p["watered"] >= water and random.random() < ontime_rate
                    if all_on_time:
                        qty, price = yld * 1.2, sell * 1.3
                        stars = 2
                    else:
                        qty, price = yld, sell
                        stars = 1
                    revenue = qty * price
                    coins += revenue
                    add_xp(8 + 2 * stars)            # harvest
                    add_xp(revenue / 10.0)           # sell: 1 XP per 10 coins
                    p["crop"] = None
            # water due plots (assume one watering pass per session suffices)
            for p in plots:
                if p["crop"] is not None:
                    p["watered"] += 1
                    add_xp(2)
            # replant
            for p in plots:
                if p["crop"] is None:
                    c = pick_crop(level, t) if level >= 2 else CROPS[0]
                    cost = 0 if (level < 2 and free_carrot_seeds > 0) else c[2]
                    if coins >= cost:
                        if level < 2:
                            free_carrot_seeds -= 1
                        else:
                            coins -= cost
                        p["crop"] = c[0]
                        p["crop_def"] = c
                        p["ready_at"] = t + c[3] / 60.0
                        p["watered"] = 0
                        add_xp(3)
            # daily missions
            if t / 24.0 >= mission_xp_from_day:
                add_xp(30 * 5)  # 5 missions/day
            # level-based milestones
            for lv, name in [(2, "L2"), (5, "L5"), (9, "L9"), (10, "L10"),
                             (20, "L20"), (30, "L30"), (40, "L40"), (50, "L50")]:
                if level >= lv:
                    note(name)
            # buy land 2 when affordable at L9+
            if level >= 9 and coins >= 1000 and not land2_bought:
                coins -= 1000
                land2_bought = True
                for _ in range(8):
                    plots.append({"crop": None, "ready_at": None,
                                  "watered": 0, "crop_def": None})
                note("land2")
            hist.append((t / 24.0, level, coins))
            t += 24.0 / sessions_per_day
    return milestones, hist, coins, level

def fmt(ms, key):
    return f"{ms.get(key, float('nan')):7.1f}d" if key in ms else "   n/a "

print("=" * 78)
print("FARM QUEST — PHASE 0 PACING SIMULATION  (targets: L10 ~14d, L30 ~90d, L50 180-270d)")
print("=" * 78)

for label, rate in [("engaged kid (90% waterings on time)", 0.90),
                    ("casual kid  (60% waterings on time)", 0.60)]:
    ms, hist, coins, level = simulate(rate)
    print(f"\n{label}")
    print(f"  L2 {fmt(ms,'L2')}  L5 {fmt(ms,'L5')}  L9 {fmt(ms,'L9')}  land2 {fmt(ms,'land2')}")
    print(f"  L10 {fmt(ms,'L10')}  L20 {fmt(ms,'L20')}  L30 {fmt(ms,'L30')}  L40 {fmt(ms,'L40')}  L50 {fmt(ms,'L50')}")
    print(f"  after 300d: level {level}, coins {coins:,.0f}")

print("\n" + "-" * 78)
print("PROFIT PER PLOT-HOUR AT ⭐ QUALITY (avg yield) — checks 'rises gently'")
print("-" * 78)
for c in sorted(CROPS, key=lambda c: c[1]):
    name, lv, seed, growth, water, yld, sell = c
    profit = yld * sell - seed
    print(f"  L{lv:2d} {name:11s} profit/cycle {profit:7.1f}  per plot-hour {profit/(growth/60):8.1f} coins")

print("\n" + "-" * 78)
print("COINS EARNED PER HARVEST (⭐) — what the player *feels* per tap")
print("-" * 78)
for c in sorted(CROPS, key=lambda c: c[1])[:12]:
    name, lv, seed, growth, water, yld, sell = c
    print(f"  L{lv:2d} {name:11s} revenue/harvest {yld*sell:7.1f}  profit/harvest {yld*sell-seed:7.1f}")

print("\n" + "-" * 78)
print("XP CURVE SANITY: total XP to reach each level")
print("-" * 78)
tot = 0
for n in range(1, 51):
    tot += xp_for_next_level(n)
    if n in (9, 29, 49):
        print(f"  XP needed to reach L{n+1}: {tot:,}")

# bankruptcy check: day-1 coin floor with worst crop choice
print("\n" + "-" * 78)
print("STARTING CAPITAL CHECK: 500 coins, 4 plots — can a kid ever go broke?")
print("-" * 78)
worst = min(CROPS, key=lambda c: c[5] * c[6] - c[2])
print(f"  Worst crop by profit/cycle: {worst[0]} (L{worst[1]}), profit {worst[5]*worst[6]-worst[2]:.1f}/cycle")
print("  Seed costs are 10–220 coins vs 500 starting capital; every unlocked crop")
print("  is profitable per cycle at ⭐ (no crop loses money), so bankruptcy is")
print("  impossible through farming alone — only decor/land overspend can drain coins.")
