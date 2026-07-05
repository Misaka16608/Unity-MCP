---
name: scene-open
description: Open a Unity scene asset in Single or Additive mode. Returns the post-open list of all opened scenes. Use 'assets-find' to locate the scene asset first.
---

# Scene / Open

Open scene from the project asset file. Use 'assets-find' tool to find the scene asset first.

  - `Single` — closes the currently opened scenes and opens this one.
  - `Additive` — keeps the currently opened scenes and opens this one alongside them.

## How to Call

```bash
unity-mcp-cli run-tool scene-open --input '{
  "sceneRef": "string_value",
  "loadSceneMode": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool scene-open --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sceneRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |
| `loadSceneMode` | `string` | No | Open scene mode. Single: closes the current scenes and opens a new one. Additive: keeps the current scene and opens additional one. |

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

