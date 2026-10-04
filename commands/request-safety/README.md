# Local request execution safety repair

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

The script downloads official sources at the commits in `sources.json`, verifies
base and patched file hashes, builds the repaired framework, runs the 14 safety,
118 routing/auth regressions and 76 EngineFree tests, checks dependency generation 8,
and optionally builds a framework-dependent local Windows server. `-SkipPublish`
skips that local server build. Nothing is uploaded or deployed.

The current framework is `8.6.0-request-safety.2`, API
`2.0.0-request-safety.2`, native host `9.2.9-request-safety.2`. Three matching
NuGet packages are bundled under the Unity package's `Editor/Delivery~`; normal
consumer restoration needs no public release of the repaired NuGet packages.
`-SeedToolProject` updates only this repository's tool-project dependency drops.

Use `-Delivery` to also build seven self-contained native server archives,
seal their hashes into the Unity package, and create an installable Windows UPM
tarball. See [paired delivery and installation](DELIVERY.md) for candidate artifacts,
actual connection verification, maintenance behavior and the separate release entry.
Native Git downloads use that exact candidate's manifest URL; this release must be
published before its delivery branch is integrated into `main`.

Scratch source, feed, cache, publish and test outputs stay under
`.local/request-safety/`. The Windows build's loopback NuGet proxy
(127.0.0.1:18763) uses Node's verified HTTPS upstream and stops only its own proxy.
Certificates are never disabled. Changed prepared upstream sources are refused
rather than overwritten.

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
  "operationId": "asset-refresh-20261004-001",
  "toolName": "assets-refresh",
  "argumentsJson": "{}"
}
```

Use the exact registered target name and arguments for your project. Query
`tool-operation-status` with `{"operationId":"asset-refresh-20261004-001"}`;
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

States are `queued`, `running`, `succeeded`, `failed`, `canceled`, and `unknown`.
`ExecutionStarted` records entry into target dispatch; `MainThreadStarted`
records entry into Unity work. `running` can include waiting for Unity's main
thread. Started synchronous work may still succeed after cancellation. `unknown`
requires inspecting outputs before intentionally choosing a different ID.
Targets returning legacy `Processing` notifications are recorded as unknown,
not as a fabricated completion. No completion notification is promised.

## Unity checks

Compile the resolver, full Runtime/Editor sources, Player dispatcher and changed
EditMode tests against real
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
