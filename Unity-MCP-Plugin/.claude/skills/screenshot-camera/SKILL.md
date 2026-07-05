---
name: screenshot-camera
description: Capture a screenshot from a Unity `Camera` and return it as a PNG image for direct LLM inspection. Falls back to `Camera.main` (then any active camera) when `cameraRef` is null. Width and height are capped to keep response size manageable.
---

# Screenshot / Camera

Captures a screenshot from a camera and returns it as an image. If no camera is specified, uses the Main Camera. Returns the image directly for visual inspection by the LLM.

## Behavior

Allocates a temporary `RenderTexture`, swaps it onto the chosen camera, calls `Camera.Render`, reads back via `Texture2D.ReadPixels`, encodes as PNG, and restores the camera's prior `targetTexture`. Returns a `ResponseCallTool.Image` with `image/png` MIME and a descriptive caption.

## How to Call

```bash
unity-mcp-cli run-tool screenshot-camera --input '{
  "cameraRef": "string_value",
  "width": 0,
  "height": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool screenshot-camera --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `cameraRef` | `any` | No | Reference to the camera GameObject. If not specified, uses the Main Camera. |
| `width` | `integer` | No | Width of the screenshot in pixels. |
| `height` | `integer` | No | Height of the screenshot in pixels. |

## Output

This tool does not return structured output.

