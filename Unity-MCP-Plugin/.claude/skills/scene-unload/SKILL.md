---
name: scene-unload
description: Unload an opened scene from the Unity Editor (asynchronously via `SceneManager.UnloadSceneAsync`). Use 'scene-list-opened' to find the scene name first.
---

# Scene / Unload

Unload scene from the Opened scenes in Unity Editor. Use 'scene-list-opened' tool to get the list of all opened scenes.

## Behavior

Runs `SceneManager.UnloadSceneAsync` on the main thread and awaits completion. Returns an `UnloadSceneResult` containing the scene name and an `AssetObjectRef` to its asset (or `null` if the scene was not backed by an asset on disk).

## How to Call

```bash
unity-mcp-cli run-tool scene-unload --input '{
  "name": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool scene-unload --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | Yes | Name of the loaded scene. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.UnloadSceneResult"
    }
  },
  "$defs": {
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
    },
    "System.Type": {
      "type": "string"
    },
    "AIGD.UnloadSceneResult": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string",
          "description": "Name of the unloaded scene."
        },
        "AssetObjectRef": {
          "$ref": "#/$defs/AIGD.AssetObjectRef",
          "description": "Reference to the unloaded scene asset."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

