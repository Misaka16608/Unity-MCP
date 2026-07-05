---
name: gameobject-modify
description: Modify GameObject fields and properties in opened Prefab or in a Scene. You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects. Three modification surfaces are available per GameObject (gameObjectDiffs, pathPatchesPerGameObject, jsonPatchesPerGameObject) — see the skill body for details.
---

# GameObject / Modify

## Three modification surfaces

Per GameObject — parallel arrays must have the same length as `gameObjectRefs`:

1. `gameObjectDiffs` — full `SerializedMember` diff per GameObject (legacy, backwards compatible).
2. `pathPatchesPerGameObject` — list of `{path, value}` patches per GameObject routed through `Reflector.TryModifyAt`; atomic per-path modification.
3. `jsonPatchesPerGameObject` — JSON Merge Patch per GameObject routed through `Reflector.TryPatch`.

When more than one is supplied for the same GameObject they run in this order: `jsonPatch` → `pathPatches` → `diff`. At least one of the three is required.

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-modify --input '{
  "gameObjectRefs": "string_value",
  "gameObjectDiffs": "string_value",
  "pathPatchesPerGameObject": "string_value",
  "jsonPatchesPerGameObject": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-modify --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRefs` | `any` | Yes | Array of GameObjects in opened Prefab or in the active Scene. |
| `gameObjectDiffs` | `any` | No | Optional. Each item in the array represents a GameObject modification of the 'gameObjectRefs' at the same index. Usually a GameObject is a container for components. Each component may have fields and properties for modification. If you need to modify components of a GameObject, please use 'gameobject-component-modify' tool. Ignore values that should not be modified. Any unknown or wrong located fields and properties will be ignored. Check the result of this command to see what was changed. The ignored fields and properties will be listed. |
| `pathPatchesPerGameObject` | `any` | No | Optional. Per-GameObject list of path-scoped patches routed through Reflector.TryModifyAt. Outer index aligns with 'gameObjectRefs'; inner list contains {path, value} entries. Pass null or omit for GameObjects that should not receive path patches. |
| `jsonPatchesPerGameObject` | `any` | No | Optional. Per-GameObject JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through Reflector.TryPatch. Outer index aligns with 'gameObjectRefs'. Pass null or omit for GameObjects that should not receive a JSON patch. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.Logs"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.LogEntry": {
      "type": "object",
      "properties": {
        "Depth": {
          "type": "integer"
        },
        "Message": {
          "type": "string"
        },
        "Type": {
          "type": "string",
          "enum": [
            "Trace",
            "Debug",
            "Info",
            "Success",
            "Warning",
            "Error",
            "Critical"
          ]
        }
      },
      "required": [
        "Depth",
        "Type"
      ]
    },
    "com.IvanMurzak.ReflectorNet.Model.Logs": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.LogEntry"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

