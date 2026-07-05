---
name: gameobject-set-parent
description: Reparent a batch of GameObjects under a new parent in the currently opened Prefab or active Scene. Per-item failures are reported in the returned status string instead of aborting the batch. Use 'gameobject-find' to locate the GameObjects first.
---

# GameObject / Set Parent

Set parent GameObject to list of GameObjects in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObjects first.

## Behavior

Iterates `gameObjectRefs` and reparents each one independently; per-item resolve errors are appended to the returned status string instead of throwing. After the loop, if at least one reparent succeeded, marks the active scene dirty and repaints editor windows.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-set-parent --input '{
  "gameObjectRefs": "string_value",
  "parentGameObjectRef": "string_value",
  "worldPositionStays": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool gameobject-set-parent --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRefs` | `any` | Yes | List of references to the GameObjects to set new parent. |
| `parentGameObjectRef` | `any` | Yes | Reference to the parent GameObject. |
| `worldPositionStays` | `boolean` | No | A boolean flag indicating whether the GameObject's world position should remain unchanged when setting its parent. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

