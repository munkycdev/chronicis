import json
import re
import sys
import xml.etree.ElementTree as ET

if len(sys.argv) < 2:
    print("Usage: extract_rates.py <path-to-cobertura.xml> [more-cobertura-files...]", file=sys.stderr)
    sys.exit(2)

cc_pattern = re.compile(r"^\s*(\d+(?:\.\d+)?)%\s*\((\d+)\s*/\s*(\d+)\)\s*$")

line_covered = 0
line_total = 0
branch_covered = 0
branch_total = 0

for path in sys.argv[1:]:
    root = ET.parse(path).getroot()
    for line in root.findall("./packages/package/classes/class/lines/line"):
        line_total += 1
        hits = int(line.attrib.get("hits", "0"))
        if hits > 0:
            line_covered += 1

        if line.attrib.get("branch", "").lower() == "true":
            cc = line.attrib.get("condition-coverage", "")
            match = cc_pattern.match(cc)
            if match:
                branch_covered += int(match.group(2))
                branch_total += int(match.group(3))


def pct(num: int, den: int):
    if den <= 0:
        return None
    return round((num / den) * 100.0, 2)


print(json.dumps({
    "linePct": pct(line_covered, line_total),
    "branchPct": pct(branch_covered, branch_total),
    "lineCovered": line_covered,
    "lineTotal": line_total,
    "branchCovered": branch_covered,
    "branchTotal": branch_total,
}))
