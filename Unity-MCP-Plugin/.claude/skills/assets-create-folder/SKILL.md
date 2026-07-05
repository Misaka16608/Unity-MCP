---
name: assets-create-folder
description: Create a new folder under a parent folder inside 'Assets/'. The parent path must start with 'Assets/' and every intermediate folder in it must already exist. Refreshes the AssetDatabase at the end and returns the GUID(s) of the created folder(s).
---

# Assets / Create Folder

Creates a new folder in the specified parent folder. The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end. Returns the GUID of the newly created folder, if successful.

## Validation

- `NewFolderName` must be non-empty and must not contain any of `/`, `\`, `<`, `>`, `:`, `"`, `|`, `?`, `*`, or control characters (these checks are cross-platform even on Linux/Mac).
- `ParentFolderPath` must already exist as an `AssetDatabase.IsValidFolder` path.
- A folder with the same target name must not already exist under the parent.

## How to Call

```bash
unity-mcp-cli run-tool assets-create-folder --input '{
  "inputs": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-create-folder --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `inputs` | `any` | Yes | The paths for the folders to create. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/AIGD.CreateFolderResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.CreateFolderResponse": {
      "type": "object",
      "properties": {
        "CreatedFolderGuids": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of GUIDs of created folders."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of errors encountered during folder creation."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

