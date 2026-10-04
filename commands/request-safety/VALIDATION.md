# Validation record — 2026-10-04

Baseline: local `main` at `9c5b2b32`; repair branch
`codex/request-execution-safety`. No push, release, deployment, business project
change or existing editor/server restart was performed.

Pinned upstream bases (full source/content checks are in `sources.json`):

| Dependency | Base commit | Local repair version |
| --- | --- | --- |
| MCP-Plugin-dotnet | `94ccb955bf95adc3499a3925491946e14367d551` | `8.6.0-request-safety.1` |
| GameDev-MCP-Server | `9bf8483789fd3d6c32d228da392126f16081022e` | `9.2.9-request-safety.1` |

## Passed

| Check | Evidence | Boundary |
| --- | --- | --- |
| RequestExecution safety suite | 14/14, `.local/request-safety/results/ChaoQian_DESKTOP-TUM1N0M_2026-10-04_14_18_56.trx` | Includes real loopback Kestrel MCP HTTP, production SignalR client/plugin, cancellation and timed-out redelivery. The blocked side-effect boundary is managed test code, not an actual Unity build. |
| Existing server regressions | 118/118, `..._14_19_46.trx` | Token routing, direct-tool/auth/pinned routing, project pins, notifications, sticky selection, per-session context and actionable errors. |
| EngineFree | 61/61, `..._14_19_10.trx` | 44 existing checks plus 17 safety cases using shipped `QueuedExecution`, durable store and patched registry source/types. No stand-in product types. |
| NuGet generation gate | 15 pins, `UNITY_MCP_DEPS_7`, propagation consistent | Tool package, EngineFree pin parity, asmdefs and all checked-in Unity test project defines. |
| Source/package/server build | Common/plugin netstandard2.1, plugin/server net9.0, local win-x64 host artifact | Paired package/DLL artifacts built from the tracked patches; no external package publication. |
| Unity source compilation | Editor and Player 0 errors; `.local/request-safety/results/unity-source-compile.log` | Real Unity 2022.3.62f3c1, actual tool dependency DLLs and compiled project assemblies. Editor includes the two new NUnit/UnityTest sources. Two MSB3277 reference-profile warnings per mode (System.Net.Http/Compression); this does not establish runtime compatibility. |

Safety assertions cover concurrent same-ID single execution and completed-result
reuse, payload conflicts, identity/session/project isolation, cancel-before-arrival
tombstones, capacity refusal without eviction, pre-dispatch reconnect retry,
delivered timeout/disconnect refusal to retry, queued cancellation and deadline
expiry with zero writes after recovery, callback reentry, and started synchronous
work finishing without a false rollback promise. Durable tests cover immediate
handles, concurrent admissions, saved completion, reload-to-unknown, rejection of
old-domain completion, identity/project isolation, exclusive ownership, immutable
admission, interrupted temporary writes, missing admission and invalid latest state.

The existing HTTP selection regression initially exposed a stale ambient
selection in the scope builder. Both remote runners now reload sticky selection
from the routing store, and all 118 checks passed after that correction.

## Engine execution not verified

An isolated project was created at
`.local/request-safety/unity-isolated-ec1dd345b421477d9409bc8487f6e8c3`.
Only the newly launched Unity process (PID 25212) was stopped at the bounded
timeout. Its window was `Sentinel LDK Protection System`, CPU remained at 0.0625
seconds, and neither `Editor.log` nor an imported `Library` appeared. The existing
Editor PID 34848 remained present. The protection condition did not change, so
the isolated launch was not repeatedly retried.

The two EditMode tests are checked in and compile against actual test-runner
assemblies, but did not execute. There is no verified Unity build/import/package
operation, domain reload in a live engine, or weatherpet/orchestrator acceptance
claim. The durable recovery tests model process/domain replacement through real
on-disk store instances. Ordinary request deduplication intentionally ends at
server/plugin restart; use durable operations for a stable cross-restart ID.

The local artifacts and prerelease pins must be used as a matched pair. Public
NuGet, default release downloads and the existing release/chain workflows have
not been published or migrated to this repair.
