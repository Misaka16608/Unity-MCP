---
name: assets-prefab-get-data
description: Retrieve the complete GameObject hierarchy of a prefab asset in a single call. Loads the prefab directly — no need to open/close the prefab stage. Supports depth control, bounds, serialized component data, and token-saving path-scoped reads. Use 'assets-find' to locate the prefab first.
---

# Assets / Prefab / Get Data

Retrieve the full GameObject hierarchy of a prefab asset without opening the prefab stage. This is the prefab equivalent of `scene-get-data` — it returns the root GameObject with its complete descendant tree, components, bounds, and serialized data in a single call. Unlike `scene-get-data` which returns a list of root GameObjects, this tool returns a single root since prefabs always have exactly one root.

## Behavior

Loads the prefab asset via `AssetDatabase.LoadAssetAtPath`, builds the root `GameObjectData` with hierarchy metadata down to `includeChildrenDepth`, and returns a `PrefabData` envelope with asset identity fields and the root GameObject. Supports path-scoped reads (`paths` / `viewQuery`) that run against the root GameObject's serialized data.

When `gameObjectRef` is provided (a scene prefab instance), the source prefab asset path is resolved via `PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot` and that prefab is loaded.

## Comparison with scene-get-data

| Feature | `scene-get-data` | `assets-prefab-get-data` |
|---|---|---|
| Target | Opened scene | Prefab asset (any `.prefab` file) |
| Root GameObjects | List (multiple roots) | Single (always one root) |
| Requires open stage | No (scene is open) | No (loads asset directly) |
| Hierarchy depth | `includeChildrenDepth` | `includeChildrenDepth` |
| Component data | `includeData` | `includeData` |
| Bounds | `includeBounds` | `includeBounds` |
| Path-scoped reads | `paths` / `viewQuery` | `paths` / `viewQuery` |

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-get-data --input '{
  "prefabAssetPath": "Assets/Prefabs/MyPrefab.prefab",
  "includeChildrenDepth": 3,
  "includeBounds": false,
  "includeData": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-prefab-get-data --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | No | Path to the prefab asset. Must start with 'Assets/' and end with '.prefab'. Use 'assets-find' to locate prefab assets first. Mutually exclusive with 'gameObjectRef'. |
| `gameObjectRef` | `any` | No | Reference to a scene GameObject that is a prefab instance. When provided, the source prefab asset is resolved from the instance. Mutually exclusive with 'prefabAssetPath'. |
| `includeChildrenDepth` | `integer` | No | Depth of the hierarchy to include. Default 3. Set to a high value (e.g. 99) to include all descendants. |
| `includeBounds` | `boolean` | No | If true, includes bounding box information for GameObjects. |
| `includeData` | `boolean` | No | If true, includes serialized component data for GameObjects. |
| `paths` | `array` | No | Optional. List of paths to read individually via Reflector.TryReadAt against the prefab root GameObject's serialized data. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View on the prefab root GameObject's serialized data. Mutually exclusive with 'paths'. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.PrefabData",
      "description": "Full hierarchy data for a prefab asset."
    }
  },
  "$defs": {
    "AIGD.PrefabData": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path to the prefab asset within the project. Starts with 'Assets/'."
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the prefab asset."
        },
        "name": {
          "type": "string",
          "description": "Name of the prefab asset."
        },
        "rootGameObject": {
          "$ref": "#/$defs/AIGD.GameObjectData",
          "description": "Root GameObject of the prefab with its full hierarchy, components, bounds, and serialized data."
        },
        "isValid": {
          "type": "boolean",
          "description": "If true, the prefab was loaded successfully and the root GameObject is valid."
        },
        "totalGameObjectCount": {
          "type": "integer",
          "description": "Total number of GameObjects in the prefab hierarchy."
        },
        "data": {
          "type": "object",
          "description": "Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' is supplied. Null otherwise."
        }
      },
      "required": ["assetPath", "assetGuid", "name", "isValid", "totalGameObjectCount"]
    }
  },
  "required": ["result"]
}
```
