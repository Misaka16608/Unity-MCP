---
name: assets-prefab-open
description: Open the prefab edit stage for a prefab instance or prefab asset GameObject. Modifications inside the edit stage propagate to all instances. Pair with 'assets-prefab-close' to exit the stage when done.
---

# Assets / Prefab / Open

Open prefab edit mode for a specific GameObject. In the Edit mode you can modify the prefab. The modification will be applied to all instances of the prefab across the project. Note: Please use 'assets-prefab-close' tool later to exit prefab editing mode.

## Behavior

Asset-side GameObjects open via the simple `OpenPrefab(path)` overload. Scene-instance GameObjects open via `OpenPrefab(path, gameObject)` so the editor remembers which instance prompted the edit. Editor windows are repainted before returning. Throws when the GameObject cannot be resolved or the stage fails to open.

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-open --input '{
  "gameObjectRef": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-prefab-open --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | GameObject that represents prefab instance of an original prefab GameObject. |

## Output

This tool does not return structured output.

