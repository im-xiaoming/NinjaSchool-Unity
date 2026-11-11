#!/usr/bin/env bash
# Starts Play mode and drives the game to the world (title -> Continue -> character select -> world). Leaves Play running.
export UNITY_NO_AGENT_HINTS=1
PROJ="$(cygpath -w "$(cd "$(dirname "$0")/.." && pwd)")"
cmd() { unity command "$@" --project-path "$PROJ" 2>&1; }
ev()  { cmd script eval "$1" | tail -1; }
screen() { ev 'Main.canvas != null && GameCanvas.currentScreen != null ? GameCanvas.currentScreen.GetType().Name : "none"' | awk '{print $3}'; }
for _ in $(seq 1 60); do ev 'UnityEditor.EditorApplication.isCompiling + "/" + UnityEditor.EditorApplication.isUpdating' | grep -q "^script.*False/False" && break; sleep 4; done
cmd editor.playmode stop >/dev/null; sleep 4; cmd console clear >/dev/null; cmd editor.playmode start >/dev/null
last=""; same=0
for _ in $(seq 1 90); do
  s="$(screen)"; [ "$s" = "$last" ] && same=$((same+1)) || { same=0; last="$s"; }
  case "$s" in
    SelectServerScr) [ $same -ge 3 ] && { ev '((SelectServerScr)GameCanvas.currentScreen).perform(1001, null); "c"' >/dev/null; sleep 3; same=0; } ;;
    SelectCharScr)   [ $same -ge 2 ] && { ev 'UnityEngine.Object.FindFirstObjectByType<NsoInputControl>().Press(NsoInputControl.Enter); "k"' >/dev/null; sleep 3; same=0; } ;;
    GameScr)         sleep 5; echo "in world"; exit 0 ;;
  esac
  sleep 2
done
echo "did not reach the world (last screen: $last)"; exit 1
