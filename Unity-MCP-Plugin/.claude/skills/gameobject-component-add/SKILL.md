---
name: gameobject-component-add
description: Add one or more Components to a GameObject in the opened Prefab or active Scene. Component types are looked up by full name (with namespace) or by class-name fallback. Use 'gameobject-find' to locate the host GameObject and 'gameobject-component-list-all' to discover valid component type names.
---

# GameObject / Component / Add

Add Component to GameObject in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first. Use 'gameobject-component-list-all' tool to find the component type names to add.

## Behavior

Per-name errors (unknown type, type not assignable to `UnityEngine.Component`, add-failed/duplicate) are accumulated in `response.Errors` / `response.Warnings` instead of throwing, so a single bad name does not abort the whole batch. Successful additions populate `response.AddedComponents` with `ComponentDataShallow` snapshots.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-add --input '{
  "componentNames": "string_value",
  "gameObjectRef": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-component-add --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `componentNames` | `any` | Yes | Full name of the Component. It should include full namespace path and the class name. |
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.AddComponentResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(AIGD.ComponentDataShallow)": {
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
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.AddComponentResponse": {
      "type": "object",
      "properties": {
        "AddedComponents": {
          "$ref": "#/$defs/System.Collections.Generic.List(AIGD.ComponentDataShallow)",
          "description": "List of successfully added components."
        },
        "Messages": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of success messages for added components."
        },
        "Warnings": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of warnings encountered during component addition."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of errors encountered during component addition."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

