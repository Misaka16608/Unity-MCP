"""Install the locally built paired server and set a project's launch override.

This does not stop/start a server or change the project's port or auth settings.
The default is a read-only plan; --apply backs up .env before changing one key.
"""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import uuid


def checksum(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", action="append", required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    build = repo / ".local/request-safety"
    versions = json.loads((Path(__file__).parent / "sources.json").read_text())
    version = versions["serverVersion"]
    if not version or any(c not in "0123456789abcdefghijklmnopqrstuvwxyz.-" for c in version):
        raise ValueError("Invalid paired server version")
    publish = build / "publish"
    for name in ("gamedev-mcp-server.exe", "gamedev-mcp-server.dll",
                 "gamedev-mcp-server.runtimeconfig.json", "McpPlugin.Common.dll"):
        if not (publish / name).is_file():
            raise ValueError(f"Build the paired server first: missing {name}")
    if versions["apiVersion"].encode("utf-16le") not in (publish / "McpPlugin.Common.dll").read_bytes():
        raise ValueError("Built server does not contain the required API version")
    source_files = [p for p in publish.rglob("*") if p.is_file()]
    cache_files = list((build / "feed").glob(f"*.{versions['packageVersion']}.nupkg"))
    if len(cache_files) != 2:
        raise ValueError("Missing paired plugin/Common restore packages")
    source_hashes = {str(p.relative_to(publish)): checksum(p) for p in source_files}
    plans = []
    key = "UNITY_MCP_SERVER_PATH"
    for project in dict.fromkeys(args.project):
        root = Path(project).resolve(strict=True)
        if not (root / "Packages/manifest.json").is_file():
            raise ValueError(f"Not a Unity project: {root}")
        installed = json.loads((root / "Assets/Plugins/NuGet/.nuget-installed.json").read_text())
        for package in ("com.IvanMurzak.McpPlugin", "com.IvanMurzak.McpPlugin.Common"):
            if installed["packages"][package]["version"] != versions["packageVersion"]:
                raise ValueError(f"Project plugin is not the matching version: {root}")
        target = root / "Library/mcp-server-paired" / version
        executable = target / "gamedev-mcp-server.exe"
        env_path = root / ".env"
        original = env_path.read_bytes() if env_path.exists() else b""
        # Keep unrelated configuration bytes intact; the resolver uses last-key-wins.
        value = None
        for line in original.decode("utf-8-sig").splitlines():
            if line.strip().startswith("#") or "=" not in line:
                continue
            name, entry = line.split("=", 1)
            if name.strip() == key:
                value = entry.strip().strip("\"'")
        payload = original
        if value != executable.as_posix():
            newline = b"\r\n" if b"\r\n" in original else b"\n"
            if payload and not payload.endswith(b"\n"):
                payload += newline
            payload += f'{key}="{executable.as_posix()}"'.encode() + newline
        needs_install = not target.exists() or any(
            not (target / relative).is_file() or checksum(target / relative) != expected
            for relative, expected in source_hashes.items())
        if target.exists() and needs_install:
            raise ValueError(f"Existing versioned install differs; preserve it before replacing: {target}")
        plans.append((root, target, env_path, original, payload, needs_install))
        print(json.dumps({"project": str(root), "server": str(executable),
                          "install": needs_install, "updateEnv": original != payload}))
    if not args.apply:
        return

    backup = build / "server-repair" / uuid.uuid4().hex
    backup.mkdir(parents=True)
    records = []
    for index, (root, target, env_path, original, payload, needs_install) in enumerate(plans):
        if (env_path.read_bytes() if env_path.exists() else b"") != original:
            raise RuntimeError(f"Configuration changed during planning: {env_path}")
        if env_path.exists():
            shutil.copy2(env_path, backup / f"{index}.env")
        records.append({"project": str(root), "envExisted": env_path.exists(),
                        "backup": str(backup / f"{index}.env"), "server": str(target)})
    (backup / "changes.json").write_text(json.dumps(records, indent=2))
    print(f"Backup: {backup}", flush=True)
    for root, target, env_path, original, payload, needs_install in plans:
        if needs_install:
            staging = target.with_name(version + ".staging-" + uuid.uuid4().hex)
            shutil.copytree(publish, staging)
            for relative, expected in source_hashes.items():
                if checksum(staging / relative) != expected:
                    raise RuntimeError(f"Staged server checksum mismatch: {relative}")
            staging.rename(target)
        cache = root / "Library/NuGetCache"
        cache.mkdir(parents=True, exist_ok=True)
        for package in cache_files:
            destination = cache / package.name
            if destination.exists() and checksum(destination) != checksum(package):
                shutil.copy2(destination, backup / (root.name + "-" + package.name))
            shutil.copy2(package, destination)
        if original != payload:
            if (env_path.read_bytes() if env_path.exists() else b"") != original:
                raise RuntimeError(f"Configuration changed before replacement: {env_path}")
            temporary = root / "Library" / ("mcp-env-" + uuid.uuid4().hex)
            temporary.write_bytes(payload)
            temporary.replace(env_path)
        print(f"Configured paired server: {root}", flush=True)


if __name__ == "__main__":
    main()
