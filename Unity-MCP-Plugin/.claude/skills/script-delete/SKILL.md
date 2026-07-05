---
name: script-delete
description: Delete one or more `.cs` script files from disk, refresh the AssetDatabase, and wait for Unity compilation to settle before delivering the final result via the request's `requestId`. Pair with 'script-read' to inspect files before deletion.
---

# Script / Delete

Delete the script file(s). Does AssetDatabase.Refresh() and waits for Unity compilation to complete before reporting results. Use 'script-read' tool to read existing script files first.

## Behavior

Validates the array (non-empty, every entry ends with `.cs`, every entry exists). Deletes each file plus its sibling `.meta` (when present). Calls `AssetDatabase.Refresh` and schedules a post-compilation notification — the final response is delivered after Unity finishes the recompile triggered by the delete.

## How to Call

```bash
unity-mcp-cli run-tool script-delete --input '{
  "files": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool script-delete --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `files` | `any` | Yes | File paths to the files. Sample: "Assets/Scripts/MyScript.cs". |

## Output

This tool does not return structured output.

