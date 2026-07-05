---
name: profiler-start
description: "Enable Unity's runtime profiler and open the Profiler window. Idempotent: calling when already enabled returns the current enabled state without error."
---

# Profiler / Start

Enables `UnityEngine.Profiling.Profiler.enabled = true` and opens `Window > Analysis > Profiler` via `EditorApplication.ExecuteMenuItem`. Returns `true` once the profiler is enabled.

## Behavior

Uses only built-in Unity APIs (`UnityEngine.Profiling`, `UnityEditor.EditorApplication`). No external Unity package is required.

Snapshot-based: this tool does not stream historical frame data — use Unity's Profiler window directly for that.

## How to Call

```bash
unity-mcp-cli run-tool profiler-start --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-start --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "boolean"
    }
  },
  "required": [
    "result"
  ]
}
```

