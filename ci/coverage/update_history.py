import json
import os
from datetime import datetime, timezone

path = os.environ.get("HISTORY_PATH", "_history/history.json")
sha = os.environ["SHA"]
line = os.environ.get("LINE_PCT")
branch = os.environ.get("BRANCH_PCT")

def to_float(x):
    try:
        return float(x) if x not in (None, "None", "") else None
    except Exception:
        return None

iso_date = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

if not os.path.exists(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = {"points": []}
else:
    with open(path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

points = data.get("points", [])
points = [p for p in points if p.get("sha") != sha]

points.append({
    "date": iso_date,
    "sha": sha,
    "linePct": to_float(line),
    "branchPct": to_float(branch),
})

data["points"] = points[-200:]

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)

print(iso_date)
