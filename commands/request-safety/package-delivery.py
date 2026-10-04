"""Build reviewable native server assets and an offline Windows UPM package. No upload."""
import argparse
import hashlib
import io
import json
import tarfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / '.local/request-safety'
PACKAGE = ROOT / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp'
RIDS = ('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'win-x86', 'osx-x64', 'osx-arm64')


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    args = argparse.ArgumentParser()
    args.add_argument('--nuget-cache', type=Path, required=True)
    config = args.parse_args()
    pins = json.loads((ROOT / 'commands/request-safety/sources.json').read_text(encoding='utf-8'))
    plugin = json.loads((PACKAGE / 'package.json').read_text(encoding='utf-8'))
    output = BUILD / 'delivery/assets'
    output.mkdir(parents=True, exist_ok=True)
    bundle = PACKAGE / 'Editor/Delivery~'
    bundle.mkdir(parents=True, exist_ok=True)
    packages = []
    for name, version in [('com.IvanMurzak.McpPlugin', pins['packageVersion']),
                          ('com.IvanMurzak.McpPlugin.Common', pins['packageVersion']),
                          ('com.IvanMurzak.ReflectorNet', '5.4.1')]:
        filename = f'{name}.{version}.nupkg'
        source = BUILD / 'feed' / filename
        if not source.exists():
            source = config.nuget_cache / name.lower() / version / filename.lower()
        target = bundle / filename
        target.write_bytes(source.read_bytes())
        packages.append(dict(id=name, version=version, fileName=filename, sha256=digest(target)))
    servers = []
    for rid in RIDS:
        folder = BUILD / 'delivery/publish' / rid
        (folder / 'LICENSE').write_bytes((BUILD / 'deps/GameDev-MCP-Server/LICENSE').read_bytes())
        executable = 'gamedev-mcp-server.exe' if rid.startswith('win-') else 'gamedev-mcp-server'
        filename = f'gamedev-mcp-server-{rid}.zip'
        archive = output / filename
        if not (folder / executable).is_file():
            raise RuntimeError(f'Missing actual published executable: {folder / executable}')
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as zip_out:
            for file in sorted(folder.rglob('*')):
                if not file.is_file() or file.suffix == '.pdb':
                    continue
                item = zipfile.ZipInfo(file.relative_to(folder).as_posix(), date_time=(2026, 1, 1, 0, 0, 0))
                item.create_system = 3
                item.external_attr = (0o100755 if file.name == executable else 0o100644) << 16
                item.compress_type = zipfile.ZIP_DEFLATED
                zip_out.writestr(item, file.read_bytes())
        servers.append(dict(id=rid, version=pins['serverVersion'], fileName=filename,
                            sha256=digest(archive), executableSha256=digest(folder / executable)))
    manifest = dict(schema=1, pluginVersion=plugin['version'], apiVersion=pins['apiVersion'],
                    serverVersion=pins['serverVersion'],
                    releaseBaseUrl='https://github.com/Misaka16608/Unity-MCP/releases/download/request-safety-9.2.9-2',
                    packages=packages, servers=servers)
    encoded = json.dumps(manifest, indent=2).encode() + b'\n'
    (bundle / 'manifest.json').write_bytes(encoded)
    (output / 'manifest.json').write_bytes(encoded)
    # Include the matching native Windows archive in a local installable UPM artifact.
    # Git installs use the manifest's release URL; ZIPs never enter the source repository.
    tgz = output / f'com.ivanmurzak.unity.mcp-{plugin["version"]}-win-x64.tgz'
    with tarfile.open(tgz, 'w:gz') as tar:
        files = [(p, 'package/' + p.relative_to(PACKAGE).as_posix()) for p in sorted(PACKAGE.rglob('*'))
                 if p.is_file() and 'servers' not in p.relative_to(PACKAGE).parts]
        files.append((output / 'gamedev-mcp-server-win-x64.zip', 'package/Editor/Delivery~/servers/gamedev-mcp-server-win-x64.zip'))
        for file, name in files:
            entry = tarfile.TarInfo(name)
            entry.size = file.stat().st_size
            entry.mode = 0o644
            entry.mtime = 1767225600
            tar.addfile(entry, io.BytesIO(file.read_bytes()))
    (output / 'SHA256SUMS').write_text(''.join(f'{digest(p)}  {p.name}\n' for p in sorted(output.iterdir())
                                             if p.is_file() and p.name != 'SHA256SUMS'), encoding='utf-8')
    print(f'Created paired manifest, 7 native archives and offline Windows UPM package: {output}')


if __name__ == '__main__':
    main()
