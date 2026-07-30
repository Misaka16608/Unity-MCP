/*
┌──────────────────────────────────────────────────────────────────┐
│  Purpose: Fire-and-forget build trigger. Calls the build menu  │
│           item and returns immediately. Does NOT block.        │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public static partial class Tool_Build
    {
        public const string BuildTriggerToolId = "build-trigger";

        [AiTool(BuildTriggerToolId, Title = "Build / Trigger", OpenWorldHint = true)]
        [AiSkillDescription("Triggers the full Android build pipeline (AB → CDN → HotFix → APK) " +
            "without blocking. Returns immediately. Monitor Builds/build_status.json.")]
        [Description("Fire-and-forget build trigger. Returns immediately. " +
            "Progress: Builds/build_status.json (ab_building → cdn_deploying → apk_building → complete/failed).")]
        public static string Trigger()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.ExecuteMenuItem(
                    "AZWorkingCat/打包工具/完整构建+部署 (AB → CDN → APK)");
            };
            return "Build triggered. Status: Builds/build_status.json";
        }
    }
}
