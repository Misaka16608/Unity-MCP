/*
┌──────────────────────────────────────────────────────────────────┐
│  Purpose: Fire-and-forget build trigger. Calls the build menu  │
│           item and returns immediately. Does NOT block.        │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public static partial class Tool_Build
    {
        public const string BuildTriggerToolId = "build-trigger";

        // 幂等守卫：防止 MCP 超时重试/请求重放导致同一构建被触发多次。
        // 1. 已有触发挂起（delayCall 未执行或构建进行中）→ 直接拒绝
        // 2. 距上次触发执行完成不足 30s → 拒绝（与 script-execute 冷却语义一致）
        private static bool s_TriggerPending;
        private static DateTime s_LastTriggerFinishTime = DateTime.MinValue;
        private static readonly TimeSpan TriggerCooldown = TimeSpan.FromSeconds(30);

        [AiTool(BuildTriggerToolId, Title = "Build / Trigger", OpenWorldHint = true)]
        [AiSkillDescription("Triggers the full Android build pipeline (AB → CDN → HotFix → APK) " +
            "without blocking. Returns immediately. Monitor Builds/build_status.json.")]
        [Description("Fire-and-forget build trigger. Returns immediately. " +
            "Progress: Builds/build_status.json (ab_building → cdn_deploying → apk_building → complete/failed).")]
        public static string Trigger()
        {
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Build.Trigger");

            if (s_TriggerPending)
            {
                logger.LogWarning(
                    "build-trigger skipped — a trigger is already pending or a build is in progress.");
                return "Build trigger skipped — a trigger is already pending.";
            }

            if ((DateTime.UtcNow - s_LastTriggerFinishTime) < TriggerCooldown)
            {
                logger.LogWarning(
                    "build-trigger skipped — within cooldown after previous build trigger.");
                return "Build trigger skipped — within cooldown after previous trigger.";
            }

            if (IsBuildPipelineBusyOrRecentlyFinished())
            {
                logger.LogWarning(
                    "build-trigger skipped — build pipeline is busy or completed within the last 30s.");
                return "Build trigger skipped — build pipeline is busy or recently completed.";
            }

            s_TriggerPending = true;

            MainThread.Instance.RunAsync(() =>
            {
                try
                {
                    // 执行前再次检查：派发可能因主线程忙碌被推迟，
                    // 期间若有其他入口（如 script-execute）完成了构建，则放弃本次迟到触发。
                    if (IsBuildPipelineBusyOrRecentlyFinished())
                    {
                        logger.LogWarning(
                            "build-trigger execution skipped — a build completed while the trigger was pending.");
                        return;
                    }

                    EditorApplication.ExecuteMenuItem(
                        "AZWorkingCat/打包工具/完整构建+部署 Dev (AB → CDN → APK)");
                }
                finally
                {
                    s_TriggerPending = false;
                    s_LastTriggerFinishTime = DateTime.UtcNow;
                }
            });
            return "Build triggered. Status: Builds/build_status.json";
        }

        /// <summary>
        /// 读取项目构建状态文件：构建中（非终态）或终态写入时间在 30s 内时返回 true。
        /// 用于在触发执行前拦截因 MCP 请求重放/迟到 delayCall 产生的重复构建。
        /// 读取失败时放行（fail-open），避免误伤正常流程。
        /// </summary>
        private static bool IsBuildPipelineBusyOrRecentlyFinished()
        {
            try
            {
                string statusPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "Builds", "build_status.json");
                if (!File.Exists(statusPath))
                {
                    return false;
                }

                string json = File.ReadAllText(statusPath);
                if (json.Contains("hotfix_compiling") ||
                    json.Contains("ab_building") ||
                    json.Contains("cdn_deploying") ||
                    json.Contains("apk_building"))
                {
                    return true;
                }

                return (DateTime.UtcNow - File.GetLastWriteTimeUtc(statusPath)) < TriggerCooldown;
            }
            catch (Exception ex)
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Tool_Build.Trigger");
                logger.LogWarning(ex, "Failed to read build status file — allowing trigger.");
                return false;
            }
        }
    }
}
