---
name: assets-get-data
description: Get asset data from the asset file in the Unity project — every serializable field and property. Supports token-saving path-scoped reads via `paths` or `viewQuery`. Use 'assets-find' to find the asset first.
---

# Assets / Get Data

Get asset data from the asset file in the Unity project. It includes all serializable fields and properties of the asset. Use 'assets-find' tool to find asset before using this tool.

## Path-scoped reads (token-saving)

Supply `paths` (a list of paths) to read only the listed fields/elements via `Reflector.TryReadAt`, or `viewQuery` (a `ViewQuery`) to navigate to a subtree and/or filter by name regex / max depth / type via `Reflector.View`. These two parameters are mutually exclusive — supply at most one. When neither is supplied the full asset is serialized (backwards compatible).

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool assets-get-data --input '{
  "assetRef": "string_value",
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-get-data --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
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

