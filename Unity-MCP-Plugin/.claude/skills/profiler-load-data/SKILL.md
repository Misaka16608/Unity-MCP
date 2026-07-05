---
name: profiler-load-data
description: Read back a previously-saved JSON snapshot from `profiler-save-data` and return its raw text.
---

# Profiler / Load Data

Reads `filePath` as UTF-8 text and returns the file body unchanged. Caller is responsible for parsing.

## Errors

- Returns `[Error]` when `filePath` is empty, the file does not exist, exceeds the 10 MB size cap, or the read fails.

## Behavior

Uses `System.IO.File.ReadAllText` (BCL) — no external Unity package is required. Files larger than 10 MB are rejected up-front to avoid OOM on a stray call.

## How to Call

```bash
unity-mcp-cli run-tool profiler-load-data --input '{
  "filePath": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-load-data --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | Path to a profiler snapshot file previously written by 'profiler-save-data'. |

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

