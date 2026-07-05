---
name: assets-prefab-get-data
description: Retrieve the full GameObject hierarchy of a prefab asset in a single call — the prefab equivalent of scene-get-data. Use 'assets-find' to locate the prefab first.
---

# Assets / Prefab / Get Data

The prefab equivalent of `scene-get-data`. Loads a `.prefab` directly via `AssetDatabase.LoadAssetAtPath` — no need to open/close the prefab stage. Returns the root `GameObjectData` with children down to `includeChildrenDepth`, plus optional component data and bounds.

## Usage

```bash
unity-mcp-cli run-tool assets-prefab-get-data --input '{
  "prefabAssetPath": "Assets/Prefabs/MyPrefab.prefab",
  "includeChildrenDepth": 99
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | * | Path to `.prefab` file. Mutually exclusive with `gameObjectRef`. |
| `gameObjectRef` | `any` | * | Scene prefab instance reference. Resolves to its source prefab. |
| `includeChildrenDepth` | `int` | No | Hierarchy depth (default 3, use 99 for all). |
| `includeBounds` | `bool` | No | Include 3D bounds. |
| `includeData` | `bool` | No | Include serialized component data. |
| `paths` | `string[]` | No | Token-saving path-scoped read. Mutually exclusive with `viewQuery`. |
| `viewQuery` | `object` | No | Token-saving view query. Mutually exclusive with `paths`. |

> *Either `prefabAssetPath` or `gameObjectRef` must be provided.

## Output

Returns `PrefabData`:

| Field | Type | Description |
|-------|------|-------------|
| `assetPath` | `string` | Asset path in project. |
| `assetGuid` | `string` | Asset GUID. |
| `name` | `string` | Prefab name. |
| `rootGameObject` | `GameObjectData` | Root GameObject with hierarchy, components, bounds. |
| `isValid` | `bool` | Whether the prefab loaded successfully. |
| `totalGameObjectCount` | `int` | Total GameObjects in the hierarchy. |
| `data` | `object` | Path-scoped read / view query result (null if unused). |
