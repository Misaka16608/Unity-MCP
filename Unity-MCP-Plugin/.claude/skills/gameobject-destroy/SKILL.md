---
name: gameobject-destroy
description: Destroy a GameObject (and all nested children) in the currently opened Prefab or active Scene. Returns the destroyed GameObject's name, path, and instance ID for confirmation. Use 'gameobject-find' to locate the target first.
---

# GameObject / Destroy

Destroy GameObject and all nested GameObjects recursively in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first.

## Behavior

Validates the `gameObjectRef`, resolves it on the main thread, then calls `Object.DestroyImmediate` (the immediate variant is required for Editor-mode operations). Returns a `DestroyGameObjectResult` containing `DestroyedName`, `DestroyedPath`, and `DestroyedInstanceId` so the caller has a record of what was removed.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-destroy --input '{
  "gameObjectRef": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-destroy --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.DestroyGameObjectResult"
    }
  },
  "$defs": {
    "AIGD.DestroyGameObjectResult": {
      "type": "object",
      "properties": {
        "DestroyedName": {
          "type": "string",
          "description": "Name of the destroyed GameObject."
        },
        "DestroyedPath": {
          "type": "string",
          "description": "Hierarchy path of the destroyed GameObject."
        },
        "DestroyedInstanceId": {
          "type": "integer",
          "description": "Instance ID of the destroyed GameObject."
        }
      },
      "required": [
        "DestroyedInstanceId"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

