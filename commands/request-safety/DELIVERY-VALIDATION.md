# Paired delivery validation — 2026-10-04

Baseline: `1301da0`, branch `codex/paired-plugin-delivery`. This extends the
original [execution safety validation](VALIDATION.md); it does not replace the
business project's UPM dependency or restart any existing editor/server.

| Check | Result | Evidence / limit |
| --- | --- | --- |
| Repaired request transport | 14 passed | `build-paired-delivery.log`, production HTTP/SignalR with controlled managed work |
| Existing routing/auth regressions | 118 passed | Same build log, unchanged routing/selection/auth contracts |
| EngineFree | 76 passed | `..._2026-10-04_15_33_50.trx`; 61 previous cases + 15 delivery cases including real tool-manager maintenance and inherited durable work admission |
| Generation gate | 15 pins, generation 8 | `check_nuget_gate.py`, all checked-in project/asmdef defines agree |
| Unity source compile | DependencyResolver, full Runtime/Editor sources, changed NUnit sources and Player dispatcher | Real Unity 2022.3.62f3c1/package references; no engine substitutes. Existing unused-field warnings in test helpers. Compilation does not prove editor startup/runtime. |
| Native build | All seven release RIDs built | Self-contained Windows x64/ARM64/x86, Linux x64/ARM64, macOS x64/ARM64; only Windows x64 executed on this machine |
| Sealed artifact integrity | Passed | Three package hashes, seven archive/executable hashes, Unix execute permissions and every UPM member matched current source |
| Packaged Windows smoke | Passed | Extracted archive; own PID 19708, loopback port 12911, `smoke-37a8187bd1ca45f69e27b630eac8690e`; real native host + managed plugin SignalR registration + MCP HTTP initialize; same request ID invoked twice, exactly one test write |

Local evidence lives under `.local/request-safety/`: build/source-compile logs,
`results/*.trx`, `delivery/assets/`, and isolated smoke logs. The successful smoke
used API `2.0.0-request-safety.2`; its process was stopped after verification.
Its primitive test write changed only an in-memory test counter, never Unity or
business assets.

Two earlier smoke attempts diagnosed harness issues: the client initially ignored
the boolean `Connect` result and appended a trailing slash to a host that the
framework concatenates directly with `/hub/mcp-server`. The final harness trims
that slash, asserts actual connection success, separates build from execution,
bounds execution and preserves diagnostics. After a reported Windows `Smoke.exe`
error dialog, the harness also gained a top-level managed exception handler and
direct DLL execution rather than a dotnet-run/apphost child. An invalid endpoint
was tested: logged failure, exit 1, no unhandled CLR exception. The corrected
success path was rerun and no Smoke/Windows Error Reporting process remained.
No business endpoint was involved.

Maintenance coverage includes blocked fresh admission, retained duplicate-result
reuse, active-work counting, idempotent/nested leases, status-style allowed calls,
and real manager acceptance of an already admitted durable child. Artifact cases
cover corrupt cached bytes, corrupt immutable source, escaping paths, duplicate
identity, differing server versions and unpinned/credential-bearing URLs.

The existing isolated Unity EditMode run remains blocked by vendor Sentinel
startup protection before an editor log was created. No real Unity runtime or
actual domain-reload/server-upgrade pass is claimed. Business-project deployment,
public native release publication and macOS/Linux execution remain separate from
these completed implementation/build checks.
