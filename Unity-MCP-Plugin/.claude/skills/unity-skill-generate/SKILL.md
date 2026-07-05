---
name: unity-skill-generate
description: "Regenerate every `SKILL.md` from the project's currently-registered MCP tools into the configured skills folder (or a project-relative override path). Writes the YAML `description:` from `[AiSkillDescription]` and the body from `[AiSkillBody]`."
---

# Skill (Tool) / Generate All

Generate all skills from the existed Tools in the Unity Project.

## Behavior

Creates the destination folder if missing, then invokes `McpPluginInstance.GenerateSkillFiles(...)` to emit a `SKILL.md` per registered MCP tool. The plugin's `SkillsPath` is temporarily swapped to the target folder and restored in `finally` so the on-disk configuration is unchanged after the call returns.

## How to Call

```bash
unity-mcp-cli run-system-tool unity-skill-generate --input '{
  "path": "string_value"
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-system-tool unity-skill-generate --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | `string` | No | Path to the skills folder. If null or empty, the default path will be used. |

## Output

This tool does not return structured output.

