---
name: gameobject-create
description: Create a new GameObject in the currently opened Prefab or active Scene, optionally parented under another GameObject and pre-positioned. Pass `primitiveType` to spawn a Unity primitive (Cube, Sphere, etc.) instead of an empty GameObject.
---

# GameObject / Create

Create a new GameObject in opened Prefab or in a Scene. If needed - provide proper 'position', 'rotation' and 'scale' to reduce amount of operations.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-create --input '{
  "name": "string_value",
  "parentGameObjectRef": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "scale": "string_value",
  "isLocalSpace": false,
  "primitiveType": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-create --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | Yes | Name of the new GameObject. |
| `parentGameObjectRef` | `any` | No | Parent GameObject reference. If not provided, the GameObject will be created at the root of the scene or prefab. |
| `position` | `any` | No | Transform position of the GameObject. |
| `rotation` | `any` | No | Transform rotation of the GameObject. Euler angles in degrees. |
| `scale` | `any` | No | Transform scale of the GameObject. |
| `isLocalSpace` | `boolean` | No | World or Local space of transform. |
| `primitiveType` | `any` | No |  |

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

