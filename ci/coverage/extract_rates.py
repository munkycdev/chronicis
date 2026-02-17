import json
import sys
import xml.etree.ElementTree as ET

if len(sys.argv) != 2:
    print("Usage: extract_rates.py <path-to-cobertura.xml>", file=sys.stderr)
    sys.exit(2)

path = sys.argv[1]
root = ET.parse(path).getroot()

line_rate = root.attrib.get("line-rate")
branch_rate = root.attrib.get("branch-rate")

def to_pct(rate):
    if rate is None:
        return None
    try:
        return round(float(rate) * 100.0, 2)
    except Exception:
        return None

print(json.dumps({
    "linePct": to_pct(line_rate),
    "branchPct": to_pct(branch_rate),
}))
