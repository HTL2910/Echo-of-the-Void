#!/usr/bin/env bash
# Notification hook: shows a macOS notification when Claude Code needs attention.
# (The original showed a Windows toast through powershell.exe.)
INPUT=$(cat)

if command -v jq &>/dev/null; then
  MESSAGE=$(echo "$INPUT" | jq -r '.message // empty' 2>/dev/null)
fi
if [ -z "$MESSAGE" ]; then
  MESSAGE=$(echo "$INPUT" | grep -oE '"message":"[^"]*"' | sed 's/"message":"//;s/"$//')
fi
[ -z "$MESSAGE" ] && MESSAGE="Claude Code needs your attention"
MESSAGE=$(printf '%s' "$MESSAGE" | head -c 200)

if command -v osascript &>/dev/null; then
  # the message is an argv item, so quotes or backslashes in it cannot break out into AppleScript
  osascript -e 'on run argv' -e 'display notification (item 1 of argv) with title "Claude Code"' -e 'end run' "$MESSAGE" >/dev/null 2>&1 &
fi

echo "Notification: $MESSAGE"
exit 0
