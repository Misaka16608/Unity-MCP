---
name: assets-copy
description: Copy assets at given paths and store them at new paths. Refreshes the AssetDatabase at the end. Use 'assets-find' to locate the source assets first.
---

# Assets / Copy

Copy assets at given paths and store them at new paths. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before copying.

## Behavior

Each source/destination pair is copied in order. Per-pair errors are accumulated in the response instead of throwing, so a single bad pair does not abort the whole batch.

## How to Call

```bash
unity-mcp-cli run-tool assets-copy --input '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-copy --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourcePaths` | `any` | Yes | The paths of the assets to copy. |
| `destinationPaths` | `any` | Yes | The paths to store the copied assets. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.CopyAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(AIGD.AssetObjectRef)": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.AssetObjectRef",
        "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
      }
    },
    "AIGD.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "System.Type": {
      "type": "string"
    },
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.CopyAssetsResponse": {
      "type": "object",
      "properties": {
        "CopiedAssets": {
          "$ref": "#/$defs/System.Collections.Generic.List(AIGD.AssetObjectRef)",
          "description": "List of copied assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of errors encountered during copy operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

