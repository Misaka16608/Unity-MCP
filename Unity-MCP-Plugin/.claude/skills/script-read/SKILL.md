---
name: script-read
description: Read a `.cs` script file and return its content as a string. Supports a 1-based `lineFrom`/`lineTo` slice for partial reads. Pair with 'script-update-or-create' to write back.
---

# Script / Read

Reads the content of a script file and returns it as a string. Use 'script-update-or-create' tool to update or create script files.

## Behavior

Reads the file with `File.ReadAllLines` and slices `[lineFrom..lineTo]` (inclusive). The slice indices are clamped — passing out-of-range `lineFrom`/`lineTo` is forgiving (read returns at-most the whole file).

## How to Call

```bash
unity-mcp-cli run-tool script-read --input '{
  "filePath": "string_value",
  "lineFrom": 0,
  "lineTo": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool script-read --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `lineFrom` | `integer` | No | The line number to start reading from (1-based). |
| `lineTo` | `integer` | No | The line number to stop reading at (1-based, -1 for all lines). |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

