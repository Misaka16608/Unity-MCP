---
name: assets-move
description: Move or rename assets at the given project paths. Refreshes the AssetDatabase at the end. Use 'assets-find' to locate the assets first.
---

# Assets / Move

Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before moving.

## Behavior

Each pair is moved independently via `AssetDatabase.MoveAsset`. Per-pair failures (Unity's `MoveAsset` returns a non-empty error string) are surfaced in `response.Errors`; successful moves accumulate into `response.MovedPaths`.

## How to Call

```bash
unity-mcp-cli run-tool assets-move --input '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-move --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourcePaths` | `any` | Yes | The paths of the assets to move. |
| `destinationPaths` | `any` | Yes | The paths of moved assets. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.MoveAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.MoveAssetsResponse": {
      "type": "object",
      "properties": {
        "MovedPaths": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of destination paths of successfully moved assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of errors encountered during move operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

