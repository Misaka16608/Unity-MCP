---
name: gameobject-component-destroy
description: Destroy one or more Components from a target GameObject. Missing (null) components are skipped — they cannot be destroyed. Use 'gameobject-find' and 'gameobject-component-get' to identify the components first.
---

# GameObject / Component / Destroy

Destroy one or many components from target GameObject. Can't destroy missed components. Use 'gameobject-find' tool to find the target GameObject and 'gameobject-component-get' to get component details first.

## Behavior

Iterates `go.GetComponents<Component>()`, skipping null entries (missing scripts). For each non-null component that matches one of `destroyComponentRefs`, the tool snapshots a `ComponentRef`, calls `Object.DestroyImmediate`, and records the destroyed reference. If no component matches at all, throws with the help text from `Error.NotFoundComponents` (which includes a preview of all available components on the GameObject).

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-destroy --input '{
  "gameObjectRef": "string_value",
  "destroyComponentRefs": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-component-destroy --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `destroyComponentRefs` | `any` | Yes | Component reference array. Used to find Component at GameObject. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.DestroyComponentsResponse"
    }
  },
  "$defs": {
    "AIGD.ComponentRefList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ComponentRef",
        "description": "Component reference. Used to find a Component at GameObject."
      },
      "description": "Component reference array. Used to find Component at GameObject."
    },
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
    "AIGD.DestroyComponentsResponse": {
      "type": "object",
      "properties": {
        "DestroyedComponents": {
          "$ref": "#/$defs/AIGD.ComponentRefList",
          "description": "List of destroyed components."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

