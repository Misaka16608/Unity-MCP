---
name: scene-set-active
description: Mark an opened scene as the Editor's active scene (the one new GameObjects are added to and that's used as the default for many operations). Use 'scene-list-opened' to enumerate opened scenes first.
---

# Scene / Set Active

Set the specified opened scene as the active scene. Use 'scene-list-opened' tool to get the list of all opened scenes.

## Behavior

Resolves the `SceneAsset`, finds the matching opened scene (by name then by path), and calls `EditorSceneManager.SetActiveScene`. No-op if the scene is already active. Returns the post-call snapshot of opened scenes.

## How to Call

```bash
unity-mcp-cli run-tool scene-set-active --input '{
  "sceneRef": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool scene-set-active --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sceneRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.SceneDataShallow-1"
    }
  },
  "$defs": {
    "AIGD.SceneDataShallow": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string"
        },
        "IsLoaded": {
          "type": "boolean"
        },
        "IsDirty": {
          "type": "boolean"
        },
        "IsSubScene": {
          "type": "boolean"
        },
        "IsValidScene": {
          "type": "boolean",
          "description": "Whether this is a valid Scene. A Scene may be invalid if, for example, you tried to open a Scene that does not exist. In this case, the Scene returned from EditorSceneManager.OpenScene would return False for IsValid."
        },
        "RootCount": {
          "type": "integer"
        },
        "path": {
          "type": "string",
          "description": "Path to the Scene within the project. Starts with 'Assets/'"
        },
        "buildIndex": {
          "type": "integer",
          "description": "Build index of the Scene in the Build Settings."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "IsLoaded",
        "IsDirty",
        "IsSubScene",
        "IsValidScene",
        "RootCount",
        "buildIndex",
        "instanceID"
      ],
      "description": "Scene reference. Used to find a Scene."
    },
    "AIGD.SceneDataShallow-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.SceneDataShallow",
        "description": "Scene reference. Used to find a Scene."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

