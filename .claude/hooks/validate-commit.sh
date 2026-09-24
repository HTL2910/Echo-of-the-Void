#!/bin/bash
# Claude Code PreToolUse hook: checks `git commit` commands. Advisory only (always exit 0).
# Adapted for Echo of the Void:
#   1. the message must start with [claude], [codex] or [anti]  (Docs/tich_hop.md)
#   2. the staged files must stay inside that agent's folders   (Tools/check_ownership.py)
#   3. no undefined tags / unchecked TODO style in C#
INPUT=$(cat)

if command -v jq >/dev/null 2>&1; then
    COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')
else
    COMMAND=$(echo "$INPUT" | grep -oE '"command"[[:space:]]*:[[:space:]]*"[^"]*"' | sed 's/"command"[[:space:]]*:[[:space:]]*"//;s/"$//')
fi

echo "$COMMAND" | grep -qE '(^|[;&|][[:space:]]*)git[[:space:]]+commit' || exit 0

STAGED=$(git diff --cached --name-only 2>/dev/null)
[ -z "$STAGED" ] && exit 0

WARNINGS=""

# 1. commit message prefix
MSG=$(echo "$COMMAND" | grep -oE -- '-m[[:space:]]+["'"'"'][^"'"'"']*' | head -1 | sed -E 's/^-m[[:space:]]+["'"'"']//')
AGENT=""
if [ -n "$MSG" ]; then
    AGENT=$(echo "$MSG" | grep -oiE '^\[(claude|codex|anti)\]' | tr -d '[]' | tr 'A-Z' 'a-z')
    [ -z "$AGENT" ] && WARNINGS="$WARNINGS\nMESSAGE: should start with [claude], [codex] or [anti]"
fi

# 2. ownership of the staged files (skipped for [integrate] commits)
if [ -n "$AGENT" ] && ! echo "$MSG" | grep -qi '\[integrate\]' && [ -f Tools/check_ownership.py ]; then
    OUT=$(python3 Tools/check_ownership.py --agent "$AGENT" --staged 2>&1 | grep 'NGOÀI PHẠM VI')
    [ -n "$OUT" ] && WARNINGS="$WARNINGS\nOWNERSHIP ($AGENT): staged files outside its folders:\n$OUT"
fi

# 3. C# hygiene
CS_FILES=$(echo "$STAGED" | grep -E '^Assets/Scripts/.*\.cs$')
if [ -n "$CS_FILES" ]; then
    while IFS= read -r file; do
        [ -f "$file" ] || continue
        if grep -nE 'tag[[:space:]]*=[[:space:]]*"(Enemy|Boss|Hazard)"' "$file" >/dev/null 2>&1; then
            WARNINGS="$WARNINGS\nTAG: $file assigns a tag that is not defined in the project (use a layer)"
        fi
        if grep -nE '(TODO|FIXME|HACK)[^(]' "$file" >/dev/null 2>&1; then
            WARNINGS="$WARNINGS\nSTYLE: $file has TODO/FIXME without an owner tag. Use TODO(name)."
        fi
    done <<< "$CS_FILES"
fi

if [ -n "$WARNINGS" ]; then
    echo -e "=== Commit warnings (advisory) ===$WARNINGS\n==================================" >&2
fi
exit 0
