---
name: assets-modify
description: Modify an asset file in the project. Use 'assets-get-data' first to inspect the asset structure before modifying. Not allowed to modify asset files in the 'Packages/' folder — modify them in 'Assets/'. Three modification surfaces are available (content, pathPatches, jsonPatch) — see the skill body for details.
---

# Assets / Modify

## Three modification surfaces

Use whichever fits the task:

1. `content` — full `SerializedMember` override (legacy, backwards compatible).
2. `pathPatches` — list of `{path, value}` pairs routed through `Reflector.TryModifyAt`.
3. `jsonPatch` — JSON Merge Patch routed through `Reflector.TryPatch`.

When more than one is supplied they run in this order: `jsonPatch` → `pathPatches` → `content`. At least one is required.

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool assets-modify --input '{
  "assetRef": "string_value",
  "content": "string_value",
  "pathPatches": "string_value",
  "jsonPatch": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-modify --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |
| `content` | `any` | No | Optional. The asset content. It overrides the existing asset content (legacy path). |
| `pathPatches` | `any` | No | Optional. List of path-scoped patches routed through Reflector.TryModifyAt. |
| `jsonPatch` | `any` | No | Optional. JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through Reflector.TryPatch. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/System.String-1"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

