---
name: gameobject-component-get
description: Get detailed information about a specific Component on a GameObject — type, enabled state, and (optionally) serialized fields and properties. Supports token-saving path-scoped reads via `paths` or `viewQuery`. Use 'gameobject-find' to list components first.
---

# GameObject / Component / Get

Get detailed information about a specific Component on a GameObject. Returns component type, enabled state, and optionally serialized fields and properties. Use this to inspect component data before modifying it. Use 'gameobject-find' tool to get the list of all components on the GameObject.

## Path-scoped reads (token-saving)

Supply `paths` (a list of paths) to read only the listed fields/elements via `Reflector.TryReadAt`, or `viewQuery` (a `ViewQuery`) to navigate to a subtree and/or filter by name regex / max depth / type via `Reflector.View`. The result is returned in the `View` field of the response, and the legacy `Fields`/`Properties` lists are skipped. These two parameters are mutually exclusive — supply at most one.

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-get --input '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "includeFields": false,
  "includeProperties": false,
  "deepSerialization": false,
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-component-get --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `componentRef` | `any` | Yes | Component reference. Used to find a Component at GameObject. |
| `includeFields` | `boolean` | No | Include serialized fields of the component. |
| `includeProperties` | `boolean` | No | Include serialized properties of the component. |
| `deepSerialization` | `boolean` | No | Performs deep serialization including all nested objects. Otherwise, only serializes top-level members. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. When supplied, the legacy 'Fields'/'Properties' lists are skipped and the result is returned in 'View'. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View. When supplied, the legacy 'Fields'/'Properties' lists are skipped and the filtered subtree is returned in 'View'. Mutually exclusive with 'paths'. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.GetComponentResponse"
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
    "System.Collections.Generic.List(com.IvanMurzak.ReflectorNet.Model.SerializedMember)": {
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
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "AIGD.GetComponentResponse": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the component for future operations."
        },
        "Index": {
          "type": "integer",
          "description": "Index of the component in the GameObject's component list."
        },
        "Component": {
          "$ref": "#/$defs/AIGD.ComponentDataShallow",
          "description": "Basic component information (type, enabled state)."
        },
        "Fields": {
          "$ref": "#/$defs/System.Collections.Generic.List(com.IvanMurzak.ReflectorNet.Model.SerializedMember)",
          "description": "Serialized fields of the component. Populated only on the legacy code path (no 'paths' / no 'viewQuery')."
        },
        "Properties": {
          "$ref": "#/$defs/System.Collections.Generic.List(com.IvanMurzak.ReflectorNet.Model.SerializedMember)",
          "description": "Serialized properties of the component. Populated only on the legacy code path (no 'paths' / no 'viewQuery')."
        },
        "View": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' was supplied. Null otherwise."
        }
      },
      "required": [
        "Index"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

