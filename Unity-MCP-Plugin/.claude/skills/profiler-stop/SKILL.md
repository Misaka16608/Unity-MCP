---
name: profiler-stop
description: Disable Unity's runtime profiler. Idempotent — calling when already disabled returns the current disabled state.
---

# Profiler / Stop

Sets `UnityEngine.Profiling.Profiler.enabled = false`. Returns the post-call value of `Profiler.enabled` (expected `false`).

## Behavior

Uses only built-in Unity APIs (`UnityEngine.Profiling`). No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-stop --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-stop --input-file args.json`.

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

