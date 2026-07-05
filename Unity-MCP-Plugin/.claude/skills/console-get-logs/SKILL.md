---
name: console-get-logs
description: Retrieve Unity Editor logs from the MCP plugin's `LogCollector`, optionally filtered by log type or time window. Useful for debugging and monitoring Editor activity.
---

# Console / Get Logs

Retrieves Unity Editor logs. Useful for debugging and monitoring Unity Editor activity.

## How to Call

```bash
unity-mcp-cli run-tool console-get-logs --input '{
  "maxEntries": 0,
  "logTypeFilter": "string_value",
  "includeStackTrace": false,
  "lastMinutes": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool console-get-logs --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `maxEntries` | `integer` | No | Maximum number of log entries to return. Minimum: 1. Default: 100 |
| `logTypeFilter` | `any` | No | Filter by log type. 'null' means All. |
| `includeStackTrace` | `boolean` | No | Include stack traces in the output. Default: false |
| `lastMinutes` | `integer` | No | Return logs from the last N minutes. If 0, returns all available logs. Default: 0 |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry-1"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.LogEntry": {
      "type": "object",
      "properties": {
        "LogType": {
          "type": "string",
          "enum": [
            "Error",
            "Assert",
            "Warning",
            "Log",
            "Exception"
          ]
        },
        "Message": {
          "type": "string"
        },
        "Timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "StackTrace": {
          "type": "string"
        }
      },
      "required": [
        "LogType",
        "Timestamp"
      ]
    },
    "com.IvanMurzak.Unity.MCP.LogEntry-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

