#!/usr/bin/env python3
"""Generate the synthetic workforce dataset used by the dashboard's demo mode.

Every number is invented. The generator keeps the data internally consistent so the
dashboard (and its tests) can rely on these identities for every row:

    works + non_works + projects + mines = manpower
    male + female = manpower
    post_graduate + graduate + diploma + iti + higher_secondary + matriculate + below_matriculate = manpower

and, for every unit and cadre, from one snapshot to the next:

    manpower(year) = manpower(year - 1) + recruitments(year) - separations(year)

Usage:  python tools/generate_sample_data.py [--seed 42] [--first-year 2019] [--last-year 2024] [--out DIR] [--sql FILE]
"""
import argparse
import csv
import os
import random

# code, name, group, executives, non-executives, has_mines
UNITS = [
    ("NPW", "North Plant", "Integrated plants", 2600, 12800, True),
    ("EPW", "East Plant", "Integrated plants", 1900, 9400, True),
    ("SPW", "South Plant", "Integrated plants", 1750, 8900, True),
    ("WPW", "West Plant", "Integrated plants", 1150, 6300, False),
    ("CPW", "Central Plant", "Integrated plants", 980, 4700, False),
    ("ASW", "Alloy Steel Works", "Special plants", 150, 820, False),
    ("SSW", "Special Steel Works", "Special plants", 170, 760, False),
    ("RMW", "Rolling Mill Works", "Special plants", 120, 1100, False),
    ("RDC", "Research & Development Centre", "Other units", 310, 140, False),
    ("ENG", "Engineering & Technology", "Other units", 420, 260, False),
    ("MKT", "Central Marketing", "Other units", 640, 900, False),
    ("RMD", "Raw Materials Division", "Other units", 380, 2100, True),
    ("CORP", "Corporate Office", "Other units", 520, 330, False),
    ("TRG", "Training Institute", "Other units", 90, 120, False),
]

FIELDS = [
    "unit_code", "snapshot_date", "cadre", "manpower", "works", "non_works", "projects", "mines",
    "recruitments", "separations", "average_age", "male", "female", "post_graduate", "graduate",
    "diploma", "iti", "higher_secondary", "matriculate", "below_matriculate",
]


def split(total, weights, rng, jitter=0.08):
    """Split `total` into integer parts proportional to (jittered) weights, summing exactly to total."""
    w = [max(0.0, x * (1 + rng.uniform(-jitter, jitter))) for x in weights]
    s = sum(w) or 1.0
    raw = [total * x / s for x in w]
    parts = [int(r) for r in raw]
    rest = total - sum(parts)
    order = sorted(range(len(raw)), key=lambda i: raw[i] - parts[i], reverse=True)
    for i in order[:rest]:
        parts[i] += 1
    return parts


def function_weights(group, has_mines):
    if group == "Other units":
        mines = 0.55 if has_mines else 0.0
        return [0.10, 0.82 - mines * 0.8, 0.08, mines]
    return [0.70, 0.20, 0.03, 0.08 if has_mines else 0.0]


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--seed", type=int, default=42)
    ap.add_argument("--first-year", type=int, default=2019)
    ap.add_argument("--last-year", type=int, default=2024)
    ap.add_argument("--out", default=os.path.join(os.path.dirname(__file__), "..", "src", "WorkforceDashboard", "Data", "sample"))
    ap.add_argument("--sql", default=os.path.join(os.path.dirname(__file__), "..", "db", "oracle", "sample_data.sql"))
    a = ap.parse_args()
    rng = random.Random(a.seed)
    os.makedirs(a.out, exist_ok=True)

    with open(os.path.join(a.out, "units.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(["unit_code", "unit_name", "unit_group", "sort_order"])
        for i, (code, name, group, *_rest) in enumerate(UNITS, start=1):
            w.writerow([code, name, group, i])

    rows = []
    for code, name, group, execs, nonexecs, has_mines in UNITS:
        for cadre, start in (("Executive", execs), ("Non-Executive", nonexecs)):
            is_exec = cadre == "Executive"
            manpower = start
            age = rng.uniform(44, 49) if is_exec else rng.uniform(47, 52)
            for year in range(a.first_year, a.last_year + 1):
                sep_rate = rng.uniform(0.045, 0.075) if is_exec else rng.uniform(0.055, 0.085)
                rec_rate = rng.uniform(0.025, 0.065) if is_exec else rng.uniform(0.010, 0.035)
                if year == 2021:  # a hiring freeze year, so the trend has a visible dip
                    rec_rate *= 0.35
                separations = round(manpower * sep_rate)
                recruitments = round(manpower * rec_rate)
                if year > a.first_year:
                    manpower = manpower + recruitments - separations
                    age += rng.uniform(-0.6, 0.2)

                works, non_works, projects, mines = split(manpower, function_weights(group, has_mines), rng)
                female_share = rng.uniform(0.06, 0.10) if is_exec else rng.uniform(0.03, 0.06)
                female = round(manpower * female_share)
                quals = ([0.27, 0.58, 0.13, 0.01, 0.007, 0.003, 0.0] if is_exec
                         else [0.08, 0.14, 0.19, 0.38, 0.11, 0.09, 0.01])
                rows.append({
                    "unit_code": code, "snapshot_date": f"{year}-04-01", "cadre": cadre,
                    "manpower": manpower, "works": works, "non_works": non_works,
                    "projects": projects, "mines": mines,
                    "recruitments": recruitments, "separations": separations,
                    "average_age": round(age, 1), "male": manpower - female, "female": female,
                    **dict(zip(FIELDS[13:], split(manpower, quals, rng, jitter=0.15))),
                })

    with open(os.path.join(a.out, "workforce.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=FIELDS, lineterminator="\n")
        w.writeheader()
        w.writerows(rows)
    write_oracle_seed(rows, a.sql)
    print(f"wrote {len(UNITS)} units and {len(rows)} snapshot rows to {os.path.abspath(a.out)}")
    print(f"wrote Oracle seed script to {os.path.abspath(a.sql)}")


def write_oracle_seed(rows, path):
    """Same data as INSERT statements for db/oracle/schema.sql."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    unit_cd = {code: i * 10 for i, (code, *_rest) in enumerate(UNITS, start=1)}
    q = lambda v: "'" + str(v).replace("'", "''") + "'"
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("-- Synthetic sample data for the Workforce Analytics dashboard.\n")
        f.write("-- Generated by tools/generate_sample_data.py - every number is invented.\n")
        f.write("-- Run after schema.sql. SET DEFINE OFF stops SQL*Plus treating '&' in names as a variable.\n\n")
        f.write("SET DEFINE OFF\n\n")
        for i, (code, name, group, *_rest) in enumerate(UNITS, start=1):
            f.write(f"INSERT INTO unit_mas (unit_cd, unit_abb, unit_name, unit_group, sort_order) "
                    f"VALUES ({unit_cd[code]}, {q(code)}, {q(name)}, {q(group)}, {i});\n")
        f.write("\n")
        cols = ["unit_cd", "year_date", "cadre", "manpower_count"] + FIELDS[4:]
        for r in rows:
            values = [str(unit_cd[r["unit_code"]]), f"DATE {q(r['snapshot_date'])}", q(r["cadre"]), str(r["manpower"])]
            values += [str(r[c]) for c in FIELDS[4:]]
            f.write(f"INSERT INTO manpower_data ({', '.join(cols)}) VALUES ({', '.join(values)});\n")
        f.write("\nCOMMIT;\n")


if __name__ == "__main__":
    main()
