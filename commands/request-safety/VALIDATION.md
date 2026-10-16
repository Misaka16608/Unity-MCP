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

## Existing-project repair — 2026-10-16

After local `main` was fast-forwarded to `1301da0bc7ee61561fd985ee4eef66f8c5038295`,
four explicitly selected linked projects were repaired. The paired source build
was repeated without `-SeedToolProject`: 14 safety, 118 server regression and 61
EngineFree tests passed (193 total, zero failures). Evidence is in the three
`admin_DESKTOP-R99PM8Q_2026-10-16_*.trx` files under
`.local/request-safety/results/` and `.local/request-safety/server-build.log`.
The 15-pin dependency generation gate also passed.

`repair-project-dependencies.py` synchronized the three managed dependency drops
and their manifest entries; `configure-project-server.py` installed the matching
local server, seeded the two prerelease restore packages and set the project-root
`.env` launch override. Both commands subsequently reported no changes needed.
Only the four identified old MCP server processes were replaced, retaining their
ports, transport and auth arguments. Existing Unity processes were kept open.

After importing the DLLs and reloading scripts, these real HTTP checks passed:

| Project | Port | Registered tools | System ping |
| --- | --- | --- | --- |
| `F:/JSLM005` | 21000 | 80 | `pong` |
| `D:/YYBS_YB/yybs_yuanban_0.1zhe` | 21500 | 80 | `pong` |
| `F:/YYBS/Android/yybs_lz/int_UseMCP` | 25631 | 80 | `pong` |
| `F:/YYBS/Android/yybs_lz/patch_UseMCP` | 25630 | 80 | `pong` |

The final responses are recorded in
`.local/request-safety/project-pairing-verification.json`. These establish live
Editor loading, version pairing and tool responsiveness; they do not establish a
Player build, gameplay regression pass or execution of the two EditMode tests.

Unity 2021 in `JSLM005` additionally rejected MCP dependencies because its legacy
Protobuf dependency selected Unsafe `4.0.4.1` instead of required `6.0.0.0`.
Editor-only copies were installed under `Assets/Plugins/McpEditorSupport`:
Unsafe from this checkout, plus a byte-identical copy of the project's existing
Google.Protobuf DLL. Only that Editor copy of Protobuf has reference-version
validation disabled. The original Protobuf/Unsafe importers now exclude Editor;
their Player platform selections, validation settings and DLL bytes were
preserved. Global assembly-version validation was not disabled.

A real `script-execute` check in the live Editor serialized and parsed a 4 KB
Protobuf string, asserted equality and confirmed Unsafe `6.0.0.0` and Protobuf
loaded from the Editor support folder. The temporary reconnect source and its
`.meta` were removed before the final successful HTTP checks.

Local rollback evidence:

- Managed DLLs/manifests:
  `.local/request-safety/project-repair/16e77bc5120d4e90abc7b91a28aabf60/`.
- Original project `.env` files:
  `.local/request-safety/server-repair/0aba7442128f45f887e3a48e117331c0/`.
- Original JSLM005 importer metadata and added-file records:
  `.local/request-safety/editor-assembly-repair/da70b37780844fa188aa7524f52d1e7d/`
  and `03de5c6ce74d446b806c7d95734ddb3c/`.

The matching server and prerelease packages remain local artifacts; this repair
did not publish a release or alter the default public server download pin.
