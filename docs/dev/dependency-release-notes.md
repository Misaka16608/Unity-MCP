Paired request-safety dependencies for the Misaka16608/Unity-MCP fork.

- McpPlugin / Common: **8.9.0-request-safety.4** (upstream 8.9.0).
- GameDev MCP Server: **9.2.12-request-safety.4** (upstream 9.2.12).
- API contract: **2.0.0-request-safety.3**. Use the paired client and server together.
- Two NuGet packages, seven self-contained server archives, source pins and SHA256SUMS are attached.
- Request cancellation and deduplication are retained, including the upstream ToolDispatcher path.

These are unsigned fork builds. Windows/macOS signing and notarization from the upstream distribution are not included. Each server archive includes upstream licenses. The fork's source manifest records exact upstream commits and patch hashes.

Install the Unity package from this fork. The Unity dependency resolver and CLI use this pinned release automatically. Existing projects with `UNITY_MCP_SERVER_PATH` configured must remove that override to resume release-managed server downloads. The release contains executable downloads; no Docker image or NuGet.org package is published.
