---
name: screenshot-scene-view
description: Capture a screenshot from the Unity Editor Scene View at the requested size. Renders via the Scene View's active camera onto a temporary `RenderTexture`. Requires an open Scene View.
---

# Screenshot / Scene View

Captures a screenshot from the Unity Editor Scene View and returns it as an image. Returns the image directly for visual inspection by the LLM.

## Behavior

Uses `SceneView.lastActiveSceneView` (falls back to the first window in `SceneView.sceneViews`). Allocates a temporary `RenderTexture`, swaps it onto the Scene View's camera, calls `Render`, reads back with `Texture2D.ReadPixels`, encodes PNG, and restores the camera's prior `targetTexture`. Throws/errors when no Scene View is open or the Scene View camera is null.

## How to Call

```bash
unity-mcp-cli run-tool screenshot-scene-view --input '{
  "width": 0,
  "height": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool screenshot-scene-view --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `width` | `integer` | No | Width of the screenshot in pixels. |
| `height` | `integer` | No | Height of the screenshot in pixels. |

## Output

This tool does not return structured output.

