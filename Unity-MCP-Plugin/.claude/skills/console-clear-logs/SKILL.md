---
name: console-clear-logs
description: Clear the MCP log cache (used by 'console-get-logs') and the Unity Editor Console window. Useful for isolating logs to a specific action by clearing the slate first.
---

# Console / Clear Logs

Clears the MCP log cache (used by console-get-logs) and the Unity Editor Console window. Useful for isolating errors related to a specific action by clearing logs before performing the action.

## Behavior

Calls `Debug.ClearDeveloperConsole()` to wipe the Editor Console, then clears the MCP-side `LogCollector` cache so subsequent 'console-get-logs' calls only see new entries.

## How to Call

```bash
unity-mcp-cli run-tool console-clear-logs --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool console-clear-logs --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

## Output

This tool does not return structured output.

