# Paired native plugin delivery candidate

This fork's candidate pairs Unity package `0.93.3-request-safety.1`, framework
`8.6.0-request-safety.2`, wire API `2.0.0-request-safety.2`, and native server
`9.2.9-request-safety.2`. The three bundled NuGet packages and seven native archive
and executable hashes are sealed in the package's `Editor/Delivery~/manifest.json`.
The repaired packages do not need a public NuGet release.

## Install the Windows candidate

1. Finish active Unity work before installing a UPM upgrade: UPM can reload the
   editor domain independently of the server update drain.
2. In Unity Package Manager, use **Add package from tarball** and select
   `.local/request-safety/delivery/assets/com.ivanmurzak.unity.mcp-0.93.3-request-safety.1-win-x64.tgz`.
3. Let the dependency resolver install the bundled Common/plugin/ReflectorNet
   packages and compile. Other standard dependencies still restore from NuGet
   when absent from the project's cache; this is not a completely offline fresh
   Unity project installer.
4. The native Windows server archive is inside this tarball. No separate .NET
   installation or server-path environment override is needed for the bundled
   server. For automatic local startup/reconnection use the project's existing
   **Custom** mode, **Keep server running**, and **Keep connected** settings.
5. The plugin window shows dependency, preparation, wait, installation and failure
   stages. Its existing Unity connection indicator reports the real connection
   state separately; a verified executable is not proof of a connection.

The business project is never installed into by these build or verification scripts.
An explicit `UNITY_MCP_SERVER_PATH` override retains precedence; its API must match
the plugin. This delivery covers native processes. Existing public Docker/cloud
servers have not received this fork's API repair and are not paired targets.

## Automatic server update

The installer verifies the archive against a hash carried inside the Unity package,
extracts to a sibling staging directory, verifies the executable, then acquires
maintenance leases on both real tool managers. Fresh executions receive a
`not admitted` error; duplicates reuse their results and operation status/cancel
remain available. Accepted durable children can finish. The installer waits for
both manager executions and detached durable operations, postponing after five
minutes without canceling or repeating their work.

Only then does the existing stop/atomic-publish/restart sequence run. Startup is
deferred until the previous domain's owned PID has been checked. A stored PID
whose executable path differs is not adopted. Occupied ports are reported without
killing other servers. The binary and version marker are installed together.

## Build and verify

```powershell
pwsh -NoProfile -File commands/request-safety/build.ps1 -SkipPublish -SeedToolProject -Delivery
pwsh -NoProfile -File commands/request-safety/compile-unity-sources.ps1
python commands/request-safety/verify-delivery.py
pwsh -NoProfile -File commands/request-safety/smoke-delivery.ps1
```

Assets are under `.local/request-safety/delivery/assets/`: seven self-contained
native archives (Windows x64/ARM64/x86, Linux x64/ARM64, macOS x64/ARM64), a Windows
UPM tarball, a manifest, and `SHA256SUMS`. The verifier checks all archives, native
executables, Unix execute permissions, bundled packages and every UPM member
against current source. The smoke script extracts a fresh copy, starts only that
server on an unused loopback port, connects a managed test plugin over real
SignalR, initializes MCP HTTP and invokes a controlled same-ID test tool twice.
It stops only processes it created; it never contacts Unity or a business endpoint.

## Release entry

The manifest's GitHub tag is `request-safety-9.2.9-2`. It is a candidate until
publication is explicitly authorized. Git installs cannot fetch its server assets
before publication; use the local Windows tarball for review. Keep this work on its
delivery branch until its native release exists, then integrate into `main`.

After approval and with an authenticated GitHub CLI available:

```powershell
# Creates a private draft for review by default; -Publish makes the prerelease public.
pwsh -NoProfile -File commands/request-safety/publish-delivery.ps1
```

This entry validates sealed bytes and clean source, creates the exact paired tag
and uploads existing assets. It does not rebuild assets during publication, touch
NuGet, publish the unrelated CLI, send notifications, or modify business projects.
Never overwrite a published tag/package version. A changed candidate needs a new
profile version and newly sealed manifest before a later release.
