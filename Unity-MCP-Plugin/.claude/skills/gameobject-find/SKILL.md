---
name: gameobject-find
description: Find a specific GameObject in the opened Prefab (preferred when present) or the active Scene. Optionally include editable data, components preview, bounds, and limited hierarchy. Supports token-saving path-scoped reads via `paths` or `viewQuery`.
---

# GameObject / Find

Finds specific GameObject by provided information in opened Prefab or in a Scene. First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. If no opened Prefab it looks into current active scene. Returns GameObject information and its children. Also, it returns Components preview just for the target GameObject.

## Toggles (all default `false` to keep responses small)

- `includeData` — full editable GameObject data (tag, layer, etc.).
- `includeComponents` — attached components references.
- `includeBounds` — 3D bounds.
- `includeHierarchy` — hierarchy metadata.
- `hierarchyDepth` (default 0) — depth of the hierarchy to include. `0` = target only, `1` = one layer below, etc.

## Path-scoped reads (token-saving)

Supply `paths` (a list of paths) to read only the listed fields/elements via `Reflector.TryReadAt`, or `viewQuery` (a `ViewQuery`) to navigate to a subtree and/or filter by name regex / max depth / type via `Reflector.View`. When either is supplied, the result populates `Data` on the returned `GameObjectData` and overrides `includeData` (which would otherwise produce a full recursive serialization). These two parameters are mutually exclusive — supply at most one.

## Path syntax

`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-find --input '{
  "gameObjectRef": "string_value",
  "includeData": false,
  "includeComponents": false,
  "includeBounds": false,
  "includeHierarchy": false,
  "hierarchyDepth": 0,
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-find --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `includeData` | `boolean` | No | Include editable GameObject data (tag, layer, etc). |
| `includeComponents` | `boolean` | No | Include attached components references. |
| `includeBounds` | `boolean` | No | Include 3D bounds of the GameObject. |
| `includeHierarchy` | `boolean` | No | Include hierarchy metadata. |
| `hierarchyDepth` | `integer` | No | Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. When supplied, replaces 'includeData'-style full serialization with a path-scoped aggregate. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View. When supplied, replaces 'includeData'-style full serialization with the filtered subtree. Mutually exclusive with 'paths'. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.GameObjectData"
    }
  },
  "$defs": {
    "AIGD.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
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
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "System.Type": {
      "type": "string"
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
    "UnityEngine.Bounds": {
      "type": "object",
      "properties": {
        "center": {
          "type": "object",
          "properties": {
            "x": {
              "type": "number"
            },
            "y": {
              "type": "number"
            },
            "z": {
              "type": "number"
            }
          },
          "required": [
            "x",
            "y",
            "z"
          ]
        },
        "size": {
          "type": "object",
          "properties": {
            "x": {
              "type": "number"
            },
            "y": {
              "type": "number"
            },
            "z": {
              "type": "number"
            }
          },
          "required": [
            "x",
            "y",
            "z"
          ]
        }
      },
      "required": [
        "center",
        "size"
      ],
      "additionalProperties": false
    },
    "AIGD.GameObjectMetadata": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "path": {
          "type": "string"
        },
        "name": {
          "type": "string"
        },
        "sceneName": {
          "type": "string"
        },
        "tag": {
          "type": "string"
        },
        "activeSelf": {
          "type": "boolean"
        },
        "activeInHierarchy": {
          "type": "boolean"
        },
        "children": {
          "$ref": "#/$defs/System.Collections.Generic.List(AIGD.GameObjectMetadata)"
        }
      },
      "required": [
        "instanceID",
        "activeSelf",
        "activeInHierarchy"
      ]
    },
    "System.Collections.Generic.List(AIGD.GameObjectMetadata)": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.GameObjectMetadata"
      }
    },
    "AIGD.ComponentDataShallow-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ComponentDataShallow"
      }
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
    "AIGD.GameObjectData": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/AIGD.GameObjectRef",
          "description": "Find GameObject in opened Prefab or in the active Scene."
        },
        "Data": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "GameObject editable data (tag, layer, etc)."
        },
        "Bounds": {
          "$ref": "#/$defs/UnityEngine.Bounds",
          "description": "Bounds of the GameObject."
        },
        "Hierarchy": {
          "$ref": "#/$defs/AIGD.GameObjectMetadata",
          "description": "Hierarchy metadata of the GameObject."
        },
        "Components": {
          "$ref": "#/$defs/AIGD.ComponentDataShallow-1",
          "description": "Attached components shallow data of the GameObject (Read-only, use Component modification tool for modification)."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

