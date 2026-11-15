# Enumeracion de balanceo (P5). Uso: python3 Docs/P5/balance.py  (desde la raiz del repo)
# Lee Assets/_Project/Data/Catalogo/CatalogoArtemis.json y repite las revisiones de RocketPhysics
# con los valores de la mision V13. Es la misma cuenta que SpaceApps -> Pruebas de balanceo en Unity.
import collections, itertools, json, math, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
P = {p["id"]: p for p in json.load(open(os.path.join(ROOT, "Assets/_Project/Data/Catalogo/CatalogoArtemis.json"), encoding="utf-8"))["parts"]}
G0 = 9.80665
OPTIONAL = [i for i, p in P.items() if p["type"] not in ("Capsule", "ServiceModule", "AbortSystem", "LaunchService")]
LAUNCHERS = ["ART-SLS-BLOCK1", "ART-SLS-BLOCK1B"]
MISSION = dict(crew=4, water=2.5, oxygen=0.84, food=0.75, base_water=135, base_oxygen=47, base_food=44,
               tli_extra=682, dv=1060, science=40, downlink_s=3600, outage_h=1.5)

def problems(ids, days, budget=3400):
    m = MISSION
    parts = [P[i] for i in ["ART-CM-ORION", "ART-ESM-ORION", "ART-LAS-ORION"] + ids]
    craft = [p for p in parts if p["type"] not in ("LaunchService", "AbortSystem")]
    sls = next(p for p in parts if p["type"] == "LaunchService")
    mass = sum(p["dryMass"] + p["fuelMass"] for p in craft)
    esm = P["ART-ESM-ORION"]
    out = []
    if sum(p["price"] for p in parts) > budget: out.append("BUDGET")
    if mass + m["tli_extra"] > sls["payloadCapacity"]: out.append("MASS")
    if esm["isp"] * G0 * math.log(mass / (mass - esm["fuelMass"])) < m["dv"]: out.append("DELTA_V")
    pd = m["crew"] * days
    if (m["base_water"] + sum(p["waterKg"] for p in craft) < pd * m["water"]
            or m["base_oxygen"] + sum(p["oxygenKg"] for p in craft) < pd * m["oxygen"]
            or m["base_food"] + sum(p["foodKg"] for p in craft) < pd * m["food"]): out.append("CONSUMABLES")
    load = sum(p["powerDraw"] for p in craft)
    if sum(p["power"] for p in craft) < load: out.append("POWER")
    if sum(p["batteryWh"] for p in craft) < load * m["outage_h"]: out.append("BATTERY")
    if sum(p["sciencePoints"] for p in craft) < m["science"]: out.append("SCIENCE")
    bits = sum(p["experimentDataBits"] for p in craft); rate = sum(p["dataRateBps"] for p in craft)
    if bits > 0 and (rate <= 0 or bits / rate > m["downlink_s"]): out.append("DOWNLINK")
    return out, sum(p["price"] for p in parts)

def enumerate_all(days, max_copies=1, budget=3400):
    total, viable, fails = 0, [], collections.Counter()
    for launcher in LAUNCHERS:
        for counts in itertools.product(range(max_copies + 1), repeat=len(OPTIONAL)):
            ids = [launcher] + [o for o, c in zip(OPTIONAL, counts) for _ in range(c)]
            f, price = problems(ids, days, budget)
            total += 1
            fails.update(set(f))
            if not f: viable.append((price, ids))
    viable.sort()
    return total, viable, fails

if __name__ == "__main__":
    for days in (12, 15):
        total, viable, fails = enumerate_all(days)
        print(f"{days} dias: {total} combinaciones, {len(viable)} viables ({100 * len(viable) / total:.2f} %)")
        print("  fallan por:", dict(fails))
        print("  mas barata:", viable[0][0], [i[4:] for i in viable[0][1]])
        print("  con Block 1B:", sum(1 for _, ids in viable if ids[0].endswith("1B")))
    if "--repetidas" in sys.argv:
        for days in (12, 15):
            total, viable, _ = enumerate_all(days, 2)
            print(f"{days} dias con hasta 2 copias: {total} combinaciones, {len(viable)} viables; mas barata {viable[0][0]} {[i[4:] for i in viable[0][1]]}")
    if "--presupuesto" in sys.argv:
        for budget in (3400, 3600, 3800, 4000):
            print(budget, "creditos:", [len(enumerate_all(d, 1, budget)[1]) for d in (12, 15)], "viables (12 / 15 dias)")
