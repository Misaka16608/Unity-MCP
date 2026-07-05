---
name: gameobject-component-modify
description: Modify a specific Component on a GameObject in opened Prefab or in a Scene. Allows direct modification of component fields and properties without wrapping in GameObject structure. Use 'gameobject-component-get' first to inspect the component structure before modifying. Three modification surfaces are available (componentDiff, pathPatches, jsonPatch) — see the skill body for details.
---

# GameObject / Component / Modify

## Three modification surfaces

Use whichever fits the task:

1. `componentDiff` — full `SerializedMember` diff (legacy, backwards compatible).
2. `pathPatches` — list of `{path, value}` pairs routed through `Reflector.TryModifyAt`; atomic per-path modification, multiple entries can target different depths.
3. `jsonPatch` — a JSON Merge Patch (RFC 7396, extended with `[i]`/`[key]` notation) routed through `Reflector.TryPatch`; multiple fields at any depth in a single call.

When more than one is supplied they run in this order: `jsonPatch` → `pathPatches` → `componentDiff`. At least one is required.

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-modify --input '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "componentDiff": "string_value",
  "pathPatches": "string_value",
  "jsonPatch": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-component-modify --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `componentRef` | `any` | Yes | Component reference. Used to find a Component at GameObject. |
| `componentDiff` | `any` | No | Optional. The full component data to apply (legacy path). Should contain 'fields' and/or 'props' with the values to modify.
Only include the fields/properties you want to change.
Any unknown or invalid fields and properties will be reported in the response. |
| `pathPatches` | `any` | No | Optional. List of path-scoped patches routed through Reflector.TryModifyAt. Each entry targets one field/element/entry by path. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. |
| `jsonPatch` | `any` | No | Optional. JSON Merge Patch (RFC 7396, extended with [i]/[key] keys) routed through Reflector.TryPatch. Allows multiple fields at any depth to be updated in a single call. Use '$type' for compatible-subtype replacement. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.ModifyComponentResponse"
    }
  },
  "$defs": {
    "AIGD.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
    },
    "AIGD.ComponentDataShallow": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "typeName": {
          "type": "string"
        },
        "isEnabled": {
          "type": "string",
          "enum": [
            "False",
            "True",
            "NA"
          ]
        }
      },
      "required": [
        "instanceID",
        "isEnabled"
      ]
    },
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.ModifyComponentResponse": {
      "type": "object",
      "properties": {
        "Success": {
          "type": "boolean",
          "description": "Whether the modification was successful."
        },
        "Reference": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the modified component."
        },
        "Index": {
          "type": "integer",
          "description": "Index of the component in the GameObject's component list."
        },
        "Component": {
          "$ref": "#/$defs/AIGD.ComponentDataShallow",
          "description": "Updated component information after modification."
        },
        "Logs": {
          "$ref": "#/$defs/System.String-1",
          "description": "Log of modifications made and any warnings/errors encountered."
        }
      },
      "required": [
        "Success",
        "Index"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

