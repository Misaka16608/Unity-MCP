---
name: assets-shader-list-all
description: List all shaders available in the project assets and packages, sorted by name. Use this to discover a valid `shaderName` for 'assets-material-create'.
---

# Assets / List Shaders

List all available shaders in the project assets and packages. Returns their names. Use this to find a shader name for 'assets-material-create' tool.

## Behavior

Enumerates shaders via `ShaderUtils.GetAllShaders`, filters out nulls, and returns the names alphabetically sorted.

## How to Call

```bash
unity-mcp-cli run-tool assets-shader-list-all --input '{
  "nothing": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool assets-shader-list-all --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/System.String-1"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

