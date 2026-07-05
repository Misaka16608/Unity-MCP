---
name: ping
description: Lightweight readiness probe. Returns the input `message` echoed back, or `'pong'` when omitted. Useful for CLI health checks and SignalR connectivity smoke tests.
---

# Ping

Lightweight readiness probe. Returns the input message or 'pong' if omitted.

## Behavior

No I/O, no Unity API calls — pure echo. Ideal for measuring round-trip latency or confirming the MCP transport is alive before invoking a heavier tool.

## How to Call

```bash
unity-mcp-cli run-system-tool ping --input '{
  "message": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-system-tool ping --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `message` | `string` | No | Optional message to echo back. |

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

