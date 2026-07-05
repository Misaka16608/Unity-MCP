---
name: assets-delete
description: Delete the assets at the given project paths. Refreshes the AssetDatabase at the end. Use 'assets-find' to locate the assets first.
---

# Assets / Delete

Delete the assets at paths from the project. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before deleting.

## Behavior

Routes through `AssetDatabase.DeleteAssets`, which deletes the batch atomically. Paths Unity reports as failed are surfaced in `response.Errors`; successfully deleted paths are surfaced in `response.DeletedPaths`. The tool is destructive (removes files from disk).

## How to Call

```bash
unity-mcp-cli run-tool assets-delete --input '{
  "paths": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-delete --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `paths` | `any` | Yes | The paths of the assets |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.DeleteAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.DeleteAssetsResponse": {
      "type": "object",
      "properties": {
        "DeletedPaths": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of paths of deleted assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of errors encountered during delete operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

