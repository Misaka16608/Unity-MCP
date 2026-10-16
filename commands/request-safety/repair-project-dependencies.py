"""Repair McpPlugin, Common and ReflectorNet in explicitly selected Unity projects.

Defaults to a read-only plan. --apply backs up changed files under .local first.
Existing PluginImporter metadata and unrelated manifest entries are preserved.
"""

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import uuid


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", action="append", required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    source = repo / "Unity-MCP-Plugin/Assets/Plugins/NuGet"
    installed = json.loads((source / ".nuget-installed.json").read_text())["packages"]
    source_packages = {name: installed[name] for name in (
        "com.IvanMurzak.McpPlugin.Common", "com.IvanMurzak.McpPlugin",
        "com.IvanMurzak.ReflectorNet",
    )}
    expected = {}
    for entry in source_packages.values():
        for name in entry["dlls"]:
            if Path(name).name != name or not (source / name).is_file():
                raise ValueError(f"Missing or invalid source dependency: {name}")
            expected[name] = digest(source / name)

    plans = []
    for project in dict.fromkeys(args.project):
        root = Path(project).resolve(strict=True)
        target = root / "Assets/Plugins/NuGet"
        if not (root / "Packages/manifest.json").is_file():
            raise ValueError(f"Not a Unity project: {root}")
        manifest_path = target / ".nuget-installed.json"
        original = manifest_path.read_bytes()
        manifest = json.loads(original)
        changes = []
        for name, checksum in expected.items():
            path = target / name
            old_hash = digest(path)
            if old_hash != checksum:
                changes.append((path, (source / name).read_bytes(), old_hash))
        for package_id, entry in source_packages.items():
            # Never replace another package's ownership of a DLL.
            for other_id, other_entry in manifest["packages"].items():
                if other_id != package_id and set(entry["dlls"]) & set(other_entry["dlls"]):
                    raise ValueError(f"Conflicting DLL ownership in {root}: {other_id}")
            manifest["packages"][package_id] = entry
        if manifest != json.loads(original):
            payload = (json.dumps(manifest, indent=2, ensure_ascii=False) + "\n").encode()
            changes.append((manifest_path, payload, digest(manifest_path)))
        plans.append((root, changes))
        print(json.dumps({"project": str(root), "changes": [str(p.relative_to(root))
                          for p, _, _ in changes]}, ensure_ascii=False))
    if not args.apply:
        return

    backup_root = repo / ".local/request-safety/project-repair" / uuid.uuid4().hex
    backup_root.mkdir(parents=True)
    records = []
    for index, (root, changes) in enumerate(plans):
        for path, payload, old_hash in changes:
            if digest(path) != old_hash:
                raise RuntimeError(f"File changed during planning: {path}")
            backup = backup_root / str(index) / path.relative_to(root)
            if path.exists():
                backup.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, backup)
            records.append({"path": str(path), "backup": str(backup) if old_hash else None,
                            "before": old_hash, "after": hashlib.sha256(payload).hexdigest()})
    (backup_root / "changes.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
    print(f"Backup: {backup_root}", flush=True)

    written = []
    try:
        for root, changes in plans:
            for path, payload, old_hash in changes:
                if digest(path) != old_hash:
                    raise RuntimeError(f"File changed before replacement: {path}")
                temporary = root / "Library" / ("mcp-dependency-repair-" + uuid.uuid4().hex)
                temporary.parent.mkdir(parents=True, exist_ok=True)
                try:
                    temporary.write_bytes(payload)
                    temporary.replace(path)
                finally:
                    temporary.unlink(missing_ok=True)
                written.append(path)
                if digest(path) != hashlib.sha256(payload).hexdigest():
                    raise RuntimeError(f"Post-write checksum mismatch: {path}")
            print(f"Repaired: {root}", flush=True)
    except Exception:
        for record in reversed(records):
            path = Path(record["path"])
            if path not in written:
                continue
            if digest(path) != record["after"]:
                print(f"Concurrent change; restore manually from {record['backup']}: {path}")
                continue
            if record["backup"]:
                shutil.copy2(record["backup"], path)
            else:
                path.unlink()
        raise


if __name__ == "__main__":
    main()
