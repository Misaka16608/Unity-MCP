---
name: profiler-save-data
description: Save a snapshot of profiler-derived stats (status + memory + rendering + script + frame capture) to a JSON file. Built-in Unity APIs only.
---

# Profiler / Save Data

Composes the outputs of `profiler-get-status`, `profiler-get-memory-stats`, `profiler-get-rendering-stats`, `profiler-get-script-stats` and `profiler-capture-frame` into a single JSON document and writes it to `filePath`. Creates any missing parent directories.

## Errors

- Returns `[Error]` when `filePath` is empty or the write fails (message includes the underlying exception text).

## Behavior

Uses `System.Text.Json` (BCL) for serialization and `System.IO.File.WriteAllText` for the write. No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-save-data --input '{
  "filePath": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-save-data --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | Absolute or workspace-relative output file path. |

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

