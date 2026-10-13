#!/usr/bin/env bash
# Verifies every ri-* icon class used in the project exists in the bundled RemixIcon v4.2 stylesheet.
# A missing icon renders as nothing, with no error, so run this before every hand-off.
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
css="$root/src/Reflect.Web/wwwroot/assets/libs/remixicon/remixicon.css"
missing=0
for icon in $(grep -rhoE "ri-[a-z0-9]+(-[a-z0-9]+)*" --include=*.cshtml --include=*.cs --include=*.js --include=*.json \
    "$root/src/Reflect.Web/Areas" "$root/src/Reflect.Web/Views" "$root/src/Reflect.Web/Models" "$root/src/Reflect.Web/menus" \
    "$root/src/Reflect.Web/wwwroot/js" "$root/src/Reflect.Domain" | sort -u); do
  if ! grep -q "\.$icon:before" "$css"; then echo "Missing icon: $icon"; missing=1; fi
done
[ "$missing" -eq 0 ] && echo "All RemixIcon classes exist."
exit $missing
