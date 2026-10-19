"""Remove a project's local server override after verifying the paired public release exists.

Default: read-only plan. --apply creates an exact .env backup before replacing it.
This does not install a plugin, replace DLLs, or restart a running Unity/server process.
"""

import argparse
import json
from pathlib import Path
import subprocess
import sys
import uuid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', action='append', required=True)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    folder = Path(__file__).resolve().parent
    subprocess.run([sys.executable, str(folder / 'release_assets.py'), 'verify-remote'], check=True)
    version = json.loads((folder / 'sources.json').read_text(encoding='utf-8'))['packageVersion']
    key = 'UNITY_MCP_SERVER_PATH'
    plans = []
    for value in dict.fromkeys(args.project):
        project = Path(value).resolve(strict=True)
        if not (project / 'Packages/manifest.json').is_file():
            raise ValueError(f'Not a Unity project: {project}')
        installed = json.loads((project / 'Assets/Plugins/NuGet/.nuget-installed.json').read_text(encoding='utf-8-sig'))
        for package in ('com.IvanMurzak.McpPlugin', 'com.IvanMurzak.McpPlugin.Common'):
            if installed['packages'][package]['version'] != version:
                raise ValueError(f'Update the Unity package/dependencies first: {project}')
        env = project / '.env'
        original = env.read_bytes() if env.exists() else b''
        lines = original.splitlines(keepends=True)
        kept = []
        for line in lines:
            text = line.decode('utf-8-sig').lstrip()
            if not text.startswith('#') and '=' in text and text.split('=', 1)[0].strip() == key:
                continue
            kept.append(line)
        updated = b''.join(kept)
        print(json.dumps({'project': str(project), 'removeServerOverride': original != updated}))
        plans.append((env, original, updated))
    if not args.apply:
        return
    backup = folder.parents[1] / '.local/request-safety/hosted-migration' / uuid.uuid4().hex
    backup.mkdir(parents=True)
    records = []
    for index, (env, original, updated) in enumerate(plans):
        if original == updated:
            continue
        if env.read_bytes() != original:
            raise RuntimeError(f'Configuration changed during planning: {env}')
        saved = backup / f'{index}.env'
        saved.write_bytes(original)
        records.append({'env': str(env), 'backup': str(saved)})
    (backup / 'changes.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
    for env, original, updated in plans:
        if original == updated:
            continue
        if env.read_bytes() != original:
            raise RuntimeError(f'Configuration changed before replacement: {env}')
        temporary = env.parent / ('.env.hosted-' + uuid.uuid4().hex)
        temporary.write_bytes(updated)
        temporary.replace(env)
    print(f'Backups: {backup}. Restart Unity to download the paired server.')


if __name__ == '__main__':
    main()
