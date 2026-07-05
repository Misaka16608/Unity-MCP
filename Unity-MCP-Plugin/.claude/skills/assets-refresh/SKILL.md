---
name: assets-refresh
description: Refresh the Unity AssetDatabase. Use after files were added or updated outside of the Unity API, or to force script recompilation when a '.cs' file changed. Returns a processing/success response and waits for compilation when triggered.
---

# Assets / Refresh

Refreshes the AssetDatabase. Use it if any file was added or updated in the project outside of Unity API. Use it if need to force scripts recompilation when '.cs' file changed.

## Behavior

Runs `AssetDatabase.Refresh(options)`. If `EditorApplication.isCompiling` is true after the refresh, schedules a post-compilation notification and returns a `Processing` response. If compilation already failed (`EditorUtility.scriptCompilationFailed`), returns `Success` with the compilation error details. Otherwise returns a plain `Success`.

## How to Call

```bash
unity-mcp-cli run-tool assets-refresh --input '{
  "options": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-refresh --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `options` | `any` | No | Asset import options. |

## Output

This tool does not return structured output.

