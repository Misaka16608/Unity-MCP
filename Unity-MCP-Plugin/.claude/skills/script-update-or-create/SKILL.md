---
name: script-update-or-create
description: Write a `.cs` script file (create or overwrite) with the provided C# code. Validates syntax via Roslyn before write — invalid code is rejected with error details and the file is left untouched. Refreshes the AssetDatabase and delivers the final result via `requestId` after Unity finishes the triggered compilation. Use 'script-read' to inspect existing content first.
---

# Script / Update or Create

Updates or creates script file with the provided C# code. Does AssetDatabase.Refresh() at the end. Provides compilation error details if the code has syntax errors. Use 'script-read' tool to read existing script files first.

## Behavior

Creates any missing parent directories, writes the file, then calls `AssetDatabase.Refresh` and schedules a post-compilation notification so the final response is delivered after Unity finishes the recompile.

## How to Call

```bash
unity-mcp-cli run-tool script-update-or-create --input '{
  "filePath": "string_value",
  "content": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool script-update-or-create --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `content` | `string` | Yes | C# code - content of the file. |

## Output

This tool does not return structured output.

