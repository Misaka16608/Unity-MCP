"""Verify sealed delivery bytes, optionally extract Windows into a fresh scratch directory."""
import argparse
import hashlib
import json
import tarfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp'
BUILD = ROOT / '.local/request-safety'


def check(data, expected, name):
    if hashlib.sha256(data).hexdigest() != expected:
        raise RuntimeError(f'Checksum mismatch: {name}')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--extract-windows', type=Path)
    args = parser.parse_args()
    assets = BUILD / 'delivery/assets'
    manifest_bytes = (PACKAGE / 'Editor/Delivery~/manifest.json').read_bytes()
    if manifest_bytes != (assets / 'manifest.json').read_bytes():
        raise RuntimeError('Candidate and committed manifests differ')
    manifest = json.loads(manifest_bytes)
    for entry in manifest['packages']:
        check((PACKAGE / 'Editor/Delivery~' / entry['fileName']).read_bytes(), entry['sha256'], entry['fileName'])
    for entry in manifest['servers']:
        archive = assets / entry['fileName']
        check(archive.read_bytes(), entry['sha256'], archive.name)
        with zipfile.ZipFile(archive) as zip_in:
            executable = 'gamedev-mcp-server.exe' if entry['id'].startswith('win-') else 'gamedev-mcp-server'
            check(zip_in.read(executable), entry['executableSha256'], entry['id'] + ' executable')
            if not entry['id'].startswith('win-') and not (zip_in.getinfo(executable).external_attr >> 16) & 0o111:
                raise RuntimeError(f'Missing executable permission: {entry["id"]}')
    tgz = assets / f'com.ivanmurzak.unity.mcp-{manifest["pluginVersion"]}-win-x64.tgz'
    with tarfile.open(tgz) as tar:
        win = next(p for p in manifest['servers'] if p['id'] == 'win-x64')
        for entry in tar.getmembers():
            relative = Path(entry.name).relative_to('package')
            if '..' in relative.parts or not entry.isfile():
                raise RuntimeError(f'Unexpected UPM member: {entry.name}')
            data = tar.extractfile(entry).read()
            if relative.as_posix() == 'Editor/Delivery~/servers/' + win['fileName']:
                check(data, win['sha256'], 'embedded Windows archive')
                continue
            if data != (PACKAGE / relative).read_bytes():
                raise RuntimeError(f'Stale UPM source or dependency: {relative}')
    if args.extract_windows:
        target = args.extract_windows.resolve()
        target.relative_to(BUILD.resolve())  # Never extract outside this task's scratch.
        if target.exists():
            raise RuntimeError('Smoke extraction must use a fresh directory')
        with zipfile.ZipFile(assets / win['fileName']) as zip_in:
            for entry in zip_in.infolist():
                member = Path(entry.filename)
                if member.is_absolute() or '..' in member.parts:
                    raise RuntimeError('Escaping archive member')
            zip_in.extractall(target)
    print('Verified 3 bundled packages, 7 archives/executables, Unix permissions and every Windows UPM member.')


if __name__ == '__main__':
    main()
