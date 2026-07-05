---
name: object-get-data
description: Get serialized data for a Unity `UnityEngine.Object` — all serializable fields and properties. Supports token-saving path-scoped reads via `paths` or `viewQuery`. Pair with 'object-modify' when you need to write back.
---

# Object / Get Data

Get data of the specified Unity Object. Returns serialized data of the object including its properties and fields. If need to modify the data use 'object-modify' tool.

## Path-scoped reads (token-saving)

Supply `paths` (a list of paths) to read only the listed fields/elements via `Reflector.TryReadAt`, or `viewQuery` (a `ViewQuery`) to navigate to a subtree and/or filter by name regex / max depth / type via `Reflector.View`. These two parameters are mutually exclusive — supply at most one. When neither is supplied the full object is serialized (backwards compatible).

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool object-get-data --input '{
  "objectRef": "string_value",
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool object-get-data --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `objectRef` | `any` | Yes | Reference to UnityEngine.Object instance. It could be GameObject, Component, Asset, etc. Anything extended from UnityEngine.Object. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. Each path may target a different depth. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View — combines a starting Path, a case-insensitive NamePattern regex, MaxDepth, and an optional TypeFilter. Mutually exclusive with 'paths'. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    }
  },
  "required": [
    "result"
  ]
}
```

