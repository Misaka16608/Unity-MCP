---
name: profiler-list-modules
description: List all known profiler module names with their local 'enabled' bookkeeping flag.
---

# Profiler / List Modules

Returns `Tool_Profiler.AvailableModules` projected into a `ProfilerModulesData` list, with each entry's `Enabled` field reflecting the wrapper's local bookkeeping.

## Behavior

Uses only built-in Unity APIs and in-process state. No external Unity package is required. Pair with `profiler-enable-module` to flip the bookkeeping flag.

## How to Call

```bash
unity-mcp-cli run-tool profiler-list-modules --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool profiler-list-modules --input-file args.json`.

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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModulesData",
      "description": "Container for 'profiler-list-modules' output."
    }
  },
  "$defs": {
    "System.Collections.Generic.List(com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModuleInfo)": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModuleInfo",
        "description": "Profiler module entry returned by 'profiler-list-modules'."
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModuleInfo": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string",
          "description": "Module name (e.g. 'CPU', 'Memory')."
        },
        "Enabled": {
          "type": "boolean",
          "description": "Whether the wrapper considers this module enabled. Local bookkeeping only."
        }
      },
      "required": [
        "Enabled"
      ],
      "description": "Profiler module entry returned by 'profiler-list-modules'."
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModulesData": {
      "type": "object",
      "properties": {
        "Modules": {
          "$ref": "#/$defs/System.Collections.Generic.List(com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ProfilerModuleInfo)",
          "description": "All known profiler modules and their wrapper-side enabled flag."
        }
      },
      "description": "Container for 'profiler-list-modules' output."
    }
  },
  "required": [
    "result"
  ]
}
```

