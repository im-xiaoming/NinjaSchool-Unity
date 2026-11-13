"""Compile current sources with Unity's own compiler/response files without touching a running Play.

This checks actual Unity references/defines, including the legacy client global types. It does not
refresh the Editor, certify serialized assets or replace the final Editor Console check after Stop.
"""

import argparse
import os
from pathlib import Path
import re
import subprocess


def main():
    root = Path(__file__).resolve().parents[2]
    os.chdir(root)
    version = re.search(
        r"m_EditorVersion: (\S+)", (root / "ProjectSettings/ProjectVersion.txt").read_text()
    )[1]
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--editor",
        type=Path,
        default=Path(os.environ.get("ProgramFiles", "C:/Program Files"))
        / "Unity/Hub/Editor"
        / version,
    )
    args = parser.parse_args()
    sdk = args.editor / "Editor/Data/DotNetSdk"
    csc = next((sdk / "sdk").glob("*/Roslyn/bincore/csc.dll"))
    output = root / ".utmp/unity-compiler"
    output.mkdir(parents=True, exist_ok=True)
    for assembly, sources in (
        ("Assembly-CSharp", ("Assets/Server", "Assets/Scripts")),
        ("Assembly-CSharp-Editor", ("Assets/Editor",)),
    ):
        responses = list((root / "Library/Bee").rglob(assembly + ".rsp"))
        if not responses:
            raise FileNotFoundError("Open the project in Unity to generate " + assembly + ".rsp")
        original = max(responses, key=lambda p: p.stat().st_mtime)
        lines = []
        for line in original.read_text(encoding="utf-8-sig").splitlines():
            path = line.strip('"').replace("\\", "/")
            if path.endswith(".cs") and path.startswith(tuple(s + "/" for s in sources)):
                continue
            if line.startswith("-out:"):
                line = f'-out:"{output / (assembly + ".dll")}"'
            if line.startswith("-refout:"):
                line = f'-refout:"{output / (assembly + ".ref.dll")}"'
            if assembly.endswith("-Editor") and line.startswith("-r:"):
                if "Assembly-CSharp.ref.dll" in line:
                    line = f'-r:"{output / "Assembly-CSharp.ref.dll"}"'
                elif "Assembly-CSharp.dll" in line:
                    line = f'-r:"{output / "Assembly-CSharp.dll"}"'
            lines.append(line)
        count = 0
        for directory in sources:
            for path in (root / directory).rglob("*.cs"):
                lines.append('"' + str(path) + '"')
                count += 1
        response = output / (assembly + ".current.rsp")
        response.write_text("\n".join(lines), encoding="utf-8")
        result = subprocess.run(
            [str(sdk / "dotnet.exe"), str(csc), "@" + str(response)], capture_output=True
        )
        log = result.stdout + result.stderr
        (output / (assembly + ".log")).write_bytes(log)
        if log:
            print(log.decode("utf-8", errors="replace"))
        print(f"{assembly}: {count} current sources, Unity compiler exit {result.returncode}")
        if result.returncode:
            return result.returncode
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
