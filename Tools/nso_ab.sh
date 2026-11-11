#!/usr/bin/env bash
# Same-session A/B check of the game layer (render-to-texture) against the original direct drawing.
# Plays the open scene, drives the game to the title menu and into the world, and at each stop captures the
# screen with the layer ON and then OFF. Compares the pairs with nso_compare.ps1 (animation noise is tolerated).
# Usage: Tools/nso_ab.sh [outdir-name]       Needs the Editor open on a scene containing the NSO Game prefab.
set -u
export UNITY_NO_AGENT_HINTS=1
HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$(cygpath -w "$HERE/..")"
NAME="${1:-ab}"
OUT="$HERE/Shots/$NAME"; rm -rf "$OUT"; mkdir -p "$OUT/on" "$OUT/off"
cmd() { unity command "$@" --project-path "$PROJ" 2>&1; }
ev()  { cmd script eval "$1" | tail -1; }
screen() { ev 'Main.canvas != null && GameCanvas.currentScreen != null ? GameCanvas.currentScreen.GetType().Name : "none"' | awk '{print $3}'; }
press() { ev "UnityEngine.Object.FindFirstObjectByType<NsoInputControl>().Press($1); \"k\"" >/dev/null; sleep "${2:-2}"; }
layer() { ev "UnityEngine.Object.FindFirstObjectByType<NsoGameLayer>().drawIntoLayer = $1; \"$1\"" >/dev/null; sleep 1.5; }
shot() { # shot <dir> <name>
  local f tmp; f="$(cygpath -w "$OUT/$1/$2.png")"; tmp="$(mktemp --suffix=.cs)"
  printf 'UnityEngine.ScreenCapture.CaptureScreenshot(@"%s"); return "queued";\n' "$f" > "$tmp"
  cmd script eval --file "$(cygpath -w "$tmp")" >/dev/null; rm -f "$tmp"; sleep 2.5
  [ -s "$OUT/$1/$2.png" ] || echo "FAILED $1/$2"
}
pair() { layer true; shot on "$1"; layer false; shot off "$1"; layer true; }

for _ in $(seq 1 60); do ev 'UnityEditor.EditorApplication.isCompiling + "/" + UnityEditor.EditorApplication.isUpdating' | grep -q "^script.*False/False" && break; sleep 4; done
cmd editor.playmode stop >/dev/null; sleep 4; cmd console clear >/dev/null; cmd editor.playmode start >/dev/null
# drive: language / title / character select / world, stopping to capture at the first stable title screen and in the world
seen_title=0; last=""; same=0
for _ in $(seq 1 90); do
  s="$(screen)"; [ "$s" = "$last" ] && same=$((same+1)) || { same=0; last="$s"; }
  case "$s" in
    SelectServerScr) [ $seen_title = 0 ] && [ $same -ge 2 ] && { pair 01_title; seen_title=1; }
                     [ $same -ge 3 ] && { ev '((SelectServerScr)GameCanvas.currentScreen).perform(1001, null); "continue"' >/dev/null; sleep 3; same=0; } ;;
    SelectCharScr)   [ $same -ge 2 ] && { pair 02_charselect; press NsoInputControl.Enter 3; same=0; } ;;
    CreateCharScr)   echo "reached CreateCharScr (no character)"; break ;;
    GameScr)         sleep 8; pair 03_world; break ;;
  esac
  sleep 2
done
ev 'var c = global::Char.getMyChar(); "player=" + (c == null ? "-" : c.cName) + " zoom=" + mGraphics.zoomLevel + " screen=" + Screen.width + "x" + Screen.height'
cmd console get --tail 80 | grep -o '"level":"error"[^}]\{0,160\}' | sort -u | head -4
cmd editor.playmode stop >/dev/null
powershell -NoProfile -ExecutionPolicy Bypass -File "$(cygpath -w "$HERE/nso_compare.ps1")" "$(cygpath -w "$OUT/on")" "$(cygpath -w "$OUT/off")"
echo "shots in $OUT"
