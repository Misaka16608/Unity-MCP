# Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json](../../Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json). Bump with `.\commands\bump-version.ps1 <version>`.

## CI/CD

See `.github/workflows/` for all pipelines. PRs from untrusted contributors require a `ci-ok` label from a maintainer before CI runs.

## Fork dependencies

Paired custom NuGet packages and seven server archives are published via the manual `release-dependencies.yml` workflow to `Misaka16608/Unity-MCP`, before any consuming plugin release. Default: dry run. See [the fork hosting procedure](../dev/github-release-hosting.zh-CN.md). Do not overwrite an existing dependency release.
