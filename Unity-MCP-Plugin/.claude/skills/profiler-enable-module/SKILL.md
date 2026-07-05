---
name: profiler-enable-module
description: Toggle the wrapper's local 'enabled' flag for a named profiler module. Bookkeeping only — Unity's runtime API does not expose direct module control; for real module visibility use the Profiler window.
---

# Profiler / Enable Module

Adds or removes the given module name from the wrapper's `EnabledModules` set. This is local bookkeeping consumed by `profiler-get-status` and `profiler-list-modules`; Unity's runtime API does not allow programmatic toggling of Profiler-window modules from a built-in namespace, so this tool intentionally does not pretend to.

## Errors

- Returns an `[Error]` string when `moduleName` is empty or unknown.

## How to Call

```bash
unity-mcp-cli run-tool profiler-enable-module --input '{
  "moduleName": "string_value",
  "enabled": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-enable-module --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `moduleName` | `string` | Yes | Profiler module name (e.g. 'CPU', 'GPU', 'Memory'). |
| `enabled` | `boolean` | No | True to mark the module enabled in local bookkeeping; false to mark disabled. |

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

