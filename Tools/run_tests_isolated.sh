#!/usr/bin/env bash
# Chạy toàn bộ test PlayMode trên MỘT BẢN SAO của project, không cần khóa Unity và không đụng
# Editor đang mở. Dùng clone APFS cho Library (tức thì, không tốn dung lượng).
# Mỗi agent nên đặt EOTV_AGENT (claude/codex/anti) để có bản sao riêng, vd.: EOTV_AGENT=codex Tools/run_tests_isolated.sh
#   Tools/run_tests_isolated.sh            # chạy tất cả
#   Tools/run_tests_isolated.sh Ability    # lọc theo tên (test filter)
set -euo pipefail

SRC="$(cd "$(dirname "$0")/.." && pwd)"
DST="${EOTV_TEST_PROJECT:-${TMPDIR:-/tmp}/eotv_test_project${EOTV_AGENT:+_$EOTV_AGENT}}"
UNITY="${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.3.13f1/Unity.app/Contents/MacOS/Unity}"
FILTER="${1:-}"

mkdir -p "$DST"

# Xếp hàng: nhiều agent có thể chạy script này cùng lúc, nhưng một bản sao chỉ chạy được một Unity.
# (Đặt EOTV_AGENT=claude|codex|anti để mỗi người có bản sao riêng và khỏi phải chờ nhau.)
LOCK="$DST.lock"
waited=0
while ! mkdir "$LOCK" 2>/dev/null || pgrep -f "projectPath $DST" >/dev/null 2>&1; do
  [ -d "$LOCK" ] && [ -n "$(find "$LOCK" -maxdepth 0 -mmin +30 2>/dev/null)" ] && rmdir "$LOCK" 2>/dev/null || true
  [ $waited -eq 0 ] && echo "Đang chờ lượt chạy test trên bản sao ($DST)..."
  waited=1; sleep 5
done
trap 'rmdir "$LOCK" 2>/dev/null || true' EXIT

if [ ! -d "$DST/Library" ] && [ -d "$SRC/Library" ]; then
  cp -cR "$SRC/Library" "$DST/Library" 2>/dev/null || cp -R "$SRC/Library" "$DST/Library"
fi

rsync -a --delete --exclude '.DS_Store' "$SRC/Assets" "$SRC/Packages" "$SRC/ProjectSettings" "$DST/"

rm -f "$DST/results.xml" "$DST/test.log"
ARGS=(-batchmode -nographics -projectPath "$DST" -runTests -testPlatform PlayMode
      -testResults "$DST/results.xml" -logFile "$DST/test.log")
[ -n "$FILTER" ] && ARGS+=(-testFilter "$FILTER")

set +e
"$UNITY" "${ARGS[@]}" >/dev/null 2>&1
CODE=$?
set -e

echo "unity exit code: $CODE  (0 = tất cả đạt, 2 = có test lỗi, khác = không chạy được)"
if grep -q "error CS" "$DST/test.log" 2>/dev/null; then
  echo "LỖI BIÊN DỊCH:"; grep "error CS" "$DST/test.log" | sort -u | head -20
fi
if [ -f "$DST/results.xml" ]; then
  python3 - "$DST/results.xml" <<'PY'
import re, sys
x = open(sys.argv[1]).read()
cases = re.findall(r'<test-case [^>]*?name="([^"]+)"[^>]*?result="(\w+)"', x)
passed = [n for n, r in cases if r == "Passed"]
failed = [(n, r) for n, r in cases if r != "Passed"]
print(f"đạt {len(passed)}/{len(cases)}")
for n, r in failed:
    print("  KHÔNG ĐẠT:", n.split('.')[-1], r)
for m in re.finditer(r'<message><!\[CDATA\[(.*?)\]\]></message>', x, re.S):
    t = m.group(1).strip()
    if 'child tests' not in t:
        print("  >", t.replace("\n", " ")[:300])
PY
else
  echo "Không có file kết quả. Log: $DST/test.log"
fi
exit $CODE
