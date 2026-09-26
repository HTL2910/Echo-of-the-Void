#!/usr/bin/env python3
"""
Kiểm tra phạm vi sở hữu thư mục giữa 3 agent (xem Docs/ke_hoach_den_100.md mục 1).

Cách dùng (chạy ở thư mục gốc project):
  python3 Tools/check_ownership.py --agent codex --working        # thay đổi chưa commit
  python3 Tools/check_ownership.py --agent anti  --staged         # đã git add
  python3 Tools/check_ownership.py --range main~5..main           # mỗi commit: agent lấy từ tiền tố [claude]/[codex]/[anti]
  python3 Tools/check_ownership.py --range HEAD~3..HEAD --agent codex   # ép tất cả commit về một agent

Thoát mã 0 nếu sạch, 1 nếu có file ngoài phạm vi. Commit có `[integrate]` (chỉ Claude) được phép chạm mọi nơi.
"""
import argparse
import re
import subprocess
import sys

# Thư mục/tệp mỗi agent ĐƯỢC sửa (khớp theo tiền tố đường dẫn). Ghi chồng lên nhau là chủ ý (vd. prefab).
ALLOWED = {
    "anti": [
        "Assets/Art/", "Assets/Audio/", "Assets/Fonts/", "Assets/Prefabs/", "Assets/Scenes/Zone",
        "Assets/CREDITS.md", "Assets/Data/Text/", "Docs/noi_dung/",
        "Docs/task_antigravity.md", "Docs/yeu_cau_tu_anti.md", "Docs/yeu_cau_giua_agent.md", "Docs/ban_giao/",
    ],
    "codex": [
        "Assets/Scripts/Enemies/", "Assets/Scripts/Combat/", "Assets/Scripts/Bosses/",
        "Assets/Scripts/Environment/Mechanics/", "Assets/Editor/Codex/",
        "Assets/Tests/PlayMode/Enemy", "Assets/Tests/PlayMode/Boss", "Assets/Tests/PlayMode/Mechanic",
        "Assets/Prefabs/Enemies/", "Assets/Prefabs/Bosses/", "Assets/Prefabs/Mechanics/",
        "Assets/Settings/Enemies/",
        "Docs/task_codex.md", "Docs/yeu_cau_giua_agent.md", "Docs/ban_giao/",
    ],
    "claude": [
        "Assets/Scripts/Player/", "Assets/Scripts/Core/", "Assets/Scripts/UI/", "Assets/Scripts/Save/",
        "Assets/Scripts/Feedback/", "Assets/Scripts/Settings/", "Assets/Scripts/EchoOfTheVoid.Runtime.asmdef",
        "Assets/Scripts/Environment/",  # trừ Mechanics/ (kiểm bên dưới)
        "Assets/Editor/", "Assets/Tests/", "Assets/Scenes/", "Assets/Prefabs/Levels/", "Assets/Prefabs/Player/",
        "Assets/Prefabs/Environment/",
        "Assets/Plugins/", "Assets/Settings/", "Assets/DefaultVolumeProfile.asset",
        "ProjectSettings/", "Packages/", "Tools/",
        "Docs/", ".gitignore", ".claude/", "CLAUDE.md", "production/", "design/", "prototypes/",
    ],
}
# Vùng độc quyền: Claude không được vào trừ khi commit có [integrate]
CLAUDE_FORBIDDEN = [
    "Assets/Scripts/Enemies/", "Assets/Scripts/Combat/", "Assets/Scripts/Bosses/",
    "Assets/Settings/Enemies/",
    "Assets/Scripts/Environment/Mechanics/", "Assets/Editor/Codex/",
    "Assets/Art/", "Assets/Audio/", "Assets/Fonts/",
]
PREFIX = re.compile(r"^\[(claude|codex|anti)\](\s*\[integrate\])?", re.I)


def git(*args):
    return subprocess.run(["git", *args], capture_output=True, text=True, check=True).stdout


def violations(agent, files, integrate=False):
    bad = []
    for f in files:
        if agent == "claude":
            if not integrate and any(f.startswith(p) for p in CLAUDE_FORBIDDEN):
                bad.append(f)
            elif not any(f.startswith(p) or f == p.rstrip('/') + '.meta' for p in ALLOWED["claude"]) and not integrate:
                bad.append(f)
        elif not any(f.startswith(p) or f == p.rstrip('/') + '.meta' for p in ALLOWED[agent]):
            bad.append(f)
    return bad


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--agent", choices=ALLOWED.keys())
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--working", action="store_true", help="thay đổi chưa commit")
    g.add_argument("--staged", action="store_true", help="thay đổi đã git add")
    g.add_argument("--range", help="khoảng commit, vd. HEAD~5..HEAD")
    a = ap.parse_args()
    failed = False

    if a.working or a.staged:
        if not a.agent:
            ap.error("--agent bắt buộc với --working/--staged")
        cmd = ["diff", "--name-only"] + (["--cached"] if a.staged else ["HEAD"])
        files = [x for x in git(*cmd).splitlines() if x]
        if a.working:
            files += [x for x in git("ls-files", "--others", "--exclude-standard").splitlines() if x]
        bad = violations(a.agent, sorted(set(files)))
        print(f"[{a.agent}] {len(set(files))} file thay đổi, {len(bad)} ngoài phạm vi")
        for f in bad:
            print("  NGOÀI PHẠM VI:", f)
        failed = bool(bad)
    else:
        for line in git("log", "--format=%h%x09%s", a.range).splitlines():
            sha, _, subject = line.partition("\t")
            m = PREFIX.match(subject)
            agent = a.agent or (m.group(1).lower() if m else None)
            if agent is None:
                print(f"{sha} bỏ qua (không có tiền tố [claude]/[codex]/[anti]): {subject[:60]}")
                continue
            integrate = bool(m and m.group(2)) and agent == "claude"
            files = [x for x in git("show", "--name-only", "--format=", sha).splitlines() if x]
            bad = violations(agent, files, integrate)
            status = "OK " if not bad else "LỖI"
            print(f"{sha} [{agent}{' integrate' if integrate else ''}] {status} {len(files)} file: {subject[:50]}")
            for f in bad:
                print("     NGOÀI PHẠM VI:", f)
            failed |= bool(bad)

    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
