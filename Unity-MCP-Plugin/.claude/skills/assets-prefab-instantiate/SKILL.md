---
name: assets-prefab-instantiate
description: Instantiate a prefab into the currently active scene at an optional position/rotation/scale, parented under an optional scene GameObject path. Use 'assets-find' to locate the prefab asset first.
---

# Assets / Prefab / Instantiate

Instantiates prefab in the current active scene. Use 'assets-find' tool to find prefab assets in the project.

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-instantiate --input '{
  "prefabAssetPath": "string_value",
  "gameObjectPath": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "scale": "string_value",
  "isLocalSpace": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-prefab-instantiate --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | Yes | Prefab asset path. |
| `gameObjectPath` | `string` | Yes | GameObject path in the current active scene. |
| `position` | `any` | No | Transform position of the GameObject. |
| `rotation` | `any` | No | Transform rotation of the GameObject. Euler angles in degrees. |
| `scale` | `any` | No | Transform scale of the GameObject. |
| `isLocalSpace` | `boolean` | No | World or Local space of transform. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "AIGD.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    }
  },
  "required": [
    "result"
  ]
}
```

