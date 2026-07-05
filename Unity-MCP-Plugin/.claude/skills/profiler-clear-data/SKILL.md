---
name: profiler-clear-data
description: Discard all frames currently held by the Editor Profiler (UnityEditorInternal.ProfilerDriver.ClearAllFrames). Cannot be undone.
---

# Profiler / Clear Data

Invokes `UnityEditorInternal.ProfilerDriver.ClearAllFrames()` on the main thread. `UnityEditorInternal` is a built-in editor namespace — no external Unity package is required.

## Behavior

After this call, the Profiler window's frame history is empty; subsequent recording starts from frame 0. Returns `true` on success.

## How to Call

```bash
unity-mcp-cli run-tool profiler-clear-data --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-clear-data --input-file args.json`.

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

