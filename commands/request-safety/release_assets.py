"""Assemble and validate the immutable GitHub Release containing all fork dependencies."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

REPO = Path(__file__).resolve().parents[2]
BUILD = REPO / '.local/request-safety'
SOURCES = json.loads((Path(__file__).parent / 'sources.json').read_text(encoding='utf-8-sig'))
OWNER_REPO = 'Misaka16608/Unity-MCP'
RIDS = ('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')
PACKAGES = ('com.IvanMurzak.McpPlugin', 'com.IvanMurzak.McpPlugin.Common')
TAG = 'v' + SOURCES['serverVersion']


def expected_assets():
    return [f'{p}.{SOURCES["packageVersion"]}.nupkg' for p in PACKAGES] + [
        f'gamedev-mcp-server-{rid}.zip' for rid in RIDS]


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def check_config():
    package = REPO / 'Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp'
    helper = (package / 'Editor/DependencyResolver/NuGetPackage.cs').read_text(encoding='utf-8-sig')
    manager = (package / 'Editor/Scripts/McpServerManager.cs').read_text(encoding='utf-8-sig')
    cli = (REPO / 'cli/src/utils/server-version.ts').read_text(encoding='utf-8-sig')
    for text, pattern, value in (
        (helper, r'const string Repository = "([^"]+)"', OWNER_REPO),
        (helper, r'const string PackageVersion = "([^"]+)"', SOURCES['packageVersion']),
        (helper, r'const string ServerVersion = "([^"]+)"', SOURCES['serverVersion']),
        (manager, r'const string ServerVersion = "([^"]+)"', SOURCES['serverVersion']),
        (cli, r"DEFAULT_SERVER_VERSION = '([^']+)'", SOURCES['serverVersion']),
        (cli, r"SERVER_RELEASE_REPO = '([^']+)'", OWNER_REPO),
    ):
        match = re.search(pattern, text)
        if not match or match[1] != value:
            raise ValueError(f'Release configuration drift: {pattern}, expected {value}')
    pins = (package / 'Editor/DependencyResolver/NuGetConfig.cs').read_text(encoding='utf-8-sig')
    for name in PACKAGES[:1]:
        if not re.search(r'new\("' + re.escape(name) + r'",\s*"' + re.escape(SOURCES['packageVersion']) + r'"', pins):
            # Older resolver syntax uses an explicit NuGetPackage constructor.
            if not re.search(r'new NuGetPackage\("' + re.escape(name) + r'",\s*"' + re.escape(SOURCES['packageVersion']) + r'"', pins):
                raise ValueError(f'Resolver pin drift: {name}')
    print(f'Paired release configuration verified: {OWNER_REPO} {TAG}')


def verify_package(path, name):
    with zipfile.ZipFile(path) as archive:
        spec = next(n for n in archive.namelist() if n.endswith('.nuspec'))
        root = ET.fromstring(archive.read(spec))
        metadata = next(e for e in root if e.tag.split('}')[-1] == 'metadata')
        fields = {e.tag.split('}')[-1]: e.text for e in metadata}
        if fields['id'] != name or fields['version'] != SOURCES['packageVersion']:
            raise ValueError(f'Unexpected package identity: {path}')
        if 'lib/netstandard2.1/' + name.removeprefix('com.IvanMurzak.') + '.dll' not in archive.namelist():
            raise ValueError(f'Missing Unity-compatible DLL: {path}')


def assemble():
    check_config()
    output = BUILD / 'release-assets' / TAG
    if output.exists() and any(output.iterdir()):
        raise ValueError(f'Preserve/remove previous artifacts before rebuilding: {output}')
    output.mkdir(parents=True, exist_ok=True)
    for name in PACKAGES:
        filename = f'{name}.{SOURCES["packageVersion"]}.nupkg'
        source = BUILD / 'feed' / filename
        verify_package(source, name)
        shutil.copy2(source, output / filename)
    for rid in RIDS:
        publish = BUILD / 'release-publish' / rid
        executable = 'gamedev-mcp-server' + ('.exe' if rid.startswith('win-') else '')
        if not (publish / executable).is_file():
            raise ValueError(f'Missing {rid} self-contained apphost')
        with zipfile.ZipFile(output / f'gamedev-mcp-server-{rid}.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
            for path in sorted(publish.rglob('*')):
                if not path.is_file() or path.suffix == '.pdb':
                    continue
                info = zipfile.ZipInfo(path.relative_to(publish).as_posix())
                info.create_system = 3
                mode = 0o755 if path.name == executable else 0o644
                info.external_attr = (stat.S_IFREG | mode) << 16
                info.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(info, path.read_bytes())
            archive.writestr('version', SOURCES['serverVersion'] + '\n')
            for project in ('MCP-Plugin-dotnet', 'GameDev-MCP-Server'):
                archive.write(BUILD / 'deps' / project / 'LICENSE', f'licenses/{project}-LICENSE')
    provenance = dict(SOURCES)
    provenance['unityCommit'] = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=REPO, text=True).strip()
    provenance['repository'] = OWNER_REPO
    provenance['runtimeIdentifiers'] = RIDS
    (output / 'sources.json').write_text(json.dumps(provenance, indent=2) + '\n', encoding='utf-8')
    names = expected_assets() + ['sources.json']
    (output / 'SHA256SUMS').write_text(''.join(f'{digest(output / n)}  {n}\n' for n in names), encoding='ascii')
    validate(output)
    print(f'Ready to publish {len(names) + 1} assets: {output}')


def validate(output):
    checksums = {}
    for line in (output / 'SHA256SUMS').read_text(encoding='utf-8-sig').splitlines():
        match = re.fullmatch(r'([0-9a-f]{64})  ([^/\\]+)', line)
        if not match or match[2] in checksums:
            raise ValueError('Malformed or duplicate checksum entry')
        checksums[match[2]] = match[1]
    if set(checksums) != set(expected_assets() + ['sources.json']):
        raise ValueError('Release asset set is incomplete')
    for name, checksum in checksums.items():
        if digest(output / name) != checksum:
            raise ValueError(f'Checksum mismatch: {name}')
    for name in PACKAGES:
        verify_package(output / f'{name}.{SOURCES["packageVersion"]}.nupkg', name)
    for rid in RIDS:
        with zipfile.ZipFile(output / f'gamedev-mcp-server-{rid}.zip') as archive:
            executable = 'gamedev-mcp-server' + ('.exe' if rid.startswith('win-') else '')
            if archive.read('version').decode().strip() != SOURCES['serverVersion']:
                raise ValueError(f'Wrong version marker: {rid}')
            if not rid.startswith('win-') and not archive.getinfo(executable).external_attr >> 16 & 0o111:
                raise ValueError(f'Lost executable permissions: {rid}')
            if archive.testzip() is not None:
                raise ValueError(f'Corrupt archive: {rid}')
    print('Release assets and SHA256SUMS verified')


def verify_remote():
    check_config()
    url = f'https://api.github.com/repos/{OWNER_REPO}/releases/tags/{TAG}'
    request = urllib.request.Request(url, headers={'User-Agent': 'Unity-MCP-Release-Check'})
    with urllib.request.urlopen(request, timeout=60) as response:
        release = json.load(response)
    assets = {asset['name'] for asset in release['assets']}
    if release['draft'] or not set(expected_assets() + ['sources.json', 'SHA256SUMS']) <= assets:
        raise ValueError(f'Publish the complete paired dependency release {TAG} first')
    print(f'Published paired release available: {release["html_url"]}')


def fetch_packages():
    """Bootstrap dotnet restore from verified public release assets on a clean CI machine."""
    check_config()
    feed = BUILD / 'feed'
    feed.mkdir(parents=True, exist_ok=True)
    base = f'https://github.com/{OWNER_REPO}/releases/download/{TAG}/'
    def download(name):
        request = urllib.request.Request(base + name, headers={'User-Agent': 'Unity-MCP-Dependency-Restore'})
        with urllib.request.urlopen(request, timeout=120) as response:
            return response.read()
    manifest = {}
    for line in download('SHA256SUMS').decode('ascii').splitlines():
        match = re.fullmatch(r'([0-9a-f]{64})  ([^/\\]+)', line)
        if not match or match[2] in manifest:
            raise ValueError('Malformed checksum manifest')
        manifest[match[2]] = match[1]
    for package in PACKAGES:
        name = f'{package}.{SOURCES["packageVersion"]}.nupkg'
        data = download(name)
        if manifest.get(name) != hashlib.sha256(data).hexdigest():
            raise ValueError(f'Invalid hosted package checksum: {name}')
        temporary = feed / (name + '.tmp')
        temporary.write_bytes(data)
        verify_package(temporary, package)
        temporary.replace(feed / name)
    if os.environ.get('GITHUB_ENV'):
        with open(os.environ['GITHUB_ENV'], 'a', encoding='utf-8') as env:
            env.write(f'RestoreAdditionalProjectSources={feed.as_posix()}\n')
    print(f'Local dotnet feed ready: {feed}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=('check-config', 'assemble', 'validate', 'verify-remote', 'fetch-packages'))
    args = parser.parse_args()
    if args.command == 'validate':
        validate(BUILD / 'release-assets' / TAG)
    else:
        {'check-config': check_config, 'assemble': assemble, 'verify-remote': verify_remote,
         'fetch-packages': fetch_packages}[args.command]()
