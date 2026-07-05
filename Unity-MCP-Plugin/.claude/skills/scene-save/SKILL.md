---
name: scene-save
description: Save an opened scene back to its asset file (or to a new path when `path` is provided). When `openedSceneName` is empty, saves the currently active scene. Use 'scene-list-opened' to find the scene name first.
---

# Scene / Save

Save Opened scene to the asset file. Use 'scene-list-opened' tool to get the list of all opened scenes.

## Validation

Throws if the scene cannot be resolved, has no existing path AND no override path was supplied, or the supplied path does not end with `.unity`. On `EditorSceneManager.SaveScene` failure, surfaces an error with the current opened-scenes list for diagnosis.

## How to Call

```bash
unity-mcp-cli run-tool scene-save --input '{
  "openedSceneName": "string_value",
  "path": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool scene-save --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `openedSceneName` | `string` | No | Name of the opened scene that should be saved. Could be empty if need to save the current active scene. |
| `path` | `string` | No | Path to the scene file. Should end with ".unity". If null or empty save to the existed scene asset file. |

## Output

This tool does not return structured output.

