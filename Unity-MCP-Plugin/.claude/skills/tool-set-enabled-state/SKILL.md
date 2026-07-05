---
name: tool-set-enabled-state
description: Enable or disable MCP tools by name in batch. Persists the change via `UnityMcpPluginEditor.Instance.Save()` only when at least one tool actually flipped. Returns per-input success flags plus optional operation logs.
---

# Tool / Set Enabled State

Enable or disable MCP tools by name. Allows controlling which tools are available for the AI agent.

## Behavior

Each entry is resolved against the tool manager's exact-name and case-insensitive lookups. Already-correct state short-circuits as success without writing. The plugin's config is saved once at the end iff at least one tool actually changed state.

## How to Call

```bash
unity-mcp-cli run-tool tool-set-enabled-state --input '{
  "tools": "string_value",
  "includeLogs": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool tool-set-enabled-state --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `tools` | `any` | Yes | Array of tools with their desired enabled state. |
| `includeLogs` | `any` | No | Include operation logs in the result. Default: false |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.ToolToggleResult"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.Logs": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.LogEntry"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.LogEntry": {
      "type": "object",
      "properties": {
        "Depth": {
          "type": "integer"
        },
        "Message": {
          "type": "string"
        },
        "Type": {
          "type": "string",
          "enum": [
            "Trace",
            "Debug",
            "Info",
            "Success",
            "Warning",
            "Error",
            "Critical"
          ]
        }
      },
      "required": [
        "Depth",
        "Type"
      ]
    },
    "System.Collections.Generic.Dictionary(System.String,System.Boolean)": {
      "type": "object",
      "additionalProperties": {
        "type": "boolean"
      }
    },
    "AIGD.ToolToggleResult": {
      "type": "object",
      "properties": {
        "Logs": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.Logs",
          "description": "Optional operation logs. Only included when 'includeLogs' is true."
        },
        "Success": {
          "$ref": "#/$defs/System.Collections.Generic.Dictionary(System.String,System.Boolean)",
          "description": "Result of each tool operation. Key: original input name as provided by the caller (case preserved as-is). Value: true if the enable/disable operation completed successfully, false if the name was unknown, ambiguous, or empty."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

