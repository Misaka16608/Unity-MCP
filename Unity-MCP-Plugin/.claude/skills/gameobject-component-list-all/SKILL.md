---
name: gameobject-component-list-all
description: List the fully-qualified C# type names of every concrete `UnityEngine.Component` subclass available in the project. Paginated (default 5/page, max 500). Use this to find a valid `componentName` for 'gameobject-component-add'.
---

# GameObject / Component / List All

List C# class names extended from UnityEngine.Component. Use this to find component type names for 'gameobject-component-add' tool. Results are paginated to avoid overwhelming responses.

## Behavior

Enumerates `AllComponentTypes` (every non-abstract subclass of `UnityEngine.Component`), filters by `search` if supplied, then returns a `ComponentListResult` containing the requested page plus `TotalCount` / `TotalPages` so the caller can iterate.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-list-all --input '{
  "search": "string_value",
  "page": 0,
  "pageSize": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-component-list-all --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `search` | `string` | No | Substring for searching components. Could be empty. |
| `page` | `integer` | No | Page number (0-based). Default is 0. |
| `pageSize` | `integer` | No | Number of items per page. Default is 5. Max is 500. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.ComponentListResult"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.ComponentListResult": {
      "type": "object",
      "properties": {
        "Items": {
          "$ref": "#/$defs/System.String-1",
          "description": "Array of component type names for the current page."
        },
        "Page": {
          "type": "integer",
          "description": "Current page number (0-based)."
        },
        "PageSize": {
          "type": "integer",
          "description": "Number of items per page."
        },
        "TotalCount": {
          "type": "integer",
          "description": "Total number of matching components."
        },
        "TotalPages": {
          "type": "integer",
          "description": "Total number of pages available."
        }
      },
      "required": [
        "Page",
        "PageSize",
        "TotalCount",
        "TotalPages"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

