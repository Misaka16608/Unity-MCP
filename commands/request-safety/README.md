# Request execution safety dependencies

This repair prevents a delivered MCP write from being retransmitted when a Unity
callback is delayed. It also provides durable IDs for work that outlives an MCP
request. Sources, packages, server artifacts and test projects remain inside this
tool repository. No business project or running editor is needed to build it.

## Reproduce

Prerequisites: Node with `fetch`, PowerShell 7, Git, Python, .NET 8 and .NET 9 SDKs.
From the repository root:

```powershell
pwsh -NoProfile -File commands/request-safety/build.ps1 -SeedToolProject
```

The script downloads official source archives at the commits in `sources.json`,
checks original file hashes, applies the tracked patches, checks patched hashes,
builds the paired packages, runs 15 safety tests, 123 existing server regression
tests and 67 EngineFree tests, checks the dependency generation gate, and builds
the local Windows server. `-SkipPublish` skips generating the server executable;
`publish` here is a local .NET build command, with no upload or deployment.

All scratch outputs are under `.local/request-safety/`:

- `deps/`: prepared upstream sources. Existing changed source is rejected instead
  of overwritten. Preserve it before preparing a different patch revision.
- `feed/`: `8.9.0-request-safety.4` Common/plugin packages.
- `nuget/<source-manifest-digest>/`: isolated restore cache for the exact patch.
- `publish/gamedev-mcp-server.exe`: host `9.2.12-request-safety.4`, framework dependent.
- `results/`: TRX results and optional Unity source compilation log.

`-SeedToolProject` updates only this repository's `Unity-MCP-Plugin` DLL drops,
installed manifest and NuGet cache. It never connects to an editor. These paired
DLLs are tracked because the tool project already tracks its dependency drops.
The patched API is `2.0.0-request-safety.3`; pair it with this fork's server.

## Distribution

The two custom packages and seven self-contained server archives share the public
`Misaka16608/Unity-MCP` release `v9.2.12-request-safety.4`, with a `SHA256SUMS`
manifest and pinned source provenance. Ordinary NuGet dependencies remain on NuGet.org.
The editor and CLI download the paired server from that release. For local development,
`UNITY_MCP_SERVER_PATH` still selects a locally built server.

Build all release assets with `build.ps1 -ReleaseAssets -SkipPublish`, then use the
manual `release-dependencies.yml` workflow (dry run by default). Published assets
must not be overwritten. [Full publication and migration procedure](../../docs/dev/github-release-hosting.zh-CN.md).
For an existing project, update its UPM package/dependencies first, then run
`use-hosted-release.py --project <path>` to plan removal of the old override;
`--apply` saves an exact `.env` backup before changing it.

Clean dotnet consumers can run `release_assets.py fetch-packages` and add
`.local/request-safety/feed` as `RestoreAdditionalProjectSources`. A chain feed must
retain this patch's execution APIs and the matching API handshake.

On this Windows sandbox, the build uses a bounded-purpose loopback NuGet proxy
(`127.0.0.1:18763`) whose upstream GETs use Node's verified HTTPS. The HTTP exception
in `NuGet.Config` applies only to that local hop. Certificates are never disabled;
package bytes are forwarded unchanged. The script stops the proxy it owns.

## Execution contract

Retries are permitted while no destination is available and nothing has been
sent. Once `InvokeAsync` starts, timeout, disconnect, cancellation or exception
returns **outcome unknown**. It never tries another engine or resends the write.
Cancellation is sent to the actual captured destination, and a transmitted
deadline guards a queued Unity callback if the cancellation message is lost.

Server admission uses a server epoch, hashed authenticated account (legacy token
or anonymous fallback), MCP session, project pin and actual sticky instance
selection. Incoming scope fields are ignored. Standard and system tools have
separate scopes. The original JSON-RPC ID, including number/string distinction,
survives the router; wire IDs are opaque hashes. Equal IDs within one scope share
in-flight and completed responses. A different payload is rejected. Each
in-memory ledger retains its admissions up to 10,000 entries and then refuses
new work; it never evicts a remembered write to admit it again. This ordinary
request ledger lasts for the current server/plugin lifetime, not across restarts.

Cancellation travels from MCP/HTTP through the server and plugin execution
context into `QueuedExecution`. The queue atomically claims a callback once,
checks expiry before entering the body, and removes an Editor update callback
before running it. Canceling a queued callback prevents its body even if Unity
updates later. Cancellation after entry cannot undo synchronous Unity work.

## Durable long operations

Choose and persist an operation ID before calling `tool-operation-start`. The
accepting call returns a queryable handle without waiting for the target tool:

```json
{
  "operationId": "script-20261019-001",
  "toolName": "script-execute",
  "argumentsJson": "{\"csharpCode\":\"await Task.Delay(1000, cancellationToken); 42\"}"
}
```

Use the exact registered target name and arguments for your project. Query
`tool-operation-status` with `{"operationId":"script-20261019-001"}`;
use a **new MCP request ID for each poll**. Cancel through `tool-operation-cancel`.
Reuse the stable operation ID and unchanged arguments after an accepting-call
timeout or reconnect. If even the handle response was lost, query that ID first.

The project-local `Library/mcp-operations` journal scopes IDs to caller identity.
It flushes an immutable admission before scheduling work, then appends flushed
immutable revisions through same-directory renames supported by Unity's BCL.
A single-owner file lease prevents competing editor/process admission. A reload
preserves terminal results and changes interrupted queued/running work to
`unknown`, without automatically resuming it. Missing admission or invalid latest
state fails closed. Partial `.tmp` writes are ignored. Keep this directory while
any outcome is unresolved; deleting it discards deduplication history. No
automatic journal cleanup is performed.

States are `queued`, `running`, `awaiting-completion`, `succeeded`, `failed`, `canceled`, and `unknown`.
`ExecutionStarted` records entry into target dispatch; `MainThreadStarted`
records entry into Unity work. `running` can include waiting for Unity's main
thread. Started synchronous work may still succeed after cancellation. `unknown`
requires inspecting outputs before intentionally choosing a different ID.
Targets returning `Processing` notifications enter `awaiting-completion`; the
later `NotifyToolRequestCompleted` result is committed by the durable request ID,
including after a domain reload. An authoritative late notification can also
resolve `unknown` if reload interrupted dispatch before `Processing` was saved.
Duplicate notifications cannot overwrite a final success, failure or cancellation.
Notifications are consumed locally before hub delivery, so persistence does not
depend on an MCP client connection.

Cancellation sets `CancellationRequested`; `awaiting-completion` remains pending
until the tool acknowledges stopping or reports its actual result. Package tools
check cancellation before `Client.Add` / `Client.Remove`; Unity does not provide
an abort API for these native requests, so once started they are observed to
completion even after cancellation. Detached package tools retain the operation's
token after returning `Processing`.

`script-execute` snippets receive a reserved `cancellationToken` variable. Pass
it to async waits and check it before side effects. Full-source entry methods can
declare a `System.Threading.CancellationToken` parameter and list that parameter
in `parameters`; its value is injected. Durable scripts await the real task with
no result-wait timeout; code that ignores cancellation can keep running. Stopping
the editor/domain still leaves interrupted execution `unknown`, without replay.

The journal persists across editor restarts, but a tool's later notification is
still required for a pending `Processing` result. Upstream producers using only
Unity `SessionState` cannot recover that notification after a full editor exit.
Such operations can remain unresolved and must be inspected; this ledger never
fabricates a completion or reruns side effects.

## Unity checks

### Repair an existing linked project

Projects referencing this checkout through UPM `file:` links keep their own
`Assets/Plugins/NuGet` DLLs. Updating this repository does not update those DLLs.
An older Common assembly produces `CS0234` for `Common.Execution` and `CS0246`
for `ToolExecutionContext`. To synchronize the three paired managed dependencies
with this checkout, inspect the plan first:

```powershell
python commands/request-safety/repair-project-dependencies.py --project 'F:/path/to/project'
# Add --apply to install; repeat --project for multiple explicitly selected projects.
```

The command backs up every changed file and records before/after hashes under
`.local/request-safety/project-repair/`. It preserves existing `.meta` importer
settings and unrelated dependencies. It updates Common, McpPlugin, ReflectorNet
and their installed-manifest entries only. Let Unity refresh/recompile afterward.
This repairs the existing DLL installation; it does not provision a NuGet cache
for future clean restores or install/start the matching native server. The API
pairing requirements above still apply.

### Install the matching server in an existing project

An API mismatch (`Plugin: 2.0.0-request-safety.3, Server: 2.0.0`) means
the DLL repair succeeded but the editor still connects to the released server.
Build the matching server with `build.ps1` (without `-SeedToolProject`), then:

```powershell
python commands/request-safety/configure-project-server.py --project 'F:/path/to/project'
# Review the plan, then add --apply. Repeat --project for selected projects.
```

This installs the verified local publish tree under the project's
`Library/mcp-server-paired/9.2.11-request-safety.3`, seeds the two matching
packages into `Library/NuGetCache`, and appends `UNITY_MCP_SERVER_PATH` to
the project-root `.env`. Existing `.env` files are backed up under
`.local/request-safety/server-repair/`. Ports, authentication and other settings
are preserved. The server requires the .NET 9 runtime.

The command does not manage running processes. Stop the project's old MCP server
and start the paired executable with the same port, transport and authentication
arguments. In Unity, refresh resources to import the repaired DLLs. If the editor
already rejected a version handshake, disconnect and reconnect explicitly (or
reload scripts with KeepConnected enabled): that rejection disables automatic
reconnection. Verify `/api/tools` and a real `POST /api/system-tools/ping` response
for each project; a listening port alone does not establish a successful pairing.

Unity 2021 projects can also reject the entire MCP assembly graph when an older
third-party `System.Runtime.CompilerServices.Unsafe` shadows the required 6.0
assembly. Check the actual assembly-load errors separately from API pairing.
An Editor-only compatibility installation can preserve the original Player DLL
selection; validate any affected third-party library with a real Editor smoke
check. Do not assume a successful source compilation establishes assembly loading
or change global assembly-version validation to hide the conflict.

Compile the changed Editor/Player source and new EditMode tests against real
installed Unity and dependency DLLs, without launching an editor:

```powershell
pwsh -NoProfile -File commands/request-safety/compile-unity-sources.ps1
```

This requires the tool project's existing generated Editor.Tests project and
compiled `Library/ScriptAssemblies`. It reads them, never generates substitutes,
and writes its temporary SDK projects inside `.local`. It is a compilation
check, not engine execution.

To run the two real Editor update tests in a **new isolated project only**:

```powershell
pwsh -NoProfile -File commands/request-safety/unity-tests.ps1 -UnityExe 'C:\path\to\Editor\Unity.exe'
```

The script disables MCP auto-connect/server startup, copies only this tool's
package/dependencies, and stops only its own process on failure or the four-minute
limit. Current machine evidence: Unity 2022.3.62f3c1 stopped at `Sentinel LDK
Protection System` before creating an Editor log or importing the isolated
project. **No Unity runtime/EditMode pass is claimed.** See `VALIDATION.md` for
the actual checks and their limits.
