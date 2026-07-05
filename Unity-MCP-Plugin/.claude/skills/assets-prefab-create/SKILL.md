---
name: assets-prefab-create
description: Create a Prefab (or Prefab Variant) at a project asset path. Source can be a scene GameObject (`gameObjectRef`) or an existing prefab asset (`sourcePrefabAssetPath`). Creates intermediate folders if missing. Use 'gameobject-find' to locate the source GameObject first.
---

# Assets / Prefab / Create

Create a prefab from a GameObject in the current active scene. The prefab will be saved in the project assets at the specified path. Creates folders recursively if they do not exist. If the source GameObject is already a prefab instance and 'connectGameObjectToPrefab' is true, a Prefab Variant is created automatically. To create a Prefab Variant from an existing prefab asset, provide 'sourcePrefabAssetPath' instead of 'gameObjectRef'. Use 'gameobject-find' tool to find the target GameObject first.

## Behavior

Creates intermediate folders along `prefabAssetPath` if they don't already exist. Returns an `AssetObjectRef` pointing at the new prefab asset.

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-create --input '{
  "prefabAssetPath": "string_value",
  "gameObjectRef": "string_value",
  "sourcePrefabAssetPath": "string_value",
  "connectGameObjectToPrefab": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-prefab-create --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | Yes | Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'. |
| `gameObjectRef` | `any` | No | Reference to a scene GameObject to create the prefab from. If the GameObject is already a prefab instance, a Prefab Variant is created when 'connectGameObjectToPrefab' is true. Optional if 'sourcePrefabAssetPath' is provided. |
| `sourcePrefabAssetPath` | `string` | No | Path to an existing prefab asset to create a Prefab Variant from (e.g. 'Assets/Prefabs/Base.prefab'). When provided, a temporary instance is created, saved as a Prefab Variant, and cleaned up. Optional if 'gameObjectRef' is provided. |
| `connectGameObjectToPrefab` | `boolean` | No | If true, the scene GameObject will be connected to the new prefab (becoming a prefab instance). If the source is already a prefab instance, this creates a Prefab Variant. If false, the prefab asset is created but the scene GameObject remains unchanged. Ignored when 'sourcePrefabAssetPath' is used (always creates a variant). |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.AssetObjectRef",
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "AIGD.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    }
  },
  "required": [
    "result"
  ]
}
```

