#!/usr/bin/env bash
# Bumps the mod version in About/About.xml (RELEASE.MINOR.PATCH) and opens a CHANGELOG section for it.
#   scripts/bump.sh release   big rework            1.4.2 -> 2.0.0
#   scripts/bump.sh minor     features / options    1.4.2 -> 1.5.0
#   scripts/bump.sh patch     fixes only ("shame")  1.4.2 -> 1.4.3
set -euo pipefail
cd "$(dirname "$0")/.."
about=About/About.xml
cur=$(sed -n 's:.*<modVersion>\(.*\)</modVersion>.*:\1:p' "$about")
IFS=. read -r rel min pat <<<"$cur"
case "${1:-}" in
	release) rel=$((rel + 1)); min=0; pat=0 ;;
	minor) min=$((min + 1)); pat=0 ;;
	patch|shame) pat=$((pat + 1)) ;;
	*) echo "usage: $0 release|minor|patch   (current: $cur)" >&2; exit 1 ;;
esac
new="$rel.$min.$pat"
sed -i "s:<modVersion>$cur</modVersion>:<modVersion>$new</modVersion>:" "$about"
today=$(date +%Y-%m-%d)
python3 - "$new" "$today" <<'PY'
import sys, pathlib
p = pathlib.Path("CHANGELOG.md")
s = p.read_text()
marker = "<!-- versions -->\n"
s = s.replace(marker, marker + f"\n## {sys.argv[1]} - {sys.argv[2]}\n\n- \n", 1)
p.write_text(s)
PY
echo "$cur -> $new (About.xml, CHANGELOG.md). Fill in the changelog, then: git tag v$new"
