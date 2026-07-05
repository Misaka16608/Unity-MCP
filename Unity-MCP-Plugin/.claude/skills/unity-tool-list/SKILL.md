---
name: unity-tool-list
description: List all Unity-MCP tools registered in the connected Unity Editor instance. Optional regex filter matches against tool name, description, and argument names/descriptions. Use the `includeDescription` / `includeInputs` toggles to control the response size.
---

# Tool / List

List all Unity-MCP tools registered in the connected Unity Editor instance. Optionally filter by regex across tool names, descriptions, and arguments.

## Behavior

Iterates `UnityMcpPluginEditor.Instance.Tools.GetAllTools()`, evaluates the filter (if any), and projects each surviving tool into a `ToolInfoData` honoring the verbosity toggles.

## How to Call

```bash
unity-mcp-cli run-tool unity-tool-list --input '{
  "regexSearch": "string_value",
  "includeDescription": "string_value",
  "includeInputs": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool unity-tool-list --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `regexSearch` | `string` | No | Regex pattern to filter tools. Matches against tool name, description, and argument names and descriptions. |
| `includeDescription` | `any` | No | Include tool descriptions in the result. Default: false |
| `includeInputs` | `any` | No | Include input arguments in the result. Default: None |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.ToolInfoData-1"
    }
  },
  "$defs": {
    "AIGD.ToolInfoData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Tool name."
        },
        "description": {
          "type": "string",
          "description": "Tool description."
        },
        "inputs": {
          "$ref": "#/$defs/AIGD.ToolInputData-1",
          "description": "Tool input arguments."
        }
      },
      "description": "MCP tool information."
    },
    "AIGD.ToolInputData-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ToolInputData",
        "description": "MCP tool input argument."
      }
    },
    "AIGD.ToolInputData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Argument name."
        },
        "description": {
          "type": "string",
          "description": "Argument description."
        }
      },
      "description": "MCP tool input argument."
    },
    "AIGD.ToolInfoData-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ToolInfoData",
        "description": "MCP tool information."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

