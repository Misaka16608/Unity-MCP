/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Skills;
using com.IvanMurzak.Unity.MCP.Editor.API;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Unity-specific skill file generator that emits <c>unity-mcp-cli run-tool</c>
    /// commands instead of <c>curl</c> HTTP API calls in the "How to Call" section.
    /// Authorization is handled automatically by the CLI (reads config file),
    /// so the authorization example block is omitted.
    /// </summary>
    public class UnitySkillFileGenerator : SkillFileGenerator
    {
        public UnitySkillFileGenerator() : base()
        {
        }
        public UnitySkillFileGenerator(ILogger? logger = null) : base(logger)
        {
        }

        /// <summary>
        /// Authorization is handled automatically by the CLI from the project config file.
        /// No need to show a separate authorization example in SKILL.md.
        /// </summary>
        public override bool IncludeAuthorizationExample => false;

        /// <summary>
        /// Description is already in the YAML front-matter — skip the duplicate paragraph
        /// after the title to save tokens.
        /// </summary>
        public override bool IncludeDescriptionBody => false;

        /// <summary>
        /// Descriptions are already shown in the parameter table — strip them from the
        /// Input JSON Schema to save tokens.
        /// </summary>
        public override bool IncludeInputSchemaPropertyDescriptions => false;

        /// <summary>
        /// Parameter types are already fully described in the input table —
        /// hide the Input JSON Schema block to eliminate intra-file $defs duplication.
        /// </summary>
        public override bool IncludeInputJsonSchema => false;

        /// <inheritdoc/>
        protected override void BuildHowToCallHeading(StringBuilder sb)
        {
            // sb.AppendLine("## Use command line");
            // sb.AppendLine();
        }

        /// <inheritdoc/>
        protected override void BuildToolCommand(StringBuilder sb, IRunTool tool, string host, string inputExample)
        {
            var command = tool.ToolType == McpToolType.System
                ? "run-system-tool"
                : "run-tool";
            sb.AppendLine("```bash");
            sb.AppendLine($"unity-mcp-cli {command} {tool.Name} --input '{inputExample}'");
            sb.AppendLine("```");
            sb.AppendLine();
            AppendInputFileHint(sb, tool, host, inputExample);
            sb.AppendLine();
        }

        /// <inheritdoc/>
        protected override void AppendInputFileHint(StringBuilder sb, IRunTool tool, string host, string inputExample)
        {
            if (inputExample == "{}")
                return;
            var command = tool.ToolType == McpToolType.System
                ? "run-system-tool"
                : "run-tool";
            sb.AppendLine($"> For complex input, save JSON to a file and use `unity-mcp-cli {command} {tool.Name} --input-file args.json`.");
            sb.AppendLine();
        }

        /// <inheritdoc/>
        /// <summary>
        /// Compact troubleshooting reference — CLI installation details are in the initial-setup skill.
        /// </summary>
        protected override void BuildTroubleshootingSections(StringBuilder sb)
        {
            sb.AppendLine("### Troubleshooting");
            sb.AppendLine();
            sb.AppendLine($"For CLI installation or connectivity issues, see the /{Skill_InitialSetup.SkillId} skill.");
        }
    }
}